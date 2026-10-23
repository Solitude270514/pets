# 新品種 Gemini 提示詞

## ⚡ 快速版：每個品種只要 2 張圖（建議）

一張「動作表」畫 6 個姿勢，一張「走路圖」畫 4 格。放進 `art/<品種代號>/`：

| 檔名 | 內容 |
|---|---|
| `sheet.jpg`（或 .png/.webp） | 動作表：兩排三列共 6 個姿勢 |
| `walk_sheet.jpg` | 走路分解圖：一排 4 格 |
| `import.json`（選用） | `{"flip": ["stand", ...], "walkFlip": false, "walkLegsOnly": false}`：Gemini 畫成**面向右邊**的姿勢列在 `flip`，匯入時翻成面向左；走路各格的頭轉向不一致（走起來一卡一卡）時，`walkLegsOnly` 設 `true`，只保留各格的腿 |

品種代號：`shiba`（柴犬）、`corgi`（柯基）、`golden`（黃金獵犬）、`husky`（哈士奇）、`dachshund`（短毛臘腸）。

### 第 1 張：動作表

附上 `art/doberman/stand_reference.webp`（只用來對齊畫風），貼上：

```
Use the attached image ONLY as a STYLE reference: same pixel-art style, same pixel size, same dark outline and shading, same cute puppy proportions.
Create a character sprite sheet of {品種描述}

Layout: exactly 6 poses of the SAME puppy in a grid of 2 rows × 3 columns, evenly spaced, with LARGE empty gaps between poses — no pose may touch or overlap another.
Row 1:
 (1) standing, 3/4 view facing left, head turned toward the viewer
 (2) sitting on its haunches, front legs straight, 3/4 view facing left
 (3) lying down asleep, eyes closed, chin resting on its front paws, side view facing left
Row 2:
 (4) standing and eating from a small round blue dog bowl on the ground, head lowered into the bowl, side view facing left (the bowl touches the muzzle)
 (5) squatting with its back hunched and tail raised, looking a little embarrassed, side view facing left (do not draw any poop)
 (6) a big play-bow stretch: front legs stretched forward, chest low, rear end raised high, mouth open in a yawn, side view facing left

All six poses: identical design, colors and markings, the same pixel size and the same scale (the dog is the same size in every pose), all facing left.
Pixel art with crisp hard-edged pixels, dark outline, no anti-aliasing, no grid lines.
Background: one flat solid magenta color (#FF00FF) only — no scenery, no ground, no shadows, no text, no labels, no numbers, no borders.
```

### 第 2 張：走路圖

先從動作表裁出站姿參考圖（只附一隻狗，Gemini 才不會把整張動作表拿來改）：

```bash
python tools/crop_ref.py shiba
```

會產生 `art/shiba/stand_reference.png`。附上**這張**（不要附整張動作表），貼上：

```
Generate a brand-new image from scratch. Do NOT edit, modify or extend the attached image — it is only a reference for what the puppy looks like.
Draw the SAME puppy (identical design, colors, markings, same pixel size and same scale as the reference) as a walk cycle:
exactly 4 frames in ONE horizontal row, evenly spaced with clear gaps between frames, and nothing else in the image.
All 4 frames face LEFT (mirror the reference if it faces right).
Frame 1: contact pose, Frame 2: passing pose, Frame 3: opposite contact pose, Frame 4: opposite passing pose.
Legs move naturally like a trotting dog; body, head and tail stay identical between frames except for a slight up-down bounce.
Wide horizontal image. Pixel art with crisp hard-edged pixels, no anti-aliasing, no grid lines.
Background: one flat solid magenta color (#FF00FF) only — no scenery, no shadows, no text, no labels.
```

如果 Gemini 還是畫成**面向右邊**，不用重生：把 `art/<品種>/import.json` 的 `"walkFlip"` 改成 `true`，匯入時會自動翻轉。
如果圖裡混進了其他姿勢，匯入工具會依閱讀順序只取前 4 格，再看總覽圖確認即可。

### 品種描述（替換 `{品種描述}`）

- **柴犬 shiba**：`a Shiba Inu puppy with a red-orange coat and cream-white markings (urajiro) on the cheeks, muzzle, throat, chest, belly and inner legs, small thick triangular upright ears, a black nose, and a fluffy tail curled over its back.`
- **柯基 corgi**：`a Pembroke Welsh Corgi puppy with very short legs and a long body, large upright pointed ears, a fox-like face, a red-and-white coat with a white blaze on the face, white collar, chest and legs, and a fluffy round bottom with no visible tail.`
- **黃金獵犬 golden**：`a Golden Retriever puppy with a soft wavy golden coat, floppy ears, a gentle happy face, feathered fur on the chest and legs, and a fluffy feathered tail.`
- **哈士奇 husky**：`a Siberian Husky puppy with a grey-and-white coat, a white face with the classic dark mask pattern, bright blue eyes, upright triangular ears, and a fluffy tail curled over its back.`
- **短毛臘腸 dachshund**：`a smooth short-haired Dachshund puppy with a very long body and very short legs, long floppy ears, a long muzzle, a shiny reddish-brown coat, and a long thin tail.`

### 檢查重點（不合格就重生）
- 動作表剛好 **6 隻**，排成 2 排 × 3 列，**彼此沒有碰在一起**，順序對（站、坐、睡 / 吃、便便、伸懶腰）
- 沒有文字、編號、外框、陰影
- 走路圖 4 格都**面向左邊**
- 6 個姿勢的狗看起來是同一隻、同樣大小

圖放好後跟 Claude 說「`shiba` 的圖放好了」，或自己執行：

```bash
python tools/import_breed.py shiba
```

會產生 `sprites/shiba/overview.png` 總覽圖，切錯或少一隻時會印出警告。

---

## 🐢 逐張版：每個品種 7 張圖（動作表一直生不好時的備案）

每個品種 7 張圖放進 `art/<品種代號>/`：`stand_reference`、`sit`、`sleep`、`eat`、`walk_sheet`、`poop`、`stretch`。逐張版需要在 `sprites/<品種>/` 下用 `tools/extract.py` 一張張轉換。

### 第 1 步：站姿參考圖

附上 `art/doberman/stand_reference.webp`，貼上：

```
Use the attached image ONLY as a STYLE reference: same pixel-art style, same pixel size, same dark outline, same shading, same 3/4 view standing pose facing left, same cute puppy proportions and roughly the same size in the frame.
Draw a different dog: {品種描述}
Background: one flat solid magenta color (#FF00FF) only — no scenery, no ground, no shadow, no text, no grid lines.
Full body visible, centered, with empty space around it.
```

### 第 2 步：其他 6 個姿勢

附上**該品種的** `stand_reference`，先貼共用設定，再接一個姿勢（一次一個）。

共用設定：
```
Using the attached image as the character reference, draw the SAME puppy (identical design, colors, markings, ear shape and tail).
Style: pixel art, same pixel size and same level of detail as the reference, crisp hard-edged pixels, dark outline, no anti-aliasing, no grid lines.
Background: one flat solid magenta color (#FF00FF) only — no scenery, no ground, no shadow, no text.
Show the full body, not cropped, centered, with empty space around it. The dog is about the same size in the frame as in the reference.
```

- **sit**：`Pose: the puppy is sitting on its haunches, front legs straight, facing the same direction as the reference (3/4 view, head turned toward the viewer), looking happy and attentive.`
- **sleep**：`Pose: the puppy is lying down asleep, curled slightly, eyes closed, chin resting on its front paws, ears relaxed, side view facing left.`
- **eat**：`Pose: the puppy is standing and eating from a small round blue dog bowl on the ground, head lowered into the bowl, side view facing left. Include the bowl.`
- **walk_sheet**：`Create a walk cycle sprite sheet: 4 frames of the puppy walking, arranged in ONE horizontal row, evenly spaced, same size in every frame, all facing left. Frame 1: contact pose, Frame 2: passing pose, Frame 3: opposite contact pose, Frame 4: opposite passing pose. Legs should move naturally like a trotting dog. The body, head and tail must stay identical between frames except for slight up-down bounce.`
- **poop**：`Pose: the puppy is squatting with its back hunched and tail raised, looking a little embarrassed, side view facing left. Do not draw the poop.`
- **stretch**：`Pose: the puppy is doing a big play-bow stretch — front legs stretched forward and chest low to the ground, rear end raised high, tail up, mouth open in a yawn, side view facing left.`

走樣時的修正：
```
The dog does not match the reference. Keep the character EXACTLY like the attached image: same face shape, same markings in the same places, same ear shape, same tail, same colors and same pixel size. Only change the pose.
```
