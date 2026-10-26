using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SlotMachine.UI
{
    /// <summary>
    /// Manages the win/prompt popup dialog.
    /// 
    /// The popup animates in with a scale + fade tween (pure coroutine),
    /// presents a headline + optional body text, and exposes YES / NO buttons
    /// whose callbacks are wired at runtime via Show().
    /// 
    /// Used for:
    ///   • Jackpot / Big Win — "Play again?" (YES/NO)
    ///   • Wild Bonus notification — auto-dismissed
    ///   • Low credits warning
    /// </summary>
    public class PopupController : MonoBehaviour
    {
        // ---------------------------------------------------------------
        // Inspector References
        // ---------------------------------------------------------------

        [Header("Panel")]
        [Tooltip("Root CanvasGroup of the popup (controls alpha + interactable).")]
        public CanvasGroup canvasGroup;

        [Tooltip("RectTransform of the popup panel (used for scale animation).")]
        public RectTransform panelRect;

        [Header("Text Fields")]
        public TextMeshProUGUI headlineText;
        public TextMeshProUGUI bodyText;

        [Header("Buttons")]
        [Tooltip("YES button — call action wired at runtime.")]
        public Button yesButton;

        [Tooltip("NO button — call action wired at runtime.")]
        public Button noButton;

        [Tooltip("Hide the NO button for info-only popups that just need YES.")]
        public bool hideNoButtonForInfo = false;

        [Header("Animation")]
        [Tooltip("Duration of the scale-in animation in seconds.")]
        [Range(0.1f, 1f)]
        public float animateInDuration = 0.25f;

        [Tooltip("Duration of the scale-out animation in seconds.")]
        [Range(0.1f, 1f)]
        public float animateOutDuration = 0.2f;

        // ---------------------------------------------------------------
        // Private State
        // ---------------------------------------------------------------

        private Action _onYes;
        private Action _onNo;
        private Coroutine _animCoroutine;

        // ---------------------------------------------------------------
        // Unity Lifecycle
        // ---------------------------------------------------------------

        private void Awake()
        {
            // Start hidden
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            if (panelRect != null)
                panelRect.localScale = Vector3.zero;

            // Wire button listeners
            yesButton?.onClick.AddListener(OnYesClicked);
            noButton?.onClick.AddListener(OnNoClicked);
        }

        // ---------------------------------------------------------------
        // Public API
        // ---------------------------------------------------------------

        /// <summary>
        /// Shows the popup with custom headline/body and optional callbacks.
        /// </summary>
        /// <param name="headline">Bold top line (e.g., "JACKPOT!").</param>
        /// <param name="body">Detail message (e.g., "You won 500 credits!").</param>
        /// <param name="onYes">Called when the YES button is pressed. Defaults to hiding popup.</param>
        /// <param name="onNo">Called when the NO button is pressed. Defaults to hiding popup.</param>
        /// <param name="showNoButton">Whether to show the NO button.</param>
        public void Show(string headline, string body,
                         Action onYes = null, Action onNo = null,
                         bool showNoButton = true)
        {
            if (headlineText != null) headlineText.text = headline;
            if (bodyText     != null) bodyText.text     = body;

            _onYes = onYes ?? Hide;
            _onNo  = onNo  ?? Hide;

            if (noButton != null)
                noButton.gameObject.SetActive(showNoButton);

            if (_animCoroutine != null)
                StopCoroutine(_animCoroutine);

            _animCoroutine = StartCoroutine(AnimateIn());
        }

        /// <summary>Hides the popup with an animate-out transition.</summary>
        public void Hide()
        {
            if (_animCoroutine != null)
                StopCoroutine(_animCoroutine);

            _animCoroutine = StartCoroutine(AnimateOut());
        }

        // ---------------------------------------------------------------
        // Button Handlers
        // ---------------------------------------------------------------

        private void OnYesClicked()
        {
            Hide();
            _onYes?.Invoke();
        }

        private void OnNoClicked()
        {
            Hide();
            _onNo?.Invoke();
        }

        // ---------------------------------------------------------------
        // Animation Coroutines
        // ---------------------------------------------------------------

        private IEnumerator AnimateIn()
        {
            // Make interactive immediately
            if (canvasGroup != null)
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            float elapsed = 0f;
            while (elapsed < animateInDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / animateInDuration);
                float eased = EaseOutBack(t);  // Overshoot spring feel

                if (canvasGroup != null) canvasGroup.alpha = t;
                if (panelRect   != null) panelRect.localScale = Vector3.one * eased;

                yield return null;
            }

            if (canvasGroup != null) canvasGroup.alpha = 1f;
            if (panelRect   != null) panelRect.localScale = Vector3.one;
        }

        private IEnumerator AnimateOut()
        {
            float elapsed = 0f;
            while (elapsed < animateOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / animateOutDuration);

                if (canvasGroup != null) canvasGroup.alpha = 1f - t;
                if (panelRect   != null) panelRect.localScale = Vector3.one * (1f - t);

                yield return null;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            if (panelRect != null)
                panelRect.localScale = Vector3.zero;
        }

        // ---------------------------------------------------------------
        // Math Helpers
        // ---------------------------------------------------------------

        /// <summary>
        /// Ease-out-back easing function for the spring overshoot effect.
        /// t in [0,1] → value in [0,1] with slight overshoot above 1.
        /// </summary>
        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }
}
