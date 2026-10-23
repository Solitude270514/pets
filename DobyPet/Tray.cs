using System;
using System.Drawing;
using WF = System.Windows.Forms;

namespace DobyPet
{
    /// <summary>右下角系統匣圖示與選單。</summary>
    public sealed class Tray : IDisposable
    {
        readonly PetHost host;
        readonly Settings cfg;
        readonly SpriteBank bank;
        readonly WF.NotifyIcon icon;
        readonly WF.ContextMenuStrip menu = new();
        readonly WF.ToolStripMenuItem header, friend, visit, dnd, update;
        readonly Updater updater = new();
        readonly System.Windows.Threading.DispatcherTimer updateTimer = new();
        string notifiedTag;
        bool installing;
        Icon currentIcon;

        public Tray(PetHost host, Settings cfg, SpriteBank bank, bool justUpdated = false, bool updateNow = false)
        {
            this.host = host;
            this.cfg = cfg;
            this.bank = bank;

            header = new WF.ToolStripMenuItem { Enabled = false };
            menu.Items.Add(header);
            menu.Items.Add(new WF.ToolStripSeparator());
            menu.Items.Add("🦴 餵零食", null, (_, __) => host.Feed());
            menu.Items.Add("👋 叫牠過來…", null, (_, __) => host.CallOver());

            var tricks = new WF.ToolStripMenuItem("🎪 表演");
            tricks.DropDownItems.Add("後空翻", null, (_, __) => host.Pet.DoBackflip());
            tricks.DropDownItems.Add("放臭屁", null, (_, __) => host.Pet.DoFart());
            tricks.DropDownItems.Add("坐下", null, (_, __) => host.Pet.DoSit());
            tricks.DropDownItems.Add("睡覺", null, (_, __) => host.Pet.DoSleep());
            tricks.DropDownItems.Add("伸懶腰", null, (_, __) => host.Pet.DoStretch());
            tricks.DropDownItems.Add(new WF.ToolStripSeparator());
            tricks.DropDownItems.Add("從螢幕邊偷看", null, (_, __) => host.Pet.DoPeek());
            tricks.DropDownItems.Add("叼拖鞋", null, (_, __) => host.Pet.DoSlipper());
            menu.Items.Add(tricks);

            menu.Items.Add(new WF.ToolStripSeparator());
            friend = new WF.ToolStripMenuItem { Enabled = false };
            menu.Items.Add(friend);
            visit = new WF.ToolStripMenuItem("🏠 去找朋友玩", null, (_, __) => host.StartVisit());
            menu.Items.Add(visit);

            menu.Items.Add(new WF.ToolStripSeparator());
            dnd = new WF.ToolStripMenuItem("🌙 勿擾模式（讓牠睡覺）") { CheckOnClick = true, Checked = cfg.DoNotDisturb };
            dnd.CheckedChanged += (_, __) =>
            {
                cfg.DoNotDisturb = dnd.Checked;
                host.Pet.DoNotDisturb = dnd.Checked;
                cfg.Save();
            };
            menu.Items.Add(dnd);
            menu.Items.Add("⚙ 設定…", null, (_, __) => OpenSettings());
            update = new WF.ToolStripMenuItem("⬆ 更新", null, (_, __) => InstallUpdate()) { Visible = false };
            menu.Items.Add(update);
            menu.Items.Add("🔄 檢查更新", null, async (_, __) => await CheckUpdate(manual: true));
            menu.Items.Add("結束", null, (_, __) => Exit());
            menu.Opening += (_, __) => RefreshFriend();

            icon = new WF.NotifyIcon { ContextMenuStrip = menu, Visible = true };
            icon.DoubleClick += (_, __) => host.CallOver();
            host.ContextMenuRequested += () => menu.Show(WF.Cursor.Position);
            host.Notify += Balloon;
            host.StatusChanged += RefreshFriend;
            Refresh();

            if (justUpdated) Balloon($"已更新到 v{Updater.CurrentText} 🎉");

            // 啟動 30 秒後檢查一次，之後每 6 小時；開了自動更新的話，每分鐘看看主人離開了沒
            updateTimer.Interval = TimeSpan.FromSeconds(updateNow ? 2 : 30);
            updateTimer.Tick += (_, __) => UpdateTick(updateNow);
            updateTimer.Start();
        }

        void Balloon(string text) => icon.ShowBalloonTip(4000, cfg.Name, text, WF.ToolTipIcon.None);

        // ── 自動更新 ─────────────────────────────

        double sinceCheck = double.MaxValue;

        async void UpdateTick(bool installImmediately)
        {
            updateTimer.Interval = TimeSpan.FromMinutes(1);
            sinceCheck += 1;
            if (sinceCheck >= 360 || installImmediately)
            {
                sinceCheck = 0;
                await CheckUpdate(manual: false);
            }
            if (updater.Available != null && (installImmediately || (cfg.AutoUpdate && Native.IdleSeconds() > 180)))
                InstallUpdate();
        }

        async System.Threading.Tasks.Task CheckUpdate(bool manual)
        {
            if (!Updater.CanSelfUpdate)
            {
                if (manual) Balloon("開發版不會自我更新，請用打包好的 DobyPet.exe");
                return;
            }
            try
            {
                var rel = await updater.CheckAsync();
                if (rel != null)
                {
                    update.Text = $"⬆ 更新到 {rel.Tag}";
                    update.Visible = true;
                    if (manual || notifiedTag != rel.Tag)
                    {
                        notifiedTag = rel.Tag;
                        Balloon(cfg.AutoUpdate
                            ? $"有新版本 {rel.Tag}，會在你離開電腦時自動更新"
                            : $"有新版本 {rel.Tag}！右鍵選單按「⬆ 更新」就能安裝");
                    }
                }
                else if (manual) Balloon($"已經是最新版 v{Updater.CurrentText}");
            }
            catch (Exception ex)
            {
                if (manual) Balloon("檢查更新失敗：" + ex.Message);
            }
        }

        async void InstallUpdate()
        {
            var rel = updater.Available;
            if (rel == null || installing) return;
            installing = true;
            update.Enabled = false;
            update.Text = $"⬇ 正在下載 {rel.Tag}…";
            try
            {
                await updater.InstallAsync(rel);   // 成功的話新版已經在啟動了
                host.Shutdown();
                System.Windows.Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                installing = false;
                update.Enabled = true;
                update.Text = $"⬆ 更新到 {rel.Tag}";
                Balloon("更新失敗：" + ex.Message);
            }
        }

        void RefreshFriend()
        {
            friend.Text = "🐶 " + host.FriendStatus;
            visit.Enabled = host.CanVisit;
        }

        /// <summary>名字或毛色改變後，更新選單標題與圖示。</summary>
        void Refresh()
        {
            header.Text = $"🐾 {cfg.Name}　v{Updater.CurrentText}";
            icon.Text = $"{cfg.Name}（小杜賓）";
            var old = currentIcon;
            currentIcon = MakeIcon();
            icon.Icon = currentIcon;
            old?.Dispose();
            RefreshFriend();
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
            string oldCode = cfg.FriendCode;
            var w = new SettingsWindow(cfg);
            if (w.ShowDialog() != true) return;
            bank.Coat = cfg.Coat;
            cfg.Save();
            host.ApplySettings();
            if (FriendLink.NormalizeCode(oldCode) != FriendLink.NormalizeCode(cfg.FriendCode)) host.RestartLink();
            Refresh();
        }

        void Exit()
        {
            host.Shutdown();
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
