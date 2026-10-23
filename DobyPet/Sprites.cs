using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;

namespace DobyPet
{
    /// <summary>一張已上色的像素圖，Px 為 0xAARRGGBB，0 代表透明。</summary>
    public sealed class PixImage
    {
        public readonly int W, H;
        public readonly uint[] Px;
        public PixImage(int w, int h) { W = w; H = h; Px = new uint[w * h]; }
    }

    /// <summary>sprites.json 裡的一格：字母格 + 水平對齊點。</summary>
    public sealed class Frame
    {
        public string[] Rows { get; set; }
        public double Ax { get; set; }
    }

    public static class Palettes
    {
        public static readonly string[] CoatNames = { "黑棕", "紅棕", "藍棕", "淺黃" };

        static uint C(string hex) => 0xFF000000u | Convert.ToUInt32(hex.TrimStart('#'), 16);

        static Dictionary<char, uint> Coat(string k, string s, string b, string h, string e, string d, string t) => new()
        {
            ['K'] = C(k), ['S'] = C(s), ['B'] = C(b), ['H'] = C(h), ['E'] = C(e), ['D'] = C(d), ['T'] = C(t),
        };

        public static readonly Dictionary<string, Dictionary<char, uint>> Coats = new()
        {
            ["黑棕"] = Coat("#0b0b0b", "#1c1d1d", "#2e2f2f", "#4a4b4a", "#6e4420", "#9a5c2c", "#bd7840"),
            ["紅棕"] = Coat("#2a120c", "#4a2216", "#6b3322", "#8c4c33", "#7a4a22", "#b0703a", "#dc9c5e"),
            ["藍棕"] = Coat("#151a22", "#38414e", "#4f5b6b", "#6f7c8e", "#7a4e2a", "#a8693a", "#d39258"),
            ["淺黃"] = Coat("#4a3a30", "#8a7262", "#a68c78", "#c4ab94", "#9a6c44", "#c4925e", "#e6be8e"),
        };

        /// <summary>特效與道具用色（不隨毛色改變）。</summary>
        public static readonly Dictionary<char, uint> Fx = new()
        {
            ['K'] = C("#0b0b0b"), ['W'] = C("#d6d6d6"), ['P'] = C("#d46a7e"), ['U'] = C("#4a90d9"), ['u'] = C("#2c5f9e"),
            ['R'] = C("#ff5c7a"), ['r'] = C("#c93a58"), ['w'] = C("#ffffff"), ['F'] = C("#b9e07e"), ['f'] = C("#86bd4c"),
            ['Y'] = C("#ffd84d"), ['Z'] = C("#5f84b3"), ['p'] = C("#7a4b24"), ['q'] = C("#a8703a"), ['o'] = C("#8a5a2c"),
            ['x'] = C("#2b2b2b"),
        };
    }

    public static class FxArt
    {
        public static readonly string[] Heart = { ".RR..RR.", "RwRRRRRR", "RRRRRRRR", "RRRRRRRR", ".RRRRRr.", "..RRRr..", "...Rr..." };
        public static readonly string[] PuffS = { "..FF..", ".FFfF.", "FFFFFf", "FfFFFF", ".FFfF.", "..FF.." };
        public static readonly string[] PuffL = { "...FFFF...", ".FFFFfFFF.", "FFfFFFFFfF", "FFFFFFFFFF", "FFFFfFFFFF", ".FFfFFFFF.", "...FFFF..." };
        public static readonly string[] Spark = { "...Y...", "...Y...", ".YYYYY.", "YYYwYYY", ".YYYYY.", "...Y...", "...Y..." };
        public static readonly string[] Zz = { "ZZZZZ", "...Z.", "..Z..", ".Z...", "ZZZZZ" };
        public static readonly string[] Poop = { "....KK.....", "...KqpK....", "..KKKKKK...", "..KpqppK...", ".KKKKKKKK..", ".KpppqppK..", "KKKKKKKKKK.", "KpqpppqppK.", "KKKKKKKKKK." };
        public static readonly string[] Bowl = { "..oooooo..", ".oooooooo.", "KUUUUUUUUK", ".KUwUUUUK.", "..KKKKKK.." };
        public static readonly string[] Slipper = { ".KKKK.....", "KPPPPKKKK.", "KPwwPPPPPK", "KKKKKKKKKK" };
    }

    /// <summary>載入內嵌的 sprites.json，依目前毛色上色並快取。</summary>
    public sealed class SpriteBank
    {
        readonly Dictionary<string, List<Frame>> data;
        readonly Dictionary<string, PixImage> cache = new();
        string coat = "黑棕";

        public SpriteBank()
        {
            using var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("DobyPet.sprites.json");
            data = JsonSerializer.Deserialize<Dictionary<string, List<Frame>>>(s, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public string Coat
        {
            get => coat;
            set { if (coat != value && Palettes.Coats.ContainsKey(value)) { coat = value; cache.Clear(); } }
        }

        public Frame Info(string pose, int i = 0) => data[pose][i];

        public PixImage Pose(string pose, int i = 0) => Get($"{pose}{i}", data[pose][i].Rows, null);

        /// <summary>站姿，可加搖尾巴（-1~1）、閉眼、瞇眼。</summary>
        public PixImage Stand(int wag, bool blink = false, bool half = false) =>
            Get($"stand{wag}{blink}{half}", data["stand"][0].Rows, g =>
            {
                Wag(g, wag);
                if (blink) CloseEyes(g);
                if (half) HalfEyes(g);
            });

        public PixImage Fx(string key, string[] rows) => Get("fx" + key, rows, null);

        PixImage Get(string key, string[] rows, Action<char[][]> mod)
        {
            if (cache.TryGetValue(key, out var img)) return img;
            var g = new char[rows.Length][];
            for (int y = 0; y < rows.Length; y++) g[y] = rows[y].ToCharArray();
            mod?.Invoke(g);
            img = Render(g);
            cache[key] = img;
            return img;
        }

        PixImage Render(char[][] g)
        {
            var pal = Palettes.Coats[coat];
            var img = new PixImage(g[0].Length, g.Length);
            for (int y = 0; y < img.H; y++)
                for (int x = 0; x < img.W; x++)
                {
                    char ch = g[y][x];
                    if (ch == '.') continue;
                    if (pal.TryGetValue(ch, out var c) || Palettes.Fx.TryGetValue(ch, out c))
                        img.Px[y * img.W + x] = c;
                }
            return img;
        }

        // ── 站姿局部變化（座標對應 stand 圖） ──────────────

        static void Shift(char[][] g, int r0, int r1, int c0, int c1, int dx, int dy)
        {
            var pts = new List<(int y, int x, char c)>();
            for (int y = r0; y <= r1; y++)
                for (int x = c0; x <= c1; x++)
                    if (g[y][x] != '.') { pts.Add((y, x, g[y][x])); g[y][x] = '.'; }
            foreach (var (y, x, c) in pts)
            {
                int ny = y + dy, nx = x + dx;
                if (ny >= 0 && ny < g.Length && nx >= 0 && nx < g[0].Length) g[ny][nx] = c;
            }
        }

        static void Wag(char[][] g, int d)
        {
            if (d == 0) return;
            Shift(g, 21, 22, 33, 40, d, 0);
            Shift(g, 23, 23, 33, 40, Math.Abs(d) > 1 ? Math.Sign(d) : 0, 0);
        }

        static void CloseEyes(char[][] g)
        {
            foreach (var x in new[] { 2, 3, 11, 12 }) { g[13][x] = 'B'; g[14][x] = 'K'; }
        }

        static void HalfEyes(char[][] g)
        {
            foreach (var x in new[] { 2, 3, 11, 12 }) g[13][x] = 'K';
        }
    }
}
