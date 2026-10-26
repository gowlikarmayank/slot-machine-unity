using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SlotMachine.UI
{
    /// <summary>
    /// Bonus Feature: Lever Pull Animation
    /// 
    /// Controls the visual animation of the slot machine lever arm.
    /// The lever has 4 states: Idle → PullDown → HoldDown → SpringUp → Idle.
    /// Uses the provided slot-machine2/3 sprite images to simulate a realistic arm pull.
    /// 
    /// Wire the SpinButton's OnClick to OnLeverClicked() in the Inspector.
    /// </summary>
    public class LeverAnimator : MonoBehaviour
    {
        // ---------------------------------------------------------------
        // Inspector References
        // ---------------------------------------------------------------

        [Header("Lever Images")]
        [Tooltip("Sprite for lever in idle/up position (slot-machine2.png — arm up).")]
        public Sprite leverUpSprite;

        [Tooltip("Sprite for lever in pulled-down position (slot-machine3.png — arm down).")]
        public Sprite leverDownSprite;

        [Tooltip("Image component on the lever GameObject.")]
        public Image leverImage;

        [Header("Animation Settings")]
        [Tooltip("Time in seconds for the lever to reach the down position.")]
        [Range(0.05f, 0.5f)]
        public float pullDownDuration = 0.12f;

        [Tooltip("How long the lever stays in the down position.")]
        [Range(0.05f, 0.5f)]
        public float holdDuration = 0.08f;

        [Tooltip("Time for the lever to spring back up.")]
        [Range(0.1f, 0.6f)]
        public float springUpDuration = 0.3f;

        [Tooltip("Position offset applied to the lever when pulled down, in local units.")]
        public Vector2 pullOffset = new Vector2(0f, -40f);

        // ---------------------------------------------------------------
        // Private State
        // ---------------------------------------------------------------

        private RectTransform _leverRect;
        private Vector2 _idlePosition;
        private bool _isAnimating = false;

        // ---------------------------------------------------------------
        // Unity Lifecycle
        // ---------------------------------------------------------------

        private void Awake()
        {
            _leverRect = leverImage != null ? leverImage.rectTransform : null;
            if (_leverRect != null)
                _idlePosition = _leverRect.anchoredPosition;
        }

        // ---------------------------------------------------------------
        // Public API
        // ---------------------------------------------------------------

        /// <summary>
        /// Plays the full lever pull animation sequence.
        /// Called by the GameManager before starting a spin.
        /// </summary>
        public void PlayPullAnimation()
        {
            if (_isAnimating) return;
            StartCoroutine(LeverPullSequence());
        }

        // ---------------------------------------------------------------
        // Animation Coroutine
        // ---------------------------------------------------------------

        private IEnumerator LeverPullSequence()
        {
            _isAnimating = true;

            // ── Pull Down ──────────────────────────────────────────────
            if (leverImage != null)
                leverImage.sprite = leverDownSprite;

            Vector2 downPosition = _idlePosition + pullOffset;

            float elapsed = 0f;
            while (elapsed < pullDownDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / pullDownDuration);
                if (_leverRect != null)
                    _leverRect.anchoredPosition = Vector2.Lerp(_idlePosition, downPosition, EaseInQuad(t));
                yield return null;
            }

            // ── Hold ───────────────────────────────────────────────────
            yield return new WaitForSeconds(holdDuration);

            // ── Spring Back Up ─────────────────────────────────────────
            if (leverImage != null)
                leverImage.sprite = leverUpSprite;

            elapsed = 0f;
            while (elapsed < springUpDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / springUpDuration);
                if (_leverRect != null)
                    _leverRect.anchoredPosition = Vector2.Lerp(downPosition, _idlePosition, EaseOutBounce(t));
                yield return null;
            }

            if (_leverRect != null)
                _leverRect.anchoredPosition = _idlePosition;

            _isAnimating = false;
        }

        // ---------------------------------------------------------------
        // Easing Functions
        // ---------------------------------------------------------------

        private static float EaseInQuad(float t) => t * t;

        /// <summary>Produces a bounce overshoot when returning to idle.</summary>
        private static float EaseOutBounce(float t)
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;

            if (t < 1f / d1)       return n1 * t * t;
            if (t < 2f / d1)       { t -= 1.5f   / d1; return n1 * t * t + 0.75f;   }
            if (t < 2.5f / d1)     { t -= 2.25f  / d1; return n1 * t * t + 0.9375f; }
                                     t -= 2.625f / d1; return n1 * t * t + 0.984375f;
        }
    }
}
