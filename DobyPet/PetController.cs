using System;

namespace DobyPet
{
    public enum PetState
    {
        Idle, Walk, Sit, Sleep, Stretch, Eat, Poop, Proud, Happy, Petted, Backflip, Fart, Dragged, Falling, Land,
        Peek, PeekHide, Offscreen, Shake, Steal, Faint,
    }

    /// <summary>
    /// 狗狗的大腦：決定現在做什麼、位置在哪，並把當下的畫面畫到 Stage 上。
    /// 不碰任何視窗，座標單位是 DIP（X = 腳底中心的螢幕座標，Alt = 離地高度）。
    /// Scripted = true 時不自己決定下一步，由 VisitDirector 指揮（兩隻狗一起玩的時候）。
    /// </summary>
    public sealed class PetController
    {
        readonly SpriteBank bank;
        readonly Random rng = new();
        readonly bool demo;

        public PetState State { get; private set; }
        double st, dur;                 // 目前狀態已經過幾秒、預計持續幾秒（0 = 不自動結束）

        public double X, Alt;
        double vAlt;
        public bool FacingRight;        // 圖本身面向左，面向右時翻轉
        public double Scale = 2;        // 一格像素 = 幾個 DIP
        public double MinX, MaxX;       // 可以散步的範圍
        public double EdgeL, EdgeR;     // 螢幕左右邊界（探頭、出門用）

        double targetX, speed;
        bool running;
        Action onArrive;

        public bool OwnerAway { get; private set; }
        double awayTime, greetX;
        bool greetPending, flipAfterHappy, poopDropped;
        bool dnd;
        double poopTimer = -1;          // 吃完飯後倒數，歸零就去便便
        double fartBoost;               // 吃完飯後放屁機率提高，隨時間遞減
        int demoStep;

        // 搗蛋
        public bool Mischief = true;
        bool carrying;                  // 嘴裡叼著拖鞋
        double peekFrom, peekTo, stealDir;
        Action afterOffscreen;

        public bool Scripted;

        public event Action<double> PoopDropped;     // 參數：便便的 X
        public event Action<double> SlipperDropped;  // 參數：拖鞋的 X
        public Func<bool> CanPoop = () => true;
        public Func<bool> CanFetchSlipper = () => true;

        public PetController(SpriteBank bank, bool demo = false)
        {
            this.bank = bank;
            this.demo = demo;
            Enter(PetState.Idle, 2);
        }

        public SpriteBank Bank => bank;

        void Enter(PetState s, double d = 0) { State = s; st = 0; dur = d; }

        /// <summary>正在做不能被打斷的事。</summary>
        public bool Busy => State is PetState.Eat or PetState.Poop or PetState.Dragged or PetState.Falling
            or PetState.Backflip or PetState.Steal or PetState.Offscreen or PetState.PeekHide or PetState.Peek;

        /// <summary>被指揮時：這一步做完了，在原地待命。</summary>
        public bool Settled => State == PetState.Idle && dur == 0;

        public bool IsOffscreen => State == PetState.Offscreen;

        // ── 每一格更新 ─────────────────────────────

        public void Update(double dt)
        {
            st += dt;
            if (OwnerAway) awayTime += dt;
            if (poopTimer > 0) poopTimer -= dt;
            fartBoost = Math.Max(0, fartBoost - dt / 600);

            switch (State)
            {
                case PetState.Walk:
                    int dir = Math.Sign(targetX - X);
                    if (dir != 0) FacingRight = dir > 0;
                    double step = speed * dt;
                    if (Math.Abs(targetX - X) <= step)
                    {
                        X = targetX;
                        var arrive = onArrive; onArrive = null;
                        if (arrive != null) arrive(); else Next();
                    }
                    else X += dir * step;
                    break;

                case PetState.Falling:
                    vAlt -= 2600 * dt;
                    Alt += vAlt * dt;
                    if (Alt <= 0) { Alt = 0; vAlt = 0; Enter(PetState.Land, .2); }
                    break;

                case PetState.Dragged:
                    break;

                case PetState.Poop:
                    if (!poopDropped && st > 1.2) { poopDropped = true; PoopDropped?.Invoke(RearX(B.PoopRear)); }
                    if (st >= dur) Enter(PetState.Proud, 3);
                    break;

                case PetState.Peek:   // 從螢幕邊緣滑進來，只露出頭
                    X = peekFrom + (peekTo - peekFrom) * Math.Min(1, st / .5);
                    if (dur > 0 && st >= dur) End();
                    break;

                case PetState.PeekHide:   // 被發現了，縮回去
                    X = peekTo + (peekFrom - peekTo) * Math.Min(1, st / .3);
                    if (st >= .3)
                    {
                        afterOffscreen = () => WalkTo(RandomX(), () => Enter(PetState.Happy, 1.2));
                        Enter(PetState.Offscreen, 1.2);
                    }
                    break;

                case PetState.Offscreen:
                    if (dur > 0 && st >= dur)
                    {
                        var after = afterOffscreen; afterOffscreen = null;
                        if (after != null) after(); else WalkTo(RandomX());
                    }
                    break;

                case PetState.Steal:   // 咬住游標往回跑
                    if (st > .35)
                    {
                        FacingRight = stealDir > 0;
                        X = Clamp(X + stealDir * 70 * Scale * dt);
                    }
                    if (st >= dur) Enter(PetState.Happy, 1.2);
                    break;

                default:
                    if (dur > 0 && st >= dur) End();
                    break;
            }
        }

        void End()
        {
            if (Scripted) { Enter(PetState.Idle, 0); return; }
            switch (State)
            {
                case PetState.Sleep:
                    Enter(PetState.Stretch, 1.8);
                    break;
                case PetState.Stretch:
                    if (greetPending) Greet(); else Enter(PetState.Idle, 1.5);
                    break;
                case PetState.Eat:
                    poopTimer = demo ? 3 : 60 + rng.NextDouble() * 120;
                    fartBoost = 1;
                    Enter(PetState.Happy, 1.4);
                    break;
                case PetState.Happy when flipAfterHappy:
                    flipAfterHappy = false;
                    Enter(PetState.Backflip, 2.8);
                    break;
                case PetState.Peek:
                    WalkTo(RandomX());   // 看夠了，大搖大擺走進來
                    break;
                case PetState.Shake:
                    WalkTo(RandomX(), DropSlipper);
                    break;
                default:
                    Next();
                    break;
            }
        }

        /// <summary>決定下一件事。</summary>
        void Next()
        {
            if (Scripted) { Enter(PetState.Idle, 0); return; }
            if (dnd) { Enter(PetState.Sleep, 60); return; }
            if (OwnerAway)
            {
                // 主人不在：先坐著等，等久了就趴下睡
                if (awayTime < 600) Enter(PetState.Sit, 30); else Enter(PetState.Sleep, 120);
                return;
            }
            if (poopTimer != -1 && poopTimer <= 0 && CanPoop())
            {
                poopTimer = -1;
                WalkTo(RandomX(), () => { poopDropped = false; Enter(PetState.Poop, 1.8); });
                return;
            }
            if (demo) { DemoNext(); return; }

            double r = rng.NextDouble();
            double pFart = 0.004 + 0.04 * fartBoost, pFlip = 0.006;
            double pPeek = Mischief ? 0.02 : 0, pSlipper = Mischief && CanFetchSlipper() ? 0.008 : 0;
            if ((r -= pFart) < 0) { Enter(PetState.Fart, 3.6); return; }
            if ((r -= pFlip) < 0) { Enter(PetState.Backflip, 2.8); return; }
            if ((r -= pPeek) < 0) { StartPeek(); return; }
            if ((r -= pSlipper) < 0) { StartSlipper(); return; }
            if ((r -= 0.34) < 0) { WalkTo(RandomX()); return; }
            if ((r -= 0.22) < 0) { Enter(PetState.Idle, 3 + rng.NextDouble() * 4); return; }
            if ((r -= 0.18) < 0) { Enter(PetState.Sit, 6 + rng.NextDouble() * 12); return; }
            if ((r -= 0.06) < 0) { Enter(PetState.Sleep, 25 + rng.NextDouble() * 50); return; }
            if ((r -= 0.04) < 0) { Enter(PetState.Stretch, 1.8); return; }
            Enter(PetState.Idle, 2 + rng.NextDouble() * 3);
        }

        /// <summary>示範模式：依序展示每個動作（開發測試用）。</summary>
        void DemoNext()
        {
            switch (demoStep++ % 10)
            {
                case 0: WalkTo(RandomX()); break;
                case 1: StartPeek(); break;
                case 2: Enter(PetState.Fart, 3.6); break;
                case 3: StartSlipper(); break;
                case 4: Enter(PetState.Backflip, 2.8); break;
                case 5: Enter(PetState.Sleep, 3); break;
                case 6: Enter(PetState.Petted, 2); break;
                case 7: Enter(PetState.Sit, 3); break;
                case 8: Enter(PetState.Happy, 2); break;
                default: Enter(PetState.Idle, 2); break;
            }
        }

        public double RandomX()
        {
            for (int i = 0; i < 8; i++)
            {
                double x = MinX + rng.NextDouble() * (MaxX - MinX);
                double d = Math.Abs(x - X);
                if (d > 80 && d < 600) return x;
            }
            return MinX + rng.NextDouble() * (MaxX - MinX);
        }

        double Clamp(double x) => Math.Max(MinX, Math.Min(MaxX, x));

        /// <summary>屁股那一側的 X（便便出現的位置）。</summary>
        double RearX(double px) => X + (FacingRight ? -1 : 1) * px * Scale;

        /// <summary>嘴巴的位置（DIP），用來叼拖鞋、咬游標。</summary>
        Breed B => bank.Breed;

        /// <summary>嘴巴相對站姿中心的水平位移（面向左時，像素）。</summary>
        double MouthDx => B.MouthX - B.StandW / 2.0;

        public double MouthX => X + (FacingRight ? -1 : 1) * MouthDx * Scale;
        public double MouthY(double ground) => ground - Alt - (B.StandH - B.MouthY) * Scale;

        /// <summary>藏在螢幕外的位置。</summary>
        public double HiddenX(bool right) => right ? EdgeR + 34 * Scale : EdgeL - 34 * Scale;

        void WalkTo(double x, Action arrive = null, bool run = false, bool free = false)
        {
            targetX = free ? x : Clamp(x);
            running = run;
            speed = (run ? 75 : 24) * Scale;
            onArrive = arrive;
            Enter(PetState.Walk);
        }

        // ── 搗蛋 ─────────────────────────────────

        /// <summary>走出螢幕，再從邊緣探頭偷看。</summary>
        void StartPeek()
        {
            bool right = rng.Next(2) == 0;
            WalkTo(HiddenX(right), () =>
            {
                FacingRight = !right;   // 轉身面向螢幕內
                peekFrom = X;
                double show = B.StandW / 2.0 - B.Peek;   // 只露出頭
                peekTo = right ? EdgeR + show * Scale : EdgeL - show * Scale;
                Enter(PetState.Peek, 4 + rng.NextDouble() * 4);
            }, free: true);
        }

        /// <summary>跑出螢幕，叼一隻拖鞋回來甩，甩夠了丟在工作列上。</summary>
        void StartSlipper()
        {
            bool right = X > (EdgeL + EdgeR) / 2;
            WalkTo(HiddenX(right), () =>
            {
                afterOffscreen = () =>
                {
                    carrying = true;
                    WalkTo(RandomX(), () => Enter(PetState.Shake, 2.5), run: true);
                };
                Enter(PetState.Offscreen, 1.5);
            }, run: true, free: true);
        }

        void DropSlipper()
        {
            if (!carrying) return;
            carrying = false;
            SlipperDropped?.Invoke(MouthX);
            if (!Busy) Enter(PetState.Proud, 2.5);
        }

        /// <summary>游標在附近而且主人沒在動滑鼠：跑過去咬住游標拖著跑。</summary>
        public bool TryStealCursor(double cursorX)
        {
            if (Busy || Scripted || carrying || State is PetState.Sleep or PetState.Eat) return false;
            bool left = cursorX < X;
            WalkTo(cursorX + (left ? -MouthDx : MouthDx) * Scale, () =>
            {
                FacingRight = !left;
                stealDir = left ? 1 : -1;   // 咬到之後往回跑
                Enter(PetState.Steal, 1.6);
            }, run: true);
            return true;
        }

        /// <summary>咬住游標中（PetHost 會把游標移到嘴巴的位置）。</summary>
        public bool HoldingCursor => State == PetState.Steal && st > .35;

        /// <summary>主人把游標搶回去了。</summary>
        public void ReleaseCursor() { if (State == PetState.Steal) Enter(PetState.Happy, 1.2); }

        // ── 外部指令 ─────────────────────────────

        public void Pet()
        {
            if (State == PetState.Peek) { Enter(PetState.PeekHide, .3); return; }   // 被抓到偷看
            if (carrying) DropSlipper();
            if (Busy) return;
            if (State == PetState.Sleep) { Enter(PetState.Stretch, 1.8); return; }
            Enter(PetState.Petted, 1.8);
        }

        public void Cheer() { if (!Busy && State != PetState.Sleep) Enter(PetState.Happy, 1.6); }
        public void DoBackflip() { if (!Busy) Enter(PetState.Backflip, 2.8); }
        public void DoFart() { if (!Busy) Enter(PetState.Fart, 3.6); }
        public void DoSit() { if (!Busy) Enter(PetState.Sit, 15); }
        public void DoSleep() { if (!Busy) Enter(PetState.Sleep, 60); }
        public void DoStretch() { if (!Busy) Enter(PetState.Stretch, 1.8); }
        public void DoPeek() { if (!Busy && !Scripted) StartPeek(); }
        public void DoSlipper() { if (!Busy && !Scripted && !carrying && CanFetchSlipper()) StartSlipper(); }

        public void ComeTo(double x)
        {
            if (Busy) return;
            WalkTo(x, () => Enter(PetState.Happy, 1.6), run: true);
        }

        /// <summary>跑去碗旁邊吃飯。忙碌中回傳 false，呼叫端稍後再試。</summary>
        public bool GoEat(double bowlX, Action arrived)
        {
            if (Busy || dnd || Scripted) return false;
            if (carrying) DropSlipper();
            bool bowlLeft = bowlX < X;
            double off = (bank.Info("eat").Rows[0].Length / 2.0 - B.BowlX) * Scale;   // 吃飯圖裡狗碗離身體中心的距離
            WalkTo(bowlLeft ? bowlX + off : bowlX - off, () =>
            {
                FacingRight = !bowlLeft;
                arrived();
                Enter(PetState.Eat, 4.5);
            }, run: true);
            return true;
        }

        public bool DoNotDisturb
        {
            get => dnd;
            set
            {
                if (dnd == value) return;
                dnd = value;
                if (Busy || Scripted) return;
                if (dnd) Enter(PetState.Sleep, 60); else Enter(PetState.Stretch, 1.8);
            }
        }

        public void BeginDrag()
        {
            if (carrying) DropSlipper();
            onArrive = null;
            Enter(PetState.Dragged);
        }

        public void DragTo(double x, double alt) { X = Clamp(x); Alt = Math.Max(0, alt); }

        public void EndDrag()
        {
            if (Alt > 1) { vAlt = 0; Enter(PetState.Falling); }
            else { Alt = 0; Enter(PetState.Land, .2); }
        }

        /// <summary>主人離開 / 回來（由閒置時間判斷）。</summary>
        public void SetOwnerAway(bool away, double cursorX)
        {
            if (away == OwnerAway) return;
            OwnerAway = away;
            if (Scripted || State == PetState.Offscreen) return;
            if (away)
            {
                awayTime = 0;
                if (!Busy) Enter(PetState.Sit, 30);
                return;
            }
            greetX = cursorX;
            if (State == PetState.Sleep) { greetPending = true; Enter(PetState.Stretch, 1.8); }
            else if (!Busy) Greet();
        }

        void Greet()
        {
            greetPending = false;
            WalkTo(greetX, () => { flipAfterHappy = rng.NextDouble() < .5; Enter(PetState.Happy, 2.4); }, run: true);
        }

        /// <summary>全螢幕程式開啟時：不顯示，讓牠安靜睡覺。</summary>
        public void ForceSleep() { if (State is not (PetState.Dragged or PetState.Falling or PetState.Offscreen)) Enter(PetState.Sleep, 0); }
        public void WakeUp() { if (State == PetState.Sleep) Enter(PetState.Stretch, 1.8); }

        // ── 出門拜訪（第三階段） ─────────────────────

        /// <summary>走到螢幕邊緣嗅一嗅。</summary>
        public void WalkToEdge(bool right, Action arrived)
        {
            if (carrying) DropSlipper();
            WalkTo(right ? MaxX : MinX, () => { FacingRight = right; arrived(); Enter(PetState.Idle, 0); });
        }

        /// <summary>走出螢幕，留在畫面外（去朋友家）。</summary>
        public void WalkOut(bool right, Action gone = null)
        {
            WalkTo(HiddenX(right), () => { Enter(PetState.Offscreen, 0); gone?.Invoke(); }, free: true);
        }

        /// <summary>從螢幕外走進來。</summary>
        public void ComeIn(bool fromRight, Action arrived = null)
        {
            X = HiddenX(fromRight);
            Alt = 0;
            FacingRight = !fromRight;
            double target = fromRight ? MaxX - rng.NextDouble() * 300 : MinX + rng.NextDouble() * 300;
            WalkTo(target, arrived ?? (() => Enter(PetState.Happy, 1.6)));
        }

        /// <summary>嗅完邊緣，朋友不在家：失望地坐下。</summary>
        public void Disappointed() { if (State == PetState.Idle) Enter(PetState.Sit, 5); }

        /// <summary>給 VisitDirector 用：移動、轉向、做某個動作。</summary>
        public void MoveTo(double x, bool run = false, bool free = false) => WalkTo(x, null, run, free);
        public void Face(bool right) => FacingRight = right;
        public void Act(PetState s, double d)
        {
            if (s == PetState.Poop) poopDropped = false;
            Enter(s, d);
        }
        public void Release() { Scripted = false; Enter(PetState.Idle, 2); }

        // ── 畫面 ────────────────────────────────

        Stage s;
        int gx, gy;

        static int WagAt(double t, double ms) => new[] { 0, 1, 0, -1 }[(int)(t / ms) % 4];

        /// <summary>依照鏡像畫特效：xRel 是以面向左為準的位置，面向右時自動對稱。</summary>
        void FxAt(string key, string[] rows, double xRel, double y)
        {
            var img = FxArt.Get(key, rows);
            s.Blit(img, FacingRight ? gx - xRel - img.W : gx + xRel, y);
        }

        void Dot(double xRel, double y, uint c)
        {
            int x = (int)Math.Round(FacingRight ? gx - xRel - 1 : gx + xRel);
            s.Set(x, (int)Math.Round(y), c);
        }

        void Dog(PixImage img, double dx = 0, double dy = 0, double sx = 1, double sy = 1, double rot = 0, double? ax = null) =>
            s.Draw(img, gx + (FacingRight ? -dx : dx), gy + dy, FacingRight, sx, sy, FacingRight ? -rot : rot, ax);

        void WalkFrame(double ms)
        {
            int f = (int)(lastT / ms) % 4;
            var stand = bank.Info("stand");
            double ax = bank.Info("walk", f).Ax + (stand.Rows[0].Length / 2.0 - stand.Ax);
            Dog(bank.Pose("walk", f), ax: ax);
        }

        /// <summary>嘴裡的拖鞋（甩的時候會晃）。</summary>
        void Slipper(double tms, bool shaking, double dy = 0)
        {
            double jx = shaking ? ((int)(tms / 90) % 2 == 0 ? -3 : 2) : 0;
            double jy = shaking ? (int)(tms / 90) % 2 : 0;
            FxAt("slipper", FxArt.Slipper, MouthDx - 5 + jx, gy - (B.StandH - B.MouthY) - 2 + jy + dy);
        }

        double lastT;

        public void Render(Stage stage, double tms)
        {
            s = stage; gx = s.W / 2; gy = s.H; lastT = tms;
            double k = st * 1000;   // 目前狀態經過的毫秒

            switch (State)
            {
                case PetState.Idle:
                    Dog(bank.Stand(WagAt(tms, 180), tms % 3400 < 150));
                    if (carrying) Slipper(tms, false);
                    break;

                case PetState.Walk:
                    WalkFrame(running ? 80 : 150);
                    if (carrying) Slipper(tms, false, (int)(tms / (running ? 80 : 150)) % 2);
                    break;

                case PetState.Dragged:   // 被拎起來時四腳亂踢：用走路圖快速播放
                    WalkFrame(70);
                    break;

                case PetState.Sit:
                case PetState.Proud:
                    Dog(bank.Pose("sit"), sy: (int)(tms / 900) % 2 == 1 ? 1.015 : 1);
                    if (State == PetState.Proud && k > 1500)
                        FxAt("heart", FxArt.Heart, -6, gy - 66 - (k - 1500) / 1500 * 10);
                    break;

                case PetState.Sleep:
                {
                    Dog(bank.Pose("sleep"), sy: (int)(tms / 1000) % 2 == 1 ? 1.03 : 1);
                    double z = tms % 2600 / 2600;
                    FxAt("zz", FxArt.Zz, 12 + z * 6, gy - 40 - z * 14);
                    if (z > .5) FxAt("zz", FxArt.Zz, 10 + (z - .5) * 6, gy - 40 - (z - .5) * 14);
                    break;
                }

                case PetState.Faint:   // 被臭暈：趴著，頭上轉星星
                {
                    Dog(bank.Pose("sleep"), sy: .96);
                    double a = tms / 180;
                    for (int i = 0; i < 3; i++)
                        FxAt("spark", FxArt.Spark, -20 + Math.Cos(a + i * 2.1) * 9, gy - 44 + Math.Sin(a + i * 2.1) * 3);
                    break;
                }

                case PetState.Stretch:
                    Dog(bank.Pose("stretch"), sx: k < 200 ? .96 : 1);
                    break;

                case PetState.Eat:
                {
                    var img = bank.Pose("eat");
                    Dog(img, sy: (int)(tms / 260) % 2 == 1 ? 1 : .985);
                    double c = tms % 900 / 900, bx = -img.W / 2.0 + B.BowlX + 2;   // 飼料屑從碗邊噴出來
                    if (c < .6)
                    {
                        uint o = Palettes.Fx['o'];
                        Dot(bx - 3 - c * 6, gy - 8 - c * 6 + c * c * 14, o);
                        Dot(bx + 8 + c * 5, gy - 9 - c * 5 + c * c * 12, o);
                    }
                    break;
                }

                case PetState.Poop:
                    Dog(bank.Pose("poop"), dx: k > 500 && k < 1300 ? (int)(tms / 80) % 2 : 0);
                    break;

                case PetState.Happy:
                {
                    double j = tms % 700, h = j < 400 ? 14 * 4 * (j / 400) * (1 - j / 400) : 0;
                    Dog(bank.Stand(WagAt(tms, 70)), dy: -h, sy: j > 400 && j < 480 ? .92 : 1);
                    FxAt("heart", FxArt.Heart, 14, gy - 60 - tms % 1600 / 1600 * 20);
                    break;
                }

                case PetState.Petted:
                {
                    Dog(bank.Stand(WagAt(tms, 90), blink: true), dy: (int)(tms / 400) % 2);
                    double hk = tms % 1400 / 1400;
                    FxAt("heart", FxArt.Heart, -24, gy - 62 - hk * 14);
                    if (hk > .4) FxAt("heart", FxArt.Heart, 8, gy - 58 - (hk - .4) * 22);
                    break;
                }

                case PetState.Backflip:
                    if (k < 500) Dog(bank.Stand(WagAt(tms, 140)));
                    else if (k < 720) Dog(bank.Stand(0, half: true), sx: 1.05, sy: .88);           // 蹲低蓄力
                    else if (k < 1520)
                    {
                        double p = (k - 720) / 800, h = 30 * 4 * p * (1 - p);
                        double e = p < .5 ? 2 * p * p : 1 - Math.Pow(-2 * p + 2, 2) / 2;
                        Dog(bank.Stand(0, blink: true), dy: -h, rot: e * Math.PI * 2);           // 空中翻一圈
                    }
                    else if (k < 1680)
                    {
                        Dog(bank.Stand(0), sx: 1.07, sy: .86);                                     // 落地壓扁
                        FxAt("spark", FxArt.Spark, -34, gy - 14);
                        FxAt("spark", FxArt.Spark, 24, gy - 12);
                    }
                    else
                    {
                        Dog(bank.Stand(WagAt(tms, 90)));
                        if (k < 2000) { FxAt("spark", FxArt.Spark, -32, gy - 22); FxAt("spark", FxArt.Spark, 26, gy - 20); }
                    }
                    break;

                case PetState.Fart:
                {
                    int dx = k > 500 && k < 900 ? ((int)(k / 60) % 2 == 1 ? 1 : -1) : 0;
                    Dog(bank.Stand(k < 500 ? WagAt(tms, 140) : 0, half: k >= 900 && k < 3000), dx: dx);
                    if (k >= 900 && k < 3200)
                    {
                        double p = (k - 900) / 2300;
                        double rx = B.RearX - B.StandW / 2.0, ry = gy - (B.StandH - B.RearY);   // 屁股的位置
                        FxAt("puffS", FxArt.PuffS, rx + p * 4, ry - p * 10);
                        if (p > .12) FxAt("puffL", FxArt.PuffL, rx + 4 + p * 6, ry - 8 - p * 12);
                        if (p > .3) FxAt("puffS", FxArt.PuffS, rx + 12 + p * 3, ry - 2 - p * 14);
                    }
                    break;
                }

                case PetState.Shake:   // 叼著拖鞋猛甩頭
                    Dog(bank.Stand(WagAt(tms, 70)), dx: (int)(tms / 90) % 2 == 0 ? -1 : 1);
                    Slipper(tms, true);
                    break;

                case PetState.Peek:
                case PetState.PeekHide:
                    Dog(bank.Stand(0, blink: tms % 2600 < 160));
                    break;

                case PetState.Steal:
                    if (k < 350) Dog(bank.Stand(WagAt(tms, 70)), dy: -Math.Sin(k / 350 * Math.PI) * 8);   // 跳起來咬
                    else WalkFrame(70);
                    break;

                case PetState.Offscreen:
                    break;

                case PetState.Falling:
                    Dog(bank.Stand(0, half: true));
                    break;

                case PetState.Land:
                    Dog(bank.Stand(0), sx: 1.07, sy: .86);
                    break;
            }
        }
    }
}
