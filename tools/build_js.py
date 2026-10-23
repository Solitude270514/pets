"""把 sprites/<品種>/ 底下的姿勢與中繼資料合併成：
- DobyPet/Assets/breeds.json     桌面程式內嵌用：{品種代號: {meta, poses}}
- sprites/sprites.js             預覽頁面 preview.html 用（杜賓的姿勢放在 SPRITES，全部品種放在 BREEDS）
- preview_standalone.html        單一檔案版預覽，可直接雙擊開啟

每個品種資料夾需要：meta.json + stand/sit/sleep/eat/walk/poop/stretch 七個 .json。
缺姿勢的品種會被略過並印出警告，程式不會載入它。

用法: python tools/build_js.py
"""
import json, glob, os, sys

sys.stdout.reconfigure(encoding='utf-8')   # Windows 主控台預設 cp950，印不出 ✓ ⚠

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
SPRITES = os.path.join(ROOT, 'sprites')
POSES = ['stand', 'sit', 'sleep', 'eat', 'walk', 'poop', 'stretch']

breeds = {}
for d in sorted(glob.glob(os.path.join(SPRITES, '*', 'meta.json'))):
    folder = os.path.dirname(d)
    bid = os.path.basename(folder)
    missing = [p for p in POSES if not os.path.exists(os.path.join(folder, p + '.json'))]
    if missing:
        print(f'⚠ {bid}: 缺少 {", ".join(missing)}，先略過')
        continue
    meta = json.load(open(d, encoding='utf-8'))
    poses = {p: json.load(open(os.path.join(folder, p + '.json'), encoding='utf-8')) for p in POSES}
    breeds[bid] = {'meta': meta, 'poses': poses}
    print(f'✓ {bid}（{meta.get("name")}）: ' + ', '.join(f'{p}×{len(v)}' for p, v in poses.items()))

json.dump(breeds, open(os.path.join(ROOT, 'DobyPet', 'Assets', 'breeds.json'), 'w', encoding='utf-8'),
          ensure_ascii=False, separators=(',', ':'))

js = ('window.SPRITES=' + json.dumps(breeds['doberman']['poses'], separators=(',', ':')) + ';\n'
      'window.BREEDS=' + json.dumps(breeds, ensure_ascii=False, separators=(',', ':')) + ';\n')
open(os.path.join(SPRITES, 'sprites.js'), 'w', encoding='utf-8').write(js)

# 單一檔案版預覽：把 sprites.js 直接塞進 preview.html
html = open(os.path.join(ROOT, 'preview.html'), encoding='utf-8').read()
html = html.replace('<script src="sprites/sprites.js"></script>', '<script>' + js + '</script>')
open(os.path.join(ROOT, 'preview_standalone.html'), 'w', encoding='utf-8').write(html)
