using System;
using System.Collections.Generic;
using System.Linq;
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

    /// <summary>姿勢裡的一格：字母格 + 水平對齊點。</summary>
    public sealed class Frame
    {
        public string[] Rows { get; set; }
        public double Ax { get; set; }
    }

    /// <summary>
    /// 一個品種：姿勢圖、毛色調色盤，以及站姿上的特徵位置（眨眼、搖尾巴、嘴巴、屁股），
    /// 全部來自 sprites/&lt;品種&gt;/meta.json，經 tools/build_js.py 合併成內嵌的 breeds.json。
    /// </summary>
    public sealed class Breed
    {
        public string Id, Name, DefaultCoat;
        public string[] CoatNames;
        public Dictionary<string, Dictionary<char, uint>> Coats;
        public Dictionary<string, List<Frame>> Poses;
        public (int y, int x, char c)[] Blink, Half;
        public (int r0, int r1, int c0, int c1, double amp)[] Tail;
        public int MouthX, MouthY, RearX, RearY, BowlX, PoopRear, Peek;
        public int[] Icon;

        public int StandW => Poses["stand"][0].Rows[0].Length;
        public int StandH => Poses["stand"][0].Rows.Length;
    }

    public static class Breeds
    {
        public const string Default = "doberman";

        public static readonly Dictionary<string, Breed> All = Load();

        public static Breed Get(string id) => id != null && All.TryGetValue(id, out var b) ? b : All[Default];

        static uint Hex(string hex) => 0xFF000000u | Convert.ToUInt32(hex.TrimStart('#'), 16);

        static Dictionary<string, Breed> Load()
        {
            using var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("DobyPet.breeds.json");
            using var doc = JsonDocument.Parse(s);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var all = new Dictionary<string, Breed>();
            foreach (var bp in doc.RootElement.EnumerateObject())
            {
                var m = bp.Value.GetProperty("meta");
                int I(string name, int def) => m.TryGetProperty(name, out var v) ? v.GetInt32() : def;
                (int, int, char)[] Pixels(string name) => m.TryGetProperty(name, out var arr)
                    ? arr.EnumerateArray().Select(p => (p[0].GetInt32(), p[1].GetInt32(), p[2].GetString()[0])).ToArray()
                    : Array.Empty<(int, int, char)>();

                var b = new Breed
                {
                    Id = bp.Name,
                    Name = m.GetProperty("name").GetString(),
                    Poses = JsonSerializer.Deserialize<Dictionary<string, List<Frame>>>(bp.Value.GetProperty("poses").GetRawText(), opts),
                    Coats = new(),
                    Blink = Pixels("blink"),
                    Half = Pixels("half"),
                };
                foreach (var c in m.GetProperty("coats").EnumerateObject())
                    b.Coats[c.Name] = c.Value.EnumerateObject().ToDictionary(e => e.Name[0], e => Hex(e.Value.GetString()));
                b.CoatNames = b.Coats.Keys.ToArray();
                b.DefaultCoat = m.TryGetProperty("defaultCoat", out var dc) ? dc.GetString() : b.CoatNames[0];
                b.Tail = m.TryGetProperty("tail", out var tail)
                    ? tail.EnumerateArray().Select(t => (t.GetProperty("rows")[0].GetInt32(), t.GetProperty("rows")[1].GetInt32(),
                                                         t.GetProperty("cols")[0].GetInt32(), t.GetProperty("cols")[1].GetInt32(),
                                                         t.GetProperty("amp").GetDouble())).ToArray()
                    : Array.Empty<(int, int, int, int, double)>();

                int w = b.StandW, h = b.StandH;
                var mouth = m.TryGetProperty("mouth", out var mo) ? mo : default;
                var rear = m.TryGetProperty("rear", out var re) ? re : default;
                b.MouthX = mouth.ValueKind == JsonValueKind.Array ? mouth[0].GetInt32() : w / 8;
                b.MouthY = mouth.ValueKind == JsonValueKind.Array ? mouth[1].GetInt32() : h * 3 / 8;
                b.RearX = rear.ValueKind == JsonValueKind.Array ? rear[0].GetInt32() : w * 7 / 8;
                b.RearY = rear.ValueKind == JsonValueKind.Array ? rear[1].GetInt32() : h / 2;
                b.BowlX = I("bowlX", 8);
                b.PoopRear = I("poopRear", b.Poses["poop"][0].Rows[0].Length / 2);
                b.Peek = I("peek", (int)(w * .42));
                b.Icon = m.TryGetProperty("icon", out var ic) ? ic.EnumerateArray().Select(v => v.GetInt32()).ToArray() : new[] { 0, 0, Math.Min(w, h) / 2 };
                all[b.Id] = b;
            }
            return all;
        }
    }

    /// <summary>特效與道具用色（不隨品種、毛色改變）。</summary>
    public static class Palettes
    {
        static uint C(string hex) => 0xFF000000u | Convert.ToUInt32(hex.TrimStart('#'), 16);

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

        static readonly Dictionary<string, PixImage> cache = new();

        /// <summary>特效小圖只用特效調色盤上色，不會跟品種的顏色代號撞到。</summary>
        public static PixImage Get(string key, string[] rows)
        {
            if (cache.TryGetValue(key, out var img)) return img;
            img = new PixImage(rows[0].Length, rows.Length);
            for (int y = 0; y < img.H; y++)
                for (int x = 0; x < img.W; x++)
                    if (Palettes.Fx.TryGetValue(rows[y][x], out var c)) img.Px[y * img.W + x] = c;
            return cache[key] = img;
        }
    }

    /// <summary>某一隻狗的圖庫：依品種與毛色上色並快取。</summary>
    public sealed class SpriteBank
    {
        readonly Dictionary<string, PixImage> cache = new();
        string coat;

        public Breed Breed { get; }

        public SpriteBank(Breed breed, string coat = null)
        {
            Breed = breed;
            this.coat = coat != null && breed.Coats.ContainsKey(coat) ? coat : breed.DefaultCoat;
        }

        public string Coat
        {
            get => coat;
            set { if (coat != value && Breed.Coats.ContainsKey(value)) { coat = value; cache.Clear(); } }
        }

        public Frame Info(string pose, int i = 0) => Breed.Poses[pose][i];

        public int FrameCount(string pose) => Breed.Poses[pose].Count;

        public PixImage Pose(string pose, int i = 0) => Get($"{pose}{i}", Breed.Poses[pose][i].Rows, null);

        /// <summary>站姿，可加搖尾巴（-1~1）、閉眼、瞇眼。</summary>
        public PixImage Stand(int wag, bool blink = false, bool half = false) =>
            Get($"stand{wag}{blink}{half}", Breed.Poses["stand"][0].Rows, g =>
            {
                Wag(g, wag);
                if (blink) foreach (var (y, x, c) in Breed.Blink) Set(g, y, x, c);
                if (half) foreach (var (y, x, c) in Breed.Half) Set(g, y, x, c);
            });

        PixImage Get(string key, string[] rows, Action<char[][]> mod)
        {
            if (cache.TryGetValue(key, out var img)) return img;
            var g = new char[rows.Length][];
            for (int y = 0; y < rows.Length; y++) g[y] = rows[y].ToCharArray();
            mod?.Invoke(g);
            var pal = Breed.Coats[coat];
            img = new PixImage(g[0].Length, g.Length);
            for (int y = 0; y < img.H; y++)
                for (int x = 0; x < img.W; x++)
                {
                    char ch = g[y][x];
                    if (ch == '.') continue;
                    if (pal.TryGetValue(ch, out var c) || Palettes.Fx.TryGetValue(ch, out c))   // 眼睛反光、舌頭等共用色
                        img.Px[y * img.W + x] = c;
                }
            return cache[key] = img;
        }

        static void Set(char[][] g, int y, int x, char c)
        {
            if (y >= 0 && y < g.Length && x >= 0 && x < g[y].Length) g[y][x] = c;
        }

        static void Shift(char[][] g, int r0, int r1, int c0, int c1, int dx)
        {
            if (dx == 0) return;
            var pts = new List<(int y, int x, char c)>();
            for (int y = r0; y <= r1 && y < g.Length; y++)
                for (int x = c0; x <= c1 && x < g[y].Length; x++)
                    if (g[y][x] != '.') { pts.Add((y, x, g[y][x])); g[y][x] = '.'; }
            foreach (var (y, x, c) in pts) Set(g, y, x + dx, c);
        }

        void Wag(char[][] g, int d)
        {
            if (d == 0) return;
            foreach (var (r0, r1, c0, c1, amp) in Breed.Tail) Shift(g, r0, r1, c0, c1, (int)(d * amp));
        }
    }
}
