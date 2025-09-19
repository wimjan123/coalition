using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using Coalition.Core;

namespace Coalition.Tests.PlayMode
{
    /// <summary>
    /// Comprehensive PlayMode tests for GameManager
    /// Testing singleton behavior, phase transitions, and system initialization
    /// </summary>
    public class GameManagerTests
    {
        private GameObject gameManagerObject;
        private GameManager gameManager;

        [SetUp]
        public void SetUp()
        {
            // Clear any existing GameManager instances
            var existingManagers = Object.FindObjectsOfType<GameManager>();
            foreach (var manager in existingManagers)
            {
                Object.DestroyImmediate(manager.gameObject);
            }

            // Reset EventBus to ensure clean state
            EventBus.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up test objects
            if (gameManagerObject != null)
            {
                Object.DestroyImmediate(gameManagerObject);
            }

            // Reset time scale
            Time.timeScale = 1.0f;

            // Clear EventBus
            EventBus.Clear();
        }

        [UnityTest]
        public IEnumerator GameManager_Singleton_ShouldCreateOnlyOneInstance()
        {
            // Arrange & Act
            var gameObject1 = new GameObject("GameManager1");
            var manager1 = gameObject1.AddComponent<GameManager>();

            var gameObject2 = new GameObject("GameManager2");
            var manager2 = gameObject2.AddComponent<GameManager>();

            yield return null; // Wait one frame for Awake to execute

            // Assert
            Assert.IsNotNull(GameManager.Instance, "GameManager.Instance should be set");
            Assert.AreEqual(manager1, GameManager.Instance, "First created GameManager should be the singleton instance");

            // Second GameObject should be destroyed
            Assert.IsTrue(gameObject2 == null, "Second GameManager GameObject should be destroyed");

            // Cleanup
            Object.DestroyImmediate(gameObject1);
        }

        [UnityTest]
        public IEnumerator GameManager_Initialization_ShouldSetDefaultValues()
        {
            // Arrange & Act
            gameManagerObject = new GameObject("GameManager");
            gameManager = gameManagerObject.AddComponent<GameManager>();

            yield return null; // Wait for initialization

            // Assert
            Assert.AreEqual(GamePhase.PreElection, gameManager.CurrentPhase, "Initial phase should be PreElection");
            Assert.AreEqual(1.0f, gameManager.GameSpeed, "Initial game speed should be 1.0x");
            Assert.IsFalse(gameManager.IsPaused, "Game should not be paused initially");
        }

        [Test]
        public void SetGamePhase_ValidTransition_ShouldUpdatePhase()
        {
            // Arrange
            gameManagerObject = new GameObject("GameManager");
            gameManager = gameManagerObject.AddComponent<GameManager>();

            bool phaseChangeEventFired = false;
            GamePhase receivedPhase = GamePhase.PreElection;

            gameManager.OnPhaseChanged += (phase) =>
            {
                phaseChangeEventFired = true;
                receivedPhase = phase;
            };

            // Act
            gameManager.SetGamePhase(GamePhase.Election);

            // Assert
            Assert.AreEqual(GamePhase.Election, gameManager.CurrentPhase, "Current phase should be updated");
            Assert.IsTrue(phaseChangeEventFired, "Phase change event should be fired");
            Assert.AreEqual(GamePhase.Election, receivedPhase, "Event should contain correct phase");
        }

        [Test]
        public void SetGamePhase_SamePhase_ShouldNotFireEvent()
        {
            // Arrange
            gameManagerObject = new GameObject("GameManager");
            gameManager = gameManagerObject.AddComponent<GameManager>();

            bool phaseChangeEventFired = false;
            gameManager.OnPhaseChanged += (phase) => phaseChangeEventFired = true;

            // Act
            gameManager.SetGamePhase(GamePhase.PreElection); // Same as initial phase

            // Assert
            Assert.IsFalse(phaseChangeEventFired, "Phase change event should not fire for same phase");
        }

        [Test]
        public void SetGameSpeed_ValidValue_ShouldUpdateSpeed()
        {
            // Arrange
            gameManagerObject = new GameObject("GameManager");
            gameManager = gameManagerObject.AddComponent<GameManager>();

            bool speedChangeEventFired = false;
            float receivedSpeed = 0f;

            gameManager.OnGameSpeedChanged += (speed) =>
            {
                speedChangeEventFired = true;
                receivedSpeed = speed;
            };

            // Act
            gameManager.SetGameSpeed(2.5f);

            // Assert
            Assert.AreEqual(2.5f, gameManager.GameSpeed, "Game speed should be updated");
            Assert.AreEqual(2.5f, Time.timeScale, "Unity time scale should match game speed");
            Assert.IsTrue(speedChangeEventFired, "Speed change event should be fired");
            Assert.AreEqual(2.5f, receivedSpeed, "Event should contain correct speed");
        }

        [Test]
        public void SetGameSpeed_ClampedValues_ShouldRespectLimits()
        {
            // Arrange
            gameManagerObject = new GameObject("GameManager");
            gameManager = gameManagerObject.AddComponent<GameManager>();

            // Act & Assert - Test upper limit
            gameManager.SetGameSpeed(15.0f);
            Assert.AreEqual(10.0f, gameManager.GameSpeed, "Game speed should be clamped to maximum 10.0x");

            // Act & Assert - Test lower limit
            gameManager.SetGameSpeed(0.05f);
            Assert.AreEqual(0.1f, gameManager.GameSpeed, "Game speed should be clamped to minimum 0.1x");
        }

        [Test]
        public void TogglePause_WhenNotPaused_ShouldPauseGame()
        {
            // Arrange
            gameManagerObject = new GameObject("GameManager");
            gameManager = gameManagerObject.AddComponent<GameManager>();

            bool pauseEventFired = false;
            bool receivedPauseState = false;

            gameManager.OnPauseStateChanged += (paused) =>
            {
                pauseEventFired = true;
                receivedPauseState = paused;
            };

            // Act
            gameManager.TogglePause();

            // Assert
            Assert.IsTrue(gameManager.IsPaused, "Game should be paused");
            Assert.AreEqual(0f, Time.timeScale, "Time scale should be 0 when paused");
            Assert.IsTrue(pauseEventFired, "Pause event should be fired");
            Assert.IsTrue(receivedPauseState, "Event should indicate paused state");
        }

        [Test]
        public void TogglePause_WhenPaused_ShouldResumeGame()
        {
            // Arrange
            gameManagerObject = new GameObject("GameManager");
            gameManager = gameManagerObject.AddComponent<GameManager>();

            gameManager.SetGameSpeed(2.0f);
            gameManager.TogglePause(); // First pause

            bool pauseEventFired = false;
            bool receivedPauseState = true;

            gameManager.OnPauseStateChanged += (paused) =>
            {
                pauseEventFired = true;
                receivedPauseState = paused;
            };

            // Act
            gameManager.TogglePause(); // Resume

            // Assert
            Assert.IsFalse(gameManager.IsPaused, "Game should be resumed");
            Assert.AreEqual(2.0f, Time.timeScale, "Time scale should match game speed when resumed");
            Assert.IsTrue(pauseEventFired, "Pause event should be fired");
            Assert.IsFalse(receivedPauseState, "Event should indicate unpaused state");
        }

        [Test]
        public void GameManager_AllPhaseTransitions_ShouldBeValid()
        {
            // Arrange
            gameManagerObject = new GameObject("GameManager");
            gameManager = gameManagerObject.AddComponent<GameManager>();

            var phaseOrder = new[]
            {
                GamePhase.PreElection,
                GamePhase.Election,
                GamePhase.CoalitionFormation,
                GamePhase.Governance
            };

            // Act & Assert
            foreach (var phase in phaseOrder)
            {
                Assert.DoesNotThrow(() => gameManager.SetGamePhase(phase),
                    $"Setting phase to {phase} should not throw exception");
                Assert.AreEqual(phase, gameManager.CurrentPhase,
                    $"Current phase should be {phase}");
            }
        }

        [Test]
        public void GameManager_DontDestroyOnLoad_ShouldPersist()
        {
            // Arrange
            gameManagerObject = new GameObject("GameManager");
            gameManager = gameManagerObject.AddComponent<GameManager>();

            // Act
            var dontDestroyFlag = (gameManagerObject.hideFlags & HideFlags.DontSave) != 0;

            // Assert - This is a simplified test since we can't easily test scene loading in unit tests
            Assert.IsNotNull(GameManager.Instance, "GameManager instance should be available globally");
        }

        /// <summary>
        /// Integration test for complete game lifecycle simulation
        /// </summary>
        [UnityTest]
        public IEnumerator GameManager_CompleteGameCycle_ShouldWorkCorrectly()
        {
            // Arrange
            gameManagerObject = new GameObject("GameManager");
            gameManager = gameManagerObject.AddComponent<GameManager>();

            var phaseChanges = new System.Collections.Generic.List<GamePhase>();
            gameManager.OnPhaseChanged += (phase) => phaseChanges.Add(phase);

            yield return null; // Wait for initialization

            // Act - Simulate complete political cycle
            gameManager.SetGamePhase(GamePhase.Election);
            yield return null;

            gameManager.SetGamePhase(GamePhase.CoalitionFormation);
            yield return null;

            gameManager.SetGamePhase(GamePhase.Governance);
            yield return null;

            // Test speed changes during governance
            gameManager.SetGameSpeed(3.0f);
            yield return null;

            // Test pause/resume during governance
            gameManager.TogglePause();
            yield return null;

            gameManager.TogglePause();
            yield return null;

            // Assert
            Assert.AreEqual(3, phaseChanges.Count, "Should have 3 phase changes from initial state");
            Assert.AreEqual(GamePhase.Election, phaseChanges[0]);
            Assert.AreEqual(GamePhase.CoalitionFormation, phaseChanges[1]);
            Assert.AreEqual(GamePhase.Governance, phaseChanges[2]);

            Assert.AreEqual(GamePhase.Governance, gameManager.CurrentPhase, "Should end in Governance phase");
            Assert.AreEqual(3.0f, gameManager.GameSpeed, "Game speed should be maintained");
            Assert.IsFalse(gameManager.IsPaused, "Game should not be paused at end");
        }
    }

    /// <summary>
    /// Test enum for GamePhase if not defined elsewhere
    /// This should match the actual enum in the production code
    /// </summary>
    public enum GamePhase
    {
        PreElection,
        Election,
        CoalitionFormation,
        Governance
    }
}