using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using Coalition.Runtime.Core;
using Coalition.Runtime.Data;

namespace Coalition.Demo
{
    /// <summary>
    /// Main game manager for the COALITION 6-week demo.
    /// Orchestrates demo phases and coordinates between all systems.
    /// </summary>
    public class DemoGameManager : MonoBehaviour
    {
        [Header("Demo Configuration")]
        [SerializeField] private DemoPoliticalDataRepository politicalDataRepository;
        [SerializeField] private bool autoStartDemo = true;
        [SerializeField] private bool enableDebugMode = false;
        [SerializeField] private float phaseTransitionDelay = 1.0f;

        [Header("Demo Phases")]
        [SerializeField] private DemoPhase currentPhase = DemoPhase.Initialization;
        [SerializeField] private bool allowPhaseSkipping = true;

        [Header("Performance Monitoring")]
        [SerializeField] private bool enablePerformanceMonitoring = true;
        [SerializeField] private float performanceWarningThreshold = 5.0f; // seconds

        // System references (will be populated during initialization)
        private DemoElectoralSystem electoralSystem;
        private CoalitionFormationEngine coalitionEngine;
        private DesktopWindowManager windowManager;
        private DemoUIManager uiManager;

        // Demo state
        private ElectionResult currentElectionResult;
        private bool isInitialized = false;
        private DateTime demoStartTime;
        private DemoPhase previousPhase;

        // Events
        public static event Action<DemoPhase> OnPhaseChanged;
        public static event Action<string> OnDemoMessage;
        public static event Action<bool> OnInitializationComplete;

        #region Unity Lifecycle

        private void Awake()
        {
            // Ensure singleton behavior
            if (FindObjectsOfType<DemoGameManager>().Length > 1)
            {
                Debug.LogWarning("[DemoGameManager] Multiple instances found, destroying duplicate");
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
            InitializeEventSubscriptions();
        }

        private void Start()
        {
            demoStartTime = DateTime.Now;

            if (autoStartDemo)
            {
                StartCoroutine(InitializeDemo());
            }
        }

        private void OnDestroy()
        {
            CleanupEventSubscriptions();
        }

        #endregion

        #region Demo Initialization

        /// <summary>
        /// Initialize the demo systems and data.
        /// </summary>
        public async Task<bool> InitializeDemoAsync()
        {
            if (isInitialized)
            {
                Debug.LogWarning("[DemoGameManager] Demo already initialized");
                return true;
            }

            try
            {
                SetPhase(DemoPhase.Initialization);
                PublishDemoMessage("Initializing COALITION Demo...");

                // Initialize political data repository
                if (politicalDataRepository == null)
                {
                    Debug.LogError("[DemoGameManager] Political data repository not assigned");
                    return false;
                }

                politicalDataRepository.Initialize();

                // Find and initialize system components
                await InitializeSystemComponents();

                // Validate all systems are ready
                bool systemsValid = ValidateSystemComponents();
                if (!systemsValid)
                {
                    Debug.LogError("[DemoGameManager] System validation failed");
                    return false;
                }

                // Initialize electoral system with 2023 data
                await InitializeElectoralSystem();

                isInitialized = true;
                PublishDemoMessage("Demo initialization complete");

                OnInitializationComplete?.Invoke(true);
                EventBus.Publish(new PoliticalDataInitializedEvent(
                    politicalDataRepository.AllParties.Length,
                    true,
                    "Demo initialization successful"
                ));

                // Transition to election results phase
                if (autoStartDemo)
                {
                    await Task.Delay(TimeSpan.FromSeconds(phaseTransitionDelay));
                    SetPhase(DemoPhase.ElectionResults);
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DemoGameManager] Initialization failed: {ex.Message}\n{ex.StackTrace}");
                OnInitializationComplete?.Invoke(false);
                return false;
            }
        }

        private IEnumerator InitializeDemo()
        {
            var initTask = InitializeDemoAsync();
            yield return new WaitUntil(() => initTask.IsCompleted);

            if (!initTask.Result)
            {
                Debug.LogError("[DemoGameManager] Demo initialization failed");
                PublishDemoMessage("Demo initialization failed. Please check the logs.");
            }
        }

        private async Task InitializeSystemComponents()
        {
            // Find electoral system
            electoralSystem = FindObjectOfType<DemoElectoralSystem>();
            if (electoralSystem == null)
            {
                var electoralGO = new GameObject("DemoElectoralSystem");
                electoralSystem = electoralGO.AddComponent<DemoElectoralSystem>();
            }

            // Find coalition formation engine
            coalitionEngine = FindObjectOfType<CoalitionFormationEngine>();
            if (coalitionEngine == null)
            {
                var coalitionGO = new GameObject("CoalitionFormationEngine");
                coalitionEngine = coalitionGO.AddComponent<CoalitionFormationEngine>();
            }

            // Find window manager
            windowManager = FindObjectOfType<DesktopWindowManager>();
            if (windowManager == null)
            {
                Debug.LogWarning("[DemoGameManager] DesktopWindowManager not found - UI features will be limited");
            }

            // Find UI manager
            uiManager = FindObjectOfType<DemoUIManager>();
            if (uiManager == null)
            {
                Debug.LogWarning("[DemoGameManager] DemoUIManager not found - UI features will be limited");
            }

            // Initialize components
            if (electoralSystem != null) await electoralSystem.InitializeAsync(politicalDataRepository);
            if (coalitionEngine != null) await coalitionEngine.InitializeAsync(politicalDataRepository);
            if (windowManager != null) windowManager.Initialize();
            if (uiManager != null) uiManager.Initialize();

            await Task.Yield(); // Ensure async context
        }

        private bool ValidateSystemComponents()
        {
            bool isValid = true;

            if (electoralSystem == null)
            {
                Debug.LogError("[DemoGameManager] Electoral system not found");
                isValid = false;
            }

            if (coalitionEngine == null)
            {
                Debug.LogError("[DemoGameManager] Coalition formation engine not found");
                isValid = false;
            }

            if (politicalDataRepository == null)
            {
                Debug.LogError("[DemoGameManager] Political data repository not assigned");
                isValid = false;
            }

            return isValid;
        }

        private async Task InitializeElectoralSystem()
        {
            if (electoralSystem == null) return;

            var startTime = Time.realtimeSinceStartup;
            currentElectionResult = await electoralSystem.CalculateElection2023();
            var calculationTime = Time.realtimeSinceStartup - startTime;

            if (enablePerformanceMonitoring && calculationTime > performanceWarningThreshold)
            {
                EventBus.Publish(new PerformanceWarningEvent(
                    "Election calculation",
                    calculationTime,
                    performanceWarningThreshold,
                    "D'Hondt algorithm calculation exceeded threshold"
                ));
            }

            Debug.Log($"[DemoGameManager] 2023 election calculated in {calculationTime:F3} seconds");
        }

        #endregion

        #region Phase Management

        /// <summary>
        /// Set the current demo phase and notify all systems.
        /// </summary>
        /// <param name="newPhase">The new phase to transition to</param>
        public void SetPhase(DemoPhase newPhase)
        {
            if (currentPhase == newPhase) return;

            previousPhase = currentPhase;
            currentPhase = newPhase;

            Debug.Log($"[DemoGameManager] Phase transition: {previousPhase} → {newPhase}");

            OnPhaseChanged?.Invoke(newPhase);
            EventBus.Publish(new DemoPhaseChangedEvent(previousPhase, newPhase, GetPhaseName(newPhase)));

            // Handle phase-specific logic
            HandlePhaseTransition(newPhase);
        }

        /// <summary>
        /// Advance to the next logical demo phase.
        /// </summary>
        public void NextPhase()
        {
            var nextPhase = GetNextPhase(currentPhase);
            if (nextPhase != currentPhase)
            {
                SetPhase(nextPhase);
            }
        }

        /// <summary>
        /// Go back to the previous demo phase if possible.
        /// </summary>
        public void PreviousPhase()
        {
            if (allowPhaseSkipping && previousPhase != currentPhase)
            {
                SetPhase(previousPhase);
            }
        }

        private DemoPhase GetNextPhase(DemoPhase current)
        {
            return current switch
            {
                DemoPhase.Initialization => DemoPhase.ElectionResults,
                DemoPhase.ElectionResults => DemoPhase.PartyExploration,
                DemoPhase.PartyExploration => DemoPhase.CoalitionBuilding,
                DemoPhase.CoalitionBuilding => DemoPhase.GovernmentFormation,
                DemoPhase.GovernmentFormation => DemoPhase.Analysis,
                DemoPhase.Analysis => DemoPhase.Tutorial,
                DemoPhase.Tutorial => DemoPhase.UserTesting,
                DemoPhase.UserTesting => DemoPhase.UserTesting, // Stay in final phase
                _ => current
            };
        }

        private string GetPhaseName(DemoPhase phase)
        {
            return phase switch
            {
                DemoPhase.Initialization => "System Initialization",
                DemoPhase.ElectionResults => "2023 Election Results",
                DemoPhase.PartyExploration => "Political Party Exploration",
                DemoPhase.CoalitionBuilding => "Coalition Formation",
                DemoPhase.GovernmentFormation => "Government Formation",
                DemoPhase.Analysis => "Political Analysis",
                DemoPhase.Tutorial => "Tutorial Mode",
                DemoPhase.UserTesting => "User Testing",
                _ => "Unknown Phase"
            };
        }

        private void HandlePhaseTransition(DemoPhase newPhase)
        {
            switch (newPhase)
            {
                case DemoPhase.ElectionResults:
                    StartCoroutine(HandleElectionResultsPhase());
                    break;

                case DemoPhase.PartyExploration:
                    HandlePartyExplorationPhase();
                    break;

                case DemoPhase.CoalitionBuilding:
                    StartCoroutine(HandleCoalitionBuildingPhase());
                    break;

                case DemoPhase.GovernmentFormation:
                    HandleGovernmentFormationPhase();
                    break;

                case DemoPhase.Analysis:
                    HandleAnalysisPhase();
                    break;

                case DemoPhase.Tutorial:
                    HandleTutorialPhase();
                    break;

                case DemoPhase.UserTesting:
                    HandleUserTestingPhase();
                    break;
            }
        }

        #endregion

        #region Phase Handlers

        private IEnumerator HandleElectionResultsPhase()
        {
            PublishDemoMessage("Displaying 2023 Dutch Election Results");

            if (currentElectionResult.PartySeats != null)
            {
                EventBus.Publish(new ElectionResultsDisplayedEvent(currentElectionResult, false));
            }

            // Open parliament window if available
            if (windowManager != null)
            {
                windowManager.OpenWindow(WindowType.Parliament);
            }

            yield return new WaitForSeconds(2.0f);

            if (autoStartDemo)
            {
                NextPhase();
            }
        }

        private void HandlePartyExplorationPhase()
        {
            PublishDemoMessage("Explore Dutch Political Parties");

            // Open party information window
            if (windowManager != null)
            {
                windowManager.OpenWindow(WindowType.PartyInfo);
            }
        }

        private IEnumerator HandleCoalitionBuildingPhase()
        {
            PublishDemoMessage("Begin Coalition Formation");

            // Open coalition builder window
            if (windowManager != null)
            {
                windowManager.OpenWindow(WindowType.CoalitionBuilder);
            }

            // Calculate viable coalitions
            if (coalitionEngine != null && currentElectionResult.PartySeats != null)
            {
                var startTime = Time.realtimeSinceStartup;
                var viableCoalitions = await coalitionEngine.FindViableCoalitions(currentElectionResult);
                var calculationTime = Time.realtimeSinceStartup - startTime;

                if (enablePerformanceMonitoring && calculationTime > performanceWarningThreshold)
                {
                    EventBus.Publish(new PerformanceWarningEvent(
                        "Coalition calculation",
                        calculationTime,
                        performanceWarningThreshold,
                        $"Found {viableCoalitions.Count} viable coalitions"
                    ));
                }

                Debug.Log($"[DemoGameManager] Found {viableCoalitions.Count} viable coalitions in {calculationTime:F3} seconds");
            }

            yield return null;
        }

        private void HandleGovernmentFormationPhase()
        {
            PublishDemoMessage("Form Government and Allocate Portfolios");

            // Open government formation window
            if (windowManager != null)
            {
                windowManager.OpenWindow(WindowType.Government);
            }
        }

        private void HandleAnalysisPhase()
        {
            PublishDemoMessage("Analyze Coalition Options");
        }

        private void HandleTutorialPhase()
        {
            PublishDemoMessage("Tutorial: Learn Dutch Coalition Formation");
        }

        private void HandleUserTestingPhase()
        {
            PublishDemoMessage("Ready for User Testing");
        }

        #endregion

        #region Event Handling

        private void InitializeEventSubscriptions()
        {
            EventBus.Subscribe<CoalitionFormedEvent>(OnCoalitionFormed);
            EventBus.Subscribe<CoalitionFormationFailedEvent>(OnCoalitionFormationFailed);
            EventBus.Subscribe<PerformanceWarningEvent>(OnPerformanceWarning);
        }

        private void CleanupEventSubscriptions()
        {
            EventBus.Unsubscribe<CoalitionFormedEvent>(OnCoalitionFormed);
            EventBus.Unsubscribe<CoalitionFormationFailedEvent>(OnCoalitionFormationFailed);
            EventBus.Unsubscribe<PerformanceWarningEvent>(OnPerformanceWarning);
        }

        private void OnCoalitionFormed(CoalitionFormedEvent eventData)
        {
            Debug.Log($"[DemoGameManager] Coalition formed with {eventData.Coalition.Parties.Count} parties");
            PublishDemoMessage($"Coalition formed: {string.Join(", ", eventData.Coalition.Parties.Select(p => p.Abbreviation))}");
        }

        private void OnCoalitionFormationFailed(CoalitionFormationFailedEvent eventData)
        {
            Debug.LogWarning($"[DemoGameManager] Coalition formation failed: {eventData.FailureReason}");
            PublishDemoMessage($"Coalition formation failed: {eventData.FailureReason}");
        }

        private void OnPerformanceWarning(PerformanceWarningEvent eventData)
        {
            Debug.LogWarning($"[DemoGameManager] Performance warning: {eventData.Operation} took {eventData.ActualTimeSeconds:F3}s (threshold: {eventData.ThresholdSeconds:F3}s)");
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get the current demo phase.
        /// </summary>
        public DemoPhase GetCurrentPhase() => currentPhase;

        /// <summary>
        /// Check if the demo is fully initialized.
        /// </summary>
        public bool IsInitialized() => isInitialized;

        /// <summary>
        /// Get the current election result.
        /// </summary>
        public ElectionResult GetCurrentElectionResult() => currentElectionResult;

        /// <summary>
        /// Get demo runtime information.
        /// </summary>
        public DemoRuntimeInfo GetRuntimeInfo()
        {
            return new DemoRuntimeInfo
            {
                IsInitialized = isInitialized,
                CurrentPhase = currentPhase,
                RuntimeDuration = DateTime.Now - demoStartTime,
                HasElectionResults = currentElectionResult.PartySeats != null,
                SystemsActive = ValidateSystemComponents()
            };
        }

        /// <summary>
        /// Restart the demo from the beginning.
        /// </summary>
        public async Task RestartDemo()
        {
            PublishDemoMessage("Restarting demo...");
            isInitialized = false;
            currentElectionResult = default;
            SetPhase(DemoPhase.Initialization);

            await InitializeDemoAsync();
        }

        #endregion

        #region Utility Methods

        private void PublishDemoMessage(string message)
        {
            if (enableDebugMode)
            {
                Debug.Log($"[DemoGameManager] {message}");
            }

            OnDemoMessage?.Invoke(message);
        }

        #endregion

        #region Debug and Development

#if UNITY_EDITOR
        [ContextMenu("Force Next Phase")]
        private void ForceNextPhase()
        {
            NextPhase();
        }

        [ContextMenu("Reset Demo")]
        private void ResetDemo()
        {
            _ = RestartDemo();
        }

        [ContextMenu("Print Demo Status")]
        private void PrintDemoStatus()
        {
            var info = GetRuntimeInfo();
            Debug.Log($"Demo Status:\n" +
                     $"  Initialized: {info.IsInitialized}\n" +
                     $"  Phase: {info.CurrentPhase}\n" +
                     $"  Runtime: {info.RuntimeDuration.TotalMinutes:F1} minutes\n" +
                     $"  Has Election Results: {info.HasElectionResults}\n" +
                     $"  Systems Active: {info.SystemsActive}");
        }
#endif

        #endregion
    }

    /// <summary>
    /// Runtime information about the demo state.
    /// </summary>
    public struct DemoRuntimeInfo
    {
        public bool IsInitialized;
        public DemoPhase CurrentPhase;
        public TimeSpan RuntimeDuration;
        public bool HasElectionResults;
        public bool SystemsActive;
    }

    // Forward declarations for components that will be implemented in future phases
    public class DemoElectoralSystem : MonoBehaviour
    {
        public async Task InitializeAsync(DemoPoliticalDataRepository repository) { await Task.Yield(); }
        public async Task<ElectionResult> CalculateElection2023() { await Task.Yield(); return default; }
    }

    public class CoalitionFormationEngine : MonoBehaviour
    {
        public async Task InitializeAsync(DemoPoliticalDataRepository repository) { await Task.Yield(); }
        public async Task<List<ViableCoalition>> FindViableCoalitions(ElectionResult result) { await Task.Yield(); return new List<ViableCoalition>(); }
    }

    public class DesktopWindowManager : MonoBehaviour
    {
        public void Initialize() { }
        public void OpenWindow(WindowType type) { }
    }

    public class DemoUIManager : MonoBehaviour
    {
        public void Initialize() { }
    }

    public enum WindowType
    {
        Parliament,
        PartyInfo,
        CoalitionBuilder,
        Government,
        ElectionResults,
        Settings,
        Help
    }
}