using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;

namespace DobyPet
{
    /// <summary>
    /// 從 GitHub Releases 檢查並安裝新版。
    /// 流程：查最新 release → 下載 DobyPet.exe 到旁邊的 .new → 核對 SHA-256 →
    /// 把執行中的 exe 改名成 .old（Windows 允許改名執行中的檔案）→ .new 換上 → 以 --updated 重新啟動。
    /// 新程序啟動後會把 .old 刪掉。
    /// </summary>
    public sealed class Updater
    {
        /// <summary>預設查詢的來源；設定 DOBYPET_UPDATE_URL 環境變數可改成別的網址（測試用）。</summary>
        const string DefaultApi = "https://api.github.com/repos/Solitude270514/pets/releases/latest";
        const string AssetName = "DobyPet.exe";

        // 注意宣告順序：靜態欄位依序初始化，http 會用到 Current，所以 Current 要放前面
        public static Version Current { get; } = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);
        public static string CurrentText => $"{Current.Major}.{Current.Minor}.{Current.Build}";

        static readonly HttpClient http = CreateClient();

        public sealed class Release
        {
            public Version Version;
            public string Tag;
            public string DownloadUrl;
            public long Size;
            public string Sha256;   // GitHub 提供的 asset digest，可能沒有
        }

        /// <summary>已經找到、還沒安裝的新版。</summary>
        public Release Available { get; private set; }

        static HttpClient CreateClient()
        {
            var c = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
            c.DefaultRequestHeaders.UserAgent.ParseAdd($"DobyPet/{CurrentText}");
            c.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            return c;
        }

        static string ApiUrl => Environment.GetEnvironmentVariable("DOBYPET_UPDATE_URL") is { Length: > 0 } u ? u : DefaultApi;

        /// <summary>只有打包成單一 exe 的正式版才更新自己（開發時用 dotnet run 不更新）。</summary>
        public static bool CanSelfUpdate =>
            Environment.ProcessPath is string p && Path.GetFileName(p).Equals(AssetName, StringComparison.OrdinalIgnoreCase)
            && !File.Exists(Path.ChangeExtension(p, ".dll"));

        /// <summary>查最新版。回傳 null 代表已是最新；查不到（離線、repo 私人）會丟例外。</summary>
        public async Task<Release> CheckAsync()
        {
            using var resp = await http.GetAsync(ApiUrl);
            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
                throw new InvalidOperationException("找不到 Releases（repo 可能還是私人的，或還沒發佈任何版本）");
            resp.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            var root = doc.RootElement;
            string tag = root.GetProperty("tag_name").GetString() ?? "";
            if (!Version.TryParse(tag.TrimStart('v', 'V'), out var ver)) return null;

            var asset = root.GetProperty("assets").EnumerateArray()
                .FirstOrDefault(a => a.GetProperty("name").GetString()?.Equals(AssetName, StringComparison.OrdinalIgnoreCase) == true);
            if (asset.ValueKind == JsonValueKind.Undefined) return null;

            var rel = new Release
            {
                Version = ver,
                Tag = tag,
                DownloadUrl = asset.GetProperty("browser_download_url").GetString(),
                Size = asset.GetProperty("size").GetInt64(),
                Sha256 = asset.TryGetProperty("digest", out var d) && d.GetString() is string dg && dg.StartsWith("sha256:")
                    ? dg["sha256:".Length..].ToLowerInvariant() : null,
            };
            var cur = new Version(Current.Major, Current.Minor, Math.Max(0, Current.Build));
            var neu = new Version(ver.Major, ver.Minor, Math.Max(0, ver.Build));
            Available = neu > cur ? rel : null;
            return Available;
        }

        /// <summary>下載並換上新版，成功後啟動新程序（呼叫端接著要結束自己）。</summary>
        public async Task InstallAsync(Release rel)
        {
            string exe = Environment.ProcessPath;
            string tmp = exe + ".new", old = exe + ".old";

            using (var resp = await http.GetAsync(rel.DownloadUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                resp.EnsureSuccessStatusCode();
                await using var fs = File.Create(tmp);
                await resp.Content.CopyToAsync(fs);
            }

            var info = new FileInfo(tmp);
            if (info.Length != rel.Size) { File.Delete(tmp); throw new InvalidDataException("下載的檔案大小不對"); }
            if (rel.Sha256 != null)
            {
                string hash;
                await using (var fs = File.OpenRead(tmp)) hash = Convert.ToHexString(await SHA256.HashDataAsync(fs)).ToLowerInvariant();
                if (hash != rel.Sha256) { File.Delete(tmp); throw new InvalidDataException("下載的檔案雜湊不符，已放棄更新"); }
            }

            if (File.Exists(old)) File.Delete(old);
            File.Move(exe, old);
            try { File.Move(tmp, exe); }
            catch { File.Move(old, exe); throw; }   // 換不上去就還原

            string args = "--updated" + (string.IsNullOrEmpty(Settings.Profile) ? "" : $" --profile {Settings.Profile}");
            Process.Start(new ProcessStartInfo(exe, args) { UseShellExecute = false });
        }

        /// <summary>更新後第一次啟動：清掉舊版檔案（舊程序可能還沒完全結束，重試幾次）。</summary>
        public static void CleanupOld()
        {
            if (Environment.ProcessPath is not string exe) return;
            string old = exe + ".old";
            _ = Task.Run(async () =>
            {
                for (int i = 0; i < 20 && File.Exists(old); i++)
                {
                    try { File.Delete(old); } catch { await Task.Delay(500); }
                }
            });
        }
    }
}
