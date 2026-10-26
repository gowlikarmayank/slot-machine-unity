using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SlotMachine.Reels;
using SlotMachine.Symbols;
using SlotMachine.Core;

namespace SlotMachine.Core
{
    /// <summary>
    /// Orchestrates all reel controllers and evaluates win conditions.
    /// 
    /// Responsibilities:
    ///   • Ask RNGService for target symbols before each spin
    ///   • Trigger reels in a staggered sequence (adds tension/drama)
    ///   • Collect results when all reels stop
    ///   • Evaluate win type and payout multiplier
    ///   • Fire events consumed by GameManager → UIManager
    /// </summary>
    public class SlotMachineController : MonoBehaviour
    {
        // ---------------------------------------------------------------
        // Inspector Configuration
        // ---------------------------------------------------------------

        [Header("Reels")]
        [Tooltip("All reel controllers in left-to-right order.")]
        public List<ReelController> reels = new List<ReelController>();

        [Header("Symbol Pool")]
        [Tooltip("Master list of all possible symbols (must match ReelStrip contents).")]
        public List<SymbolData> allSymbols = new List<SymbolData>();

        [Header("Timing")]
        [Tooltip("Delay in seconds between each reel stopping (stagger effect).")]
        [Range(0f, 2f)]
        public float reelStopStagger = 0.4f;

        // ---------------------------------------------------------------
        // Events
        // ---------------------------------------------------------------

        /// <summary>Fired after all reels have stopped. Passes the spin result.</summary>
        public event Action<SpinResult> OnSpinEvaluated;

        // ---------------------------------------------------------------
        // Private State
        // ---------------------------------------------------------------

        private List<SymbolData> _landedSymbols = new List<SymbolData>();
        private int _reelsCompletedCount = 0;
        private int[] _targetStripIndices;

        // ---------------------------------------------------------------
        // Public API
        // ---------------------------------------------------------------

        /// <summary>
        /// Initiates a full spin cycle:
        ///   1. Selects random target symbols via RNGService.
        ///   2. Starts each reel with a stagger delay.
        ///   3. Listens for all reels to complete.
        ///   4. Evaluates the result and fires OnSpinEvaluated.
        /// </summary>
        public void StartSpin()
        {
            StartCoroutine(SpinSequence());
        }

        // ---------------------------------------------------------------
        // Spin Sequence
        // ---------------------------------------------------------------

        private IEnumerator SpinSequence()
        {
            // Reset tracking
            _landedSymbols.Clear();
            _reelsCompletedCount = 0;

            // Pre-determine all target strip indices before any reel starts spinning.
            // This ensures the outcome is decided upfront (fair RNG), not mid-spin.
            _targetStripIndices = new int[reels.Count];
            for (int i = 0; i < reels.Count; i++)
            {
                ReelStrip strip = reels[i].reelStrip;
                // Pick a random strip position that contains a weighted-random symbol
                SymbolData chosen = RNGService.GetWeightedRandomSymbol(allSymbols);
                _targetStripIndices[i] = FindSymbolOnStrip(strip, chosen);

                Debug.Log($"[SlotMachineController] Reel {i} target: {chosen.DisplayName} at strip index {_targetStripIndices[i]}");
            }

            // Subscribe to each reel's completion event
            for (int i = 0; i < reels.Count; i++)
            {
                reels[i].OnSpinComplete += HandleReelComplete;
            }

            // Start reels one at a time with a small stagger so they all spin
            // simultaneously but stop in a pleasing sequential order
            for (int i = 0; i < reels.Count; i++)
            {
                reels[i].Spin(_targetStripIndices[i]);
                // No stagger on start — all start together; stop stagger is handled by spin duration
            }

            // Wait until all reels have reported completion
            yield return new WaitUntil(() => _reelsCompletedCount >= reels.Count);

            // Unsubscribe from events
            for (int i = 0; i < reels.Count; i++)
                reels[i].OnSpinComplete -= HandleReelComplete;

            // Evaluate result
            SpinResult result = EvaluateResult(_landedSymbols);
            Debug.Log($"[SlotMachineController] Spin result: {result.WinType}, Multiplier: {result.PayoutMultiplier}x");

            // Highlight winning reels
            if (result.WinType != WinType.None)
            {
                foreach (ReelController reel in reels)
                    reel.SetWinHighlight(true);
            }

            OnSpinEvaluated?.Invoke(result);
        }

        /// <summary>Callback registered per-reel; collects results as each stops.</summary>
        private void HandleReelComplete(SymbolData symbol)
        {
            _landedSymbols.Add(symbol);
            _reelsCompletedCount++;
        }

        // ---------------------------------------------------------------
        // Win Evaluation
        // ---------------------------------------------------------------

        /// <summary>
        /// Analyses the three landed symbols and returns a <see cref="SpinResult"/>.
        /// 
        /// Win hierarchy (highest priority first):
        ///   • Jackpot: 3× Seven
        ///   • Big Win: 3× same symbol (any)
        ///   • Wild Bonus: ≥2 Wild symbols → free spin trigger
        ///   • Small Win: 2 matching symbols (same type)
        ///   • No Win
        /// </summary>
        private SpinResult EvaluateResult(List<SymbolData> symbols)
        {
            if (symbols == null || symbols.Count == 0)
                return new SpinResult(WinType.None, 0, null);

            // Count wilds
            int wildCount = 0;
            List<SymbolData> nonWilds = new List<SymbolData>();
            foreach (SymbolData s in symbols)
            {
                if (s != null && s.isWild)
                    wildCount++;
                else if (s != null)
                    nonWilds.Add(s);
            }

            // Treat wilds as matching: fill 'effective' types
            // e.g. [Cherry, Wild, Cherry] → effectively [Cherry, Cherry, Cherry]
            SymbolType? baseType = nonWilds.Count > 0 ? (SymbolType?)nonWilds[0].symbolType : null;
            bool allSameType = true;
            foreach (SymbolData s in nonWilds)
            {
                if (s.symbolType != baseType)
                {
                    allSameType = false;
                    break;
                }
            }

            // All three match (possibly with wilds helping)
            bool isFullMatch = allSameType && (nonWilds.Count + wildCount == symbols.Count);

            if (isFullMatch && baseType.HasValue)
            {
                // Find the non-wild symbol data for payout info
                SymbolData paySymbol = nonWilds.Count > 0 ? nonWilds[0] : null;

                if (baseType == SymbolType.Seven)
                    return new SpinResult(WinType.Jackpot, paySymbol?.payoutMultiplier ?? 10, paySymbol);

                return new SpinResult(WinType.BigWin, paySymbol?.payoutMultiplier ?? 5, paySymbol);
            }

            // Wild Bonus: 2+ wilds without a full match → free spin
            if (wildCount >= 2)
                return new SpinResult(WinType.WildBonus, 0, null);

            // Partial match: any 2 symbols of the same type
            for (int i = 0; i < symbols.Count; i++)
            {
                for (int j = i + 1; j < symbols.Count; j++)
                {
                    if (symbols[i] != null && symbols[j] != null &&
                        !symbols[i].isWild && !symbols[j].isWild &&
                        symbols[i].symbolType == symbols[j].symbolType)
                    {
                        return new SpinResult(WinType.SmallWin, 1, symbols[i]);
                    }
                }
            }

            return new SpinResult(WinType.None, 0, null);
        }

        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------

        /// <summary>
        /// Finds the first strip index on <paramref name="strip"/> where the symbol
        /// type matches <paramref name="target"/>. Falls back to index 0 if not found.
        /// </summary>
        private static int FindSymbolOnStrip(ReelStrip strip, SymbolData target)
        {
            if (strip == null || target == null) return 0;

            int idx = strip.FindFirstIndexOf(target.symbolType);
            return idx >= 0 ? idx : 0;
        }
    }

    // ---------------------------------------------------------------
    // Result Data Types
    // ---------------------------------------------------------------

    /// <summary>Types of winning outcomes a spin can produce.</summary>
    public enum WinType
    {
        None,       // No win
        SmallWin,   // 2 matching symbols — bet returned
        BigWin,     // 3 matching symbols — multiplied payout
        WildBonus,  // 2+ Wilds — free spin awarded
        Jackpot     // 3× Lucky 7 — maximum payout
    }

    /// <summary>
    /// Immutable value object encapsulating a spin's outcome.
    /// Passed via events to GameManager and UIManager.
    /// </summary>
    [Serializable]
    public class SpinResult
    {
        /// <summary>Category of win (or no win).</summary>
        public readonly WinType WinType;

        /// <summary>
        /// Payout multiplier to apply to the current bet.
        /// 0 = no payout (None / WildBonus handled separately).
        /// </summary>
        public readonly int PayoutMultiplier;

        /// <summary>
        /// The primary winning symbol (null for None/WildBonus).
        /// Used by UIManager to display the winning symbol name.
        /// </summary>
        public readonly SymbolData WinningSymbol;

        public SpinResult(WinType winType, int payoutMultiplier, SymbolData winningSymbol)
        {
            WinType = winType;
            PayoutMultiplier = payoutMultiplier;
            WinningSymbol = winningSymbol;
        }

        /// <summary>True if the player won anything this spin.</summary>
        public bool IsWin => WinType != WinType.None;

        /// <summary>True if this result awards a free spin.</summary>
        public bool AwardsFreeSpIn => WinType == WinType.WildBonus;
    }
}
