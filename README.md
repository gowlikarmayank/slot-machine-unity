# 🎰 Slot Machine Game — Unity

A feature-complete 3-reel slot machine game built in Unity with smooth animations, weighted RNG, win detection, and multiple bonus features.

---

## 🎮 Game Overview

A classic 3-reel, single-payline slot machine where the player bets credits and spins to match symbols on the centre payline. Win conditions range from a simple 2-match return to a full Jackpot on three Lucky 7s.

### Symbols & Payouts

| Symbol | 3-Match Payout |
|---|---|
| 🎰 Lucky Seven | **10× bet** (Jackpot) |
| BAR | **5× bet** |
| 🔔 Bell | **3× bet** |
| 🍒 Cherry | **2× bet** |
| ⭐ Wild | Joker — matches any symbol |
| *Any 2 matching* | **1× bet** returned |

### Bonus Features

- **Wild Symbol** — Acts as a joker; match any pair. 2× Wild on a spin = **Free Spin** awarded.
- **Jackpot Effect** — Screen flash + gold particle burst on 3× Seven.
- **Hot Streak** — Tracks consecutive wins and displays a 🔥 streak badge.
- **Lever Pull** — Clicking the lever triggers a spring-back mechanical animation.
- **Paytable Panel** — Press "?" to slide in a full payout table.
- **Auto-Spin** — Hold the spin button to queue up to 10 automatic spins.

---

## 🚀 How to Run the WebGL Build

1. Download or clone this repository.
2. Navigate to `Build/WebGL/`.
3. Open `index.html` in a modern browser (**Chrome** or **Firefox** recommended).
   - If you see a CORS error locally, run a simple local server:
     ```bash
     # Python 3
     python -m http.server 8080
     # Then open: http://localhost:8080/Build/WebGL/
     ```
4. Click **SPIN** or pull the lever to play!

> **Controls:**
> - **Spin Button / Lever** → Start spin
> - **← / →** Arrow buttons → Decrease / Increase bet
> - **?** Button → Toggle paytable
> - Hold **Spin** → Activate auto-spin

---

## 🏗️ Project Structure

```
project 12/
├── Assets/
│   ├── Scripts/
│   │   ├── Core/
│   │   │   ├── GameManager.cs          — Singleton state machine (Idle→Spin→Evaluate)
│   │   │   ├── SlotMachineController.cs — Reel orchestrator + win evaluator
│   │   │   └── RNGService.cs           — Seeded weighted random number generator
│   │   ├── Reels/
│   │   │   ├── ReelController.cs       — Per-reel scroll + snap animation (coroutines)
│   │   │   └── ReelStrip.cs            — Virtual reel tape data (ScriptableObject-driven)
│   │   ├── Symbols/
│   │   │   └── SymbolData.cs           — Symbol ScriptableObject (type, sprite, payout, weight)
│   │   ├── UI/
│   │   │   ├── UIManager.cs            — HUD updates, screen flash, particles
│   │   │   ├── PopupController.cs      — Animated YES/NO popup dialog
│   │   │   ├── BetController.cs        — Bet amount with min/max clamping
│   │   │   ├── LeverAnimator.cs        — Lever pull spring animation
│   │   │   └── PaytablePanel.cs        — Slide-in payout table (bonus)
│   │   └── Audio/
│   │       └── AudioManager.cs         — Singleton audio with spin loop + SFX
│   ├── UI/                             — All provided image assets
│   ├── Prefabs/                        — Reel, Symbol, WinPopup prefabs
│   ├── Animations/                     — Lever + win flash animation clips
│   ├── ScriptableObjects/              — SO_Cherry, SO_Bell, SO_Bar, SO_Seven, SO_Wild
│   ├── Sounds/                         — Audio clips (spin, win, click)
│   └── Scenes/
│       └── GameScene.unity
├── Build/
│   └── WebGL/                          — Playable WebGL build
├── Packages/
│   └── manifest.json
├── SceneSetup.md                       — Step-by-step scene assembly guide
└── README.md
```

---

## 🧠 Thought Process & Approach

### Architecture

The game follows a clean **event-driven, state-machine** architecture:

```
GameManager (FSM)
    │
    ├──► SlotMachineController  (orchestrates reels, evaluates wins)
    │         │
    │         ├──► ReelController × 3  (animation)
    │         │         └──► ReelStrip (data)
    │         └──► RNGService (weighted random)
    │
    └──► UIManager  (display only — reacts to events)
              └──► PopupController, BetController, LeverAnimator
```

- **GameManager** never knows about specific symbols or animation details — it only transitions states and handles credits.
- **SlotMachineController** pre-picks all outcomes *before* spinning starts, making the RNG timing immune to frame-rate differences (fair outcome).
- **ReelController** uses only Unity built-ins (Coroutines + AnimationCurves) — no external dependencies.
- All symbol data is in **ScriptableObjects**, so designers can tweak payouts and weights without touching code.

### RNG Fairness

The weighted random algorithm uses a classic "accumulated-weight dart-throw":
1. Sum all symbol weights.
2. Draw a random integer in `[0, totalWeight)`.
3. Walk the symbol list accumulating weights until the roll is exceeded.

This guarantees each symbol's probability = `its weight / totalWeight`, regardless of list order.

### Animation Design

Reel animation has two phases:
1. **Fast phase** (1.5s): Symbols scroll at 15 symbols/sec — gives the feel of a real spinning reel.
2. **Decelerate phase** (0.6s): An `EaseOut` curve with a slight overshoot makes the reel "snap" satisfyingly onto the target symbol (similar to DOTween's `OutBounce`).

Reels stop in sequence (400ms stagger) to build tension: reel 1 locks, then 2, then 3 — mimicking the drama of physical slot machines.

---

## 📌 Notes

- Audio clips are **optional** — the game runs silently without them.
- The `SceneSetup.md` file contains the full Unity Editor scene-building guide.
- Wild symbol sprite is not included in the provided asset pack — use any glow/star sprite or generate one in Unity's Sprite Editor.

---

*Built with Unity 2022 LTS · TextMeshPro · Pure C# Coroutines*
