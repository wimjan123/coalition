using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Coalition.Runtime.Core;
using Coalition.Runtime.Data;

namespace Coalition.Political.Coalitions
{
    /// <summary>
    /// Manages coalition formation UI integration and real-time compatibility feedback
    /// Integrates with Unity EventBus for seamless UI updates and user interaction
    /// Performance target: Real-time updates <100ms, full analysis <5 seconds
    /// </summary>
    public class CoalitionFormationManager : MonoBehaviour
    {
        [Header("Coalition Formation Settings")]
        [SerializeField] private bool enableRealTimeUpdates = true;
        [SerializeField] private float updateThrottleMs = 100f; // Minimum time between updates
        [SerializeField] private bool showRedLineWarnings = true;
        [SerializeField] private bool enablePerformanceMonitoring = true;

        [Header("UI Feedback Configuration")]
        [SerializeField] private bool enableVisualFeedback = true;
        [SerializeField] private bool enableCompatibilityMeter = true;
        [SerializeField] private bool enableSeatCounter = true;
        [SerializeField] private bool enableRedLineAlerts = true;

        // Private fields
        private List<string> _selectedParties = new List<string>();
        private List<ElectionResult> _currentElectionResults;
        private CoalitionFormationSystem.CoalitionAnalysis _lastAnalysis;
        private DateTime _lastUpdateTime = DateTime.MinValue;
        private bool _isAnalyzing = false;

        // Performance monitoring
        private readonly System.Diagnostics.Stopwatch _performanceTimer = new System.Diagnostics.Stopwatch();
        private readonly List<float> _updateTimes = new List<float>();

        #region Unity Lifecycle

        private void Start()
        {
            InitializeCoalitionManager();
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        #endregion

        #region Initialization

        private void InitializeCoalitionManager()
        {
            Debug.Log("Coalition Formation Manager initialized");

            // Validate Dutch political data
            if (DutchPoliticalDataGenerator.ValidateGeneratedData())
            {
                Debug.Log("Dutch political data validation: PASSED");
            }
            else
            {
                Debug.LogWarning("Dutch political data validation: FAILED - Coalition calculations may be inaccurate");
            }

            // Initialize with current election results if available
            LoadCurrentElectionResults();
        }

        private void LoadCurrentElectionResults()
        {
            // Try to get current election results from D'Hondt system
            var votes = DHondtElectionSystem.Get2023ElectionVotes();
            var calculatedResults = DHondtElectionSystem.CalculateSeats(votes);

            if (calculatedResults != null && calculatedResults.Count > 0)
            {
                _currentElectionResults = calculatedResults;
                Debug.Log($"Loaded {calculatedResults.Count} party election results for coalition analysis");
            }
        }

        #endregion

        #region Event Handling

        private void SubscribeToEvents()
        {
            EventBus.Subscribe<PartySelectedEvent>(OnPartySelected);
            EventBus.Subscribe<ElectionCalculatedEvent>(OnElectionCalculated);
            EventBus.Subscribe<CoalitionScenarioRequestedEvent>(OnCoalitionScenarioRequested);
        }

        private void UnsubscribeFromEvents()
        {
            EventBus.Unsubscribe<PartySelectedEvent>(OnPartySelected);
            EventBus.Unsubscribe<ElectionCalculatedEvent>(OnElectionCalculated);
            EventBus.Unsubscribe<CoalitionScenarioRequestedEvent>(OnCoalitionScenarioRequested);
        }

        private void OnPartySelected(PartySelectedEvent eventData)
        {
            HandlePartySelection(eventData.PartyName);
        }

        private void OnElectionCalculated(ElectionCalculatedEvent eventData)
        {
            UpdateElectionResults(eventData.Result);
        }

        private void OnCoalitionScenarioRequested(CoalitionScenarioRequestedEvent eventData)
        {
            LoadCoalitionScenario(eventData.ScenarioName, eventData.PartyAbbreviations);
        }

        #endregion

        #region Party Selection Management

        /// <summary>
        /// Handle party selection/deselection for coalition building
        /// Provides real-time compatibility feedback
        /// </summary>
        public void HandlePartySelection(string partyAbbreviation)
        {
            if (string.IsNullOrEmpty(partyAbbreviation))
                return;

            _performanceTimer.Restart();

            bool wasSelected = _selectedParties.Contains(partyAbbreviation);

            if (wasSelected)
            {
                // Deselect party
                _selectedParties.Remove(partyAbbreviation);
                Debug.Log($"Party deselected: {partyAbbreviation}");

                EventBus.Publish(new CoalitionFormationEvent(
                    new List<string>(_selectedParties),
                    CoalitionFormationState.PartyRemoved,
                    $"Removed {partyAbbreviation} from coalition"));
            }
            else
            {
                // Select party
                _selectedParties.Add(partyAbbreviation);
                Debug.Log($"Party selected: {partyAbbreviation}");

                EventBus.Publish(new CoalitionFormationEvent(
                    new List<string>(_selectedParties),
                    CoalitionFormationState.PartyAdded,
                    $"Added {partyAbbreviation} to coalition"));
            }

            // Update coalition compatibility in real-time
            if (enableRealTimeUpdates && ShouldUpdateNow())
            {
                UpdateCoalitionCompatibility();
            }

            // Highlight selected parties in parliament visualization
            EventBus.Publish(new HighlightCoalitionEvent(new List<string>(_selectedParties)));

            RecordPerformanceMetric();
        }

        /// <summary>
        /// Clear all selected parties and reset coalition
        /// </summary>
        public void ClearCoalition()
        {
            _selectedParties.Clear();

            EventBus.Publish(new CoalitionFormationEvent(
                new List<string>(),
                CoalitionFormationState.Started,
                "Coalition cleared"));

            EventBus.Publish(new HighlightCoalitionEvent(new List<string>(), true));

            // Clear compatibility display
            EventBus.Publish(new CoalitionCompatibilityUpdatedEvent(
                new List<string>(), 0f, 0, false, new List<string>()));
        }

        #endregion

        #region Real-time Compatibility Calculation

        /// <summary>
        /// Calculate and broadcast real-time compatibility updates
        /// Optimized for <100ms response time
        /// </summary>
        private void UpdateCoalitionCompatibility()
        {
            if (_selectedParties.Count < 2)
            {
                // Not enough parties for coalition
                EventBus.Publish(new CoalitionCompatibilityUpdatedEvent(
                    new List<string>(_selectedParties), 0f, GetTotalSeats(), false, new List<string>()));
                return;
            }

            _performanceTimer.Restart();

            try
            {
                // Get selected political parties
                var allParties = DutchPoliticalDataGenerator.GenerateAllDutchParties();
                var selectedPoliticalParties = _selectedParties
                    .Select(abbrev => allParties.FirstOrDefault(p => p.Abbreviation == abbrev))
                    .Where(p => p != null)
                    .ToList();

                if (selectedPoliticalParties.Count != _selectedParties.Count)
                {
                    Debug.LogWarning("Some selected parties not found in political data");
                }

                // Calculate compatibility metrics
                float compatibilityScore = CoalitionFormationSystem.CalculateCompatibility(selectedPoliticalParties);
                int totalSeats = GetTotalSeats();
                bool isViable = totalSeats >= 75; // Majority threshold

                // Check for red-line violations
                var redLineViolations = FindRedLineViolations(selectedPoliticalParties);

                // Publish real-time update
                EventBus.Publish(new CoalitionCompatibilityUpdatedEvent(
                    new List<string>(_selectedParties),
                    compatibilityScore,
                    totalSeats,
                    isViable,
                    redLineViolations));

                // Publish red-line warnings if enabled
                if (showRedLineWarnings && redLineViolations.Count > 0)
                {
                    foreach (var violation in redLineViolations)
                    {
                        var parts = violation.Split(' ');
                        if (parts.Length >= 3)
                        {
                            EventBus.Publish(new RedLineViolationDetectedEvent(
                                parts[0], parts[2], violation, 0.5f));
                        }
                    }
                }

                _lastUpdateTime = DateTime.Now;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error calculating coalition compatibility: {ex.Message}");
            }

            RecordPerformanceMetric();
        }

        private List<string> FindRedLineViolations(List<PoliticalParty> parties)
        {
            var violations = new List<string>();

            foreach (var party in parties)
            {
                foreach (var otherParty in parties)
                {
                    if (party == otherParty) continue;

                    if (party.ExcludedCoalitionPartners.Contains(otherParty.Abbreviation))
                    {
                        violations.Add($"{party.Abbreviation} excludes {otherParty.Abbreviation}");
                    }
                }
            }

            return violations.Distinct().ToList();
        }

        private int GetTotalSeats()
        {
            if (_currentElectionResults == null)
                return 0;

            int totalSeats = 0;
            foreach (var partyAbbrev in _selectedParties)
            {
                var result = _currentElectionResults.FirstOrDefault(r => r.abbreviation == partyAbbrev);
                totalSeats += result?.seats ?? 0;
            }

            return totalSeats;
        }

        #endregion

        #region Full Coalition Analysis

        /// <summary>
        /// Perform comprehensive coalition analysis
        /// Used for scenario testing and detailed compatibility assessment
        /// </summary>
        public void PerformFullAnalysis()
        {
            if (_currentElectionResults == null || _currentElectionResults.Count == 0)
            {
                Debug.LogWarning("No election results available for coalition analysis");
                return;
            }

            if (_isAnalyzing)
            {
                Debug.Log("Coalition analysis already in progress");
                return;
            }

            _isAnalyzing = true;
            _performanceTimer.Restart();

            EventBus.Publish(new CoalitionAnalysisStartedEvent(new List<string>(_selectedParties)));

            try
            {
                // Perform comprehensive analysis
                var analysis = CoalitionFormationSystem.DetectViableCoalitions(_currentElectionResults);
                _lastAnalysis = analysis;

                // Publish results
                EventBus.Publish(new CoalitionAnalysisCompletedEvent(analysis));

                // Performance monitoring
                if (enablePerformanceMonitoring && analysis.AnalysisTimeMs > 5000f)
                {
                    EventBus.Publish(new PerformanceWarningEvent(
                        "Coalition Analysis",
                        analysis.AnalysisTimeMs / 1000f,
                        5.0f,
                        $"Analyzed {analysis.TotalCombinationsAnalyzed} combinations"));
                }

                Debug.Log($"Coalition analysis completed: {analysis.ViableCoalitions.Count} viable coalitions found");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Coalition analysis failed: {ex.Message}");

                EventBus.Publish(new CoalitionFormationFailedEvent(
                    new List<string>(_selectedParties),
                    ex.Message,
                    CoalitionFailureType.UserCancelled));
            }
            finally
            {
                _isAnalyzing = false;
                RecordPerformanceMetric();
            }
        }

        #endregion

        #region Scenario Management

        /// <summary>
        /// Load and analyze specific coalition scenarios
        /// Supports historical scenarios and alternative configurations
        /// </summary>
        public void LoadCoalitionScenario(string scenarioName, List<string> partyAbbreviations)
        {
            Debug.Log($"Loading coalition scenario: {scenarioName}");

            if (partyAbbreviations == null || partyAbbreviations.Count == 0)
            {
                // Generate authentic Dutch scenarios
                if (_currentElectionResults != null)
                {
                    var scenarios = CoalitionFormationSystem.GenerateAuthenticScenarios(_currentElectionResults);

                    if (scenarios.ContainsKey(scenarioName))
                    {
                        var scenario = scenarios[scenarioName];
                        _selectedParties = new List<string>(scenario.PartyNames);

                        EventBus.Publish(new CoalitionFormationEvent(
                            _selectedParties,
                            CoalitionFormationState.Started,
                            $"Loaded scenario: {scenarioName}"));

                        UpdateCoalitionCompatibility();
                        EventBus.Publish(new HighlightCoalitionEvent(_selectedParties));
                    }
                }
            }
            else
            {
                // Load specific party combination
                _selectedParties = new List<string>(partyAbbreviations);

                EventBus.Publish(new CoalitionFormationEvent(
                    _selectedParties,
                    CoalitionFormationState.Started,
                    $"Loaded custom scenario: {scenarioName}"));

                UpdateCoalitionCompatibility();
                EventBus.Publish(new HighlightCoalitionEvent(_selectedParties));
            }
        }

        /// <summary>
        /// Get list of available authentic Dutch coalition scenarios
        /// </summary>
        public List<string> GetAvailableScenarios()
        {
            if (_currentElectionResults == null)
                return new List<string>();

            var scenarios = CoalitionFormationSystem.GenerateAuthenticScenarios(_currentElectionResults);
            return scenarios.Keys.ToList();
        }

        #endregion

        #region Performance Monitoring

        private bool ShouldUpdateNow()
        {
            return (DateTime.Now - _lastUpdateTime).TotalMilliseconds >= updateThrottleMs;
        }

        private void RecordPerformanceMetric()
        {
            if (!enablePerformanceMonitoring)
                return;

            _performanceTimer.Stop();
            float elapsedMs = (float)_performanceTimer.Elapsed.TotalMilliseconds;

            _updateTimes.Add(elapsedMs);

            // Keep only last 100 measurements
            if (_updateTimes.Count > 100)
            {
                _updateTimes.RemoveAt(0);
            }

            // Log performance warnings
            if (elapsedMs > 100f)
            {
                Debug.LogWarning($"Coalition update took {elapsedMs:F2}ms (target: <100ms)");
            }
        }

        private void UpdateElectionResults(ElectionResult result)
        {
            // Convert ElectionResult struct to list format expected by coalition system
            // This would need to be implemented based on the actual ElectionResult structure
            Debug.Log("Election results updated - coalition analysis will use new data");
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get current coalition compatibility score
        /// </summary>
        public float GetCurrentCompatibilityScore()
        {
            if (_selectedParties.Count < 2)
                return 0f;

            var allParties = DutchPoliticalDataGenerator.GenerateAllDutchParties();
            var selectedPoliticalParties = _selectedParties
                .Select(abbrev => allParties.FirstOrDefault(p => p.Abbreviation == abbrev))
                .Where(p => p != null)
                .ToList();

            return CoalitionFormationSystem.CalculateCompatibility(selectedPoliticalParties);
        }

        /// <summary>
        /// Get current selected parties
        /// </summary>
        public List<string> GetSelectedParties()
        {
            return new List<string>(_selectedParties);
        }

        /// <summary>
        /// Check if current coalition is viable (>75 seats)
        /// </summary>
        public bool IsCurrentCoalitionViable()
        {
            return GetTotalSeats() >= 75;
        }

        /// <summary>
        /// Get performance statistics
        /// </summary>
        public string GetPerformanceStats()
        {
            if (_updateTimes.Count == 0)
                return "No performance data available";

            float avgTime = _updateTimes.Average();
            float maxTime = _updateTimes.Max();

            return $"Coalition Formation Performance:\n" +
                   $"Average Update Time: {avgTime:F2}ms\n" +
                   $"Max Update Time: {maxTime:F2}ms\n" +
                   $"Target: <100ms\n" +
                   $"Performance: {(maxTime < 100f ? "GOOD" : "NEEDS OPTIMIZATION")}";
        }

        #endregion
    }
}