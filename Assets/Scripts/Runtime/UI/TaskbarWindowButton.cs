using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Coalition.Runtime.UI
{
    /// <summary>
    /// Individual window button for the desktop taskbar.
    /// Displays window title, icon, and provides click interaction.
    /// </summary>
    public class TaskbarWindowButton : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Button Components")]
        [SerializeField] private Button button;
        [SerializeField] private Text titleText;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private GameObject activeIndicator;

        [Header("Button Styling")]
        [SerializeField] private Color normalColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        [SerializeField] private Color activeColor = new Color(0.08f, 0.26f, 0.45f, 1f);
        [SerializeField] private Color hoverColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        [SerializeField] private Color minimizedColor = new Color(0.6f, 0.6f, 0.6f, 1f);

        [Header("Button Behavior")]
        [SerializeField] private bool showTooltip = true;
        [SerializeField] private float tooltipDelay = 1f;
        [SerializeField] private bool showPreviewOnHover = false;

        // State
        private Window associatedWindow;
        private DesktopTaskbar parentTaskbar;
        private bool isActive = false;
        private bool isHovered = false;

        // Tooltip and preview
        private GameObject tooltipObject;
        private float hoverStartTime;
        private bool tooltipShown = false;

        #region Properties

        public Window AssociatedWindow => associatedWindow;
        public bool IsActive => isActive;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeComponents();
        }

        private void Update()
        {
            UpdateTooltip();
            UpdateButtonState();
        }

        private void OnDestroy()
        {
            CleanupTooltip();
        }

        #endregion

        #region Initialization

        private void InitializeComponents()
        {
            // Find components if not assigned
            if (button == null)
                button = GetComponent<Button>();

            if (button == null)
                button = gameObject.AddComponent<Button>();

            if (titleText == null)
                titleText = GetComponentInChildren<Text>();

            if (iconImage == null)
            {
                var images = GetComponentsInChildren<Image>();
                foreach (var img in images)
                {
                    if (img != backgroundImage && img.name.ToLower().Contains("icon"))
                    {
                        iconImage = img;
                        break;
                    }
                }
            }

            if (backgroundImage == null)
                backgroundImage = GetComponent<Image>();

            // Setup button
            if (button != null)
            {
                button.onClick.AddListener(OnButtonClicked);
            }
        }

        /// <summary>
        /// Initialize the window button with its associated window and taskbar.
        /// </summary>
        /// <param name="window">Associated window</param>
        /// <param name="taskbar">Parent taskbar</param>
        public void Initialize(Window window, DesktopTaskbar taskbar)
        {
            associatedWindow = window;
            parentTaskbar = taskbar;

            UpdateButtonDisplay();
            SetActive(false);

            Debug.Log($"[TaskbarWindowButton] Initialized button for window: {window.WindowTitle}");
        }

        #endregion

        #region Button State Management

        /// <summary>
        /// Set the active state of the button.
        /// </summary>
        /// <param name="active">Whether the button should be active</param>
        public void SetActive(bool active)
        {
            isActive = active;
            UpdateVisualState();

            if (activeIndicator != null)
            {
                activeIndicator.SetActive(active);
            }
        }

        private void UpdateButtonState()
        {
            if (associatedWindow == null) return;

            // Update button display based on window state
            var title = associatedWindow.WindowTitle;
            if (titleText != null && titleText.text != title)
            {
                titleText.text = title;
            }

            UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            if (backgroundImage == null) return;

            Color targetColor;

            if (associatedWindow != null && associatedWindow.IsMinimized)
            {
                targetColor = minimizedColor;
            }
            else if (isActive)
            {
                targetColor = activeColor;
            }
            else if (isHovered)
            {
                targetColor = hoverColor;
            }
            else
            {
                targetColor = normalColor;
            }

            backgroundImage.color = targetColor;
        }

        private void UpdateButtonDisplay()
        {
            if (associatedWindow == null) return;

            // Update title
            if (titleText != null)
            {
                titleText.text = associatedWindow.WindowTitle;
            }

            // Update icon (placeholder - could be extended with actual window icons)
            if (iconImage != null)
            {
                // For now, use a default icon or hide if no icon available
                iconImage.gameObject.SetActive(false);
            }
        }

        #endregion

        #region Input Handling

        public void OnPointerClick(PointerEventData eventData)
        {
            OnButtonClicked();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;
            hoverStartTime = Time.time;
            tooltipShown = false;
            UpdateVisualState();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            CleanupTooltip();
            UpdateVisualState();
        }

        private void OnButtonClicked()
        {
            if (associatedWindow == null || parentTaskbar == null) return;

            parentTaskbar.OnWindowButtonClicked(associatedWindow);
            Debug.Log($"[TaskbarWindowButton] Button clicked for window: {associatedWindow.WindowTitle}");
        }

        #endregion

        #region Tooltip System

        private void UpdateTooltip()
        {
            if (!showTooltip || !isHovered || tooltipShown) return;

            if (Time.time - hoverStartTime >= tooltipDelay)
            {
                ShowTooltip();
                tooltipShown = true;
            }
        }

        private void ShowTooltip()
        {
            if (associatedWindow == null) return;

            // Create a simple tooltip
            var tooltipText = $"{associatedWindow.WindowTitle}";
            if (associatedWindow.IsMinimized)
                tooltipText += " (Minimized)";
            if (associatedWindow.IsMaximized)
                tooltipText += " (Maximized)";

            CreateTooltip(tooltipText);
        }

        private void CreateTooltip(string text)
        {
            CleanupTooltip();

            // Find or create tooltip canvas
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;

            // Create tooltip object
            tooltipObject = new GameObject("Tooltip");
            tooltipObject.transform.SetParent(canvas.transform, false);

            // Add background
            var backgroundImage = tooltipObject.AddComponent<Image>();
            backgroundImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

            // Add text
            var textObject = new GameObject("Text");
            textObject.transform.SetParent(tooltipObject.transform, false);

            var textComponent = textObject.AddComponent<Text>();
            textComponent.text = text;
            textComponent.color = Color.white;
            textComponent.fontSize = 12;
            textComponent.alignment = TextAnchor.MiddleCenter;

            // Use default font or theme font
            var theme = FindObjectOfType<UIManager>()?.GetCurrentTheme();
            if (theme?.Font != null)
            {
                textComponent.font = theme.Font;
            }
            else
            {
                textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            // Size and position tooltip
            var rectTransform = tooltipObject.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(text.Length * 8 + 20, 30);

            // Position above the button
            var buttonRect = GetComponent<RectTransform>();
            var worldPos = buttonRect.TransformPoint(Vector3.up * (buttonRect.rect.height + 10));
            rectTransform.position = worldPos;

            // Ensure tooltip stays on screen
            var canvasRect = canvas.GetComponent<RectTransform>().rect;
            var tooltipPos = rectTransform.anchoredPosition;

            if (tooltipPos.x + rectTransform.rect.width > canvasRect.width)
            {
                tooltipPos.x = canvasRect.width - rectTransform.rect.width;
                rectTransform.anchoredPosition = tooltipPos;
            }
        }

        private void CleanupTooltip()
        {
            if (tooltipObject != null)
            {
                Destroy(tooltipObject);
                tooltipObject = null;
            }
            tooltipShown = false;
        }

        #endregion

        #region Theme Support

        /// <summary>
        /// Apply UI theme to the window button.
        /// </summary>
        /// <param name="theme">Theme to apply</param>
        public void ApplyTheme(UITheme theme)
        {
            if (theme == null) return;

            // Update colors based on theme
            normalColor = theme.LightGray;
            activeColor = theme.RijksBlauw;
            hoverColor = theme.GetLightAccentColor(0.2f);
            minimizedColor = theme.DarkGray;

            // Apply to text
            if (titleText != null)
            {
                titleText.color = theme.TextColor;
                if (theme.Font != null)
                    titleText.font = theme.Font;
            }

            // Apply to button
            if (button != null)
            {
                var colors = button.colors;
                colors.normalColor = normalColor;
                colors.highlightedColor = hoverColor;
                colors.pressedColor = activeColor;
                colors.disabledColor = minimizedColor;
                button.colors = colors;
            }

            UpdateVisualState();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Refresh the button display from the associated window.
        /// </summary>
        public void RefreshDisplay()
        {
            UpdateButtonDisplay();
            UpdateVisualState();
        }

        /// <summary>
        /// Flash the button to draw attention.
        /// </summary>
        public void FlashButton()
        {
            StartCoroutine(FlashAnimation());
        }

        private System.Collections.IEnumerator FlashAnimation()
        {
            var originalColor = backgroundImage.color;
            var flashColor = Color.yellow;
            var flashDuration = 0.5f;

            for (int i = 0; i < 3; i++)
            {
                // Flash to attention color
                backgroundImage.color = flashColor;
                yield return new WaitForSeconds(flashDuration / 6);

                // Return to original
                backgroundImage.color = originalColor;
                yield return new WaitForSeconds(flashDuration / 6);
            }

            UpdateVisualState();
        }

        #endregion

#if UNITY_EDITOR
        [ContextMenu("Test Flash")]
        private void TestFlash()
        {
            FlashButton();
        }

        [ContextMenu("Refresh Display")]
        private void TestRefreshDisplay()
        {
            RefreshDisplay();
        }
#endif
    }
}