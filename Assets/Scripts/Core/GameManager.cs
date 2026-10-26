using System.Collections;
using UnityEngine;
using SlotMachine.Reels;
using SlotMachine.UI;

namespace SlotMachine.Core
{
    /// <summary>
    /// Central game state machine (Singleton).
    /// 
    /// State Flow:
    ///   Idle ──[Player clicks Spin]──► Spinning
    ///   Spinning ──[All reels stop]──► Evaluating
    ///   Evaluating ──[Result computed]──► ShowResult
    ///   ShowResult ──[Player dismisses]──► Idle  (or FreeSpinning if WildBonus)
    /// 
    /// This class wires together SlotMachineController, UIManager, and BetController.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // ---------------------------------------------------------------
        // Singleton
        // ---------------------------------------------------------------

        public static GameManager Instance { get; private set; }

        // ---------------------------------------------------------------
        // Inspector References
        // ---------------------------------------------------------------

        [Header("Controllers")]
        public SlotMachineController slotMachineController;
        public UIManager uiManager;
        public BetController betController;

        [Header("Lever Arm Animation")]
        [Tooltip("Animator controlling the lever pull animation.")]
        public Animator leverAnimator;

        [Header("Starting Credits")]
        [Tooltip("How many credits the player starts with.")]
        public int startingCredits = 1000;

        [Header("Auto-Spin")]
        [Tooltip("Maximum number of auto-spins per session.")]
        [Range(1, 50)]
        public int maxAutoSpins = 10;

        // ---------------------------------------------------------------
        // Game State
        // ---------------------------------------------------------------

        /// <summary>Possible states of the game state machine.</summary>
        public enum GameState { Idle, Spinning, Evaluating, ShowResult, FreeSpinning }

        public GameState CurrentState { get; private set; } = GameState.Idle;

        // Runtime tracking
        private int _credits;
        private int _freeSpinsRemaining = 0;
        private int _consecutiveWins = 0;        // Streak tracker (bonus feature)
        private int _autoSpinsRemaining = 0;

        // ---------------------------------------------------------------
        // Unity Lifecycle
        // ---------------------------------------------------------------

        private void Awake()
        {
            // Enforce singleton pattern
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Initialize RNG once at startup
            RNGService.Initialize();
        }

        private void Start()
        {
            _credits = startingCredits;
            UpdateUI();

            // Subscribe to slot machine result event
            slotMachineController.OnSpinEvaluated += HandleSpinResult;

            Debug.Log("[GameManager] Game started. Credits: " + _credits);
        }

        private void OnDestroy()
        {
            // Unsubscribe to prevent memory leaks
            if (slotMachineController != null)
                slotMachineController.OnSpinEvaluated -= HandleSpinResult;
        }

        // ---------------------------------------------------------------
        // Public Actions (called by UI buttons)
        // ---------------------------------------------------------------

        /// <summary>
        /// Called when the player presses the Spin button or pulls the lever.
        /// Validates bet, deducts credits, then starts the spin.
        /// </summary>
        public void OnSpinButtonPressed()
        {
            if (CurrentState != GameState.Idle) return;

            int bet = betController.CurrentBet;

            // Guard: ensure player has enough credits
            if (_credits < bet)
            {
                uiManager.ShowNotEnoughCreditsMessage();
                return;
            }

            // Deduct bet from credits
            _credits -= bet;
            UpdateUI();

            // Transition to spinning state
            TransitionTo(GameState.Spinning);

            // Animate the lever pull if available
            if (leverAnimator != null)
                leverAnimator.SetTrigger("Pull");

            // Begin spin
            slotMachineController.StartSpin();
        }

        /// <summary>
        /// Called when the player holds the spin button to activate Auto-Spin.
        /// Queues up to maxAutoSpins automatic spins.
        /// </summary>
        public void OnAutoSpinRequested()
        {
            if (CurrentState != GameState.Idle) return;
            _autoSpinsRemaining = maxAutoSpins;
            OnSpinButtonPressed();
        }

        /// <summary>
        /// Cancels an ongoing auto-spin sequence after the current spin completes.
        /// </summary>
        public void CancelAutoSpin()
        {
            _autoSpinsRemaining = 0;
        }

        // ---------------------------------------------------------------
        // Spin Result Handler
        // ---------------------------------------------------------------

        /// <summary>
        /// Receives the SpinResult from SlotMachineController and processes it:
        ///   • Calculates payout
        ///   • Updates credits
        ///   • Triggers appropriate UI feedback
        ///   • Manages free spin and win streak tracking
        /// </summary>
        private void HandleSpinResult(SpinResult result)
        {
            TransitionTo(GameState.Evaluating);

            int bet = betController.CurrentBet;
            int payout = 0;

            switch (result.WinType)
            {
                case WinType.Jackpot:
                    payout = bet * result.PayoutMultiplier;
                    _credits += payout;
                    _consecutiveWins++;
                    uiManager.ShowJackpotWin(payout, _consecutiveWins);
                    AudioManager.Instance?.PlayWinSound(true);
                    break;

                case WinType.BigWin:
                    payout = bet * result.PayoutMultiplier;
                    _credits += payout;
                    _consecutiveWins++;
                    uiManager.ShowBigWin(result.WinningSymbol?.DisplayName ?? "MATCH", payout, _consecutiveWins);
                    AudioManager.Instance?.PlayWinSound(false);
                    break;

                case WinType.WildBonus:
                    // Wild bonus: award a free spin instead of coins
                    _freeSpinsRemaining++;
                    _consecutiveWins++;
                    uiManager.ShowWildBonus(_freeSpinsRemaining);
                    AudioManager.Instance?.PlayBonusSound();
                    break;

                case WinType.SmallWin:
                    // Return the bet amount
                    payout = bet;
                    _credits += payout;
                    _consecutiveWins++;
                    uiManager.ShowSmallWin(payout);
                    AudioManager.Instance?.PlayWinSound(false);
                    break;

                case WinType.None:
                    _consecutiveWins = 0;
                    uiManager.ShowLose();
                    AudioManager.Instance?.PlayLoseSound();
                    break;
            }

            UpdateUI();

            // Show popup for wins / check for free spins
            if (_freeSpinsRemaining > 0)
            {
                // Auto-trigger the free spin after a short delay
                StartCoroutine(TriggerFreeSpinAfterDelay(1.5f));
                return;
            }

            TransitionTo(GameState.ShowResult);

            // Auto-spin continuation
            if (_autoSpinsRemaining > 0)
            {
                _autoSpinsRemaining--;
                StartCoroutine(AutoSpinDelay(1.5f));
            }
        }

        // ---------------------------------------------------------------
        // Free Spin & Auto-Spin Coroutines
        // ---------------------------------------------------------------

        private IEnumerator TriggerFreeSpinAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            _freeSpinsRemaining--;
            TransitionTo(GameState.Idle);
            OnSpinButtonPressed();  // Free spin — no bet deducted this call
        }

        private IEnumerator AutoSpinDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            TransitionTo(GameState.Idle);
            OnSpinButtonPressed();
        }

        // ---------------------------------------------------------------
        // State Machine
        // ---------------------------------------------------------------

        private void TransitionTo(GameState newState)
        {
            Debug.Log($"[GameManager] State: {CurrentState} → {newState}");
            CurrentState = newState;
            uiManager?.OnStateChanged(newState);
        }

        // ---------------------------------------------------------------
        // UI Refresh
        // ---------------------------------------------------------------

        private void UpdateUI()
        {
            uiManager?.SetCredits(_credits, betController.CurrentBet, _consecutiveWins, _freeSpinsRemaining);
        }
    }
}
