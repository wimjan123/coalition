using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using Coalition.Political.Coalitions;
using Coalition.Runtime.Core;

namespace Coalition.Tests.Integration
{
    /// <summary>
    /// Integration tests for Coalition Formation Core system
    /// Validates multi-dimensional compatibility scoring, viable coalition detection,
    /// and authentic Dutch political scenarios against 77 years of coalition history
    ///
    /// Performance targets:
    /// - Individual compatibility calculations: <5ms
    /// - Full coalition analysis: <5 seconds
    /// - Historical accuracy: >90% match with expert assessments
    /// </summary>
    public class CoalitionFormationTests
    {
        private List<PoliticalParty> _dutchParties;
        private List<ElectionResult> _2023ElectionResults;
        private CoalitionFormationManager _coalitionManager;

        [SetUp]
        public void SetUp()
        {
            // Initialize Dutch political data
            _dutchParties = DutchPoliticalDataGenerator.GenerateAllDutchParties();

            // Create 2023 election results for testing
            var votes = DHondtElectionSystem.Get2023ElectionVotes();
            _2023ElectionResults = DHondtElectionSystem.CalculateSeats(votes);

            // Create coalition formation manager
            var gameObject = new GameObject("CoalitionFormationManager");
            _coalitionManager = gameObject.AddComponent<CoalitionFormationManager>();

            Assert.IsNotNull(_dutchParties, "Dutch political data should be initialized");
            Assert.IsNotNull(_2023ElectionResults, "2023 election results should be calculated");
            Assert.AreEqual(15, _dutchParties.Count, "Should have 15 major Dutch parties");
        }

        [TearDown]
        public void TearDown()
        {
            if (_coalitionManager != null)
            {
                Object.DestroyImmediate(_coalitionManager.gameObject);
            }
        }

        #region Compatibility Algorithm Tests

        [Test]
        public void TestMultiDimensionalCompatibility_VVDAndD66_ShouldBeHighlyCompatible()
        {
            // Arrange: VVD and D66 are traditional coalition partners
            var vvd = _dutchParties.First(p => p.Abbreviation == "VVD");
            var d66 = _dutchParties.First(p => p.Abbreviation == "D66");
            var parties = new List<PoliticalParty> { vvd, d66 };

            // Act
            var startTime = System.DateTime.Now;
            float compatibility = CoalitionFormationSystem.CalculateCompatibility(parties);
            var calculationTime = (System.DateTime.Now - startTime).TotalMilliseconds;

            // Assert
            Assert.Greater(compatibility, 0.6f, "VVD-D66 should have high compatibility (Purple Coalition history)");
            Assert.Less(calculationTime, 5.0, "Compatibility calculation should be fast (<5ms)");

            Debug.Log($"VVD-D66 Compatibility: {compatibility:F3} (calculated in {calculationTime:F2}ms)");
        }

        [Test]
        public void TestMultiDimensionalCompatibility_PVVAndGLPvdA_ShouldBeIncompatible()
        {
            // Arrange: PVV and GL-PvdA are ideologically opposed
            var pvv = _dutchParties.First(p => p.Abbreviation == "PVV");
            var glpvda = _dutchParties.First(p => p.Abbreviation == "GL-PvdA");
            var parties = new List<PoliticalParty> { pvv, glpvda };

            // Act
            float compatibility = CoalitionFormationSystem.CalculateCompatibility(parties);

            // Assert
            Assert.Less(compatibility, 0.3f, "PVV and GL-PvdA should have very low compatibility");

            Debug.Log($"PVV-GL/PvdA Compatibility: {compatibility:F3} (expected: very low)");
        }

        [Test]
        public void TestIdeologicalDistanceCalculation_ExtremeParties()
        {
            // Arrange: Test parties at extreme ends of ideological spectrum
            var pvv = _dutchParties.First(p => p.Abbreviation == "PVV");  // Far-right
            var sp = _dutchParties.First(p => p.Abbreviation == "SP");    // Far-left
            var parties = new List<PoliticalParty> { pvv, sp };

            // Act
            float compatibility = CoalitionFormationSystem.CalculateCompatibility(parties);

            // Assert
            Assert.Less(compatibility, 0.2f, "Extreme ideological distance should result in very low compatibility");

            Debug.Log($"PVV-SP Compatibility: {compatibility:F3} (extreme ideological distance)");
        }

        [Test]
        public void TestHistoricalPartnershipBonus_CDAAndCU()
        {
            // Arrange: CDA and CU are traditional Christian partners
            var cda = _dutchParties.First(p => p.Abbreviation == "CDA");
            var cu = _dutchParties.First(p => p.Abbreviation == "CU");
            var parties = new List<PoliticalParty> { cda, cu };

            // Act
            float compatibility = CoalitionFormationSystem.CalculateCompatibility(parties);

            // Assert
            Assert.Greater(compatibility, 0.7f, "CDA-CU should benefit from strong historical partnership bonus");

            Debug.Log($"CDA-CU Compatibility: {compatibility:F3} (Christian partnership bonus)");
        }

        #endregion

        #region Coalition Detection Tests

        [Test]
        public void TestViableCoalitionDetection_2023Results_ShouldFindMajorityCoalitions()
        {
            // Act
            var startTime = System.DateTime.Now;
            var analysis = CoalitionFormationSystem.DetectViableCoalitions(_2023ElectionResults);
            var analysisTime = (System.DateTime.Now - startTime).TotalMilliseconds;

            // Assert
            Assert.IsNotNull(analysis, "Coalition analysis should complete successfully");
            Assert.Greater(analysis.ViableCoalitions.Count, 0, "Should find at least one viable coalition");
            Assert.Less(analysisTime, 5000.0, "Full analysis should complete in <5 seconds");

            // Check that all viable coalitions have >75 seats
            foreach (var coalition in analysis.ViableCoalitions)
            {
                Assert.GreaterOrEqual(coalition.TotalSeats, 75,
                    $"Coalition {string.Join("-", coalition.PartyNames)} should have majority (>75 seats)");
            }

            Debug.Log($"Coalition Analysis Results:");
            Debug.Log($"- Viable coalitions found: {analysis.ViableCoalitions.Count}");
            Debug.Log($"- Minority options: {analysis.MinorityOptions.Count}");
            Debug.Log($"- Analysis time: {analysisTime:F2}ms");
            Debug.Log($"- Combinations analyzed: {analysis.TotalCombinationsAnalyzed}");
        }

        [Test]
        public void TestCurrentGovernmentCoalition_PVVVVDNSCBBBShouldBeViable()
        {
            // Arrange: Test the actual 2023-2024 government coalition
            var coalitionParties = new[] { "PVV", "VVD", "NSC", "BBB" };
            var parties = _dutchParties.Where(p => coalitionParties.Contains(p.Abbreviation)).ToList();

            // Act
            var analysis = CoalitionFormationSystem.DetectViableCoalitions(_2023ElectionResults);
            var actualCoalition = analysis.ViableCoalitions.FirstOrDefault(c =>
                coalitionParties.All(party => c.PartyNames.Contains(party)));

            // Assert
            Assert.IsNotNull(actualCoalition, "Current government coalition should be detected as viable");
            Assert.GreaterOrEqual(actualCoalition.TotalSeats, 75, "Current coalition should have majority");
            Assert.Greater(actualCoalition.CompatibilityScore, 0.3f, "Should have reasonable compatibility despite challenges");

            Debug.Log($"Current Government Coalition Analysis:");
            Debug.Log($"- Parties: {string.Join(", ", actualCoalition.PartyNames)}");
            Debug.Log($"- Total seats: {actualCoalition.TotalSeats}");
            Debug.Log($"- Compatibility score: {actualCoalition.CompatibilityScore:F3}");
            Debug.Log($"- Red-line violations: {actualCoalition.RedLineViolations.Count}");
        }

        [Test]
        public void TestLeftCoalitionScenario_GLPvdAD66Volt()
        {
            // Arrange: Test progressive left coalition scenario
            var coalitionParties = new[] { "GL-PvdA", "D66", "Volt", "PvdD", "SP" };
            var parties = _dutchParties.Where(p => coalitionParties.Contains(p.Abbreviation)).ToList();

            // Act
            float compatibility = CoalitionFormationSystem.CalculateCompatibility(parties);
            int totalSeats = 0;
            foreach (var partyAbbrev in coalitionParties)
            {
                var result = _2023ElectionResults.FirstOrDefault(r => r.abbreviation == partyAbbrev);
                totalSeats += result?.seats ?? 0;
            }

            // Assert
            Assert.Greater(compatibility, 0.7f, "Left coalition should have high ideological compatibility");
            // Note: This coalition may not have majority seats due to 2023 election results

            Debug.Log($"Left Coalition Analysis:");
            Debug.Log($"- Parties: {string.Join(", ", coalitionParties)}");
            Debug.Log($"- Total seats: {totalSeats}");
            Debug.Log($"- Compatibility score: {compatibility:F3}");
            Debug.Log($"- Viable: {totalSeats >= 75}");
        }

        #endregion

        #region Authentic Dutch Scenarios

        [Test]
        public void TestAuthenticDutchScenarios_ShouldGenerateRealisticOptions()
        {
            // Act
            var scenarios = CoalitionFormationSystem.GenerateAuthenticScenarios(_2023ElectionResults);

            // Assert
            Assert.IsNotNull(scenarios, "Should generate authentic scenarios");
            Assert.Greater(scenarios.Count, 0, "Should provide multiple scenario options");

            // Check for key scenario types
            Assert.IsTrue(scenarios.ContainsKey("Current Government"), "Should include current government scenario");
            Assert.IsTrue(scenarios.ContainsKey("Purple Coalition"), "Should include purple coalition scenario");
            Assert.IsTrue(scenarios.ContainsKey("Left Coalition"), "Should include left coalition scenario");
            Assert.IsTrue(scenarios.ContainsKey("Right Coalition"), "Should include right coalition scenario");

            Debug.Log($"Authentic Dutch Scenarios Generated:");
            foreach (var scenario in scenarios)
            {
                Debug.Log($"- {scenario.Key}: {string.Join(", ", scenario.Value.PartyNames)} " +
                         $"({scenario.Value.TotalSeats} seats, {scenario.Value.CompatibilityScore:F3} compatibility)");
            }
        }

        [Test]
        public void TestRedLineViolations_PVVExclusions()
        {
            // Arrange: Test PVV exclusions by other parties
            var pvv = _dutchParties.First(p => p.Abbreviation == "PVV");
            var glpvda = _dutchParties.First(p => p.Abbreviation == "GL-PvdA");
            var d66 = _dutchParties.First(p => p.Abbreviation == "D66");
            var parties = new List<PoliticalParty> { pvv, glpvda, d66 };

            // Act
            var analysis = CoalitionFormationSystem.DetectViableCoalitions(_2023ElectionResults);
            var problematicCoalition = analysis.BlockedCoalitions.FirstOrDefault(c =>
                c.PartyNames.Contains("PVV") && (c.PartyNames.Contains("GL-PvdA") || c.PartyNames.Contains("D66")));

            // Assert
            Assert.IsNotNull(problematicCoalition, "Should detect red-line violations with PVV");
            Assert.Greater(problematicCoalition.RedLineViolations.Count, 0, "Should have documented red-line violations");

            Debug.Log($"Red-Line Violations Test:");
            Debug.Log($"- Coalition: {string.Join(", ", problematicCoalition.PartyNames)}");
            Debug.Log($"- Violations: {string.Join("; ", problematicCoalition.RedLineViolations)}");
        }

        #endregion

        #region Historical Validation

        [Test]
        public void TestHistoricalValidation_ShouldAchieveHighAccuracy()
        {
            // Act
            var analysis = CoalitionFormationSystem.DetectViableCoalitions(_2023ElectionResults);
            float accuracy = CoalitionFormationSystem.ValidateAgainstHistory(analysis);

            // Assert
            Assert.GreaterOrEqual(accuracy, 90.0f, "Historical validation should achieve >90% accuracy");

            Debug.Log($"Historical Validation Results:");
            Debug.Log($"- Accuracy: {accuracy:F1}%");
            Debug.Log($"- Target: >90%");
            Debug.Log($"- Result: {(accuracy >= 90.0f ? "PASSED" : "FAILED")}");
        }

        [Test]
        public void TestRutteCoalitionPatterns_ShouldRecognizeSuccessfulFormulas()
        {
            // Arrange: Test patterns from successful Rutte coalitions
            var rutteIPattern = new[] { "VVD", "CDA" }; // Simplified Rutte I
            var rutteIIPattern = new[] { "VVD", "PvdA" }; // Rutte II core
            var rutteIIIPattern = new[] { "VVD", "D66", "CDA", "CU" }; // Rutte III

            // Act & Assert for each pattern
            foreach (var pattern in new[] { rutteIPattern, rutteIIPattern, rutteIIIPattern })
            {
                var parties = _dutchParties.Where(p => pattern.Contains(p.Abbreviation)).ToList();
                float compatibility = CoalitionFormationSystem.CalculateCompatibility(parties);

                Assert.Greater(compatibility, 0.5f,
                    $"Rutte coalition pattern {string.Join("-", pattern)} should show reasonable compatibility");

                Debug.Log($"Rutte Pattern {string.Join("-", pattern)}: {compatibility:F3} compatibility");
            }
        }

        #endregion

        #region Performance Tests

        [Test]
        public void TestPerformanceTargets_CompatibilityCalculation()
        {
            // Arrange
            var testParties = _dutchParties.Take(4).ToList();
            var iterations = 100;
            var times = new List<double>();

            // Act
            for (int i = 0; i < iterations; i++)
            {
                var startTime = System.DateTime.Now;
                CoalitionFormationSystem.CalculateCompatibility(testParties);
                var endTime = System.DateTime.Now;
                times.Add((endTime - startTime).TotalMilliseconds);
            }

            // Assert
            double averageTime = times.Average();
            double maxTime = times.Max();

            Assert.Less(averageTime, 5.0, "Average compatibility calculation should be <5ms");
            Assert.Less(maxTime, 10.0, "Maximum compatibility calculation should be <10ms");

            Debug.Log($"Performance Test Results:");
            Debug.Log($"- Iterations: {iterations}");
            Debug.Log($"- Average time: {averageTime:F2}ms");
            Debug.Log($"- Max time: {maxTime:F2}ms");
            Debug.Log($"- Target: <5ms average");
        }

        [Test]
        public void TestPerformanceTargets_FullAnalysis()
        {
            // Act
            var startTime = System.DateTime.Now;
            var analysis = CoalitionFormationSystem.DetectViableCoalitions(_2023ElectionResults);
            var endTime = System.DateTime.Now;
            var totalTime = (endTime - startTime).TotalSeconds;

            // Assert
            Assert.Less(totalTime, 5.0, "Full coalition analysis should complete in <5 seconds");
            Assert.Greater(analysis.TotalCombinationsAnalyzed, 50, "Should analyze substantial number of combinations");

            Debug.Log($"Full Analysis Performance:");
            Debug.Log($"- Total time: {totalTime:F2} seconds");
            Debug.Log($"- Combinations analyzed: {analysis.TotalCombinationsAnalyzed}");
            Debug.Log($"- Analysis rate: {analysis.TotalCombinationsAnalyzed / totalTime:F0} combinations/second");
        }

        #endregion

        #region Integration Tests

        [UnityTest]
        public IEnumerator TestCoalitionManagerIntegration_RealTimeUpdates()
        {
            // Arrange
            bool compatibilityEventReceived = false;
            CoalitionCompatibilityUpdatedEvent lastEvent = null;

            EventBus.Subscribe<CoalitionCompatibilityUpdatedEvent>(evt => {
                compatibilityEventReceived = true;
                lastEvent = evt;
            });

            // Act
            _coalitionManager.HandlePartySelection("VVD");
            yield return new WaitForSeconds(0.1f);

            _coalitionManager.HandlePartySelection("D66");
            yield return new WaitForSeconds(0.2f);

            // Assert
            Assert.IsTrue(compatibilityEventReceived, "Should receive compatibility update events");
            Assert.IsNotNull(lastEvent, "Should have received event data");
            Assert.AreEqual(2, lastEvent.SelectedParties.Count, "Should track selected parties");
            Assert.Greater(lastEvent.CompatibilityScore, 0f, "Should calculate compatibility score");

            Debug.Log($"Real-time Integration Test:");
            Debug.Log($"- Parties: {string.Join(", ", lastEvent.SelectedParties)}");
            Debug.Log($"- Compatibility: {lastEvent.CompatibilityScore:F3}");
            Debug.Log($"- Total seats: {lastEvent.TotalSeats}");
            Debug.Log($"- Viable: {lastEvent.IsViable}");

            // Cleanup
            EventBus.Unsubscribe<CoalitionCompatibilityUpdatedEvent>(evt => {
                compatibilityEventReceived = true;
                lastEvent = evt;
            });
        }

        #endregion
    }
}