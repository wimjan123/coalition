using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Coalition.Runtime.Core;

namespace Coalition.Runtime.UI
{
    /// <summary>
    /// Desktop taskbar with window buttons, system tray, and application launcher.
    /// Provides window switching and system status display.
    /// </summary>
    public class DesktopTaskbar : MonoBehaviour
    {
        [Header("Taskbar Configuration")]
        [SerializeField] private RectTransform windowButtonContainer;
        [SerializeField] private RectTransform systemTrayContainer;
        [SerializeField] private Button startMenuButton;
        [SerializeField] private Text clockText;
        [SerializeField] private Image taskbarBackground;

        [Header("Window Button Settings")]
        [SerializeField] private GameObject windowButtonPrefab;
        [SerializeField] private float maxButtonWidth = 200f;
        [SerializeField] private float minButtonWidth = 120f;
        [SerializeField] private bool showWindowPreviews = true;

        [Header("System Tray")]
        [SerializeField] private Text systemStatusText;
        [SerializeField] private GameObject notificationPrefab;
        [SerializeField] private int maxNotifications = 5;

        [Header("Taskbar Behavior")]
        [SerializeField] private bool autoHide = false;
        [SerializeField] private float autoHideDelay = 3f;
        [SerializeField] private float taskbarHeight = 40f;

        // Window tracking
        private Dictionary<int, TaskbarWindowButton> windowButtons;
        private List<Window> trackedWindows;
        private Window activeWindow;

        // System tray
        private Queue<TaskbarNotification> notifications;
        private List<GameObject> notificationObjects;

        // Auto-hide functionality
        private bool isHidden = false;
        private float lastInteractionTime;
        private Vector2 originalPosition;

        // Clock and status
        private string lastTimeString;
        private float nextClockUpdate;

        #region Properties

        public bool IsAutoHideEnabled
        {
            get => autoHide;
            set => autoHide = value;
        }

        public int WindowCount => trackedWindows.Count;
        public Window ActiveWindow => activeWindow;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeTaskbar();
        }

        private void Start()
        {
            SetupTaskbarLayout();
            InitializeEventSubscriptions();
            UpdateClock();
        }

        private void Update()
        {
            UpdateClock();
            HandleAutoHide();
            ProcessNotifications();
        }

        private void OnDestroy()
        {
            CleanupEventSubscriptions();
        }

        #endregion

        #region Initialization

        private void InitializeTaskbar()
        {
            windowButtons = new Dictionary<int, TaskbarWindowButton>();
            trackedWindows = new List<Window>();
            notifications = new Queue<TaskbarNotification>();
            notificationObjects = new List<GameObject>();

            // Store original position for auto-hide
            originalPosition = transform.position;

            Debug.Log("[DesktopTaskbar] Taskbar initialized");
        }

        private void SetupTaskbarLayout()
        {
            // Ensure taskbar is at bottom of screen
            var rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchorMin = new Vector2(0, 0);
                rectTransform.anchorMax = new Vector2(1, 0);
                rectTransform.pivot = new Vector2(0.5f, 0);
                rectTransform.sizeDelta = new Vector2(0, taskbarHeight);
                rectTransform.anchoredPosition = Vector2.zero;
            }

            // Setup start menu button
            if (startMenuButton != null)
            {
                startMenuButton.onClick.AddListener(OnStartMenuClicked);
            }
        }

        private void InitializeEventSubscriptions()
        {
            EventBus.Subscribe<DemoPhaseChangedEvent>(OnDemoPhaseChanged);
        }

        private void CleanupEventSubscriptions()
        {
            EventBus.Unsubscribe<DemoPhaseChangedEvent>(OnDemoPhaseChanged);
        }

        #endregion

        #region Window Management

        /// <summary>
        /// Add a window to the taskbar.
        /// </summary>
        /// <param name="window">Window to add</param>
        public void AddWindow(Window window)
        {
            if (window == null || trackedWindows.Contains(window)) return;

            trackedWindows.Add(window);
            CreateWindowButton(window);

            UpdateWindowButtonLayout();
            lastInteractionTime = Time.time;

            Debug.Log($"[DesktopTaskbar] Added window to taskbar: {window.WindowTitle}");
        }

        /// <summary>
        /// Remove a window from the taskbar.
        /// </summary>
        /// <param name="window">Window to remove</param>
        public void RemoveWindow(Window window)
        {
            if (window == null || !trackedWindows.Contains(window)) return;

            trackedWindows.Remove(window);

            if (windowButtons.ContainsKey(window.WindowId))
            {
                var button = windowButtons[window.WindowId];
                windowButtons.Remove(window.WindowId);

                if (button != null && button.gameObject != null)
                {
                    Destroy(button.gameObject);
                }
            }

            if (activeWindow == window)
            {
                activeWindow = null;
            }

            UpdateWindowButtonLayout();
            lastInteractionTime = Time.time;

            Debug.Log($"[DesktopTaskbar] Removed window from taskbar: {window.WindowTitle}");
        }

        /// <summary>
        /// Set the active window on the taskbar.
        /// </summary>
        /// <param name="window">Window to set as active</param>
        public void SetActiveWindow(Window window)
        {
            if (activeWindow == window) return;

            // Update previous active window button
            if (activeWindow != null && windowButtons.ContainsKey(activeWindow.WindowId))
            {
                windowButtons[activeWindow.WindowId].SetActive(false);
            }

            activeWindow = window;

            // Update new active window button
            if (activeWindow != null && windowButtons.ContainsKey(activeWindow.WindowId))
            {
                windowButtons[activeWindow.WindowId].SetActive(true);
            }

            lastInteractionTime = Time.time;
        }

        private void CreateWindowButton(Window window)
        {
            if (windowButtonPrefab == null || windowButtonContainer == null) return;

            var buttonObject = Instantiate(windowButtonPrefab, windowButtonContainer);
            var windowButton = buttonObject.GetComponent<TaskbarWindowButton>();

            if (windowButton == null)
            {
                windowButton = buttonObject.AddComponent<TaskbarWindowButton>();
            }

            windowButton.Initialize(window, this);
            windowButtons[window.WindowId] = windowButton;
        }

        private void UpdateWindowButtonLayout()
        {
            if (windowButtonContainer == null || windowButtons.Count == 0) return;

            var containerWidth = windowButtonContainer.rect.width;
            var buttonCount = windowButtons.Count;
            var buttonWidth = Mathf.Clamp(containerWidth / buttonCount, minButtonWidth, maxButtonWidth);

            var index = 0;
            foreach (var button in windowButtons.Values)
            {
                if (button == null) continue;

                var rectTransform = button.GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(buttonWidth, taskbarHeight - 4);

                // Position button
                var layoutElement = button.GetComponent<LayoutElement>();
                if (layoutElement == null)
                {
                    layoutElement = button.gameObject.AddComponent<LayoutElement>();
                }
                layoutElement.preferredWidth = buttonWidth;

                index++;
            }
        }

        #endregion

        #region System Tray

        /// <summary>
        /// Add a notification to the system tray.
        /// </summary>
        /// <param name="title">Notification title</param>
        /// <param name="message">Notification message</param>
        /// <param name="icon">Optional notification icon</param>
        public void AddNotification(string title, string message, Sprite icon = null)
        {
            var notification = new TaskbarNotification
            {
                Title = title,
                Message = message,
                Icon = icon,
                Timestamp = System.DateTime.Now,
                Duration = 5f
            };

            notifications.Enqueue(notification);

            // Remove oldest notification if at limit
            if (notifications.Count > maxNotifications)
            {
                notifications.Dequeue();
            }

            ShowNotificationPopup(notification);
            lastInteractionTime = Time.time;

            Debug.Log($"[DesktopTaskbar] Added notification: {title}");
        }

        /// <summary>
        /// Update system status text.
        /// </summary>
        /// <param name="status">Status message</param>
        public void SetSystemStatus(string status)
        {
            if (systemStatusText != null)
            {
                systemStatusText.text = status;
            }
        }

        private void ShowNotificationPopup(TaskbarNotification notification)
        {
            if (notificationPrefab == null || systemTrayContainer == null) return;

            var notificationObject = Instantiate(notificationPrefab, systemTrayContainer);
            var notificationComponent = notificationObject.GetComponent<TaskbarNotificationPopup>();

            if (notificationComponent != null)
            {
                notificationComponent.Initialize(notification);
                notificationObjects.Add(notificationObject);
            }
        }

        private void ProcessNotifications()
        {
            // Clean up expired notification objects
            for (int i = notificationObjects.Count - 1; i >= 0; i--)
            {
                if (notificationObjects[i] == null)
                {
                    notificationObjects.RemoveAt(i);
                }
            }
        }

        #endregion

        #region Clock and Status

        private void UpdateClock()
        {
            if (Time.time < nextClockUpdate) return;

            nextClockUpdate = Time.time + 1f; // Update every second

            var timeString = System.DateTime.Now.ToString("HH:mm");
            if (timeString != lastTimeString)
            {
                lastTimeString = timeString;
                if (clockText != null)
                {
                    clockText.text = timeString;
                }
            }
        }

        #endregion

        #region Auto-Hide Functionality

        private void HandleAutoHide()
        {
            if (!autoHide) return;

            var timeSinceInteraction = Time.time - lastInteractionTime;
            var shouldHide = timeSinceInteraction > autoHideDelay;

            if (shouldHide && !isHidden)
            {
                HideTaskbar();
            }
            else if (!shouldHide && isHidden)
            {
                ShowTaskbar();
            }
        }

        private void HideTaskbar()
        {
            if (isHidden) return;

            isHidden = true;
            var targetPosition = originalPosition - Vector2.up * (taskbarHeight - 5);

            StartCoroutine(AnimateToPosition(targetPosition));
            Debug.Log("[DesktopTaskbar] Hiding taskbar");
        }

        private void ShowTaskbar()
        {
            if (!isHidden) return;

            isHidden = false;
            StartCoroutine(AnimateToPosition(originalPosition));
            Debug.Log("[DesktopTaskbar] Showing taskbar");
        }

        private System.Collections.IEnumerator AnimateToPosition(Vector2 targetPosition)
        {
            var startPosition = transform.position;
            var duration = 0.3f;
            var elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                var progress = elapsedTime / duration;
                var easedProgress = Mathf.SmoothStep(0, 1, progress);

                transform.position = Vector2.Lerp(startPosition, targetPosition, easedProgress);
                yield return null;
            }

            transform.position = targetPosition;
        }

        #endregion

        #region Event Handlers

        private void OnStartMenuClicked()
        {
            Debug.Log("[DesktopTaskbar] Start menu clicked");
            lastInteractionTime = Time.time;

            // Publish start menu event
            EventBus.Publish(new TaskbarStartMenuClickedEvent());
        }

        public void OnWindowButtonClicked(Window window)
        {
            if (window == null) return;

            if (window.IsMinimized)
            {
                window.RestoreWindow();
            }
            else if (window == activeWindow)
            {
                window.MinimizeWindow();
            }
            else
            {
                // Focus the window through the window manager
                var windowManager = FindObjectOfType<WindowManager>();
                if (windowManager != null)
                {
                    windowManager.FocusWindow(window);
                }
            }

            lastInteractionTime = Time.time;
        }

        private void OnDemoPhaseChanged(DemoPhaseChangedEvent eventData)
        {
            AddNotification("Demo Phase", $"Switched to: {eventData.NewPhase}", null);
        }

        #endregion

        #region Theme Support

        /// <summary>
        /// Apply UI theme to the taskbar.
        /// </summary>
        /// <param name="theme">Theme to apply</param>
        public void ApplyTheme(UITheme theme)
        {
            if (theme == null) return;

            // Apply taskbar background
            if (taskbarBackground != null)
            {
                taskbarBackground.color = theme.WindowTitleBarColor;
            }

            // Apply theme to clock
            if (clockText != null)
            {
                clockText.color = theme.WindowTitleTextColor;
                if (theme.Font != null)
                    clockText.font = theme.Font;
            }

            // Apply theme to system status
            if (systemStatusText != null)
            {
                systemStatusText.color = theme.TextColor;
                if (theme.Font != null)
                    systemStatusText.font = theme.Font;
            }

            // Apply theme to start menu button
            if (startMenuButton != null)
            {
                var colors = startMenuButton.colors;
                colors.normalColor = theme.ButtonNormalColor;
                colors.highlightedColor = theme.ButtonHighlightColor;
                colors.pressedColor = theme.ButtonPressedColor;
                colors.disabledColor = theme.ButtonDisabledColor;
                startMenuButton.colors = colors;
            }

            // Apply theme to window buttons
            foreach (var button in windowButtons.Values)
            {
                if (button != null)
                {
                    button.ApplyTheme(theme);
                }
            }

            Debug.Log("[DesktopTaskbar] Applied theme to taskbar");
        }

        #endregion

        #region Public API

        /// <summary>
        /// Force show the taskbar (override auto-hide).
        /// </summary>
        public void ForceShow()
        {
            lastInteractionTime = Time.time;
            ShowTaskbar();
        }

        /// <summary>
        /// Get taskbar statistics.
        /// </summary>
        /// <returns>Taskbar statistics</returns>
        public TaskbarStats GetStats()
        {
            return new TaskbarStats
            {
                WindowCount = trackedWindows.Count,
                NotificationCount = notifications.Count,
                IsHidden = isHidden,
                ActiveWindowTitle = activeWindow?.WindowTitle ?? "None"
            };
        }

        #endregion

#if UNITY_EDITOR
        [ContextMenu("Debug Taskbar Status")]
        private void DebugTaskbarStatus()
        {
            var stats = GetStats();
            Debug.Log($"Taskbar Status:\n" +
                     $"  Window Count: {stats.WindowCount}\n" +
                     $"  Notifications: {stats.NotificationCount}\n" +
                     $"  Hidden: {stats.IsHidden}\n" +
                     $"  Active Window: {stats.ActiveWindowTitle}");
        }

        [ContextMenu("Test Notification")]
        private void TestNotification()
        {
            AddNotification("Test", "This is a test notification", null);
        }

        [ContextMenu("Toggle Auto-Hide")]
        private void ToggleAutoHide()
        {
            autoHide = !autoHide;
            Debug.Log($"Auto-hide: {autoHide}");
        }
#endif
    }

    /// <summary>
    /// Taskbar statistics data structure.
    /// </summary>
    [System.Serializable]
    public struct TaskbarStats
    {
        public int WindowCount;
        public int NotificationCount;
        public bool IsHidden;
        public string ActiveWindowTitle;
    }

    /// <summary>
    /// Taskbar notification data structure.
    /// </summary>
    [System.Serializable]
    public struct TaskbarNotification
    {
        public string Title;
        public string Message;
        public Sprite Icon;
        public System.DateTime Timestamp;
        public float Duration;
    }

    /// <summary>
    /// Event published when start menu is clicked.
    /// </summary>
    public class TaskbarStartMenuClickedEvent
    {
        public System.DateTime Timestamp { get; } = System.DateTime.Now;
    }
}