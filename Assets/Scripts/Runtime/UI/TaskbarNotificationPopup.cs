using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Coalition.Runtime.UI
{
    /// <summary>
    /// Notification popup component for the desktop taskbar.
    /// Displays temporary notifications with fade in/out animations.
    /// </summary>
    public class TaskbarNotificationPopup : MonoBehaviour
    {
        [Header("Notification Components")]
        [SerializeField] private Text titleText;
        [SerializeField] private Text messageText;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Button closeButton;

        [Header("Animation Settings")]
        [SerializeField] private float slideInDuration = 0.5f;
        [SerializeField] private float displayDuration = 5f;
        [SerializeField] private float slideOutDuration = 0.3f;
        [SerializeField] private AnimationCurve slideInCurve = AnimationCurve.EaseOut(0, 0, 1, 1);
        [SerializeField] private AnimationCurve slideOutCurve = AnimationCurve.EaseIn(0, 0, 1, 1);

        [Header("Visual Settings")]
        [SerializeField] private Vector2 slideOffset = new Vector2(300, 0);
        [SerializeField] private Color normalBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        [SerializeField] private Color warningBackgroundColor = new Color(0.8f, 0.6f, 0.2f, 0.9f);
        [SerializeField] private Color errorBackgroundColor = new Color(0.8f, 0.2f, 0.2f, 0.9f);

        // Notification data
        private TaskbarNotification notification;
        private NotificationType notificationType = NotificationType.Info;

        // Animation state
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Vector2 originalPosition;
        private bool isAnimating = false;
        private Coroutine currentAnimation;

        // Auto-dismiss
        private bool autoDismiss = true;
        private float timeRemaining;

        #region Properties

        public TaskbarNotification Notification => notification;
        public bool IsAnimating => isAnimating;
        public float TimeRemaining => timeRemaining;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            SetupEventHandlers();
        }

        private void Update()
        {
            UpdateAutoDismiss();
        }

        private void OnDestroy()
        {
            StopAllAnimations();
        }

        #endregion

        #region Initialization

        private void InitializeComponents()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();

            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            // Find components if not assigned
            if (titleText == null)
            {
                var texts = GetComponentsInChildren<Text>();
                if (texts.Length > 0) titleText = texts[0];
                if (texts.Length > 1) messageText = texts[1];
            }

            if (iconImage == null)
            {
                var images = GetComponentsInChildren<Image>();
                foreach (var img in images)
                {
                    if (img.name.ToLower().Contains("icon"))
                    {
                        iconImage = img;
                        break;
                    }
                }
            }

            if (backgroundImage == null)
                backgroundImage = GetComponent<Image>();

            if (closeButton == null)
                closeButton = GetComponentInChildren<Button>();

            // Store original position
            originalPosition = rectTransform.anchoredPosition;
        }

        private void SetupEventHandlers()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(DismissNotification);
            }
        }

        #endregion

        #region Notification Management

        /// <summary>
        /// Initialize the notification popup with data.
        /// </summary>
        /// <param name="notificationData">Notification data to display</param>
        /// <param name="type">Type of notification for styling</param>
        public void Initialize(TaskbarNotification notificationData, NotificationType type = NotificationType.Info)
        {
            notification = notificationData;
            notificationType = type;

            UpdateNotificationDisplay();
            SetupNotificationStyling();

            // Set auto-dismiss timer
            timeRemaining = notification.Duration > 0 ? notification.Duration : displayDuration;
            autoDismiss = timeRemaining > 0;

            // Start slide-in animation
            SlideIn();

            Debug.Log($"[TaskbarNotificationPopup] Initialized notification: {notification.Title}");
        }

        private void UpdateNotificationDisplay()
        {
            // Update title
            if (titleText != null)
            {
                titleText.text = notification.Title ?? "Notification";
            }

            // Update message
            if (messageText != null)
            {
                messageText.text = notification.Message ?? "";
            }

            // Update icon
            if (iconImage != null)
            {
                if (notification.Icon != null)
                {
                    iconImage.sprite = notification.Icon;
                    iconImage.gameObject.SetActive(true);
                }
                else
                {
                    iconImage.gameObject.SetActive(false);
                }
            }
        }

        private void SetupNotificationStyling()
        {
            if (backgroundImage == null) return;

            // Set background color based on notification type
            Color backgroundColor;
            switch (notificationType)
            {
                case NotificationType.Warning:
                    backgroundColor = warningBackgroundColor;
                    break;
                case NotificationType.Error:
                    backgroundColor = errorBackgroundColor;
                    break;
                default:
                    backgroundColor = normalBackgroundColor;
                    break;
            }

            backgroundImage.color = backgroundColor;
        }

        #endregion

        #region Animation

        public void SlideIn()
        {
            if (isAnimating) return;

            StopCurrentAnimation();
            currentAnimation = StartCoroutine(SlideInAnimation());
        }

        public void SlideOut()
        {
            if (isAnimating) return;

            StopCurrentAnimation();
            currentAnimation = StartCoroutine(SlideOutAnimation());
        }

        private IEnumerator SlideInAnimation()
        {
            isAnimating = true;

            // Set initial position (off-screen)
            var startPosition = originalPosition + slideOffset;
            var endPosition = originalPosition;

            rectTransform.anchoredPosition = startPosition;
            canvasGroup.alpha = 0f;

            var elapsedTime = 0f;

            while (elapsedTime < slideInDuration)
            {
                elapsedTime += Time.deltaTime;
                var progress = elapsedTime / slideInDuration;
                var curveValue = slideInCurve.Evaluate(progress);

                rectTransform.anchoredPosition = Vector2.Lerp(startPosition, endPosition, curveValue);
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, progress);

                yield return null;
            }

            // Ensure final values
            rectTransform.anchoredPosition = endPosition;
            canvasGroup.alpha = 1f;

            isAnimating = false;
            currentAnimation = null;

            Debug.Log($"[TaskbarNotificationPopup] Slide-in animation complete: {notification.Title}");
        }

        private IEnumerator SlideOutAnimation()
        {
            isAnimating = true;

            var startPosition = rectTransform.anchoredPosition;
            var endPosition = startPosition + slideOffset;

            var elapsedTime = 0f;

            while (elapsedTime < slideOutDuration)
            {
                elapsedTime += Time.deltaTime;
                var progress = elapsedTime / slideOutDuration;
                var curveValue = slideOutCurve.Evaluate(progress);

                rectTransform.anchoredPosition = Vector2.Lerp(startPosition, endPosition, curveValue);
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, progress);

                yield return null;
            }

            // Ensure final values
            rectTransform.anchoredPosition = endPosition;
            canvasGroup.alpha = 0f;

            isAnimating = false;
            currentAnimation = null;

            // Destroy the notification after slide-out
            DestroyNotification();

            Debug.Log($"[TaskbarNotificationPopup] Slide-out animation complete: {notification.Title}");
        }

        private void StopCurrentAnimation()
        {
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
                currentAnimation = null;
                isAnimating = false;
            }
        }

        private void StopAllAnimations()
        {
            StopCurrentAnimation();
        }

        #endregion

        #region Auto-Dismiss

        private void UpdateAutoDismiss()
        {
            if (!autoDismiss || timeRemaining <= 0) return;

            timeRemaining -= Time.deltaTime;

            if (timeRemaining <= 0)
            {
                DismissNotification();
            }
        }

        public void DismissNotification()
        {
            autoDismiss = false;
            SlideOut();

            Debug.Log($"[TaskbarNotificationPopup] Dismissed notification: {notification.Title}");
        }

        private void DestroyNotification()
        {
            Destroy(gameObject);
        }

        #endregion

        #region Interaction

        /// <summary>
        /// Extend the display time of the notification.
        /// </summary>
        /// <param name="extraTime">Additional time to display</param>
        public void ExtendDisplayTime(float extraTime)
        {
            timeRemaining += extraTime;
            Debug.Log($"[TaskbarNotificationPopup] Extended display time by {extraTime}s");
        }

        /// <summary>
        /// Set the notification to not auto-dismiss.
        /// </summary>
        public void MakePersistent()
        {
            autoDismiss = false;
            timeRemaining = 0;
            Debug.Log($"[TaskbarNotificationPopup] Made notification persistent: {notification.Title}");
        }

        /// <summary>
        /// Update the notification content.
        /// </summary>
        /// <param name="newTitle">New title</param>
        /// <param name="newMessage">New message</param>
        public void UpdateContent(string newTitle, string newMessage)
        {
            notification.Title = newTitle;
            notification.Message = newMessage;

            UpdateNotificationDisplay();

            Debug.Log($"[TaskbarNotificationPopup] Updated notification content: {newTitle}");
        }

        #endregion

        #region Theme Support

        /// <summary>
        /// Apply UI theme to the notification popup.
        /// </summary>
        /// <param name="theme">Theme to apply</param>
        public void ApplyTheme(UITheme theme)
        {
            if (theme == null) return;

            // Update text colors and fonts
            if (titleText != null)
            {
                titleText.color = Color.white; // Override for notification visibility
                if (theme.HeadingFont != null)
                    titleText.font = theme.HeadingFont;
            }

            if (messageText != null)
            {
                messageText.color = Color.white; // Override for notification visibility
                if (theme.Font != null)
                    messageText.font = theme.Font;
            }

            // Update close button
            if (closeButton != null)
            {
                var colors = closeButton.colors;
                colors.normalColor = theme.GetColorWithAlpha(Color.white, 0.8f);
                colors.highlightedColor = theme.GetColorWithAlpha(Color.white, 1f);
                colors.pressedColor = theme.GetColorWithAlpha(Color.white, 0.6f);
                closeButton.colors = colors;
            }

            // Adjust background colors to theme
            normalBackgroundColor = theme.GetColorWithAlpha(theme.DarkGray, 0.9f);
            warningBackgroundColor = theme.GetColorWithAlpha(Color.yellow, 0.9f);
            errorBackgroundColor = theme.GetColorWithAlpha(Color.red, 0.9f);

            SetupNotificationStyling();

            Debug.Log($"[TaskbarNotificationPopup] Applied theme to notification: {notification.Title}");
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get notification popup information.
        /// </summary>
        /// <returns>Notification popup info</returns>
        public NotificationPopupInfo GetInfo()
        {
            return new NotificationPopupInfo
            {
                Title = notification.Title,
                Message = notification.Message,
                Type = notificationType,
                TimeRemaining = timeRemaining,
                IsAnimating = isAnimating,
                IsPersistent = !autoDismiss
            };
        }

        #endregion

#if UNITY_EDITOR
        [ContextMenu("Test Dismiss")]
        private void TestDismiss()
        {
            DismissNotification();
        }

        [ContextMenu("Make Persistent")]
        private void TestMakePersistent()
        {
            MakePersistent();
        }

        [ContextMenu("Extend Time")]
        private void TestExtendTime()
        {
            ExtendDisplayTime(5f);
        }

        [ContextMenu("Debug Notification Info")]
        private void DebugNotificationInfo()
        {
            var info = GetInfo();
            Debug.Log($"Notification Info:\n" +
                     $"  Title: {info.Title}\n" +
                     $"  Type: {info.Type}\n" +
                     $"  Time Remaining: {info.TimeRemaining:F1}s\n" +
                     $"  Animating: {info.IsAnimating}\n" +
                     $"  Persistent: {info.IsPersistent}");
        }
#endif
    }

    /// <summary>
    /// Notification type enumeration for styling.
    /// </summary>
    public enum NotificationType
    {
        Info,
        Warning,
        Error,
        Success
    }

    /// <summary>
    /// Notification popup information data structure.
    /// </summary>
    [System.Serializable]
    public struct NotificationPopupInfo
    {
        public string Title;
        public string Message;
        public NotificationType Type;
        public float TimeRemaining;
        public bool IsAnimating;
        public bool IsPersistent;
    }
}