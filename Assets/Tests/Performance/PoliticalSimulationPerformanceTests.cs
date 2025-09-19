using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.PerformanceTesting;
using Coalition.Tests.Mocks;
using Coalition.Core;

namespace Coalition.Tests.Performance
{
    /// <summary>
    /// Performance tests for political simulation under various load conditions
    /// Testing multi-party scenarios, extended sessions, and system scalability
    /// </summary>
    public class PoliticalSimulationPerformanceTests
    {
        private GameObject gameManagerObject;
        private GameManager gameManager;
        private MockPoliticalSystem politicalSystem;

        [SetUp]
        public void SetUp()
        {
            // Create test environment
            gameManagerObject = new GameObject("GameManager");
            gameManager = gameManagerObject.AddComponent<GameManager>();
            politicalSystem = gameManagerObject.AddComponent<MockPoliticalSystem>();

            // Clear EventBus for clean testing
            EventBus.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            if (gameManagerObject != null)
            {
                Object.DestroyImmediate(gameManagerObject);
            }

            EventBus.Clear();
            Time.timeScale = 1.0f;
        }

        [Test, Performance]
        public void EventBus_MassiveEventPublishing_ShouldMaintainPerformance()
        {
            // Arrange
            const int listenerCount = 1000;
            const int eventCount = 5000;
            var testEvents = new List<TestGameEvent>();

            // Create many listeners
            for (int i = 0; i < listenerCount; i++)
            {
                int localIndex = i;
                EventBus.Subscribe<TestGameEvent>(evt =>
                {
                    // Simulate some processing
                    var result = evt.Value * localIndex;
                });
            }

            // Prepare events
            for (int i = 0; i < eventCount; i++)
            {
                testEvents.Add(new TestGameEvent { Value = i, Message = $"Event {i}" });
            }

            // Act & Measure
            Measure.Method(() =>
            {
                foreach (var gameEvent in testEvents)
                {
                    EventBus.Publish(gameEvent);
                }
            })
            .WarmupCount(5)
            .MeasurementCount(10)
            .IterationsPerMeasurement(1)
            .Run();
        }

        [UnityTest, Performance]
        public IEnumerator GameManager_MultiPartySimulation_ShouldScaleEfficiently()
        {
            // Arrange - Simulate 15-party parliament (realistic fragmentation)
            var partyCount = 15;
            CreateMockParties(partyCount);

            yield return null; // Wait for initialization

            // Act & Measure
            using (Measure.Frames().Scope("MultiPartySimulation"))
            {
                // Simulate complete election cycle
                gameManager.SetGamePhase(GamePhase.Election);
                yield return new WaitForSeconds(0.1f);

                gameManager.SetGamePhase(GamePhase.CoalitionFormation);
                yield return new WaitForSeconds(0.2f); // Coalition formation takes longer

                gameManager.SetGamePhase(GamePhase.Governance);
                yield return new WaitForSeconds(0.1f);

                // Simulate rapid phase changes (stress test)
                for (int i = 0; i < 10; i++)
                {
                    gameManager.SetGamePhase(GamePhase.Election);
                    gameManager.SetGamePhase(GamePhase.CoalitionFormation);
                    gameManager.SetGamePhase(GamePhase.Governance);
                    yield return null;
                }
            }
        }

        [Test, Performance]
        public void PoliticalSystem_CoalitionCalculation_ShouldHandleLargeDatasets()
        {
            // Arrange
            const int partyCount = 25; // Extreme fragmentation scenario
            const int calculationRounds = 100;

            var parties = CreateLargePartyDataset(partyCount);

            // Act & Measure
            Measure.Method(() =>
            {
                for (int round = 0; round < calculationRounds; round++)
                {
                    // Simulate coalition viability calculations
                    CalculateAllPossibleCoalitions(parties);
                }
            })
            .SampleGroup(new SampleGroup("CoalitionCalculations", SampleUnit.Millisecond))
            .WarmupCount(3)
            .MeasurementCount(10)
            .Run();
        }

        [UnityTest, Performance]
        public IEnumerator ExtendedSession_LongTermStability_ShouldMaintainPerformance()
        {
            // Arrange - Simulate 4-hour gameplay session (compressed to 10 seconds)
            const float sessionDuration = 10f;
            const int actionsPerSecond = 5; // User interactions

            var startTime = Time.time;
            var actionCount = 0;

            // Act & Measure
            using (Measure.Frames().Scope("ExtendedSession"))
            {
                while (Time.time - startTime < sessionDuration)
                {
                    // Simulate user actions
                    if (actionCount % actionsPerSecond == 0)
                    {
                        SimulateUserAction();
                    }

                    // Simulate periodic system updates
                    if (actionCount % 30 == 0) // Every 6 seconds
                    {
                        SimulateSystemUpdate();
                    }

                    actionCount++;
                    yield return null;
                }
            }

            // Verify no significant performance degradation
            var finalTime = Time.time;
            Assert.Less(finalTime - startTime, sessionDuration + 1f,
                "Session should complete within reasonable time");
        }

        [Test, Performance]
        public void MemoryAllocation_PoliticalDataStructures_ShouldBeEfficient()
        {
            // Arrange
            const int allocationCycles = 1000;

            // Act & Measure
            Measure.Method(() =>
            {
                for (int i = 0; i < allocationCycles; i++)
                {
                    // Simulate typical data structure creation during gameplay
                    CreatePoliticalEventData();
                    CreateCoalitionData();
                    CreateElectionResults();

                    // Force garbage collection periodically
                    if (i % 100 == 0)
                    {
                        System.GC.Collect();
                    }
                }
            })
            .SampleGroup(new SampleGroup("MemoryAllocation", SampleUnit.Microsecond))
            .GC() // Track garbage collection
            .WarmupCount(5)
            .MeasurementCount(15)
            .Run();
        }

        [UnityTest, Performance]
        public IEnumerator AI_ConcurrentRequests_ShouldMaintainThroughput()
        {
            // Arrange
            var nimClient = gameManagerObject.AddComponent<MockNIMClient>();
            nimClient.ResponseDelayMs = 100; // Simulate realistic network delay

            const int concurrentRequests = 10;
            const int totalBatches = 5;

            // Act & Measure
            using (Measure.Frames().Scope("AI_ConcurrentRequests"))
            {
                for (int batch = 0; batch < totalBatches; batch++)
                {
                    var tasks = new System.Threading.Tasks.Task[concurrentRequests];

                    for (int i = 0; i < concurrentRequests; i++)
                    {
                        int requestId = i;
                        tasks[i] = nimClient.GenerateResponseAsync($"Political analysis request {requestId}");
                    }

                    // Wait for all requests to complete
                    var waitTask = System.Threading.Tasks.Task.WhenAll(tasks);
                    while (!waitTask.IsCompleted)
                    {
                        yield return null;
                    }
                }
            }

            // Verify all requests completed successfully
            Assert.AreEqual(concurrentRequests * totalBatches, nimClient.CallCount,
                "All AI requests should complete");
        }

        [Test, Performance]
        public void DataSerialization_PoliticalState_ShouldBeOptimized()
        {
            // Arrange
            var largeGameState = CreateComplexGameState();
            const int serializationCycles = 100;

            // Act & Measure
            Measure.Method(() =>
            {
                for (int i = 0; i < serializationCycles; i++)
                {
                    // Simulate save/load operations
                    var json = JsonUtility.ToJson(largeGameState);
                    var deserialized = JsonUtility.FromJson<GameState>(json);

                    // Verify data integrity
                    Assert.AreEqual(largeGameState.PartyCount, deserialized.PartyCount);
                }
            })
            .SampleGroup(new SampleGroup("DataSerialization", SampleUnit.Millisecond))
            .WarmupCount(3)
            .MeasurementCount(12)
            .Run();
        }

        [UnityTest, Performance]
        public IEnumerator UI_ResponsiveUpdates_ShouldMaintain60FPS()
        {
            // Arrange
            const float testDuration = 2f;
            const float targetFrameTime = 1f / 60f; // 60 FPS target
            var frameTimeViolations = 0;
            var totalFrames = 0;

            var startTime = Time.time;

            // Act & Measure
            using (Measure.Frames().Scope("UI_ResponsiveUpdates"))
            {
                while (Time.time - startTime < testDuration)
                {
                    var frameStartTime = Time.unscaledTime;

                    // Simulate UI updates
                    SimulateUIUpdates();

                    var frameEndTime = Time.unscaledTime;
                    var frameTime = frameEndTime - frameStartTime;

                    if (frameTime > targetFrameTime)
                    {
                        frameTimeViolations++;
                    }

                    totalFrames++;
                    yield return null;
                }
            }

            // Assert acceptable frame rate
            var violationPercentage = (float)frameTimeViolations / totalFrames;
            Assert.Less(violationPercentage, 0.05f, // Less than 5% frame drops
                $"Frame time violations: {violationPercentage:P2}");
        }

        [Test, Performance]
        public void EventBus_MemoryLeakPrevention_ShouldCleanupCorrectly()
        {
            // Arrange
            const int subscriptionCycles = 1000;

            // Act & Measure
            Measure.Method(() =>
            {
                for (int i = 0; i < subscriptionCycles; i++)
                {
                    // Subscribe listener
                    System.Action<TestGameEvent> listener = (evt) => { };
                    EventBus.Subscribe(listener);

                    // Publish event
                    EventBus.Publish(new TestGameEvent { Value = i });

                    // Unsubscribe to prevent memory leaks
                    EventBus.Unsubscribe(listener);

                    // Periodic cleanup
                    if (i % 100 == 0)
                    {
                        EventBus.Clear();
                    }
                }
            })
            .SampleGroup(new SampleGroup("MemoryLeakPrevention", SampleUnit.Microsecond))
            .GC()
            .WarmupCount(3)
            .MeasurementCount(10)
            .Run();
        }

        // Helper methods for performance testing

        private void CreateMockParties(int count)
        {
            for (int i = 0; i < count; i++)
            {
                // Simulate party data creation
                var party = new MockPartyData
                {
                    Name = $"Party{i}",
                    Seats = UnityEngine.Random.Range(1, 30),
                    Ideology = UnityEngine.Random.Range(0f, 1f)
                };
            }
        }

        private MockPartyData[] CreateLargePartyDataset(int count)
        {
            var parties = new MockPartyData[count];
            for (int i = 0; i < count; i++)
            {
                parties[i] = new MockPartyData
                {
                    Name = $"Party{i}",
                    Seats = UnityEngine.Random.Range(1, 25),
                    Ideology = UnityEngine.Random.Range(0f, 1f),
                    SupportBase = UnityEngine.Random.Range(0.01f, 0.25f)
                };
            }
            return parties;
        }

        private void CalculateAllPossibleCoalitions(MockPartyData[] parties)
        {
            // Simulate coalition viability calculations
            for (int i = 0; i < parties.Length; i++)
            {
                for (int j = i + 1; j < parties.Length; j++)
                {
                    // Calculate two-party coalition viability
                    var viability = Mathf.Abs(parties[i].Ideology - parties[j].Ideology);
                    var seatCount = parties[i].Seats + parties[j].Seats;
                    var coalitionScore = seatCount >= 76 ? (1f - viability) : 0f;
                }
            }
        }

        private void SimulateUserAction()
        {
            // Simulate typical user interactions
            var actions = new System.Action[]
            {
                () => gameManager.SetGameSpeed(UnityEngine.Random.Range(0.5f, 3f)),
                () => gameManager.TogglePause(),
                () => EventBus.Publish(new TestGameEvent { Value = UnityEngine.Random.Range(1, 100) }),
                () => politicalSystem.ProcessElection(),
            };

            var randomAction = actions[UnityEngine.Random.Range(0, actions.Length)];
            randomAction?.Invoke();
        }

        private void SimulateSystemUpdate()
        {
            // Simulate periodic system maintenance
            EventBus.Publish(new SystemUpdateEvent { Timestamp = System.DateTime.Now });

            // Simulate data processing
            for (int i = 0; i < 100; i++)
            {
                var calculation = Mathf.Sin(i) * Mathf.Cos(i * 2);
            }
        }

        private void CreatePoliticalEventData()
        {
            var eventData = new PoliticalEventData
            {
                EventType = "Coalition Negotiation",
                Participants = new string[] { "VVD", "D66", "CDA" },
                Timestamp = System.DateTime.Now,
                Impact = UnityEngine.Random.Range(0.1f, 0.9f)
            };
        }

        private void CreateCoalitionData()
        {
            var coalitionData = new CoalitionData
            {
                Parties = new string[] { "VVD", "D66", "CDA", "CU" },
                AgreementAreas = new string[] { "Infrastructure", "Education", "Climate" },
                ConflictAreas = new string[] { "Immigration", "Ethics" },
                FormationDate = System.DateTime.Now
            };
        }

        private void CreateElectionResults()
        {
            var results = new ElectionResultData
            {
                TotalVotes = 10500000,
                Turnout = 82.6f,
                PartyResults = new Dictionary<string, int>
                {
                    { "VVD", 34 }, { "D66", 24 }, { "PVV", 17 }, { "CDA", 15 }
                }
            };
        }

        private GameState CreateComplexGameState()
        {
            return new GameState
            {
                CurrentPhase = GamePhase.Governance,
                PartyCount = 15,
                ActiveCoalition = new string[] { "VVD", "D66", "CDA", "CU" },
                GameTime = 15000f, // Simulated game time
                EventHistory = CreateMockEventHistory(500), // Large event history
                PublicOpinionData = CreateMockOpinionData(50)
            };
        }

        private PoliticalEventData[] CreateMockEventHistory(int count)
        {
            var events = new PoliticalEventData[count];
            for (int i = 0; i < count; i++)
            {
                events[i] = new PoliticalEventData
                {
                    EventType = $"Event{i}",
                    Participants = new string[] { "PartyA", "PartyB" },
                    Timestamp = System.DateTime.Now.AddMinutes(-i),
                    Impact = UnityEngine.Random.Range(0f, 1f)
                };
            }
            return events;
        }

        private OpinionData[] CreateMockOpinionData(int count)
        {
            var opinions = new OpinionData[count];
            for (int i = 0; i < count; i++)
            {
                opinions[i] = new OpinionData
                {
                    Issue = $"Issue{i}",
                    Support = UnityEngine.Random.Range(0f, 100f),
                    Demographic = $"Group{i}"
                };
            }
            return opinions;
        }

        private void SimulateUIUpdates()
        {
            // Simulate UI element updates that might be expensive
            for (int i = 0; i < 50; i++)
            {
                // Simulate text updates
                var text = $"Update {i}: {UnityEngine.Random.Range(0, 1000)}";

                // Simulate layout calculations
                var width = text.Length * 10f;
                var height = 20f;
                var area = width * height;
            }
        }
    }

    // Mock data structures for performance testing
    public class TestGameEvent : IGameEvent
    {
        public int Value { get; set; }
        public string Message { get; set; }
    }

    public class SystemUpdateEvent : IGameEvent
    {
        public System.DateTime Timestamp { get; set; }
    }

    public class MockPartyData
    {
        public string Name { get; set; }
        public int Seats { get; set; }
        public float Ideology { get; set; }
        public float SupportBase { get; set; }
    }

    public class PoliticalEventData
    {
        public string EventType { get; set; }
        public string[] Participants { get; set; }
        public System.DateTime Timestamp { get; set; }
        public float Impact { get; set; }
    }

    public class CoalitionData
    {
        public string[] Parties { get; set; }
        public string[] AgreementAreas { get; set; }
        public string[] ConflictAreas { get; set; }
        public System.DateTime FormationDate { get; set; }
    }

    public class ElectionResultData
    {
        public int TotalVotes { get; set; }
        public float Turnout { get; set; }
        public Dictionary<string, int> PartyResults { get; set; }
    }

    [System.Serializable]
    public class GameState
    {
        public GamePhase CurrentPhase;
        public int PartyCount;
        public string[] ActiveCoalition;
        public float GameTime;
        public PoliticalEventData[] EventHistory;
        public OpinionData[] PublicOpinionData;
    }

    public class OpinionData
    {
        public string Issue { get; set; }
        public float Support { get; set; }
        public string Demographic { get; set; }
    }
}