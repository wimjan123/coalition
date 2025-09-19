using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Coalition.Core
{
    /// <summary>
    /// Main game controller for COALITION political simulation
    /// Coordinates political system, campaign mechanics, and AI integration
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Game Systems")]
        [SerializeField] private PoliticalSystem politicalSystem;
        [SerializeField] private CampaignSystem campaignSystem;
        [SerializeField] private AIResponseSystem aiSystem;
        [SerializeField] private UIManager uiManager;

        [Header("Game State")]
        [SerializeField] private GamePhase currentPhase = GamePhase.PreElection;
        [SerializeField] private float gameSpeedMultiplier = 1.0f;
        [SerializeField] private bool isPaused = false;

        // Events
        public System.Action<GamePhase> OnPhaseChanged;
        public System.Action<float> OnGameSpeedChanged;
        public System.Action<bool> OnPauseStateChanged;

        // Singleton pattern for easy access
        public static GameManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeGame();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private async void InitializeGame()
        {
            Debug.Log("[GameManager] Initializing COALITION political simulation...");

            // Initialize core systems
            await InitializePoliticalSystem();
            await InitializeCampaignSystem();
            await InitializeAISystem();
            InitializeUI();

            // Start pre-election phase
            SetGamePhase(GamePhase.PreElection);

            Debug.Log("[GameManager] Game initialization complete!");
        }

        private async Task InitializePoliticalSystem()
        {
            // Load Dutch political parties and setup electoral system
            if (politicalSystem != null)
            {
                await politicalSystem.Initialize();
                Debug.Log("[GameManager] Political system initialized");
            }
        }

        private async Task InitializeCampaignSystem()
        {
            // Setup campaign mechanics and social media integration
            if (campaignSystem != null)
            {
                await campaignSystem.Initialize();
                Debug.Log("[GameManager] Campaign system initialized");
            }
        }

        private async Task InitializeAISystem()
        {
            // Connect to NVIDIA NIM local LLM
            if (aiSystem != null)
            {
                await aiSystem.Initialize();
                Debug.Log("[GameManager] AI system initialized");
            }
        }

        private void InitializeUI()
        {
            // Setup desktop-style UI framework
            if (uiManager != null)
            {
                uiManager.Initialize();
                Debug.Log("[GameManager] UI system initialized");
            }
        }

        public void SetGamePhase(GamePhase newPhase)
        {
            if (currentPhase != newPhase)
            {
                Debug.Log($"[GameManager] Phase transition: {currentPhase} → {newPhase}");
                currentPhase = newPhase;
                OnPhaseChanged?.Invoke(newPhase);

                // Handle phase-specific logic
                switch (newPhase)
                {
                    case GamePhase.PreElection:
                        StartPreElectionPhase();
                        break;
                    case GamePhase.Election:
                        StartElectionPhase();
                        break;
                    case GamePhase.CoalitionFormation:
                        StartCoalitionFormationPhase();
                        break;
                    case GamePhase.Governance:
                        StartGovernancePhase();
                        break;
                }
            }
        }

        private void StartPreElectionPhase()
        {
            // Enable campaign mechanics: social media, debates, rallies
            campaignSystem?.EnableCampaignMode();
            Debug.Log("[GameManager] Pre-election campaign phase started");
        }

        private void StartElectionPhase()
        {
            // Process election results using D'Hondt method
            politicalSystem?.ProcessElection();
            Debug.Log("[GameManager] Election phase started");
        }

        private void StartCoalitionFormationPhase()
        {
            // Begin coalition negotiation process
            politicalSystem?.StartCoalitionFormation();
            Debug.Log("[GameManager] Coalition formation phase started");
        }

        private void StartGovernancePhase()
        {
            // Enter governing phase with policy implementation
            politicalSystem?.StartGovernance();
            Debug.Log("[GameManager] Governance phase started");
        }

        public void SetGameSpeed(float multiplier)
        {
            gameSpeedMultiplier = Mathf.Clamp(multiplier, 0.1f, 10.0f);
            Time.timeScale = isPaused ? 0 : gameSpeedMultiplier;
            OnGameSpeedChanged?.Invoke(gameSpeedMultiplier);
            Debug.Log($"[GameManager] Game speed set to {gameSpeedMultiplier}x");
        }

        public void TogglePause()
        {
            isPaused = !isPaused;
            Time.timeScale = isPaused ? 0 : gameSpeedMultiplier;
            OnPauseStateChanged?.Invoke(isPaused);
            Debug.Log($"[GameManager] Game {(isPaused ? "paused" : "resumed")}");
        }

        // Getters for other systems
        public GamePhase CurrentPhase => currentPhase;
        public float GameSpeed => gameSpeedMultiplier;
        public bool IsPaused => isPaused;
    }

    public enum GamePhase
    {
        PreElection,
        Election,
        CoalitionFormation,
        Governance
    }
}