using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using Coalition.Runtime.Core;

namespace Coalition.Runtime.UI
{
    /// <summary>
    /// Desktop-style window with drag, resize, minimize, maximize, and close functionality.
    /// Integrates with WindowManager for Z-order and focus management.
    /// </summary>
    public class Window : UIPanel, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerDownHandler
    {
        [Header("Window Configuration")]
        [SerializeField] private string windowTitle = "Window";
        [SerializeField] private bool isResizable = true;
        [SerializeField] private bool isMovable = true;
        [SerializeField] private bool canMinimize = true;
        [SerializeField] private bool canMaximize = true;
        [SerializeField] private bool canClose = true;

        [Header("Window Constraints")]
        [SerializeField] private Vector2 minSize = new Vector2(300, 200);
        [SerializeField] private Vector2 maxSize = new Vector2(1600, 1200);
        [SerializeField] private RectTransform constraintArea; // If null, uses screen bounds

        [Header("Window Components")]
        [SerializeField] private RectTransform titleBar;
        [SerializeField] private Text titleText;
        [SerializeField] private Button minimizeButton;
        [SerializeField] private Button maximizeButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private RectTransform contentArea;
        [SerializeField] private RectTransform resizeHandle;

        [Header("Window Styling")]
        [SerializeField] private Image windowBorder;
        [SerializeField] private Image titleBarBackground;
        [SerializeField] private float borderWidth = 2f;
        [SerializeField] private Color focusedBorderColor = new Color(0.08f, 0.26f, 0.45f, 1f);
        [SerializeField] private Color unfocusedBorderColor = new Color(0.6f, 0.6f, 0.6f, 1f);

        // Window state
        private WindowManager windowManager;
        private bool isDragging = false;
        private bool isResizing = false;
        private bool isMaximized = false;
        private bool isMinimized = false;
        private bool isFocused = false;

        private Vector2 dragOffset;
        private Vector2 lastMousePosition;
        private Vector2 preMaximizeSize;
        private Vector2 preMaximizePosition;

        // Window data
        private int windowId;
        private float lastInteractionTime;

        // Events
        public System.Action<Window> OnWindowFocused;
        public System.Action<Window> OnWindowClosed;
        public System.Action<Window> OnWindowMinimized;
        public System.Action<Window> OnWindowMaximized;
        public System.Action<Window> OnWindowRestored;

        #region Properties

        public string WindowTitle
        {
            get => windowTitle;
            set
            {
                windowTitle = value;
                if (titleText != null) titleText.text = value;
            }
        }

        public int WindowId
        {
            get => windowId;
            set => windowId = value;
        }

        public bool IsFocused
        {
            get => isFocused;
            set => SetFocused(value);
        }

        public bool IsMaximized => isMaximized;
        public bool IsMinimized => isMinimized;
        public bool IsResizable => isResizable;
        public bool IsMovable => isMovable;
        public RectTransform ContentArea => contentArea;
        public float LastInteractionTime => lastInteractionTime;

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            InitializeWindowComponents();
        }

        protected override void Start()
        {
            base.Start();
            SetupEventHandlers();
            UpdateTitleText();
            SetFocused(true); // New windows start focused
        }

        protected override void OnDestroy()
        {
            CleanupEventHandlers();
            base.OnDestroy();
        }

        #endregion

        #region Initialization

        private void InitializeWindowComponents()
        {
            // Find components if not assigned
            if (titleBar == null)
                titleBar = transform.Find("TitleBar")?.GetComponent<RectTransform>();

            if (titleText == null && titleBar != null)
                titleText = titleBar.GetComponentInChildren<Text>();

            if (contentArea == null)
                contentArea = transform.Find("ContentArea")?.GetComponent<RectTransform>();

            if (resizeHandle == null)
                resizeHandle = transform.Find("ResizeHandle")?.GetComponent<RectTransform>();

            // Find control buttons
            if (titleBar != null)
            {
                var buttons = titleBar.GetComponentsInChildren<Button>();
                foreach (var button in buttons)
                {
                    switch (button.name.ToLower())
                    {
                        case "minimizebutton":
                            minimizeButton = button;
                            break;
                        case "maximizebutton":
                            maximizeButton = button;
                            break;
                        case "closebutton":
                            closeButton = button;
                            break;
                    }
                }
            }

            // Generate unique window ID
            windowId = GetInstanceID();
        }

        private void SetupEventHandlers()
        {
            if (minimizeButton != null)
                minimizeButton.onClick.AddListener(MinimizeWindow);

            if (maximizeButton != null)
                maximizeButton.onClick.AddListener(ToggleMaximize);

            if (closeButton != null)
                closeButton.onClick.AddListener(CloseWindow);

            // Setup resize handle
            if (resizeHandle != null && isResizable)
            {
                var resizeEvents = resizeHandle.gameObject.AddComponent<ResizeEventHandler>();
                resizeEvents.OnResizeBegin += OnResizeBegin;
                resizeEvents.OnResize += OnResize;
                resizeEvents.OnResizeEnd += OnResizeEnd;
            }
        }

        private void CleanupEventHandlers()
        {
            if (minimizeButton != null)
                minimizeButton.onClick.RemoveListener(MinimizeWindow);

            if (maximizeButton != null)
                maximizeButton.onClick.RemoveListener(ToggleMaximize);

            if (closeButton != null)
                closeButton.onClick.RemoveListener(CloseWindow);
        }

        #endregion

        #region Window Control

        public void SetWindowManager(WindowManager manager)
        {
            windowManager = manager;
        }

        public void MinimizeWindow()
        {
            if (!canMinimize || isMinimized) return;

            isMinimized = true;
            SetVisibility(false, false);
            OnWindowMinimized?.Invoke(this);

            // Notify window manager
            windowManager?.OnWindowMinimized(this);

            lastInteractionTime = Time.time;
            Debug.Log($"[Window] Minimized window: {windowTitle}");
        }

        public void RestoreWindow()
        {
            if (!isMinimized) return;

            isMinimized = false;
            SetVisibility(true, false);
            SetFocused(true);
            OnWindowRestored?.Invoke(this);

            // Notify window manager
            windowManager?.OnWindowRestored(this);

            lastInteractionTime = Time.time;
            Debug.Log($"[Window] Restored window: {windowTitle}");
        }

        public void MaximizeWindow()
        {
            if (!canMaximize || isMaximized) return;

            // Store current size/position
            preMaximizeSize = rectTransform.sizeDelta;
            preMaximizePosition = rectTransform.anchoredPosition;

            // Get constraint area or use screen bounds
            var targetArea = constraintArea ?? (RectTransform)transform.parent;
            var targetSize = targetArea.rect.size;
            var targetPosition = Vector2.zero;

            // Account for margins/padding
            targetSize -= Vector2.one * borderWidth * 2;

            // Apply maximized state
            rectTransform.sizeDelta = targetSize;
            rectTransform.anchoredPosition = targetPosition;

            isMaximized = true;
            OnWindowMaximized?.Invoke(this);
            lastInteractionTime = Time.time;

            Debug.Log($"[Window] Maximized window: {windowTitle}");
        }

        public void RestoreFromMaximized()
        {
            if (!isMaximized) return;

            // Restore previous size/position
            rectTransform.sizeDelta = preMaximizeSize;
            rectTransform.anchoredPosition = preMaximizePosition;

            isMaximized = false;
            OnWindowRestored?.Invoke(this);
            lastInteractionTime = Time.time;

            Debug.Log($"[Window] Restored from maximized: {windowTitle}");
        }

        public void ToggleMaximize()
        {
            if (isMaximized)
                RestoreFromMaximized();
            else
                MaximizeWindow();
        }

        public void CloseWindow()
        {
            if (!canClose) return;

            OnWindowClosed?.Invoke(this);

            // Notify window manager
            windowManager?.OnWindowClosed(this);

            // Close with animation
            Close(true);

            Debug.Log($"[Window] Closed window: {windowTitle}");
        }

        public void FocusWindow()
        {
            SetFocused(true);
            BringToFront();

            // Notify window manager
            windowManager?.OnWindowFocused(this);

            lastInteractionTime = Time.time;
        }

        private void SetFocused(bool focused)
        {
            if (isFocused == focused) return;

            isFocused = focused;
            UpdateVisualFocusState();

            if (focused)
                OnWindowFocused?.Invoke(this);
        }

        private void UpdateVisualFocusState()
        {
            if (windowBorder != null)
            {
                windowBorder.color = isFocused ? focusedBorderColor : unfocusedBorderColor;
            }

            if (titleBarBackground != null)
            {
                var color = titleBarBackground.color;
                color.a = isFocused ? 1.0f : 0.8f;
                titleBarBackground.color = color;
            }
        }

        #endregion

        #region Drag Handling

        public void OnPointerDown(PointerEventData eventData)
        {
            FocusWindow();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!isMovable || isMaximized) return;

            // Only allow dragging from title bar
            if (titleBar == null || !RectTransformUtility.RectangleContainsScreenPoint(titleBar, eventData.position, eventData.pressEventCamera))
                return;

            isDragging = true;
            dragOffset = (Vector2)transform.position - eventData.position;
            lastInteractionTime = Time.time;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging || !isMovable || isMaximized) return;

            Vector2 newPosition = eventData.position + dragOffset;

            // Apply position constraints
            newPosition = ConstrainPosition(newPosition);

            transform.position = newPosition;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            isDragging = false;
        }

        private Vector2 ConstrainPosition(Vector2 position)
        {
            if (constraintArea == null) return position;

            var windowRect = rectTransform.rect;
            var constraintRect = constraintArea.rect;

            // Convert to local space
            var localPos = constraintArea.InverseTransformPoint(position);

            // Apply constraints
            localPos.x = Mathf.Clamp(localPos.x,
                constraintRect.xMin + windowRect.width / 2,
                constraintRect.xMax - windowRect.width / 2);

            localPos.y = Mathf.Clamp(localPos.y,
                constraintRect.yMin + windowRect.height / 2,
                constraintRect.yMax - windowRect.height / 2);

            return constraintArea.TransformPoint(localPos);
        }

        #endregion

        #region Resize Handling

        private void OnResizeBegin(Vector2 startPosition)
        {
            if (!isResizable || isMaximized) return;

            isResizing = true;
            lastMousePosition = startPosition;
            lastInteractionTime = Time.time;
        }

        private void OnResize(Vector2 currentPosition)
        {
            if (!isResizing || !isResizable || isMaximized) return;

            Vector2 sizeDelta = currentPosition - lastMousePosition;
            Vector2 newSize = rectTransform.sizeDelta + sizeDelta;

            // Apply size constraints
            newSize.x = Mathf.Clamp(newSize.x, minSize.x, maxSize.x);
            newSize.y = Mathf.Clamp(newSize.y, minSize.y, maxSize.y);

            rectTransform.sizeDelta = newSize;
            lastMousePosition = currentPosition;
        }

        private void OnResizeEnd()
        {
            isResizing = false;
        }

        #endregion

        #region Theme Support

        public override void ApplyTheme(UITheme theme)
        {
            base.ApplyTheme(theme);

            if (theme == null) return;

            // Apply window-specific theming
            if (windowBorder != null)
            {
                focusedBorderColor = theme.RijksBlauw;
                unfocusedBorderColor = theme.WindowBorderColor;
                windowBorder.color = isFocused ? focusedBorderColor : unfocusedBorderColor;
            }

            if (titleBarBackground != null)
            {
                titleBarBackground.color = theme.WindowTitleBarColor;
            }

            if (titleText != null)
            {
                titleText.color = theme.WindowTitleTextColor;
                if (theme.HeadingFont != null)
                    titleText.font = theme.HeadingFont;
            }

            // Update control button colors
            UpdateButtonColors(theme);
        }

        private void UpdateButtonColors(UITheme theme)
        {
            var buttons = new[] { minimizeButton, maximizeButton, closeButton };

            foreach (var button in buttons)
            {
                if (button == null) continue;

                var colors = button.colors;
                colors.normalColor = theme.ButtonNormalColor;
                colors.highlightedColor = theme.ButtonHighlightColor;
                colors.pressedColor = theme.ButtonPressedColor;
                colors.disabledColor = theme.ButtonDisabledColor;
                button.colors = colors;
            }
        }

        #endregion

        #region Utility Methods

        private void UpdateTitleText()
        {
            if (titleText != null)
                titleText.text = windowTitle;
        }

        public void SetSize(Vector2 size)
        {
            size.x = Mathf.Clamp(size.x, minSize.x, maxSize.x);
            size.y = Mathf.Clamp(size.y, minSize.y, maxSize.y);
            rectTransform.sizeDelta = size;
        }

        public void SetPosition(Vector2 position)
        {
            position = ConstrainPosition(position);
            rectTransform.anchoredPosition = position;
        }

        public Vector2 GetSize()
        {
            return rectTransform.sizeDelta;
        }

        public Vector2 GetPosition()
        {
            return rectTransform.anchoredPosition;
        }

        #endregion

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            // Ensure valid constraints
            minSize.x = Mathf.Max(100, minSize.x);
            minSize.y = Mathf.Max(50, minSize.y);

            if (maxSize.x < minSize.x) maxSize.x = minSize.x;
            if (maxSize.y < minSize.y) maxSize.y = minSize.y;

            borderWidth = Mathf.Max(0, borderWidth);

            UpdateTitleText();
        }

        [ContextMenu("Test Focus")]
        private void TestFocus()
        {
            FocusWindow();
        }

        [ContextMenu("Test Minimize")]
        private void TestMinimize()
        {
            MinimizeWindow();
        }

        [ContextMenu("Test Maximize")]
        private void TestMaximize()
        {
            ToggleMaximize();
        }
#endif
    }

    /// <summary>
    /// Helper component for handling resize events on the resize handle.
    /// </summary>
    public class ResizeEventHandler : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
    {
        public System.Action<Vector2> OnResizeBegin;
        public System.Action<Vector2> OnResize;
        public System.Action OnResizeEnd;

        public void OnBeginDrag(PointerEventData eventData)
        {
            OnResizeBegin?.Invoke(eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            OnResize?.Invoke(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            OnResizeEnd?.Invoke();
        }
    }
}