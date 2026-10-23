using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DobyPet
{
    public enum ItemKind { Bowl, Poop }

    /// <summary>放在工作列上的小道具：狗碗、便便（點一下就清掉）。</summary>
    public sealed class ItemWindow : Window
    {
        const int StageW = 24, StageH = 22;

        readonly ItemKind kind;
        readonly SpriteBank bank;
        readonly Stage stage = new(StageW, StageH);
        readonly WriteableBitmap bmp;
        readonly Image image;
        double scale, ground, cleanT = -1;

        public readonly double X;
        public event Action<ItemWindow> Cleaned;

        public ItemWindow(ItemKind kind, SpriteBank bank, double x, double scale, double ground)
        {
            this.kind = kind;
            this.bank = bank;
            X = x;

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
            if (kind == ItemKind.Poop) { Cursor = Cursors.Hand; ToolTip = "點一下清掉"; }

            SourceInitialized += (_, __) => Native.MakeToolWindow(new WindowInteropHelper(this).Handle);
            MouseLeftButtonUp += (_, __) => { if (this.kind == ItemKind.Poop && cleanT < 0) cleanT = 0; };

            ApplyScale(scale, ground);
            Tick(0, 0);
        }

        public void ApplyScale(double scale, double ground)
        {
            this.scale = scale;
            this.ground = ground;
            Width = image.Width = StageW * scale;
            Height = image.Height = StageH * scale;
            Left = X - StageW / 2.0 * scale;
            Top = ground - StageH * scale;
        }

        public void Tick(double tms, double dt)
        {
            stage.Clear();
            if (kind == ItemKind.Bowl)
            {
                var b = bank.Fx("bowl", FxArt.Bowl);
                stage.Blit(b, (StageW - b.W) / 2, StageH - b.H);
            }
            else if (cleanT < 0)
            {
                var p = bank.Fx("poop", FxArt.Poop);
                stage.Blit(p, (StageW - p.W) / 2, StageH - p.H);
                // 兩隻小蒼蠅繞圈
                double a = tms / 240, cx = StageW / 2.0, cy = StageH - 14;
                uint fly = Palettes.Fx['x'];
                stage.Set((int)Math.Round(cx + Math.Cos(a) * 6), (int)Math.Round(cy + Math.Sin(a * 1.3) * 3), fly);
                stage.Set((int)Math.Round(cx + Math.Cos(a + 3) * 5), (int)Math.Round(cy - 2 + Math.Sin(a * 1.1 + 2) * 3), fly);
            }
            else
            {
                cleanT += dt;
                var sp = bank.Fx("spark", FxArt.Spark);
                int f = (int)(cleanT / .11) % 2;
                stage.Blit(sp, StageW / 2 - 6 + f, StageH - 12 - f);
                stage.Blit(sp, StageW / 2 + f - 1, StageH - 8);
                if (cleanT > .6) { Cleaned?.Invoke(this); Close(); return; }
            }
            bmp.WritePixels(new Int32Rect(0, 0, StageW, StageH), stage.Px, StageW * 4, 0);
        }
    }
}
