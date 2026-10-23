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

        public static string Folder
        {
            get
            {
                var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DobyPet");
                Directory.CreateDirectory(dir);
                return dir;
            }
        }

        static string FilePath => Path.Combine(Folder, "settings.json");

        public static Settings Load()
        {
            try
            {
                if (File.Exists(FilePath))
                    return JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath)) ?? new Settings();
            }
            catch { }
            return new Settings();
        }

        public void Save()
        {
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            }));
            ApplyAutoStart();
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
