"""像素編輯器的本機伺服器：讀寫 sprites/<品種>/ 的姿勢檔，並提供編輯畫面 editor.html。

用法：python tools/editor.py          （會自動開瀏覽器到 http://127.0.0.1:8770）
      python tools/editor.py --no-open

只綁定 127.0.0.1，別台電腦連不進來。存檔時會：
    - 重新計算對齊點（ax）、裁掉四周空白
    - 把姿勢名稱記進 sprites/<品種>/manual.json，之後 import_breed.py 重新匯入時不會蓋掉手動修改
「套用到程式」會執行 tools/build_js.py，把修改合併進 DobyPet/Assets/breeds.json。
"""
import json, os, sys, subprocess, webbrowser, threading
from urllib.parse import urlsplit
from http.server import ThreadingHTTPServer, BaseHTTPRequestHandler

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import extract as ex

sys.stdout.reconfigure(encoding='utf-8')
ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
SPRITES = os.path.join(ROOT, 'sprites')
POSES = ['stand', 'walk', 'sit', 'sleep', 'eat', 'poop', 'stretch']
PORT = 8770


def breed_dir(bid):
    d = os.path.join(SPRITES, bid)
    if os.path.basename(os.path.normpath(d)) != bid or not os.path.exists(os.path.join(d, 'meta.json')):
        raise FileNotFoundError(bid)
    return d


def load_breed(bid):
    d = breed_dir(bid)
    meta = json.load(open(os.path.join(d, 'meta.json'), encoding='utf-8'))
    manual_path = os.path.join(d, 'manual.json')
    return {
        'id': bid,
        'meta': meta,
        'poses': {p: json.load(open(os.path.join(d, p + '.json'))) for p in POSES if os.path.exists(os.path.join(d, p + '.json'))},
        'manual': json.load(open(manual_path, encoding='utf-8')) if os.path.exists(manual_path) else [],
    }


def save_pose(bid, pose, frames):
    if pose not in POSES: raise ValueError(pose)
    d = breed_dir(bid)
    clean = []
    for f in frames:
        rows = ex.trim([''.join(c if c else '.' for c in r) for r in f['rows']])
        clean.append(ex.make_frame(rows))
    json.dump(clean, open(os.path.join(d, pose + '.json'), 'w'))
    manual_path = os.path.join(d, 'manual.json')
    manual = json.load(open(manual_path, encoding='utf-8')) if os.path.exists(manual_path) else []
    if pose not in manual:
        manual.append(pose)
        json.dump(manual, open(manual_path, 'w', encoding='utf-8'), indent=1)
    return clean


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
            path = urlsplit(self.path).path   # 去掉 ?breed=husky 這類參數
            if path in ('/', '/index.html'):
                return self.send(200, open(os.path.join(ROOT, 'tools', 'editor.html'), 'rb').read(), 'text/html; charset=utf-8')
            if path == '/api/breeds':
                items = []
                for bid in sorted(os.listdir(SPRITES)):
                    mp = os.path.join(SPRITES, bid, 'meta.json')
                    if os.path.exists(mp): items.append({'id': bid, 'name': json.load(open(mp, encoding='utf-8')).get('name', bid)})
                return self.send(200, items)
            if path.startswith('/api/breed/'):
                return self.send(200, load_breed(path.split('/')[-1]))
            self.send(404, {'error': 'not found'})
        except FileNotFoundError:
            self.send(404, {'error': 'not found'})
        except Exception as e:
            self.send(500, {'error': str(e)})

    def do_POST(self):
        try:
            # 只接受同一個本機頁面送來的請求
            if self.headers.get('Origin') not in (None, f'http://127.0.0.1:{PORT}', f'http://localhost:{PORT}'):
                return self.send(403, {'error': 'forbidden'})
            body = json.loads(self.rfile.read(int(self.headers.get('Content-Length', 0))) or b'{}')
            parts = urlsplit(self.path).path.strip('/').split('/')
            if parts[:2] == ['api', 'save'] and len(parts) == 4:
                frames = save_pose(parts[2], parts[3], body['frames'])
                return self.send(200, {'ok': True, 'frames': frames})
            if parts == ['api', 'build']:
                r = subprocess.run([sys.executable, os.path.join(ROOT, 'tools', 'build_js.py')], capture_output=True, text=True, encoding='utf-8')
                return self.send(200 if r.returncode == 0 else 500, {'ok': r.returncode == 0, 'output': r.stdout + r.stderr})
            self.send(404, {'error': 'not found'})
        except Exception as e:
            self.send(500, {'error': str(e)})


def main():
    server = ThreadingHTTPServer(('127.0.0.1', PORT), Handler)
    url = f'http://127.0.0.1:{PORT}/'
    print(f'像素編輯器：{url}（按 Ctrl+C 結束）')
    if '--no-open' not in sys.argv:
        threading.Timer(.5, lambda: webbrowser.open(url)).start()
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass


if __name__ == '__main__':
    main()
