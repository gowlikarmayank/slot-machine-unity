# Scene Setup Guide — Unity Slot Machine
## (Follow this after opening the project in Unity Editor)

This guide walks you through building the `GameScene.unity` from scratch
using the provided assets and scripts. Unity scenes are binary files that
must be assembled in the Editor — this document is your step-by-step blueprint.

---

## 1. Unity Project Setup

1. Open **Unity Hub** → **Open Project** → select `project 12/`
2. Wait for Unity to import all assets (first open takes ~1–2 min)
3. In **Project Settings → Player**:
   - Set **Resolution** to `1920 × 1080` (Landscape) or `1080 × 1920` (Portrait)
   - Enable **Allow Fullscreen Switch**

---

## 2. Create SymbolData ScriptableObjects

> **Path:** `Assets/ScriptableObjects/`

Right-click in Project panel → **Create → SlotMachine → Symbol Data** — create 5 assets:

| Asset Name | Symbol Type | Sprite | Payout Multiplier | Spawn Weight | Is Wild |
|---|---|---|---|---|---|
| `SO_Cherry` | Cherry | `slot-symbol2` | 2 | 35 | No |
| `SO_Bell` | Bell | `slot-symbol3` | 3 | 30 | No |
| `SO_Bar` | Bar | `slot-symbol4` | 5 | 20 | No |
| `SO_Seven` | Seven | `slot-symbol1` | 10 | 10 | No |
| `SO_Wild` | Wild | *(any glow sprite)* | 0 | 5 | **Yes** |

---

## 3. Scene Hierarchy

Create the following hierarchy in the Scene:

```
Scene Root
├── [Canvas] MainCanvas
│   ├── [Image] Background          ← bg_gradient.png, stretch to fill
│   ├── [Image] SlotMachineFrame    ← slot-machine1.png (centre)
│   │   ├── [Image] LeverArm        ← slot-machine2.png; add LeverAnimator.cs
│   │   └── [Mask] ReelWindow       ← Rect Mask 2D to clip symbols
│   │       ├── [GameObject] Reel_1 ← Add ReelController + ReelStrip
│   │       │   ├── [Image] Symbol_Top
│   │       │   ├── [Image] Symbol_Middle  ← Payline (add Outline component)
│   │       │   └── [Image] Symbol_Bottom
│   │       ├── [GameObject] Reel_2 (same structure)
│   │       └── [GameObject] Reel_3 (same structure)
│   ├── [Panel] BottomUI
│   │   ├── [Image] CreditsBox      ← slot_machine_Middle_box.png
│   │   │   ├── [TMP] CreditsText
│   │   │   └── [TMP] BetText
│   │   ├── [Button] BetMinusBtn    ← slot_machine_buttons-04.png (arrow left)
│   │   │   └── OnClick → BetController.DecreaseBet()
│   │   ├── [Button] SpinBtn        ← slot_machine_buttons-03.png (arrow right)
│   │   │   └── OnClick → GameManager.OnSpinButtonPressed()
│   │   ├── [Button] BetPlusBtn     ← slot_machine_buttons-03.png (arrow right)
│   │   │   └── OnClick → BetController.IncreaseBet()
│   │   └── [Button] PaytableBtn    ← "?" text button
│   │       └── OnClick → PaytablePanel.Toggle()
│   ├── [Panel] WinMessagePanel     ← Hidden by default (SetActive false)
│   │   ├── [TMP] WinHeadlineText
│   │   └── [TMP] WinDetailText
│   ├── [Panel] WinPopup            ← popup.png as background; add PopupController.cs
│   │   ├── [TMP] PopupHeadline
│   │   ├── [TMP] PopupBody
│   │   ├── [Button] YesButton      ← Yes_No_Btn.png (left half)
│   │   └── [Button] NoButton       ← Yes_No_Btn.png (right half)
│   ├── [Image] ScreenFlash         ← White image, full stretch; CanvasGroup
│   ├── [ParticleSystem] JackpotFX  ← Gold confetti burst (Particle System)
│   ├── [Panel] PaytablePanel       ← add PaytablePanel.cs; start off-screen right
│   │   ├── [TMP] PaytableTitle
│   │   └── [ScrollView] RowContainer
│   └── [TMP] FreeSpinsText        ← Hidden by default
│
├── [GameObject] GameController      ← Add GameManager.cs
├── [GameObject] SlotController      ← Add SlotMachineController.cs
├── [GameObject] AudioSource         ← Add AudioManager.cs + AudioSource
└── [EventSystem]                    ← Unity UI requires this
```

---

## 4. Configure ReelStrip

For each of Reel_1, Reel_2, Reel_3:
1. Select the reel GameObject
2. In **ReelStrip** component → **Strip Symbols** list, add symbols in this repeating order:
   ```
   Cherry, Bell, Cherry, Bar, Cherry, Bell, Seven, Cherry, Wild, Bar,
   Bell, Cherry, Bar, Bell, Cherry, Seven, Bell, Bar, Cherry, Bell
   ```
   (20 entries — more symbols = smoother animation)

---

## 5. Configure ReelController

For each reel's **ReelController** component:
- **Reel Strip** → drag the same GameObject's **ReelStrip** component
- **Symbol Slots** → drag Symbol_Top (index 0), Symbol_Middle (index 1), Symbol_Bottom (index 2)
- **Symbol Height** → 120 (adjust to match your slot window size)
- **Fast Spin Speed** → 15
- **Fast Spin Duration** → 1.5
- **Decelerate Duration** → 0.6
- **Decelerate Curve** → Set to Ease-Out shape (start steep, flatten at end)
- **Payline Outline** → drag the Outline component on Symbol_Middle

---

## 6. Wire GameManager

On the **GameController** GameObject:
- **Slot Machine Controller** → drag **SlotController**
- **UI Manager** → drag **MainCanvas** (or a dedicated UIManager object)
- **Bet Controller** → drag the object with **BetController.cs**
- **Starting Credits** → 1000

On **SlotMachineController**:
- **Reels** → drag Reel_1, Reel_2, Reel_3 in order
- **All Symbols** → drag SO_Cherry, SO_Bell, SO_Bar, SO_Seven, SO_Wild

---

## 7. Sprite Import Settings (Important!)

For all `.png` files in `Assets/UI/`:
1. Select the sprite in Project panel
2. In Inspector:
   - **Texture Type** → `Sprite (2D and UI)`
   - **Sprite Mode** → `Single`
   - **Filter Mode** → `Bilinear`
   - **Compression** → `None` (for crisp pixel art look)
3. Click **Apply**

For `slot_machine_buttons-02.png`, `03.png`, `04.png` and `Yes_No_Btn.png`
(which contain multiple button states in one image):
   - **Sprite Mode** → `Multiple`
   - Open **Sprite Editor** → use **Slice → Grid by Cell Size** to cut individual states

---

## 8. Canvas Settings

- **Canvas Scaler** → Scale Mode: `Scale With Screen Size`
- **Reference Resolution** → `1920 × 1080`
- **Match** → `0.5` (balanced width/height)

---

## 9. Build WebGL

1. **File → Build Settings**
2. Select **WebGL** platform → **Switch Platform**
3. **Player Settings → Publishing Settings** → Enable **Compression Format: Gzip**
4. Click **Build** → choose `Build/WebGL/` as output folder
5. Commit the entire `Build/WebGL/` folder to GitHub

---

## 10. Testing Checklist

- [ ] Spin 10 times — no console errors
- [ ] Credits deduct correctly on each spin
- [ ] Win popup appears on matching symbols
- [ ] Jackpot particles fire on 3× Seven
- [ ] Wild bonus awards free spin
- [ ] Bet +/- buttons clamp at min/max
- [ ] Paytable panel slides in/out
- [ ] Lever arm animates on spin
- [ ] WebGL build loads in browser without errors
