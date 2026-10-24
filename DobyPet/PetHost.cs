using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using WF = System.Windows.Forms;

namespace DobyPet
{
    /// <summary>家裡的一隻狗：設定、圖庫、大腦、視窗，以及牠自己的狗碗。</summary>
    public sealed class Dog
    {
        public PetConfig Config;
        public SpriteBank Bank;
        public PetController Pet;
        public DogWindow Win;
        public ItemWindow Bowl;
        public bool FeedPending;
        public string Name => Config.Name;
    }

    /// <summary>
    /// 總管：主迴圈、家裡的每一隻狗、工作列上的道具、全螢幕與閒置偵測、咬游標、
    /// 家裡狗狗互相玩，以及跟朋友的連線（出門拜訪、接待來作客的狗）。
    /// </summary>
    public sealed class PetHost
    {
        readonly Settings cfg;
        readonly bool demo, alwaysPresent;
        readonly Random rng = new();
        readonly Stopwatch clock = Stopwatch.StartNew();
        readonly DispatcherTimer timer;

        public readonly List<Dog> Dogs = new();

        readonly List<ItemWindow> poops = new();
        ItemWindow slipper;
        CallOverlay overlay;

        double last, checkTimer, topmostTimer, ground, dpi = 1, guestScale = 2;
        bool hiddenForFullscreen, ownerAway;
        System.Drawing.Point? lastSetCursor;
        System.Drawing.Point lastCursor;
        double cursorMovedAt = -99;

        // 家裡兩隻以上時，偶爾挑兩隻一起玩
        VisitDirector homePlay;
        double homePlayCooldown = 90;

        // 朋友連線
        FriendLink link;
        double helloTimer;
        bool greeted, peerWasOnline, quickVisit, awaySent;

        // 自家的狗出門拜訪
        enum VisitPhase { None, Sniffing, Waiting, Away }
        VisitPhase vphase;
        Dog visitor;
        string visitId;
        double vTimer, awayFor, visitCooldown = 90;

        // 朋友的狗來作客
        PetController guestPet;
        DogWindow guestWin;
        VisitDirector director;
        string guestVisitId, guestName;

        /// <summary>右鍵點了哪隻狗（null = 從系統匣開選單，對全部的狗）。</summary>
        public event Action<Dog> ContextMenuRequested;
        public event Action<string> Notify;
        public event Action StatusChanged;

        public double Now => clock.Elapsed.TotalSeconds;

        public PetHost(Settings cfg, bool demo, bool alwaysPresent = false)
        {
            this.cfg = cfg;
            this.demo = demo;
            this.alwaysPresent = alwaysPresent;

            foreach (var pc in cfg.Pets) AddDog(pc);
            ArrangeDogs();

            timer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(33) };
            timer.Tick += Tick;
            timer.Start();

            if (demo)   // 示範模式：開場就餵一次，順便測試吃飯與便便
            {
                var once = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
                once.Tick += (_, __) => { once.Stop(); Feed(null); };
                once.Start();
            }

            RestartLink();
        }

        // ── 建立、同步狗狗 ──────────────────────────

        Dog AddDog(PetConfig pc)
        {
            var dog = new Dog { Config = pc, Bank = new SpriteBank(Breeds.Get(pc.Breed), pc.Coat) };
            dog.Pet = new PetController(dog.Bank, demo) { DoNotDisturb = cfg.DoNotDisturb, Mischief = cfg.Mischief };
            dog.Pet.PoopDropped += x => SpawnPoop(x, ScaleOf(dog));   // 便便、拖鞋跟著這隻狗的大小
            dog.Pet.SlipperDropped += x => SpawnSlipper(x, ScaleOf(dog));
            dog.Pet.CanPoop = () => poops.Count < 3 + Dogs.Count;
            dog.Pet.CanFetchSlipper = () => slipper == null;
            dog.Win = new DogWindow(dog.Pet, pc.Name, () => Now);
            dog.Win.ContextMenuRequested += () => ContextMenuRequested?.Invoke(dog);
            dog.Win.SourceInitialized += (_, __) => ApplyScale();
            Dogs.Add(dog);
            dog.Win.Show();   // 會觸發 SourceInitialized → ApplyScale，設定好大小與活動範圍
            return dog;
        }

        void RemoveDog(Dog dog)
        {
            if (homePlay?.Involves(dog.Pet) == true) { homePlay.Abort(); homePlay = null; }
            if (director?.Involves(dog.Pet) == true) director.Abort();
            if (visitor == dog) { vphase = VisitPhase.None; visitor = null; }
            dog.Bowl?.Close();
            dog.Win.Close();
            Dogs.Remove(dog);
        }

        /// <summary>把狗狗平均排在工作列上。</summary>
        void ArrangeDogs()
        {
            var wa = SystemParameters.WorkArea;
            for (int i = 0; i < Dogs.Count; i++)
                Dogs[i].Pet.X = wa.Left + wa.Width * (Dogs.Count == 1 ? .7 : .25 + .6 * i / (Dogs.Count - 1));
        }

        /// <summary>設定改過之後：新增、移除、換品種或毛色、改名字。</summary>
        public void ApplySettings()
        {
            foreach (var dog in Dogs.ToArray())
                if (!cfg.Pets.Any(p => p.Id == dog.Config.Id)) RemoveDog(dog);

            foreach (var pc in cfg.Pets)
            {
                var dog = Dogs.FirstOrDefault(d => d.Config.Id == pc.Id);
                if (dog == null) { AddDog(pc).Pet.ComeIn(fromRight: false); continue; }   // 新來的狗從左邊走進來
                if (dog.Bank.Breed.Id != pc.Breed && dog.Pet.Busy) continue;   // 正在忙（例如出門中），下次再換
                if (dog.Bank.Breed.Id != pc.Breed)
                {
                    double x = dog.Pet.X;
                    int index = Dogs.IndexOf(dog);
                    RemoveDog(dog);
                    var nd = AddDog(pc);
                    Dogs.Remove(nd);
                    Dogs.Insert(index, nd);
                    nd.Pet.X = x;
                    continue;
                }
                dog.Config = pc;
                dog.Bank.Coat = pc.Coat;
                dog.Win.SetName(pc.Name);
            }
            foreach (var d in Dogs) d.Pet.Mischief = cfg.Mischief;
            ApplyScale();
            StatusChanged?.Invoke();
        }

        IEnumerable<PetController> AllPets()
        {
            foreach (var d in Dogs) yield return d.Pet;
            if (guestPet != null) yield return guestPet;
        }

        IEnumerable<DogWindow> AllWindows()
        {
            foreach (var d in Dogs) yield return d.Win;
            if (guestWin != null) yield return guestWin;
        }

        /// <summary>這隻狗的像素倍率換算成 DIP（每隻狗可以設定不同大小）。</summary>
        double ScaleOf(Dog d) => (d.Config.Size is >= 2 and <= 4 ? d.Config.Size : cfg.PixelScale) / dpi;

        void Setup(PetController p, double scale)
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
            if (Dogs.Count == 0) return;
            dpi = Dogs[0].Win.DpiScale;
            ground = SystemParameters.WorkArea.Bottom;
            foreach (var d in Dogs)
            {
                double s = ScaleOf(d);
                Setup(d.Pet, s);
                if (d.Pet.State is not (PetState.Offscreen or PetState.Peek or PetState.PeekHide or PetState.Walk))
                    d.Pet.X = Math.Max(d.Pet.MinX, Math.Min(d.Pet.MaxX, d.Pet.X));
                d.Win.ApplyScale(s);
                d.Win.Ground = ground;
                d.Bowl?.ApplyScale(s, ground);
            }
            if (guestPet != null) { Setup(guestPet, guestScale); guestWin.ApplyScale(guestScale); guestWin.Ground = ground; }
            foreach (var p in poops) p.ApplyScale(p.Scale, ground);
            slipper?.ApplyScale(slipper.Scale, ground);
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
                foreach (var w in AllWindows()) if (w.Hwnd != IntPtr.Zero) Native.KeepTopmost(w.Hwnd);
            }

            foreach (var d in Dogs)
                if (d.FeedPending && d.Bowl != null && d.Pet.GoEat(d.Bowl.X, () => RemoveBowl(d))) d.FeedPending = false;

            foreach (var p in AllPets()) p.Update(dt);
            director?.Tick();
            homePlay?.Tick();
            if (homePlay?.Finished == true) homePlay = null;

            foreach (var w in AllWindows()) w.Render(tms);
            foreach (var p in poops.ToArray()) p.Tick(tms, dt);
            foreach (var d in Dogs) d.Bowl?.Tick(tms, dt);
            slipper?.Tick(tms, dt);

            UpdateCursorSteal();
            WatchCursor();
            UpdateVisitTimers(dt);
        }

        /// <summary>你在狗狗附近動滑鼠時，發呆或坐著的狗會轉頭看游標。</summary>
        void WatchCursor()
        {
            var cur = WF.Cursor.Position;
            if (cur != lastCursor) { lastCursor = cur; cursorMovedAt = Now; }
            if (Now - cursorMovedAt > 1.5 || hiddenForFullscreen) return;
            var c = AnyWindow.CursorDip();
            if (c.Y < ground - 250 || c.Y > ground + 60) return;
            foreach (var d in Dogs)
                if (Math.Abs(c.X - d.Pet.X) < 400) d.Pet.WatchCursor(c.X);
        }

        DogWindow AnyWindow => Dogs[0].Win;

        /// <summary>每秒檢查一次：全螢幕、主人在不在、要不要搗蛋、互相玩或出門。</summary>
        void PeriodicChecks()
        {
            bool fullscreen = Native.ForegroundIsFullscreen();
            if (fullscreen != hiddenForFullscreen)
            {
                hiddenForFullscreen = fullscreen;
                var v = fullscreen ? Visibility.Hidden : Visibility.Visible;
                foreach (var w in AllWindows()) w.Visibility = v;
                foreach (var p in poops) p.Visibility = v;
                foreach (var d in Dogs) if (d.Bowl != null) d.Bowl.Visibility = v;
                if (slipper != null) slipper.Visibility = v;
                foreach (var d in Dogs) { if (fullscreen) d.Pet.ForceSleep(); else d.Pet.WakeUp(); }
            }

            helloTimer -= 1;
            if (helloTimer <= 0) { helloTimer = 20; SendHello(); }
            CheckVisitFailsafes();
            UpdateFriendPresence();

            if (hiddenForFullscreen || AllWindows().Any(w => w.Dragging)) return;

            if (!demo)
            {
                double idle = Native.IdleSeconds();
                ownerAway = !alwaysPresent && idle > 180;
                var cursor = AnyWindow.CursorDip();
                foreach (var d in Dogs) d.Pet.SetOwnerAway(ownerAway, cursor.X);

                // 你停下滑鼠 2.5 秒以上、游標在某隻狗附近的工作列上方：偶爾會走過去聞一聞，或咬走游標
                if (idle > 2.5 && idle < 60 && !ownerAway && rng.NextDouble() < .03 && cursor.Y < ground)
                {
                    var near = Dogs.Where(d => Math.Abs(cursor.X - d.Pet.X) < 300).OrderBy(d => Math.Abs(cursor.X - d.Pet.X)).FirstOrDefault();
                    if (near != null && cursor.Y > ground - 90 * ScaleOf(near))
                    {
                        if (cfg.StealCursor && rng.NextDouble() < .6) near.Pet.TryStealCursor(cursor.X);
                        else near.Pet.TrySniffCursor(cursor.X);
                    }
                }
            }

            // 主人離開 / 回來時馬上告訴朋友，對方才知道這邊有沒有人在看
            if (ownerAway != awaySent) { awaySent = ownerAway; SendHello(); }

            // 家裡兩隻以上：每隔幾分鐘挑兩隻一起玩
            homePlayCooldown -= 1;
            if (homePlayCooldown <= 0 && homePlay == null && !ownerAway && !cfg.DoNotDisturb)
            {
                var free = Dogs.Where(d => FreeForPlay(d.Pet)).OrderBy(_ => rng.Next()).Take(2).ToList();
                if (free.Count == 2)
                {
                    homePlay = new VisitDirector(free[0].Pet, free[1].Pet, () => Now, fromOutside: false);
                    homePlayCooldown = 120 + rng.NextDouble() * 240;
                }
                else homePlayCooldown = 30;
            }

            // 對方主人在電腦前時才去拜訪（有人看才好玩）：剛上線那次 1～2 分鐘內出門，之後平均約 10 分鐘一次
            visitCooldown -= 1;
            bool friendWatching = PeerOnline && !link.PeerAway;
            if (visitCooldown <= 0 && friendWatching && CanVisit && (quickVisit || rng.NextDouble() < 1 / 600.0))
            {
                quickVisit = false;
                StartVisit(null);
            }
        }

        bool FreeForPlay(PetController p) =>
            !p.Busy && !p.Scripted && p.State is not (PetState.Sleep or PetState.Eat) && director?.Involves(p) != true;

        /// <summary>朋友剛上線：跳通知，並安排很快去拜訪一次。</summary>
        void UpdateFriendPresence()
        {
            bool online = PeerOnline;
            if (online && !peerWasOnline)
            {
                Notify?.Invoke($"{link.PeerName ?? "朋友"} 上線了！");
                // 兩邊會同時發現對方上線，由識別碼比較小的那邊先出門，免得兩隻狗同時出門撞在一起
                if (string.CompareOrdinal(cfg.InstanceId, link.PeerId) < 0)
                {
                    quickVisit = true;
                    visitCooldown = 45 + rng.NextDouble() * 45;
                }
                StatusChanged?.Invoke();
            }
            peerWasOnline = online;
        }

        /// <summary>咬住游標時，把游標移到嘴巴的位置；主人一動滑鼠就鬆口。</summary>
        void UpdateCursorSteal()
        {
            var thief = Dogs.FirstOrDefault(d => d.Pet.HoldingCursor);
            if (thief == null) { lastSetCursor = null; return; }
            var cur = WF.Cursor.Position;
            if (lastSetCursor is { } p && (Math.Abs(cur.X - p.X) > 4 || Math.Abs(cur.Y - p.Y) > 4))
            {
                thief.Pet.ReleaseCursor();
                lastSetCursor = null;
                return;
            }
            double dpi = AnyWindow.DpiScale;
            int x = (int)Math.Round(thief.Pet.MouthX * dpi), y = (int)Math.Round(thief.Pet.MouthY(ground) * dpi);
            Native.SetCursorPos(x, y);
            lastSetCursor = new System.Drawing.Point(x, y);
        }

        // ── 對選單目標下指令 ────────────────────────

        /// <summary>對某隻狗（或全部，target = null）下指令。</summary>
        public void Each(Dog target, Action<PetController> act)
        {
            foreach (var d in target != null ? new[] { target } : Dogs.ToArray()) act(d.Pet);
        }

        public void SetDoNotDisturb(bool on)
        {
            foreach (var d in Dogs) d.Pet.DoNotDisturb = on;
        }

        // ── 餵食、便便、拖鞋 ─────────────────────────

        /// <summary>在狗旁邊放一個狗碗（target = null 時每隻狗都有一碗）。</summary>
        public void Feed(Dog target)
        {
            foreach (var d in target != null ? new[] { target } : Dogs.ToArray())
            {
                if (d.Bowl != null || d.Pet.IsOffscreen) continue;
                double off = (120 + rng.NextDouble() * 160) * (rng.Next(2) == 0 ? -1 : 1);
                double x = d.Pet.X + off;
                if (x < d.Pet.MinX + 40 || x > d.Pet.MaxX - 40) x = d.Pet.X - off;
                d.Bowl = new ItemWindow(ItemKind.Bowl, x, ScaleOf(d), ground);
                d.Bowl.Show();
                d.FeedPending = true;
            }
        }

        void RemoveBowl(Dog d)
        {
            d.Bowl?.Close();
            d.Bowl = null;
        }

        void SpawnPoop(double x, double scale)
        {
            var p = new ItemWindow(ItemKind.Poop, x, scale, ground);
            p.Cleaned += item =>
            {
                poops.Remove(item);
                foreach (var d in Dogs) if (Math.Abs(item.X - d.Pet.X) < 200) d.Pet.Cheer();
            };
            poops.Add(p);
            p.Show();
        }

        void SpawnSlipper(double x, double scale)
        {
            if (slipper != null) return;
            slipper = new ItemWindow(ItemKind.Slipper, x, scale, ground);
            slipper.Cleaned += _ => slipper = null;
            slipper.Show();
        }

        /// <summary>進入選位置模式，點哪裡就跑去哪裡（target = null 時全部一起跑過去）。</summary>
        public void CallOver(Dog target)
        {
            if (overlay != null) return;
            string who = target?.Name ?? (Dogs.Count == 1 ? Dogs[0].Name : "大家");
            overlay = new CallOverlay(who, ground, x =>
            {
                var list = target != null ? new[] { target } : Dogs.ToArray();
                for (int i = 0; i < list.Length; i++)
                    list[i].Pet.ComeTo(x + (i - (list.Length - 1) / 2.0) * 45 * ScaleOf(list[i]));   // 好幾隻時並排站
            });
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
            peerWasOnline = false;   // 換了配對碼，新朋友上線時要重新通知、重新安排第一次拜訪
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
            link?.Send(new FriendMsg { T = "hello", Name = cfg.PetNames, Id = reply ? "reply" : null, Away = ownerAway });
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
                if (vphase == VisitPhase.Away && visitor != null) return $"{visitor.Name} 正在 {link.PeerName} 家玩";
                if (director != null) return $"{guestName} 正在家裡玩";
                if (link.PeerAway) return $"{link.PeerName} 在線上（主人不在電腦前）";
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
                    var hostDog = PickHostDog();
                    bool ok = guestPet == null && hostDog != null && !hiddenForFullscreen && !cfg.DoNotDisturb;
                    link.Send(new FriendMsg { T = ok ? "visit-ok" : "visit-no", Id = m.Id });
                    if (ok) SpawnGuest(m, hostDog);
                    break;

                case "visit-ok" when vphase == VisitPhase.Waiting && m.Id == visitId && visitor != null:
                    vphase = VisitPhase.Away;
                    awayFor = 0;
                    visitor.Pet.WalkOut(right: true);
                    Notify?.Invoke($"{visitor.Name} 跑去 {link.PeerName} 家玩了！");
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

        /// <summary>挑一隻在家、有空的狗來接待客人（家裡的狗正在互相玩就先中斷）。</summary>
        Dog PickHostDog()
        {
            var home = Dogs.Where(d => d != visitor && !d.Pet.IsOffscreen).ToList();
            if (home.Count == 0) return null;
            if (homePlay != null) { homePlay.Abort(); homePlay = null; }
            return home.OrderBy(d => d.Pet.Busy ? 1 : 0).ThenBy(_ => rng.Next()).First();
        }

        // ── 自家的狗出門 ─────────────────────────────

        public bool CanVisit => PeerOnline && vphase == VisitPhase.None && director == null && !cfg.DoNotDisturb
                                && Dogs.Any(d => FreeForPlay(d.Pet));

        /// <summary>走到螢幕右邊嗅一嗅，問朋友方不方便，方便就走出去（target = null 時隨機挑一隻）。</summary>
        public void StartVisit(Dog target)
        {
            if (!CanVisit) return;
            var dog = target != null && FreeForPlay(target.Pet) ? target
                    : Dogs.Where(d => FreeForPlay(d.Pet)).OrderBy(_ => rng.Next()).FirstOrDefault();
            if (dog == null) return;
            visitor = dog;
            vphase = VisitPhase.Sniffing;
            vTimer = 60;
            dog.Pet.WalkToEdge(right: true, () =>
            {
                visitId = Guid.NewGuid().ToString("N")[..10];
                link.Send(new FriendMsg { T = "visit-req", Id = visitId, Name = dog.Name, Coat = dog.Bank.Coat, Breed = dog.Bank.Breed.Id, Size = dog.Config.Size });
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
            if (waited) visitor?.Pet.Disappointed();
            visitor = null;
        }

        void ComeHome()
        {
            vphase = VisitPhase.None;
            visitCooldown = 600;
            if (visitor != null)
            {
                if (visitor.Pet.IsOffscreen) visitor.Pet.ComeIn(fromRight: true);
                Notify?.Invoke($"{visitor.Name} 玩夠了，回家了！");
            }
            visitor = null;
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

        void SpawnGuest(FriendMsg m, Dog hostDog)
        {
            guestName = string.IsNullOrEmpty(m.Name) ? "朋友的狗" : m.Name;
            guestVisitId = m.Id;
            var gbank = new SpriteBank(Breeds.Get(m.Breed), m.Coat);   // 不認得的品種（對方版本較新）就用杜賓代替
            guestPet = new PetController(gbank) { Scripted = true, Mischief = false };
            guestScale = (m.Size is >= 2 and <= 4 ? m.Size : cfg.PixelScale) / dpi;   // 朋友那邊設定的大小
            Setup(guestPet, guestScale);
            guestPet.X = guestPet.HiddenX(false);
            guestPet.PoopDropped += x => SpawnPoop(x, guestScale);
            guestPet.CanPoop = () => poops.Count < 3 + Dogs.Count;

            guestWin = new DogWindow(guestPet, $"{guestName}（來作客）", () => Now) { Ground = ground };
            guestWin.ContextMenuRequested += () => ContextMenuRequested?.Invoke(null);
            guestWin.SourceInitialized += (_, __) => guestWin.ApplyScale(guestScale);
            guestWin.Show();

            director = new VisitDirector(hostDog.Pet, guestPet, () => Now);
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
            foreach (var d in Dogs) { d.Bowl?.Close(); d.Win.Close(); }
            slipper?.Close();
            guestWin?.Close();
        }
    }
}
