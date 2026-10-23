"""把 Gemini 生成的像素狗圖（洋紅 #FF00FF 背景）轉成調色盤字母格。

可以當指令用，也可以被 import_breed.py 匯入使用。

指令用法（在 sprites/<品種>/ 資料夾下執行，產生 <名稱>.json 字母格與 <名稱>.png 放大檢查圖）：
    cd sprites/doberman
    python ../../tools/extract.py ../../art/doberman/sit.jpg sit
    python ../../tools/extract.py ../../art/doberman/walk_sheet.jpg walk --rows2       # 一張圖多格，全部存成同一個姿勢
    python ../../tools/extract.py sheet.jpg x --sheet stand,sit,sleep,eat,poop,stretch  # 動作表：依閱讀順序切成多個姿勢
    python ../../tools/extract.py sit.jpg sit --palette palette.json                    # 用指定調色盤（新品種）

轉完記得跑 python tools/build_js.py 把資料合併進預覽頁與桌面程式。
"""
import sys, json
import numpy as np
from PIL import Image
from collections import deque

# ── 讀圖：偵測像素格大小，取每一格的代表色 ─────────────────

def _period(sig, lo=5, hi=26):
    s = sig - sig.mean(); n = np.arange(len(s)); best = (0, 0)
    for p in np.arange(lo, hi, 0.02):
        c = abs((s * np.exp(-2j*np.pi*n/p)).sum()) * (p ** 0.5)
        if c > best[0]: best = (c, p)
    return best[1]

def _phase(sig, p):
    s = sig - sig.mean(); n = np.arange(len(s))
    return (-np.angle((s*np.exp(-2j*np.pi*n/p)).sum())/(2*np.pi)*p) % p

def is_bg(px):
    r, g, b = px
    if r > 170 and b > 170 and g < 120: return True
    return b > g+40 and r > g+40 and abs(r-b) < 60   # 背景與輪廓混色的邊緣

def load_cells(path, with_origin=False):
    """回傳 (cells[H,W,3], 前景遮罩[H,W], 格子大小)；with_origin=True 時多回傳格線起點 (px, py)，用來換算回原圖座標。"""
    im = np.asarray(Image.open(path).convert('RGB')).astype(float)
    g = im.mean(axis=2)
    dx = np.abs(np.diff(g, axis=1)).mean(axis=0); dy = np.abs(np.diff(g, axis=0)).mean(axis=1)
    p = (_period(dx) + _period(dy)) / 2
    px, py = _phase(dx, p), _phase(dy, p)
    W = int((im.shape[1]-px)//p); H = int((im.shape[0]-py)//p)
    cells = np.zeros((H, W, 3))
    m = max(1, int(p*0.25))
    for j in range(H):
        for i in range(W):
            x0 = px+i*p; y0 = py+j*p
            blk = im[int(y0)+m:int(y0+p)-m, int(x0)+m:int(x0+p)-m]
            cells[j, i] = np.median(blk.reshape(-1, 3), axis=0)
    fg = np.array([[not is_bg(cells[j, i]) for i in range(W)] for j in range(H)])
    return (cells, fg, p, (px, py)) if with_origin else (cells, fg, p)

# ── 切出每一隻狗 ───────────────────────────────

def _label(mask):
    H, W = mask.shape; lab = np.zeros((H, W), int); n = 0
    for y in range(H):
        for x in range(W):
            if mask[y, x] and not lab[y, x]:
                n += 1; q = deque([(y, x)]); lab[y, x] = n
                while q:
                    cy, cx = q.popleft()
                    for dy in (-1, 0, 1):
                        for dx in (-1, 0, 1):
                            ny, nx = cy+dy, cx+dx
                            if 0 <= ny < H and 0 <= nx < W and mask[ny, nx] and not lab[ny, nx]:
                                lab[ny, nx] = n; q.append((ny, nx))
    return lab, n

def _dilate(mask, r):
    out = mask.copy()
    H, W = mask.shape
    for dy in range(-r, r+1):
        for dx in range(-r, r+1):
            sh = np.zeros_like(mask)
            sh[max(0, dy):H+min(0, dy), max(0, dx):W+min(0, dx)] = mask[max(0, -dy):H+min(0, -dy), max(0, -dx):W+min(0, -dx)]
            out |= sh
    return out

def find_sprites(fg, gap=1, min_px=30):
    """把前景切成一塊塊（相距 gap 格以內的視為同一隻（太大會把排與排之間的狗連起來），例如吃飯時的狗碗），依閱讀順序（由上而下、由左而右）排好。
    回傳 [(y0, x0, mask), ...]。"""
    if gap:
        # 先濾掉零散的小雜點（JPEG 壓縮殘影），不然擴張時會把兩隻相距很遠的狗串在一起
        lab0, n0 = _label(fg)
        sizes = np.bincount(lab0.ravel(), minlength=n0 + 1)
        fg = fg & (sizes[lab0] >= 8)
    lab, n = _label(_dilate(fg, gap) if gap else fg)
    blobs = []
    for k in range(1, n+1):
        m = (lab == k) & fg
        if m.sum() < min_px: continue
        ys, xs = np.where(m)
        y0, y1, x0, x1 = ys.min(), ys.max()+1, xs.min(), xs.max()+1
        blobs.append((y0, x0, m[y0:y1, x0:x1]))
    # 分排：中心點落在目前這一排的上下範圍內就算同一排
    blobs.sort(key=lambda b: b[0] + b[2].shape[0] / 2)
    rows = []
    for b in blobs:
        cy = b[0] + b[2].shape[0] / 2
        if rows and rows[-1]['top'] <= cy <= rows[-1]['bottom']:
            rows[-1]['items'].append(b)
            rows[-1]['bottom'] = max(rows[-1]['bottom'], b[0] + b[2].shape[0])
        else:
            rows.append({'top': b[0], 'bottom': b[0] + b[2].shape[0], 'items': [b]})
    return [b for r in rows for b in sorted(r['items'], key=lambda b: b[1])]

# ── 上色分類 ──────────────────────────────────

def doberman_letter(px):
    """杜賓用的固定分類：中性色依亮度分 K/S/B/H，暖色分 E/D/T。"""
    r, g, b = px; L = (r+g+b)/3
    if b > r+25 and b > g+15:              # 藍色狗碗
        return 'U' if L > 90 else 'u'
    if r > g+20 and b < g+30:
        return 'E' if r < 120 else ('D' if r < 165 else 'T')
    if r > g+30 and b > g+20: return 'P'   # 舌頭/嘴巴
    if L < 12: return 'K'
    if L < 38: return 'S'
    if L < 58: return 'B'
    if L < 95: return 'H'
    return 'W'

DOBERMAN_PREVIEW = {'K':(11,11,11),'S':(28,29,29),'B':(46,47,47),'H':(74,75,74),'W':(214,214,214),'E':(110,68,32),
                    'D':(154,92,44),'T':(189,120,64),'P':(200,90,110),'U':(80,140,220),'u':(40,80,160)}

def hex_rgb(h): h = h.lstrip('#'); return tuple(int(h[i:i+2], 16) for i in (0, 2, 4))

def palette_letter(palette):
    """用指定調色盤 {字母: '#色碼'} 做最近色分類。"""
    keys = list(palette.keys()); cols = np.array([hex_rgb(palette[k]) for k in keys], float)
    def f(px):
        return keys[int(((cols - np.asarray(px)) ** 2).sum(1).argmin())]
    return f

PALETTE_SYMBOLS = '0123456789abcdefgh'

def auto_palette(pixel_arrays, k=12, seed=0):
    """從前景像素用 k-means 找出 k 個主要顏色，依亮度排序後編成 '0','1',…,'9','a','b'（0 最暗）。"""
    px = np.concatenate(pixel_arrays).astype(float)
    rng = np.random.default_rng(seed)
    # k-means++ 初始化
    cent = [px[rng.integers(len(px))]]
    for _ in range(k - 1):
        d = np.min(((px[:, None] - np.array(cent)[None]) ** 2).sum(-1), axis=1)
        cent.append(px[rng.choice(len(px), p=d / d.sum())])
    cent = np.array(cent)

    def fit(cent):
        for _ in range(40):
            lab = ((px[:, None] - cent[None]) ** 2).sum(-1).argmin(1)
            cent = np.array([px[lab == i].mean(0) if (lab == i).any() else cent[i] for i in range(len(cent))])
        return cent

    cent = fit(cent)
    # 只佔幾個像素、但跟所有主色都差很多的顏色（例如眼睛反光、舌頭）另外保留，不要被大片顏色吃掉
    for _ in range(len(PALETTE_SYMBOLS) - k):
        dist = np.sqrt(((px[:, None] - cent[None]) ** 2).sum(-1).min(1))
        far = px[dist > 55]
        if len(far) < 3: break
        cent = fit(np.vstack([cent, far.mean(0)]))
    cent = cent[np.argsort(cent @ [0.299, 0.587, 0.114])]
    return {PALETTE_SYMBOLS[i]: '#%02x%02x%02x' % tuple(int(round(v)) for v in c) for i, c in enumerate(cent)}

def to_rows(cells, y0, x0, m, letter, peel_pink=False):
    rows = [''.join(letter(cells[y0+j, x0+i]) if m[j, i] else '.' for i in range(m.shape[1])) for j in range(m.shape[0])]
    if peel_pink:
        # 剝掉貼著透明區的粉紅邊（JPEG 混色殘影），舌頭在嘴巴裡面不會被剝到
        g = [list(r) for r in rows]; changed = True
        while changed:
            changed = False
            for j in range(len(g)):
                for i in range(len(g[0])):
                    if g[j][i] == 'P' and any(not (0 <= j+dy < len(g) and 0 <= i+dx < len(g[0])) or g[j+dy][i+dx] == '.'
                                              for dy, dx in ((1,0),(-1,0),(0,1),(0,-1))):
                        g[j][i] = '.'; changed = True
        rows = [''.join(r) for r in g]
    return trim(rows)

def trim(rows):
    """去掉四周全透明的行列（剝邊後可能多出來）。"""
    ys = [j for j, r in enumerate(rows) if r.strip('.')]
    if not ys: return rows
    rows = rows[ys[0]:ys[-1]+1]
    xs = [i for i in range(len(rows[0])) if any(r[i] != '.' for r in rows)]
    return [r[xs[0]:xs[-1]+1] for r in rows]

def make_frame(rows):
    # 對齊點：上半身（頭+背）像素的水平重心，讓走路各格不會左右抖動
    top = [i for j in range(min(25, len(rows))) for i, ch in enumerate(rows[j]) if ch != '.']
    return {'rows': rows, 'ax': round(sum(top)/len(top), 1) if top else len(rows[0]) / 2}

def resize_rows(rows, factor):
    """最近鄰縮放字母格（統一動作表與走路圖的大小用）。"""
    H, W = len(rows), len(rows[0]); nh, nw = max(1, round(H*factor)), max(1, round(W*factor))
    return [''.join(rows[min(H-1, int(j/factor))][min(W-1, int(i/factor))] for i in range(nw)) for j in range(nh)]

def legs_only(frames, split=0.62):
    """走路圖「只換腿」：每一格的上半身（頭、背、尾巴）都換成第 1 格的，下半身（腿）保留各格原本的。
    Gemini 有時每格的頭轉向不同（正面 / 側面交替），播放時頭會一直跳；這樣處理後只有腳在動。
    split：上下半身的分界在第 1 格高度的幾成。"""
    base = frames[0]['rows']
    bh, bw = len(base), len(base[0])
    cut = int(bh * split)
    bm = np.array([[c != '.' for c in r] for r in base])
    out = [frames[0]]
    for f in frames[1:]:
        rows = f['rows']; h, w = len(rows), len(rows[0])
        m = np.array([[c != '.' for c in r] for r in rows])
        # 找出這一格的上半身對到第 1 格的最佳位移：以腳底對齊為起點，左右搜尋幾格；
        # 上下只容許 1 格，不然會被原本那格「低頭的側臉」帶偏，整隻狗變成上下跳
        best, best_d = None, (0, h - bh)
        for dy in range(h - bh - 1, h - bh + 2):
            for dx in range(-6, 7):
                score = 0
                for y in range(cut):
                    ty = y + dy
                    if not 0 <= ty < h: continue
                    for x in range(bw):
                        tx = x + dx
                        if 0 <= tx < w and bm[y, x] == m[ty, tx] and bm[y, x]: score += 1
                if best is None or score > best: best, best_d = score, (dx, dy)
        dx, dy = best_d
        # 畫布要容得下第 1 格的上半身與這一格的腿
        left = min(0, dx); top = min(0, dy)
        W = max(w, bw + dx) - left; H = max(h, bh + dy) - top
        g = [['.'] * W for _ in range(H)]
        for y in range(h):           # 這一格的腿
            if y < cut + dy: continue
            for x in range(w):
                if rows[y][x] != '.': g[y - top][x - left] = rows[y][x]
        for y in range(cut):         # 第 1 格的上半身
            for x in range(bw):
                if base[y][x] != '.': g[y + dy - top][x + dx - left] = base[y][x]
        out.append(make_frame(trim([''.join(r) for r in g])))
    return out

def save_preview(frames_by_name, path, color_of):
    """把多個姿勢並排畫成一張放大檢查圖。"""
    items = [(n, f['rows']) for n, fs in frames_by_name.items() for f in fs]
    Wt = sum(len(r[0]) + 3 for _, r in items); Ht = max(len(r) for _, r in items)
    img = Image.new('RGB', (Wt, Ht), (200, 230, 255)); ox = 0
    for _, rows in items:
        for j, r in enumerate(rows):
            for i, ch in enumerate(r):
                if ch != '.': img.putpixel((ox+i, Ht - len(rows) + j), color_of(ch))
        ox += len(rows[0]) + 3
    img.resize((Wt*5, Ht*5), Image.NEAREST).save(path)

# ── 指令 ─────────────────────────────────────

def main():
    sys.stdout.reconfigure(encoding='utf-8')
    args = sys.argv[1:]
    src, name = args[0], args[1]
    sheet = args[args.index('--sheet') + 1].split(',') if '--sheet' in args else None
    palette = json.load(open(args[args.index('--palette') + 1], encoding='utf-8')) if '--palette' in args else None

    cells, fg, p = load_cells(src)
    blobs = find_sprites(fg, gap=1 if sheet else 0)
    letter = palette_letter(palette) if palette else doberman_letter
    frames = [make_frame(to_rows(cells, y0, x0, m, letter, peel_pink=not palette)) for y0, x0, m in blobs]
    print(f'{src}: 格子 {p:.2f}px，找到 {len(frames)} 隻：{[(len(f["rows"][0]), len(f["rows"])) for f in frames]}')

    out = {}
    if sheet:
        if len(frames) != len(sheet):
            print(f'⚠ 動作表應該有 {len(sheet)} 隻，實際找到 {len(frames)} 隻，請檢查 {name}_check.png')
        for n, f in zip(sheet, frames): out[n] = [f]
    else:
        out[name] = frames
    for n, fs in out.items(): json.dump(fs, open(f'{n}.json', 'w'))
    color_of = (lambda ch: hex_rgb(palette[ch]) if ch in palette else (255, 0, 255)) if palette else (lambda ch: DOBERMAN_PREVIEW[ch])
    save_preview({f'{i}': [f] for i, f in enumerate(frames)} if sheet else out, f'{name}_check.png' if sheet else f'{name}.png', color_of)

if __name__ == '__main__':
    main()
