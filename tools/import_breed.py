"""一鍵匯入新品種：把 art/<品種>/ 的「動作表」與「走路圖」轉成 sprites/<品種>/ 的姿勢資料。

需要的檔案（副檔名 jpg/png/webp 都可以）：
    art/<品種>/sheet.*        一張圖、兩排三列共 6 個姿勢：站、坐、睡 / 吃、便便、伸懶腰
    art/<品種>/walk_sheet.*   一排 4 格走路分解圖
    art/<品種>/import.json    （選用）{"flip": ["stand", ...], "walkFlip": false, "walkLegsOnly": false}
                              Gemini 畫成面向右邊的姿勢列在 flip，匯入時會翻成面向左（程式假設都面向左）
                              walkLegsOnly：走路各格的頭轉向不一致時設 true，只保留各格的腿、上半身都用第 1 格

做的事：
    1. 從兩張圖的前景顏色自動找出 12 色調色盤（sprites/<品種>/palette.json）
    2. 動作表依閱讀順序切成 stand/sit/sleep/eat/poop/stretch，走路圖切成 walk 的 4 格
    3. 走路圖跟動作表大小不一致時，自動縮放到同樣大小
    4. 沒有 meta.json 就建立一份（眨眼、搖尾巴之後再標）
    5. 產生 sprites/<品種>/overview.png 總覽檢查圖，並執行 build_js.py

用法：python tools/import_breed.py shiba
      python tools/import_breed.py shiba --force   # 連用像素編輯器手動修改過的姿勢也重新產生
"""
import sys, os, json, glob, subprocess
import numpy as np
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import extract as ex

sys.stdout.reconfigure(encoding='utf-8')
ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
SHEET_POSES = ['stand', 'sit', 'sleep', 'eat', 'poop', 'stretch']
NAMES = {'shiba': '柴犬', 'corgi': '柯基', 'golden': '黃金獵犬', 'husky': '哈士奇', 'dachshund': '臘腸', 'poodle': '大貴賓'}


def find(folder, stem):
    hits = [f for f in glob.glob(os.path.join(folder, stem + '.*')) if f.lower().endswith(('.jpg', '.jpeg', '.png', '.webp'))]
    if not hits: sys.exit(f'找不到 {os.path.join(folder, stem)}.jpg/png/webp')
    return hits[0]


def main():
    breed = sys.argv[1]
    art = os.path.join(ROOT, 'art', breed)
    out = os.path.join(ROOT, 'sprites', breed)
    os.makedirs(out, exist_ok=True)

    sheet_cells, sheet_fg, sp = ex.load_cells(find(art, 'sheet'))
    walk_cells, walk_fg, wp = ex.load_cells(find(art, 'walk_sheet'))
    sheet_blobs = ex.find_sprites(sheet_fg, gap=1)
    walk_blobs = ex.find_sprites(walk_fg, gap=1)
    print(f'動作表：格子 {sp:.2f}px，找到 {len(sheet_blobs)} 隻（應該是 6）')
    print(f'走路圖：格子 {wp:.2f}px，找到 {len(walk_blobs)} 格（應該是 4）')

    # 1. 調色盤：兩張圖的前景像素一起算，同一品種的顏色才會一致
    def fg_pixels(cells, blobs):
        return np.concatenate([cells[y0:y0+m.shape[0], x0:x0+m.shape[1]][m] for y0, x0, m in blobs])
    palette = ex.auto_palette([fg_pixels(sheet_cells, sheet_blobs), fg_pixels(walk_cells, walk_blobs)])
    json.dump(palette, open(os.path.join(out, 'palette.json'), 'w'), indent=1)
    letter = ex.palette_letter(palette)

    # 2. 切圖
    opts_path = os.path.join(art, 'import.json')
    opts = json.load(open(opts_path, encoding='utf-8')) if os.path.exists(opts_path) else {}
    flip = set(opts.get('flip', []))
    mirror = lambda rows: [r[::-1] for r in rows]

    sheet_frames = [ex.make_frame(ex.to_rows(sheet_cells, y0, x0, m, letter)) for y0, x0, m in sheet_blobs]
    sheet_frames = [ex.make_frame(mirror(f['rows'])) if n in flip else f for n, f in zip(SHEET_POSES, sheet_frames)] + sheet_frames[len(SHEET_POSES):]
    walk_frames = [ex.make_frame(ex.to_rows(walk_cells, y0, x0, m, letter)) for y0, x0, m in walk_blobs]
    if opts.get('walkFlip'): walk_frames = [ex.make_frame(mirror(f['rows'])) for f in walk_frames]
    if len(walk_frames) > 4:
        # Gemini 有時會把附圖當成要修改的圖，只把前面幾格改成走路、後面留著舊姿勢；依閱讀順序取前 4 格
        print(f'⚠ 走路圖找到 {len(walk_frames)} 格，只取前 4 格，請看 overview.png 確認都是走路')
        walk_frames = walk_frames[:4]
    if len(sheet_frames) < len(SHEET_POSES):
        print(f'⚠ 動作表只切出 {len(sheet_frames)} 隻，可能有兩隻黏在一起或畫太少，請看 overview.png，必要時重新生成')
    if len(sheet_frames) > len(SHEET_POSES):
        print(f'⚠ 動作表切出 {len(sheet_frames)} 隻，多出來的會被忽略（可能是文字或雜點）')

    # 3. 走路圖縮放到跟站姿一樣高
    if sheet_frames and walk_frames:
        stand_h = len(sheet_frames[0]['rows'])
        walk_h = float(np.median([len(f['rows']) for f in walk_frames]))
        factor = stand_h / walk_h
        if abs(factor - 1) > .08:
            print(f'走路圖比動作表 {"大" if factor < 1 else "小"}，縮放 ×{factor:.2f}')
            walk_frames = [ex.make_frame(ex.resize_rows(f['rows'], factor)) for f in walk_frames]

    if opts.get('walkLegsOnly') and len(walk_frames) > 1:
        walk_frames = ex.legs_only(walk_frames, opts.get('walkSplit', 0.62))
        print('走路圖：只換腿（上半身都用第 1 格）')

    poses = dict(zip(SHEET_POSES, ([f] for f in sheet_frames)))
    poses['walk'] = walk_frames

    # 用像素編輯器（tools/editor/）手動修改過的姿勢記在 manual.json，預設不覆蓋
    manual_path = os.path.join(out, 'manual.json')
    manual = json.load(open(manual_path, encoding='utf-8')) if os.path.exists(manual_path) else []
    force = '--force' in sys.argv
    for n, fs in poses.items():
        path = os.path.join(out, n + '.json')
        if n in manual and not force:
            print(f'⚠ {n} 手動修改過，保留不覆蓋（要重新產生請加 --force）')
            poses[n] = json.load(open(path))
            continue
        json.dump(fs, open(path, 'w'))
    if force and os.path.exists(manual_path): os.remove(manual_path)

    # 4. meta.json（已存在就保留標好的位置與毛色；調色盤變了要再跑 make_coats.py）
    meta_path = os.path.join(out, 'meta.json')
    if os.path.exists(meta_path):
        meta = json.load(open(meta_path, encoding='utf-8'))
        print('meta.json 已存在，保留原本的設定；調色盤已更新，記得執行 python tools/make_coats.py ' + breed)
    else:
        stand = poses.get('stand', [{'rows': ['.']}])[0]['rows']
        w, h = len(stand[0]), len(stand)
        meta = {
            'name': NAMES.get(breed, breed),
            'defaultCoat': '原色',
            'coats': {'原色': palette},
            'blink': [], 'half': [], 'tail': [],   # 之後看 overview.png 再標
            'mouth': [w // 8, h * 3 // 8],
            'rear': [w * 7 // 8, h // 2],
            'icon': [0, 0, min(w, h) // 2],
        }
    json.dump(meta, open(meta_path, 'w', encoding='utf-8'), ensure_ascii=False, indent=2)

    # 5. 總覽圖與合併
    ex.save_preview(poses, os.path.join(out, 'overview.png'), lambda ch: ex.hex_rgb(palette[ch]) if ch in palette else (255, 0, 255))
    print('姿勢：' + '、'.join(f'{n} {len(fs[0]["rows"][0])}×{len(fs[0]["rows"])}' + (f'×{len(fs)}格' if len(fs) > 1 else '') for n, fs in poses.items()))
    print(f'總覽圖：{os.path.join(out, "overview.png")}')
    subprocess.run([sys.executable, os.path.join(ROOT, 'tools', 'build_js.py')], check=True)


if __name__ == '__main__':
    main()
