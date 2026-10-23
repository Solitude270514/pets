"""從品種的原始調色盤（sprites/<品種>/palette.json）產生多種毛色，寫進 meta.json 的 coats。

做法：每個品種指定哪些顏色代號是「毛色」，換毛色時只改這些顏色——
依原本的明暗在毛色裡的相對位置，對應到新毛色「由深到淺」的色階，所以陰影、亮部的層次都會保留。
輪廓、眼睛、鼻子、粉紅耳朵、舌頭、狗碗等其他顏色不動。

用法：python tools/make_coats.py            # 全部品種
      python tools/make_coats.py shiba      # 只做柴犬
重新匯入品種（import_breed.py）後要再跑一次。
"""
import json, os, sys, colorsys

sys.stdout.reconfigure(encoding='utf-8')
ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')

# 品種 → (要換色的顏色代號, 原色名稱, {新毛色: (最深, 最淺)})
SPECS = {
    'shiba': ('245bc', '赤柴', {
        '黑柴':   ('#141012', '#4a4044'),
        '芝麻柴': ('#4a2c1e', '#b0784c'),
        '白柴':   ('#c8a274', '#f4e2bc'),
    }),
    'corgi': ('147', '紅白', {
        '黃白': ('#8a5a26', '#eab468'),
        '三色': ('#101012', '#3e3c40'),
        '深紅': ('#4e1a0e', '#b03e1a'),
    }),
    'golden': ('3478bcdefgh', '金黃', {
        '奶油':   ('#8c6c4a', '#faf0d8'),
        '深紅金': ('#4a200e', '#e08c48'),
    }),
    'husky': ('256', '灰白', {
        '黑白': ('#0e1012', '#2e3238'),
        '紅白': ('#4a2414', '#a85c34'),
        '銀白': ('#9ca2a8', '#d6dadf'),
    }),
    'dachshund': ('234589af', '紅棕', {
        '黑色':   ('#221a18', '#564842'),   # 太黑會跟黑眼珠、輪廓混在一起
        '巧克力': ('#2e160c', '#7a4a2c'),
        '奶油':   ('#9c6a3c', '#f0c88e'),
    }),
}


def rgb(h): return tuple(int(h[i:i + 2], 16) for i in (1, 3, 5))
def hexc(c): return '#%02x%02x%02x' % tuple(max(0, min(255, round(v))) for v in c)
def light(h): return colorsys.rgb_to_hls(*(v / 255 for v in rgb(h)))[1]


def make(breed):
    letters, base_name, variants = SPECS[breed]
    folder = os.path.join(ROOT, 'sprites', breed)
    palette = json.load(open(os.path.join(folder, 'palette.json'), encoding='utf-8'))
    meta_path = os.path.join(folder, 'meta.json')
    meta = json.load(open(meta_path, encoding='utf-8'))

    coat_keys = [k for k in letters if k in palette]
    ls = {k: light(palette[k]) for k in coat_keys}
    lo, hi = min(ls.values()), max(ls.values())

    coats = {base_name: dict(palette)}
    for name, (dark, bright) in variants.items():
        d, b = rgb(dark), rgb(bright)
        p = dict(palette)
        for k in coat_keys:
            t = (ls[k] - lo) / (hi - lo) if hi > lo else .5   # 原本在毛色裡有多亮（0 最暗、1 最亮）
            p[k] = hexc([d[i] + (b[i] - d[i]) * t for i in range(3)])
        coats[name] = p
    meta['coats'] = coats
    meta['defaultCoat'] = base_name
    json.dump(meta, open(meta_path, 'w', encoding='utf-8'), ensure_ascii=False, indent=2)
    print(f'{breed}: ' + '、'.join(coats))


if __name__ == '__main__':
    for b in sys.argv[1:] or SPECS: make(b)
