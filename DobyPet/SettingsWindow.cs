using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DobyPet
{
    /// <summary>設定視窗：我的狗狗（新增、移除、名字、品種、毛色、大小）、搗蛋、朋友配對碼、開機與更新。</summary>
    public sealed class SettingsWindow : Window
    {
        readonly List<PetConfig> pets;
        readonly ListBox list = new() { Height = 96 };
        readonly TextBox name = new() { MaxLength = 12 };
        readonly ComboBox breed = new() { DisplayMemberPath = nameof(Breed.DisplayName) };
        readonly ComboBox coat = new();
        readonly ComboBox size = new();
        readonly Button dice = new() { Content = "🎲", Width = 26, Margin = new Thickness(4, 0, 0, 0), ToolTip = "換一個隨機名字" };
        readonly Random rng = new();
        readonly Button add = new() { Content = "＋ 新增", Margin = new Thickness(0, 0, 0, 6) };
        readonly Button remove = new() { Content = "－ 移除" };
        bool loading;

        PetConfig Current => list.SelectedIndex >= 0 ? pets[list.SelectedIndex] : null;

        static readonly (int px, string label)[] Sizes = { (2, "小"), (3, "中"), (4, "大") };

        /// <summary>新增狗狗時隨機挑一個沒用過的名字。</summary>
        static readonly string[] NamePool =
        {
            "麻糬", "布丁", "湯圓", "黑糖", "奶茶", "豆豆", "可樂", "芝麻", "饅頭", "球球", "皮蛋", "年糕",
            "花生", "雪球", "糖糖", "阿福", "旺財", "小虎", "毛毛", "咖啡", "可可", "蛋捲", "飯糰", "泡芙",
            "肉鬆", "豆花", "米香", "仙草", "QQ", "Lucky", "Momo", "Coco", "Lucy", "Max",
        };

        string RandomName() =>
            NamePool.Where(n => pets.All(p => p.Name != n)).OrderBy(_ => rng.Next()).FirstOrDefault() ?? $"狗狗{pets.Count + 1}";

        public SettingsWindow(Settings cfg)
        {
            Title = "小杜賓設定";
            Width = 470;
            SizeToContent = SizeToContent.Height;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Topmost = true;

            // 在副本上編輯，按儲存才寫回
            pets = cfg.Pets.Select(p => new PetConfig { Id = p.Id, Name = p.Name, Breed = p.Breed, Coat = p.Coat, Size = p.Size }).ToList();
            breed.ItemsSource = Breeds.All.Values.ToList();

            list.SelectionChanged += (_, __) => LoadCurrent();
            name.TextChanged += (_, __) => { if (!loading && Current != null) { Current.Name = name.Text.Trim(); RefreshList(); } };
            breed.SelectionChanged += (_, __) =>
            {
                if (loading || Current == null || breed.SelectedItem is not Breed b) return;
                Current.Breed = b.Id;
                Current.Coat = b.DefaultCoat;
                LoadCurrent();
                RefreshList();
            };
            coat.SelectionChanged += (_, __) => { if (!loading && Current != null && coat.SelectedItem is string c) { Current.Coat = c; RefreshList(); } };
            size.ItemsSource = Sizes.Select(x => x.label).ToArray();
            size.SelectionChanged += (_, __) => { if (!loading && Current != null && size.SelectedIndex >= 0) { Current.Size = Sizes[size.SelectedIndex].px; RefreshList(); } };
            dice.Click += (_, __) => { if (Current != null) { Current.Name = RandomName(); LoadCurrent(); RefreshList(); } };
            add.Click += (_, __) =>
            {
                // 新來的狗：隨機名字、隨機品種與毛色，大小跟第一隻一樣
                var all = Breeds.All.Values.ToList();
                var b = all[rng.Next(all.Count)];
                pets.Add(new PetConfig
                {
                    Name = RandomName(), Breed = b.Id, Coat = b.CoatNames[rng.Next(b.CoatNames.Length)],
                    Size = pets.Count > 0 ? pets[0].Size : cfg.PixelScale,
                });
                RefreshList();
                list.SelectedIndex = pets.Count - 1;
            };
            remove.Click += (_, __) =>
            {
                if (Current == null || pets.Count <= 1) return;
                int i = list.SelectedIndex;
                pets.RemoveAt(i);
                RefreshList();
                list.SelectedIndex = System.Math.Min(i, pets.Count - 1);
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
            var autoUpdate = new CheckBox { Content = "有新版時自動更新（在你離開電腦時安裝）", IsChecked = cfg.AutoUpdate, Margin = new Thickness(0, 6, 0, 0) };

            var ok = new Button { Content = "儲存", IsDefault = true, Width = 80, Margin = new Thickness(0, 0, 8, 0) };
            var cancel = new Button { Content = "取消", IsCancel = true, Width = 80 };
            ok.Click += (_, __) =>
            {
                foreach (var p in pets)
                    if (string.IsNullOrWhiteSpace(p.Name)) p.Name = RandomName();
                cfg.Pets = pets;
                cfg.Mischief = mischief.IsChecked == true;
                cfg.StealCursor = steal.IsChecked == true;
                cfg.FriendCode = FriendLink.NormalizeCode(code.Text);
                cfg.AutoStart = auto.IsChecked == true;
                cfg.AutoUpdate = autoUpdate.IsChecked == true;
                DialogResult = true;
            };

            var panel = new StackPanel { Margin = new Thickness(16) };
            void Label(string text, double top = 8) => panel.Children.Add(new TextBlock { Text = text, Margin = new Thickness(0, top, 0, 4) });
            void Row(string label, FrameworkElement c) { Label(label); panel.Children.Add(c); }

            Label($"我的狗狗（最多 {Settings.MaxPets} 隻）", 0);
            var buttons1 = new StackPanel { Margin = new Thickness(8, 0, 0, 0), Width = 72 };
            buttons1.Children.Add(add);
            buttons1.Children.Add(remove);
            var listRow = new DockPanel();
            DockPanel.SetDock(buttons1, Dock.Right);
            listRow.Children.Add(buttons1);
            listRow.Children.Add(list);
            panel.Children.Add(listRow);

            var editor = new Grid { Margin = new Thickness(0, 4, 0, 0) };
            foreach (var w in new[] { 3.0, 0, 2.4, 0, 2.2, 0, 1.4 })   // 名字、品種、毛色、大小，中間 8px 間隔
                editor.ColumnDefinitions.Add(new ColumnDefinition { Width = w == 0 ? new GridLength(8) : new GridLength(w, GridUnitType.Star) });
            void Cell(int col, string label, FrameworkElement c)
            {
                var sp = new StackPanel();
                sp.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 4, 0, 4) });
                sp.Children.Add(c);
                Grid.SetColumn(sp, col);
                editor.Children.Add(sp);
            }
            var nameRow = new DockPanel();
            DockPanel.SetDock(dice, Dock.Right);
            nameRow.Children.Add(dice);
            nameRow.Children.Add(name);
            Cell(0, "名字", nameRow);
            Cell(2, "品種", breed);
            Cell(4, "毛色", coat);
            Cell(6, "大小", size);
            panel.Children.Add(editor);

            panel.Children.Add(mischief);
            panel.Children.Add(steal);
            Row("朋友配對碼", codeRow);
            panel.Children.Add(codeHint);
            panel.Children.Add(auto);
            panel.Children.Add(autoUpdate);
            panel.Children.Add(new TextBlock { Text = $"目前版本 v{Updater.CurrentText}", FontSize = 11, Opacity = .6, Margin = new Thickness(0, 8, 0, 0) });
            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };
            buttons.Children.Add(ok);
            buttons.Children.Add(cancel);
            panel.Children.Add(buttons);
            Content = panel;

            RefreshList();
            list.SelectedIndex = 0;
        }

        void RefreshList()
        {
            int sel = list.SelectedIndex;
            loading = true;
            list.Items.Clear();
            foreach (var p in pets)
                list.Items.Add($"{(string.IsNullOrWhiteSpace(p.Name) ? "（未命名）" : p.Name)}　{Breeds.Get(p.Breed).Name}・{p.Coat}・{Sizes.FirstOrDefault(x => x.px == p.Size).label ?? "小"}");
            list.SelectedIndex = System.Math.Min(sel, pets.Count - 1);
            loading = false;
            add.IsEnabled = pets.Count < Settings.MaxPets;
            remove.IsEnabled = pets.Count > 1;
        }

        void LoadCurrent()
        {
            if (loading || Current == null) return;
            loading = true;
            var b = Breeds.Get(Current.Breed);
            name.Text = Current.Name;
            breed.SelectedItem = b;
            coat.ItemsSource = b.CoatNames;
            coat.SelectedItem = Current.Coat;
            size.SelectedIndex = System.Math.Max(0, System.Array.FindIndex(Sizes, x => x.px == Current.Size));
            loading = false;
        }
    }
}
