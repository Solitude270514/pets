"""從動作表裁出第一隻（站姿）當作參考圖，給 Gemini 生走路圖時附上。

只附單獨一隻狗，Gemini 就不會把整張動作表拿來「修改」，而是重新畫一張走路圖。

用法：python tools/crop_ref.py shiba corgi golden husky dachshund
產出：art/<品種>/stand_reference.png（原圖解析度，四周留洋紅色空白）
"""
import sys, os
from PIL import Image
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import extract as ex
from import_breed import find, ROOT

sys.stdout.reconfigure(encoding='utf-8')


def crop(breed):
    art = os.path.join(ROOT, 'art', breed)
    path = find(art, 'sheet')
    cells, fg, p, (px, py) = ex.load_cells(path, with_origin=True)
    blobs = ex.find_sprites(fg, gap=1)
    if not blobs: sys.exit(f'{breed}: 動作表裡找不到狗')
    y0, x0, m = blobs[0]
    pad = 4   # 四周多留幾格
    left, top = px + (x0 - pad) * p, py + (y0 - pad) * p
    right, bottom = px + (x0 + m.shape[1] + pad) * p, py + (y0 + m.shape[0] + pad) * p
    src = Image.open(path).convert('RGB')
    box = tuple(int(round(v)) for v in (max(0, left), max(0, top), min(src.width, right), min(src.height, bottom)))
    out = Image.new('RGB', (box[2] - box[0], box[3] - box[1]), (255, 0, 255))
    # 只貼這隻狗的像素，旁邊別隻狗被框進來的部分保持洋紅色
    piece = src.crop(box)
    mask = Image.new('L', piece.size, 0)
    for j in range(m.shape[0]):
        for i in range(m.shape[1]):
            if m[j, i]:
                cx, cy = px + (x0 + i) * p - box[0], py + (y0 + j) * p - box[1]
                mask.paste(255, (int(cx), int(cy), int(cx + p + 1), int(cy + p + 1)))
    out.paste(piece, (0, 0), mask)
    dst = os.path.join(art, 'stand_reference.png')
    out.save(dst)
    print(f'{breed}: {dst}（{out.width}×{out.height}）')


if __name__ == '__main__':
    for b in sys.argv[1:]: crop(b)
