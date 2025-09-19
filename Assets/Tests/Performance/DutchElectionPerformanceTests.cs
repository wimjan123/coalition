using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Coalition.Political.Elections;
using Coalition.Political.Parties;

namespace Coalition.Tests.Performance
{
    /// <summary>
    /// Performance validation tests for Dutch election system
    /// Ensures <1 second calculation time requirement is met
    /// Tests scalability and resource efficiency
    /// </summary>
    [TestFixture]
    public class DutchElectionPerformanceTests
    {
        private Dictionary<string, int> authentic2023Votes;
        private Stopwatch stopwatch;

        [SetUp]
        public void Setup()
        {
            authentic2023Votes = DHondtElectionSystem.Get2023ElectionVotes();
            stopwatch = new Stopwatch();
        }

        [Test]
        [Category("Performance Target")]
        [Timeout(2000)] // Fail if test takes more than 2 seconds
        public void DHondt_SingleCalculation_CompletesUnder1Second()
        {
            // Arrange
            const double TARGET_TIME_MS = 1000.0;

            // Act
            stopwatch.Start();
            var results = DHondtElectionSystem.CalculateSeats(authentic2023Votes);
            stopwatch.Stop();

            var elapsedMs = stopwatch.Elapsed.TotalMilliseconds;

            // Assert
            Assert.IsNotNull(results, "Results should not be null");
            Assert.AreEqual(150, results.Sum(r => r.seats), "Should allocate exactly 150 seats");
            Assert.Less(elapsedMs, TARGET_TIME_MS,
                $"Single calculation should complete in <{TARGET_TIME_MS}ms, took {elapsedMs:F2}ms");

            UnityEngine.Debug.Log($"✅ Single calculation performance: {elapsedMs:F2}ms (target: <{TARGET_TIME_MS}ms)");
        }

        [Test]
        [Category("Performance Stress")]
        public void DHondt_MultipleCalculations_MaintainPerformance()
        {
            // Arrange
            const int iterations = 100;
            const double MAX_AVERAGE_TIME_MS = 500.0; // Even stricter for multiple calculations
            var times = new List<double>();

            // Act
            for (int i = 0; i < iterations; i++)
            {
                stopwatch.Restart();
                var results = DHondtElectionSystem.CalculateSeats(authentic2023Votes);
                stopwatch.Stop();

                times.Add(stopwatch.Elapsed.TotalMilliseconds);

                // Validate results are consistent
                Assert.AreEqual(150, results.Sum(r => r.seats), $"Iteration {i}: Should allocate exactly 150 seats");
            }

            // Assert
            double averageTime = times.Average();
            double maxTime = times.Max();
            double minTime = times.Min();

            Assert.Less(averageTime, MAX_AVERAGE_TIME_MS,
                $"Average time ({averageTime:F2}ms) should be <{MAX_AVERAGE_TIME_MS}ms over {iterations} iterations");

            Assert.Less(maxTime, 1000.0,
                $"Maximum time ({maxTime:F2}ms) should still be <1000ms");

            UnityEngine.Debug.Log($"✅ Stress test performance over {iterations} iterations:");
            UnityEngine.Debug.Log($"   Average: {averageTime:F2}ms");
            UnityEngine.Debug.Log($"   Min: {minTime:F2}ms, Max: {maxTime:F2}ms");
        }

        [Test]
        [Category("Memory Efficiency")]
        public void DHondt_MemoryUsage_RemainsEfficient()
        {
            // Arrange
            long initialMemory = System.GC.GetTotalMemory(true);
            const int iterations = 50;

            // Act
            for (int i = 0; i < iterations; i++)
            {
                var results = DHondtElectionSystem.CalculateSeats(authentic2023Votes);
                // Ensure results are used to prevent optimization
                Assert.Greater(results.Count, 0);
            }

            // Force garbage collection and measure
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();

            long finalMemory = System.GC.GetTotalMemory(false);
            long memoryIncrease = finalMemory - initialMemory;

            // Assert
            // Memory increase should be minimal (less than 10MB for 50 iterations)
            const long MAX_MEMORY_INCREASE = 10 * 1024 * 1024; // 10MB
            Assert.Less(memoryIncrease, MAX_MEMORY_INCREASE,
                $"Memory increase ({memoryIncrease / 1024 / 1024}MB) should be <{MAX_MEMORY_INCREASE / 1024 / 1024}MB over {iterations} iterations");

            UnityEngine.Debug.Log($"✅ Memory efficiency test: {memoryIncrease / 1024}KB increase over {iterations} iterations");
        }

        [Test]
        [Category("Scalability")]
        public void DHondt_LargerElection_ScalesWell()
        {
            // Arrange - Simulate a larger election with more parties and votes
            var largeElectionVotes = new Dictionary<string, int>();

            // Add original parties with increased vote counts
            foreach (var party in authentic2023Votes)
            {
                largeElectionVotes[party.Key] = party.Value * 2; // Double the votes
            }

            // Add additional fictional parties to test scalability
            for (int i = 1; i <= 10; i++)
            {
                largeElectionVotes[$"TestParty{i}"] = UnityEngine.Random.Range(50000, 500000);
            }

            const double MAX_TIME_MS = 1500.0; // Slightly more lenient for larger election

            // Act
            stopwatch.Start();
            var results = DHondtElectionSystem.CalculateSeats(largeElectionVotes);
            stopwatch.Stop();

            var elapsedMs = stopwatch.Elapsed.TotalMilliseconds;

            // Assert
            Assert.AreEqual(150, results.Sum(r => r.seats), "Should still allocate exactly 150 seats");
            Assert.Less(elapsedMs, MAX_TIME_MS,
                $"Larger election should complete in <{MAX_TIME_MS}ms, took {elapsedMs:F2}ms");

            UnityEngine.Debug.Log($"✅ Scalability test with {largeElectionVotes.Count} parties: {elapsedMs:F2}ms");
        }

        [UnityTest]
        [Category("Unity Performance")]
        public IEnumerator DutchElectionManager_InitializationPerformance_CompletesQuickly()
        {
            // Arrange
            var gameObject = new GameObject("TestElectionManager");
            const double MAX_INIT_TIME_MS = 2000.0; // 2 seconds for full initialization

            // Act
            stopwatch.Start();
            var manager = gameObject.AddComponent<DutchElectionManager>();

            // Wait a frame for initialization
            yield return null;

            // Trigger validation
            bool isValid = manager.ValidateElectionSystem();
            stopwatch.Stop();

            var elapsedMs = stopwatch.Elapsed.TotalMilliseconds;

            // Assert
            Assert.IsTrue(isValid, "Election system should validate successfully");
            Assert.Less(elapsedMs, MAX_INIT_TIME_MS,
                $"Full system initialization should complete in <{MAX_INIT_TIME_MS}ms, took {elapsedMs:F2}ms");

            // Cleanup
            Object.DestroyImmediate(gameObject);

            UnityEngine.Debug.Log($"✅ Full system initialization: {elapsedMs:F2}ms (validation included)");
        }

        [Test]
        [Category("Data Generation Performance")]
        public void DutchPoliticalData_Generation_CompletesQuickly()
        {
            // Arrange
            const double MAX_GENERATION_TIME_MS = 500.0;

            // Act
            stopwatch.Start();
            var parties = DutchPoliticalDataGenerator.GenerateAllDutchParties();
            stopwatch.Stop();

            var elapsedMs = stopwatch.Elapsed.TotalMilliseconds;

            // Assert
            Assert.AreEqual(15, parties.Count, "Should generate exactly 15 parties");
            Assert.Less(elapsedMs, MAX_GENERATION_TIME_MS,
                $"Party data generation should complete in <{MAX_GENERATION_TIME_MS}ms, took {elapsedMs:F2}ms");

            // Cleanup
            foreach (var party in parties)
            {
                if (party != null)
                {
                    Object.DestroyImmediate(party);
                }
            }

            UnityEngine.Debug.Log($"✅ Political data generation: {elapsedMs:F2}ms for 15 parties");
        }

        [Test]
        [Category("Coalition Analysis Performance")]
        public void CoalitionAnalysis_Performance_RemainsEfficient()
        {
            // Arrange
            var gameObject = new GameObject("TestElectionManager");
            var manager = gameObject.AddComponent<DutchElectionManager>();

            // Wait for initialization and run election
            var results = manager.RunAuthentic2023Election();

            const double MAX_COALITION_TIME_MS = 1000.0;

            // Act
            stopwatch.Start();
            var coalitionPossibilities = manager.GetCoalitionPossibilities();
            stopwatch.Stop();

            var elapsedMs = stopwatch.Elapsed.TotalMilliseconds;

            // Assert
            Assert.Greater(coalitionPossibilities.Count, 0, "Should find at least one viable coalition");
            Assert.Less(elapsedMs, MAX_COALITION_TIME_MS,
                $"Coalition analysis should complete in <{MAX_COALITION_TIME_MS}ms, took {elapsedMs:F2}ms");

            // Cleanup
            Object.DestroyImmediate(gameObject);

            UnityEngine.Debug.Log($"✅ Coalition analysis: {elapsedMs:F2}ms for {coalitionPossibilities.Count} possibilities");
        }

        [Test]
        [Category("Overall System Performance")]
        public void CompleteElectionCycle_EndToEnd_MeetsPerformanceTargets()
        {
            // Arrange
            var gameObject = new GameObject("TestElectionManager");
            const double MAX_TOTAL_TIME_MS = 3000.0; // 3 seconds for complete cycle

            // Act
            stopwatch.Start();

            // Full election cycle: Initialize → Run Election → Analyze Coalitions → Get Stats
            var manager = gameObject.AddComponent<DutchElectionManager>();
            var results = manager.RunAuthentic2023Election();
            var coalitions = manager.GetCoalitionPossibilities();
            var stats = manager.GetPerformanceStats();

            stopwatch.Stop();

            var elapsedMs = stopwatch.Elapsed.TotalMilliseconds;

            // Assert
            Assert.IsNotNull(results, "Election results should not be null");
            Assert.Greater(coalitions.Count, 0, "Should find coalition possibilities");
            Assert.Greater((int)stats["ElectionsCalculated"], 0, "Should track elections calculated");

            Assert.Less(elapsedMs, MAX_TOTAL_TIME_MS,
                $"Complete election cycle should finish in <{MAX_TOTAL_TIME_MS}ms, took {elapsedMs:F2}ms");

            // Cleanup
            Object.DestroyImmediate(gameObject);

            UnityEngine.Debug.Log($"✅ Complete election cycle: {elapsedMs:F2}ms");
            UnityEngine.Debug.Log($"   - Election results: {results.Count} parties");
            UnityEngine.Debug.Log($"   - Coalition possibilities: {coalitions.Count}");
            UnityEngine.Debug.Log($"   - Performance stats: {stats.Count} metrics");
        }

        [TearDown]
        public void TearDown()
        {
            stopwatch?.Stop();
        }
    }
}