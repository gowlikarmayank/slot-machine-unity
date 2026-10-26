using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SlotMachine.Symbols;
using SlotMachine.Core;

namespace SlotMachine.Reels
{
    /// <summary>
    /// Controls the visual spinning animation and symbol display for a single reel.
    /// 
    /// Each reel owns a column of Image slots. During a spin the images scroll upward
    /// at high speed (fast phase), then decelerate and snap to the target symbol (slow phase).
    /// 
    /// Animation uses pure Unity Coroutines + AnimationCurves — no DOTween dependency.
    /// 
    /// Lifecycle:
    ///   1. SlotMachineController calls Spin(targetStripIndex)
    ///   2. Reel scrolls fast for FastSpinDuration seconds
    ///   3. Reel decelerates and snaps to targetStripIndex
    ///   4. OnSpinComplete event fires with the landed SymbolData
    /// </summary>
    public class ReelController : MonoBehaviour
    {
        // ---------------------------------------------------------------
        // Inspector Configuration
        // ---------------------------------------------------------------

        [Header("Strip Reference")]
        [Tooltip("The ReelStrip component defining this reel's symbol sequence.")]
        public ReelStrip reelStrip;

        [Header("Slot Images")]
        [Tooltip("UI Image components for visible symbol slots (top, middle, bottom). " +
                 "Middle slot (index 1) is the payline.")]
        public List<Image> symbolSlots = new List<Image>();

        [Header("Animation Settings")]
        [Tooltip("Height in pixels of each symbol slot (used for scrolling math).")]
        public float symbolHeight = 120f;

        [Tooltip("Speed of fast-spin phase in symbols-per-second.")]
        public float fastSpinSpeed = 15f;

        [Tooltip("Duration of the fast-spin phase in seconds.")]
        public float fastSpinDuration = 1.5f;

        [Tooltip("Duration of the deceleration/snap phase in seconds.")]
        public float decelerateDuration = 0.6f;

        [Tooltip("AnimationCurve controlling deceleration easing. Use ease-out for a snap feel.")]
        public AnimationCurve decelerateCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Glow / Highlight")]
        [Tooltip("Outline component on the middle (payline) slot for win highlight.")]
        public Outline paylineOutline;

        [Tooltip("Color used to highlight a winning symbol.")]
        public Color winHighlightColor = new Color(1f, 0.85f, 0f, 1f); // Gold

        // ---------------------------------------------------------------
        // Events
        // ---------------------------------------------------------------

        /// <summary>Fires when the reel finishes spinning, passing the result symbol.</summary>
        public event Action<SymbolData> OnSpinComplete;

        // ---------------------------------------------------------------
        // Private State
        // ---------------------------------------------------------------

        private int _currentStripIndex = 0;    // Which strip position is currently showing in middle slot
        private bool _isSpinning = false;
        private SymbolData _landedSymbol;

        // ---------------------------------------------------------------
        // Unity Lifecycle
        // ---------------------------------------------------------------

        private void Awake()
        {
            // Disable win highlight on start
            if (paylineOutline != null)
                paylineOutline.enabled = false;
        }

        private void Start()
        {
            // Show initial symbols so reel isn't blank on game load
            RefreshDisplay(_currentStripIndex);
        }

        // ---------------------------------------------------------------
        // Public API
        // ---------------------------------------------------------------

        /// <summary>
        /// Returns whether this reel is currently in a spin animation.
        /// </summary>
        public bool IsSpinning => _isSpinning;

        /// <summary>
        /// The symbol currently showing on the payline (middle slot).
        /// Valid only after a spin completes.
        /// </summary>
        public SymbolData LandedSymbol => _landedSymbol;

        /// <summary>
        /// Starts the reel spin animation, targeting the given strip index.
        /// </summary>
        /// <param name="targetStripIndex">
        ///   The strip index that should appear on the payline when the reel stops.
        /// </param>
        public void Spin(int targetStripIndex)
        {
            if (_isSpinning)
            {
                Debug.LogWarning($"[ReelController] '{name}' already spinning — ignoring duplicate Spin() call.");
                return;
            }
            StartCoroutine(SpinCoroutine(targetStripIndex));
        }

        /// <summary>
        /// Immediately illuminates (or clears) the winning symbol highlight on the payline slot.
        /// </summary>
        /// <param name="isWin">True to show the gold glow, false to hide it.</param>
        public void SetWinHighlight(bool isWin)
        {
            if (paylineOutline == null) return;

            paylineOutline.enabled = isWin;
            if (isWin)
                paylineOutline.effectColor = winHighlightColor;
        }

        // ---------------------------------------------------------------
        // Animation Coroutine
        // ---------------------------------------------------------------

        /// <summary>
        /// Main spin coroutine. Two phases:
        ///   Phase 1 — Fast scroll: symbols fly by at high speed.
        ///   Phase 2 — Decelerate: ease out to the target strip index and snap.
        /// </summary>
        private IEnumerator SpinCoroutine(int targetStripIndex)
        {
            _isSpinning = true;
            SetWinHighlight(false);

            // ── Phase 1: Fast Spin ──────────────────────────────────────
            float elapsed = 0f;
            float symbolsScrolled = 0f;  // Fractional count of symbols scrolled past

            while (elapsed < fastSpinDuration)
            {
                float delta = Time.deltaTime;
                elapsed += delta;

                // Advance scroll by fastSpinSpeed symbols per second
                symbolsScrolled += fastSpinSpeed * delta;

                // When we've scrolled past a whole symbol, advance the strip index
                while (symbolsScrolled >= 1f)
                {
                    symbolsScrolled -= 1f;
                    _currentStripIndex = (_currentStripIndex + 1) % reelStrip.Length;
                }

                // Smoothly offset the visible slots by the sub-symbol fraction
                ApplyScrollOffset(symbolsScrolled);
                yield return null;
            }

            // Reset fractional offset before decelerate phase
            ApplyScrollOffset(0f);
            RefreshDisplay(_currentStripIndex);

            // ── Phase 2: Decelerate / Snap to Target ────────────────────
            // Calculate how many more symbols we need to scroll to land on target
            int stepsToTarget = CalculateStepsToTarget(_currentStripIndex, targetStripIndex, reelStrip.Length);

            elapsed = 0f;
            float totalProgress = 0f;   // 0 → stepsToTarget (symbols to scroll)

            while (elapsed < decelerateDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / decelerateDuration);
                float curveValue = decelerateCurve.Evaluate(t);             // 0 → 1 via curve
                float newProgress = curveValue * stepsToTarget;

                float delta = newProgress - totalProgress;
                totalProgress = newProgress;

                symbolsScrolled += delta;

                while (symbolsScrolled >= 1f)
                {
                    symbolsScrolled -= 1f;
                    _currentStripIndex = (_currentStripIndex + 1) % reelStrip.Length;
                }

                ApplyScrollOffset(symbolsScrolled);
                yield return null;
            }

            // ── Final Snap ──────────────────────────────────────────────
            _currentStripIndex = targetStripIndex;
            ApplyScrollOffset(0f);
            RefreshDisplay(_currentStripIndex);

            _isSpinning = false;
            _landedSymbol = reelStrip.GetSymbolAt(_currentStripIndex);

            // Notify listeners
            OnSpinComplete?.Invoke(_landedSymbol);
        }

        // ---------------------------------------------------------------
        // Display Helpers
        // ---------------------------------------------------------------

        /// <summary>
        /// Updates all visible slots with the correct symbols relative to the
        /// current strip index. Middle slot = payline.
        /// </summary>
        private void RefreshDisplay(int middleIndex)
        {
            int slotCount = symbolSlots.Count;

            for (int i = 0; i < slotCount; i++)
            {
                // Offset so middle slot is strip[middleIndex]
                int offset = i - (slotCount / 2);
                int stripIndex = ((middleIndex + offset) % reelStrip.Length + reelStrip.Length) % reelStrip.Length;

                SymbolData sym = reelStrip.GetSymbolAt(stripIndex);
                if (symbolSlots[i] != null && sym != null)
                    symbolSlots[i].sprite = sym.sprite;
            }
        }

        /// <summary>
        /// Applies a vertical pixel offset to the symbol slot RectTransforms
        /// to create smooth sub-symbol scrolling during the spin phases.
        /// </summary>
        /// <param name="fraction">
        ///   Value in [0, 1) representing how far through the next symbol we've scrolled.
        /// </param>
        private void ApplyScrollOffset(float fraction)
        {
            float pixelOffset = fraction * symbolHeight;

            int slotCount = symbolSlots.Count;
            for (int i = 0; i < slotCount; i++)
            {
                if (symbolSlots[i] == null) continue;

                RectTransform rt = symbolSlots[i].rectTransform;
                Vector2 pos = rt.anchoredPosition;
                // The base Y for each slot; offset moves all slots downward during scroll
                float baseY = (i - slotCount / 2) * symbolHeight;
                rt.anchoredPosition = new Vector2(pos.x, baseY - pixelOffset);
            }

            // When we've scrolled a full symbol, refresh the sprites to match
            if (fraction < 0.01f)
                RefreshDisplay(_currentStripIndex);
        }

        /// <summary>
        /// Calculates how many forward steps to scroll from <paramref name="current"/>
        /// to reach <paramref name="target"/>, wrapping around the strip length.
        /// Always returns a positive number (minimum 1 full revolution for visual feel).
        /// </summary>
        private static int CalculateStepsToTarget(int current, int target, int stripLength)
        {
            int steps = (target - current + stripLength) % stripLength;

            // Ensure at least one full revolution so the decelerate phase is visible
            if (steps == 0)
                steps = stripLength;

            return steps;
        }
    }
}
