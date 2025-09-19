using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Coalition.Runtime.Data;

namespace Coalition.Runtime.UI
{
    /// <summary>
    /// Draggable party card for the coalition builder interface.
    /// Displays party information and provides drag-and-drop functionality.
    /// </summary>
    public class PartyCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Card Components")]
        [SerializeField] private Text partyNameText;
        [SerializeField] private Text seatCountText;
        [SerializeField] private Text votePercentageText;
        [SerializeField] private Image partyColorImage;
        [SerializeField] private Image cardBackground;
        [SerializeField] private Image cardBorder;

        [Header("Drag Behavior")]
        [SerializeField] private bool isDraggable = true;
        [SerializeField] private float dragOpacity = 0.7f;
        [SerializeField] private Vector3 hoverScale = new Vector3(1.05f, 1.05f, 1f);
        [SerializeField] private float animationDuration = 0.2f;

        [Header("Visual States")]
        [SerializeField] private Color normalBorderColor = Color.gray;
        [SerializeField] private Color hoverBorderColor = Color.white;
        [SerializeField] private Color dragBorderColor = Color.yellow;
        [SerializeField] private Color coalitionBorderColor = new Color(0.08f, 0.26f, 0.45f, 1f);

        // Card data
        private PartyResult partyResult;
        private CoalitionBuilder parentBuilder;
        private UITheme currentTheme;

        // Drag state
        private Canvas dragCanvas;
        private CanvasGroup canvasGroup;
        private RectTransform rectTransform;
        private Vector3 originalPosition;
        private Transform originalParent;
        private bool isDragging = false;
        private bool isInCoalition = false;

        // Visual state
        private Vector3 originalScale;
        private Color originalBackgroundColor;
        private System.Collections.IEnumerator currentAnimation;

        #region Properties

        public string PartyName => partyResult?.PartyName ?? "Unknown";
        public int SeatCount => partyResult?.Seats ?? 0;
        public float VotePercentage => partyResult?.VotePercentage ?? 0f;
        public bool IsInCoalition => isInCoalition;
        public bool IsDraggable => isDraggable;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            originalScale = transform.localScale;
            if (cardBackground != null)
                originalBackgroundColor = cardBackground.color;
        }

        private void OnDestroy()
        {
            StopAllAnimations();
        }

        #endregion

        #region Initialization

        private void InitializeComponents()
        {
            // Get required components
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();

            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            // Find drag canvas (should be the main canvas)
            dragCanvas = GetComponentInParent<Canvas>();
            while (dragCanvas.transform.parent != null && dragCanvas.transform.parent.GetComponent<Canvas>() != null)
            {
                dragCanvas = dragCanvas.transform.parent.GetComponent<Canvas>();
            }

            // Find components if not assigned
            if (partyNameText == null)
                partyNameText = GetComponentInChildren<Text>();

            var textComponents = GetComponentsInChildren<Text>();
            if (textComponents.Length > 1)
            {
                partyNameText = textComponents[0];
                seatCountText = textComponents[1];
                if (textComponents.Length > 2)
                    votePercentageText = textComponents[2];
            }

            var images = GetComponentsInChildren<Image>();
            if (images.Length > 0)
            {
                cardBackground = images[0];
                if (images.Length > 1)
                    partyColorImage = images[1];
                if (images.Length > 2)
                    cardBorder = images[2];
            }
        }

        /// <summary>
        /// Initialize the party card with data and parent builder.
        /// </summary>
        /// <param name="result">Party result data</param>
        /// <param name="builder">Parent coalition builder</param>
        public void Initialize(PartyResult result, CoalitionBuilder builder)
        {
            partyResult = result;
            parentBuilder = builder;

            UpdateCardDisplay();
            UpdateVisualState();

            Debug.Log($"[PartyCard] Initialized card for {result.PartyName} ({result.Seats} seats)");
        }

        #endregion

        #region Card Display

        private void UpdateCardDisplay()
        {
            if (partyResult == null) return;

            // Update texts
            if (partyNameText != null)
                partyNameText.text = partyResult.PartyName;

            if (seatCountText != null)
                seatCountText.text = $"{partyResult.Seats} seats";

            if (votePercentageText != null)
                votePercentageText.text = $"{partyResult.VotePercentage:F1}%";

            // Update party color
            if (partyColorImage != null)
            {
                var partyColor = GetPartyColor(partyResult.PartyName);
                partyColorImage.color = partyColor;
            }
        }

        private Color GetPartyColor(string partyName)
        {
            if (currentTheme != null)
            {
                return currentTheme.GetPartyColor(partyName);
            }

            // Fallback colors
            var colorMap = new System.Collections.Generic.Dictionary<string, Color>
            {
                ["VVD"] = new Color(0.0f, 0.4f, 0.8f, 1f),
                ["PVV"] = new Color(1.0f, 0.8f, 0.0f, 1f),
                ["NSC"] = new Color(0.0f, 0.6f, 0.4f, 1f),
                ["GL-PvdA"] = new Color(0.6f, 0.8f, 0.2f, 1f),
                ["D66"] = new Color(0.0f, 0.8f, 0.2f, 1f),
                ["BBB"] = new Color(1.0f, 0.6f, 0.0f, 1f),
                ["CDA"] = new Color(0.2f, 0.6f, 0.8f, 1f),
                ["SP"] = new Color(0.8f, 0.2f, 0.2f, 1f),
                ["FvD"] = new Color(0.5f, 0.3f, 0.7f, 1f),
                ["CU"] = new Color(0.3f, 0.5f, 0.8f, 1f),
                ["SGP"] = new Color(1.0f, 0.4f, 0.0f, 1f),
                ["Volt"] = new Color(0.8f, 0.0f, 0.8f, 1f)
            };

            return colorMap.ContainsKey(partyName) ? colorMap[partyName] : Color.gray;
        }

        private void UpdateVisualState()
        {
            if (cardBorder != null)
            {
                Color borderColor;

                if (isDragging)
                    borderColor = dragBorderColor;
                else if (isInCoalition)
                    borderColor = coalitionBorderColor;
                else
                    borderColor = normalBorderColor;

                cardBorder.color = borderColor;
            }
        }

        #endregion

        #region Drag and Drop

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!isDraggable) return;

            isDragging = true;
            originalPosition = rectTransform.anchoredPosition;
            originalParent = transform.parent;

            // Move to drag canvas for proper layering
            transform.SetParent(dragCanvas.transform, true);
            transform.SetAsLastSibling();

            // Reduce opacity during drag
            canvasGroup.alpha = dragOpacity;
            canvasGroup.blocksRaycasts = false;

            UpdateVisualState();

            Debug.Log($"[PartyCard] Started dragging {PartyName}");
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging) return;

            // Update position to follow mouse
            Vector2 localPointerPosition;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                dragCanvas.transform as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out localPointerPosition))
            {
                rectTransform.localPosition = localPointerPosition;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isDragging) return;

            isDragging = false;
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;

            UpdateVisualState();

            // Let the coalition builder handle the drop logic
            if (parentBuilder != null)
            {
                parentBuilder.OnDrop(eventData);
            }
            else
            {
                ReturnToOriginalPosition();
            }

            Debug.Log($"[PartyCard] Finished dragging {PartyName}");
        }

        public void ReturnToOriginalPosition()
        {
            if (originalParent != null)
            {
                transform.SetParent(originalParent, false);
                rectTransform.anchoredPosition = originalPosition;
            }

            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            isDragging = false;

            UpdateVisualState();
        }

        #endregion

        #region Hover Effects

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (isDragging) return;

            AnimateScale(hoverScale);

            if (cardBorder != null)
                cardBorder.color = hoverBorderColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (isDragging) return;

            AnimateScale(originalScale);
            UpdateVisualState();
        }

        #endregion

        #region Animation

        private void AnimateScale(Vector3 targetScale)
        {
            StopCurrentAnimation();
            currentAnimation = AnimateScaleCoroutine(targetScale);
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

        #region Coalition State

        /// <summary>
        /// Set whether this card is in the coalition.
        /// </summary>
        /// <param name="inCoalition">Coalition status</param>
        public void SetCoalitionStatus(bool inCoalition)
        {
            isInCoalition = inCoalition;
            UpdateVisualState();
        }

        /// <summary>
        /// Flash the card to draw attention.
        /// </summary>
        public void Flash(Color flashColor, int flashCount = 3, float flashDuration = 1f)
        {
            StopCurrentAnimation();
            currentAnimation = FlashCoroutine(flashColor, flashCount, flashDuration);
            StartCoroutine(currentAnimation);
        }

        private System.Collections.IEnumerator FlashCoroutine(Color flashColor, int flashCount, float flashDuration)
        {
            if (cardBackground == null) yield break;

            var singleFlashDuration = flashDuration / (flashCount * 2);

            for (int i = 0; i < flashCount; i++)
            {
                // Flash to attention color
                cardBackground.color = flashColor;
                yield return new WaitForSeconds(singleFlashDuration);

                // Return to original
                cardBackground.color = originalBackgroundColor;
                yield return new WaitForSeconds(singleFlashDuration);
            }

            currentAnimation = null;
        }

        #endregion

        #region Theme Support

        /// <summary>
        /// Apply UI theme to the party card.
        /// </summary>
        /// <param name="theme">Theme to apply</param>
        public void ApplyTheme(UITheme theme)
        {
            currentTheme = theme;

            if (theme == null) return;

            // Update text colors and fonts
            var textComponents = new[] { partyNameText, seatCountText, votePercentageText };
            foreach (var text in textComponents)
            {
                if (text != null)
                {
                    text.color = theme.TextColor;
                    if (theme.Font != null)
                        text.font = theme.Font;
                }
            }

            // Update background colors
            if (cardBackground != null)
            {
                cardBackground.color = theme.PanelBackgroundColor;
                originalBackgroundColor = cardBackground.color;
            }

            // Update border colors based on theme
            normalBorderColor = theme.LightGray;
            hoverBorderColor = theme.GetLightAccentColor(0.3f);
            coalitionBorderColor = theme.RijksBlauw;

            // Update party color
            if (partyColorImage != null && partyResult != null)
            {
                partyColorImage.color = theme.GetPartyColor(partyResult.PartyName);
            }

            UpdateVisualState();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Enable or disable dragging for this card.
        /// </summary>
        /// <param name="draggable">Whether the card should be draggable</param>
        public void SetDraggable(bool draggable)
        {
            isDraggable = draggable;

            // Update visual indication of draggability
            if (canvasGroup != null)
            {
                canvasGroup.alpha = draggable ? 1f : 0.6f;
            }
        }

        /// <summary>
        /// Get card information summary.
        /// </summary>
        /// <returns>Card information</returns>
        public PartyCardInfo GetInfo()
        {
            return new PartyCardInfo
            {
                PartyName = PartyName,
                SeatCount = SeatCount,
                VotePercentage = VotePercentage,
                IsInCoalition = isInCoalition,
                IsDraggable = isDraggable,
                IsDragging = isDragging
            };
        }

        /// <summary>
        /// Update the card with new party result data.
        /// </summary>
        /// <param name="result">New party result</param>
        public void UpdateData(PartyResult result)
        {
            partyResult = result;
            UpdateCardDisplay();
        }

        #endregion

#if UNITY_EDITOR
        [ContextMenu("Flash Card")]
        private void TestFlash()
        {
            Flash(Color.yellow, 3, 1f);
        }

        [ContextMenu("Toggle Coalition Status")]
        private void TestToggleCoalitionStatus()
        {
            SetCoalitionStatus(!isInCoalition);
        }

        [ContextMenu("Toggle Draggable")]
        private void TestToggleDraggable()
        {
            SetDraggable(!isDraggable);
        }

        [ContextMenu("Debug Card Info")]
        private void DebugCardInfo()
        {
            var info = GetInfo();
            Debug.Log($"Party Card Info:\n" +
                     $"  Name: {info.PartyName}\n" +
                     $"  Seats: {info.SeatCount}\n" +
                     $"  Vote %: {info.VotePercentage:F1}%\n" +
                     $"  In Coalition: {info.IsInCoalition}\n" +
                     $"  Draggable: {info.IsDraggable}\n" +
                     $"  Dragging: {info.IsDragging}");
        }
#endif
    }

    /// <summary>
    /// Party card information data structure.
    /// </summary>
    [System.Serializable]
    public struct PartyCardInfo
    {
        public string PartyName;
        public int SeatCount;
        public float VotePercentage;
        public bool IsInCoalition;
        public bool IsDraggable;
        public bool IsDragging;
    }
}