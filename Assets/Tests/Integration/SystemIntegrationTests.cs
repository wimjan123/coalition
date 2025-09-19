using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Coalition.Tests.Integration
{
    /// <summary>
    /// Comprehensive system integration tests for the complete COALITION demo
    /// Validates all systems working together with performance requirements
    /// </summary>
    public class SystemIntegrationTests
    {
        private DemoGameManager demoGameManager;
        private Stopwatch performanceTimer;
        private const float TARGET_FPS = 60f;
        private const int MAX_CALCULATION_TIME_MS = 5000;
        private const long MAX_MEMORY_USAGE_BYTES = 1_073_741_824; // 1GB

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            performanceTimer = new Stopwatch();
        }

        [SetUp]
        public void SetUp()
        {
            // Create test GameObject with DemoGameManager
            var gameObject = new GameObject("TestDemoGameManager");
            demoGameManager = gameObject.AddComponent<DemoGameManager>();

            // Clear EventBus to ensure clean state
            EventBus.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            if (demoGameManager != null)
            {
                Object.DestroyImmediate(demoGameManager.gameObject);
            }
            EventBus.Clear();
        }

        [UnityTest]
        public IEnumerator FullSystemIntegration_ValidatesAllSystems()
        {
            // Test: Complete system initialization and integration
            performanceTimer.Start();

            // Initialize all systems
            demoGameManager.InitializeSystems();
            yield return new WaitForSeconds(1f);

            // Verify all critical systems are initialized
            Assert.IsTrue(demoGameManager.IsSystemInitialized("ElectoralSystem"), "Electoral system not initialized");
            Assert.IsTrue(demoGameManager.IsSystemInitialized("CoalitionFormation"), "Coalition formation not initialized");
            Assert.IsTrue(demoGameManager.IsSystemInitialized("UIManager"), "UI Manager not initialized");
            Assert.IsTrue(demoGameManager.IsSystemInitialized("EventBus"), "EventBus not initialized");

            performanceTimer.Stop();
            Assert.Less(performanceTimer.ElapsedMilliseconds, MAX_CALCULATION_TIME_MS,
                $"System initialization took {performanceTimer.ElapsedMilliseconds}ms, exceeds {MAX_CALCULATION_TIME_MS}ms limit");
        }

        [UnityTest]
        public IEnumerator PoliticalDataFlow_ValidatesCompleteFlow()
        {
            // Test: Data flows correctly through all political systems
            yield return StartCoroutine(InitializeTestEnvironment());

            performanceTimer.Restart();

            // Load Dutch political parties
            var parties = Resources.LoadAll<PoliticalParty>("Political/Parties");
            Assert.GreaterOrEqual(parties.Length, 15, "Should have all 15 Dutch political parties");

            // Run election simulation
            var electionResults = demoGameManager.SimulateElection();
            yield return new WaitUntil(() => electionResults.IsCompleted);

            // Validate election results
            Assert.IsNotNull(electionResults.Result, "Election results should not be null");
            Assert.AreEqual(150, electionResults.Result.TotalSeats, "Dutch parliament should have 150 seats");

            // Test coalition formation
            var coalitionResults = demoGameManager.FormCoalition(electionResults.Result);
            yield return new WaitUntil(() => coalitionResults.IsCompleted);

            // Validate coalition results
            Assert.IsNotNull(coalitionResults.Result, "Coalition results should not be null");
            Assert.GreaterOrEqual(coalitionResults.Result.TotalSeats, 76, "Coalition should have majority (76+ seats)");

            performanceTimer.Stop();
            Assert.Less(performanceTimer.ElapsedMilliseconds, MAX_CALCULATION_TIME_MS,
                $"Complete political flow took {performanceTimer.ElapsedMilliseconds}ms, exceeds {MAX_CALCULATION_TIME_MS}ms limit");
        }

        [UnityTest]
        public IEnumerator EventBusPerformance_ValidatesHighThroughput()
        {
            // Test: EventBus can handle high-throughput scenarios
            const int EVENT_COUNT = 10000;
            int eventsReceived = 0;

            // Subscribe to test events
            EventBus.Subscribe<PoliticalEvents.PartyDataUpdated>(evt => eventsReceived++);

            performanceTimer.Restart();

            // Publish many events rapidly
            for (int i = 0; i < EVENT_COUNT; i++)
            {
                EventBus.Publish(new PoliticalEvents.PartyDataUpdated { PartyId = i.ToString() });
                if (i % 1000 == 0) yield return null; // Yield occasionally to prevent frame drops
            }

            yield return new WaitForSeconds(0.1f); // Allow event processing

            performanceTimer.Stop();

            Assert.AreEqual(EVENT_COUNT, eventsReceived, "All events should be received");
            Assert.Less(performanceTimer.ElapsedMilliseconds, 1000,
                $"EventBus processing {EVENT_COUNT} events took {performanceTimer.ElapsedMilliseconds}ms, should be <1000ms");
        }

        [UnityTest]
        public IEnumerator MemoryUsage_StaysWithinLimits()
        {
            // Test: Memory usage stays within acceptable limits
            yield return StartCoroutine(InitializeTestEnvironment());

            long initialMemory = System.GC.GetTotalMemory(true);

            // Run intensive operations
            for (int i = 0; i < 100; i++)
            {
                var electionResults = demoGameManager.SimulateElection();
                yield return new WaitUntil(() => electionResults.IsCompleted);

                var coalitionResults = demoGameManager.FormCoalition(electionResults.Result);
                yield return new WaitUntil(() => coalitionResults.IsCompleted);

                if (i % 10 == 0)
                {
                    System.GC.Collect();
                    yield return null;
                }
            }

            long finalMemory = System.GC.GetTotalMemory(true);
            long memoryIncrease = finalMemory - initialMemory;

            Assert.Less(finalMemory, MAX_MEMORY_USAGE_BYTES,
                $"Memory usage {finalMemory / (1024 * 1024)}MB exceeds {MAX_MEMORY_USAGE_BYTES / (1024 * 1024)}MB limit");
            Assert.Less(memoryIncrease, MAX_MEMORY_USAGE_BYTES / 2,
                $"Memory increase {memoryIncrease / (1024 * 1024)}MB indicates potential memory leak");
        }

        [UnityTest]
        public IEnumerator FrameRate_MaintainsTargetFPS()
        {
            // Test: System maintains target FPS under load
            yield return StartCoroutine(InitializeTestEnvironment());

            float frameTimeSum = 0f;
            int frameCount = 0;
            const int TEST_DURATION_FRAMES = 300; // 5 seconds at 60 FPS

            // Run demo scenarios while measuring FPS
            var demoTask = demoGameManager.RunDemoScenario("FullCoalitionFormation");

            while (frameCount < TEST_DURATION_FRAMES && !demoTask.IsCompleted)
            {
                float frameTime = Time.unscaledDeltaTime;
                frameTimeSum += frameTime;
                frameCount++;
                yield return null;
            }

            float averageFPS = frameCount / frameTimeSum;
            Assert.GreaterOrEqual(averageFPS, TARGET_FPS * 0.9f,
                $"Average FPS {averageFPS:F1} below target {TARGET_FPS}. Minimum acceptable: {TARGET_FPS * 0.9f:F1}");
        }

        [UnityTest]
        public IEnumerator UIWindowManagement_ValidatesMultiWindow()
        {
            // Test: Desktop UI system manages multiple windows correctly
            yield return StartCoroutine(InitializeTestEnvironment());

            var windowManager = demoGameManager.GetComponent<DesktopWindowManager>();
            Assert.IsNotNull(windowManager, "DesktopWindowManager should be available");

            // Open multiple windows
            windowManager.OpenWindow(WindowType.ParliamentVisualization);
            windowManager.OpenWindow(WindowType.CoalitionBuilder);
            windowManager.OpenWindow(WindowType.PartyComparison);

            yield return new WaitForSeconds(0.5f);

            // Validate windows are properly managed
            Assert.AreEqual(3, windowManager.ActiveWindowCount, "Should have 3 active windows");
            Assert.IsTrue(windowManager.IsWindowOpen(WindowType.ParliamentVisualization), "Parliament window should be open");
            Assert.IsTrue(windowManager.IsWindowOpen(WindowType.CoalitionBuilder), "Coalition builder should be open");
            Assert.IsTrue(windowManager.IsWindowOpen(WindowType.PartyComparison), "Party comparison should be open");

            // Test window focus management
            windowManager.FocusWindow(WindowType.CoalitionBuilder);
            Assert.AreEqual(WindowType.CoalitionBuilder, windowManager.FocusedWindow, "Coalition builder should be focused");

            // Test window closing
            windowManager.CloseWindow(WindowType.PartyComparison);
            yield return new WaitForSeconds(0.1f);
            Assert.AreEqual(2, windowManager.ActiveWindowCount, "Should have 2 active windows after closing one");
        }

        [UnityTest]
        public IEnumerator DataAccuracy_ValidatesDutchElectionSystem()
        {
            // Test: Dutch election system produces accurate results
            yield return StartCoroutine(InitializeTestEnvironment());

            var electionSystem = demoGameManager.GetElectoralSystem();

            // Load 2023 Dutch election data for validation
            var parties = Resources.LoadAll<PoliticalParty>("Political/Parties");
            var testVotes = CreateTest2023VoteDistribution();

            performanceTimer.Restart();
            var results = electionSystem.CalculateSeats(testVotes);
            performanceTimer.Stop();

            // Validate against known 2023 results
            ValidateAgainst2023Results(results);

            Assert.Less(performanceTimer.ElapsedMilliseconds, 1000,
                "D'Hondt calculation should complete within 1 second");
        }

        [UnityTest]
        public IEnumerator UserTestingFramework_ValidatesMetricsCollection()
        {
            // Test: User testing framework collects metrics correctly
            yield return StartCoroutine(InitializeTestEnvironment());

            var testingFramework = demoGameManager.GetUserTestingFramework();
            Assert.IsNotNull(testingFramework, "User testing framework should be available");

            // Start test session
            var sessionId = testingFramework.StartTestSession("SystemIntegrationTest");
            Assert.IsNotNull(sessionId, "Test session should start successfully");

            // Record test metrics
            testingFramework.RecordInteraction("coalition_formation_start");
            testingFramework.RecordMetric("calculation_time", 2.5f);
            testingFramework.RecordUserFeedback("system_responsiveness", 4.2f);

            yield return new WaitForSeconds(1f);

            // End session and validate data collection
            var sessionData = testingFramework.EndTestSession(sessionId);
            Assert.IsNotNull(sessionData, "Session data should be collected");
            Assert.Greater(sessionData.InteractionCount, 0, "Should have recorded interactions");
            Assert.Greater(sessionData.MetricCount, 0, "Should have recorded metrics");
        }

        private IEnumerator InitializeTestEnvironment()
        {
            demoGameManager.InitializeSystems();
            yield return new WaitForSeconds(1f);

            // Ensure all systems are ready
            yield return new WaitUntil(() => demoGameManager.AreAllSystemsReady());
        }

        private Dictionary<string, int> CreateTest2023VoteDistribution()
        {
            // Simplified 2023 Dutch election vote distribution for testing
            return new Dictionary<string, int>
            {
                { "VVD", 2261326 },
                { "D66", 1496100 },
                { "PVV", 1367227 },
                { "CDA", 1007939 },
                { "SP", 595393 },
                { "PvdA", 580074 },
                { "GL", 1149245 },
                { "FvD", 507994 },
                { "PvdD", 416155 },
                { "CU", 348980 },
                { "Volt", 293765 },
                { "JA21", 257917 },
                { "SGP", 248395 },
                { "DENK", 168973 },
                { "50PLUS", 84733 }
            };
        }

        private void ValidateAgainst2023Results(Dictionary<string, int> calculatedResults)
        {
            // Known 2023 seat distribution for validation
            var expected2023Results = new Dictionary<string, int>
            {
                { "VVD", 34 },
                { "D66", 24 },
                { "PVV", 17 },
                { "CDA", 15 },
                { "SP", 9 },
                { "PvdA", 9 },
                { "GL", 8 },
                { "FvD", 8 },
                { "PvdD", 6 },
                { "CU", 5 },
                { "Volt", 3 },
                { "JA21", 3 },
                { "SGP", 3 },
                { "DENK", 3 },
                { "50PLUS", 1 }
            };

            foreach (var expected in expected2023Results)
            {
                if (calculatedResults.ContainsKey(expected.Key))
                {
                    Assert.AreEqual(expected.Value, calculatedResults[expected.Key],
                        $"Seat count for {expected.Key} should match 2023 results");
                }
            }
        }
    }

    /// <summary>
    /// Performance benchmark tests for critical system components
    /// </summary>
    [TestFixture]
    public class PerformanceBenchmarkTests
    {
        [Test]
        public void CoalitionFormation_BenchmarkPerformance()
        {
            var stopwatch = Stopwatch.StartNew();

            // Benchmark coalition formation with all 15 Dutch parties
            var parties = GenerateTestParties(15);
            var coalitionEngine = new CoalitionFormationEngine();

            var result = coalitionEngine.FindOptimalCoalition(parties, minimumSeats: 76);

            stopwatch.Stop();

            Assert.IsNotNull(result, "Coalition formation should produce a result");
            Assert.Less(stopwatch.ElapsedMilliseconds, 5000,
                $"Coalition formation took {stopwatch.ElapsedMilliseconds}ms, should be <5000ms");

            UnityEngine.Debug.Log($"Coalition formation benchmark: {stopwatch.ElapsedMilliseconds}ms");
        }

        [Test]
        public void DHondtCalculation_BenchmarkPerformance()
        {
            var stopwatch = Stopwatch.StartNew();

            // Benchmark D'Hondt calculation with realistic vote numbers
            var votes = GenerateTestVotes(15);
            var electionSystem = new DHondtElectionSystem();

            var result = electionSystem.CalculateSeats(votes, totalSeats: 150);

            stopwatch.Stop();

            Assert.IsNotNull(result, "D'Hondt calculation should produce results");
            Assert.Less(stopwatch.ElapsedMilliseconds, 1000,
                $"D'Hondt calculation took {stopwatch.ElapsedMilliseconds}ms, should be <1000ms");

            UnityEngine.Debug.Log($"D'Hondt calculation benchmark: {stopwatch.ElapsedMilliseconds}ms");
        }

        private List<PoliticalParty> GenerateTestParties(int count)
        {
            var parties = new List<PoliticalParty>();
            for (int i = 0; i < count; i++)
            {
                var party = ScriptableObject.CreateInstance<PoliticalParty>();
                party.PartyName = $"TestParty{i}";
                party.PartyAbbreviation = $"TP{i}";
                party.Seats = UnityEngine.Random.Range(1, 40);
                parties.Add(party);
            }
            return parties;
        }

        private Dictionary<string, int> GenerateTestVotes(int partyCount)
        {
            var votes = new Dictionary<string, int>();
            for (int i = 0; i < partyCount; i++)
            {
                votes[$"TestParty{i}"] = UnityEngine.Random.Range(50000, 3000000);
            }
            return votes;
        }
    }
}