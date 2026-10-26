"""動畫檢查器的本機伺服器：把多張圖（或一張多格的圖）前處理成像素格，排成動畫，檢查播起來順不順。

用法：雙擊同資料夾的「啟動動畫檢查器.bat」，或
      python tools/animcheck/animcheck.py          （會自動開瀏覽器到 http://127.0.0.1:8771）
      python tools/animcheck/animcheck.py --no-open

只綁定 127.0.0.1。圖可以從 art/ 資料夾挑，也可以直接拖進網頁。前處理步驟：
    1. 像素格偵測：找出 Gemini 圖的「一格像素」有多大，每格取代表色（也可以指定大小或改成直接縮小）
    2. 去背：洋紅背景 / 角落顏色 / PNG 透明
    3. 去雜點：拿掉 JPEG 壓縮留下的零散小點
    4. 切格：一張圖裡有好幾隻就依閱讀順序切開
    5. 翻轉、尺寸統一（比對身形量出真正的大小比例，不會因為抬腳、甩頭而誤判）、統一調色盤（減少每格顏色閃爍）、只換腿
    6. 對齊：腳底貼地，水平依「軀幹比對 / 身形比對 / 上半身重心 / 整體中心」對齊
處理結果在網頁上預覽，可以另存 GIF；按「套用到品種」才會寫入 sprites/<品種>/<姿勢>.json
（並記進 manual.json，重新匯入時不會被蓋掉）。
"""
import base64, hashlib, io, json, os, subprocess, sys, threading, webbrowser
from urllib.parse import urlsplit
from http.server import ThreadingHTTPServer, BaseHTTPRequestHandler

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.dirname(HERE))   # tools/，共用的 extract.py 在那裡
import extract as ex

sys.stdout.reconfigure(encoding='utf-8')
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
PORT = int(os.environ.get('PORT', 8771))
IMAGE_EXT = ('.png', '.jpg', '.jpeg', '.webp')


# ── 讀圖 ───────────────────────────────────────

def open_source(src):
    """src：{'path': 專案內相對路徑} 或 {'name': 檔名, 'data': base64}。回傳 (標籤, RGBA 陣列)。"""
    if 'path' in src:
        full = os.path.realpath(os.path.join(ROOT, src['path']))
        if not full.startswith(os.path.realpath(ROOT) + os.sep) or not full.lower().endswith(IMAGE_EXT):
            raise ValueError(f'不能讀這個檔案：{src["path"]}')
        im, label = Image.open(full), src['path']
    else:
        data = src['data'].split(',', 1)[-1]
        im, label = Image.open(io.BytesIO(base64.b64decode(data))), src.get('name', '上傳的圖')
    return label, np.asarray(im.convert('RGBA')).astype(float)


def to_cells(im, grid, cell, shrink):
    """把原圖變成「一格一個顏色」。grid='auto' 自動偵測格子大小、'manual' 用 cell、'off' 縮小 shrink 倍。
    回傳 (cells RGB, alpha, 格子大小)。"""
    rgb, a = im[..., :3], im[..., 3]
    if grid == 'off':
        p, px, py = max(1.0, float(shrink)), 0.0, 0.0
    else:
        g = rgb.mean(axis=2)
        dx = np.abs(np.diff(g, axis=1)).mean(axis=0); dy = np.abs(np.diff(g, axis=0)).mean(axis=1)
        p = float(cell) if grid == 'manual' else (ex._period(dx) + ex._period(dy)) / 2
        px, py = ex._phase(dx, p), ex._phase(dy, p)
    W = int((im.shape[1] - px) // p); H = int((im.shape[0] - py) // p)
    cells = np.zeros((H, W, 3)); alpha = np.zeros((H, W))
    m = max(0, int(p * 0.25)) if p >= 3 else 0
    for j in range(H):
        y0 = int(py + j * p); y1 = max(y0 + 1, int(py + (j + 1) * p))
        for i in range(W):
            x0 = int(px + i * p); x1 = max(x0 + 1, int(px + (i + 1) * p))
            blk = rgb[y0 + m:max(y0 + m + 1, y1 - m), x0 + m:max(x0 + m + 1, x1 - m)].reshape(-1, 3)
            cells[j, i] = np.median(blk, axis=0)
            alpha[j, i] = np.median(a[y0:y1, x0:x1])
    return cells, alpha, p


def background_mask(cells, alpha, mode, tol):
    """回傳背景遮罩（True = 背景）。"""
    H, W = cells.shape[:2]
    if mode == 'alpha':
        return alpha < 128
    if mode == 'corner':
        border = np.concatenate([cells[0], cells[-1], cells[:, 0], cells[:, -1]])
        bg = np.median(border, axis=0)
        return np.sqrt(((cells - bg) ** 2).sum(-1)) < tol
    if mode == 'magenta':
        return np.array([[ex.is_bg(cells[j, i]) for i in range(W)] for j in range(H)])
    return np.zeros((H, W), bool)


def despeckle(fg, min_px):
    if min_px <= 0: return fg
    lab, n = ex._label(fg)
    sizes = np.bincount(lab.ravel(), minlength=n + 1)
    return fg & (sizes[lab] >= min_px)


def resize_nearest(cells, mask, factor):
    H, W = mask.shape; nh, nw = max(1, round(H * factor)), max(1, round(W * factor))
    ys = np.minimum(H - 1, (np.arange(nh) / factor).astype(int)); xs = np.minimum(W - 1, (np.arange(nw) / factor).astype(int))
    return cells[ys][:, xs], mask[ys][:, xs]


def resize_mask(mask, factor):
    H, W = mask.shape; nh, nw = max(1, round(H * factor)), max(1, round(W * factor))
    ys = np.minimum(H - 1, (np.arange(nh) / factor).astype(int)); xs = np.minimum(W - 1, (np.arange(nw) / factor).astype(int))
    return mask[ys][:, xs]


SCALES = np.exp(np.linspace(np.log(.75), np.log(1.33), 41))


def register(ref, m, scales=SCALES, reach=8):
    """把 m 縮放、平移去疊 ref（腳底對齊），找重疊最多（IoU 最高）的組合。
    回傳 (iou, s, off)：m 縮放 s 倍後最吻合；ref 上的 x 對到縮放後 m 的 x + off（用來換算對齊錨點）。
    高度會因為抬腳、甩頭而不同，用整個身形比對才量得出真正的大小差異。"""
    rh, rw = ref.shape
    best = (-1, 1.0, 0)
    for s in scales:
        mm = resize_mask(m, s); h, w = mm.shape
        H = max(rh, h) + 3; W = max(rw, w) + 2 * reach + 2
        ax0 = (W - rw) // 2
        A = np.zeros((H, W), bool); A[H - 1 - rh:H - 1, ax0:ax0 + rw] = ref
        union0 = A.sum()
        for dy in (-1, 0, 1):
            y0 = H - 1 - h + dy
            if y0 < 0 or y0 + h > H: continue
            for dx in range(-reach, reach + 1):
                x0 = (W - w) // 2 + dx
                if x0 < 0 or x0 + w > W: continue
                inter = (A[y0:y0 + h, x0:x0 + w] & mm).sum()
                iou = inter / (union0 + mm.sum() - inter)
                if iou > best[0]: best = (iou, s, ax0 - x0)
    return best


# ── 對齊 ───────────────────────────────────────

def torso_shift(base, rows, split):
    """跟第 1 格比對軀幹那一段（頭和腿不比），找出最吻合的水平位移；腳底對齊為起點。"""
    bh, bw = len(base), len(base[0]); h, w = len(rows), len(rows[0])
    cut = int(bh * split)
    bm = np.array([[c != '.' for c in r] for r in base]); m = np.array([[c != '.' for c in r] for r in rows])
    dy = h - bh
    best, best_dx = -1, 0
    for dx in range(-12, 13):
        score = 0
        for y in range(max(0, cut - 14), cut):
            ty = y + dy
            if not 0 <= ty < h: continue
            for x in range(bw):
                tx = x + dx
                if bm[y, x] and 0 <= tx < w and m[ty, tx]: score += 1
        if score > best: best, best_dx = score, dx
    return best_dx


def anchor_x(rows, mode, base=None, base_ax=0, split=.62):
    """這一格要對到畫面中線的 x（以格子為單位）。"""
    w = len(rows[0])
    if mode == 'center': return w / 2
    if mode == 'mass': return ex.make_frame(rows)['ax']
    if mode == 'torso' and base is not None:
        # 這一格的 x = 第 1 格的 x + 位移，所以錨點也一起位移
        return base_ax + torso_shift(base, rows, split)
    if mode == 'shape' and base is not None:
        mask = lambda rs: np.array([[c != '.' for c in r] for r in rs])
        return base_ax + register(mask(base), mask(rows), scales=[1.0], reach=12)[2]
    if mode == 'left': return 0
    return w / 2


# ── 主流程 ─────────────────────────────────────

_cache = {}


def cut_source(src, o):
    """讀圖 → 像素格 → 去背 → 去雜點 → 切格。最花時間的部分，相同的圖和設定會直接用上次的結果。"""
    ident = src.get('path') or hashlib.sha1(src['data'].encode()).hexdigest()
    if 'path' in src:
        ident += '@%d' % os.path.getmtime(os.path.join(ROOT, src['path']))
    key = (ident,) + tuple(str(o.get(k)) for k in ('grid', 'cell', 'shrink', 'bg', 'tol', 'speckle', 'split', 'gap', 'minPx'))
    if key in _cache: return _cache[key]
    label, im = open_source(src)
    cells, alpha, p = to_cells(im, o.get('grid', 'auto'), o.get('cell', 8), o.get('shrink', 4))
    fg = ~background_mask(cells, alpha, o.get('bg', 'magenta'), float(o.get('tol', 60)))
    fg = despeckle(fg, int(o.get('speckle', 8)))
    if o.get('split', True):
        blobs = ex.find_sprites(fg, gap=int(o.get('gap', 1)), min_px=int(o.get('minPx', 30)))
    else:
        ys, xs = np.where(fg)
        blobs = [(ys.min(), xs.min(), fg[ys.min():ys.max() + 1, xs.min():xs.max() + 1])] if len(ys) else []
    line = f'{label}：{im.shape[1]}×{im.shape[0]}px，一格約 {p:.2f}px → {cells.shape[1]}×{cells.shape[0]} 格，切出 {len(blobs)} 隻'
    pieces = [{'label': f'{os.path.basename(label)} #{k + 1}', 'cells': cells[y0:y0 + m.shape[0], x0:x0 + m.shape[1]], 'mask': m}
              for k, (y0, x0, m) in enumerate(blobs)]
    if len(_cache) > 40: _cache.clear()
    _cache[key] = (line, pieces)
    return line, pieces


def process(req):
    o = req.get('options', {})
    log, pieces = [], []
    for src in req['sources']:
        line, ps = cut_source(src, o)
        log.append(line)
        pieces += [dict(pc) for pc in ps]
    if not pieces:
        return {'frames': [], 'palette': {}, 'log': log + ['⚠ 沒有找到任何圖案，試試調整去背方式或容差']}

    flips = set(o.get('flip', []))
    for i, pc in enumerate(pieces):
        if o.get('flipAll') != (i in flips):
            pc['cells'], pc['mask'] = pc['cells'][:, ::-1], pc['mask'][:, ::-1]

    # 尺寸統一：shape＝跟第 1 格比對身形量出真正的大小比例；height＝只看高度
    size_mode = o.get('sizeMode', 'off')
    if size_mode != 'off' and len(pieces) > 1:
        ref = pieces[0]['mask']
        if size_mode == 'shape':
            rel = [1.0] + [1 / register(ref, pc['mask'])[1] for pc in pieces[1:]]   # 每格是第 1 格的幾倍大
        else:
            rel = [pc['mask'].shape[0] / ref.shape[0] for pc in pieces]
        target = {'median': float(np.median(rel)), 'max': max(rel)}.get(o.get('sizeTo'), 1.0)
        log.append('大小（以第 1 格為 1）：' + '、'.join(f'{r:.2f}' for r in rel) + f'，統一成 {target:.2f}')
        for pc, r in zip(pieces, rel):
            f = target / r
            if abs(f - 1) > .015: pc['cells'], pc['mask'] = resize_nearest(pc['cells'], pc['mask'], f)

    # 上色：統一調色盤（k-means）或保留原色
    if o.get('quantize', True):
        pal = ex.auto_palette([pc['cells'][pc['mask']] for pc in pieces], k=int(o.get('k', 12)))
        letter = ex.palette_letter(pal)
    else:
        pal, lut = {}, {}
        def letter(px):
            key = '#%02x%02x%02x' % tuple(int(round(v)) for v in px)
            if key not in lut:
                ch = chr(0x100 + len(lut)); lut[key] = ch; pal[ch] = key
            return lut[key]
    frames = []
    for pc in pieces:
        m = pc['mask']
        rows = ex.trim([''.join(letter(pc['cells'][j, i]) if m[j, i] else '.' for i in range(m.shape[1])) for j in range(m.shape[0])])
        frames.append({'rows': rows, 'label': pc['label']})

    split = float(o.get('split_at', .62))
    if o.get('legsOnly') and len(frames) > 1:
        merged = ex.legs_only([ex.make_frame(f['rows']) for f in frames], split=split)
        for f, mf in zip(frames, merged): f['rows'] = mf['rows']

    mode = o.get('align', 'mass')
    base = frames[0]['rows']; base_ax = anchor_x(base, 'mass' if mode in ('torso', 'shape') else mode)
    mask = lambda rs: np.array([[c != '.' for c in r] for r in rs])
    for i, f in enumerate(frames):
        f['ax'] = base_ax if i == 0 else anchor_x(f['rows'], mode, base, base_ax, split)
        f['w'], f['h'] = len(f['rows'][0]), len(f['rows'])
        # 跟第 1 格比對身形量出的大小比例，給診斷的「大小」用
        f['rel'] = 1.0 if i == 0 else round(1 / register(mask(base), mask(f['rows']))[1], 3)
    log.append(f'共 {len(frames)} 格，調色盤 {len(pal)} 色')
    return {'frames': frames, 'palette': pal, 'log': log}


# ── 套用到品種 ───────────────────────────────

SPRITES = os.path.join(ROOT, 'sprites')
APP_POSES = ['stand', 'walk', 'sit', 'sleep', 'eat', 'poop', 'stretch']                 # 程式目前會用到的
NEW_POSES = ['sniff', 'chase', 'dizzy', 'pant', 'shake', 'sidesleep']                    # 新行為（之後才接進程式）
# 桌面程式的特效共用色（DobyPet/Sprites.cs 的 Palettes.Fx），有些品種的姿勢會用到
FX = {'K': '#0b0b0b', 'W': '#d6d6d6', 'P': '#d46a7e', 'U': '#4a90d9', 'u': '#2c5f9e', 'R': '#ff5c7a', 'r': '#c93a58',
      'w': '#ffffff', 'F': '#b9e07e', 'f': '#86bd4c', 'Y': '#ffd84d', 'Z': '#5f84b3', 'p': '#7a4b24', 'q': '#a8703a',
      'o': '#8a5a2c', 'x': '#2b2b2b'}


def breed_dir(bid):
    d = os.path.join(SPRITES, bid)
    if os.path.basename(os.path.normpath(d)) != bid or not os.path.exists(os.path.join(d, 'meta.json')):
        raise FileNotFoundError(bid)
    return d


def list_breeds():
    out = []
    for bid in sorted(os.listdir(SPRITES)):
        d = os.path.join(SPRITES, bid)
        if not os.path.exists(os.path.join(d, 'meta.json')): continue
        meta = json.load(open(os.path.join(d, 'meta.json'), encoding='utf-8'))
        poses = {}
        for p in APP_POSES + NEW_POSES:
            f = os.path.join(d, p + '.json')
            if os.path.exists(f):
                fr = json.load(open(f))
                poses[p] = {'frames': len(fr), 'w': len(fr[0]['rows'][0]), 'h': len(fr[0]['rows'])}
        out.append({'id': bid, 'name': meta.get('name', bid), 'poses': poses})
    return out


def breed_palette(d):
    """品種預設毛色的調色盤，加上這個品種姿勢裡用到的特效共用色。"""
    meta = json.load(open(os.path.join(d, 'meta.json'), encoding='utf-8'))
    pal = dict(meta['coats'][meta.get('defaultCoat') or next(iter(meta['coats']))])
    used = set()
    for p in APP_POSES:
        f = os.path.join(d, p + '.json')
        if os.path.exists(f):
            for fr in json.load(open(f)): used.update(''.join(fr['rows']))
    for ch in used:
        if ch in FX and ch not in pal: pal[ch] = FX[ch]
    return pal


def save_pose(req):
    """把檢查器裡排好的格子（含手動微調）存成 sprites/<品種>/<姿勢>.json。
    顏色換成這個品種調色盤的字母（換毛色才會正常），大小可以對齊站姿或原本的同名姿勢。"""
    bid, pose = req['breed'], req['pose']
    if pose not in APP_POSES + NEW_POSES: raise ValueError(pose)
    d = breed_dir(bid)
    src_pal = req['palette']
    letter = ex.palette_letter(breed_palette(d))
    frames = []
    for f in req['frames']:
        rows = [''.join('.' if c == '.' else letter(ex.hex_rgb(src_pal[c])) for c in r) for r in f['rows']]
        frames.append({'rows': rows, 'ax': f['ax'] - f.get('dx', 0), 'dy': f.get('dy', 0)})

    # 大小：跟哪個姿勢一樣高（取各格高度的中位數比較）
    scale, ref = req.get('scale', 'same'), None
    if scale == 'same' and os.path.exists(os.path.join(d, pose + '.json')): ref = pose
    elif scale in ('same', 'stand'): ref = 'stand'
    factor = float(req.get('factor', 1)) if scale == 'custom' else 1.0
    if ref:
        rf = json.load(open(os.path.join(d, ref + '.json')))
        factor = float(np.median([len(x['rows']) for x in rf])) / float(np.median([len(x['rows']) for x in frames]))
    if abs(factor - 1) > .015:
        for f in frames:
            f['rows'] = ex.resize_rows(f['rows'], factor); f['ax'] *= factor; f['dy'] = round(f['dy'] * factor)

    # 存檔：往上微調（dy < 0）的格子在腳底下補透明列，程式是以圖的底邊貼地
    out = []
    for f in frames:
        rows = f['rows']
        if f['dy'] < 0: rows = rows + ['.' * len(rows[0])] * -f['dy']
        out.append({'rows': rows, 'ax': round(f['ax'], 1)})
    json.dump(out, open(os.path.join(d, pose + '.json'), 'w'))
    manual_path = os.path.join(d, 'manual.json')
    manual = json.load(open(manual_path, encoding='utf-8')) if os.path.exists(manual_path) else []
    if pose not in manual:
        manual.append(pose)
        json.dump(manual, open(manual_path, 'w', encoding='utf-8'), indent=1)

    msg = [f'已存成 sprites/{bid}/{pose}.json：{len(out)} 格，{len(out[0]["rows"][0])}×{len(out[0]["rows"])}' +
           (f'（縮放 ×{factor:.2f}，對齊 {ref}）' if ref else f'（縮放 ×{factor:.2f}）' if factor != 1 else '')]
    msg.append('已記進 manual.json，重新匯入品種時不會被蓋掉')
    if pose in APP_POSES:
        r = subprocess.run([sys.executable, os.path.join(ROOT, 'tools', 'build_js.py')], capture_output=True, text=True, encoding='utf-8')
        msg.append('已執行 build_js.py，重新建置桌面程式就會用到' if r.returncode == 0 else '⚠ build_js.py 失敗：' + r.stdout + r.stderr)
    else:
        msg.append('這是新行為的姿勢，程式還沒接上，先存著')
    return {'ok': True, 'message': '\n'.join(msg)}


def list_images():
    out = []
    for top in ('art', 'sprites'):
        for d, _, files in os.walk(os.path.join(ROOT, top)):
            for fn in sorted(files):
                if fn.lower().endswith(IMAGE_EXT):
                    out.append(os.path.relpath(os.path.join(d, fn), ROOT).replace('\\', '/'))
    return sorted(out)


def make_gif(req):
    imgs = [Image.open(io.BytesIO(base64.b64decode(u.split(',', 1)[-1]))).convert('RGB') for u in req['frames']]
    buf = io.BytesIO()
    imgs[0].save(buf, 'GIF', save_all=True, append_images=imgs[1:], duration=int(req.get('ms', 150)), loop=0, disposal=2)
    return 'data:image/gif;base64,' + base64.b64encode(buf.getvalue()).decode()


class Handler(BaseHTTPRequestHandler):
    def log_message(self, fmt, *args): pass

    def send(self, code, body, ctype='application/json; charset=utf-8'):
        data = body if isinstance(body, bytes) else json.dumps(body, ensure_ascii=False).encode('utf-8')
        self.send_response(code)
        self.send_header('Content-Type', ctype)
        self.send_header('Content-Length', str(len(data)))
        self.send_header('Cache-Control', 'no-store')
        self.end_headers()
        self.wfile.write(data)

    def do_GET(self):
        try:
            path = urlsplit(self.path).path
            if path in ('/', '/index.html'):
                return self.send(200, open(os.path.join(HERE, 'animcheck.html'), 'rb').read(), 'text/html; charset=utf-8')
            if path == '/api/images':
                return self.send(200, list_images())
            if path == '/api/breeds':
                return self.send(200, list_breeds())
            self.send(404, {'error': 'not found'})
        except Exception as e:
            self.send(500, {'error': str(e)})

    def do_POST(self):
        try:
            # 只接受同一個本機頁面送來的請求
            if self.headers.get('Origin') not in (None, f'http://127.0.0.1:{PORT}', f'http://localhost:{PORT}'):
                return self.send(403, {'error': 'forbidden'})
            body = json.loads(self.rfile.read(int(self.headers.get('Content-Length', 0))) or b'{}')
            path = urlsplit(self.path).path
            if path == '/api/process': return self.send(200, process(body))
            if path == '/api/gif': return self.send(200, {'gif': make_gif(body)})
            if path == '/api/save': return self.send(200, save_pose(body))
            self.send(404, {'error': 'not found'})
        except Exception as e:
            self.send(500, {'error': f'{type(e).__name__}: {e}'})


class Server(ThreadingHTTPServer):
    # Windows 預設允許兩個程式綁同一個埠，會變成開了兩份；關掉才偵測得到「已經在執行」
    allow_reuse_address = False


def main():
    url = f'http://127.0.0.1:{PORT}/'
    try:
        server = Server(('127.0.0.1', PORT), Handler)
    except OSError:
        # 已經開著一個了：直接開瀏覽器過去（如果是改程式前開的舊版，要先關掉它再重開）
        print(f'動畫檢查器已經在執行，開啟 {url}')
        webbrowser.open(url)
        return
    print(f'動畫檢查器：{url}（按 Ctrl+C 結束）')
    if '--no-open' not in sys.argv:
        threading.Timer(.5, lambda: webbrowser.open(url)).start()
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass


if __name__ == '__main__':
    main()
