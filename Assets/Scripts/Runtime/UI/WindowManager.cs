using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Coalition.Runtime.Core;

namespace Coalition.Runtime.UI
{
    /// <summary>
    /// Manages desktop-style windows with Z-order, focus management, and taskbar integration.
    /// Handles window lifecycle and provides window discovery services.
    /// </summary>
    public class WindowManager : MonoBehaviour
    {
        [Header("Window Management")]
        [SerializeField] private RectTransform windowContainer;
        [SerializeField] private DesktopTaskbar taskbar;
        [SerializeField] private int maxWindows = 20;
        [SerializeField] private float windowCascadeOffset = 30f;

        [Header("Focus Management")]
        [SerializeField] private bool clickToFocus = true;
        [SerializeField] private bool autoFocusNewWindows = true;
        [SerializeField] private float focusAnimationDuration = 0.1f;

        [Header("Window Spawning")]
        [SerializeField] private Vector2 defaultWindowSize = new Vector2(800, 600);
        [SerializeField] private Vector2 defaultSpawnPosition = new Vector2(100, 100);
        [SerializeField] private bool cascadeNewWindows = true;

        // Window tracking
        private List<Window> managedWindows;
        private Dictionary<int, Window> windowLookup;
        private Stack<Vector2> cascadePositions;
        private Window focusedWindow;
        private int nextWindowId = 1;

        // Performance optimization
        private readonly Queue<System.Action> deferredActions = new Queue<System.Action>();
        private float lastCleanupTime;
        private const float CleanupInterval = 5f;

        // Events
        public System.Action<Window> OnWindowOpened;
        public System.Action<Window> OnWindowClosed;
        public System.Action<Window> OnWindowFocusChanged;
        public System.Action<List<Window>> OnWindowListChanged;

        #region Properties

        public int WindowCount => managedWindows.Count;
        public Window FocusedWindow => focusedWindow;
        public List<Window> AllWindows => new List<Window>(managedWindows);
        public DesktopTaskbar Taskbar => taskbar;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeWindowManager();
        }

        private void Start()
        {
            SetupWindowContainer();
            InitializeEventSubscriptions();
        }

        private void Update()
        {
            ProcessDeferredActions();
            PerformPeriodicCleanup();
            HandleKeyboardShortcuts();
        }

        private void OnDestroy()
        {
            CleanupEventSubscriptions();
        }

        #endregion

        #region Initialization

        private void InitializeWindowManager()
        {
            managedWindows = new List<Window>();
            windowLookup = new Dictionary<int, Window>();
            cascadePositions = new Stack<Vector2>();

            if (windowContainer == null)
            {
                windowContainer = GetComponent<RectTransform>();
                if (windowContainer == null)
                {
                    Debug.LogError("[WindowManager] No window container found");
                }
            }

            Debug.Log("[WindowManager] Window manager initialized");
        }

        private void SetupWindowContainer()
        {
            if (windowContainer == null) return;

            // Ensure window container fills the screen
            windowContainer.anchorMin = Vector2.zero;
            windowContainer.anchorMax = Vector2.one;
            windowContainer.offsetMin = Vector2.zero;
            windowContainer.offsetMax = Vector2.zero;
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
        /// Register a window with the window manager.
        /// </summary>
        /// <param name="window">Window to register</param>
        public void RegisterWindow(Window window)
        {
            if (window == null)
            {
                Debug.LogError("[WindowManager] Cannot register null window");
                return;
            }

            if (managedWindows.Count >= maxWindows)
            {
                Debug.LogWarning($"[WindowManager] Maximum window limit ({maxWindows}) reached");
                return;
            }

            // Assign window ID and manager reference
            window.WindowId = nextWindowId++;
            window.SetWindowManager(this);

            // Add to tracking collections
            managedWindows.Add(window);
            windowLookup[window.WindowId] = window;

            // Set window parent and initial position
            SetupNewWindow(window);

            // Subscribe to window events
            SubscribeToWindowEvents(window);

            // Update UI
            OnWindowOpened?.Invoke(window);
            OnWindowListChanged?.Invoke(managedWindows);

            // Update taskbar
            taskbar?.AddWindow(window);

            // Auto-focus if enabled
            if (autoFocusNewWindows)
            {
                FocusWindow(window);
            }

            Debug.Log($"[WindowManager] Registered window: {window.WindowTitle} (ID: {window.WindowId})");
        }

        /// <summary>
        /// Unregister a window from the window manager.
        /// </summary>
        /// <param name="window">Window to unregister</param>
        public void UnregisterWindow(Window window)
        {
            if (window == null || !managedWindows.Contains(window)) return;

            // Unsubscribe from window events
            UnsubscribeFromWindowEvents(window);

            // Remove from tracking collections
            managedWindows.Remove(window);
            windowLookup.Remove(window.WindowId);

            // Handle focus transfer
            if (focusedWindow == window)
            {
                FocusNextAvailableWindow();
            }

            // Update UI
            OnWindowClosed?.Invoke(window);
            OnWindowListChanged?.Invoke(managedWindows);

            // Update taskbar
            taskbar?.RemoveWindow(window);

            Debug.Log($"[WindowManager] Unregistered window: {window.WindowTitle} (ID: {window.WindowId})");
        }

        /// <summary>
        /// Create a new window of the specified type.
        /// </summary>
        /// <typeparam name="T">Window type</typeparam>
        /// <param name="windowPrefab">Window prefab to instantiate</param>
        /// <param name="title">Window title</param>
        /// <returns>Created window instance</returns>
        public T CreateWindow<T>(GameObject windowPrefab, string title = "") where T : Window
        {
            if (windowPrefab == null)
            {
                Debug.LogError("[WindowManager] Cannot create window from null prefab");
                return null;
            }

            var windowObject = Instantiate(windowPrefab, windowContainer);
            var window = windowObject.GetComponent<T>();

            if (window == null)
            {
                Debug.LogError($"[WindowManager] Window prefab does not contain component: {typeof(T).Name}");
                Destroy(windowObject);
                return null;
            }

            if (!string.IsNullOrEmpty(title))
            {
                window.WindowTitle = title;
            }

            RegisterWindow(window);
            return window;
        }

        /// <summary>
        /// Focus a specific window.
        /// </summary>
        /// <param name="window">Window to focus</param>
        public void FocusWindow(Window window)
        {
            if (window == null || !managedWindows.Contains(window)) return;

            // Unfocus current window
            if (focusedWindow != null && focusedWindow != window)
            {
                focusedWindow.IsFocused = false;
            }

            // Focus new window
            focusedWindow = window;
            window.IsFocused = true;

            // Bring to front
            BringWindowToFront(window);

            // Update taskbar
            taskbar?.SetActiveWindow(window);

            OnWindowFocusChanged?.Invoke(window);

            Debug.Log($"[WindowManager] Focused window: {window.WindowTitle}");
        }

        /// <summary>
        /// Bring a window to the front of the Z-order.
        /// </summary>
        /// <param name="window">Window to bring forward</param>
        public void BringWindowToFront(Window window)
        {
            if (window == null) return;

            window.transform.SetAsLastSibling();
        }

        /// <summary>
        /// Send a window to the back of the Z-order.
        /// </summary>
        /// <param name="window">Window to send back</param>
        public void SendWindowToBack(Window window)
        {
            if (window == null) return;

            window.transform.SetAsFirstSibling();
        }

        /// <summary>
        /// Close all windows.
        /// </summary>
        public void CloseAllWindows()
        {
            var windowsToClose = new List<Window>(managedWindows);
            foreach (var window in windowsToClose)
            {
                window.CloseWindow();
            }
        }

        /// <summary>
        /// Minimize all windows.
        /// </summary>
        public void MinimizeAllWindows()
        {
            foreach (var window in managedWindows)
            {
                if (!window.IsMinimized)
                {
                    window.MinimizeWindow();
                }
            }
        }

        /// <summary>
        /// Restore all minimized windows.
        /// </summary>
        public void RestoreAllWindows()
        {
            foreach (var window in managedWindows)
            {
                if (window.IsMinimized)
                {
                    window.RestoreWindow();
                }
            }
        }

        #endregion

        #region Window Discovery

        /// <summary>
        /// Find a window by its ID.
        /// </summary>
        /// <param name="windowId">Window ID</param>
        /// <returns>Window if found, null otherwise</returns>
        public Window FindWindowById(int windowId)
        {
            return windowLookup.GetValueOrDefault(windowId);
        }

        /// <summary>
        /// Find a window by its title.
        /// </summary>
        /// <param name="title">Window title</param>
        /// <returns>First window with matching title, null if not found</returns>
        public Window FindWindowByTitle(string title)
        {
            return managedWindows.FirstOrDefault(w => w.WindowTitle.Equals(title, System.StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Find windows by type.
        /// </summary>
        /// <typeparam name="T">Window type</typeparam>
        /// <returns>List of windows of the specified type</returns>
        public List<T> FindWindowsByType<T>() where T : Window
        {
            return managedWindows.OfType<T>().ToList();
        }

        /// <summary>
        /// Get all visible (non-minimized) windows.
        /// </summary>
        /// <returns>List of visible windows</returns>
        public List<Window> GetVisibleWindows()
        {
            return managedWindows.Where(w => !w.IsMinimized).ToList();
        }

        /// <summary>
        /// Get all minimized windows.
        /// </summary>
        /// <returns>List of minimized windows</returns>
        public List<Window> GetMinimizedWindows()
        {
            return managedWindows.Where(w => w.IsMinimized).ToList();
        }

        #endregion

        #region Window Layout

        /// <summary>
        /// Arrange windows in a cascade pattern.
        /// </summary>
        public void CascadeWindows()
        {
            var visibleWindows = GetVisibleWindows();
            Vector2 position = defaultSpawnPosition;

            for (int i = 0; i < visibleWindows.Count; i++)
            {
                var window = visibleWindows[i];
                window.SetPosition(position);
                window.SetSize(defaultWindowSize);

                position += Vector2.one * windowCascadeOffset;

                // Wrap around if we go off screen
                if (position.x > Screen.width - defaultWindowSize.x ||
                    position.y > Screen.height - defaultWindowSize.y)
                {
                    position = defaultSpawnPosition;
                }
            }

            Debug.Log($"[WindowManager] Cascaded {visibleWindows.Count} windows");
        }

        /// <summary>
        /// Tile windows to fill the available space.
        /// </summary>
        public void TileWindows()
        {
            var visibleWindows = GetVisibleWindows();
            if (visibleWindows.Count == 0) return;

            var containerSize = windowContainer.rect.size;
            var rows = Mathf.CeilToInt(Mathf.Sqrt(visibleWindows.Count));
            var cols = Mathf.CeilToInt((float)visibleWindows.Count / rows);

            var windowSize = new Vector2(
                containerSize.x / cols,
                containerSize.y / rows
            );

            for (int i = 0; i < visibleWindows.Count; i++)
            {
                var row = i / cols;
                var col = i % cols;

                var position = new Vector2(
                    col * windowSize.x + windowSize.x / 2,
                    row * windowSize.y + windowSize.y / 2
                );

                var window = visibleWindows[i];
                window.SetPosition(position);
                window.SetSize(windowSize * 0.95f); // Slight margin
            }

            Debug.Log($"[WindowManager] Tiled {visibleWindows.Count} windows in {rows}x{cols} grid");
        }

        #endregion

        #region Event Handling

        private void SubscribeToWindowEvents(Window window)
        {
            window.OnWindowClosed += OnWindowClosedInternal;
            window.OnWindowFocused += OnWindowFocusedInternal;
            window.OnWindowMinimized += OnWindowMinimizedInternal;
            window.OnWindowRestored += OnWindowRestoredInternal;
        }

        private void UnsubscribeFromWindowEvents(Window window)
        {
            window.OnWindowClosed -= OnWindowClosedInternal;
            window.OnWindowFocused -= OnWindowFocusedInternal;
            window.OnWindowMinimized -= OnWindowMinimizedInternal;
            window.OnWindowRestored -= OnWindowRestoredInternal;
        }

        public void OnWindowFocused(Window window)
        {
            FocusWindow(window);
        }

        public void OnWindowClosed(Window window)
        {
            QueueDeferredAction(() => UnregisterWindow(window));
        }

        public void OnWindowMinimized(Window window)
        {
            if (focusedWindow == window)
            {
                FocusNextAvailableWindow();
            }
        }

        public void OnWindowRestored(Window window)
        {
            FocusWindow(window);
        }

        private void OnWindowClosedInternal(Window window)
        {
            OnWindowClosed(window);
        }

        private void OnWindowFocusedInternal(Window window)
        {
            FocusWindow(window);
        }

        private void OnWindowMinimizedInternal(Window window)
        {
            OnWindowMinimized(window);
        }

        private void OnWindowRestoredInternal(Window window)
        {
            OnWindowRestored(window);
        }

        #endregion

        #region Utility Methods

        private void SetupNewWindow(Window window)
        {
            // Set parent
            window.transform.SetParent(windowContainer, false);

            // Calculate spawn position
            Vector2 spawnPosition = defaultSpawnPosition;

            if (cascadeNewWindows && managedWindows.Count > 1)
            {
                spawnPosition += Vector2.one * windowCascadeOffset * (managedWindows.Count - 1);
            }

            // Apply constraints
            var containerSize = windowContainer.rect.size;
            spawnPosition.x = Mathf.Clamp(spawnPosition.x, 0, containerSize.x - defaultWindowSize.x);
            spawnPosition.y = Mathf.Clamp(spawnPosition.y, 0, containerSize.y - defaultWindowSize.y);

            window.SetPosition(spawnPosition);
            window.SetSize(defaultWindowSize);
        }

        private void FocusNextAvailableWindow()
        {
            var visibleWindows = GetVisibleWindows();
            if (visibleWindows.Count > 0)
            {
                FocusWindow(visibleWindows[0]);
            }
            else
            {
                focusedWindow = null;
                taskbar?.SetActiveWindow(null);
            }
        }

        private void QueueDeferredAction(System.Action action)
        {
            deferredActions.Enqueue(action);
        }

        private void ProcessDeferredActions()
        {
            while (deferredActions.Count > 0)
            {
                try
                {
                    var action = deferredActions.Dequeue();
                    action.Invoke();
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[WindowManager] Error processing deferred action: {ex.Message}");
                }
            }
        }

        private void PerformPeriodicCleanup()
        {
            if (Time.time - lastCleanupTime < CleanupInterval) return;

            lastCleanupTime = Time.time;

            // Remove null windows
            managedWindows.RemoveAll(w => w == null);

            // Update lookup dictionary
            var keysToRemove = windowLookup.Where(kvp => kvp.Value == null).Select(kvp => kvp.Key).ToList();
            foreach (var key in keysToRemove)
            {
                windowLookup.Remove(key);
            }
        }

        private void HandleKeyboardShortcuts()
        {
            if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
            {
                if (Input.GetKeyDown(KeyCode.Tab))
                {
                    CycleWindowFocus();
                }
                else if (Input.GetKeyDown(KeyCode.F4))
                {
                    if (focusedWindow != null)
                        focusedWindow.CloseWindow();
                }
            }

            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                if (Input.GetKeyDown(KeyCode.W))
                {
                    if (focusedWindow != null)
                        focusedWindow.CloseWindow();
                }
            }
        }

        private void CycleWindowFocus()
        {
            var visibleWindows = GetVisibleWindows();
            if (visibleWindows.Count <= 1) return;

            var currentIndex = focusedWindow != null ? visibleWindows.IndexOf(focusedWindow) : -1;
            var nextIndex = (currentIndex + 1) % visibleWindows.Count;

            FocusWindow(visibleWindows[nextIndex]);
        }

        private void OnDemoPhaseChanged(DemoPhaseChangedEvent eventData)
        {
            Debug.Log($"[WindowManager] Demo phase changed: {eventData.NewPhase}");
            // Handle demo phase transitions if needed
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get window manager statistics.
        /// </summary>
        /// <returns>Window statistics</returns>
        public WindowManagerStats GetStats()
        {
            return new WindowManagerStats
            {
                TotalWindows = managedWindows.Count,
                VisibleWindows = GetVisibleWindows().Count,
                MinimizedWindows = GetMinimizedWindows().Count,
                FocusedWindowId = focusedWindow?.WindowId ?? -1,
                FocusedWindowTitle = focusedWindow?.WindowTitle ?? "None"
            };
        }

        #endregion

#if UNITY_EDITOR
        [ContextMenu("Debug Window Status")]
        private void DebugWindowStatus()
        {
            var stats = GetStats();
            Debug.Log($"Window Manager Status:\n" +
                     $"  Total Windows: {stats.TotalWindows}\n" +
                     $"  Visible: {stats.VisibleWindows}\n" +
                     $"  Minimized: {stats.MinimizedWindows}\n" +
                     $"  Focused: {stats.FocusedWindowTitle} (ID: {stats.FocusedWindowId})");
        }

        [ContextMenu("Cascade Windows")]
        private void TestCascadeWindows()
        {
            CascadeWindows();
        }

        [ContextMenu("Tile Windows")]
        private void TestTileWindows()
        {
            TileWindows();
        }
#endif
    }

    /// <summary>
    /// Window manager statistics data structure.
    /// </summary>
    [System.Serializable]
    public struct WindowManagerStats
    {
        public int TotalWindows;
        public int VisibleWindows;
        public int MinimizedWindows;
        public int FocusedWindowId;
        public string FocusedWindowTitle;
    }
}