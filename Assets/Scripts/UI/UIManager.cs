using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SlotMachine.Core;

namespace SlotMachine.UI
{
    /// <summary>
    /// Manages all runtime UI updates: credit/bet display, win messages,
    /// popup visibility, button interactability, and screen effects.
    /// 
    /// Wire all references in the Inspector. This class never directly
    /// touches game logic — it only reads data passed to it.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        // ---------------------------------------------------------------
        // Inspector References — HUD
        // ---------------------------------------------------------------

        [Header("Credit & Bet Display")]
        [Tooltip("Text field showing current player credits.")]
        public TextMeshProUGUI creditsText;

        [Tooltip("Text field showing current bet amount.")]
        public TextMeshProUGUI betText;

        [Tooltip("Text field showing free spins remaining.")]
        public TextMeshProUGUI freeSpinsText;

        [Tooltip("Text field showing consecutive win streak.")]
        public TextMeshProUGUI streakText;

        [Header("Buttons")]
        [Tooltip("The main spin button — disabled during spinning.")]
        public Button spinButton;

        [Tooltip("Bet increase button — disabled during spinning.")]
        public Button betPlusButton;

        [Tooltip("Bet decrease button — disabled during spinning.")]
        public Button betMinusButton;

        // ---------------------------------------------------------------
        // Inspector References — Win Feedback
        // ---------------------------------------------------------------

        [Header("Win Message")]
        [Tooltip("Root panel that shows the win/result text (hidden by default).")]
        public GameObject winMessagePanel;

        [Tooltip("Main win headline text (e.g., 'JACKPOT!', 'BIG WIN!').")]
        public TextMeshProUGUI winHeadlineText;

        [Tooltip("Secondary detail text (e.g., 'You won 500 credits!').")]
        public TextMeshProUGUI winDetailText;

        [Header("Jackpot Effect")]
        [Tooltip("Particle System to burst on Jackpot win.")]
        public ParticleSystem jackpotParticles;

        [Tooltip("CanvasGroup used to flash the screen white on Jackpot.")]
        public CanvasGroup screenFlash;

        // ---------------------------------------------------------------
        // Inspector References — Popup
        // ---------------------------------------------------------------

        [Header("Popup")]
        public PopupController popup;

        // ---------------------------------------------------------------
        // Constants
        // ---------------------------------------------------------------

        private const string HOT_STREAK_FORMAT   = "🔥 x{0} Streak!";
        private const string FREE_SPINS_FORMAT    = "Free Spins: {0}";
        private const float  FLASH_DURATION       = 0.4f;
        private const float  WIN_PANEL_SHOW_SECS  = 2.5f;

        // ---------------------------------------------------------------
        // Unity Lifecycle
        // ---------------------------------------------------------------

        private void Awake()
        {
            // Hide win panel at start
            if (winMessagePanel != null)
                winMessagePanel.SetActive(false);

            if (screenFlash != null)
                screenFlash.alpha = 0f;
        }

        // ---------------------------------------------------------------
        // HUD Updates (called by GameManager)
        // ---------------------------------------------------------------

        /// <summary>
        /// Refreshes all HUD text fields with current game values.
        /// </summary>
        public void SetCredits(int credits, int bet, int streak, int freeSpins)
        {
            if (creditsText != null)
                creditsText.text = $"CREDITS\n{credits}";

            if (betText != null)
                betText.text = $"BET\n{bet}";

            if (freeSpinsText != null)
            {
                freeSpinsText.gameObject.SetActive(freeSpins > 0);
                freeSpinsText.text = string.Format(FREE_SPINS_FORMAT, freeSpins);
            }

            if (streakText != null)
            {
                streakText.gameObject.SetActive(streak >= 2);
                streakText.text = string.Format(HOT_STREAK_FORMAT, streak);
            }
        }

        // ---------------------------------------------------------------
        // State-Driven Button Management
        // ---------------------------------------------------------------

        /// <summary>
        /// Called by GameManager on each state transition.
        /// Enables/disables buttons appropriately for each game state.
        /// </summary>
        public void OnStateChanged(GameManager.GameState state)
        {
            bool isIdle = (state == GameManager.GameState.Idle);
            bool showResult = (state == GameManager.GameState.ShowResult);

            SetButtonsInteractable(isIdle);

            // Hide win message when returning to idle
            if (isIdle && winMessagePanel != null)
                winMessagePanel.SetActive(false);

            // Show popup on result if there was a notable win
            // (Popup is only shown for Jackpot/BigWin by the win handlers below)
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (spinButton   != null) spinButton.interactable   = interactable;
            if (betPlusButton != null) betPlusButton.interactable = interactable;
            if (betMinusButton != null) betMinusButton.interactable = interactable;
        }

        // ---------------------------------------------------------------
        // Win / Lose Display Methods
        // ---------------------------------------------------------------

        /// <summary>Shows the JACKPOT win — screen flash + particles + popup.</summary>
        public void ShowJackpotWin(int amount, int streak)
        {
            ShowWinMessage("🎰 JACKPOT! 🎰", $"You won {amount} credits!");
            TriggerScreenFlash();
            TriggerParticles();

            if (streak >= 2)
                popup?.Show("HOT STREAK!", $"x{streak} wins in a row! Keep going?");
            else
                popup?.Show("JACKPOT!", $"Incredible! You won {amount} credits!\nPlay again?");
        }

        /// <summary>Shows a Big Win popup for 3 matching non-Seven symbols.</summary>
        public void ShowBigWin(string symbolName, int amount, int streak)
        {
            string streakText = streak >= 2 ? $"\n🔥 {streak} win streak!" : "";
            ShowWinMessage($"⭐ BIG WIN! ⭐", $"3x {symbolName}! You won {amount} credits!{streakText}");
            popup?.Show("BIG WIN!", $"3 × {symbolName}\n+{amount} credits!");
        }

        /// <summary>Shows a Wild Bonus notification (free spin awarded).</summary>
        public void ShowWildBonus(int freeSpinsTotal)
        {
            ShowWinMessage("🌟 WILD BONUS! 🌟", $"Free spin awarded! ({freeSpinsTotal} remaining)");
        }

        /// <summary>Shows a small win (2 matching symbols — bet returned).</summary>
        public void ShowSmallWin(int amount)
        {
            ShowWinMessage("✓ Small Win", $"Matched 2 symbols. +{amount} credits.");
        }

        /// <summary>Shows a lose result briefly on the HUD (no popup).</summary>
        public void ShowLose()
        {
            ShowWinMessage("No Match", "Try again!", duration: 1.5f);
        }

        /// <summary>Shows a warning when the player has insufficient credits.</summary>
        public void ShowNotEnoughCreditsMessage()
        {
            ShowWinMessage("⚠ Insufficient Credits", "Lower your bet to continue.", duration: 2f);
        }

        // ---------------------------------------------------------------
        // Internal Helpers
        // ---------------------------------------------------------------

        private void ShowWinMessage(string headline, string detail, float duration = WIN_PANEL_SHOW_SECS)
        {
            if (winMessagePanel == null) return;

            winHeadlineText.text = headline;
            winDetailText.text   = detail;
            winMessagePanel.SetActive(true);

            // Auto-hide after duration
            CancelInvoke(nameof(HideWinMessage));
            Invoke(nameof(HideWinMessage), duration);
        }

        private void HideWinMessage()
        {
            if (winMessagePanel != null)
                winMessagePanel.SetActive(false);
        }

        /// <summary>Plays a white screen-flash coroutine for Jackpot.</summary>
        private void TriggerScreenFlash()
        {
            if (screenFlash != null)
                StartCoroutine(ScreenFlashCoroutine());
        }

        private System.Collections.IEnumerator ScreenFlashCoroutine()
        {
            // Fade in
            float elapsed = 0f;
            while (elapsed < FLASH_DURATION * 0.5f)
            {
                elapsed += Time.deltaTime;
                screenFlash.alpha = Mathf.Lerp(0f, 0.7f, elapsed / (FLASH_DURATION * 0.5f));
                yield return null;
            }
            // Fade out
            elapsed = 0f;
            while (elapsed < FLASH_DURATION * 0.5f)
            {
                elapsed += Time.deltaTime;
                screenFlash.alpha = Mathf.Lerp(0.7f, 0f, elapsed / (FLASH_DURATION * 0.5f));
                yield return null;
            }
            screenFlash.alpha = 0f;
        }

        private void TriggerParticles()
        {
            if (jackpotParticles != null)
            {
                jackpotParticles.Stop();
                jackpotParticles.Play();
            }
        }
    }
}
