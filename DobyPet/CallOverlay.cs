using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DobyPet
{
    /// <summary>
    /// 「叫牠過來」的選位置模式：蓋一層幾乎透明的全螢幕視窗接住滑鼠，
    /// 工作列上有個腳印跟著游標，點一下就是目的地；Esc 或右鍵取消。
    /// </summary>
    public sealed class CallOverlay : Window
    {
        readonly Action<double> onPick;
        readonly Border hint;
        readonly TextBlock paw;
        readonly double ground;

        public CallOverlay(string petName, double ground, Action<double> onPick)
        {
            this.onPick = onPick;
            this.ground = ground;

            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));   // alpha 1：看不見但接得到點擊
            Topmost = true;
            ShowInTaskbar = false;
            ResizeMode = ResizeMode.NoResize;
            Left = SystemParameters.VirtualScreenLeft;
            Top = SystemParameters.VirtualScreenTop;
            Width = SystemParameters.VirtualScreenWidth;
            Height = SystemParameters.VirtualScreenHeight;
            Cursor = Cursors.Cross;

            hint = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(230, 40, 34, 32)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 6, 10, 6),
                IsHitTestVisible = false,
                Child = new TextBlock
                {
                    Text = $"點一下，叫{petName}跑過來這裡（Esc 取消）",
                    Foreground = Brushes.White,
                    FontSize = 14,
                    FontFamily = new FontFamily("Microsoft JhengHei UI"),
                },
            };
            paw = new TextBlock { Text = "🐾", FontSize = 26, IsHitTestVisible = false };

            var canvas = new Canvas();
            canvas.Children.Add(paw);
            canvas.Children.Add(hint);
            Content = canvas;

            Loaded += (_, __) =>
            {
                Activate();
                // 滑鼠還沒動之前 Mouse.GetPosition 不可靠，改用實際游標的螢幕座標
                var c = System.Windows.Forms.Cursor.Position;
                Follow(PointFromScreen(new Point(c.X, c.Y)));
            };
            MouseMove += (_, e) => Follow(e.GetPosition(this));
            MouseLeftButtonUp += (_, e) => { var p = e.GetPosition(this); Finish(); this.onPick(p.X + Left); };
            MouseRightButtonUp += (_, __) => Finish();
            KeyDown += (_, e) => { if (e.Key == Key.Escape) Finish(); };
            Deactivated += (_, __) => Finish();   // 切到別的視窗也視為取消
        }

        bool finished;

        void Finish()
        {
            if (finished) return;
            finished = true;
            Close();
        }

        /// <summary>提示框跟在游標右下，腳印貼在工作列上方、對齊游標的 X。</summary>
        void Follow(Point p)
        {
            Canvas.SetLeft(hint, p.X + 18);
            Canvas.SetTop(hint, p.Y + 18);
            Canvas.SetLeft(paw, p.X - 16);
            Canvas.SetTop(paw, ground - Top - 38);
        }
    }
}
