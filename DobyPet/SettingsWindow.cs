using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DobyPet
{
    /// <summary>設定視窗：名字、毛色、大小、搗蛋、朋友配對碼、開機自動啟動。</summary>
    public sealed class SettingsWindow : Window
    {
        public SettingsWindow(Settings cfg)
        {
            Title = "小杜賓設定";
            Width = 340;
            SizeToContent = SizeToContent.Height;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Topmost = true;

            var name = new TextBox { Text = cfg.Name, MaxLength = 12 };
            var coat = new ComboBox { ItemsSource = Palettes.CoatNames, SelectedItem = cfg.Coat };
            var sizes = new[] { (2, "小"), (3, "中"), (4, "大") };
            var size = new ComboBox
            {
                ItemsSource = sizes.Select(s => s.Item2).ToArray(),
                SelectedIndex = System.Math.Max(0, System.Array.FindIndex(sizes, s => s.Item1 == cfg.PixelScale)),
            };
            var mischief = new CheckBox { Content = "允許搗蛋（從螢幕邊偷看、叼拖鞋）", IsChecked = cfg.Mischief, Margin = new Thickness(0, 12, 0, 0) };
            var steal = new CheckBox { Content = "允許咬走游標（只在你停下滑鼠時）", IsChecked = cfg.StealCursor, Margin = new Thickness(0, 6, 0, 0) };

            var code = new TextBox { Text = cfg.FriendCode, CharacterCasing = CharacterCasing.Upper, MaxLength = 32 };
            var gen = new Button { Content = "產生", Width = 56, Margin = new Thickness(6, 0, 0, 0) };
            gen.Click += (_, __) => code.Text = FriendLink.NewCode();
            var codeRow = new DockPanel();
            DockPanel.SetDock(gen, Dock.Right);
            codeRow.Children.Add(gen);
            codeRow.Children.Add(code);
            var codeHint = new TextBlock
            {
                Text = "兩台電腦輸入同一組配對碼，狗狗就會變成朋友，互相去對方家玩。按「產生」建立新的配對碼，再傳給朋友輸入。留白 = 不連線。",
                TextWrapping = TextWrapping.Wrap,
                FontSize = 11,
                Opacity = .7,
                Margin = new Thickness(0, 4, 0, 0),
            };

            var auto = new CheckBox { Content = "開機時自動啟動", IsChecked = cfg.AutoStart, Margin = new Thickness(0, 12, 0, 0) };

            var ok = new Button { Content = "儲存", IsDefault = true, Width = 80, Margin = new Thickness(0, 0, 8, 0) };
            var cancel = new Button { Content = "取消", IsCancel = true, Width = 80 };
            ok.Click += (_, __) =>
            {
                cfg.Name = string.IsNullOrWhiteSpace(name.Text) ? "小黑" : name.Text.Trim();
                cfg.Coat = (string)coat.SelectedItem ?? cfg.Coat;
                cfg.PixelScale = sizes[size.SelectedIndex].Item1;
                cfg.Mischief = mischief.IsChecked == true;
                cfg.StealCursor = steal.IsChecked == true;
                cfg.FriendCode = FriendLink.NormalizeCode(code.Text);
                cfg.AutoStart = auto.IsChecked == true;
                DialogResult = true;
            };

            var panel = new StackPanel { Margin = new Thickness(16) };
            void Row(string label, FrameworkElement c)
            {
                panel.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 8, 0, 4) });
                panel.Children.Add(c);
            }
            Row("名字", name);
            Row("毛色", coat);
            Row("大小", size);
            panel.Children.Add(mischief);
            panel.Children.Add(steal);
            Row("朋友配對碼", codeRow);
            panel.Children.Add(codeHint);
            panel.Children.Add(auto);
            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };
            buttons.Children.Add(ok);
            buttons.Children.Add(cancel);
            panel.Children.Add(buttons);
            Content = panel;
        }
    }
}
