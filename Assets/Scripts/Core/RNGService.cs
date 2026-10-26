using System;
using System.Collections.Generic;
using UnityEngine;
using SlotMachine.Symbols;

namespace SlotMachine.Core
{
    /// <summary>
    /// Provides seeded, weighted random number generation for reel symbol selection.
    /// Supports reproducible outcomes (useful for testing) via an optional seed.
    /// 
    /// Usage:
    ///   RNGService.Initialize();            // Random seed
    ///   RNGService.Initialize(42);          // Fixed seed for testing
    ///   int symbolIndex = RNGService.GetWeightedRandomIndex(symbolDataList);
    /// </summary>
    public static class RNGService
    {
        // ---------------------------------------------------------------
        // Private state
        // ---------------------------------------------------------------

        private static System.Random _random;
        private static bool _isInitialized = false;

        // ---------------------------------------------------------------
        // Initialization
        // ---------------------------------------------------------------

        /// <summary>
        /// Initializes the RNG with a truly random seed (based on current time).
        /// Call once at game startup.
        /// </summary>
        public static void Initialize()
        {
            _random = new System.Random();
            _isInitialized = true;
            Debug.Log("[RNGService] Initialized with random seed.");
        }

        /// <summary>
        /// Initializes the RNG with a fixed seed for deterministic/testable outcomes.
        /// </summary>
        /// <param name="seed">The seed value to use.</param>
        public static void Initialize(int seed)
        {
            _random = new System.Random(seed);
            _isInitialized = true;
            Debug.Log($"[RNGService] Initialized with fixed seed: {seed}");
        }

        // ---------------------------------------------------------------
        // Core RNG Methods
        // ---------------------------------------------------------------

        /// <summary>
        /// Returns a random integer in the range [min, max).
        /// </summary>
        public static int Range(int min, int max)
        {
            EnsureInitialized();
            return _random.Next(min, max);
        }

        /// <summary>
        /// Returns a random float in the range [0.0, 1.0).
        /// </summary>
        public static float Value()
        {
            EnsureInitialized();
            return (float)_random.NextDouble();
        }

        // ---------------------------------------------------------------
        // Weighted Symbol Selection
        // ---------------------------------------------------------------

        /// <summary>
        /// Selects a symbol index from the provided list using weighted probability.
        /// Each symbol's <c>spawnWeight</c> field determines its relative frequency.
        /// 
        /// Algorithm: Accumulated-weight / dart-throw (O(n))
        /// </summary>
        /// <param name="symbols">List of SymbolData with spawn weights.</param>
        /// <returns>The index of the randomly chosen symbol.</returns>
        public static int GetWeightedRandomIndex(List<SymbolData> symbols)
        {
            EnsureInitialized();

            if (symbols == null || symbols.Count == 0)
            {
                Debug.LogError("[RNGService] Symbol list is null or empty!");
                return 0;
            }

            // 1. Sum all weights
            int totalWeight = 0;
            foreach (SymbolData sym in symbols)
                totalWeight += Mathf.Max(1, sym.spawnWeight);

            // 2. Pick a random number in [0, totalWeight)
            int roll = _random.Next(0, totalWeight);

            // 3. Walk through symbols until we exceed the roll (dart-throw)
            int accumulated = 0;
            for (int i = 0; i < symbols.Count; i++)
            {
                accumulated += Mathf.Max(1, symbols[i].spawnWeight);
                if (roll < accumulated)
                    return i;
            }

            // Fallback (should never reach here)
            return symbols.Count - 1;
        }

        /// <summary>
        /// Picks a random symbol from the list using weighted probability.
        /// </summary>
        public static SymbolData GetWeightedRandomSymbol(List<SymbolData> symbols)
        {
            int index = GetWeightedRandomIndex(symbols);
            return symbols[index];
        }

        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------

        private static void EnsureInitialized()
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("[RNGService] Not initialized. Auto-initializing with random seed.");
                Initialize();
            }
        }
    }
}
