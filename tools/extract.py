"""把 Gemini 生成的像素狗圖（洋紅 #FF00FF 背景）轉成調色盤字母格。

在 sprites/ 資料夾下執行，會產生 <輸出名稱>.json（字母格）與 <輸出名稱>.png（放大檢查圖）：
    cd sprites
    python ../tools/extract.py ../art/doberman/sit.jpg sit
    python ../tools/extract.py ../art/doberman/walk_sheet.jpg walk --rows2   # 兩排的分解圖

轉完記得跑 python tools/build_js.py 把資料合併進預覽頁與桌面程式。
"""
import sys, json
import numpy as np
from PIL import Image
from collections import deque

def period(sig, lo=5, hi=26):
    s = sig - sig.mean(); n = np.arange(len(s)); best = (0, 0)
    for p in np.arange(lo, hi, 0.02):
        c = abs((s * np.exp(-2j*np.pi*n/p)).sum()) * (p ** 0.5)
        if c > best[0]: best = (c, p)
    return best[1]

def phase(sig, p):
    s = sig - sig.mean(); n = np.arange(len(s))
    return (-np.angle((s*np.exp(-2j*np.pi*n/p)).sum())/(2*np.pi)*p) % p

def letter(px):
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

def is_bg(px):
    r, g, b = px
    if r > 170 and b > 170 and g < 120: return True
    return b > g+40 and r > g+40 and abs(r-b) < 60   # 背景與輪廓混色的邊緣

def components(mask):
    H, W = mask.shape; lab = np.zeros((H, W), int); n = 0; out = []
    for y in range(H):
        for x in range(W):
            if mask[y, x] and not lab[y, x]:
                n += 1; q = deque([(y, x)]); lab[y, x] = n; pts = []
                while q:
                    cy, cx = q.popleft(); pts.append((cy, cx))
                    for dy in (-1, 0, 1):
                        for dx in (-1, 0, 1):
                            ny, nx = cy+dy, cx+dx
                            if 0 <= ny < H and 0 <= nx < W and mask[ny, nx] and not lab[ny, nx]:
                                lab[ny, nx] = n; q.append((ny, nx))
                out.append(pts)
    return out

def fill_holes(m):
    H, W = m.shape; mm = np.pad(m, 1); out = np.zeros_like(mm); q = deque([(0, 0)]); out[0, 0] = True
    while q:
        y, x = q.popleft()
        for dy, dx in ((1,0),(-1,0),(0,1),(0,-1)):
            ny, nx = y+dy, x+dx
            if 0 <= ny < H+2 and 0 <= nx < W+2 and not out[ny, nx] and not mm[ny, nx]:
                out[ny, nx] = True; q.append((ny, nx))
    return m | ~out[1:-1, 1:-1]

def main():
    src, name = sys.argv[1], sys.argv[2]
    im = np.asarray(Image.open(src).convert('RGB')).astype(float)
    g = im.mean(axis=2)
    dx = np.abs(np.diff(g, axis=1)).mean(axis=0); dy = np.abs(np.diff(g, axis=0)).mean(axis=1)
    p = (period(dx) + period(dy)) / 2
    px, py = phase(dx, p), phase(dy, p)
    W = int((im.shape[1]-px)//p); H = int((im.shape[0]-py)//p)
    cells = np.zeros((H, W, 3))
    for j in range(H):
        for i in range(W):
            x0 = px+i*p; y0 = py+j*p; m = max(1, int(p*0.25))
            blk = im[int(y0)+m:int(y0+p)-m, int(x0)+m:int(x0+p)-m]
            cells[j, i] = np.median(blk.reshape(-1, 3), axis=0)
    fg = np.array([[not is_bg(cells[j, i]) for i in range(W)] for j in range(H)])
    comps = [c for c in components(fg) if len(c) > 30]
    comps.sort(key=lambda c: min(x for _, x in c) + 1000*(min(y for y, _ in c)//(H//2+1) if '--rows2' in sys.argv else 0))
    sprites = []
    for c in comps:
        ys = [y for y, _ in c]; xs = [x for _, x in c]
        y0, y1, x0, x1 = min(ys), max(ys)+1, min(xs), max(xs)+1
        m = np.zeros((y1-y0, x1-x0), bool)
        for y, x in c: m[y-y0, x-x0] = True
        rows = [''.join(letter(cells[y0+j, x0+i]) if m[j, i] else '.' for i in range(x1-x0)) for j in range(y1-y0)]
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
        # 對齊點：上半身（頭+背）像素的水平重心，讓各格不會左右抖動
        top = [i for j in range(min(25, len(rows))) for i, ch in enumerate(rows[j]) if ch != '.']
        sprites.append({'rows': rows, 'ax': round(sum(top)/len(top), 1)})
    print(f'{name}: period={p:.2f} grid={W}x{H} sprites={[(len(s["rows"][0]), len(s["rows"]), s["ax"]) for s in sprites]}')
    json.dump(sprites, open(f'{name}.json', 'w'))
    # 預覽圖
    sprites_rows = [s['rows'] for s in sprites]
    pal = {'K':(11,11,11),'S':(28,29,29),'B':(46,47,47),'H':(74,75,74),'W':(214,214,214),'E':(110,68,32),'D':(154,92,44),'T':(189,120,64),'P':(200,90,110),'U':(80,140,220),'u':(40,80,160)}
    Wt = sum(len(s[0])+2 for s in sprites_rows); Ht = max(len(s) for s in sprites_rows)
    img = Image.new('RGB', (Wt, Ht), (200, 230, 255)); ox = 0
    for s in sprites_rows:
        for j, r in enumerate(s):
            for i, ch in enumerate(r):
                if ch != '.': img.putpixel((ox+i, j), pal[ch])
        ox += len(s[0])+2
    img.resize((Wt*6, Ht*6), Image.NEAREST).save(f'{name}.png')

main()
