using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Coalition.Runtime.Core;

namespace Coalition.Runtime.UI
{
    /// <summary>
    /// Core UI management system for the COALITION demo.
    /// Provides basic UI framework for desktop-style interface.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("UI Configuration")]
        [SerializeField] private Canvas mainCanvas;
        [SerializeField] private GraphicRaycaster raycaster;
        [SerializeField] private CanvasScaler canvasScaler;

        [Header("UI Theme")]
        [SerializeField] private UITheme currentTheme;
        [SerializeField] private bool applyThemeOnStart = true;

        [Header("Performance")]
        [SerializeField] private bool enableUIOptimization = true;
        [SerializeField] private int maxSimultaneousAnimations = 5;

        // UI state management
        private Dictionary<string, UIPanel> registeredPanels;
        private Stack<UIPanel> panelHistory;
        private UIPanel currentActivePanel;
        private bool isInitialized = false;

        // UI optimization
        private Queue<System.Action> deferredUIActions;
        private int activeAnimations = 0;

        // Events
        public static event System.Action<UIPanel> OnPanelOpened;
        public static event System.Action<UIPanel> OnPanelClosed;
        public static event System.Action<UITheme> OnThemeChanged;

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeUISystem();
        }

        private void Start()
        {
            if (applyThemeOnStart && currentTheme != null)
            {
                ApplyTheme(currentTheme);
            }

            InitializeEventSubscriptions();
        }

        private void Update()
        {
            ProcessDeferredUIActions();
        }

        private void OnDestroy()
        {
            CleanupEventSubscriptions();
        }

        #endregion

        #region Initialization

        private void InitializeUISystem()
        {
            registeredPanels = new Dictionary<string, UIPanel>();
            panelHistory = new Stack<UIPanel>();
            deferredUIActions = new Queue<System.Action>();

            SetupMainCanvas();
            isInitialized = true;

            Debug.Log("[UIManager] UI system initialized");
        }

        private void SetupMainCanvas()
        {
            if (mainCanvas == null)
            {
                mainCanvas = FindObjectOfType<Canvas>();
                if (mainCanvas == null)
                {
                    Debug.LogError("[UIManager] No Canvas found in scene");
                    return;
                }
            }

            // Ensure canvas has required components
            if (raycaster == null)
                raycaster = mainCanvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
                raycaster = mainCanvas.gameObject.AddComponent<GraphicRaycaster>();

            if (canvasScaler == null)
                canvasScaler = mainCanvas.GetComponent<CanvasScaler>();
            if (canvasScaler == null)
                canvasScaler = mainCanvas.gameObject.AddComponent<CanvasScaler>();

            // Configure for desktop UI
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f;
        }

        #endregion

        #region Panel Management

        /// <summary>
        /// Register a UI panel with the manager.
        /// </summary>
        /// <param name="panelId">Unique identifier for the panel</param>
        /// <param name="panel">The UI panel component</param>
        public void RegisterPanel(string panelId, UIPanel panel)
        {
            if (string.IsNullOrEmpty(panelId) || panel == null)
            {
                Debug.LogError("[UIManager] Cannot register panel: invalid parameters");
                return;
            }

            if (registeredPanels.ContainsKey(panelId))
            {
                Debug.LogWarning($"[UIManager] Panel {panelId} already registered, replacing");
            }

            registeredPanels[panelId] = panel;
            panel.Initialize(this, panelId);

            Debug.Log($"[UIManager] Registered panel: {panelId}");
        }

        /// <summary>
        /// Unregister a UI panel from the manager.
        /// </summary>
        /// <param name="panelId">Panel identifier to unregister</param>
        public void UnregisterPanel(string panelId)
        {
            if (registeredPanels.ContainsKey(panelId))
            {
                registeredPanels.Remove(panelId);
                Debug.Log($"[UIManager] Unregistered panel: {panelId}");
            }
        }

        /// <summary>
        /// Open a registered UI panel.
        /// </summary>
        /// <param name="panelId">Panel identifier</param>
        /// <param name="addToHistory">Whether to add to navigation history</param>
        public void OpenPanel(string panelId, bool addToHistory = true)
        {
            if (!registeredPanels.ContainsKey(panelId))
            {
                Debug.LogError($"[UIManager] Panel {panelId} not registered");
                return;
            }

            var panel = registeredPanels[panelId];

            // Close current panel if different
            if (currentActivePanel != null && currentActivePanel != panel)
            {
                ClosePanel(currentActivePanel.PanelId, false);
            }

            // Add to history
            if (addToHistory && currentActivePanel != null)
            {
                panelHistory.Push(currentActivePanel);
            }

            // Open new panel
            currentActivePanel = panel;
            panel.Open();

            OnPanelOpened?.Invoke(panel);
            Debug.Log($"[UIManager] Opened panel: {panelId}");
        }

        /// <summary>
        /// Close a UI panel.
        /// </summary>
        /// <param name="panelId">Panel identifier</param>
        /// <param name="returnToPrevious">Whether to return to previous panel in history</param>
        public void ClosePanel(string panelId, bool returnToPrevious = true)
        {
            if (!registeredPanels.ContainsKey(panelId))
            {
                Debug.LogError($"[UIManager] Panel {panelId} not registered");
                return;
            }

            var panel = registeredPanels[panelId];
            panel.Close();

            if (currentActivePanel == panel)
            {
                currentActivePanel = null;

                // Return to previous panel if requested and available
                if (returnToPrevious && panelHistory.Count > 0)
                {
                    var previousPanel = panelHistory.Pop();
                    OpenPanel(previousPanel.PanelId, false);
                }
            }

            OnPanelClosed?.Invoke(panel);
            Debug.Log($"[UIManager] Closed panel: {panelId}");
        }

        /// <summary>
        /// Toggle a UI panel (open if closed, close if open).
        /// </summary>
        /// <param name="panelId">Panel identifier</param>
        public void TogglePanel(string panelId)
        {
            if (!registeredPanels.ContainsKey(panelId))
            {
                Debug.LogError($"[UIManager] Panel {panelId} not registered");
                return;
            }

            var panel = registeredPanels[panelId];
            if (panel.IsOpen)
            {
                ClosePanel(panelId);
            }
            else
            {
                OpenPanel(panelId);
            }
        }

        /// <summary>
        /// Check if a panel is currently open.
        /// </summary>
        /// <param name="panelId">Panel identifier</param>
        /// <returns>True if panel is open</returns>
        public bool IsPanelOpen(string panelId)
        {
            if (registeredPanels.ContainsKey(panelId))
            {
                return registeredPanels[panelId].IsOpen;
            }
            return false;
        }

        /// <summary>
        /// Get a registered panel by ID.
        /// </summary>
        /// <param name="panelId">Panel identifier</param>
        /// <returns>The panel if found, null otherwise</returns>
        public UIPanel GetPanel(string panelId)
        {
            return registeredPanels.GetValueOrDefault(panelId);
        }

        #endregion

        #region Theme Management

        /// <summary>
        /// Apply a UI theme to all registered panels.
        /// </summary>
        /// <param name="theme">Theme to apply</param>
        public void ApplyTheme(UITheme theme)
        {
            if (theme == null)
            {
                Debug.LogError("[UIManager] Cannot apply null theme");
                return;
            }

            currentTheme = theme;

            // Apply theme to canvas
            if (mainCanvas != null)
            {
                var canvasImage = mainCanvas.GetComponent<Image>();
                if (canvasImage != null)
                {
                    canvasImage.color = theme.BackgroundColor;
                }
            }

            // Apply theme to all registered panels
            foreach (var panel in registeredPanels.Values)
            {
                panel.ApplyTheme(theme);
            }

            OnThemeChanged?.Invoke(theme);
            Debug.Log($"[UIManager] Applied theme: {theme.name}");
        }

        /// <summary>
        /// Get the current UI theme.
        /// </summary>
        public UITheme GetCurrentTheme() => currentTheme;

        #endregion

        #region UI Optimization

        /// <summary>
        /// Queue a UI action to be executed when performance allows.
        /// </summary>
        /// <param name="action">Action to execute</param>
        public void QueueUIAction(System.Action action)
        {
            if (action != null)
            {
                deferredUIActions.Enqueue(action);
            }
        }

        /// <summary>
        /// Execute deferred UI actions based on performance constraints.
        /// </summary>
        private void ProcessDeferredUIActions()
        {
            if (!enableUIOptimization) return;

            // Limit UI actions per frame to maintain performance
            int actionsThisFrame = 0;
            int maxActionsPerFrame = 3;

            while (deferredUIActions.Count > 0 && actionsThisFrame < maxActionsPerFrame)
            {
                var action = deferredUIActions.Dequeue();
                try
                {
                    action.Invoke();
                    actionsThisFrame++;
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[UIManager] Error executing deferred UI action: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Check if an animation can be started based on performance constraints.
        /// </summary>
        /// <returns>True if animation can start</returns>
        public bool CanStartAnimation()
        {
            return !enableUIOptimization || activeAnimations < maxSimultaneousAnimations;
        }

        /// <summary>
        /// Register that an animation has started.
        /// </summary>
        public void RegisterAnimationStart()
        {
            activeAnimations++;
        }

        /// <summary>
        /// Register that an animation has ended.
        /// </summary>
        public void RegisterAnimationEnd()
        {
            activeAnimations = Mathf.Max(0, activeAnimations - 1);
        }

        #endregion

        #region Event Handling

        private void InitializeEventSubscriptions()
        {
            EventBus.Subscribe<DemoPhaseChangedEvent>(OnDemoPhaseChanged);
        }

        private void CleanupEventSubscriptions()
        {
            EventBus.Unsubscribe<DemoPhaseChangedEvent>(OnDemoPhaseChanged);
        }

        private void OnDemoPhaseChanged(DemoPhaseChangedEvent eventData)
        {
            Debug.Log($"[UIManager] Demo phase changed to: {eventData.NewPhase}");
            // Handle UI changes based on demo phase if needed
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get UI manager initialization status.
        /// </summary>
        public bool IsInitialized() => isInitialized;

        /// <summary>
        /// Get the main canvas reference.
        /// </summary>
        public Canvas GetMainCanvas() => mainCanvas;

        /// <summary>
        /// Get count of registered panels.
        /// </summary>
        public int GetRegisteredPanelCount() => registeredPanels.Count;

        /// <summary>
        /// Get list of all registered panel IDs.
        /// </summary>
        public List<string> GetRegisteredPanelIds()
        {
            return new List<string>(registeredPanels.Keys);
        }

        /// <summary>
        /// Close all open panels.
        /// </summary>
        public void CloseAllPanels()
        {
            var panelsToClose = new List<string>();
            foreach (var kvp in registeredPanels)
            {
                if (kvp.Value.IsOpen)
                {
                    panelsToClose.Add(kvp.Key);
                }
            }

            foreach (var panelId in panelsToClose)
            {
                ClosePanel(panelId, false);
            }

            panelHistory.Clear();
            currentActivePanel = null;

            Debug.Log("[UIManager] Closed all panels");
        }

        #endregion

#if UNITY_EDITOR
        [ContextMenu("Debug Panel Status")]
        private void DebugPanelStatus()
        {
            Debug.Log($"UI Manager Status:\n" +
                     $"  Initialized: {isInitialized}\n" +
                     $"  Registered Panels: {registeredPanels.Count}\n" +
                     $"  Current Active: {(currentActivePanel?.PanelId ?? "None")}\n" +
                     $"  History Depth: {panelHistory.Count}\n" +
                     $"  Active Animations: {activeAnimations}\n" +
                     $"  Deferred Actions: {deferredUIActions.Count}");
        }
#endif
    }
}