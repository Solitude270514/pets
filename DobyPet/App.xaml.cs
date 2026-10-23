using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;

namespace DobyPet
{
    public partial class App : Application
    {
        Mutex mutex;
        bool ownsMutex;
        Tray tray;

        protected override void OnStartup(StartupEventArgs e)
        {
            mutex = new Mutex(true, "DobyPet_SingleInstance", out ownsMutex);
            if (!ownsMutex) { Shutdown(); return; }   // 已經有一隻在跑了

            base.OnStartup(e);
            DispatcherUnhandledException += (_, ex) => { Log(ex.Exception); ex.Handled = true; };

            var cfg = Settings.Load();
            var bank = new SpriteBank { Coat = cfg.Coat };
            var win = new PetWindow(cfg, bank, e.Args.Contains("--demo"));
            win.Show();
            tray = new Tray(win, cfg, bank);
            if (e.Args.Contains("--call")) win.Dispatcher.BeginInvoke(win.CallOver);   // 測試用：啟動就進入選位置模式
        }

        protected override void OnExit(ExitEventArgs e)
        {
            tray?.Dispose();
            if (ownsMutex) mutex.ReleaseMutex();
            base.OnExit(e);
        }

        static void Log(Exception ex)
        {
            try { File.AppendAllText(Path.Combine(Settings.Folder, "error.log"), $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex}\n\n"); }
            catch { }
        }
    }
}
