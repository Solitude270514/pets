using System;
using System.Collections.Generic;

namespace DobyPet
{
    /// <summary>
    /// 朋友的狗來作客時的導演：用一段腳本指揮兩隻狗一起玩。
    /// 腳本是 iterator，每一步 yield 一個「等到什麼時候才繼續」的條件。
    /// 流程：客人從左邊走進來 → 碰鼻子打招呼 → 隨機玩幾輪 → 道別 → 客人從左邊離開。
    /// </summary>
    public sealed class VisitDirector
    {
        readonly PetController host, guest;
        readonly Func<double> clock;
        readonly Random rng = new();
        IEnumerator<Func<bool>> script;
        Func<bool> waiting;

        public bool Finished { get; private set; }
        public event Action Done;

        public VisitDirector(PetController host, PetController guest, Func<double> clock)
        {
            this.host = host;
            this.guest = guest;
            this.clock = clock;
            script = Script().GetEnumerator();
        }

        double S => host.Scale;

        public void Tick()
        {
            if (Finished) return;
            if (waiting != null && !waiting()) return;
            if (!script.MoveNext())
            {
                Finished = true;
                if (host.Scripted) host.Release();
                Done?.Invoke();
                return;
            }
            waiting = script.Current;
        }

        /// <summary>提早結束（朋友離線、程式要關閉）：客人直接走掉。</summary>
        public void Abort()
        {
            if (Finished) return;
            script = Leave().GetEnumerator();
            waiting = null;
        }

        Func<bool> Wait(double sec) { double end = clock() + sec; return () => clock() >= end; }
        Func<bool> Both => () => host.Settled && guest.Settled;
        static Func<bool> Done1(PetController p) => () => p.Settled;

        IEnumerable<Func<bool>> Script()
        {
            // 客人從左邊螢幕外走進來，主人狗忙完手邊的事再接待
            guest.Scripted = true;
            guest.X = guest.HiddenX(false);
            guest.FacingRight = true;
            yield return () => !host.Busy;
            host.Scripted = true;
            host.Act(PetState.Idle, 0);

            double hx = Math.Max(host.X, host.MinX + 60 * S);
            host.MoveTo(hx);
            guest.MoveTo(hx - 42 * S, run: true, free: true);
            yield return Both;

            // 碰鼻子打招呼
            FaceEachOther();
            host.Act(PetState.Happy, 2.2);
            guest.Act(PetState.Happy, 2.2);
            yield return Both;

            int rounds = 3 + rng.Next(3);
            int last = -1;
            for (int i = 0; i < rounds; i++)
            {
                int pick;
                do pick = rng.Next(6); while (pick == last);
                last = pick;
                IEnumerable<Func<bool>> act = pick switch
                {
                    0 => Chase(),
                    1 => FartPrank(),
                    2 => SitTogether(),
                    3 => NapTogether(),
                    4 => ShowOff(),
                    _ => Chase(),
                };
                foreach (var w in act) yield return w;
            }

            // 客人偶爾會在你家便便
            if (rng.NextDouble() < .3)
            {
                guest.MoveTo(guest.RandomX());
                yield return Done1(guest);
                guest.Act(PetState.Poop, 1.8);
                yield return Done1(guest);
            }

            // 道別
            FaceEachOther();
            guest.Act(PetState.Petted, 1.6);
            host.Act(PetState.Happy, 1.6);
            yield return Both;
            foreach (var w in Leave()) yield return w;
        }

        IEnumerable<Func<bool>> Leave()
        {
            guest.Scripted = true;
            guest.MoveTo(guest.HiddenX(false), free: true);
            yield return Done1(guest);
        }

        void FaceEachOther()
        {
            host.Face(guest.X > host.X);
            guest.Face(host.X > guest.X);
        }

        (PetController a, PetController b) Pair() => rng.Next(2) == 0 ? (host, guest) : (guest, host);

        /// <summary>你追我跑，兩輪。</summary>
        IEnumerable<Func<bool>> Chase()
        {
            var (a, b) = Pair();
            for (int round = 0; round < 2; round++)
            {
                double x = a.RandomX();
                a.MoveTo(x, run: true);
                yield return Wait(.5);
                b.MoveTo(x + (x > b.X ? -36 : 36) * S, run: true);
                yield return Both;
                (a, b) = (b, a);
            }
            FaceEachOther();
            host.Act(PetState.Happy, 1.2);
            guest.Act(PetState.Happy, 1.2);
            yield return Both;
        }

        /// <summary>屁股對著對方放臭屁，對方被臭暈。</summary>
        IEnumerable<Func<bool>> FartPrank()
        {
            var (f, v) = Pair();
            f.Face(v.X < f.X);   // 背對受害者，屁股朝向牠
            v.Face(f.X > v.X);
            f.Act(PetState.Fart, 3.6);
            yield return Wait(1.3);
            v.Act(PetState.Faint, 2.6);
            yield return Both;
            v.Act(PetState.Stretch, 1.8);
            yield return Done1(v);
        }

        /// <summary>並排坐著。</summary>
        IEnumerable<Func<bool>> SitTogether()
        {
            guest.MoveTo(host.X - 40 * S);
            yield return Done1(guest);
            bool right = host.X < (host.MinX + host.MaxX) / 2;
            host.Face(right);
            guest.Face(right);
            host.Act(PetState.Sit, 6);
            guest.Act(PetState.Sit, 6);
            yield return Both;
        }

        /// <summary>一起打個盹。</summary>
        IEnumerable<Func<bool>> NapTogether()
        {
            guest.MoveTo(host.X - 52 * S);
            yield return Done1(guest);
            host.Act(PetState.Sleep, 7);
            guest.Act(PetState.Sleep, 7);
            yield return Both;
            host.Act(PetState.Stretch, 1.8);
            guest.Act(PetState.Stretch, 1.8);
            yield return Both;
        }

        /// <summary>一隻表演後空翻，另一隻坐著看，然後鼓掌（開心跳）。</summary>
        IEnumerable<Func<bool>> ShowOff()
        {
            var (a, b) = Pair();
            FaceEachOther();
            a.Act(PetState.Backflip, 2.8);
            b.Act(PetState.Sit, 2.8);
            yield return Both;
            b.Act(PetState.Happy, 1.6);
            yield return Done1(b);
        }
    }
}
