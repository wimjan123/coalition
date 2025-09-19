using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Coalition.Runtime.Core;
using Coalition.Runtime.Data;

namespace Coalition.Runtime.UI
{
    /// <summary>
    /// Interactive coalition builder interface with drag-and-drop party selection.
    /// Provides real-time seat counting, majority indicator, and compatibility analysis.
    /// </summary>
    public class CoalitionBuilder : UIPanel, IDropHandler
    {
        [Header("Coalition Builder Components")]
        [SerializeField] private RectTransform availablePartiesContainer;
        [SerializeField] private RectTransform coalitionContainer;
        [SerializeField] private RectTransform dropZone;

        [Header("Information Display")]
        [SerializeField] private Text totalSeatsText;
        [SerializeField] private Text majorityIndicatorText;
        [SerializeField] private Image majorityStatusImage;
        [SerializeField] private Slider compatibilitySlider;
        [SerializeField] private Text compatibilityText;

        [Header("Controls")]
        [SerializeField] private Button clearCoalitionButton;
        [SerializeField] private Button analyzeButton;
        [SerializeField] private Button formGovernmentButton;
        [SerializeField] private Toggle autoAnalysisToggle;

        [Header("Visual Feedback")]
        [SerializeField] private GameObject partyCardPrefab;
        [SerializeField] private Color validDropColor = Color.green;
        [SerializeField] private Color invalidDropColor = Color.red;
        [SerializeField] private Color majorityColor = Color.green;
        [SerializeField] private Color minorityColor = Color.red;

        [Header("Coalition Analysis")]
        [SerializeField] private bool enableCompatibilityAnalysis = true;
        [SerializeField] private bool showRedLineWarnings = true;
        [SerializeField] private float updateDelay = 0.5f;

        // Coalition data
        private List<PartyCard> availablePartyCards;
        private List<PartyCard> coalitionPartyCards;
        private ElectionResult currentElectionResult;
        private Coalition currentCoalition;

        // Coalition analysis
        private float lastUpdateTime;
        private bool isDirty = true;
        private CoalitionCompatibilityAnalyzer compatibilityAnalyzer;

        // Red line tracking
        private Dictionary<string, List<string>> partyRedLines;
        private List<string> currentWarnings;

        #region Properties

        public Coalition CurrentCoalition => currentCoalition;
        public int TotalCoalitionSeats { get; private set; }
        public bool HasMajority => TotalCoalitionSeats >= 76;
        public float CompatibilityScore { get; private set; }

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            InitializeCoalitionBuilder();
        }

        protected override void Start()
        {
            base.Start();
            SetupEventHandlers();
            InitializeEventSubscriptions();
        }

        protected override void Update()
        {
            base.Update();
            HandleDelayedUpdates();
        }

        protected override void OnDestroy()
        {
            CleanupEventSubscriptions();
            base.OnDestroy();
        }

        #endregion

        #region Initialization

        private void InitializeCoalitionBuilder()
        {
            availablePartyCards = new List<PartyCard>();
            coalitionPartyCards = new List<PartyCard>();
            partyRedLines = new Dictionary<string, List<string>>();
            currentWarnings = new List<string>();

            // Initialize compatibility analyzer
            compatibilityAnalyzer = new CoalitionCompatibilityAnalyzer();
            LoadPartyRedLines();

            Debug.Log("[CoalitionBuilder] Coalition builder initialized");
        }

        private void SetupEventHandlers()
        {
            if (clearCoalitionButton != null)
                clearCoalitionButton.onClick.AddListener(ClearCoalition);

            if (analyzeButton != null)
                analyzeButton.onClick.AddListener(AnalyzeCoalition);

            if (formGovernmentButton != null)
                formGovernmentButton.onClick.AddListener(FormGovernment);

            if (autoAnalysisToggle != null)
                autoAnalysisToggle.onValueChanged.AddListener(OnAutoAnalysisToggled);
        }

        private void InitializeEventSubscriptions()
        {
            EventBus.Subscribe<ElectionResultsUpdatedEvent>(OnElectionResultsUpdated);
            EventBus.Subscribe<PartyDataUpdatedEvent>(OnPartyDataUpdated);
        }

        private void CleanupEventSubscriptions()
        {
            EventBus.Unsubscribe<ElectionResultsUpdatedEvent>(OnElectionResultsUpdated);
            EventBus.Unsubscribe<PartyDataUpdatedEvent>(OnPartyDataUpdated);
        }

        private void LoadPartyRedLines()
        {
            // Initialize Dutch political party red lines (incompatibilities)
            partyRedLines = new Dictionary<string, List<string>>
            {
                ["PVV"] = new List<string> { "GL-PvdA", "D66", "Volt" },
                ["GL-PvdA"] = new List<string> { "PVV", "FvD", "JA21" },
                ["D66"] = new List<string> { "PVV", "FvD", "SGP" },
                ["FvD"] = new List<string> { "GL-PvdA", "D66", "Volt", "CU" },
                ["SP"] = new List<string> { "VVD", "CDA" },
                ["SGP"] = new List<string> { "D66", "Volt" },
                ["Volt"] = new List<string> { "PVV", "FvD", "SGP" }
            };
        }

        #endregion

        #region Election Data Integration

        public void SetElectionResult(ElectionResult electionResult)
        {
            currentElectionResult = electionResult;
            RefreshAvailableParties();
            isDirty = true;

            Debug.Log($"[CoalitionBuilder] Updated with election result: {electionResult?.PartyResults?.Count ?? 0} parties");
        }

        private void RefreshAvailableParties()
        {
            if (currentElectionResult == null || availablePartiesContainer == null) return;

            // Clear existing party cards
            ClearPartyCards();

            // Create new party cards from election results
            foreach (var partyResult in currentElectionResult.PartyResults.OrderByDescending(p => p.Seats))
            {
                CreatePartyCard(partyResult, availablePartiesContainer);
            }
        }

        private void ClearPartyCards()
        {
            foreach (var card in availablePartyCards)
            {
                if (card != null && card.gameObject != null)
                    Destroy(card.gameObject);
            }

            foreach (var card in coalitionPartyCards)
            {
                if (card != null && card.gameObject != null)
                    Destroy(card.gameObject);
            }

            availablePartyCards.Clear();
            coalitionPartyCards.Clear();
        }

        private void CreatePartyCard(PartyResult partyResult, RectTransform container)
        {
            if (partyCardPrefab == null) return;

            var cardObject = Instantiate(partyCardPrefab, container);
            var partyCard = cardObject.GetComponent<PartyCard>();

            if (partyCard == null)
            {
                partyCard = cardObject.AddComponent<PartyCard>();
            }

            partyCard.Initialize(partyResult, this);

            if (container == availablePartiesContainer)
            {
                availablePartyCards.Add(partyCard);
            }
            else
            {
                coalitionPartyCards.Add(partyCard);
            }
        }

        #endregion

        #region Drag and Drop

        public void OnDrop(PointerEventData eventData)
        {
            var draggedCard = eventData.pointerDrag?.GetComponent<PartyCard>();
            if (draggedCard == null) return;

            // Determine if this is a valid drop
            var dropArea = GetDropArea(eventData.position);

            switch (dropArea)
            {
                case DropArea.Coalition:
                    AddPartyToCoalition(draggedCard);
                    break;
                case DropArea.Available:
                    RemovePartyFromCoalition(draggedCard);
                    break;
                default:
                    draggedCard.ReturnToOriginalPosition();
                    break;
            }
        }

        private DropArea GetDropArea(Vector2 screenPosition)
        {
            // Check if dropped in coalition area
            if (RectTransformUtility.RectangleContainsScreenPoint(coalitionContainer, screenPosition))
                return DropArea.Coalition;

            // Check if dropped in available parties area
            if (RectTransformUtility.RectangleContainsScreenPoint(availablePartiesContainer, screenPosition))
                return DropArea.Available;

            return DropArea.Invalid;
        }

        public void AddPartyToCoalition(PartyCard partyCard)
        {
            if (partyCard == null || coalitionPartyCards.Contains(partyCard)) return;

            // Check for red line violations
            if (showRedLineWarnings && HasRedLineViolation(partyCard.PartyName))
            {
                ShowRedLineWarning(partyCard.PartyName);
                partyCard.ReturnToOriginalPosition();
                return;
            }

            // Move card to coalition
            availablePartyCards.Remove(partyCard);
            coalitionPartyCards.Add(partyCard);
            partyCard.transform.SetParent(coalitionContainer, false);

            UpdateCoalitionAnalysis();
            isDirty = true;

            Debug.Log($"[CoalitionBuilder] Added {partyCard.PartyName} to coalition");
        }

        public void RemovePartyFromCoalition(PartyCard partyCard)
        {
            if (partyCard == null || !coalitionPartyCards.Contains(partyCard)) return;

            // Move card back to available
            coalitionPartyCards.Remove(partyCard);
            availablePartyCards.Add(partyCard);
            partyCard.transform.SetParent(availablePartiesContainer, false);

            UpdateCoalitionAnalysis();
            isDirty = true;

            Debug.Log($"[CoalitionBuilder] Removed {partyCard.PartyName} from coalition");
        }

        #endregion

        #region Coalition Analysis

        private void UpdateCoalitionAnalysis()
        {
            // Calculate total seats
            TotalCoalitionSeats = coalitionPartyCards.Sum(card => card.SeatCount);

            // Update UI
            UpdateSeatDisplay();
            UpdateMajorityIndicator();

            if (enableCompatibilityAnalysis)
            {
                CalculateCompatibility();
                UpdateCompatibilityDisplay();
            }

            // Update current coalition object
            UpdateCurrentCoalition();

            // Publish coalition change event
            EventBus.Publish(new CoalitionChangedEvent
            {
                Coalition = currentCoalition,
                TotalSeats = TotalCoalitionSeats,
                HasMajority = HasMajority
            });
        }

        private void UpdateSeatDisplay()
        {
            if (totalSeatsText != null)
            {
                totalSeatsText.text = $"{TotalCoalitionSeats}/150 seats";
            }
        }

        private void UpdateMajorityIndicator()
        {
            var hasMajority = HasMajority;

            if (majorityIndicatorText != null)
            {
                majorityIndicatorText.text = hasMajority ? "MAJORITY" : "MINORITY";
                majorityIndicatorText.color = hasMajority ? majorityColor : minorityColor;
            }

            if (majorityStatusImage != null)
            {
                majorityStatusImage.color = hasMajority ? majorityColor : minorityColor;
            }

            // Enable/disable form government button based on majority
            if (formGovernmentButton != null)
            {
                formGovernmentButton.interactable = hasMajority;
            }
        }

        private void CalculateCompatibility()
        {
            if (coalitionPartyCards.Count < 2)
            {
                CompatibilityScore = 1f;
                return;
            }

            var partyNames = coalitionPartyCards.Select(card => card.PartyName).ToList();
            CompatibilityScore = compatibilityAnalyzer.CalculateCompatibility(partyNames);
        }

        private void UpdateCompatibilityDisplay()
        {
            if (compatibilitySlider != null)
            {
                compatibilitySlider.value = CompatibilityScore;
            }

            if (compatibilityText != null)
            {
                var percentage = Mathf.RoundToInt(CompatibilityScore * 100);
                compatibilityText.text = $"Compatibility: {percentage}%";

                // Color based on compatibility level
                if (CompatibilityScore >= 0.8f)
                    compatibilityText.color = Color.green;
                else if (CompatibilityScore >= 0.6f)
                    compatibilityText.color = Color.yellow;
                else
                    compatibilityText.color = Color.red;
            }
        }

        private void UpdateCurrentCoalition()
        {
            var partyNames = coalitionPartyCards.Select(card => card.PartyName).ToList();

            currentCoalition = new Coalition
            {
                PartyNames = partyNames,
                TotalSeats = TotalCoalitionSeats,
                CompatibilityScore = CompatibilityScore,
                IsViable = HasMajority && CompatibilityScore >= 0.5f
            };
        }

        #endregion

        #region Red Line Violations

        private bool HasRedLineViolation(string newPartyName)
        {
            if (!partyRedLines.ContainsKey(newPartyName)) return false;

            var incompatibleParties = partyRedLines[newPartyName];
            var coalitionPartyNames = coalitionPartyCards.Select(card => card.PartyName);

            return incompatibleParties.Any(incompatible => coalitionPartyNames.Contains(incompatible));
        }

        private void ShowRedLineWarning(string partyName)
        {
            var incompatibleParties = partyRedLines.GetValueOrDefault(partyName, new List<string>());
            var conflictingParties = incompatibleParties.Where(party =>
                coalitionPartyCards.Any(card => card.PartyName == party)).ToList();

            if (conflictingParties.Any())
            {
                var warningMessage = $"{partyName} is incompatible with: {string.Join(", ", conflictingParties)}";

                // Show warning UI (could be a popup, notification, etc.)
                Debug.LogWarning($"[CoalitionBuilder] Red line violation: {warningMessage}");

                // Publish warning event
                EventBus.Publish(new CoalitionRedLineWarningEvent
                {
                    PartyName = partyName,
                    ConflictingParties = conflictingParties,
                    Message = warningMessage
                });
            }
        }

        #endregion

        #region UI Event Handlers

        private void ClearCoalition()
        {
            // Move all coalition parties back to available
            var partiesToMove = new List<PartyCard>(coalitionPartyCards);
            foreach (var card in partiesToMove)
            {
                RemovePartyFromCoalition(card);
            }

            Debug.Log("[CoalitionBuilder] Cleared coalition");
        }

        private void AnalyzeCoalition()
        {
            if (coalitionPartyCards.Count == 0) return;

            // Perform detailed analysis
            var analysis = PerformDetailedAnalysis();

            // Show analysis results
            ShowAnalysisResults(analysis);

            Debug.Log("[CoalitionBuilder] Performed coalition analysis");
        }

        private void FormGovernment()
        {
            if (!HasMajority)
            {
                Debug.LogWarning("[CoalitionBuilder] Cannot form government without majority");
                return;
            }

            // Publish government formation event
            EventBus.Publish(new GovernmentFormedEvent
            {
                Coalition = currentCoalition,
                FormationDate = System.DateTime.Now
            });

            Debug.Log($"[CoalitionBuilder] Formed government with {currentCoalition.PartyNames.Count} parties");
        }

        private void OnAutoAnalysisToggled(bool enabled)
        {
            enableCompatibilityAnalysis = enabled;

            if (enabled)
            {
                UpdateCoalitionAnalysis();
            }
        }

        #endregion

        #region Analysis

        private CoalitionAnalysisResult PerformDetailedAnalysis()
        {
            var partyNames = coalitionPartyCards.Select(card => card.PartyName).ToList();

            return new CoalitionAnalysisResult
            {
                Parties = partyNames,
                TotalSeats = TotalCoalitionSeats,
                HasMajority = HasMajority,
                CompatibilityScore = CompatibilityScore,
                RedLineViolations = GetRedLineViolations(),
                PolicyAgreement = CalculatePolicyAgreement(),
                StabilityFactor = CalculateStabilityFactor()
            };
        }

        private List<string> GetRedLineViolations()
        {
            var violations = new List<string>();
            var partyNames = coalitionPartyCards.Select(card => card.PartyName).ToList();

            foreach (var party in partyNames)
            {
                if (partyRedLines.ContainsKey(party))
                {
                    var incompatibleParties = partyRedLines[party];
                    var conflicts = incompatibleParties.Where(incompatible => partyNames.Contains(incompatible));

                    foreach (var conflict in conflicts)
                    {
                        violations.Add($"{party} ↔ {conflict}");
                    }
                }
            }

            return violations.Distinct().ToList();
        }

        private float CalculatePolicyAgreement()
        {
            // Simplified policy agreement calculation
            // In a real implementation, this would analyze party programs
            return Mathf.Max(0.3f, CompatibilityScore * UnityEngine.Random.Range(0.8f, 1.2f));
        }

        private float CalculateStabilityFactor()
        {
            // Factor in party size distribution and historical stability
            var seatCounts = coalitionPartyCards.Select(card => card.SeatCount).ToList();
            var maxSeats = seatCounts.Max();
            var minSeats = seatCounts.Min();
            var seatDistribution = minSeats / (float)maxSeats;

            return (CompatibilityScore + seatDistribution) * 0.5f;
        }

        private void ShowAnalysisResults(CoalitionAnalysisResult analysis)
        {
            // This could open a detailed analysis window
            Debug.Log($"Coalition Analysis:\n" +
                     $"  Parties: {string.Join(", ", analysis.Parties)}\n" +
                     $"  Seats: {analysis.TotalSeats}/150\n" +
                     $"  Majority: {analysis.HasMajority}\n" +
                     $"  Compatibility: {analysis.CompatibilityScore:F2}\n" +
                     $"  Policy Agreement: {analysis.PolicyAgreement:F2}\n" +
                     $"  Stability: {analysis.StabilityFactor:F2}\n" +
                     $"  Violations: {analysis.RedLineViolations.Count}");
        }

        #endregion

        #region Update Management

        private void HandleDelayedUpdates()
        {
            if (!isDirty || Time.time - lastUpdateTime < updateDelay) return;

            lastUpdateTime = Time.time;
            isDirty = false;

            // Perform any delayed updates here
            if (autoAnalysisToggle != null && autoAnalysisToggle.isOn)
            {
                UpdateCoalitionAnalysis();
            }
        }

        #endregion

        #region Event Handlers

        private void OnElectionResultsUpdated(ElectionResultsUpdatedEvent eventData)
        {
            SetElectionResult(eventData.ElectionResult);
        }

        private void OnPartyDataUpdated(PartyDataUpdatedEvent eventData)
        {
            // Refresh party cards when party data changes
            RefreshAvailableParties();
        }

        #endregion

        #region Theme Support

        public override void ApplyTheme(UITheme theme)
        {
            base.ApplyTheme(theme);

            if (theme == null) return;

            // Update text components
            var textComponents = new[] { totalSeatsText, majorityIndicatorText, compatibilityText };
            foreach (var text in textComponents)
            {
                if (text != null)
                {
                    text.color = theme.TextColor;
                    if (theme.Font != null)
                        text.font = theme.Font;
                }
            }

            // Update button colors
            var buttons = new[] { clearCoalitionButton, analyzeButton, formGovernmentButton };
            foreach (var button in buttons)
            {
                if (button != null)
                {
                    var colors = button.colors;
                    colors.normalColor = theme.ButtonNormalColor;
                    colors.highlightedColor = theme.ButtonHighlightColor;
                    colors.pressedColor = theme.ButtonPressedColor;
                    colors.disabledColor = theme.ButtonDisabledColor;
                    button.colors = colors;
                }
            }

            // Update party cards
            foreach (var card in availablePartyCards.Concat(coalitionPartyCards))
            {
                if (card != null)
                    card.ApplyTheme(theme);
            }
        }

        #endregion

#if UNITY_EDITOR
        [ContextMenu("Debug Coalition Status")]
        private void DebugCoalitionStatus()
        {
            Debug.Log($"Coalition Status:\n" +
                     $"  Parties: {coalitionPartyCards.Count}\n" +
                     $"  Total Seats: {TotalCoalitionSeats}/150\n" +
                     $"  Has Majority: {HasMajority}\n" +
                     $"  Compatibility: {CompatibilityScore:F2}\n" +
                     $"  Available Cards: {availablePartyCards.Count}");
        }

        [ContextMenu("Test Clear Coalition")]
        private void TestClearCoalition()
        {
            ClearCoalition();
        }

        [ContextMenu("Test Form Government")]
        private void TestFormGovernment()
        {
            FormGovernment();
        }
#endif
    }

    /// <summary>
    /// Drop area enumeration for drag and drop operations.
    /// </summary>
    public enum DropArea
    {
        Invalid,
        Available,
        Coalition
    }

    /// <summary>
    /// Coalition analysis result data structure.
    /// </summary>
    [System.Serializable]
    public struct CoalitionAnalysisResult
    {
        public List<string> Parties;
        public int TotalSeats;
        public bool HasMajority;
        public float CompatibilityScore;
        public List<string> RedLineViolations;
        public float PolicyAgreement;
        public float StabilityFactor;
    }

    /// <summary>
    /// Event published when coalition composition changes.
    /// </summary>
    public class CoalitionChangedEvent
    {
        public Coalition Coalition { get; set; }
        public int TotalSeats { get; set; }
        public bool HasMajority { get; set; }
        public System.DateTime Timestamp { get; } = System.DateTime.Now;
    }

    /// <summary>
    /// Event published when a red line violation is detected.
    /// </summary>
    public class CoalitionRedLineWarningEvent
    {
        public string PartyName { get; set; }
        public List<string> ConflictingParties { get; set; }
        public string Message { get; set; }
        public System.DateTime Timestamp { get; } = System.DateTime.Now;
    }

    /// <summary>
    /// Event published when a government is formed.
    /// </summary>
    public class GovernmentFormedEvent
    {
        public Coalition Coalition { get; set; }
        public System.DateTime FormationDate { get; set; }
        public System.DateTime Timestamp { get; } = System.DateTime.Now;
    }

    /// <summary>
    /// Simple coalition compatibility analyzer.
    /// </summary>
    public class CoalitionCompatibilityAnalyzer
    {
        private readonly Dictionary<string, Dictionary<string, float>> compatibilityMatrix;

        public CoalitionCompatibilityAnalyzer()
        {
            // Initialize compatibility matrix (simplified example)
            compatibilityMatrix = InitializeCompatibilityMatrix();
        }

        public float CalculateCompatibility(List<string> parties)
        {
            if (parties.Count < 2) return 1f;

            var totalCompatibility = 0f;
            var pairCount = 0;

            for (int i = 0; i < parties.Count; i++)
            {
                for (int j = i + 1; j < parties.Count; j++)
                {
                    totalCompatibility += GetPairCompatibility(parties[i], parties[j]);
                    pairCount++;
                }
            }

            return pairCount > 0 ? totalCompatibility / pairCount : 0f;
        }

        private float GetPairCompatibility(string party1, string party2)
        {
            if (compatibilityMatrix.ContainsKey(party1) &&
                compatibilityMatrix[party1].ContainsKey(party2))
            {
                return compatibilityMatrix[party1][party2];
            }

            if (compatibilityMatrix.ContainsKey(party2) &&
                compatibilityMatrix[party2].ContainsKey(party1))
            {
                return compatibilityMatrix[party2][party1];
            }

            return 0.5f; // Default neutral compatibility
        }

        private Dictionary<string, Dictionary<string, float>> InitializeCompatibilityMatrix()
        {
            // Simplified compatibility matrix based on Dutch political landscape
            return new Dictionary<string, Dictionary<string, float>>
            {
                ["VVD"] = new Dictionary<string, float>
                {
                    ["D66"] = 0.8f, ["CDA"] = 0.9f, ["NSC"] = 0.85f, ["BBB"] = 0.7f,
                    ["PVV"] = 0.3f, ["GL-PvdA"] = 0.2f, ["SP"] = 0.1f
                },
                ["PVV"] = new Dictionary<string, float>
                {
                    ["BBB"] = 0.6f, ["FvD"] = 0.7f, ["JA21"] = 0.8f,
                    ["VVD"] = 0.3f, ["GL-PvdA"] = 0.0f, ["D66"] = 0.0f
                },
                ["NSC"] = new Dictionary<string, float>
                {
                    ["VVD"] = 0.85f, ["CDA"] = 0.8f, ["D66"] = 0.7f, ["BBB"] = 0.75f
                },
                ["GL-PvdA"] = new Dictionary<string, float>
                {
                    ["D66"] = 0.9f, ["SP"] = 0.7f, ["Volt"] = 0.85f, ["CU"] = 0.6f,
                    ["PVV"] = 0.0f, ["FvD"] = 0.0f
                },
                ["D66"] = new Dictionary<string, float>
                {
                    ["VVD"] = 0.8f, ["GL-PvdA"] = 0.9f, ["Volt"] = 0.95f, ["CU"] = 0.6f,
                    ["PVV"] = 0.0f, ["SGP"] = 0.2f
                }
            };
        }
    }
}