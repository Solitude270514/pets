"""把 sprites/*.json 合併成：
- sprites/sprites.js             預覽頁面 preview.html 載入用
- DobyPet/Assets/sprites.json    桌面程式內嵌用
- preview_standalone.html        單一檔案版預覽，可直接雙擊開啟

用法: python tools/build_js.py
"""
import json, glob, os
os.chdir(os.path.join(os.path.dirname(__file__), '..', 'sprites'))
out = {}
rows = json.load(open('stand_rows.json'))
top = [i for j in range(25) for i, ch in enumerate(rows[j]) if ch != '.']
out['stand'] = [{'rows': rows, 'ax': round(sum(top)/len(top), 1)}]
for f in sorted(glob.glob('*.json')):
    if f == 'stand_rows.json': continue
    out[f[:-5]] = json.load(open(f))
# 桌面程式內嵌用
json.dump(out, open(os.path.join('..', 'DobyPet', 'Assets', 'sprites.json'), 'w', encoding='utf-8'), separators=(',', ':'))
open('sprites.js', 'w', encoding='utf-8').write('window.SPRITES=' + json.dumps(out, separators=(',', ':')) + ';\n')
print({k: len(v) for k, v in out.items()})

# 單一檔案版預覽：把 sprites.js 直接塞進 preview.html
root = os.path.join('..')
html = open(os.path.join(root, 'preview.html'), encoding='utf-8').read()
js = open('sprites.js', encoding='utf-8').read()
html = html.replace('<script src="sprites/sprites.js"></script>', '<script>' + js + '</script>')
open(os.path.join(root, 'preview_standalone.html'), 'w', encoding='utf-8').write(html)
