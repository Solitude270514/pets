using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WF = System.Windows.Forms;

namespace DobyPet
{
    /// <summary>
    /// 顯示一隻狗的透明視窗。視窗跟著狗移動，只有狗身上的像素能被點到，
    /// 透明的地方滑鼠會直接穿過去。點一下摸頭、點兩下後空翻、按住拖曳拎起來。
    /// </summary>
    public sealed class DogWindow : Window
    {
        public const int StageW = 100, StageH = 100;

        public readonly PetController Pet;
        readonly Stage stage = new(StageW, StageH);
        readonly WriteableBitmap bmp;
        readonly Image image;
        readonly Func<double> clock;

        public IntPtr Hwnd { get; private set; }
        public double Ground;

        Point downPt;
        bool mouseDown;
        double lastClick;
        public bool Dragging { get; private set; }

        public event Action ContextMenuRequested;

        public DogWindow(PetController pet, string name, Func<double> clock)
        {
            Pet = pet;
            this.clock = clock;

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
            ToolTip = name;

            SourceInitialized += (_, __) =>
            {
                Hwnd = new WindowInteropHelper(this).Handle;
                Native.MakeToolWindow(Hwnd);
            };

            MouseLeftButtonDown += OnDown;
            MouseMove += OnMove;
            MouseLeftButtonUp += OnUp;
            MouseRightButtonUp += (_, __) => ContextMenuRequested?.Invoke();
        }

        public double DpiScale => VisualTreeHelper.GetDpi(this).DpiScaleX;

        public void SetName(string name) => ToolTip = name;

        public void ApplyScale(double scale)
        {
            Width = image.Width = StageW * scale;
            Height = image.Height = StageH * scale;
        }

        public void Render(double tms)
        {
            stage.Clear();
            Pet.Render(stage, tms);
            Left = Pet.X - StageW / 2.0 * Pet.Scale;
            Top = Ground - StageH * Pet.Scale - Pet.Alt;

            // 超出主螢幕左右邊界的部分不畫（探頭時才不會在隔壁螢幕露出整隻狗）
            for (int x = 0; x < StageW; x++)
            {
                double dip = Left + (x + .5) * Pet.Scale;
                if (dip >= Pet.EdgeL && dip <= Pet.EdgeR) continue;
                for (int y = 0; y < StageH; y++) stage.Px[y * StageW + x] = 0;
            }
            bmp.WritePixels(new Int32Rect(0, 0, StageW, StageH), stage.Px, StageW * 4, 0);
        }

        /// <summary>目前游標位置（DIP）。</summary>
        public Point CursorDip()
        {
            var p = WF.Cursor.Position;
            var src = PresentationSource.FromVisual(this);
            return src?.CompositionTarget != null
                ? src.CompositionTarget.TransformFromDevice.Transform(new Point(p.X, p.Y))
                : new Point(p.X, p.Y);
        }

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
            if (!Dragging && (Math.Abs(p.X - downPt.X) > 5 || Math.Abs(p.Y - downPt.Y) > 5))
            {
                Dragging = true;
                Pet.BeginDrag();
            }
            // 手抓在背上，狗的腳底大約在游標下方 40 格
            if (Dragging) Pet.DragTo(p.X, Ground - (p.Y + 40 * Pet.Scale));
        }

        void OnUp(object sender, MouseButtonEventArgs e)
        {
            ReleaseMouseCapture();
            if (!mouseDown) return;
            mouseDown = false;
            if (Dragging) { Dragging = false; Pet.EndDrag(); return; }

            double now = clock();
            if (now - lastClick < .35) Pet.DoBackflip(); else Pet.Pet();
            lastClick = now;
        }
    }
}
