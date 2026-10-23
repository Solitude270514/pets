using System;

namespace DobyPet
{
    /// <summary>小型軟體繪圖畫布：所有東西先畫在低解析度像素格上，再整張放大顯示，保持像素風。</summary>
    public sealed class Stage
    {
        public readonly int W, H;
        public readonly uint[] Px;

        public Stage(int w, int h) { W = w; H = h; Px = new uint[w * h]; }

        public void Clear() => Array.Clear(Px);

        public void Set(int x, int y, uint c)
        {
            if (x >= 0 && x < W && y >= 0 && y < H) Px[y * W + x] = c;
        }

        public void Blit(PixImage img, double x, double y)
        {
            int ox = (int)Math.Round(x), oy = (int)Math.Round(y);
            for (int j = 0; j < img.H; j++)
            {
                int ty = oy + j;
                if (ty < 0 || ty >= H) continue;
                for (int i = 0; i < img.W; i++)
                {
                    int tx = ox + i;
                    uint c = img.Px[j * img.W + i];
                    if (c != 0 && tx >= 0 && tx < W) Px[ty * W + tx] = c;
                }
            }
        }

        /// <summary>
        /// 以腳底為基準畫圖：(fx, fy) 是腳底位置，ax 是水平對齊點（預設圖寬一半）。
        /// 支援左右翻轉、縮放（壓扁/拉長）與繞身體中心旋轉（後空翻）。
        /// </summary>
        public void Draw(PixImage img, double fx, double fy, bool flip = false,
                         double sx = 1, double sy = 1, double rot = 0, double? ax = null)
        {
            double a = ax ?? img.W / 2.0;
            int ox = (int)Math.Round(fx), oy = (int)Math.Round(fy);
            double fsx = sx * (flip ? -1 : 1);
            double piv = -img.H / 2.0 * sy;
            double cos = Math.Cos(rot), sin = Math.Sin(rot);

            // 先算四個角落轉換後的範圍，只掃描這個範圍
            double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
            foreach (var (u, v) in new[] { (0.0, 0.0), (img.W, 0.0), (0.0, img.H), (img.W, img.H) })
            {
                double lx = (u - a) * fsx, ly = (v - img.H) * sy - piv;
                double qx = lx * cos - ly * sin + ox, qy = lx * sin + ly * cos + piv + oy;
                minX = Math.Min(minX, qx); maxX = Math.Max(maxX, qx);
                minY = Math.Min(minY, qy); maxY = Math.Max(maxY, qy);
            }
            int x0 = Math.Max(0, (int)Math.Floor(minX)), x1 = Math.Min(W - 1, (int)Math.Ceiling(maxX));
            int y0 = Math.Max(0, (int)Math.Floor(minY)), y1 = Math.Min(H - 1, (int)Math.Ceiling(maxY));

            // 反向對應：每個目標像素回推到原圖哪一格（最近鄰，不會糊）
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    double dx = x + 0.5 - ox, dy = y + 0.5 - oy - piv;
                    double lx = dx * cos + dy * sin;
                    double ly = -dx * sin + dy * cos + piv;
                    int u = (int)Math.Floor(lx / fsx + a);
                    int v = (int)Math.Floor(ly / sy + img.H);
                    if (u < 0 || u >= img.W || v < 0 || v >= img.H) continue;
                    uint c = img.Px[v * img.W + u];
                    if (c != 0) Px[y * W + x] = c;
                }
        }
    }
}
