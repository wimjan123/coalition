using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Coalition.Runtime.Core;
using Coalition.Runtime.Data;

namespace Coalition.Runtime.UI
{
    /// <summary>
    /// Main desktop manager that orchestrates the complete COALITION demo UI experience.
    /// Manages windows, taskbar, themes, and integration with political systems.
    /// </summary>
    public class DesktopManager : MonoBehaviour
    {
        [Header("Desktop Configuration")]
        [SerializeField] private Canvas desktopCanvas;
        [SerializeField] private UITheme dutchGovernmentTheme;
        [SerializeField] private Image desktopBackground;
        [SerializeField] private bool enableAccessibilityFeatures = true;

        [Header("Window Management")]
        [SerializeField] private WindowManager windowManager;
        [SerializeField] private DesktopTaskbar taskbar;
        [SerializeField] private UIManager uiManager;

        [Header("Window Prefabs")]
        [SerializeField] private GameObject parliamentWindowPrefab;
        [SerializeField] private GameObject coalitionBuilderWindowPrefab;
        [SerializeField] private GameObject partyInfoWindowPrefab;
        [SerializeField] private GameObject governmentDisplayWindowPrefab;
        [SerializeField] private GameObject demoControlsWindowPrefab;

        [Header("Performance")]
        [SerializeField] private bool enablePerformanceMonitoring = true;
        [SerializeField] private int targetFrameRate = 60;
        [SerializeField] private Text performanceText;

        [Header("Accessibility")]
        [SerializeField] private bool highContrastMode = false;
        [SerializeField] private float uiScale = 1f;
        [SerializeField] private bool enableKeyboardNavigation = true;

        // Window instances
        private ParliamentWindow parliamentWindow;
        private CoalitionBuilderWindow coalitionBuilderWindow;
        private Window partyInfoWindow;
        private Window governmentDisplayWindow;
        private Window demoControlsWindow;

        // System integration
        private DemoGameManager demoGameManager;
        private ElectionResult currentElectionResult;
        private Coalition currentCoalition;

        // Performance monitoring
        private float lastFrameTime;
        private float averageFrameTime;
        private int frameCount;
        private float lastPerformanceUpdate;

        // Desktop state
        private bool isDesktopInitialized = false;
        private DesktopThemeMode currentThemeMode = DesktopThemeMode.Professional;

        #region Properties

        public bool IsDesktopReady => isDesktopInitialized;
        public WindowManager WindowManager => windowManager;
        public DesktopTaskbar Taskbar => taskbar;
        public UITheme CurrentTheme => dutchGovernmentTheme;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeDesktop();
        }

        private void Start()
        {
            SetupDesktopEnvironment();
            InitializeEventSubscriptions();
            LoadDefaultWindows();
        }

        private void Update()
        {
            UpdatePerformanceMonitoring();
            HandleKeyboardShortcuts();
        }

        private void OnDestroy()
        {
            CleanupEventSubscriptions();
        }

        #endregion

        #region Desktop Initialization

        private void InitializeDesktop()
        {
            // Set target frame rate
            Application.targetFrameRate = targetFrameRate;

            // Find core components
            if (windowManager == null)
                windowManager = FindObjectOfType<WindowManager>();

            if (taskbar == null)
                taskbar = FindObjectOfType<DesktopTaskbar>();

            if (uiManager == null)
                uiManager = FindObjectOfType<UIManager>();

            if (demoGameManager == null)
                demoGameManager = FindObjectOfType<DemoGameManager>();

            // Setup desktop canvas
            SetupDesktopCanvas();

            Debug.Log("[DesktopManager] Desktop initialization complete");
        }

        private void SetupDesktopCanvas()
        {
            if (desktopCanvas == null)
            {
                desktopCanvas = GetComponent<Canvas>();
                if (desktopCanvas == null)
                {
                    desktopCanvas = gameObject.AddComponent<Canvas>();
                }
            }

            // Configure canvas for desktop UI
            desktopCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            desktopCanvas.sortingOrder = -100; // Behind windows

            // Add canvas scaler for proper resolution handling
            var canvasScaler = desktopCanvas.GetComponent<CanvasScaler>();
            if (canvasScaler == null)
            {
                canvasScaler = desktopCanvas.gameObject.AddComponent<CanvasScaler>();
            }

            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f;

            // Add graphic raycaster
            var raycaster = desktopCanvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                raycaster = desktopCanvas.gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        private void SetupDesktopEnvironment()
        {
            // Apply Dutch government theme
            ApplyDutchGovernmentTheme();

            // Setup desktop background
            SetupDesktopBackground();

            // Initialize accessibility features
            if (enableAccessibilityFeatures)
            {
                InitializeAccessibilityFeatures();
            }

            isDesktopInitialized = true;

            Debug.Log("[DesktopManager] Desktop environment setup complete");
        }

        private void ApplyDutchGovernmentTheme()
        {
            if (dutchGovernmentTheme == null)
            {
                Debug.LogWarning("[DesktopManager] Dutch government theme not assigned, creating default");
                CreateDefaultDutchTheme();
            }

            // Apply theme to UI manager
            if (uiManager != null)
            {
                uiManager.ApplyTheme(dutchGovernmentTheme);
            }

            // Apply theme to taskbar
            if (taskbar != null)
            {
                taskbar.ApplyTheme(dutchGovernmentTheme);
            }

            Debug.Log("[DesktopManager] Applied Dutch government theme");
        }

        private void CreateDefaultDutchTheme()
        {
            // This would create a default Dutch government theme if none is assigned
            Debug.Log("[DesktopManager] Creating default Dutch government theme");
        }

        private void SetupDesktopBackground()
        {
            if (desktopBackground == null)
            {
                // Create desktop background
                var backgroundObject = new GameObject("DesktopBackground");
                backgroundObject.transform.SetParent(desktopCanvas.transform, false);

                desktopBackground = backgroundObject.AddComponent<Image>();
                var rectTransform = backgroundObject.GetComponent<RectTransform>();

                // Make background fill entire canvas
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;

                // Set as first sibling to be behind everything
                backgroundObject.transform.SetAsFirstSibling();
            }

            // Set background color based on theme
            if (dutchGovernmentTheme != null)
            {
                desktopBackground.color = dutchGovernmentTheme.BackgroundColor;
            }
            else
            {
                desktopBackground.color = new Color(0.95f, 0.95f, 0.95f, 1f); // Light gray
            }
        }

        #endregion

        #region Window Management

        private void LoadDefaultWindows()
        {
            // Create and open default windows for the demo
            OpenParliamentWindow();
            OpenCoalitionBuilderWindow();
            OpenDemoControlsWindow();

            Debug.Log("[DesktopManager] Loaded default windows");
        }

        public void OpenParliamentWindow()
        {
            if (parliamentWindow != null) return;

            if (parliamentWindowPrefab != null && windowManager != null)
            {
                parliamentWindow = windowManager.CreateWindow<ParliamentWindow>(
                    parliamentWindowPrefab, "Parliament - Tweede Kamer");

                if (parliamentWindow != null)
                {
                    // Set initial position and size
                    parliamentWindow.SetPosition(new Vector2(100, 200));
                    parliamentWindow.SetSize(new Vector2(900, 700));

                    // Apply current data
                    if (currentElectionResult != null)
                        parliamentWindow.SetElectionResult(currentElectionResult);

                    if (currentCoalition != null)
                        parliamentWindow.SetCoalition(currentCoalition);

                    parliamentWindow.Open();
                }
            }

            Debug.Log("[DesktopManager] Opened Parliament window");
        }

        public void OpenCoalitionBuilderWindow()
        {
            if (coalitionBuilderWindow != null) return;

            if (coalitionBuilderWindowPrefab != null && windowManager != null)
            {
                coalitionBuilderWindow = windowManager.CreateWindow<CoalitionBuilderWindow>(
                    coalitionBuilderWindowPrefab, "Coalition Builder");

                if (coalitionBuilderWindow != null)
                {
                    // Set initial position and size
                    coalitionBuilderWindow.SetPosition(new Vector2(1020, 200));
                    coalitionBuilderWindow.SetSize(new Vector2(800, 700));

                    // Apply current data
                    if (currentElectionResult != null)
                        coalitionBuilderWindow.SetElectionResult(currentElectionResult);

                    coalitionBuilderWindow.Open();
                }
            }

            Debug.Log("[DesktopManager] Opened Coalition Builder window");
        }

        public void OpenPartyInfoWindow()
        {
            if (partyInfoWindow != null) return;

            if (partyInfoWindowPrefab != null && windowManager != null)
            {
                partyInfoWindow = windowManager.CreateWindow<Window>(
                    partyInfoWindowPrefab, "Party Information");

                if (partyInfoWindow != null)
                {
                    partyInfoWindow.SetPosition(new Vector2(300, 100));
                    partyInfoWindow.SetSize(new Vector2(600, 500));
                    partyInfoWindow.Open();
                }
            }

            Debug.Log("[DesktopManager] Opened Party Info window");
        }

        public void OpenGovernmentDisplayWindow()
        {
            if (governmentDisplayWindow != null) return;

            if (governmentDisplayWindowPrefab != null && windowManager != null)
            {
                governmentDisplayWindow = windowManager.CreateWindow<Window>(
                    governmentDisplayWindowPrefab, "Government Display");

                if (governmentDisplayWindow != null)
                {
                    governmentDisplayWindow.SetPosition(new Vector2(200, 300));
                    governmentDisplayWindow.SetSize(new Vector2(700, 500));
                    governmentDisplayWindow.Open();
                }
            }

            Debug.Log("[DesktopManager] Opened Government Display window");
        }

        public void OpenDemoControlsWindow()
        {
            if (demoControlsWindow != null) return;

            if (demoControlsWindowPrefab != null && windowManager != null)
            {
                demoControlsWindow = windowManager.CreateWindow<Window>(
                    demoControlsWindowPrefab, "Demo Controls");

                if (demoControlsWindow != null)
                {
                    demoControlsWindow.SetPosition(new Vector2(50, 50));
                    demoControlsWindow.SetSize(new Vector2(400, 300));
                    demoControlsWindow.Open();
                }
            }

            Debug.Log("[DesktopManager] Opened Demo Controls window");
        }

        #endregion

        #region System Integration

        private void InitializeEventSubscriptions()
        {
            EventBus.Subscribe<DemoPhaseChangedEvent>(OnDemoPhaseChanged);
            EventBus.Subscribe<ElectionResultsUpdatedEvent>(OnElectionResultsUpdated);
            EventBus.Subscribe<CoalitionChangedEvent>(OnCoalitionChanged);
            EventBus.Subscribe<GovernmentFormedEvent>(OnGovernmentFormed);
            EventBus.Subscribe<TaskbarStartMenuClickedEvent>(OnStartMenuClicked);
        }

        private void CleanupEventSubscriptions()
        {
            EventBus.Unsubscribe<DemoPhaseChangedEvent>(OnDemoPhaseChanged);
            EventBus.Unsubscribe<ElectionResultsUpdatedEvent>(OnElectionResultsUpdated);
            EventBus.Unsubscribe<CoalitionChangedEvent>(OnCoalitionChanged);
            EventBus.Unsubscribe<GovernmentFormedEvent>(OnGovernmentFormed);
            EventBus.Unsubscribe<TaskbarStartMenuClickedEvent>(OnStartMenuClicked);
        }

        #endregion

        #region Performance Monitoring

        private void UpdatePerformanceMonitoring()
        {
            if (!enablePerformanceMonitoring) return;

            frameCount++;
            lastFrameTime = Time.unscaledDeltaTime;
            averageFrameTime = (averageFrameTime * (frameCount - 1) + lastFrameTime) / frameCount;

            // Update performance display every second
            if (Time.time - lastPerformanceUpdate >= 1f)
            {
                lastPerformanceUpdate = Time.time;
                UpdatePerformanceDisplay();
            }
        }

        private void UpdatePerformanceDisplay()
        {
            if (performanceText == null) return;

            var fps = 1f / averageFrameTime;
            var fpsColor = fps >= 55f ? "green" : fps >= 30f ? "yellow" : "red";

            performanceText.text = $"<color={fpsColor}>FPS: {fps:F1}</color>\n" +
                                  $"Frame: {lastFrameTime * 1000:F1}ms\n" +
                                  $"Windows: {windowManager?.WindowCount ?? 0}";
        }

        #endregion

        #region Accessibility Features

        private void InitializeAccessibilityFeatures()
        {
            // Setup high contrast mode if enabled
            if (highContrastMode)
            {
                EnableHighContrastMode();
            }

            // Apply UI scaling
            if (uiScale != 1f)
            {
                ApplyUIScaling();
            }

            // Setup keyboard navigation
            if (enableKeyboardNavigation)
            {
                SetupKeyboardNavigation();
            }

            Debug.Log("[DesktopManager] Accessibility features initialized");
        }

        private void EnableHighContrastMode()
        {
            // This would modify the theme for high contrast
            Debug.Log("[DesktopManager] High contrast mode enabled");
        }

        private void ApplyUIScaling()
        {
            if (desktopCanvas != null)
            {
                var canvasScaler = desktopCanvas.GetComponent<CanvasScaler>();
                if (canvasScaler != null)
                {
                    canvasScaler.scaleFactor = uiScale;
                }
            }

            Debug.Log($"[DesktopManager] UI scaling applied: {uiScale}");
        }

        private void SetupKeyboardNavigation()
        {
            // Setup keyboard navigation system
            Debug.Log("[DesktopManager] Keyboard navigation setup");
        }

        #endregion

        #region Input Handling

        private void HandleKeyboardShortcuts()
        {
            // Desktop-level keyboard shortcuts
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                if (Input.GetKeyDown(KeyCode.Alpha1))
                    OpenParliamentWindow();
                else if (Input.GetKeyDown(KeyCode.Alpha2))
                    OpenCoalitionBuilderWindow();
                else if (Input.GetKeyDown(KeyCode.Alpha3))
                    OpenPartyInfoWindow();
                else if (Input.GetKeyDown(KeyCode.Alpha4))
                    OpenGovernmentDisplayWindow();
                else if (Input.GetKeyDown(KeyCode.Alpha5))
                    OpenDemoControlsWindow();
                else if (Input.GetKeyDown(KeyCode.T))
                    ToggleThemeMode();
                else if (Input.GetKeyDown(KeyCode.H))
                    ToggleHighContrastMode();
            }

            // Function keys for quick actions
            if (Input.GetKeyDown(KeyCode.F1))
                ShowHelp();
            else if (Input.GetKeyDown(KeyCode.F11))
                ToggleFullscreen();
            else if (Input.GetKeyDown(KeyCode.Escape))
                HandleEscapeKey();
        }

        private void ToggleThemeMode()
        {
            currentThemeMode = currentThemeMode == DesktopThemeMode.Professional
                ? DesktopThemeMode.HighContrast
                : DesktopThemeMode.Professional;

            ApplyThemeMode();
            Debug.Log($"[DesktopManager] Toggled theme mode: {currentThemeMode}");
        }

        private void ApplyThemeMode()
        {
            // Apply the selected theme mode
            if (currentThemeMode == DesktopThemeMode.HighContrast)
            {
                EnableHighContrastMode();
            }
            else
            {
                ApplyDutchGovernmentTheme();
            }
        }

        private void ToggleHighContrastMode()
        {
            highContrastMode = !highContrastMode;
            if (highContrastMode)
                EnableHighContrastMode();
            else
                ApplyDutchGovernmentTheme();

            Debug.Log($"[DesktopManager] High contrast mode: {highContrastMode}");
        }

        private void ToggleFullscreen()
        {
            Screen.fullScreen = !Screen.fullScreen;
            Debug.Log($"[DesktopManager] Fullscreen: {Screen.fullScreen}");
        }

        private void ShowHelp()
        {
            Debug.Log("[DesktopManager] Showing help");
            // This could open a help window or overlay
        }

        private void HandleEscapeKey()
        {
            // Close focused window or show desktop menu
            if (windowManager?.FocusedWindow != null)
            {
                windowManager.FocusedWindow.MinimizeWindow();
            }
        }

        #endregion

        #region Event Handlers

        private void OnDemoPhaseChanged(DemoPhaseChangedEvent eventData)
        {
            Debug.Log($"[DesktopManager] Demo phase changed: {eventData.NewPhase}");

            // Update desktop based on demo phase
            switch (eventData.NewPhase)
            {
                case DemoPhase.Elections:
                    // Focus on parliament window
                    if (parliamentWindow != null)
                        parliamentWindow.FocusWindow();
                    break;

                case DemoPhase.CoalitionFormation:
                    // Focus on coalition builder
                    if (coalitionBuilderWindow != null)
                        coalitionBuilderWindow.FocusWindow();
                    break;

                case DemoPhase.GovernmentFormation:
                    // Open government display if needed
                    OpenGovernmentDisplayWindow();
                    break;
            }
        }

        private void OnElectionResultsUpdated(ElectionResultsUpdatedEvent eventData)
        {
            currentElectionResult = eventData.ElectionResult;

            // Update all relevant windows
            if (parliamentWindow != null)
                parliamentWindow.SetElectionResult(currentElectionResult);

            if (coalitionBuilderWindow != null)
                coalitionBuilderWindow.SetElectionResult(currentElectionResult);

            Debug.Log($"[DesktopManager] Election results updated: {currentElectionResult.PartyResults.Count} parties");
        }

        private void OnCoalitionChanged(CoalitionChangedEvent eventData)
        {
            currentCoalition = eventData.Coalition;

            // Update parliament window to highlight coalition
            if (parliamentWindow != null)
                parliamentWindow.SetCoalition(currentCoalition);

            Debug.Log($"[DesktopManager] Coalition changed: {currentCoalition?.PartyNames?.Count ?? 0} parties");
        }

        private void OnGovernmentFormed(GovernmentFormedEvent eventData)
        {
            // Open government display window
            OpenGovernmentDisplayWindow();

            // Show celebration notification
            if (taskbar != null)
            {
                taskbar.AddNotification("Government Formed!",
                    $"New government with {eventData.Coalition.PartyNames.Count} parties", null);
            }

            Debug.Log($"[DesktopManager] Government formed: {string.Join(", ", eventData.Coalition.PartyNames)}");
        }

        private void OnStartMenuClicked(TaskbarStartMenuClickedEvent eventData)
        {
            // Show start menu or application launcher
            ShowStartMenu();
        }

        #endregion

        #region Start Menu

        private void ShowStartMenu()
        {
            Debug.Log("[DesktopManager] Showing start menu");

            // This could show a popup menu with application options
            // For now, we'll provide keyboard shortcuts info
            if (taskbar != null)
            {
                taskbar.AddNotification("Keyboard Shortcuts",
                    "Ctrl+1: Parliament, Ctrl+2: Coalition Builder, Ctrl+3: Party Info", null);
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get desktop statistics and status.
        /// </summary>
        /// <returns>Desktop status information</returns>
        public DesktopStatus GetDesktopStatus()
        {
            return new DesktopStatus
            {
                IsInitialized = isDesktopInitialized,
                WindowCount = windowManager?.WindowCount ?? 0,
                CurrentFPS = 1f / averageFrameTime,
                ThemeMode = currentThemeMode,
                HighContrastEnabled = highContrastMode,
                UIScale = uiScale,
                HasElectionData = currentElectionResult != null,
                HasCoalitionData = currentCoalition != null
            };
        }

        /// <summary>
        /// Force refresh all windows with current data.
        /// </summary>
        public void RefreshAllWindows()
        {
            if (currentElectionResult != null)
            {
                EventBus.Publish(new ElectionResultsUpdatedEvent { ElectionResult = currentElectionResult });
            }

            if (currentCoalition != null)
            {
                EventBus.Publish(new CoalitionChangedEvent
                {
                    Coalition = currentCoalition,
                    TotalSeats = currentCoalition.TotalSeats,
                    HasMajority = currentCoalition.TotalSeats >= 76
                });
            }

            Debug.Log("[DesktopManager] Refreshed all windows");
        }

        /// <summary>
        /// Apply Dutch government theme to all UI elements.
        /// </summary>
        public void ApplyDutchTheme()
        {
            ApplyDutchGovernmentTheme();
        }

        #endregion

#if UNITY_EDITOR
        [ContextMenu("Debug Desktop Status")]
        private void DebugDesktopStatus()
        {
            var status = GetDesktopStatus();
            Debug.Log($"Desktop Status:\n" +
                     $"  Initialized: {status.IsInitialized}\n" +
                     $"  Windows: {status.WindowCount}\n" +
                     $"  FPS: {status.CurrentFPS:F1}\n" +
                     $"  Theme: {status.ThemeMode}\n" +
                     $"  High Contrast: {status.HighContrastEnabled}\n" +
                     $"  UI Scale: {status.UIScale}\n" +
                     $"  Has Election Data: {status.HasElectionData}\n" +
                     $"  Has Coalition Data: {status.HasCoalitionData}");
        }

        [ContextMenu("Open All Windows")]
        private void TestOpenAllWindows()
        {
            OpenParliamentWindow();
            OpenCoalitionBuilderWindow();
            OpenPartyInfoWindow();
            OpenGovernmentDisplayWindow();
            OpenDemoControlsWindow();
        }

        [ContextMenu("Toggle High Contrast")]
        private void TestToggleHighContrast()
        {
            ToggleHighContrastMode();
        }

        [ContextMenu("Refresh All Windows")]
        private void TestRefreshAllWindows()
        {
            RefreshAllWindows();
        }
#endif
    }

    /// <summary>
    /// Desktop theme mode enumeration.
    /// </summary>
    public enum DesktopThemeMode
    {
        Professional,
        HighContrast,
        LargeText
    }

    /// <summary>
    /// Desktop status information data structure.
    /// </summary>
    [System.Serializable]
    public struct DesktopStatus
    {
        public bool IsInitialized;
        public int WindowCount;
        public float CurrentFPS;
        public DesktopThemeMode ThemeMode;
        public bool HighContrastEnabled;
        public float UIScale;
        public bool HasElectionData;
        public bool HasCoalitionData;
    }
}