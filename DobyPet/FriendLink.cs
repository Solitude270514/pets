using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;

namespace DobyPet
{
    /// <summary>兩隻狗之間傳的訊息。</summary>
    public sealed class FriendMsg
    {
        public string T { get; set; }       // hello / bye / visit-req / visit-ok / visit-no / visit-end
        public string From { get; set; }    // 發送者的 InstanceId
        public string Name { get; set; }
        public string Coat { get; set; }
        public string Id { get; set; }      // 拜訪編號，用來對應請求與回覆
    }

    /// <summary>
    /// 透過公開 MQTT 中繼站（EMQX，免註冊）跟朋友的電腦傳話。
    /// - 頻道名稱是配對碼的雜湊值，別人猜不到
    /// - 內容用配對碼衍生的金鑰做 AES-GCM 加密，就算被看到也讀不懂
    /// 斷線會自動重連。收到的訊息會切回 UI 執行緒再丟出 Received 事件。
    /// </summary>
    public sealed class FriendLink : IDisposable
    {
        const string Broker = "broker.emqx.io";
        const int Port = 8883;

        readonly string myId;
        readonly string topic;
        readonly byte[] key;
        readonly IMqttClient client;
        readonly SynchronizationContext ui;
        readonly CancellationTokenSource cts = new();

        public event Action<FriendMsg> Received;
        public event Action StatusChanged;

        public bool Connected => client.IsConnected;
        public string PeerName { get; private set; }
        public string PeerCoat { get; private set; }
        public string PeerId { get; private set; }
        DateTime lastPeer = DateTime.MinValue;

        /// <summary>60 秒內收到過朋友的訊息就算在線上。</summary>
        public bool PeerOnline => (DateTime.UtcNow - lastPeer).TotalSeconds < 60;

        public FriendLink(string code, string myId)
        {
            this.myId = myId;
            ui = SynchronizationContext.Current;
            code = NormalizeCode(code);
            topic = "dobypet/v1/" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("topic:" + code)))[..32].ToLowerInvariant();
            key = Rfc2898DeriveBytes.Pbkdf2(code, Encoding.UTF8.GetBytes("dobypet-v1"), 100_000, HashAlgorithmName.SHA256, 32);

            client = new MqttFactory().CreateMqttClient();
            client.ApplicationMessageReceivedAsync += e =>
            {
                var msg = Decrypt(e.ApplicationMessage.PayloadSegment);
                if (msg != null && msg.From != myId) ui.Post(_ => Handle(msg), null);
                return Task.CompletedTask;
            };
            client.DisconnectedAsync += _ => { ui.Post(__ => StatusChanged?.Invoke(), null); return Task.CompletedTask; };
        }

        public static string NormalizeCode(string code) => (code ?? "").Trim().ToUpperInvariant();

        /// <summary>產生一組好唸的配對碼，例如 DOBY-7K3M-Q9TX。</summary>
        public static string NewCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";   // 拿掉容易看錯的 0/O/1/I
            var sb = new StringBuilder("DOBY");
            for (int i = 0; i < 8; i++)
            {
                if (i % 4 == 0) sb.Append('-');
                sb.Append(chars[RandomNumberGenerator.GetInt32(chars.Length)]);
            }
            return sb.ToString();
        }

        void Handle(FriendMsg msg)
        {
            bool wasOnline = PeerOnline;
            if (msg.T == "bye") lastPeer = DateTime.MinValue;
            else
            {
                lastPeer = DateTime.UtcNow;
                PeerId = msg.From;
                if (!string.IsNullOrEmpty(msg.Name)) PeerName = msg.Name;
                if (!string.IsNullOrEmpty(msg.Coat)) PeerCoat = msg.Coat;
            }
            Received?.Invoke(msg);
            if (wasOnline != PeerOnline) StatusChanged?.Invoke();
        }

        /// <summary>背景連線迴圈：斷線就每 15 秒重試一次。</summary>
        public void Start()
        {
            _ = Task.Run(async () =>
            {
                var options = new MqttClientOptionsBuilder()
                    .WithTcpServer(Broker, Port)
                    .WithTlsOptions(o => o.UseTls())
                    .WithClientId("dobypet-" + myId[..8] + "-" + Guid.NewGuid().ToString("N")[..6])
                    .WithCleanSession()
                    .WithKeepAlivePeriod(TimeSpan.FromSeconds(30))
                    .Build();
                while (!cts.IsCancellationRequested)
                {
                    try
                    {
                        if (!client.IsConnected)
                        {
                            await client.ConnectAsync(options, cts.Token);
                            await client.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(topic).Build(), cts.Token);
                            ui.Post(_ => StatusChanged?.Invoke(), null);
                        }
                    }
                    catch (Exception) when (!cts.IsCancellationRequested) { }
                    try { await Task.Delay(15_000, cts.Token); } catch (TaskCanceledException) { }
                }
            });
        }

        public void Send(FriendMsg msg) =>
            _ = SendAsync(msg).ContinueWith(t => _ = t.Exception, TaskContinuationOptions.OnlyOnFaulted);

        public Task SendAsync(FriendMsg msg)
        {
            if (!client.IsConnected) return Task.CompletedTask;
            msg.From = myId;
            var payload = Encrypt(msg);
            return client.PublishAsync(new MqttApplicationMessageBuilder().WithTopic(topic).WithPayload(payload).Build(), cts.Token);
        }

        byte[] Encrypt(FriendMsg msg)
        {
            var plain = JsonSerializer.SerializeToUtf8Bytes(msg);
            var nonce = RandomNumberGenerator.GetBytes(12);
            var tag = new byte[16];
            var cipher = new byte[plain.Length];
            using (var aes = new AesGcm(key, 16)) aes.Encrypt(nonce, plain, cipher, tag);
            var outp = new byte[12 + 16 + cipher.Length];
            nonce.CopyTo(outp, 0);
            tag.CopyTo(outp, 12);
            cipher.CopyTo(outp, 28);
            return outp;
        }

        FriendMsg Decrypt(ArraySegment<byte> data)
        {
            try
            {
                if (data.Count < 29) return null;
                var span = data.AsSpan();
                var plain = new byte[data.Count - 28];
                using (var aes = new AesGcm(key, 16)) aes.Decrypt(span[..12], span[28..], span[12..28], plain);
                return JsonSerializer.Deserialize<FriendMsg>(plain);
            }
            catch { return null; }   // 金鑰不對（不同配對碼）或資料壞掉
        }

        public void Dispose()
        {
            cts.Cancel();
            try { client.DisconnectAsync().Wait(1500); } catch { }
            client.Dispose();
        }
    }
}
