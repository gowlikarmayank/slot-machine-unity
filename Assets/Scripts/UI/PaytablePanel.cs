using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SlotMachine.UI
{
    /// <summary>
    /// Bonus Feature: Paytable Panel
    /// 
    /// Slides in from the right side of the screen, showing all winning
    /// combinations and their payout multipliers. Activated by a "?" info button.
    /// 
    /// Uses the provided symbol sprites to build the table dynamically at runtime.
    /// </summary>
    public class PaytablePanel : MonoBehaviour
    {
        // ---------------------------------------------------------------
        // Inspector References
        // ---------------------------------------------------------------

        [Header("Panel")]
        [Tooltip("RectTransform of the full paytable panel.")]
        public RectTransform panelRect;

        [Tooltip("CanvasGroup controlling panel visibility.")]
        public CanvasGroup panelGroup;

        [Header("Row Template")]
        [Tooltip("Prefab for a single paytable row (Icon × 3 + Text).")]
        public GameObject rowPrefab;

        [Tooltip("Parent transform where rows are instantiated.")]
        public Transform rowContainer;

        [Header("Symbols")]
        [Tooltip("All symbol data to display — must have sprites assigned.")]
        public List<Symbols.SymbolData> symbolsForTable = new List<Symbols.SymbolData>();

        [Header("Slide Animation")]
        [Tooltip("X position when panel is hidden (off-screen right).")]
        public float hiddenX = 700f;

        [Tooltip("X position when panel is visible (on-screen).")]
        public float visibleX = 0f;

        [Tooltip("Duration of the slide animation in seconds.")]
        [Range(0.1f, 0.8f)]
        public float slideDuration = 0.35f;

        // ---------------------------------------------------------------
        // Private State
        // ---------------------------------------------------------------

        private bool _isVisible = false;
        private Coroutine _slideCoroutine;

        // ---------------------------------------------------------------
        // Unity Lifecycle
        // ---------------------------------------------------------------

        private void Start()
        {
            // Start hidden
            if (panelRect   != null) panelRect.anchoredPosition = new Vector2(hiddenX, panelRect.anchoredPosition.y);
            if (panelGroup  != null) { panelGroup.alpha = 0f; panelGroup.blocksRaycasts = false; }

            BuildTable();
        }

        // ---------------------------------------------------------------
        // Public API
        // ---------------------------------------------------------------

        /// <summary>Toggles the paytable panel visible/hidden.</summary>
        public void Toggle()
        {
            if (_isVisible) Hide(); else Show();
        }

        public void Show()
        {
            _isVisible = true;
            if (_slideCoroutine != null) StopCoroutine(_slideCoroutine);
            _slideCoroutine = StartCoroutine(SlidePanel(visibleX, 1f));
            if (panelGroup != null) panelGroup.blocksRaycasts = true;
        }

        public void Hide()
        {
            _isVisible = false;
            if (_slideCoroutine != null) StopCoroutine(_slideCoroutine);
            _slideCoroutine = StartCoroutine(SlidePanel(hiddenX, 0f));
            if (panelGroup != null) panelGroup.blocksRaycasts = false;
        }

        // ---------------------------------------------------------------
        // Table Builder
        // ---------------------------------------------------------------

        /// <summary>
        /// Dynamically creates paytable rows at runtime from symbolsForTable.
        /// Each row shows the symbol's 3 icons and payout multiplier text.
        /// </summary>
        private void BuildTable()
        {
            if (rowPrefab == null || rowContainer == null) return;

            // Clear any existing rows (e.g., from a previous build)
            foreach (Transform child in rowContainer)
                Destroy(child.gameObject);

            // Special row at the top: Wild Bonus
            CreateSpecialRow("2× WILD", "= FREE SPIN");

            // One row per symbol (sorted by payout descending for clarity)
            var sorted = new List<Symbols.SymbolData>(symbolsForTable);
            sorted.Sort((a, b) => b.payoutMultiplier.CompareTo(a.payoutMultiplier));

            foreach (var sym in sorted)
            {
                if (sym == null || sym.isWild) continue;
                string payLabel = $"= {sym.payoutMultiplier}× BET";
                CreateSymbolRow(sym.sprite, sym.DisplayName, payLabel);
            }

            // Partial win row
            CreateSpecialRow("ANY 2 SAME", "= BET BACK");
        }

        private void CreateSymbolRow(Sprite sprite, string symbolName, string payText)
        {
            GameObject row = Instantiate(rowPrefab, rowContainer);
            // Assumes rowPrefab has Image children for 3 icons, and 2 TMP texts
            Image[] icons = row.GetComponentsInChildren<Image>();
            for (int i = 0; i < 3 && i < icons.Length; i++)
                icons[i].sprite = sprite;

            TextMeshProUGUI[] texts = row.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length > 0) texts[0].text = symbolName;
            if (texts.Length > 1) texts[1].text = payText;
        }

        private void CreateSpecialRow(string label, string payText)
        {
            GameObject row = Instantiate(rowPrefab, rowContainer);
            TextMeshProUGUI[] texts = row.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length > 0) texts[0].text = label;
            if (texts.Length > 1) texts[1].text = payText;

            // Disable icons for special rows
            Image[] icons = row.GetComponentsInChildren<Image>();
            foreach (Image img in icons)
                img.gameObject.SetActive(false);
        }

        // ---------------------------------------------------------------
        // Slide Coroutine
        // ---------------------------------------------------------------

        private IEnumerator SlidePanel(float targetX, float targetAlpha)
        {
            Vector2 startPos = panelRect.anchoredPosition;
            float startAlpha = panelGroup != null ? panelGroup.alpha : 0f;
            float elapsed = 0f;

            while (elapsed < slideDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / slideDuration);
                float eased = EaseInOutQuart(t);

                panelRect.anchoredPosition = new Vector2(
                    Mathf.Lerp(startPos.x, targetX, eased),
                    startPos.y
                );

                if (panelGroup != null)
                    panelGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);

                yield return null;
            }

            panelRect.anchoredPosition = new Vector2(targetX, startPos.y);
            if (panelGroup != null) panelGroup.alpha = targetAlpha;
        }

        private static float EaseInOutQuart(float t)
        {
            return t < 0.5f
                ? 8f * t * t * t * t
                : 1f - Mathf.Pow(-2f * t + 2f, 4f) * 0.5f;
        }
    }
}
