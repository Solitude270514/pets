using System;

namespace DobyPet
{
    public enum PetState { Idle, Walk, Sit, Sleep, Stretch, Eat, Poop, Proud, Happy, Petted, Backflip, Fart, Dragged, Falling, Land }

    /// <summary>
    /// 狗狗的大腦：決定現在做什麼、位置在哪，並把當下的畫面畫到 Stage 上。
    /// 不碰任何視窗，座標單位是 DIP（X = 腳底中心的螢幕座標，Alt = 離地高度）。
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
        public double MinX, MaxX;

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

        public event Action<double> PoopDropped;   // 參數：便便的 X
        public Func<bool> CanPoop = () => true;

        public PetController(SpriteBank bank, bool demo = false)
        {
            this.bank = bank;
            this.demo = demo;
            Enter(PetState.Idle, 2);
        }

        void Enter(PetState s, double d = 0) { State = s; st = 0; dur = d; }

        /// <summary>正在做不能被打斷的事。</summary>
        public bool Busy => State is PetState.Eat or PetState.Poop or PetState.Dragged or PetState.Falling or PetState.Backflip;

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
                    if (!poopDropped && st > 1.2) { poopDropped = true; PoopDropped?.Invoke(RearX(24)); }
                    if (st >= dur) Enter(PetState.Proud, 3);
                    break;

                default:
                    if (dur > 0 && st >= dur) End();
                    break;
            }
        }

        void End()
        {
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
                default:
                    Next();
                    break;
            }
        }

        /// <summary>決定下一件事。</summary>
        void Next()
        {
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
            if ((r -= pFart) < 0) { Enter(PetState.Fart, 3.6); return; }
            if ((r -= pFlip) < 0) { Enter(PetState.Backflip, 2.8); return; }
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
            switch (demoStep++ % 8)
            {
                case 0: WalkTo(RandomX()); break;
                case 1: Enter(PetState.Sit, 3); break;
                case 2: Enter(PetState.Fart, 3.6); break;
                case 3: Enter(PetState.Backflip, 2.8); break;
                case 4: Enter(PetState.Sleep, 3); break;
                case 5: Enter(PetState.Petted, 2); break;
                case 6: Enter(PetState.Happy, 2); break;
                default: Enter(PetState.Idle, 2); break;
            }
        }

        double RandomX()
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

        /// <summary>屁股那一側的 X（便便、臭屁出現的位置）。</summary>
        double RearX(double px) => X + (FacingRight ? -1 : 1) * px * Scale;

        void WalkTo(double x, Action arrive = null, bool run = false)
        {
            targetX = Clamp(x);
            running = run;
            speed = (run ? 75 : 24) * Scale;
            onArrive = arrive;
            Enter(PetState.Walk);
        }

        // ── 外部指令 ─────────────────────────────

        public void Pet()
        {
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

        public void ComeTo(double x)
        {
            if (Busy) return;
            WalkTo(x, () => Enter(PetState.Happy, 1.6), run: true);
        }

        /// <summary>跑去碗旁邊吃飯。忙碌中回傳 false，呼叫端稍後再試。</summary>
        public bool GoEat(double bowlX, Action arrived)
        {
            if (Busy || dnd) return false;
            bool bowlLeft = bowlX < X;
            double off = 21 * Scale;   // 吃飯圖裡狗碗在嘴巴前方約 21 格
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
                if (Busy) return;
                if (dnd) Enter(PetState.Sleep, 60); else Enter(PetState.Stretch, 1.8);
            }
        }

        public void BeginDrag() { onArrive = null; Enter(PetState.Dragged); }

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
        public void ForceSleep() { if (State is not (PetState.Dragged or PetState.Falling)) Enter(PetState.Sleep, 0); }
        public void WakeUp() { if (State == PetState.Sleep) Enter(PetState.Stretch, 1.8); }

        // ── 畫面 ────────────────────────────────

        Stage s;
        int gx, gy;

        static int WagAt(double t, double ms) => new[] { 0, 1, 0, -1 }[(int)(t / ms) % 4];

        /// <summary>依照鏡像畫特效：xRel 是以面向左為準的位置，面向右時自動對稱。</summary>
        void FxAt(string key, string[] rows, double xRel, double y)
        {
            var img = bank.Fx(key, rows);
            s.Blit(img, FacingRight ? gx - xRel - img.W : gx + xRel, y);
        }

        void Dot(double xRel, double y, uint c)
        {
            int x = (int)Math.Round(FacingRight ? gx - xRel - 1 : gx + xRel);
            s.Set(x, (int)Math.Round(y), c);
        }

        void Dog(PixImage img, double dx = 0, double dy = 0, double sx = 1, double sy = 1, double rot = 0, double? ax = null) =>
            s.Draw(img, gx + (FacingRight ? -dx : dx), gy + dy, FacingRight, sx, sy, FacingRight ? -rot : rot, ax);

        public void Render(Stage stage, double tms)
        {
            s = stage; gx = s.W / 2; gy = s.H;
            double k = st * 1000;   // 目前狀態經過的毫秒

            switch (State)
            {
                case PetState.Idle:
                    Dog(bank.Stand(WagAt(tms, 180), tms % 3400 < 150));
                    break;

                case PetState.Walk:
                case PetState.Dragged:
                {
                    // 被拎起來時四腳亂踢：用走路圖快速播放
                    double ms = State == PetState.Dragged ? 70 : running ? 80 : 150;
                    int f = (int)(tms / ms) % 4;
                    var stand = bank.Info("stand");
                    double ax = bank.Info("walk", f).Ax + (stand.Rows[0].Length / 2.0 - stand.Ax);
                    Dog(bank.Pose("walk", f), ax: ax);
                    break;
                }

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

                case PetState.Stretch:
                    Dog(bank.Pose("stretch"), sx: k < 200 ? .96 : 1);
                    break;

                case PetState.Eat:
                {
                    var img = bank.Pose("eat");
                    Dog(img, sy: (int)(tms / 260) % 2 == 1 ? 1 : .985);
                    double c = tms % 900 / 900, bx = -img.W / 2.0 + 10;   // 飼料屑從碗邊噴出來
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
                        FxAt("puffS", FxArt.PuffS, 17 + p * 4, gy - 26 - p * 10);
                        if (p > .12) FxAt("puffL", FxArt.PuffL, 21 + p * 6, gy - 34 - p * 12);
                        if (p > .3) FxAt("puffS", FxArt.PuffS, 29 + p * 3, gy - 28 - p * 14);
                    }
                    break;
                }

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
