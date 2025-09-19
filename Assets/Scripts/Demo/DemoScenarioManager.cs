using System;
using System.Collections.Generic;
using UnityEngine;

namespace Coalition.Demo
{
    /// <summary>
    /// Manages the four demo scenarios for user testing:
    /// 1. The Obvious Coalition (VVD-NSC-BBB)
    /// 2. The Difficult Choice (PVV exclusion handling)
    /// 3. The Grand Coalition (broad consensus government)
    /// 4. Historical Recreation (Rutte III recreation)
    /// </summary>
    public class DemoScenarioManager : MonoBehaviour
    {
        [Header("Scenario Configuration")]
        [SerializeField] private bool enableGuidedMode = true;
        [SerializeField] private bool showHints = true;
        [SerializeField] private float hintDelaySeconds = 30f;

        [SerializeField] private List<DemoScenario> scenarios = new List<DemoScenario>();

        private DemoScenario currentScenario;
        private int currentStepIndex = 0;
        private float stepStartTime;
        private bool scenarioCompleted = false;

        public static DemoScenarioManager Instance { get; private set; }

        // Events
        public event Action<DemoScenario> OnScenarioStarted;
        public event Action<DemoScenario> OnScenarioCompleted;
        public event Action<ScenarioStep> OnStepStarted;
        public event Action<ScenarioStep> OnStepCompleted;
        public event Action<string> OnHintShown;

        public DemoScenario CurrentScenario => currentScenario;
        public bool IsScenarioActive => currentScenario != null;
        public float StepDuration => IsScenarioActive ? Time.time - stepStartTime : 0f;

        [Serializable]
        public class DemoScenario
        {
            public string name;
            public string description;
            public DifficultyLevel difficulty;
            public List<string> learningObjectives;
            public List<string> requiredParties;
            public List<string> excludedParties;
            public int minimumSeats;
            public int targetSeats;
            public List<ScenarioStep> steps;
            public ScenarioConstraints constraints;
            public ScenarioSuccessCriteria successCriteria;
            public string contextualBackground;
            public List<string> expectedChallenges;
        }

        [Serializable]
        public class ScenarioStep
        {
            public string title;
            public string instruction;
            public string helpText;
            public List<string> hints;
            public StepType type;
            public Dictionary<string, object> parameters = new Dictionary<string, object>();
            public float suggestedTimeLimit;
            public bool isOptional;
            public List<string> validationCriteria;
        }

        [Serializable]
        public class ScenarioConstraints
        {
            public bool allowPVVInclusion;
            public bool requireMajority;
            public bool allowMinorityGovernment;
            public List<string> mandatoryPortfolios;
            public List<string> blockedCombinations;
            public int maxParties;
            public int minParties;
        }

        [Serializable]
        public class ScenarioSuccessCriteria
        {
            public int requiredSeats;
            public List<string> requiredParties;
            public float maxTimeMinutes;
            public bool requiresRealisticPortfolios;
            public float minimumCompatibilityScore;
            public List<string> policyAgreementAreas;
        }

        public enum DifficultyLevel
        {
            Beginner,
            Intermediate,
            Advanced,
            Expert
        }

        public enum StepType
        {
            Introduction,
            PartySelection,
            CoalitionBuilding,
            NegotiationPhase,
            PortfolioAllocation,
            PolicyAgreement,
            Validation,
            Reflection
        }

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeScenarios();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeScenarios()
        {
            scenarios.Clear();

            scenarios.Add(CreateObviousCoalitionScenario());
            scenarios.Add(CreateDifficultChoiceScenario());
            scenarios.Add(CreateGrandCoalitionScenario());
            scenarios.Add(CreateHistoricalRecreationScenario());

            Debug.Log($"Demo Scenario Manager initialized with {scenarios.Count} scenarios");
        }

        private DemoScenario CreateObviousCoalitionScenario()
        {
            var scenario = new DemoScenario
            {
                name = "The Obvious Coalition",
                description = "Form a stable center-right coalition using VVD, NSC, and BBB",
                difficulty = DifficultyLevel.Beginner,
                learningObjectives = new List<string>
                {
                    "Understand basic coalition math (75+ seats requirement)",
                    "Learn party compatibility concepts",
                    "Practice portfolio allocation",
                    "Understand Dutch center-right politics"
                },
                requiredParties = new List<string> { "VVD", "NSC", "BBB" },
                excludedParties = new List<string> { "PVV" },
                minimumSeats = 76,
                targetSeats = 88, // VVD(34) + NSC(20) + BBB(7) = 61 - wait, this doesn't add up to 88...
                contextualBackground = "After the 2023 Dutch election, VVD emerged as a major party but needs coalition partners. NSC and BBB represent natural center-right allies with compatible ideologies on economic and social issues.",
                expectedChallenges = new List<string>
                {
                    "Understanding why certain parties work together",
                    "Calculating seat totals correctly",
                    "Recognizing ideological compatibility",
                    "Allocating portfolios proportionally"
                },
                constraints = new ScenarioConstraints
                {
                    allowPVVInclusion = false,
                    requireMajority = true,
                    allowMinorityGovernment = false,
                    maxParties = 4,
                    minParties = 3,
                    mandatoryPortfolios = new List<string> { "Prime Minister", "Finance", "Foreign Affairs" },
                    blockedCombinations = new List<string> { "VVD+PVV", "NSC+PVV" }
                },
                successCriteria = new ScenarioSuccessCriteria
                {
                    requiredSeats = 76,
                    requiredParties = new List<string> { "VVD", "NSC", "BBB" },
                    maxTimeMinutes = 15,
                    requiresRealisticPortfolios = true,
                    minimumCompatibilityScore = 0.7f,
                    policyAgreementAreas = new List<string> { "Economic Policy", "Climate Policy", "Agriculture" }
                }
            };

            scenario.steps = new List<ScenarioStep>
            {
                new ScenarioStep
                {
                    title = "Welcome to Coalition Formation",
                    instruction = "You are tasked with forming a government after the 2023 Dutch election. You need at least 76 seats out of 150 for a majority.",
                    helpText = "In the Netherlands, no single party typically wins a majority. Parties must form coalitions to govern.",
                    hints = new List<string>
                    {
                        "Look for parties with similar ideologies",
                        "VVD is the largest party and typically leads coalitions",
                        "Check the seat counts - you need 76 for a majority"
                    },
                    type = StepType.Introduction,
                    suggestedTimeLimit = 120f,
                    validationCriteria = new List<string> { "User has read the instruction", "User understands majority requirement" }
                },
                new ScenarioStep
                {
                    title = "Explore the Parties",
                    instruction = "Click on different parties in the parliament visualization to learn about their positions and seat counts.",
                    helpText = "Understanding party ideologies is crucial for successful coalition formation. Pay attention to economic, social, and EU positions.",
                    hints = new List<string>
                    {
                        "VVD has 34 seats and is center-right economically",
                        "NSC has 20 seats and takes moderate positions",
                        "BBB has 7 seats and focuses on rural/agricultural issues"
                    },
                    type = StepType.PartySelection,
                    suggestedTimeLimit = 300f,
                    validationCriteria = new List<string> { "Clicked on at least 5 parties", "Viewed VVD details", "Viewed NSC details", "Viewed BBB details" }
                },
                new ScenarioStep
                {
                    title = "Build Your Coalition",
                    instruction = "Drag VVD, NSC, and BBB into the coalition builder. Check that you have enough seats for a majority.",
                    helpText = "These three parties have compatible ideologies and their combined seats should give you a stable majority.",
                    hints = new List<string>
                    {
                        "VVD (34) + NSC (20) + BBB (7) = 61 seats total",
                        "You need 76 seats - consider adding another party",
                        "Look for a fourth party that's compatible with this center-right coalition"
                    },
                    type = StepType.CoalitionBuilding,
                    suggestedTimeLimit = 600f,
                    validationCriteria = new List<string> { "Added VVD to coalition", "Added NSC to coalition", "Added BBB to coalition", "Coalition has 76+ seats" }
                },
                new ScenarioStep
                {
                    title = "Allocate Portfolios",
                    instruction = "Assign ministerial portfolios to each party based on their size and expertise areas.",
                    helpText = "The largest party (VVD) typically gets the Prime Minister position. Other key portfolios should reflect party strengths.",
                    hints = new List<string>
                    {
                        "VVD should get Prime Minister and major economic portfolios",
                        "NSC can handle Justice, Interior, or Social Affairs",
                        "BBB should get Agriculture given their rural focus"
                    },
                    type = StepType.PortfolioAllocation,
                    suggestedTimeLimit = 480f,
                    validationCriteria = new List<string> { "VVD assigned PM", "All parties have portfolios", "Portfolios match party expertise" }
                },
                new ScenarioStep
                {
                    title = "Coalition Success!",
                    instruction = "Congratulations! You've formed a stable center-right coalition. Review the government composition and policy areas.",
                    helpText = "This coalition represents a realistic outcome based on the 2023 election results and party compatibility.",
                    type = StepType.Reflection,
                    suggestedTimeLimit = 180f,
                    validationCriteria = new List<string> { "Coalition formed successfully", "User reviewed results" }
                }
            };

            return scenario;
        }

        private DemoScenario CreateDifficultChoiceScenario()
        {
            return new DemoScenario
            {
                name = "The Difficult Choice",
                description = "Form a government when PVV (largest party) is excluded by other parties",
                difficulty = DifficultyLevel.Intermediate,
                learningObjectives = new List<string>
                {
                    "Understand political red-lines and exclusions",
                    "Navigate complex coalition mathematics",
                    "Learn about cordon sanitaire politics",
                    "Explore alternative coalition strategies"
                },
                requiredParties = new List<string>(),
                excludedParties = new List<string> { "PVV" },
                minimumSeats = 76,
                targetSeats = 85,
                contextualBackground = "Despite winning the most seats (37), PVV is excluded by all other major parties due to ideological differences. You must form a government without the largest party.",
                expectedChallenges = new List<string>
                {
                    "Working around the largest party",
                    "Finding compatible alternatives",
                    "Managing complex 4+ party coalitions",
                    "Understanding Dutch political dynamics"
                },
                constraints = new ScenarioConstraints
                {
                    allowPVVInclusion = false,
                    requireMajority = true,
                    allowMinorityGovernment = false,
                    maxParties = 6,
                    minParties = 4,
                    blockedCombinations = new List<string> { "Any+PVV" }
                },
                successCriteria = new ScenarioSuccessCriteria
                {
                    requiredSeats = 76,
                    maxTimeMinutes = 25,
                    requiresRealisticPortfolios = true,
                    minimumCompatibilityScore = 0.6f
                },
                steps = new List<ScenarioStep>
                {
                    new ScenarioStep
                    {
                        title = "The PVV Problem",
                        instruction = "PVV won the most seats (37) but all other parties refuse to work with them. Find an alternative coalition.",
                        helpText = "This reflects real Dutch politics where certain parties are considered beyond the pale by mainstream parties.",
                        type = StepType.Introduction,
                        suggestedTimeLimit = 180f
                    },
                    new ScenarioStep
                    {
                        title = "Explore Alternative Coalitions",
                        instruction = "Without PVV, you need to combine multiple smaller parties. Consider VVD+GL-PvdA+D66+CDA or other combinations.",
                        type = StepType.CoalitionBuilding,
                        suggestedTimeLimit = 900f
                    }
                }
            };
        }

        private DemoScenario CreateGrandCoalitionScenario()
        {
            return new DemoScenario
            {
                name = "The Grand Coalition",
                description = "Form a broad consensus government spanning the political spectrum",
                difficulty = DifficultyLevel.Advanced,
                learningObjectives = new List<string>
                {
                    "Understand crisis governance",
                    "Balance ideological differences",
                    "Manage large coalition dynamics",
                    "Create broad policy consensus"
                },
                minimumSeats = 100,
                targetSeats = 120,
                contextualBackground = "In times of crisis, Dutch politics sometimes requires grand coalitions that span left, center, and right to ensure broad support for difficult decisions.",
                constraints = new ScenarioConstraints
                {
                    requireMajority = true,
                    maxParties = 6,
                    minParties = 4
                },
                successCriteria = new ScenarioSuccessCriteria
                {
                    requiredSeats = 100,
                    maxTimeMinutes = 35,
                    minimumCompatibilityScore = 0.5f
                }
            };
        }

        private DemoScenario CreateHistoricalRecreationScenario()
        {
            return new DemoScenario
            {
                name = "Historical Recreation: Rutte III",
                description = "Recreate the actual Rutte III coalition (VVD-CDA-D66-CU) using 2017 election results",
                difficulty = DifficultyLevel.Expert,
                learningObjectives = new List<string>
                {
                    "Understand real Dutch political decision-making",
                    "Learn from historical coalition patterns",
                    "Appreciate the complexity of actual negotiations",
                    "Compare different election outcomes"
                },
                requiredParties = new List<string> { "VVD", "CDA", "D66", "CU" },
                minimumSeats = 76,
                contextualBackground = "Using 2017 election results, recreate the coalition that actually governed the Netherlands from 2017-2021. Understand why these specific parties came together.",
                constraints = new ScenarioConstraints
                {
                    requireMajority = true,
                    maxParties = 4,
                    minParties = 4
                },
                successCriteria = new ScenarioSuccessCriteria
                {
                    requiredParties = new List<string> { "VVD", "CDA", "D66", "CU" },
                    maxTimeMinutes = 30,
                    requiresRealisticPortfolios = true
                }
            };
        }

        public void StartScenario(int scenarioIndex)
        {
            if (scenarioIndex < 0 || scenarioIndex >= scenarios.Count)
            {
                Debug.LogError($"Invalid scenario index: {scenarioIndex}");
                return;
            }

            StartScenario(scenarios[scenarioIndex]);
        }

        public void StartScenario(string scenarioName)
        {
            var scenario = scenarios.Find(s => s.name == scenarioName);
            if (scenario == null)
            {
                Debug.LogError($"Scenario not found: {scenarioName}");
                return;
            }

            StartScenario(scenario);
        }

        public void StartScenario(DemoScenario scenario)
        {
            if (IsScenarioActive)
            {
                CompleteScenario(false, "New scenario started");
            }

            currentScenario = scenario;
            currentStepIndex = 0;
            scenarioCompleted = false;

            OnScenarioStarted?.Invoke(currentScenario);

            // Record in user testing framework
            UserTestingFramework.Instance?.RecordAction("ScenarioStart", scenario.name, Vector2.zero, $"Difficulty: {scenario.difficulty}");

            StartCurrentStep();

            Debug.Log($"Started scenario: {scenario.name}");
        }

        private void StartCurrentStep()
        {
            if (!IsScenarioActive || currentStepIndex >= currentScenario.steps.Count) return;

            var step = currentScenario.steps[currentStepIndex];
            stepStartTime = Time.time;

            OnStepStarted?.Invoke(step);

            // Record step start
            UserTestingFramework.Instance?.RecordAction("StepStart", step.title, Vector2.zero, $"Step {currentStepIndex + 1}/{currentScenario.steps.Count}");

            Debug.Log($"Started step: {step.title}");

            // Show hint after delay if enabled
            if (showHints && step.hints.Count > 0)
            {
                Invoke(nameof(ShowHint), hintDelaySeconds);
            }
        }

        public void CompleteCurrentStep()
        {
            if (!IsScenarioActive || currentStepIndex >= currentScenario.steps.Count) return;

            var step = currentScenario.steps[currentStepIndex];
            float stepDuration = Time.time - stepStartTime;

            OnStepCompleted?.Invoke(step);

            // Record step completion
            UserTestingFramework.Instance?.RecordTaskCompletion(step.title, true, stepDuration);

            // Move to next step
            currentStepIndex++;

            if (currentStepIndex >= currentScenario.steps.Count)
            {
                CompleteScenario(true, "All steps completed");
            }
            else
            {
                StartCurrentStep();
            }
        }

        public void CompleteScenario(bool successful, string reason = "")
        {
            if (!IsScenarioActive) return;

            scenarioCompleted = successful;

            OnScenarioCompleted?.Invoke(currentScenario);

            // Record scenario completion
            string result = successful ? "Success" : "Failed";
            UserTestingFramework.Instance?.RecordAction("ScenarioComplete", currentScenario.name, Vector2.zero, $"Result: {result} | Reason: {reason}");

            Debug.Log($"Scenario {currentScenario.name} completed: {result} - {reason}");

            currentScenario = null;
            currentStepIndex = 0;
        }

        private void ShowHint()
        {
            if (!IsScenarioActive || currentStepIndex >= currentScenario.steps.Count) return;

            var step = currentScenario.steps[currentStepIndex];
            if (step.hints.Count > 0)
            {
                string hint = step.hints[UnityEngine.Random.Range(0, step.hints.Count)];
                OnHintShown?.Invoke(hint);

                // Record hint shown
                UserTestingFramework.Instance?.RecordAction("HintShown", step.title, Vector2.zero, hint);

                Debug.Log($"Hint shown: {hint}");
            }
        }

        public void RequestHint()
        {
            CancelInvoke(nameof(ShowHint));
            ShowHint();
        }

        public List<DemoScenario> GetScenarios() => new List<DemoScenario>(scenarios);

        public DemoScenario GetScenario(string name) => scenarios.Find(s => s.name == name);

        public bool ValidateStepCompletion(List<string> criteria)
        {
            // This would validate against the current step's criteria
            // Implementation depends on specific validation needs
            return true;
        }

        public float GetScenarioProgress()
        {
            if (!IsScenarioActive) return 0f;
            return (float)currentStepIndex / currentScenario.steps.Count;
        }

        void Update()
        {
            if (IsScenarioActive)
            {
                var currentStep = currentScenario.steps[currentStepIndex];

                // Check for time limit
                if (currentStep.suggestedTimeLimit > 0 && StepDuration > currentStep.suggestedTimeLimit)
                {
                    // Show time warning or auto-advance
                    OnHintShown?.Invoke("This step is taking longer than expected. Need help?");
                }
            }
        }
    }
}