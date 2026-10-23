using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;

namespace DobyPet
{
    /// <summary>
    /// 啟動參數：
    ///   --demo         示範模式：依序展示每個動作，開場自動餵一次
    ///   --call         啟動就進入「叫牠過來」選位置模式（測試用）
    ///   --profile X    使用獨立的設定檔分身，可以在同一台電腦開第二隻狗測試互訪
    ///   --visit        啟動 5 秒後自動出門拜訪（測試用）
    ///   --present      一律當作主人在電腦前，不做閒置偵測（測試用）
    ///   --trick X      每 15 秒表演一次指定動作：peek / slipper / fart / backflip（測試用）
    ///   --update-now   啟動就檢查更新，有新版立刻安裝（測試用）
    ///   --updated      由自動更新重新啟動時帶入：等舊程序結束、清掉舊檔、跳出「已更新」通知
    /// </summary>
    public partial class App : Application
    {
        Mutex mutex;
        bool ownsMutex;
        Tray tray;

        protected override void OnStartup(StartupEventArgs e)
        {
            int pi = Array.IndexOf(e.Args, "--profile");
            if (pi >= 0 && pi + 1 < e.Args.Length) Settings.Profile = e.Args[pi + 1];
            bool updated = e.Args.Contains("--updated");

            mutex = new Mutex(true, "DobyPet_SingleInstance_" + (Settings.Profile ?? ""), out ownsMutex);
            if (!ownsMutex && updated)
            {
                // 剛更新完：舊版程序正在關閉，等它放開
                try { ownsMutex = mutex.WaitOne(TimeSpan.FromSeconds(20)); }
                catch (AbandonedMutexException) { ownsMutex = true; }
            }
            if (!ownsMutex) { Shutdown(); return; }   // 已經有一隻在跑了
            if (updated) Updater.CleanupOld();

            base.OnStartup(e);
            DispatcherUnhandledException += (_, ex) => { Log(ex.Exception); ex.Handled = true; };

            var cfg = Settings.Load();
            var bank = new SpriteBank { Coat = cfg.Coat };
            var host = new PetHost(cfg, bank, e.Args.Contains("--demo"), e.Args.Contains("--present"));
            tray = new Tray(host, cfg, bank, justUpdated: updated, updateNow: e.Args.Contains("--update-now"));

            if (e.Args.Contains("--call")) Dispatcher.BeginInvoke(host.CallOver);
            int ti = Array.IndexOf(e.Args, "--trick");
            if (ti >= 0 && ti + 1 < e.Args.Length)
            {
                Action trick = e.Args[ti + 1] switch
                {
                    "peek" => host.Pet.DoPeek,
                    "slipper" => host.Pet.DoSlipper,
                    "fart" => host.Pet.DoFart,
                    _ => host.Pet.DoBackflip,
                };
                var t = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
                t.Tick += (_, __) => trick();
                t.Start();
                Dispatcher.BeginInvoke(trick);
            }
            if (e.Args.Contains("--visit"))
            {
                var t = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
                t.Tick += (_, __) => { if (host.CanVisit) { t.Stop(); host.StartVisit(); } };
                t.Start();
            }
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
