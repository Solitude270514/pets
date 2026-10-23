using System;
using System.Drawing;
using WF = System.Windows.Forms;

namespace DobyPet
{
    /// <summary>右下角系統匣圖示與選單。</summary>
    public sealed class Tray : IDisposable
    {
        readonly PetWindow win;
        readonly Settings cfg;
        readonly SpriteBank bank;
        readonly WF.NotifyIcon icon;
        readonly WF.ContextMenuStrip menu = new();
        readonly WF.ToolStripMenuItem header, dnd;
        Icon currentIcon;

        public Tray(PetWindow win, Settings cfg, SpriteBank bank)
        {
            this.win = win;
            this.cfg = cfg;
            this.bank = bank;

            header = new WF.ToolStripMenuItem { Enabled = false };
            menu.Items.Add(header);
            menu.Items.Add(new WF.ToolStripSeparator());
            menu.Items.Add("🦴 餵零食", null, (_, __) => win.Feed());
            menu.Items.Add("👋 叫牠過來…", null, (_, __) => win.CallOver());

            var tricks = new WF.ToolStripMenuItem("🎪 表演");
            tricks.DropDownItems.Add("後空翻", null, (_, __) => win.Pet.DoBackflip());
            tricks.DropDownItems.Add("放臭屁", null, (_, __) => win.Pet.DoFart());
            tricks.DropDownItems.Add("坐下", null, (_, __) => win.Pet.DoSit());
            tricks.DropDownItems.Add("睡覺", null, (_, __) => win.Pet.DoSleep());
            tricks.DropDownItems.Add("伸懶腰", null, (_, __) => win.Pet.DoStretch());
            menu.Items.Add(tricks);

            dnd = new WF.ToolStripMenuItem("🌙 勿擾模式（讓牠睡覺）") { CheckOnClick = true, Checked = cfg.DoNotDisturb };
            dnd.CheckedChanged += (_, __) =>
            {
                cfg.DoNotDisturb = dnd.Checked;
                win.Pet.DoNotDisturb = dnd.Checked;
                cfg.Save();
            };
            menu.Items.Add(dnd);
            menu.Items.Add(new WF.ToolStripSeparator());
            menu.Items.Add("⚙ 設定…", null, (_, __) => OpenSettings());
            menu.Items.Add("結束", null, (_, __) => Exit());

            icon = new WF.NotifyIcon { ContextMenuStrip = menu, Visible = true };
            icon.DoubleClick += (_, __) => win.CallOver();
            win.ContextMenuRequested += () => menu.Show(WF.Cursor.Position);
            Refresh();
        }

        /// <summary>名字或毛色改變後，更新選單標題與圖示。</summary>
        void Refresh()
        {
            header.Text = $"🐾 {cfg.Name}";
            icon.Text = $"{cfg.Name}（小杜賓）";
            var old = currentIcon;
            currentIcon = MakeIcon();
            icon.Icon = currentIcon;
            old?.Dispose();
        }

        /// <summary>用站姿的頭部做 32×32 圖示。</summary>
        Icon MakeIcon()
        {
            var img = bank.Stand(0);
            const int crop = 22;   // 頭部大約在左上 22×22
            using var bmp = new Bitmap(32, 32);
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                {
                    int sx = x * crop / 32, sy = y * crop / 32 + 1;
                    uint c = sy < img.H && sx < img.W ? img.Px[sy * img.W + sx] : 0;
                    bmp.SetPixel(x, y, Color.FromArgb(unchecked((int)c)));
                }
            return Icon.FromHandle(bmp.GetHicon());
        }

        void OpenSettings()
        {
            var w = new SettingsWindow(cfg);
            if (w.ShowDialog() != true) return;
            bank.Coat = cfg.Coat;
            win.ApplyScale();
            win.SetName(cfg.Name);
            cfg.Save();
            Refresh();
        }

        void Exit()
        {
            win.Shutdown();
            System.Windows.Application.Current.Shutdown();
        }

        public void Dispose()
        {
            icon.Visible = false;
            icon.Dispose();
            menu.Dispose();
            currentIcon?.Dispose();
        }
    }
}
