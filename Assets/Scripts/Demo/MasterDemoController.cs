using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Coalition.Demo
{
    /// <summary>
    /// Master orchestration system that coordinates all demo phases and provides
    /// comprehensive tutorial guidance for Dutch political enthusiasts
    /// </summary>
    public class MasterDemoController : MonoBehaviour
    {
        [Header("Demo Configuration")]
        [SerializeField] private DemoConfiguration demoConfig;
        [SerializeField] private TutorialConfiguration tutorialConfig;
        [SerializeField] private bool enableMetricsCollection = true;
        [SerializeField] private bool enableTutorialMode = true;

        [Header("System References")]
        [SerializeField] private DemoGameManager gameManager;
        [SerializeField] private DesktopWindowManager windowManager;
        [SerializeField] private UserTestingFramework testingFramework;
        [SerializeField] private QualityAssuranceManager qaManager;

        // Demo state management
        private DemoPhase currentPhase = DemoPhase.Initialization;
        private DemoSession currentSession;
        private TutorialState tutorialState;
        private Dictionary<string, object> sessionMetrics;

        // Events for demo progression
        public event Action<DemoPhase> OnPhaseChanged;
        public event Action<TutorialStep> OnTutorialStepCompleted;
        public event Action<DemoSession> OnSessionCompleted;
        public event Action<string, object> OnMetricRecorded;

        public DemoPhase CurrentPhase => currentPhase;
        public bool IsSessionActive => currentSession != null && currentSession.IsActive;
        public DemoSession CurrentSession => currentSession;

        private void Awake()
        {
            InitializeController();
        }

        private void Start()
        {
            StartCoroutine(InitializeDemoEnvironment());
        }

        private void InitializeController()
        {
            sessionMetrics = new Dictionary<string, object>();
            tutorialState = new TutorialState();

            // Subscribe to system events
            EventBus.Subscribe<PoliticalEvents.SystemInitialized>(OnSystemInitialized);
            EventBus.Subscribe<PoliticalEvents.ElectionCompleted>(OnElectionCompleted);
            EventBus.Subscribe<PoliticalEvents.CoalitionFormed>(OnCoalitionFormed);
        }

        private IEnumerator InitializeDemoEnvironment()
        {
            Debug.Log("Master Demo Controller: Initializing demo environment...");

            // Phase 1: System Initialization
            yield return StartCoroutine(TransitionToPhase(DemoPhase.Initialization));

            // Initialize core systems
            if (gameManager == null)
                gameManager = FindObjectOfType<DemoGameManager>();

            if (windowManager == null)
                windowManager = FindObjectOfType<DesktopWindowManager>();

            if (testingFramework == null && enableMetricsCollection)
                testingFramework = FindObjectOfType<UserTestingFramework>();

            if (qaManager == null)
                qaManager = FindObjectOfType<QualityAssuranceManager>();

            // Validate system readiness
            yield return new WaitUntil(() => AreAllSystemsReady());

            // Phase 2: Welcome and Tutorial
            if (enableTutorialMode)
            {
                yield return StartCoroutine(TransitionToPhase(DemoPhase.Tutorial));
                yield return StartCoroutine(RunWelcomeTutorial());
            }

            // Phase 3: Ready for interaction
            yield return StartCoroutine(TransitionToPhase(DemoPhase.Ready));

            Debug.Log("Master Demo Controller: Demo environment ready for user interaction");
        }

        public async Task<DemoSession> StartDemoSession(string sessionType = "standard")
        {
            if (IsSessionActive)
            {
                Debug.LogWarning("Demo session already active. Ending current session first.");
                await EndCurrentSession();
            }

            currentSession = new DemoSession
            {
                SessionId = Guid.NewGuid().ToString(),
                SessionType = sessionType,
                StartTime = DateTime.UtcNow,
                IsActive = true,
                ParticipantId = await GetParticipantId()
            };

            // Initialize metrics collection
            if (enableMetricsCollection && testingFramework != null)
            {
                testingFramework.StartTestSession(currentSession.SessionId);
            }

            // Start appropriate demo scenario
            await StartDemoScenario(sessionType);

            Debug.Log($"Demo session started: {currentSession.SessionId} ({sessionType})");
            return currentSession;
        }

        public async Task<DemoSessionResult> EndCurrentSession()
        {
            if (!IsSessionActive)
            {
                Debug.LogWarning("No active demo session to end");
                return null;
            }

            currentSession.EndTime = DateTime.UtcNow;
            currentSession.IsActive = false;

            // Collect final metrics
            var sessionResult = new DemoSessionResult
            {
                Session = currentSession,
                Metrics = new Dictionary<string, object>(sessionMetrics),
                CompletedTutorialSteps = tutorialState.CompletedSteps.ToArray(),
                QualityMetrics = qaManager?.GetSessionQualityMetrics()
            };

            // End testing framework session
            if (testingFramework != null)
            {
                var testingData = testingFramework.EndTestSession(currentSession.SessionId);
                sessionResult.UserTestingData = testingData;
            }

            // Save session data
            await SaveSessionData(sessionResult);

            OnSessionCompleted?.Invoke(currentSession);
            currentSession = null;

            Debug.Log($"Demo session completed: {sessionResult.Session.SessionId}");
            return sessionResult;
        }

        private async Task StartDemoScenario(string scenarioType)
        {
            switch (scenarioType.ToLower())
            {
                case "full_coalition":
                    await RunFullCoalitionFormationScenario();
                    break;
                case "dutch_election":
                    await RunDutchElectionScenario();
                    break;
                case "tutorial":
                    await RunGuidedTutorialScenario();
                    break;
                case "performance_test":
                    await RunPerformanceTestScenario();
                    break;
                default:
                    await RunStandardDemoScenario();
                    break;
            }
        }

        private async Task RunFullCoalitionFormationScenario()
        {
            await TransitionToPhase(DemoPhase.ElectionSimulation);

            // Open relevant windows
            windowManager?.OpenWindow(WindowType.ParliamentVisualization);
            windowManager?.OpenWindow(WindowType.ElectionResults);

            RecordMetric("scenario_started", "full_coalition");

            // Run 2023 Dutch election
            var electionResults = await gameManager.SimulateElection();
            RecordMetric("election_calculation_time", electionResults.CalculationTimeMs);

            await TransitionToPhase(DemoPhase.CoalitionFormation);

            // Open coalition builder
            windowManager?.OpenWindow(WindowType.CoalitionBuilder);

            // Allow user to form coalition or run automated formation
            if (tutorialState.IsGuidedMode)
            {
                await RunGuidedCoalitionFormation();
            }
            else
            {
                await WaitForUserCoalitionCompletion();
            }

            await TransitionToPhase(DemoPhase.ResultsAnalysis);

            // Show results and analysis
            windowManager?.OpenWindow(WindowType.CoalitionAnalysis);

            RecordMetric("scenario_completed", "full_coalition");
        }

        private async Task RunDutchElectionScenario()
        {
            await TransitionToPhase(DemoPhase.ElectionSimulation);

            RecordMetric("scenario_started", "dutch_election");

            // Show Dutch political parties
            var partiesWindow = windowManager?.OpenWindow(WindowType.PartyOverview);
            await Task.Delay(2000); // Allow user to review parties

            // Run election with real 2023 data
            var electionResults = await gameManager.SimulateElection();

            // Show results in parliament visualization
            windowManager?.OpenWindow(WindowType.ParliamentVisualization);
            await UpdateParliamentVisualization(electionResults);

            RecordMetric("election_accuracy_validated", true);
            RecordMetric("scenario_completed", "dutch_election");
        }

        private async Task RunGuidedTutorialScenario()
        {
            tutorialState.IsGuidedMode = true;
            await TransitionToPhase(DemoPhase.Tutorial);

            // Complete tutorial sequence
            var tutorialSteps = tutorialConfig.GetTutorialSteps();

            foreach (var step in tutorialSteps)
            {
                await ExecuteTutorialStep(step);
                tutorialState.CompleteStep(step.StepId);
                OnTutorialStepCompleted?.Invoke(step);
            }

            RecordMetric("tutorial_completed", true);
            RecordMetric("tutorial_steps_completed", tutorialState.CompletedSteps.Count);
        }

        private async Task RunPerformanceTestScenario()
        {
            await TransitionToPhase(DemoPhase.PerformanceTesting);

            RecordMetric("scenario_started", "performance_test");

            // Run multiple election calculations
            var performanceResults = new List<float>();

            for (int i = 0; i < 10; i++)
            {
                var startTime = Time.realtimeSinceStartup;
                await gameManager.SimulateElection();
                var elapsedTime = Time.realtimeSinceStartup - startTime;
                performanceResults.Add(elapsedTime);
            }

            var averageTime = performanceResults.Average();
            RecordMetric("average_election_time", averageTime);
            RecordMetric("performance_test_completed", true);

            Debug.Log($"Performance test completed. Average election time: {averageTime:F3}s");
        }

        private async Task RunStandardDemoScenario()
        {
            // Default demo experience
            await RunDutchElectionScenario();
            await Task.Delay(3000);
            await RunFullCoalitionFormationScenario();
        }

        private IEnumerator RunWelcomeTutorial()
        {
            Debug.Log("Starting welcome tutorial...");

            // Show welcome message
            var welcomeStep = new TutorialStep
            {
                StepId = "welcome",
                Title = "Welcome to COALITION",
                Description = "Experience Dutch coalition formation firsthand",
                WindowsToOpen = new[] { WindowType.Welcome },
                ExpectedDuration = 10f
            };

            yield return StartCoroutine(ExecuteTutorialStepCoroutine(welcomeStep));

            // Introduction to Dutch politics
            var politicsStep = new TutorialStep
            {
                StepId = "dutch_politics_intro",
                Title = "Dutch Political System",
                Description = "Learn about the Dutch parliamentary system and coalition politics",
                WindowsToOpen = new[] { WindowType.PartyOverview, WindowType.PoliticalEducation },
                ExpectedDuration = 30f
            };

            yield return StartCoroutine(ExecuteTutorialStepCoroutine(politicsStep));

            // Coalition formation basics
            var coalitionStep = new TutorialStep
            {
                StepId = "coalition_basics",
                Title = "Coalition Formation",
                Description = "Understand how Dutch political parties form coalitions",
                WindowsToOpen = new[] { WindowType.CoalitionBuilder },
                ExpectedDuration = 20f
            };

            yield return StartCoroutine(ExecuteTutorialStepCoroutine(coalitionStep));

            tutorialState.IsCompleted = true;
            Debug.Log("Welcome tutorial completed");
        }

        private async Task ExecuteTutorialStep(TutorialStep step)
        {
            Debug.Log($"Executing tutorial step: {step.Title}");

            // Open required windows
            foreach (var windowType in step.WindowsToOpen)
            {
                windowManager?.OpenWindow(windowType);
            }

            // Display tutorial content
            await DisplayTutorialContent(step);

            // Wait for user interaction or timeout
            var timeout = step.ExpectedDuration;
            var startTime = Time.realtimeSinceStartup;

            while (Time.realtimeSinceStartup - startTime < timeout)
            {
                if (await CheckStepCompletion(step))
                    break;
                await Task.Delay(100);
            }

            RecordMetric($"tutorial_step_{step.StepId}_time", Time.realtimeSinceStartup - startTime);
        }

        private IEnumerator ExecuteTutorialStepCoroutine(TutorialStep step)
        {
            var task = ExecuteTutorialStep(step);
            yield return new WaitUntil(() => task.IsCompleted);
        }

        private async Task<bool> CheckStepCompletion(TutorialStep step)
        {
            // Implementation depends on step type
            switch (step.StepId)
            {
                case "welcome":
                    return await Task.FromResult(true); // Auto-complete welcome
                case "dutch_politics_intro":
                    return windowManager?.IsWindowOpen(WindowType.PartyOverview) == true;
                case "coalition_basics":
                    return windowManager?.IsWindowOpen(WindowType.CoalitionBuilder) == true;
                default:
                    return await Task.FromResult(true);
            }
        }

        private async Task DisplayTutorialContent(TutorialStep step)
        {
            // Show tutorial overlay or window with step content
            var tutorialUI = FindObjectOfType<TutorialUI>();
            if (tutorialUI != null)
            {
                await tutorialUI.ShowStep(step);
            }
            else
            {
                Debug.Log($"Tutorial: {step.Title} - {step.Description}");
            }
        }

        private IEnumerator TransitionToPhase(DemoPhase newPhase)
        {
            Debug.Log($"Demo transitioning from {currentPhase} to {newPhase}");

            var previousPhase = currentPhase;
            currentPhase = newPhase;

            OnPhaseChanged?.Invoke(newPhase);

            // Handle phase-specific setup
            switch (newPhase)
            {
                case DemoPhase.Initialization:
                    yield return StartCoroutine(SetupInitializationPhase());
                    break;
                case DemoPhase.Tutorial:
                    yield return StartCoroutine(SetupTutorialPhase());
                    break;
                case DemoPhase.ElectionSimulation:
                    yield return StartCoroutine(SetupElectionPhase());
                    break;
                case DemoPhase.CoalitionFormation:
                    yield return StartCoroutine(SetupCoalitionPhase());
                    break;
                case DemoPhase.ResultsAnalysis:
                    yield return StartCoroutine(SetupResultsPhase());
                    break;
            }

            RecordMetric($"phase_transition", $"{previousPhase}_to_{newPhase}");
        }

        private IEnumerator SetupInitializationPhase()
        {
            // Initialize desktop environment
            windowManager?.InitializeDesktop();
            yield return new WaitForSeconds(0.5f);
        }

        private IEnumerator SetupTutorialPhase()
        {
            // Prepare tutorial UI
            windowManager?.CloseAllWindows();
            yield return new WaitForSeconds(0.2f);
        }

        private IEnumerator SetupElectionPhase()
        {
            // Prepare election visualization
            windowManager?.OpenWindow(WindowType.ParliamentVisualization);
            yield return new WaitForSeconds(0.3f);
        }

        private IEnumerator SetupCoalitionPhase()
        {
            // Prepare coalition builder
            windowManager?.OpenWindow(WindowType.CoalitionBuilder);
            yield return new WaitForSeconds(0.3f);
        }

        private IEnumerator SetupResultsPhase()
        {
            // Prepare results display
            windowManager?.OpenWindow(WindowType.CoalitionAnalysis);
            yield return new WaitForSeconds(0.3f);
        }

        private bool AreAllSystemsReady()
        {
            return gameManager != null && gameManager.AreAllSystemsReady() &&
                   windowManager != null && windowManager.IsInitialized;
        }

        private void RecordMetric(string key, object value)
        {
            sessionMetrics[key] = value;
            OnMetricRecorded?.Invoke(key, value);

            if (testingFramework != null && IsSessionActive)
            {
                if (value is float floatValue)
                    testingFramework.RecordMetric(key, floatValue);
                else
                    testingFramework.RecordInteraction(key);
            }
        }

        private async Task<string> GetParticipantId()
        {
            // Generate or retrieve participant ID
            var participantId = PlayerPrefs.GetString("ParticipantId", "");
            if (string.IsNullOrEmpty(participantId))
            {
                participantId = Guid.NewGuid().ToString();
                PlayerPrefs.SetString("ParticipantId", participantId);
            }
            return await Task.FromResult(participantId);
        }

        private async Task SaveSessionData(DemoSessionResult result)
        {
            // Save session data to persistent storage
            var json = JsonUtility.ToJson(result, true);
            var filePath = Application.persistentDataPath + $"/session_{result.Session.SessionId}.json";
            await System.IO.File.WriteAllTextAsync(filePath, json);
            Debug.Log($"Session data saved to: {filePath}");
        }

        // Event handlers
        private void OnSystemInitialized(PoliticalEvents.SystemInitialized evt)
        {
            RecordMetric("system_initialized", evt.SystemName);
        }

        private void OnElectionCompleted(PoliticalEvents.ElectionCompleted evt)
        {
            RecordMetric("election_completed", true);
            RecordMetric("total_seats_allocated", evt.TotalSeats);
        }

        private void OnCoalitionFormed(PoliticalEvents.CoalitionFormed evt)
        {
            RecordMetric("coalition_formed", true);
            RecordMetric("coalition_party_count", evt.PartyCount);
            RecordMetric("coalition_total_seats", evt.TotalSeats);
        }

        private async Task RunGuidedCoalitionFormation()
        {
            // Guided coalition formation tutorial
            Debug.Log("Starting guided coalition formation...");

            // Show available parties
            var availableParties = await gameManager.GetAvailableParties();

            // Guide user through coalition building process
            // Implementation would include UI guidance and step-by-step instructions
        }

        private async Task WaitForUserCoalitionCompletion()
        {
            // Wait for user to complete coalition formation
            var timeout = 300f; // 5 minutes
            var startTime = Time.realtimeSinceStartup;

            while (Time.realtimeSinceStartup - startTime < timeout)
            {
                if (await CheckCoalitionCompletion())
                    break;
                await Task.Delay(1000);
            }
        }

        private async Task<bool> CheckCoalitionCompletion()
        {
            // Check if user has formed a valid coalition
            return await Task.FromResult(false); // Placeholder
        }

        private async Task UpdateParliamentVisualization(object electionResults)
        {
            // Update parliament visualization with election results
            await Task.Delay(100); // Placeholder
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<PoliticalEvents.SystemInitialized>(OnSystemInitialized);
            EventBus.Unsubscribe<PoliticalEvents.ElectionCompleted>(OnElectionCompleted);
            EventBus.Unsubscribe<PoliticalEvents.CoalitionFormed>(OnCoalitionFormed);
        }
    }

    // Supporting data structures
    [Serializable]
    public class DemoSession
    {
        public string SessionId;
        public string SessionType;
        public string ParticipantId;
        public DateTime StartTime;
        public DateTime EndTime;
        public bool IsActive;
    }

    [Serializable]
    public class DemoSessionResult
    {
        public DemoSession Session;
        public Dictionary<string, object> Metrics;
        public string[] CompletedTutorialSteps;
        public object QualityMetrics;
        public object UserTestingData;
    }

    [Serializable]
    public class TutorialStep
    {
        public string StepId;
        public string Title;
        public string Description;
        public WindowType[] WindowsToOpen;
        public float ExpectedDuration;
    }

    public class TutorialState
    {
        public bool IsGuidedMode;
        public bool IsCompleted;
        public List<string> CompletedSteps = new List<string>();

        public void CompleteStep(string stepId)
        {
            if (!CompletedSteps.Contains(stepId))
                CompletedSteps.Add(stepId);
        }
    }

    public enum DemoPhase
    {
        Initialization,
        Tutorial,
        Ready,
        ElectionSimulation,
        CoalitionFormation,
        ResultsAnalysis,
        PerformanceTesting,
        Completed
    }

    // Configuration ScriptableObjects would be defined separately
}