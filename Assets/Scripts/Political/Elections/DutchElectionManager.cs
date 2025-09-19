using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Coalition.Data;
using Coalition.Political.Parties;

namespace Coalition.Political.Elections
{
    /// <summary>
    /// Comprehensive Dutch election management system for COALITION demo
    /// Integrates D'Hondt algorithm with authentic political party data
    /// Provides simulation framework for democratic representation
    /// </summary>
    public class DutchElectionManager : MonoBehaviour
    {
        [Header("Election Configuration")]
        [SerializeField] private bool useAuthentic2023Data = true;
        [SerializeField] private bool enablePerformanceLogging = true;
        [SerializeField] private bool validateResultsOnStart = true;

        [Header("Runtime State")]
        [SerializeField] private List<PoliticalParty> availableParties;
        [SerializeField] private List<ElectionResult> lastElectionResults;

        [Header("Demo Visualization")]
        [SerializeField] private Transform parliamentVisualization;
        [SerializeField] private GameObject seatPrefab;

        // Performance tracking
        private float lastCalculationTime;
        private int electionsCalculated;
        private Dictionary<string, float> performanceMetrics;

        // Events
        public event System.Action<List<ElectionResult>> OnElectionCompleted;
        public event System.Action<string> OnPerformanceUpdate;

        private void Awake()
        {
            InitializeElectionSystem();
        }

        private void Start()
        {
            if (validateResultsOnStart)
            {
                ValidateElectionSystem();
            }

            if (useAuthentic2023Data)
            {
                RunAuthentic2023Election();
            }
        }

        /// <summary>
        /// Initialize the election system with Dutch political parties
        /// </summary>
        private void InitializeElectionSystem()
        {
            LogInfo("🇳🇱 Initializing Dutch Election Management System");

            // Generate all Dutch political parties
            availableParties = DutchPoliticalDataGenerator.GenerateAllDutchParties();
            lastElectionResults = new List<ElectionResult>();
            performanceMetrics = new Dictionary<string, float>();
            electionsCalculated = 0;

            LogInfo($"✅ Initialized with {availableParties.Count} Dutch political parties");
        }

        /// <summary>
        /// Run election simulation using authentic 2023 Dutch election data
        /// </summary>
        public List<ElectionResult> RunAuthentic2023Election()
        {
            LogInfo("🗳️ Running authentic 2023 Dutch election simulation");

            var startTime = Time.realtimeSinceStartup;

            // Get authentic 2023 vote data
            var voteData = DHondtElectionSystem.Get2023ElectionVotes();

            // Calculate seats using D'Hondt method
            var results = DHondtElectionSystem.CalculateSeats(voteData);

            // Record performance
            lastCalculationTime = (Time.realtimeSinceStartup - startTime) * 1000f; // Convert to milliseconds
            electionsCalculated++;

            // Validate against expected results
            var expectedSeats = DHondtElectionSystem.Get2023ExpectedSeats();
            bool isValid = DHondtElectionSystem.ValidateResults(results, expectedSeats);

            LogInfo($"📊 Election calculation completed in {lastCalculationTime:F2}ms");
            LogInfo($"✅ Results validation: {(isValid ? "PASSED" : "FAILED")}");

            // Store results
            lastElectionResults = results;

            // Update performance metrics
            UpdatePerformanceMetrics();

            // Trigger events
            OnElectionCompleted?.Invoke(results);
            OnPerformanceUpdate?.Invoke($"Calculation: {lastCalculationTime:F2}ms, Validation: {(isValid ? "PASS" : "FAIL")}");

            return results;
        }

        /// <summary>
        /// Run custom election with specified vote percentages
        /// </summary>
        /// <param name="partyVotePercentages">Dictionary of party abbreviation to vote percentage</param>
        /// <param name="totalVoters">Total number of voters (default: realistic Dutch turnout)</param>
        /// <returns>Election results with seat allocation</returns>
        public List<ElectionResult> RunCustomElection(Dictionary<string, float> partyVotePercentages, int totalVoters = 10_300_000)
        {
            LogInfo("🗳️ Running custom election simulation");

            var startTime = Time.realtimeSinceStartup;

            // Convert percentages to vote counts
            var voteData = new Dictionary<string, int>();
            foreach (var entry in partyVotePercentages)
            {
                int votes = Mathf.RoundToInt((entry.Value / 100f) * totalVoters);
                voteData[entry.Key] = votes;
            }

            // Calculate seats
            var results = DHondtElectionSystem.CalculateSeats(voteData);

            // Record performance
            lastCalculationTime = (Time.realtimeSinceStartup - startTime) * 1000f;
            electionsCalculated++;

            LogInfo($"📊 Custom election completed in {lastCalculationTime:F2}ms");

            // Store results
            lastElectionResults = results;

            // Update performance metrics
            UpdatePerformanceMetrics();

            // Trigger events
            OnElectionCompleted?.Invoke(results);

            return results;
        }

        /// <summary>
        /// Validate the election system against known 2023 results
        /// </summary>
        public bool ValidateElectionSystem()
        {
            LogInfo("🔍 Validating Dutch election system...");

            try
            {
                // Test data integrity
                bool dataValid = DutchPoliticalDataGenerator.ValidateGeneratedData();
                if (!dataValid)
                {
                    LogError("❌ Political data validation failed");
                    return false;
                }

                // Test D'Hondt algorithm accuracy
                var testVotes = DHondtElectionSystem.Get2023ElectionVotes();
                var testResults = DHondtElectionSystem.CalculateSeats(testVotes);
                var expectedSeats = DHondtElectionSystem.Get2023ExpectedSeats();

                bool algorithmValid = DHondtElectionSystem.ValidateResults(testResults, expectedSeats);
                if (!algorithmValid)
                {
                    LogError("❌ D'Hondt algorithm validation failed");
                    return false;
                }

                // Test performance benchmark
                double averageTime = DHondtElectionSystem.BenchmarkPerformance(20);
                bool performanceValid = averageTime < 1000.0; // <1 second target

                if (!performanceValid)
                {
                    LogWarning($"⚠️ Performance target not met: {averageTime:F2}ms (target: <1000ms)");
                }

                LogInfo("✅ Dutch election system validation completed successfully");
                LogInfo($"📈 Performance: {averageTime:F2}ms average");

                return dataValid && algorithmValid;
            }
            catch (Exception ex)
            {
                LogError($"❌ Validation failed with exception: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get performance statistics for the election system
        /// </summary>
        /// <returns>Dictionary of performance metrics</returns>
        public Dictionary<string, object> GetPerformanceStats()
        {
            return new Dictionary<string, object>
            {
                ["ElectionsCalculated"] = electionsCalculated,
                ["LastCalculationTime"] = lastCalculationTime,
                ["AverageCalculationTime"] = performanceMetrics.ContainsKey("AverageTime") ? performanceMetrics["AverageTime"] : 0f,
                ["AvailableParties"] = availableParties?.Count ?? 0,
                ["LastResultsCount"] = lastElectionResults?.Count ?? 0,
                ["SystemValidated"] = validateResultsOnStart
            };
        }

        /// <summary>
        /// Get coalition formation possibilities based on last election results
        /// </summary>
        /// <param name="minimumSeats">Minimum seats required for majority (default: 76)</param>
        /// <returns>List of possible coalition combinations</returns>
        public List<CoalitionPossibility> GetCoalitionPossibilities(int minimumSeats = 76)
        {
            if (lastElectionResults == null || lastElectionResults.Count == 0)
            {
                LogWarning("⚠️ No election results available for coalition analysis");
                return new List<CoalitionPossibility>();
            }

            var possibilities = new List<CoalitionPossibility>();

            // Get parties that won seats
            var seatedParties = lastElectionResults.Where(r => r.seats > 0).OrderByDescending(r => r.seats).ToList();

            // Find all possible coalitions that reach minimum seats
            for (int partyCount = 2; partyCount <= Math.Min(6, seatedParties.Count); partyCount++)
            {
                var combinations = GetCombinations(seatedParties, partyCount);

                foreach (var combination in combinations)
                {
                    int totalSeats = combination.Sum(p => p.seats);
                    if (totalSeats >= minimumSeats)
                    {
                        float compatibility = CalculateCoalitionCompatibility(combination);
                        possibilities.Add(new CoalitionPossibility
                        {
                            Parties = combination.Select(p => p.abbreviation).ToList(),
                            TotalSeats = totalSeats,
                            CompatibilityScore = compatibility,
                            IsViable = compatibility > 0.3f // Minimum viability threshold
                        });
                    }
                }
            }

            // Sort by viability and compatibility
            possibilities = possibilities
                .Where(p => p.IsViable)
                .OrderByDescending(p => p.CompatibilityScore)
                .Take(10) // Top 10 most viable coalitions
                .ToList();

            LogInfo($"📋 Found {possibilities.Count} viable coalition possibilities");

            return possibilities;
        }

        /// <summary>
        /// Update parliament visualization with current election results
        /// </summary>
        public void UpdateParliamentVisualization()
        {
            if (parliamentVisualization == null || seatPrefab == null || lastElectionResults == null)
            {
                LogWarning("⚠️ Cannot update parliament visualization - missing components");
                return;
            }

            LogInfo("🏛️ Updating parliament visualization");

            // Clear existing seats
            foreach (Transform child in parliamentVisualization)
            {
                DestroyImmediate(child.gameObject);
            }

            // Create seats for each party
            int seatIndex = 0;
            foreach (var result in lastElectionResults.Where(r => r.seats > 0).OrderByDescending(r => r.seats))
            {
                var party = availableParties.FirstOrDefault(p => p.Abbreviation == result.abbreviation);
                if (party != null)
                {
                    for (int i = 0; i < result.seats; i++)
                    {
                        var seat = Instantiate(seatPrefab, parliamentVisualization);
                        ConfigureSeat(seat, party, seatIndex++);
                    }
                }
            }

            LogInfo($"🏛️ Parliament visualization updated with {seatIndex} seats");
        }

        #region Private Methods

        private void UpdatePerformanceMetrics()
        {
            if (!performanceMetrics.ContainsKey("TotalTime"))
                performanceMetrics["TotalTime"] = 0f;

            performanceMetrics["TotalTime"] += lastCalculationTime;
            performanceMetrics["AverageTime"] = performanceMetrics["TotalTime"] / electionsCalculated;

            if (enablePerformanceLogging)
            {
                LogInfo($"📊 Performance: {lastCalculationTime:F2}ms (avg: {performanceMetrics["AverageTime"]:F2}ms)");
            }
        }

        private void ConfigureSeat(GameObject seat, PoliticalParty party, int seatIndex)
        {
            // Set seat color to party color
            var renderer = seat.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = party.PartyColor;
            }

            // Position in semicircle arrangement
            float angle = (seatIndex / 150f) * 180f; // 180-degree semicircle
            float radius = 5f + (seatIndex % 3) * 0.5f; // Multiple rows

            float x = Mathf.Cos(angle * Mathf.Deg2Rad) * radius;
            float z = Mathf.Sin(angle * Mathf.Deg2Rad) * radius;

            seat.transform.localPosition = new Vector3(x, 0, z);
        }

        private float CalculateCoalitionCompatibility(List<ElectionResult> coalition)
        {
            if (coalition.Count < 2) return 0f;

            float totalCompatibility = 0f;
            int comparisons = 0;

            for (int i = 0; i < coalition.Count; i++)
            {
                var party1 = availableParties.FirstOrDefault(p => p.Abbreviation == coalition[i].abbreviation);
                if (party1 == null) continue;

                for (int j = i + 1; j < coalition.Count; j++)
                {
                    var party2 = availableParties.FirstOrDefault(p => p.Abbreviation == coalition[j].abbreviation);
                    if (party2 == null) continue;

                    totalCompatibility += party1.CalculateCoalitionCompatibility(party2);
                    comparisons++;
                }
            }

            return comparisons > 0 ? totalCompatibility / comparisons : 0f;
        }

        private IEnumerable<List<T>> GetCombinations<T>(List<T> items, int count)
        {
            if (count == 1)
                return items.Select(item => new List<T> { item });

            return GetCombinations(items, count - 1)
                .SelectMany((t, i) => items.Skip(i + 1).Take(items.Count - i - 1), (t, item) => t.Concat(new List<T> { item }).ToList());
        }

        private void LogInfo(string message)
        {
            Debug.Log($"[DutchElectionManager] {message}");
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[DutchElectionManager] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[DutchElectionManager] {message}");
        }

        #endregion
    }

    /// <summary>
    /// Represents a possible coalition formation
    /// </summary>
    [System.Serializable]
    public class CoalitionPossibility
    {
        public List<string> Parties;
        public int TotalSeats;
        public float CompatibilityScore;
        public bool IsViable;

        public override string ToString()
        {
            return $"{string.Join("-", Parties)} ({TotalSeats} seats, {CompatibilityScore:F2} compatibility)";
        }
    }
}