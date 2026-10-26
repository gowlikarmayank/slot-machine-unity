using System.Collections.Generic;
using UnityEngine;
using SlotMachine.Symbols;

namespace SlotMachine.Reels
{
    /// <summary>
    /// Represents the virtual reel strip — the ordered sequence of symbols
    /// that a single reel cycles through. Think of it as the physical tape
    /// inside a classic mechanical slot machine.
    /// 
    /// Designers can adjust strip contents in the Inspector without touching code.
    /// The strip is circular: after the last symbol it wraps back to the first.
    /// </summary>
    public class ReelStrip : MonoBehaviour
    {
        // ---------------------------------------------------------------
        // Inspector Fields
        // ---------------------------------------------------------------

        [Header("Strip Configuration")]
        [Tooltip("Ordered list of symbols on this reel strip. " +
                 "Symbols appear in this order during a spin.")]
        public List<SymbolData> stripSymbols = new List<SymbolData>();

        // ---------------------------------------------------------------
        // Public API
        // ---------------------------------------------------------------

        /// <summary>
        /// Total number of symbols on this strip.
        /// </summary>
        public int Length => stripSymbols.Count;

        /// <summary>
        /// Gets the symbol at a given position, wrapping around if index
        /// exceeds strip length (circular strip behaviour).
        /// </summary>
        /// <param name="index">Strip position (can exceed strip length).</param>
        public SymbolData GetSymbolAt(int index)
        {
            if (stripSymbols == null || stripSymbols.Count == 0)
            {
                Debug.LogError($"[ReelStrip] Strip on '{name}' is empty!");
                return null;
            }

            // Modulo ensures wrapping even for negative indices
            int wrappedIndex = ((index % stripSymbols.Count) + stripSymbols.Count) % stripSymbols.Count;
            return stripSymbols[wrappedIndex];
        }

        /// <summary>
        /// Finds the strip index of the first occurrence of the given symbol type.
        /// Returns -1 if not found.
        /// </summary>
        public int FindFirstIndexOf(SymbolType type)
        {
            for (int i = 0; i < stripSymbols.Count; i++)
            {
                if (stripSymbols[i] != null && stripSymbols[i].symbolType == type)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Returns all strip indices that contain a particular symbol type.
        /// Useful for forced-win debugging.
        /// </summary>
        public List<int> FindAllIndicesOf(SymbolType type)
        {
            var results = new List<int>();
            for (int i = 0; i < stripSymbols.Count; i++)
            {
                if (stripSymbols[i] != null && stripSymbols[i].symbolType == type)
                    results.Add(i);
            }
            return results;
        }
    }
}
