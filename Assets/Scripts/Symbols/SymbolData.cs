using UnityEngine;

namespace SlotMachine.Symbols
{
    /// <summary>
    /// Enumeration of all possible symbol types in the slot machine.
    /// Ordered roughly by rarity (most common last in enum, but weights are set per symbol).
    /// </summary>
    public enum SymbolType
    {
        Cherry  = 0,
        Bell    = 1,
        Bar     = 2,
        Seven   = 3,
        Wild    = 4   // Bonus symbol — acts as a joker
    }

    /// <summary>
    /// ScriptableObject that stores all data for a single slot symbol.
    /// Create via: Assets > Create > SlotMachine > Symbol Data
    /// </summary>
    [CreateAssetMenu(fileName = "SymbolData", menuName = "SlotMachine/Symbol Data")]
    public class SymbolData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("The type/name of this symbol.")]
        public SymbolType symbolType;

        [Header("Visuals")]
        [Tooltip("The sprite displayed on the reel for this symbol.")]
        public Sprite sprite;

        [Header("Gameplay")]
        [Tooltip("Payout multiplier when 3 of this symbol appear. E.g., 10 = 10x the bet.")]
        [Range(1, 100)]
        public int payoutMultiplier = 2;

        [Tooltip("Relative weight in the RNG pool. Higher = more likely to appear.")]
        [Range(1, 100)]
        public int spawnWeight = 20;

        [Tooltip("If true, this symbol acts as a Wild (matches any other symbol).")]
        public bool isWild = false;

        /// <summary>
        /// Returns a user-friendly display name for the symbol.
        /// </summary>
        public string DisplayName => symbolType.ToString();
    }
}
