# 新行為的 Gemini 提示詞（逐張版）

這批是 v0.6.0 那些新行為要用的專屬圖。一次只畫一個動作，畫質比一次畫 6 格好。

## 怎麼用

1. 每個品種開**一個新的 Gemini 對話**。
2. 第一則訊息附上該品種的參考圖（下面每個品種都有標出路徑），貼上第 1 段提示詞。
3. 同一個對話裡直接貼下一段，不用再附圖（提示詞裡已經寫了「參考圖在前面」）；Gemini 開始走樣的話，再附一次參考圖。
4. 每張圖存成 `art/<品種>/poses/<動作>.png`，檔名用每段標題括號裡的英文，例如 `art/shiba/poses/sniff.png`。

| 動作 | 檔名 | 程式裡的用途 |
|---|---|---|
| 聞地板 | `sniff` | 低頭沿著地板聞、聞游標 |
| 追尾巴 | `chase` | 原地轉圈追尾巴 |
| 頭暈 | `dizzy` | 轉太多圈頭暈（星星由程式畫） |
| 喘氣 | `pant` | 暴衝跑完坐著喘 |
| 抖毛 | `shake` | 睡醒抖抖毛。**一排 4 格動畫**：從頭甩到尾像電風扇，程式會快速循環播放，最後慢下來停住 |
| 側躺睡 | `sidesleep` | 睡久了翻身、做夢踢腳 |

### 檢查重點（不合格就重生）
- 只有**一隻**狗，**整個頭和鼻子都完整**，身體沒被切掉（抖毛是例外：剛好 **4 格排成一排**，4 格的**腳都站在同一個位置**）
- **面向左邊**（畫成面向右也沒關係，跟我說一聲，匯入時會翻過來）
- 跟參考圖是同一隻：花色、斑紋位置、耳朵、尾巴都一樣，大小也差不多
- 背景是純洋紅色，沒有文字、地面、陰影、星星或動作線

走樣時接著貼這句：
```
The dog does not match the reference. Keep the character EXACTLY like the reference image: same face shape, same markings in the same places, same ear shape, same tail, same colors and same pixel size. Only change the pose.
```
頭被切掉或沒畫完整時：
```
The head is cut off or incomplete. Redraw the same pose with the whole head, muzzle and nose fully visible inside the image, with empty space around the dog.
```

---

## 杜賓 `doberman`

參考圖：`art/doberman/stand_reference.webp`　→　存到 `art/doberman/poses/`

### 1. 聞地板（`sniff`）

```
Generate a brand-new image from scratch. Do NOT edit, modify or extend the reference image — it only shows what the puppy looks like (it is attached, or I attached it earlier in this chat).
Draw the SAME puppy as the reference: a Doberman Pinscher puppy with a glossy black coat, rust-tan markings on the eyebrows, muzzle, chest and legs, upright pointed ears, and a short docked tail.
Keep the identical design, colors, markings, ear shape and tail, the same pixel size and the same scale as the reference.
Pose: the puppy is standing on all four legs and sniffing the ground: the head is lowered all the way down so the tip of the nose touches the ground just in front of its front paws, ears hanging forward, tail relaxed. The WHOLE head, muzzle and nose must be fully drawn and clearly visible. Side view facing LEFT. No bowl, no food, no objects.
Exactly ONE puppy in the image, full body visible and not cropped, centered, with empty space around it.
Pixel art with crisp hard-edged pixels, dark outline, no anti-aliasing, no grid lines.
Background: one flat solid magenta color (#FF00FF) only — no scenery, no ground, no shadows, no motion lines, no effects, no text, no labels, no borders.
```

### 2. 追尾巴（`chase`）

```
Generate a brand-new image from scratch. Do NOT edit, modify or extend the reference image — it only shows what the puppy looks like (it is attached, or I attached it earlier in this chat).
Draw the SAME puppy as the reference: a Doberman Pinscher puppy with a glossy black coat, rust-tan markings on the eyebrows, muzzle, chest and legs, upright pointed ears, and a short docked tail.
Keep the identical design, colors, markings, ear shape and tail, the same pixel size and the same scale as the reference.
Pose: the puppy is spinning around chasing its own short stubby tail: the body is bent into a tight C-shaped curve, the head is turned back toward the rear end, mouth open, excited playful eyes, one front paw lifted off the ground. Side view, head on the LEFT side of the body.
Exactly ONE puppy in the image, full body visible and not cropped, centered, with empty space around it.
Pixel art with crisp hard-edged pixels, dark outline, no anti-aliasing, no grid lines.
Background: one flat solid magenta color (#FF00FF) only — no scenery, no ground, no shadows, no motion lines, no effects, no text, no labels, no borders.
```

### 3. 頭暈（`dizzy`）

```
Generate a brand-new image from scratch. Do NOT edit, modify or extend the reference image — it only shows what the puppy looks like (it is attached, or I attached it earlier in this chat).
Draw the SAME puppy as the reference: a Doberman Pinscher puppy with a glossy black coat, rust-tan markings on the eyebrows, muzzle, chest and legs, upright pointed ears, and a short docked tail.
Keep the identical design, colors, markings, ear shape and tail, the same pixel size and the same scale as the reference.
Pose: the puppy is dizzy after spinning around: standing on wobbly legs spread a little apart, body leaning slightly to one side, eyes drawn as small spirals, tongue sticking out, a silly dazed expression. 3/4 view facing LEFT. Do NOT draw stars, birds or any effects around the head.
Exactly ONE puppy in the image, full body visible and not cropped, centered, with empty space around it.
Pixel art with crisp hard-edged pixels, dark outline, no anti-aliasing, no grid lines.
Background: one flat solid magenta color (#FF00FF) only — no scenery, no ground, no shadows, no motion lines, no effects, no text, no labels, no borders.
```

### 4. 喘氣（`pant`）

```
Generate a brand-new image from scratch. Do NOT edit, modify or extend the reference image — it only shows what the puppy looks like (it is attached, or I attached it earlier in this chat).
Draw the SAME puppy as the reference: a Doberman Pinscher puppy with a glossy black coat, rust-tan markings on the eyebrows, muzzle, chest and legs, upright pointed ears, and a short docked tail.
Keep the identical design, colors, markings, ear shape and tail, the same pixel size and the same scale as the reference.
Pose: the puppy is sitting on its haunches and panting after running: front legs straight, mouth wide open, pink tongue hanging out, happy tired eyes. 3/4 view facing LEFT, the same sitting posture as a normal sit.
Exactly ONE puppy in the image, full body visible and not cropped, centered, with empty space around it.
Pixel art with crisp hard-edged pixels, dark outline, no anti-aliasing, no grid lines.
Background: one flat solid magenta color (#FF00FF) only — no scenery, no ground, no shadows, no motion lines, no effects, no text, no labels, no borders.
```

### 5. 抖毛（`shake`，一排 4 格動畫）

```
Generate a brand-new image from scratch. Do NOT edit, modify or extend the reference image — it only shows what the puppy looks like (it is attached, or I attached it earlier in this chat).
Draw the SAME puppy as the reference: a Doberman Pinscher puppy with a glossy black coat, rust-tan markings on the eyebrows, muzzle, chest and legs, upright pointed ears, and a short docked tail.
Keep the identical design, colors, markings, ear shape and tail, the same pixel size and the same scale as the reference.
Create a 4-frame animation of the puppy shaking its whole body like a wet dog, arranged in ONE horizontal row, evenly spaced with clear gaps between frames, and nothing else in the image.
All 4 frames: side view facing LEFT, the same size, standing with legs braced apart — the four paws stay in exactly the same place in every frame.
The shake twists through the body from head to tail like a spinning fan:
Frame 1: the head and ears whip toward the viewer, ears flying out to the side, the fur or loose skin on the neck swinging the same way; the body is still straight.
Frame 2: the twist moves to the chest and middle of the body — shoulders and belly swing toward the viewer while the head is already turning back and the ears fly the other way.
Frame 3: the head and ears whip away from the viewer, ears flying in the opposite direction, the neck fur swinging away.
Frame 4: the twist reaches the rear — hips and tail swing, the fur on the back and haunches puffs out, and the head starts coming back toward the viewer.
Eyes squeezed shut in all frames, fur ruffled and puffed up.
Do NOT draw water drops, sweat, motion lines, blur or speed effects.
Wide horizontal image. Pixel art with crisp hard-edged pixels, dark outline, no anti-aliasing, no grid lines.
Background: one flat solid magenta color (#FF00FF) only — no scenery, no ground, no shadows, no text, no labels, no numbers, no borders.
```

### 6. 側躺睡（`sidesleep`）

```
Generate a brand-new image from scratch. Do NOT edit, modify or extend the reference image — it only shows what the puppy looks like (it is attached, or I attached it earlier in this chat).
Draw the SAME puppy as the reference: a Doberman Pinscher puppy with a glossy black coat, rust-tan markings on the eyebrows, muzzle, chest and legs, upright pointed ears, and a short docked tail.
Keep the identical design, colors, markings, ear shape and tail, the same pixel size and the same scale as the reference.
Pose: the puppy is fast asleep lying flat on its side: the side of its body rests on the ground, all four legs stretched out loosely in front of its belly, the head resting flat on the ground, eyes closed, totally relaxed. Seen from the side with the head on the LEFT.
Exactly ONE puppy in the image, full body visible and not cropped, centered, with empty space around it.
Pixel art with crisp hard-edged pixels, dark outline, no anti-aliasing, no grid lines.
Background: one flat solid magenta color (#FF00FF) only — no scenery, no ground, no shadows, no motion lines, no effects, no text, no labels, no borders.
```

---

## 柴犬 `shiba`

參考圖：`art/shiba/stand_reference.png`　→　存到 `art/shiba/poses/`

### 1. 聞地板（`sniff`）

```
Generate a brand-new image from scratch. Do NOT edit, modify or extend the reference image — it only shows what the puppy looks like (it is attached, or I attached it earlier in this chat).
Draw the SAME puppy as the reference: a Shiba Inu puppy with a red-orange coat and cream-white markings (urajiro) on the cheeks, muzzle, throat, chest, belly and inner legs, small thick triangular upright ears, a black nose, and a fluffy tail curled over its back.
Keep the identical design, colors, markings, ear shape and tail, the same pixel size and the same scale as the reference.
Pose: the puppy is standing on all four legs and sniffing the ground: the head is lowered all the way down so the tip of the nose touches the ground just in front of its front paws, ears hanging forward, tail relaxed. The WHOLE head, muzzle and nose must be fully drawn and clearly visible. Side view facing LEFT. No bowl, no food, no objects.
Exactly ONE puppy in the image, full body visible and not cropped, centered, with empty space around it.
Pixel art with crisp hard-edged pixels, dark outline, no anti-aliasing, no grid lines.
Background: one flat solid magenta color (#FF00FF) only — no scenery, no ground, no shadows, no motion lines, no effects, no text, no labels, no borders.
```

### 2. 追尾巴（`chase`）

```
Generate a brand-new image from scratch. Do NOT edit, modify or extend the reference image — it only shows what the puppy looks like (it is attached, or I attached it earlier in this chat).
Draw the SAME puppy as the reference: a Shiba Inu puppy with a red-orange coat and cream-white markings (urajiro) on the cheeks, muzzle, throat, chest, belly and inner legs, small thick triangular upright ears, a black nose, and a fluffy tail curled over its back.
Keep the identical design, colors, markings, ear shape and tail, the same pixel size and the same scale as the reference.
Pose: the puppy is spinning around chasing its own tail: the body is bent into a tight C-shaped curve, the head is turned back toward the rear end, mouth open, excited playful eyes, one front paw lifted off the ground. Side view, head on the LEFT side of the body.
Exactly ONE puppy in the image, full body visible and not cropped, centered, with empty space around it.
Pixel art with crisp hard-edged pixels, dark outline, no anti-aliasing, no grid lines.
Background: one flat solid magenta color (#FF00FF) only — no scenery, no ground, no shadows, no motion lines, no effects, no text, no labels, no borders.
```

### 3. 頭暈（`dizzy`）

```
Generate a brand-new image from scratch. Do NOT edit, modify or extend the reference image — it only shows what the puppy looks like (it is attached, or I attached it earlier in this chat).
Draw the SAME puppy as the reference: a Shiba Inu puppy with a red-orange coat and cream-white markings (urajiro) on the cheeks, muzzle, throat, chest, belly and inner legs, small thick triangular upright ears, a black nose, and a fluffy tail curled over its back.
Keep the identical design, colors, markings, ear shape and tail, the same pixel size and the same scale as the reference.
Pose: the puppy is dizzy after spinning around: standing on wobbly legs spread a little apart, body leaning slightly to one side, eyes drawn as small spirals, tongue sticking out, a silly dazed expression. 3/4 view facing LEFT. Do NOT draw stars, birds or any effects around the head.
Exactly ONE puppy in the image, full body visible and not cropped, centered, with empty space around it.
Pixel art with crisp hard-edged pixels, dark outline, no anti-aliasing, no grid lines.
Background: one flat solid magenta color (#FF00FF) only — no scenery, no ground, no shadows, no motion lines, no effects, no text, no labels, no borders.
```

### 4. 喘氣（`pant`）

```
Generate a brand-new image from scratch. Do NOT edit, modify or extend the reference image — it only shows what the puppy looks like (it is attached, or I attached it earlier in this chat).
Draw the SAME puppy as the reference: a Shiba Inu puppy with a red-orange coat and cream-white markings (urajiro) on the cheeks, muzzle, throat, chest, belly and inner legs, small thick triangular upright ears, a black nose, and a fluffy tail curled over its back.
Keep the identical design, colors, markings, ear shape and tail, the same pixel size and the same scale as the reference.
Pose: the puppy is sitting on its haunches and panting after running: front legs straight, mouth wide open, pink tongue hanging out, happy tired eyes. 3/4 view facing LEFT, the same sitting posture as a normal sit.
Exactly ONE puppy in the image, full body visible and not cropped, centered, with empty space around it.
Pixel art with crisp hard-edged pixels, dark outline, no anti-aliasing, no grid lines.
Background: one flat solid magenta color (#FF00FF) only — no scenery, no ground, no shadows, no motion lines, no effects, no text, no labels, no borders.
```

### 5. 抖毛（`shake`，一排 4 格動畫）

```
Generate a brand-new image from scratch. Do NOT edit, modify or extend the reference image — it only shows what the puppy looks like (it is attached, or I attached it earlier in this chat).
Draw the SAME puppy as the reference: a Shiba Inu puppy with a red-orange coat and cream-white markings (urajiro) on the cheeks, muzzle, throat, chest, belly and inner legs, small thick triangular upright ears, a black nose, and a fluffy tail curled over its back.
Keep the identical design, colors, markings, ear shape and tail, the same pixel size and the same scale as the reference.
Create a 4-frame animation of the puppy shaking its whole body like a wet dog, arranged in ONE horizontal row, evenly spaced with clear gaps between frames, and nothing else in the image.
All 4 frames: side view facing LEFT, the same size, standing with legs braced apart — the four paws stay in exactly the same place in every frame.
The shake twists through the body from head to tail like a spinning fan:
Frame 1: the head and ears whip toward the viewer, ears flying out to the side, the fur or loose skin on the neck swinging the same way; the body is still straight.
Frame 2: the twist moves to the chest and middle of the body — shoulders and belly swing toward the viewer while the head is already turning back and the ears fly the other way.
Frame 3: the head and ears whip away from the viewer, ears flying in the opposite direction, the neck fur swinging away.
Frame 4: the twist reaches the rear — hips and tail swing, the fur on the back and haunches puffs out, and the head starts coming back toward the viewer.
Eyes squeezed shut in all frames, fur ruffled and puffed up.
Do NOT draw water drops, sweat, motion lines, blur or speed effects.
Wide horizontal image. Pixel art with crisp hard-edged pixels, dark outline, no anti-aliasing, no grid lines.
Background: one flat solid magenta color (#FF00FF) only — no scenery, no ground, no shadows, no text, no labels, no numbers, no borders.
```

### 6. 側躺睡（`sidesleep`）

```
Generate a brand-new image from scratch. Do NOT edit, modify or extend the reference image — it only shows what the puppy looks like (it is attached, or I attached it earlier in this chat).
Draw the SAME puppy as the reference: a Shiba Inu puppy with a red-orange coat and cream-white markings (urajiro) on the cheeks, muzzle, throat, chest, belly and inner legs, small thick triangular upright ears, a black nose, and a fluffy tail curled over its back.
Keep the identical design, colors, markings, ear shape and tail, the same pixel size and the same scale as the reference.
Pose: the puppy is fast asleep lying flat on its side: the side of its body rests on the ground, all four legs stretched out loosely in front of its belly, the head resting flat on the ground, eyes closed, totally relaxed. Seen from the side with the head on the LEFT.
Exactly ONE puppy in the image, full body visible and not cropped, centered, with empty space around it.
Pixel art with crisp hard-edged pixels, dark outline, no anti-aliasing, no grid lines.
Background: one flat solid magenta color (#FF00FF) only — no scenery, no ground, no shadows, no motion lines, no effects, no text, no labels, no borders.
```

---

## 柯基 `corgi`

參考圖：`art/corgi/stand_reference.png`　→　存到 `art/corgi/poses/`

### 1. 聞地板（`sniff`）

```
Generate a brand-new image from scratch. Do NOT edit, modify or extend the reference image — it only shows what the puppy looks like (it is attached, or I attached it earlier in this chat).
Draw the SAME puppy as the reference: a Pembroke Welsh Corgi puppy with very short legs and a long body, large upright pointed ears, a fox-like face, a red-and-white coat with a white blaze on the face, white collar, chest and legs, and a fluffy round bottom with no visible tail.
Keep the identical design, colors, markings, ear shape and tail, the same pixel size and the same scale as the reference.
Pose: the puppy is standing on all four legs and sniffing the ground: the head is lowered all the way down so the tip of the nose touches the ground just in front of its front paws, ears hanging forward, tail relaxed. The WHOLE head, muzzle and nose must be fully drawn and clearly visible. Side view facing LEFT. No bowl, no food, no objects.
Exactly ONE puppy in the image, full body visible and not cropped, centered, with empty space around it.
Pixel art with crisp hard-edged pixels, dark outline, no anti-aliasing, no grid lines.
Background: one flat solid magenta color (#FF00FF) only — no scenery, no ground, no shadows, no motion lines, no effects, no text, no labels, no borders.
```

### 2. 追尾巴（`chase`）

```
Generate a brand-new image from scratch. Do NOT edit, modify or extend the reference image — it only shows what the puppy looks like (it is attached, or I attached it earlier in this chat).
Draw the SAME puppy as the reference: a Pembroke Welsh Corgi puppy with very short legs and a long body, large upright pointed ears, a fox-like face, a red-and-white coat with a white blaze on the face, white collar, chest and legs, and a fluffy round bottom with no visible tail.
Keep the identical design, colors, markings, ear shape and tail, the same pixel size and the same scale as the reference.
Pose: the puppy is spinning around chasing its own fluffy round bottom (it has no visible tail): the body is bent into a tight C-shaped curve, the head is turned back toward the rear end, mouth open, excited playful eyes, one front paw lifted off the ground. Side view, head on the LEFT side of the body.
Exactly ONE puppy in the image, full body visible and not cropped, centered, with empty space around it.
Pixel art with crisp hard-edged pixels, dark outline, no anti-aliasing, no grid lines.
Background: one flat solid magenta color (#FF00FF) only — no scenery, no ground, no shadows, no motion lines, no effects, no text, no labels, no borders.
```

### 3. 頭暈（`dizzy`）

```
Generate a brand-new image from scratch. Do NOT edit, modify or extend the reference image — it only shows what the puppy looks like (it is attached, or I attached it earlier in this chat).
Draw the SAME puppy as the reference: a Pembroke Welsh Corgi puppy with very short legs and a long body, large upright pointed ears, a fox-like face, a red-and-white coat with a white blaze on the face, white collar, chest and legs, and a fluffy round bottom with no visible tail.
Keep the identical design, colors, markings, ear shape and tail, the same pixel size and the same scale as the reference.
Pose: the puppy is dizzy after spinning around: standing on wobbly legs spread a little apart, body leaning slightly to one side, eyes drawn as small spirals, tongue sticking out, a silly dazed expression. 3/4 view facing LEFT. Do NOT draw stars, birds or any effects around the head.
Exactly ONE puppy in the image, full body visible and not cropped, centered, with empty space around it.
Pixel art with crisp hard-edged pixels, dark outline, no anti-aliasing, no grid lines.
Background: one flat solid magenta color (#FF00FF) only — no scenery, no ground, no shadows, no motion lines, no effects, no text, no labels, no borders.
```

### 4. 喘氣（`pant`）

```
Generate a brand-new image from scratch. Do NOT edit, modify or extend the reference image — it only shows what the puppy looks like (it is attached, or I attached it earlier in this chat).
Draw the SAME puppy as the reference: a Pembroke Welsh Corgi puppy with very short legs and a long body, large upright pointed ears, a fox-like face, a red-and-white coat with a white blaze on the face, white collar, chest and legs, and a fluffy round bottom with no visible tail.
Keep the identical design, colors, markings, ear shape and tail, the same pixel size and the same scale as the reference.
Pose: the puppy is sitting on its haunches and panting after running: front legs straight, mouth wide open, pink tongue hanging out, happy tired eyes. 3/4 view facing LEFT, the same sitting posture as a normal sit.
Exactly ONE puppy in the image, full body visible and not cropped, centered, with empty space around it.
Pixel art with crisp hard-edged pixels, dark outline, no anti-aliasing, no grid lines.
Background: one flat solid magenta color (#FF00FF) only — no scenery, no ground, no shadows, no motion lines, no effects, no text, no labels, no borders.
```

### 5. 抖毛（`shake`，一排 4 格動畫）

```
Generate a brand-new image from scratch. Do NOT edit, modify or extend the reference image — it only shows what the puppy looks like (it is attached, or I attached it earlier in this chat).
Draw the SAME puppy as the reference: a Pembroke Welsh Corgi puppy with very short legs and a long body, large upright pointed ears, a fox-like face, a red-and-white coat with a white blaze on the face, white collar, chest and legs, and a fluffy round bottom with no visible tail.
Keep the identical design, colors, markings, ear shape and tail, the same pixel size and the same scale as the reference.
Create a 4-frame animation of the puppy shaking its whole body like a wet dog, arranged in ONE horizontal row, evenly spaced with clear gaps between frames, and nothing else in the image.
All 4 frames: side view facing LEFT, the same size, standing with legs braced apart — the four paws stay in exactly the same place in every frame.
The shake twists through the body from head to tail like a spinning fan:
Frame 1: the head and ears whip toward the viewer, ears flying out to the side, the fur or loose skin on the neck swinging the same way; the body is still straight.
Frame 2: the twist moves to the chest and middle of the body — shoulders and belly swing toward the viewer while the head is already turning back and the ears fly the other way.
Frame 3: the head and ears whip away from the viewer, ears flying in the opposite direction, the neck fur swinging away.
Frame 4: the twist reaches the rear — hips and tail swing, the fur on the back and haunches puffs out, and the head starts coming back toward the viewer.
Eyes squeezed shut in all frames, fur ruffled and puffed up.
Do NOT draw water drops, sweat, motion lines, blur or speed effects.
Wide horizontal image. Pixel art with crisp hard-edged pixels, dark outline, no anti-aliasing, no grid lines.
Background: one flat solid magenta color (#FF00FF) only — no scenery, no ground, no shadows, no text, no labels, no numbers, no borders.
```

### 6. 側躺睡（`sidesleep`）

```
Generate a brand-new image from scratch. Do NOT edit, modify or extend the reference image — it only shows what the puppy looks like (it is attached, or I attached it earlier in this chat).
Draw the SAME puppy as the reference: a Pembroke Welsh Corgi puppy with very short legs and a long body, large upright pointed ears, a fox-like face, a red-and-white coat with a white blaze on the face, white collar, chest and legs, and a fluffy round bottom with no visible tail.
Keep the identical design, colors, markings, ear shape and tail, the same pixel size and the same scale as the reference.
Pose: the puppy is fast asleep lying flat on its side: the side of its body rests on the ground, all four legs stretched out loosely in front of its belly, the head resting flat on the ground, eyes closed, totally relaxed. Seen from the side with the head on the LEFT.
Exactly ONE puppy in the image, full body visible and not cropped, centered, with empty space around it.
Pixel art with crisp hard-edged pixels, dark outline, no anti-aliasing, no grid lines.
Background: one flat solid magenta color (#FF00FF) only — no scenery, no ground, no shadows, no motion lines, no effects, no text, no labels, no borders.
```

---

## 黃金獵犬 `golden`

參考圖：`art/golden/stand_reference.png`　→　存到 `art/golden/poses/`

### 1. 聞地板（`sniff`）

```
Generate a brand-new image from scratch. Do NOT edit, modify or extend the reference image — it only shows what the puppy looks like (it is attached, or I attached it earlier in this chat).
Draw the SAME puppy as the reference: a Golden Retriever puppy with a soft wavy golden coat, floppy ears, a gentle happy face, feathered fur on the chest and legs, and a fluffy feathered tail.
Keep the identical design, colors, markings, ear shape and tail, the same pixel size and the same scale as the reference.
Pose: the puppy is standing on all four legs and sniffing the ground: the head is lowered all the way down so the tip of the nose touches the ground just in front of its front paws, ears hanging forward, tail relaxed. The WHOLE head, muzzle and nose must be fully drawn and clearly visible. Side view facing LEFT. No bowl, no food, no objects.
Exactly ONE puppy in the image, full body visible and not cropped, centered, with empty space around it.
Pixel art with crisp hard-edged pixels, dark outline, no anti-aliasing, no grid lines.
Background: one flat solid magenta color (#FF00FF) only — no scenery, no ground, no shadows, no motion lines, no effects, no text, no labels, no borders.
```

### 2. 追尾巴（`chase`）

```
Generate a brand-new image from scratch. Do NOT edit, modify or extend the reference image — it only shows what the puppy looks like (it is attached, or I attached it earlier in this chat).
Draw the SAME puppy as the reference: a Golden Retriever puppy with a soft wavy golden coat, floppy ears, a gentle happy face, feathered fur on the chest and legs, and a fluffy feathered tail.
Keep the identical design, colors, markings, ear shape and tail, the same pixel size and the same scale as the reference.
Pose: the puppy is spinning around chasing its own tail: the body is bent into a tight C-shaped curve, the head is turned back toward the rear end, mouth open, excited playful eyes, one front paw lifted off the ground. Side view, head on the LEFT side of the body.
Exactly ONE puppy in the image, full body visible and not cropped, centered, with empty space around it.
Pixel art with crisp hard-edged pixels, dark outline, no anti-aliasing, no grid lines.
Background: one flat solid magenta color (#FF00FF) only — no scenery, no ground, no shadows, no motion lines, no effects, no text, no labels, no borders.
```

### 3. 頭暈（`dizzy`）

```
Generate a brand-new image from scratch. Do NOT edit, modify or extend the reference image — it only shows what the puppy looks like (it is attached, or I attached it earlier in this chat).
Draw the SAME puppy as the reference: a Golden Retriever puppy with a soft wavy golden coat, floppy ears, a gentle happy face, feathered fur on the chest and legs, and a fluffy feathered tail.
Keep the identical design, colors, markings, ear shape and tail, the same pixel size and the same scale as the reference.
Pose: the puppy is dizzy after spinning around: standing on wobbly legs spread a little apart, body leaning slightly to one side, eyes drawn as small spirals, tongue sticking out, a silly dazed expression. 3/4 view facing LEFT. Do NOT draw stars, birds or any effects around the head.
Exactly ONE puppy in the image, full body visible and not cropped, centered, with empty space around it.
Pixel art with crisp hard-edged pixels, dark outline, no anti-aliasing, no grid lines.
Background: one flat solid magenta color (#FF00FF) only — no scenery, no ground, no shadows, no motion lines, no effects, no text, no labels, no borders.
```

### 4. 喘氣（`pant`）

```
Generate a brand-new image from scratch. Do NOT edit, modify or extend the reference image — it only shows what the puppy looks like (it is attached, or I attached it earlier in this chat).
Draw the SAME puppy as the reference: a Golden Retriever puppy with a soft wavy golden coat, floppy ears, a gentle happy face, feathered fur on the chest and legs, and a fluffy feathered tail.
Keep the identical design, colors, markings, ear shape and tail, the same pixel size and the same scale as the reference.
Pose: the puppy is sitting on its haunches and panting after running: front legs straight, mouth wide open, pink tongue hanging out, happy tired eyes. 3/4 view facing LEFT, the same sitting posture as a normal sit.
Exactly ONE puppy in the image, full body visible and not cropped, centered, with empty space around it.
Pixel art with crisp hard-edged pixels, dark outline, no anti-aliasing, no grid lines.
Background: one flat solid magenta color (#FF00FF) only — no scenery, no ground, no shadows, no motion lines, no effects, no text, no labels, no borders.
```

### 5. 抖毛（`shake`，一排 4 格動畫）

```
Generate a brand-new image from scratch. Do NOT edit, modify or extend the reference image — it only shows what the puppy looks like (it is attached, or I attached it earlier in this chat).
Draw the SAME puppy as the reference: a Golden Retriever puppy with a soft wavy golden coat, floppy ears, a gentle happy face, feathered fur on the chest and legs, and a fluffy feathered tail.
Keep the identical design, colors, markings, ear shape and tail, the same pixel size and the same scale as the reference.
Create a 4-frame animation of the puppy shaking its whole body like a wet dog, arranged in ONE horizontal row, evenly spaced with clear gaps between frames, and nothing else in the image.
All 4 frames: side view facing LEFT, the same size, standing with legs braced apart — the four paws stay in exactly the same place in every frame.
The shake twists through the body from head to tail like a spinning fan:
Frame 1: the head and ears whip toward the viewer, ears flying out to the side, the fur or loose skin on the neck swinging the same way; the body is still straight.
Frame 2: the twist moves to the chest and middle of the body — shoulders and belly swing toward the viewer while the head is already turning back and the ears fly the other way.
Frame 3: the head and ears whip away from the viewer, ears flying in the opposite direction, the neck fur swinging away.
Frame 4: the twist reaches the rear — hips and tail swing, the fur on the back and haunches puffs out, and the head starts coming back toward the viewer.
Eyes squeezed shut in all frames, fur ruffled and puffed up.
Do NOT draw water drops, sweat, motion lines, blur or speed effects.
Wide horizontal image. Pixel art with crisp hard-edged pixels, dark outline, no anti-aliasing, no grid lines.
Background: one flat solid magenta color (#FF00FF) only — no scenery, no ground, no shadows, no text, no labels, no numbers, no borders.
```

### 6. 側躺睡（`sidesleep`）

```
Generate a brand-new image from scratch. Do NOT edit, modify or extend the reference image — it only shows what the puppy looks like (it is attached, or I attached it earlier in this chat).
Draw the SAME puppy as the reference: a Golden Retriever puppy with a soft wavy golden coat, floppy ears, a gentle happy face, feathered fur on the chest and legs, and a fluffy feathered tail.
Keep the identical design, colors, markings, ear shape and tail, the same pixel size and the same scale as the reference.
Pose: the puppy is fast asleep lying flat on its side: the side of its body rests on the ground, all four legs stretched out loosely in front of its belly, the head resting flat on the ground, eyes closed, totally relaxed. Seen from the side with the head on the LEFT.
Exactly ONE puppy in the image, full body visible and not cropped, centered, with empty space around it.
Pixel art with crisp hard-edged pixels, dark outline, no anti-aliasing, no grid lines.
Background: one flat solid magenta color (#FF00FF) only — no scenery, no ground, no shadows, no motion lines, no effects, no text, no labels, no borders.
```

---

## 哈士奇 `husky`

參考圖：`art/husky/stand_reference.png`　→　存到 `art/husky/poses/`

### 1. 聞地板（`sniff`）

```
Generate a brand-new image from scratch. Do NOT edit, modify or extend the reference image — it only shows what the puppy looks like (it is attached, or I attached it earlier in this chat).
Draw the SAME puppy as the reference: a Siberian Husky puppy with a grey-and-white coat, a white face with the classic dark mask pattern, bright blue eyes, upright triangular ears, and a fluffy tail curled over its back.
Keep the identical design, colors, markings, ear shape and tail, the same pixel size and the same scale as the reference.
Pose: the puppy is standing on all four legs and sniffing the ground: the head is lowered all the way down so the tip of the nose touches the ground just in front of its front paws, ears hanging forward, tail relaxed. The WHOLE head, muzzle and nose must be fully drawn and clearly visible. Side view facing LEFT. No bowl, no food, no objects.
Exactly ONE puppy in the image, full body visible and not cropped, centered, with empty space around it.
Pixel art with crisp hard-edged pixels, dark outline, no anti-aliasing, no grid lines.
Background: one flat solid magenta color (#FF00FF) only — no scenery, no ground, no shadows, no motion lines, no effects, no text, no labels, no borders.
```

### 2. 追尾巴（`chase`）

```
Generate a brand-new image from scratch. Do NOT edit, modify or extend the reference image — it only shows what the puppy looks like (it is attached, or I attached it earlier in this chat).
Draw the SAME puppy as the reference: a Siberian Husky puppy with a grey-and-white coat, a white face with the classic dark mask pattern, bright blue eyes, upright triangular ears, and a fluffy tail curled over its back.
Keep the identical design, colors, markings, ear shape and tail, the same pixel size and the same scale as the reference.
Pose: the puppy is spinning around chasing its own tail: the body is bent into a tight C-shaped curve, the head is turned back toward the rear end, mouth open, excited playful eyes, one front paw lifted off the ground. Side view, head on the LEFT side of the body.
Exactly ONE puppy in the image, full body visible and not cropped, centered, with empty space around it.
Pixel art with crisp hard-edged pixels, dark outline, no anti-aliasing, no grid lines.
Background: one flat solid magenta color (#FF00FF) only — no scenery, no ground, no shadows, no motion lines, no effects, no text, no labels, no borders.
```

### 3. 頭暈（`dizzy`）

```
Generate a brand-new image from scratch. Do NOT edit, modify or extend the reference image — it only shows what the puppy looks like (it is attached, or I attached it earlier in this chat).
Draw the SAME puppy as the reference: a Siberian Husky puppy with a grey-and-white coat, a white face with the classic dark mask pattern, bright blue eyes, upright triangular ears, and a fluffy tail curled over its back.
Keep the identical design, colors, markings, ear shape and tail, the same pixel size and the same scale as the reference.
Pose: the puppy is dizzy after spinning around: standing on wobbly legs spread a little apart, body leaning slightly to one side, eyes drawn as small spirals, tongue sticking out, a silly dazed expression. 3/4 view facing LEFT. Do NOT draw stars, birds or any effects around the head.
Exactly ONE puppy in the image, full body visible and not cropped, centered, with empty space around it.
Pixel art with crisp hard-edged pixels, dark outline, no anti-aliasing, no grid lines.
Background: one flat solid magenta color (#FF00FF) only — no scenery, no ground, no shadows, no motion lines, no effects, no text, no labels, no borders.
```

### 4. 喘氣（`pant`）

```
Generate a brand-new image from scratch. Do NOT edit, modify or extend the reference image — it only shows what the puppy looks like (it is attached, or I attached it earlier in this chat).
Draw the SAME puppy as the reference: a Siberian Husky puppy with a grey-and-white coat, a white face with the classic dark mask pattern, bright blue eyes, upright triangular ears, and a fluffy tail curled over its back.
Keep the identical design, colors, markings, ear shape and tail, the same pixel size and the same scale as the reference.
Pose: the puppy is sitting on its haunches and panting after running: front legs straight, mouth wide open, pink tongue hanging out, happy tired eyes. 3/4 view facing LEFT, the same sitting posture as a normal sit.
Exactly ONE puppy in the image, full body visible and not cropped, centered, with empty space around it.
Pixel art with crisp hard-edged pixels, dark outline, no anti-aliasing, no grid lines.
Background: one flat solid magenta color (#FF00FF) only — no scenery, no ground, no shadows, no motion lines, no effects, no text, no labels, no borders.
```

### 5. 抖毛（`shake`，一排 4 格動畫）

```
Generate a brand-new image from scratch. Do NOT edit, modify or extend the reference image — it only shows what the puppy looks like (it is attached, or I attached it earlier in this chat).
Draw the SAME puppy as the reference: a Siberian Husky puppy with a grey-and-white coat, a white face with the classic dark mask pattern, bright blue eyes, upright triangular ears, and a fluffy tail curled over its back.
Keep the identical design, colors, markings, ear shape and tail, the same pixel size and the same scale as the reference.
Create a 4-frame animation of the puppy shaking its whole body like a wet dog, arranged in ONE horizontal row, evenly spaced with clear gaps between frames, and nothing else in the image.
All 4 frames: side view facing LEFT, the same size, standing with legs braced apart — the four paws stay in exactly the same place in every frame.
The shake twists through the body from head to tail like a spinning fan:
Frame 1: the head and ears whip toward the viewer, ears flying out to the side, the fur or loose skin on the neck swinging the same way; the body is still straight.
Frame 2: the twist moves to the chest and middle of the body — shoulders and belly swing toward the viewer while the head is already turning back and the ears fly the other way.
Frame 3: the head and ears whip away from the viewer, ears flying in the opposite direction, the neck fur swinging away.
Frame 4: the twist reaches the rear — hips and tail swing, the fur on the back and haunches puffs out, and the head starts coming back toward the viewer.
Eyes squeezed shut in all frames, fur ruffled and puffed up.
Do NOT draw water drops, sweat, motion lines, blur or speed effects.
Wide horizontal image. Pixel art with crisp hard-edged pixels, dark outline, no anti-aliasing, no grid lines.
Background: one flat solid magenta color (#FF00FF) only — no scenery, no ground, no shadows, no text, no labels, no numbers, no borders.
```

### 6. 側躺睡（`sidesleep`）

```
Generate a brand-new image from scratch. Do NOT edit, modify or extend the reference image — it only shows what the puppy looks like (it is attached, or I attached it earlier in this chat).
Draw the SAME puppy as the reference: a Siberian Husky puppy with a grey-and-white coat, a white face with the classic dark mask pattern, bright blue eyes, upright triangular ears, and a fluffy tail curled over its back.
Keep the identical design, colors, markings, ear shape and tail, the same pixel size and the same scale as the reference.
Pose: the puppy is fast asleep lying flat on its side: the side of its body rests on the ground, all four legs stretched out loosely in front of its belly, the head resting flat on the ground, eyes closed, totally relaxed. Seen from the side with the head on the LEFT.
Exactly ONE puppy in the image, full body visible and not cropped, centered, with empty space around it.
Pixel art with crisp hard-edged pixels, dark outline, no anti-aliasing, no grid lines.
Background: one flat solid magenta color (#FF00FF) only — no scenery, no ground, no shadows, no motion lines, no effects, no text, no labels, no borders.
```

---

## 短毛臘腸 `dachshund`

參考圖：`art/dachshund/stand_reference.png`　→　存到 `art/dachshund/poses/`

### 1. 聞地板（`sniff`）

```
Generate a brand-new image from scratch. Do NOT edit, modify or extend the reference image — it only shows what the puppy looks like (it is attached, or I attached it earlier in this chat).
Draw the SAME puppy as the reference: a smooth short-haired Dachshund puppy with a very long body and very short legs, long floppy ears, a long muzzle, a shiny reddish-brown coat, and a long thin tail.
Keep the identical design, colors, markings, ear shape and tail, the same pixel size and the same scale as the reference.
Pose: the puppy is standing on all four legs and sniffing the ground: the head is lowered all the way down so the tip of the nose touches the ground just in front of its front paws, ears hanging forward, tail relaxed. The WHOLE head, muzzle and nose must be fully drawn and clearly visible. Side view facing LEFT. No bowl, no food, no objects.
Exactly ONE puppy in the image, full body visible and not cropped, centered, with empty space around it.
Pixel art with crisp hard-edged pixels, dark outline, no anti-aliasing, no grid lines.
Background: one flat solid magenta color (#FF00FF) only — no scenery, no ground, no shadows, no motion lines, no effects, no text, no labels, no borders.
```

### 2. 追尾巴（`chase`）

```
Generate a brand-new image from scratch. Do NOT edit, modify or extend the reference image — it only shows what the puppy looks like (it is attached, or I attached it earlier in this chat).
Draw the SAME puppy as the reference: a smooth short-haired Dachshund puppy with a very long body and very short legs, long floppy ears, a long muzzle, a shiny reddish-brown coat, and a long thin tail.
Keep the identical design, colors, markings, ear shape and tail, the same pixel size and the same scale as the reference.
Pose: the puppy is spinning around chasing its own tail: the body is bent into a tight C-shaped curve, the head is turned back toward the rear end, mouth open, excited playful eyes, one front paw lifted off the ground. Side view, head on the LEFT side of the body.
Exactly ONE puppy in the image, full body visible and not cropped, centered, with empty space around it.
Pixel art with crisp hard-edged pixels, dark outline, no anti-aliasing, no grid lines.
Background: one flat solid magenta color (#FF00FF) only — no scenery, no ground, no shadows, no motion lines, no effects, no text, no labels, no borders.
```

### 3. 頭暈（`dizzy`）

```
Generate a brand-new image from scratch. Do NOT edit, modify or extend the reference image — it only shows what the puppy looks like (it is attached, or I attached it earlier in this chat).
Draw the SAME puppy as the reference: a smooth short-haired Dachshund puppy with a very long body and very short legs, long floppy ears, a long muzzle, a shiny reddish-brown coat, and a long thin tail.
Keep the identical design, colors, markings, ear shape and tail, the same pixel size and the same scale as the reference.
Pose: the puppy is dizzy after spinning around: standing on wobbly legs spread a little apart, body leaning slightly to one side, eyes drawn as small spirals, tongue sticking out, a silly dazed expression. 3/4 view facing LEFT. Do NOT draw stars, birds or any effects around the head.
Exactly ONE puppy in the image, full body visible and not cropped, centered, with empty space around it.
Pixel art with crisp hard-edged pixels, dark outline, no anti-aliasing, no grid lines.
Background: one flat solid magenta color (#FF00FF) only — no scenery, no ground, no shadows, no motion lines, no effects, no text, no labels, no borders.
```

### 4. 喘氣（`pant`）

```
Generate a brand-new image from scratch. Do NOT edit, modify or extend the reference image — it only shows what the puppy looks like (it is attached, or I attached it earlier in this chat).
Draw the SAME puppy as the reference: a smooth short-haired Dachshund puppy with a very long body and very short legs, long floppy ears, a long muzzle, a shiny reddish-brown coat, and a long thin tail.
Keep the identical design, colors, markings, ear shape and tail, the same pixel size and the same scale as the reference.
Pose: the puppy is sitting on its haunches and panting after running: front legs straight, mouth wide open, pink tongue hanging out, happy tired eyes. 3/4 view facing LEFT, the same sitting posture as a normal sit.
Exactly ONE puppy in the image, full body visible and not cropped, centered, with empty space around it.
Pixel art with crisp hard-edged pixels, dark outline, no anti-aliasing, no grid lines.
Background: one flat solid magenta color (#FF00FF) only — no scenery, no ground, no shadows, no motion lines, no effects, no text, no labels, no borders.
```

### 5. 抖毛（`shake`，一排 4 格動畫）

```
Generate a brand-new image from scratch. Do NOT edit, modify or extend the reference image — it only shows what the puppy looks like (it is attached, or I attached it earlier in this chat).
Draw the SAME puppy as the reference: a smooth short-haired Dachshund puppy with a very long body and very short legs, long floppy ears, a long muzzle, a shiny reddish-brown coat, and a long thin tail.
Keep the identical design, colors, markings, ear shape and tail, the same pixel size and the same scale as the reference.
Create a 4-frame animation of the puppy shaking its whole body like a wet dog, arranged in ONE horizontal row, evenly spaced with clear gaps between frames, and nothing else in the image.
All 4 frames: side view facing LEFT, the same size, standing with legs braced apart — the four paws stay in exactly the same place in every frame.
The shake twists through the body from head to tail like a spinning fan:
Frame 1: the head and ears whip toward the viewer, ears flying out to the side, the fur or loose skin on the neck swinging the same way; the body is still straight.
Frame 2: the twist moves to the chest and middle of the body — shoulders and belly swing toward the viewer while the head is already turning back and the ears fly the other way.
Frame 3: the head and ears whip away from the viewer, ears flying in the opposite direction, the neck fur swinging away.
Frame 4: the twist reaches the rear — hips and tail swing, the fur on the back and haunches puffs out, and the head starts coming back toward the viewer.
Eyes squeezed shut in all frames, fur ruffled and puffed up.
Do NOT draw water drops, sweat, motion lines, blur or speed effects.
Wide horizontal image. Pixel art with crisp hard-edged pixels, dark outline, no anti-aliasing, no grid lines.
Background: one flat solid magenta color (#FF00FF) only — no scenery, no ground, no shadows, no text, no labels, no numbers, no borders.
```

### 6. 側躺睡（`sidesleep`）

```
Generate a brand-new image from scratch. Do NOT edit, modify or extend the reference image — it only shows what the puppy looks like (it is attached, or I attached it earlier in this chat).
Draw the SAME puppy as the reference: a smooth short-haired Dachshund puppy with a very long body and very short legs, long floppy ears, a long muzzle, a shiny reddish-brown coat, and a long thin tail.
Keep the identical design, colors, markings, ear shape and tail, the same pixel size and the same scale as the reference.
Pose: the puppy is fast asleep lying flat on its side: the side of its body rests on the ground, all four legs stretched out loosely in front of its belly, the head resting flat on the ground, eyes closed, totally relaxed. Seen from the side with the head on the LEFT.
Exactly ONE puppy in the image, full body visible and not cropped, centered, with empty space around it.
Pixel art with crisp hard-edged pixels, dark outline, no anti-aliasing, no grid lines.
Background: one flat solid magenta color (#FF00FF) only — no scenery, no ground, no shadows, no motion lines, no effects, no text, no labels, no borders.
```

---

## 大貴賓 `poodle`

參考圖：`art/poodle/stand_reference.png`　→　存到 `art/poodle/poses/`

### 1. 聞地板（`sniff`）

```
Generate a brand-new image from scratch. Do NOT edit, modify or extend the reference image — it only shows what the puppy looks like (it is attached, or I attached it earlier in this chat).
Draw the SAME puppy as the reference: a Standard Poodle puppy with a curly, fluffy apricot coat in a cute puppy clip: a round fluffy topknot on the head, long fluffy curly ears, a slim muzzle, slender legs with fluffy pom-pom paws, and a fluffy pom-pom tail held up. The curls are drawn with clear lighter and darker shading.
Keep the identical design, colors, markings, ear shape and tail, the same pixel size and the same scale as the reference.
Pose: the puppy is standing on all four legs and sniffing the ground: the head is lowered all the way down so the tip of the nose touches the ground just in front of its front paws, ears hanging forward, tail relaxed. The WHOLE head, muzzle and nose must be fully drawn and clearly visible. Side view facing LEFT. No bowl, no food, no objects.
Exactly ONE puppy in the image, full body visible and not cropped, centered, with empty space around it.
Pixel art with crisp hard-edged pixels, dark outline, no anti-aliasing, no grid lines.
Background: one flat solid magenta color (#FF00FF) only — no scenery, no ground, no shadows, no motion lines, no effects, no text, no labels, no borders.
```

### 2. 追尾巴（`chase`）

```
Generate a brand-new image from scratch. Do NOT edit, modify or extend the reference image — it only shows what the puppy looks like (it is attached, or I attached it earlier in this chat).
Draw the SAME puppy as the reference: a Standard Poodle puppy with a curly, fluffy apricot coat in a cute puppy clip: a round fluffy topknot on the head, long fluffy curly ears, a slim muzzle, slender legs with fluffy pom-pom paws, and a fluffy pom-pom tail held up. The curls are drawn with clear lighter and darker shading.
Keep the identical design, colors, markings, ear shape and tail, the same pixel size and the same scale as the reference.
Pose: the puppy is spinning around chasing its own tail: the body is bent into a tight C-shaped curve, the head is turned back toward the rear end, mouth open, excited playful eyes, one front paw lifted off the ground. Side view, head on the LEFT side of the body.
Exactly ONE puppy in the image, full body visible and not cropped, centered, with empty space around it.
Pixel art with crisp hard-edged pixels, dark outline, no anti-aliasing, no grid lines.
Background: one flat solid magenta color (#FF00FF) only — no scenery, no ground, no shadows, no motion lines, no effects, no text, no labels, no borders.
```

### 3. 頭暈（`dizzy`）

```
Generate a brand-new image from scratch. Do NOT edit, modify or extend the reference image — it only shows what the puppy looks like (it is attached, or I attached it earlier in this chat).
Draw the SAME puppy as the reference: a Standard Poodle puppy with a curly, fluffy apricot coat in a cute puppy clip: a round fluffy topknot on the head, long fluffy curly ears, a slim muzzle, slender legs with fluffy pom-pom paws, and a fluffy pom-pom tail held up. The curls are drawn with clear lighter and darker shading.
Keep the identical design, colors, markings, ear shape and tail, the same pixel size and the same scale as the reference.
Pose: the puppy is dizzy after spinning around: standing on wobbly legs spread a little apart, body leaning slightly to one side, eyes drawn as small spirals, tongue sticking out, a silly dazed expression. 3/4 view facing LEFT. Do NOT draw stars, birds or any effects around the head.
Exactly ONE puppy in the image, full body visible and not cropped, centered, with empty space around it.
Pixel art with crisp hard-edged pixels, dark outline, no anti-aliasing, no grid lines.
Background: one flat solid magenta color (#FF00FF) only — no scenery, no ground, no shadows, no motion lines, no effects, no text, no labels, no borders.
```

### 4. 喘氣（`pant`）

```
Generate a brand-new image from scratch. Do NOT edit, modify or extend the reference image — it only shows what the puppy looks like (it is attached, or I attached it earlier in this chat).
Draw the SAME puppy as the reference: a Standard Poodle puppy with a curly, fluffy apricot coat in a cute puppy clip: a round fluffy topknot on the head, long fluffy curly ears, a slim muzzle, slender legs with fluffy pom-pom paws, and a fluffy pom-pom tail held up. The curls are drawn with clear lighter and darker shading.
Keep the identical design, colors, markings, ear shape and tail, the same pixel size and the same scale as the reference.
Pose: the puppy is sitting on its haunches and panting after running: front legs straight, mouth wide open, pink tongue hanging out, happy tired eyes. 3/4 view facing LEFT, the same sitting posture as a normal sit.
Exactly ONE puppy in the image, full body visible and not cropped, centered, with empty space around it.
Pixel art with crisp hard-edged pixels, dark outline, no anti-aliasing, no grid lines.
Background: one flat solid magenta color (#FF00FF) only — no scenery, no ground, no shadows, no motion lines, no effects, no text, no labels, no borders.
```

### 5. 抖毛（`shake`，一排 4 格動畫）

```
Generate a brand-new image from scratch. Do NOT edit, modify or extend the reference image — it only shows what the puppy looks like (it is attached, or I attached it earlier in this chat).
Draw the SAME puppy as the reference: a Standard Poodle puppy with a curly, fluffy apricot coat in a cute puppy clip: a round fluffy topknot on the head, long fluffy curly ears, a slim muzzle, slender legs with fluffy pom-pom paws, and a fluffy pom-pom tail held up. The curls are drawn with clear lighter and darker shading.
Keep the identical design, colors, markings, ear shape and tail, the same pixel size and the same scale as the reference.
Create a 4-frame animation of the puppy shaking its whole body like a wet dog, arranged in ONE horizontal row, evenly spaced with clear gaps between frames, and nothing else in the image.
All 4 frames: side view facing LEFT, the same size, standing with legs braced apart — the four paws stay in exactly the same place in every frame.
The shake twists through the body from head to tail like a spinning fan:
Frame 1: the head and ears whip toward the viewer, ears flying out to the side, the fur or loose skin on the neck swinging the same way; the body is still straight.
Frame 2: the twist moves to the chest and middle of the body — shoulders and belly swing toward the viewer while the head is already turning back and the ears fly the other way.
Frame 3: the head and ears whip away from the viewer, ears flying in the opposite direction, the neck fur swinging away.
Frame 4: the twist reaches the rear — hips and tail swing, the fur on the back and haunches puffs out, and the head starts coming back toward the viewer.
Eyes squeezed shut in all frames, fur ruffled and puffed up.
Do NOT draw water drops, sweat, motion lines, blur or speed effects.
Wide horizontal image. Pixel art with crisp hard-edged pixels, dark outline, no anti-aliasing, no grid lines.
Background: one flat solid magenta color (#FF00FF) only — no scenery, no ground, no shadows, no text, no labels, no numbers, no borders.
```

### 6. 側躺睡（`sidesleep`）

```
Generate a brand-new image from scratch. Do NOT edit, modify or extend the reference image — it only shows what the puppy looks like (it is attached, or I attached it earlier in this chat).
Draw the SAME puppy as the reference: a Standard Poodle puppy with a curly, fluffy apricot coat in a cute puppy clip: a round fluffy topknot on the head, long fluffy curly ears, a slim muzzle, slender legs with fluffy pom-pom paws, and a fluffy pom-pom tail held up. The curls are drawn with clear lighter and darker shading.
Keep the identical design, colors, markings, ear shape and tail, the same pixel size and the same scale as the reference.
Pose: the puppy is fast asleep lying flat on its side: the side of its body rests on the ground, all four legs stretched out loosely in front of its belly, the head resting flat on the ground, eyes closed, totally relaxed. Seen from the side with the head on the LEFT.
Exactly ONE puppy in the image, full body visible and not cropped, centered, with empty space around it.
Pixel art with crisp hard-edged pixels, dark outline, no anti-aliasing, no grid lines.
Background: one flat solid magenta color (#FF00FF) only — no scenery, no ground, no shadows, no motion lines, no effects, no text, no labels, no borders.
```
