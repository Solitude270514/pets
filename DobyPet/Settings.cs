using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Win32;

namespace DobyPet
{
    /// <summary>一隻狗的設定。</summary>
    public sealed class PetConfig
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
        public string Name { get; set; } = "小黑";
        public string Breed { get; set; } = Breeds.Default;
        public string Coat { get; set; }
    }

    /// <summary>使用者設定，存在 %AppData%\DobyPet\settings.json。</summary>
    public sealed class Settings
    {
        public const int MaxPets = 4;

        /// <summary>養的狗（至少一隻，最多 MaxPets 隻）。</summary>
        public List<PetConfig> Pets { get; set; } = new();

        /// <summary>v0.3 以前只有一隻狗時的欄位，讀到就搬進 Pets，存檔時不再寫出。</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string Name { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string Coat { get; set; }

        /// <summary>一格像素在螢幕上佔幾個實體像素（2=小、3=中、4=大）。</summary>
        public int PixelScale { get; set; } = 2;
        public bool AutoStart { get; set; }
        public bool DoNotDisturb { get; set; }

        /// <summary>允許探頭、叼拖鞋這類搗蛋。</summary>
        public bool Mischief { get; set; } = true;
        /// <summary>允許咬住游標拖著跑（只在你沒動滑鼠、游標在牠附近時才會發生）。</summary>
        public bool StealCursor { get; set; } = true;

        /// <summary>有新版時自動安裝（只在你離開電腦時進行），否則只跳通知。</summary>
        public bool AutoUpdate { get; set; }

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
            bool dirty = false;
            if (string.IsNullOrEmpty(s.InstanceId))   // 第一次執行（或舊版設定檔）：產生並存下識別碼
            {
                s.InstanceId = Guid.NewGuid().ToString("N");
                dirty = true;
            }
            if (s.Pets == null || s.Pets.Count == 0)   // 舊版只有一隻狗：把名字、毛色搬過來
            {
                s.Pets = new() { new PetConfig { Name = s.Name ?? "小黑", Breed = Breeds.Default, Coat = s.Coat } };
                dirty = true;
            }
            if (s.Name != null || s.Coat != null) { s.Name = null; s.Coat = null; dirty = true; }
            foreach (var p in s.Pets)   // 品種或毛色不存在（例如改過版本）就換回預設
            {
                var b = Breeds.Get(p.Breed);
                if (p.Breed != b.Id) { p.Breed = b.Id; dirty = true; }
                if (p.Coat == null || !b.Coats.ContainsKey(p.Coat)) { p.Coat = b.DefaultCoat; dirty = true; }
            }
            if (s.Pets.Count > MaxPets) { s.Pets = s.Pets.Take(MaxPets).ToList(); dirty = true; }
            if (dirty) s.Save();
            else if (s.AutoStart && string.IsNullOrEmpty(Profile)) s.ApplyAutoStart();   // exe 搬過位置也能跟著更新開機啟動路徑
            return s;
        }

        /// <summary>選單標題用：「小黑、小白」。</summary>
        [JsonIgnore] public string PetNames => string.Join("、", Pets.Select(p => p.Name));

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
