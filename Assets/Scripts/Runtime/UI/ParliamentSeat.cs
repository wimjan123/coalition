using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Coalition.Runtime.UI
{
    /// <summary>
    /// Individual parliament seat component with interactive functionality.
    /// Represents a single seat in the Tweede Kamer visualization.
    /// </summary>
    public class ParliamentSeat : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("Visual Components")]
        [SerializeField] private Image seatImage;
        [SerializeField] private Image borderImage;
        [SerializeField] private Image glowImage;
        [SerializeField] private Text seatNumberText;

        [Header("Visual States")]
        [SerializeField] private Color defaultColor = Color.gray;
        [SerializeField] private Color hoverColor = Color.white;
        [SerializeField] private float hoverScale = 1.1f;
        [SerializeField] private float animationDuration = 0.2f;

        [Header("Coalition Highlighting")]
        [SerializeField] private bool isCoalitionSeat = false;
        [SerializeField] private Color coalitionBorderColor = Color.yellow;
        [SerializeField] private float coalitionGlowIntensity = 1.5f;

        // Seat data
        private int seatIndex;
        private string assignedParty;
        private ParliamentVisualization parentVisualization;

        // Visual state
        private Color currentColor;
        private bool isHovered = false;
        private Vector3 originalScale;

        // Animation
        private System.Collections.IEnumerator currentAnimation;

        #region Properties

        public int SeatIndex
        {
            get => seatIndex;
            set => seatIndex = value;
        }

        public string AssignedParty
        {
            get => assignedParty;
            set => SetAssignedParty(value);
        }

        public bool IsCoalitionSeat
        {
            get => isCoalitionSeat;
            set => SetCoalitionStatus(value);
        }

        public Color SeatColor
        {
            get => currentColor;
            set => SetColor(value);
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            originalScale = transform.localScale;
            UpdateVisual();
        }

        private void OnDestroy()
        {
            StopAllAnimations();
        }

        #endregion

        #region Initialization

        private void InitializeComponents()
        {
            // Find components if not assigned
            if (seatImage == null)
                seatImage = GetComponent<Image>();

            if (seatImage == null)
            {
                seatImage = gameObject.AddComponent<Image>();
                seatImage.raycastTarget = true;
            }

            if (borderImage == null)
            {
                // Create border image as child
                var borderObject = new GameObject("Border");
                borderObject.transform.SetParent(transform, false);
                borderImage = borderObject.AddComponent<Image>();

                var borderRect = borderImage.GetComponent<RectTransform>();
                borderRect.anchorMin = Vector2.zero;
                borderRect.anchorMax = Vector2.one;
                borderRect.offsetMin = Vector2.zero;
                borderRect.offsetMax = Vector2.zero;

                borderImage.color = Color.clear;
                borderImage.raycastTarget = false;
            }

            if (glowImage == null)
            {
                // Create glow image as child
                var glowObject = new GameObject("Glow");
                glowObject.transform.SetParent(transform, false);
                glowImage = glowObject.AddComponent<Image>();

                var glowRect = glowImage.GetComponent<RectTransform>();
                glowRect.anchorMin = Vector2.zero;
                glowRect.anchorMax = Vector2.one;
                glowRect.offsetMin = Vector2.one * -2; // Slightly larger for glow effect
                glowRect.offsetMax = Vector2.one * 2;

                glowImage.color = Color.clear;
                glowImage.raycastTarget = false;
            }

            if (seatNumberText == null)
            {
                // Create seat number text as child (optional)
                var textObject = new GameObject("SeatNumber");
                textObject.transform.SetParent(transform, false);
                seatNumberText = textObject.AddComponent<Text>();

                var textRect = seatNumberText.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;

                seatNumberText.text = "";
                seatNumberText.alignment = TextAnchor.MiddleCenter;
                seatNumberText.fontSize = 8;
                seatNumberText.color = Color.white;
                seatNumberText.raycastTarget = false;

                // Use default font
                seatNumberText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            currentColor = defaultColor;
        }

        /// <summary>
        /// Initialize the seat with its index and parent visualization.
        /// </summary>
        /// <param name="index">Seat index (0-149)</param>
        /// <param name="visualization">Parent parliament visualization</param>
        public void Initialize(int index, ParliamentVisualization visualization)
        {
            seatIndex = index;
            parentVisualization = visualization;

            // Optionally show seat number
            if (seatNumberText != null && seatNumberText.gameObject.activeInHierarchy)
            {
                seatNumberText.text = (index + 1).ToString();
            }

            UpdateVisual();
        }

        #endregion

        #region Seat Management

        private void SetAssignedParty(string party)
        {
            assignedParty = party;
            UpdateVisual();
        }

        public void SetColor(Color color)
        {
            currentColor = color;
            UpdateVisual();
        }

        public void SetCoalitionStatus(bool isCoalition, Color borderColor = default, float glowIntensity = 1.5f)
        {
            isCoalitionSeat = isCoalition;

            if (isCoalition)
            {
                coalitionBorderColor = borderColor != default ? borderColor : coalitionBorderColor;
                coalitionGlowIntensity = glowIntensity;
            }

            UpdateCoalitionVisuals();
        }

        #endregion

        #region Visual Updates

        public void UpdateVisual()
        {
            if (seatImage == null) return;

            // Update main seat color
            var displayColor = isHovered ? Color.Lerp(currentColor, hoverColor, 0.5f) : currentColor;
            seatImage.color = displayColor;

            // Update coalition visuals
            UpdateCoalitionVisuals();
        }

        private void UpdateCoalitionVisuals()
        {
            if (borderImage != null)
            {
                borderImage.color = isCoalitionSeat ? coalitionBorderColor : Color.clear;
            }

            if (glowImage != null)
            {
                var glowColor = isCoalitionSeat ? coalitionBorderColor : Color.clear;
                glowColor.a = isCoalitionSeat ? 0.3f * coalitionGlowIntensity : 0f;
                glowImage.color = glowColor;
            }
        }

        #endregion

        #region Input Handling

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;
            AnimateScale(originalScale * hoverScale);
            UpdateVisual();

            // Notify parent visualization
            parentVisualization?.OnSeatHovered(this, true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            AnimateScale(originalScale);
            UpdateVisual();

            // Notify parent visualization
            parentVisualization?.OnSeatHovered(this, false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // Animate click feedback
            AnimateClick();

            // Notify parent visualization
            parentVisualization?.OnSeatClicked(this);
        }

        #endregion

        #region Animation

        private void AnimateScale(Vector3 targetScale)
        {
            StopCurrentAnimation();
            currentAnimation = AnimateScaleCoroutine(targetScale);
            StartCoroutine(currentAnimation);
        }

        private void AnimateClick()
        {
            StopCurrentAnimation();
            currentAnimation = AnimateClickCoroutine();
            StartCoroutine(currentAnimation);
        }

        private System.Collections.IEnumerator AnimateScaleCoroutine(Vector3 targetScale)
        {
            var startScale = transform.localScale;
            var elapsedTime = 0f;

            while (elapsedTime < animationDuration)
            {
                elapsedTime += Time.deltaTime;
                var progress = elapsedTime / animationDuration;
                var easedProgress = Mathf.SmoothStep(0, 1, progress);

                transform.localScale = Vector3.Lerp(startScale, targetScale, easedProgress);
                yield return null;
            }

            transform.localScale = targetScale;
            currentAnimation = null;
        }

        private System.Collections.IEnumerator AnimateClickCoroutine()
        {
            var startScale = transform.localScale;
            var clickScale = startScale * 1.2f;
            var duration = animationDuration * 0.5f;

            // Scale up
            var elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                var progress = elapsedTime / duration;
                transform.localScale = Vector3.Lerp(startScale, clickScale, progress);
                yield return null;
            }

            // Scale back down
            elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                var progress = elapsedTime / duration;
                transform.localScale = Vector3.Lerp(clickScale, startScale, progress);
                yield return null;
            }

            transform.localScale = startScale;
            currentAnimation = null;
        }

        private void StopCurrentAnimation()
        {
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
                currentAnimation = null;
            }
        }

        private void StopAllAnimations()
        {
            StopCurrentAnimation();
        }

        #endregion

        #region Theme Support

        /// <summary>
        /// Apply UI theme to the seat.
        /// </summary>
        /// <param name="theme">Theme to apply</param>
        public void ApplyTheme(UITheme theme)
        {
            if (theme == null) return;

            // Update text styling
            if (seatNumberText != null)
            {
                seatNumberText.color = theme.WindowTitleTextColor;
                if (theme.Font != null)
                    seatNumberText.font = theme.Font;
            }

            // Update default hover color based on theme
            hoverColor = theme.GetLightAccentColor(0.3f);

            UpdateVisual();
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Show or hide the seat number text.
        /// </summary>
        /// <param name="show">Whether to show seat numbers</param>
        public void ShowSeatNumber(bool show)
        {
            if (seatNumberText != null)
            {
                seatNumberText.gameObject.SetActive(show);
            }
        }

        /// <summary>
        /// Set seat transparency.
        /// </summary>
        /// <param name="alpha">Alpha value (0-1)</param>
        public void SetAlpha(float alpha)
        {
            alpha = Mathf.Clamp01(alpha);

            if (seatImage != null)
            {
                var color = seatImage.color;
                color.a = alpha;
                seatImage.color = color;
            }

            if (borderImage != null)
            {
                var color = borderImage.color;
                color.a = alpha * (isCoalitionSeat ? 1f : 0f);
                borderImage.color = color;
            }

            if (glowImage != null)
            {
                var color = glowImage.color;
                color.a = alpha * (isCoalitionSeat ? 0.3f * coalitionGlowIntensity : 0f);
                glowImage.color = color;
            }
        }

        /// <summary>
        /// Flash the seat to draw attention.
        /// </summary>
        public void Flash(Color flashColor, int flashCount = 3, float flashDuration = 0.5f)
        {
            StopCurrentAnimation();
            currentAnimation = FlashCoroutine(flashColor, flashCount, flashDuration);
            StartCoroutine(currentAnimation);
        }

        private System.Collections.IEnumerator FlashCoroutine(Color flashColor, int flashCount, float flashDuration)
        {
            var originalColor = seatImage.color;
            var singleFlashDuration = flashDuration / (flashCount * 2);

            for (int i = 0; i < flashCount; i++)
            {
                // Flash to attention color
                seatImage.color = flashColor;
                yield return new WaitForSeconds(singleFlashDuration);

                // Return to original
                seatImage.color = originalColor;
                yield return new WaitForSeconds(singleFlashDuration);
            }

            currentAnimation = null;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get seat information summary.
        /// </summary>
        /// <returns>Seat information</returns>
        public SeatInfo GetInfo()
        {
            return new SeatInfo
            {
                SeatIndex = seatIndex,
                AssignedParty = assignedParty,
                IsCoalitionSeat = isCoalitionSeat,
                Color = currentColor,
                IsHovered = isHovered
            };
        }

        #endregion

#if UNITY_EDITOR
        [ContextMenu("Flash Seat")]
        private void TestFlash()
        {
            Flash(Color.yellow, 3, 1f);
        }

        [ContextMenu("Toggle Seat Number")]
        private void TestToggleSeatNumber()
        {
            if (seatNumberText != null)
            {
                ShowSeatNumber(!seatNumberText.gameObject.activeInHierarchy);
            }
        }

        [ContextMenu("Toggle Coalition Status")]
        private void TestToggleCoalitionStatus()
        {
            SetCoalitionStatus(!isCoalitionSeat);
        }
#endif
    }

    /// <summary>
    /// Seat information data structure.
    /// </summary>
    [System.Serializable]
    public struct SeatInfo
    {
        public int SeatIndex;
        public string AssignedParty;
        public bool IsCoalitionSeat;
        public Color Color;
        public bool IsHovered;
    }
}