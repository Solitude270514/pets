using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using WF = System.Windows.Forms;

namespace DobyPet
{
    /// <summary>
    /// 總管：主迴圈、自家的狗、工作列上的道具、全螢幕與閒置偵測、咬游標，
    /// 以及第三階段的朋友連線（出門拜訪、接待來作客的狗）。
    /// </summary>
    public sealed class PetHost
    {
        readonly Settings cfg;
        readonly SpriteBank bank;
        readonly bool demo;
        readonly Random rng = new();
        readonly Stopwatch clock = Stopwatch.StartNew();
        readonly DispatcherTimer timer;

        public readonly PetController Pet;
        readonly DogWindow win;

        readonly List<ItemWindow> poops = new();
        ItemWindow bowl, slipper;
        bool feedPending;
        CallOverlay overlay;

        double last, checkTimer, topmostTimer, ground, scale = 2;
        bool hiddenForFullscreen;
        System.Drawing.Point? lastSetCursor;

        // 朋友連線
        FriendLink link;
        double helloTimer;
        bool greeted;

        // 自家的狗出門拜訪
        enum VisitPhase { None, Sniffing, Waiting, Away }
        VisitPhase vphase;
        string visitId;
        double vTimer, awayFor, visitCooldown = 90;

        // 朋友的狗來作客
        PetController guestPet;
        DogWindow guestWin;
        VisitDirector director;
        string guestVisitId, guestName;

        public event Action ContextMenuRequested;
        public event Action<string> Notify;
        public event Action StatusChanged;

        public double Now => clock.Elapsed.TotalSeconds;

        public PetHost(Settings cfg, SpriteBank bank, bool demo)
        {
            this.cfg = cfg;
            this.bank = bank;
            this.demo = demo;

            Pet = new PetController(bank, demo) { DoNotDisturb = cfg.DoNotDisturb, Mischief = cfg.Mischief };
            Pet.PoopDropped += SpawnPoop;
            Pet.SlipperDropped += SpawnSlipper;
            Pet.CanPoop = () => poops.Count < 3;
            Pet.CanFetchSlipper = () => slipper == null;

            win = new DogWindow(Pet, cfg.Name, () => Now);
            win.ContextMenuRequested += () => ContextMenuRequested?.Invoke();
            win.SourceInitialized += (_, __) =>
            {
                ApplyScale();
                var wa = SystemParameters.WorkArea;
                Pet.X = wa.Left + wa.Width * .7;
            };
            win.Show();

            timer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(33) };
            timer.Tick += Tick;
            timer.Start();

            if (demo)   // 示範模式：開場就餵一次，順便測試吃飯與便便
            {
                var once = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
                once.Tick += (_, __) => { once.Stop(); Feed(); };
                once.Start();
            }

            RestartLink();
        }

        IEnumerable<PetController> AllPets()
        {
            yield return Pet;
            if (guestPet != null) yield return guestPet;
        }

        void Setup(PetController p)
        {
            var wa = SystemParameters.WorkArea;
            p.Scale = scale;
            p.MinX = wa.Left + 30 * scale;
            p.MaxX = wa.Right - 30 * scale;
            p.EdgeL = wa.Left;
            p.EdgeR = wa.Right;
        }

        /// <summary>依設定的像素倍率與螢幕 DPI 算出大小，讓每一格像素都是整數個實體像素，不會糊。</summary>
        public void ApplyScale()
        {
            scale = cfg.PixelScale / win.DpiScale;
            ground = SystemParameters.WorkArea.Bottom;
            foreach (var p in AllPets()) Setup(p);
            if (Pet.State is not (PetState.Offscreen or PetState.Peek or PetState.PeekHide or PetState.Walk))
                Pet.X = Math.Max(Pet.MinX, Math.Min(Pet.MaxX, Pet.X));
            win.ApplyScale(scale);
            win.Ground = ground;
            if (guestWin != null) { guestWin.ApplyScale(scale); guestWin.Ground = ground; }
            foreach (var p in poops) p.ApplyScale(scale, ground);
            bowl?.ApplyScale(scale, ground);
            slipper?.ApplyScale(scale, ground);
        }

        public void ApplySettings()
        {
            Pet.Mischief = cfg.Mischief;
            win.SetName(cfg.Name);
            ApplyScale();
        }

        // ── 主迴圈 ─────────────────────────────────

        void Tick(object sender, EventArgs e)
        {
            double now = Now, dt = Math.Min(.1, now - last), tms = now * 1000;
            last = now;

            checkTimer -= dt;
            if (checkTimer <= 0) { checkTimer = 1; PeriodicChecks(); }
            if (hiddenForFullscreen) return;

            topmostTimer -= dt;
            if (topmostTimer <= 0)
            {
                topmostTimer = 5;
                if (win.Hwnd != IntPtr.Zero) Native.KeepTopmost(win.Hwnd);
                if (guestWin?.Hwnd is IntPtr g && g != IntPtr.Zero) Native.KeepTopmost(g);
            }

            if (feedPending && bowl != null && Pet.GoEat(bowl.X, RemoveBowl)) feedPending = false;

            Pet.Update(dt);
            guestPet?.Update(dt);
            director?.Tick();

            win.Render(tms);
            guestWin?.Render(tms);
            foreach (var p in poops.ToArray()) p.Tick(tms, dt);
            bowl?.Tick(tms, dt);
            slipper?.Tick(tms, dt);

            UpdateCursorSteal();
            UpdateVisitTimers(dt);
        }

        /// <summary>每秒檢查一次：全螢幕、主人在不在、要不要搗蛋或出門。</summary>
        void PeriodicChecks()
        {
            bool fullscreen = Native.ForegroundIsFullscreen();
            if (fullscreen != hiddenForFullscreen)
            {
                hiddenForFullscreen = fullscreen;
                var v = fullscreen ? Visibility.Hidden : Visibility.Visible;
                win.Visibility = v;
                if (guestWin != null) guestWin.Visibility = v;
                foreach (var p in poops) p.Visibility = v;
                if (bowl != null) bowl.Visibility = v;
                if (slipper != null) slipper.Visibility = v;
                if (fullscreen) Pet.ForceSleep(); else Pet.WakeUp();
            }

            helloTimer -= 1;
            if (helloTimer <= 0) { helloTimer = 20; SendHello(); }
            CheckVisitFailsafes();

            if (hiddenForFullscreen || win.Dragging || demo) return;

            double idle = Native.IdleSeconds();
            Pet.SetOwnerAway(idle > 180, win.CursorDip().X);

            // 咬游標：你停下滑鼠 2.5 秒以上、游標在牠附近的工作列上方，偶爾會發生
            if (cfg.StealCursor && idle > 2.5 && idle < 60 && !Pet.OwnerAway && rng.NextDouble() < .03)
            {
                var c = win.CursorDip();
                if (c.Y > ground - 90 * scale && c.Y < ground && Math.Abs(c.X - Pet.X) < 300)
                    Pet.TryStealCursor(c.X);
            }

            // 朋友在線上時，偶爾自己跑去找朋友玩（平均約 10 分鐘一次）
            visitCooldown -= 1;
            if (visitCooldown <= 0 && CanVisit && !Pet.OwnerAway && rng.NextDouble() < 1 / 600.0) StartVisit();
        }

        /// <summary>咬住游標時，把游標移到嘴巴的位置；主人一動滑鼠就鬆口。</summary>
        void UpdateCursorSteal()
        {
            if (!Pet.HoldingCursor) { lastSetCursor = null; return; }
            var cur = WF.Cursor.Position;
            if (lastSetCursor is { } p && (Math.Abs(cur.X - p.X) > 4 || Math.Abs(cur.Y - p.Y) > 4))
            {
                Pet.ReleaseCursor();
                lastSetCursor = null;
                return;
            }
            double dpi = win.DpiScale;
            int x = (int)Math.Round(Pet.MouthX * dpi), y = (int)Math.Round(Pet.MouthY(ground) * dpi);
            Native.SetCursorPos(x, y);
            lastSetCursor = new System.Drawing.Point(x, y);
        }

        // ── 餵食、便便、拖鞋 ─────────────────────────

        public void Feed()
        {
            if (bowl != null) return;
            double off = (120 + rng.NextDouble() * 160) * (rng.Next(2) == 0 ? -1 : 1);
            double x = Pet.X + off;
            if (x < Pet.MinX + 40 || x > Pet.MaxX - 40) x = Pet.X - off;
            bowl = new ItemWindow(ItemKind.Bowl, bank, x, scale, ground);
            bowl.Show();
            feedPending = true;
        }

        void RemoveBowl()
        {
            bowl?.Close();
            bowl = null;
        }

        void SpawnPoop(double x)
        {
            var p = new ItemWindow(ItemKind.Poop, bank, x, scale, ground);
            p.Cleaned += item =>
            {
                poops.Remove(item);
                if (Math.Abs(item.X - Pet.X) < 200) Pet.Cheer();
            };
            poops.Add(p);
            p.Show();
        }

        void SpawnSlipper(double x)
        {
            if (slipper != null) return;
            slipper = new ItemWindow(ItemKind.Slipper, bank, x, scale, ground);
            slipper.Cleaned += _ => slipper = null;
            slipper.Show();
        }

        /// <summary>進入選位置模式，點哪裡就跑去哪裡。</summary>
        public void CallOver()
        {
            if (overlay != null) return;
            overlay = new CallOverlay(cfg.Name, ground, x => Pet.ComeTo(x));
            overlay.Closed += (_, __) => overlay = null;
            overlay.Show();
        }

        // ── 朋友連線 ────────────────────────────────

        /// <summary>配對碼改變後重新連線。</summary>
        public void RestartLink()
        {
            if (link != null)
            {
                SendNow(new FriendMsg { T = "bye" });
                link.Dispose();
                link = null;
            }
            greeted = false;
            if (!string.IsNullOrWhiteSpace(cfg.FriendCode))
            {
                link = new FriendLink(cfg.FriendCode, cfg.InstanceId);
                link.Received += OnFriendMsg;
                link.StatusChanged += () =>
                {
                    if (link?.Connected == true && !greeted) { greeted = true; SendHello(); }
                    StatusChanged?.Invoke();
                };
                link.Start();
            }
            StatusChanged?.Invoke();
        }

        void SendHello(bool reply = false)
        {
            link?.Send(new FriendMsg { T = "hello", Name = cfg.Name, Coat = cfg.Coat, Id = reply ? "reply" : null });
        }

        void SendNow(FriendMsg m)
        {
            try { link?.SendAsync(m).Wait(1000); } catch { }
        }

        public bool PeerOnline => link?.PeerOnline == true;

        public string FriendStatus
        {
            get
            {
                if (link == null) return "尚未配對（到設定輸入配對碼）";
                if (!link.Connected) return "連線中…";
                if (!link.PeerOnline) return "朋友目前不在線上";
                if (vphase == VisitPhase.Away) return $"{cfg.Name} 正在 {link.PeerName} 家玩";
                if (director != null) return $"{guestName} 正在家裡玩";
                return $"{link.PeerName} 在線上";
            }
        }

        void OnFriendMsg(FriendMsg m)
        {
            switch (m.T)
            {
                case "hello":
                    if (m.Id != "reply") SendHello(reply: true);   // 對方剛上線，回個招呼讓牠馬上知道我在
                    break;

                case "bye":
                    if (vphase == VisitPhase.Away) ComeHome();
                    director?.Abort();
                    break;

                case "visit-req":
                    bool ok = guestPet == null && vphase == VisitPhase.None && !hiddenForFullscreen
                              && !cfg.DoNotDisturb && !Pet.IsOffscreen;
                    link.Send(new FriendMsg { T = ok ? "visit-ok" : "visit-no", Id = m.Id });
                    if (ok) SpawnGuest(m);
                    break;

                case "visit-ok" when vphase == VisitPhase.Waiting && m.Id == visitId:
                    vphase = VisitPhase.Away;
                    awayFor = 0;
                    Pet.WalkOut(right: true);
                    Notify?.Invoke($"{cfg.Name} 跑去 {link.PeerName} 家玩了！");
                    break;

                case "visit-no" when vphase == VisitPhase.Waiting && m.Id == visitId:
                    VisitFailed();
                    break;

                case "visit-end" when vphase == VisitPhase.Away && m.Id == visitId:
                    ComeHome();
                    break;
            }
            StatusChanged?.Invoke();
        }

        // ── 自家的狗出門 ─────────────────────────────

        public bool CanVisit => PeerOnline && vphase == VisitPhase.None && director == null && !cfg.DoNotDisturb;

        /// <summary>走到螢幕右邊嗅一嗅，問朋友方不方便，方便就走出去。</summary>
        public void StartVisit()
        {
            if (!CanVisit || Pet.Busy) return;
            vphase = VisitPhase.Sniffing;
            vTimer = 60;
            Pet.WalkToEdge(right: true, () =>
            {
                visitId = Guid.NewGuid().ToString("N")[..10];
                link.Send(new FriendMsg { T = "visit-req", Id = visitId, Name = cfg.Name, Coat = cfg.Coat });
                vphase = VisitPhase.Waiting;
                vTimer = 6;
            });
        }

        void UpdateVisitTimers(double dt)
        {
            if (vphase is not (VisitPhase.Sniffing or VisitPhase.Waiting)) return;
            vTimer -= dt;
            if (vTimer <= 0) VisitFailed();
        }

        void VisitFailed()
        {
            bool waited = vphase == VisitPhase.Waiting;
            vphase = VisitPhase.None;
            visitCooldown = 300;
            if (waited) Pet.Disappointed();
        }

        void ComeHome()
        {
            vphase = VisitPhase.None;
            visitCooldown = 600;
            if (Pet.IsOffscreen) Pet.ComeIn(fromRight: true);
            Notify?.Invoke($"{cfg.Name} 玩夠了，回家了！");
            StatusChanged?.Invoke();
        }

        /// <summary>斷線保險：朋友離線或玩太久，就讓狗回家 / 讓客人離開。</summary>
        void CheckVisitFailsafes()
        {
            if (vphase == VisitPhase.Away)
            {
                awayFor += 1;
                if (awayFor > 420 || (!PeerOnline && awayFor > 20)) ComeHome();
            }
            if (director != null && !PeerOnline) director.Abort();
        }

        // ── 朋友的狗來作客 ───────────────────────────

        void SpawnGuest(FriendMsg m)
        {
            guestName = string.IsNullOrEmpty(m.Name) ? "朋友的狗" : m.Name;
            guestVisitId = m.Id;
            var gbank = new SpriteBank { Coat = m.Coat ?? "黑棕" };
            guestPet = new PetController(gbank) { Scripted = true, Mischief = false };
            Setup(guestPet);
            guestPet.X = guestPet.HiddenX(false);
            guestPet.PoopDropped += SpawnPoop;
            guestPet.CanPoop = () => poops.Count < 3;

            guestWin = new DogWindow(guestPet, $"{guestName}（來作客）", () => Now) { Ground = ground };
            guestWin.ContextMenuRequested += () => ContextMenuRequested?.Invoke();
            guestWin.SourceInitialized += (_, __) => guestWin.ApplyScale(scale);
            guestWin.Show();

            director = new VisitDirector(Pet, guestPet, () => Now);
            director.Done += EndHosting;
            Notify?.Invoke($"{guestName} 來你家玩了！");
        }

        void EndHosting()
        {
            link?.Send(new FriendMsg { T = "visit-end", Id = guestVisitId });
            guestWin?.Close();
            guestWin = null;
            guestPet = null;
            director = null;
            StatusChanged?.Invoke();
        }

        // ── 關閉 ────────────────────────────────────

        public void Shutdown()
        {
            timer.Stop();
            if (director != null) SendNow(new FriendMsg { T = "visit-end", Id = guestVisitId });
            SendNow(new FriendMsg { T = "bye" });
            link?.Dispose();
            foreach (var p in poops.ToArray()) p.Close();
            bowl?.Close();
            slipper?.Close();
            guestWin?.Close();
            win.Close();
        }
    }
}
