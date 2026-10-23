using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WF = System.Windows.Forms;

namespace DobyPet
{
    /// <summary>
    /// 狗狗本體的透明視窗。視窗跟著狗移動，只有狗身上的像素能被點到，
    /// 透明的地方滑鼠會直接穿過去。
    /// </summary>
    public sealed class PetWindow : Window
    {
        const int StageW = 100, StageH = 100;

        readonly Settings cfg;
        readonly SpriteBank bank;
        readonly bool demo;
        public readonly PetController Pet;
        readonly Stage stage = new(StageW, StageH);
        readonly WriteableBitmap bmp;
        readonly Image image;
        readonly DispatcherTimer timer;
        readonly Stopwatch clock = Stopwatch.StartNew();
        readonly List<ItemWindow> poops = new();
        ItemWindow bowl;
        bool feedPending;

        IntPtr hwnd;
        double last, checkTimer, topmostTimer;
        bool hiddenForFullscreen;
        double ground;

        // 滑鼠
        Point downPt;
        bool mouseDown, dragging;
        double lastClick;

        public event Action ContextMenuRequested;

        public PetWindow(Settings cfg, SpriteBank bank, bool demo)
        {
            this.cfg = cfg;
            this.bank = bank;
            this.demo = demo;
            Pet = new PetController(bank, demo) { DoNotDisturb = cfg.DoNotDisturb };
            Pet.PoopDropped += SpawnPoop;
            Pet.CanPoop = () => poops.Count < 3;

            Title = "DobyPet";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;
            ShowInTaskbar = false;
            ShowActivated = false;
            ResizeMode = ResizeMode.NoResize;

            bmp = new WriteableBitmap(StageW, StageH, 96, 96, PixelFormats.Bgra32, null);
            image = new Image { Source = bmp, Stretch = Stretch.Fill };
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
            Content = image;
            ToolTip = cfg.Name;

            SourceInitialized += (_, __) =>
            {
                hwnd = new WindowInteropHelper(this).Handle;
                Native.MakeToolWindow(hwnd);
                ApplyScale();
                var wa = SystemParameters.WorkArea;
                Pet.X = wa.Left + wa.Width * .7;
                Place();
            };

            MouseLeftButtonDown += OnDown;
            MouseMove += OnMove;
            MouseLeftButtonUp += OnUp;
            MouseRightButtonUp += (_, __) => ContextMenuRequested?.Invoke();

            timer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(33) };
            timer.Tick += Tick;
            timer.Start();

            if (demo)   // 示範模式：開場就餵一次，順便測試吃飯與便便
            {
                var once = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
                once.Tick += (_, __) => { once.Stop(); Feed(); };
                once.Start();
            }
        }

        // ── 尺寸與位置 ─────────────────────────────

        /// <summary>依設定的像素倍率與螢幕 DPI 算出大小，讓每一格像素都是整數個實體像素，不會糊。</summary>
        public void ApplyScale()
        {
            double dpi = VisualTreeHelper.GetDpi(this).DpiScaleX;
            Pet.Scale = cfg.PixelScale / dpi;
            Width = image.Width = StageW * Pet.Scale;
            Height = image.Height = StageH * Pet.Scale;
            var wa = SystemParameters.WorkArea;
            ground = wa.Bottom;
            Pet.MinX = wa.Left + 30 * Pet.Scale;
            Pet.MaxX = wa.Right - 30 * Pet.Scale;
            Pet.X = Math.Max(Pet.MinX, Math.Min(Pet.MaxX, Pet.X));
            foreach (var p in poops) p.ApplyScale(Pet.Scale, ground);
            bowl?.ApplyScale(Pet.Scale, ground);
        }

        void Place()
        {
            Left = Pet.X - StageW / 2.0 * Pet.Scale;
            Top = ground - StageH * Pet.Scale - Pet.Alt;
        }

        public void SetName(string name) => ToolTip = name;

        // ── 主迴圈 ─────────────────────────────────

        void Tick(object sender, EventArgs e)
        {
            double now = clock.Elapsed.TotalSeconds, dt = Math.Min(.1, now - last);
            last = now;

            checkTimer -= dt;
            if (checkTimer <= 0) { checkTimer = 1; PeriodicChecks(); }
            if (hiddenForFullscreen) return;

            topmostTimer -= dt;
            if (topmostTimer <= 0 && hwnd != IntPtr.Zero) { topmostTimer = 5; Native.KeepTopmost(hwnd); }

            if (feedPending && bowl != null && Pet.GoEat(bowl.X, RemoveBowl)) feedPending = false;

            Pet.Update(dt);
            stage.Clear();
            Pet.Render(stage, now * 1000);
            bmp.WritePixels(new Int32Rect(0, 0, StageW, StageH), stage.Px, StageW * 4, 0);
            Place();

            foreach (var p in poops.ToArray()) p.Tick(now * 1000, dt);
            bowl?.Tick(now * 1000, dt);
        }

        /// <summary>每秒檢查一次：全螢幕、主人在不在。</summary>
        void PeriodicChecks()
        {
            bool fullscreen = Native.ForegroundIsFullscreen();
            if (fullscreen != hiddenForFullscreen)
            {
                hiddenForFullscreen = fullscreen;
                var v = fullscreen ? Visibility.Hidden : Visibility.Visible;
                Visibility = v;
                foreach (var p in poops) p.Visibility = v;
                if (bowl != null) bowl.Visibility = v;
                if (fullscreen) Pet.ForceSleep(); else Pet.WakeUp();
            }
            if (hiddenForFullscreen || dragging) return;

            if (demo) return;   // 示範模式不判斷主人在不在，方便測試
            bool away = Native.IdleSeconds() > 180;
            Pet.SetOwnerAway(away, CursorDip().X);
        }

        Point CursorDip()
        {
            var p = WF.Cursor.Position;
            var src = PresentationSource.FromVisual(this);
            return src?.CompositionTarget != null
                ? src.CompositionTarget.TransformFromDevice.Transform(new Point(p.X, p.Y))
                : new Point(p.X, p.Y);
        }

        // ── 滑鼠：點一下摸頭、點兩下後空翻、拖曳拎起來 ──────────

        void OnDown(object sender, MouseButtonEventArgs e)
        {
            downPt = CursorDip();
            mouseDown = true;
            CaptureMouse();
        }

        void OnMove(object sender, MouseEventArgs e)
        {
            if (!mouseDown) return;
            var p = CursorDip();
            if (!dragging && (Math.Abs(p.X - downPt.X) > 5 || Math.Abs(p.Y - downPt.Y) > 5))
            {
                dragging = true;
                Pet.BeginDrag();
            }
            // 手抓在背上，狗的腳底大約在游標下方 40 格
            if (dragging) Pet.DragTo(p.X, ground - (p.Y + 40 * Pet.Scale));
        }

        void OnUp(object sender, MouseButtonEventArgs e)
        {
            ReleaseMouseCapture();
            if (!mouseDown) return;
            mouseDown = false;
            if (dragging) { dragging = false; Pet.EndDrag(); return; }

            double now = clock.Elapsed.TotalSeconds;
            if (now - lastClick < .35) Pet.DoBackflip(); else Pet.Pet();
            lastClick = now;
        }

        // ── 餵食與便便 ──────────────────────────────

        public void Feed()
        {
            if (bowl != null) return;
            var rnd = new Random();
            double off = (120 + rnd.NextDouble() * 160) * (rnd.Next(2) == 0 ? -1 : 1);
            double x = Pet.X + off;
            if (x < Pet.MinX + 40 || x > Pet.MaxX - 40) x = Pet.X - off;
            bowl = new ItemWindow(ItemKind.Bowl, bank, x, Pet.Scale, ground);
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
            var p = new ItemWindow(ItemKind.Poop, bank, x, Pet.Scale, ground);
            p.Cleaned += item =>
            {
                poops.Remove(item);
                if (Math.Abs(item.X - Pet.X) < 400 * Pet.Scale / 2) Pet.Cheer();
            };
            poops.Add(p);
            p.Show();
        }

        CallOverlay overlay;

        /// <summary>進入選位置模式，點哪裡就跑去哪裡。</summary>
        public void CallOver()
        {
            if (overlay != null) return;
            overlay = new CallOverlay(cfg.Name, ground, x => Pet.ComeTo(x));
            overlay.Closed += (_, __) => overlay = null;
            overlay.Show();
        }

        public void Shutdown()
        {
            timer.Stop();
            foreach (var p in poops.ToArray()) p.Close();
            bowl?.Close();
            Close();
        }
    }
}
