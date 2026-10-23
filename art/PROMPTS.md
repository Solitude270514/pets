# 新品種 Gemini 提示詞

每個品種需要 **7 張圖**，放進 `art/<品種代號>/`，檔名固定如下：

| 檔名 | 內容 |
|---|---|
| `stand_reference.png`（或 .jpg/.webp） | 站姿參考圖（第 1 步產生，之後每張都要附上它） |
| `sit.jpg` | 坐下 |
| `sleep.jpg` | 趴著睡覺 |
| `eat.jpg` | 低頭吃狗碗 |
| `walk_sheet.jpg` | 走路分解圖（一排 4 格） |
| `poop.jpg` | 蹲著便便 |
| `stretch.jpg` | 伸懶腰打哈欠 |

品種代號：`shiba`（柴犬）、`corgi`（柯基）、`golden`（黃金獵犬）、`husky`（哈士奇）、`dachshund`（短毛臘腸）。

---

## 第 1 步：產生站姿參考圖

附上 `art/doberman/stand_reference.webp`（杜賓的參考圖，用來對齊畫風），貼上：

```
Use the attached image ONLY as a STYLE reference: same pixel-art style, same pixel size, same dark outline, same shading, same 3/4 view standing pose facing left, same cute puppy proportions and roughly the same size in the frame.
Draw a different dog: {品種描述}
Background: one flat solid magenta color (#FF00FF) only — no scenery, no ground, no shadow, no text, no grid lines.
Full body visible, centered, with empty space around it.
```

把 `{品種描述}` 換成下面對應的英文：

- **柴犬 shiba**：`a Shiba Inu puppy with a red-orange coat and cream-white markings (urajiro) on the cheeks, muzzle, throat, chest, belly and inner legs, small thick triangular upright ears, a black nose, and a fluffy tail curled over its back.`
- **柯基 corgi**：`a Pembroke Welsh Corgi puppy with very short legs and a long body, large upright pointed ears, a fox-like face, a red-and-white coat with a white blaze on the face, white collar, chest and legs, and a fluffy round bottom with no visible tail.`
- **黃金獵犬 golden**：`a Golden Retriever puppy with a soft wavy golden coat, floppy ears, a gentle happy face, feathered fur on the chest and legs, and a fluffy feathered tail.`
- **哈士奇 husky**：`a Siberian Husky puppy with a grey-and-white coat, a white face with the classic dark mask pattern, bright blue eyes, upright triangular ears, and a fluffy tail curled over its back.`
- **短毛臘腸 dachshund**：`a smooth short-haired Dachshund puppy with a very long body and very short legs, long floppy ears, a long muzzle, a shiny reddish-brown coat, and a long thin tail.`

生出來的圖滿意了，就存成 `stand_reference`，後面 6 張都附上這張。

---

## 第 2 步：其他 6 個姿勢

附上**該品種的** `stand_reference`，先貼「共用設定」，再接一個姿勢（一次一個）。

### 共用設定

```
Using the attached image as the character reference, draw the SAME puppy (identical design, colors, markings, ear shape and tail).
Style: pixel art, same pixel size and same level of detail as the reference, crisp hard-edged pixels, dark outline, no anti-aliasing, no grid lines.
Background: one flat solid magenta color (#FF00FF) only — no scenery, no ground, no shadow, no text.
Show the full body, not cropped, centered, with empty space around it. The dog is about the same size in the frame as in the reference.
```

### 姿勢

**坐下 sit**
```
Pose: the puppy is sitting on its haunches, front legs straight, facing the same direction as the reference (3/4 view, head turned toward the viewer), looking happy and attentive.
```

**睡覺 sleep**
```
Pose: the puppy is lying down asleep, curled slightly, eyes closed, chin resting on its front paws, ears relaxed, side view facing left.
```

**吃飯 eat**
```
Pose: the puppy is standing and eating from a small round blue dog bowl on the ground, head lowered into the bowl, side view facing left. Include the bowl.
```

**走路 walk_sheet**
```
Create a walk cycle sprite sheet: 4 frames of the puppy walking, arranged in ONE horizontal row, evenly spaced, same size in every frame, all facing left.
Frame 1: contact pose, Frame 2: passing pose, Frame 3: opposite contact pose, Frame 4: opposite passing pose.
Legs should move naturally like a trotting dog. The body, head and tail must stay identical between frames except for slight up-down bounce.
```

**便便 poop**
```
Pose: the puppy is squatting with its back hunched and tail raised, looking a little embarrassed, side view facing left. Do not draw the poop.
```

**伸懶腰 stretch**
```
Pose: the puppy is doing a big play-bow stretch — front legs stretched forward and chest low to the ground, rear end raised high, tail up, mouth open in a yawn, side view facing left.
```

### 走樣時的修正
```
The dog does not match the reference. Keep the character EXACTLY like the attached image: same face shape, same markings in the same places, same ear shape, same tail, same colors and same pixel size. Only change the pose.
```

---

## 小提醒
- 背景一定要純洋紅色 `#FF00FF`，去背才不會誤刪狗的顏色。**柯基、柴犬身上沒有洋紅色**，沒問題；如果某個品種剛好有粉紅色的部位（例如舌頭），也沒關係，程式會處理。
- 走路圖最重要也最難，4 格都要**面向左邊**。生不好就多生幾次，或退而求其次 2 格。
- 圖放好後跟 Claude 說「`shiba` 的圖放好了」，就會轉換並接進程式。
