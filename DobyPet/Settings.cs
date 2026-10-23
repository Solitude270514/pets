using System;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;

namespace DobyPet
{
    /// <summary>使用者設定，存在 %AppData%\DobyPet\settings.json。</summary>
    public sealed class Settings
    {
        public string Name { get; set; } = "小黑";
        public string Coat { get; set; } = "黑棕";
        /// <summary>一格像素在螢幕上佔幾個實體像素（2=小、3=中、4=大）。</summary>
        public int PixelScale { get; set; } = 2;
        public bool AutoStart { get; set; }
        public bool DoNotDisturb { get; set; }

        /// <summary>允許探頭、叼拖鞋這類搗蛋。</summary>
        public bool Mischief { get; set; } = true;
        /// <summary>允許咬住游標拖著跑（只在你沒動滑鼠、游標在牠附近時才會發生）。</summary>
        public bool StealCursor { get; set; } = true;

        /// <summary>跟朋友的配對碼，空白 = 不連線。</summary>
        public string FriendCode { get; set; } = "";
        /// <summary>這台電腦的識別碼，用來分辨訊息是誰送的。</summary>
        public string InstanceId { get; set; } = "";

        /// <summary>
        /// 設定檔分身（--profile B），讓同一台電腦能開兩隻狗測試互訪。
        /// 正式使用時是 null。
        /// </summary>
        public static string Profile;

        public static string Folder
        {
            get
            {
                var name = string.IsNullOrEmpty(Profile) ? "DobyPet" : "DobyPet_" + Profile;
                var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), name);
                Directory.CreateDirectory(dir);
                return dir;
            }
        }

        static string FilePath => Path.Combine(Folder, "settings.json");

        public static Settings Load()
        {
            Settings s = null;
            try
            {
                if (File.Exists(FilePath))
                    s = JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath));
            }
            catch { }
            s ??= new Settings();
            if (string.IsNullOrEmpty(s.InstanceId))   // 第一次執行（或舊版設定檔）：產生並存下識別碼
            {
                s.InstanceId = Guid.NewGuid().ToString("N");
                s.Save();
            }
            return s;
        }

        public void Save()
        {
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            }));
            if (string.IsNullOrEmpty(Profile)) ApplyAutoStart();
        }

        const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

        void ApplyAutoStart()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
            if (key == null) return;
            if (AutoStart) key.SetValue("DobyPet", $"\"{Environment.ProcessPath}\"");
            else if (key.GetValue("DobyPet") != null) key.DeleteValue("DobyPet");
        }
    }
}
