using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SlotMachine.UI
{
    /// <summary>
    /// Controls the bet amount with min/max clamping.
    /// Exposes increment/decrement methods for UI buttons.
    /// </summary>
    public class BetController : MonoBehaviour
    {
        // ---------------------------------------------------------------
        // Inspector Configuration
        // ---------------------------------------------------------------

        [Header("Bet Settings")]
        [Tooltip("Minimum allowable bet.")]
        public int minBet = 1;

        [Tooltip("Maximum allowable bet.")]
        public int maxBet = 100;

        [Tooltip("Starting bet amount.")]
        public int startingBet = 10;

        [Tooltip("Step size for each bet increment/decrement.")]
        public int betStep = 10;

        [Header("Display")]
        [Tooltip("Text field that shows the current bet value.")]
        public TextMeshProUGUI betValueText;

        // ---------------------------------------------------------------
        // Public Property
        // ---------------------------------------------------------------

        /// <summary>The currently active bet amount.</summary>
        public int CurrentBet { get; private set; }

        // ---------------------------------------------------------------
        // Unity Lifecycle
        // ---------------------------------------------------------------

        private void Start()
        {
            CurrentBet = Mathf.Clamp(startingBet, minBet, maxBet);
            RefreshDisplay();
        }

        // ---------------------------------------------------------------
        // Public API (wired to UI buttons)
        // ---------------------------------------------------------------

        /// <summary>Increases the bet by one step, clamped to maxBet.</summary>
        public void IncreaseBet()
        {
            CurrentBet = Mathf.Clamp(CurrentBet + betStep, minBet, maxBet);
            RefreshDisplay();
            AudioManager.Instance?.PlayClickSound();
        }

        /// <summary>Decreases the bet by one step, clamped to minBet.</summary>
        public void DecreaseBet()
        {
            CurrentBet = Mathf.Clamp(CurrentBet - betStep, minBet, maxBet);
            RefreshDisplay();
            AudioManager.Instance?.PlayClickSound();
        }

        /// <summary>Sets the bet to a specific value, clamped to [minBet, maxBet].</summary>
        public void SetBet(int amount)
        {
            CurrentBet = Mathf.Clamp(amount, minBet, maxBet);
            RefreshDisplay();
        }

        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------

        private void RefreshDisplay()
        {
            if (betValueText != null)
                betValueText.text = CurrentBet.ToString();
        }
    }
}
