using System.Threading.Tasks;
using UnityEngine;
using Coalition.Political;

namespace Coalition.Tests.Mocks
{
    /// <summary>
    /// Mock implementation of PoliticalSystem for isolated testing
    /// Provides controllable behavior for testing game flow without dependencies
    /// </summary>
    public class MockPoliticalSystem : MonoBehaviour
    {
        // Test control flags
        public bool InitializationSuccessful { get; set; } = true;
        public bool ElectionProcessingSuccessful { get; set; } = true;
        public bool CoalitionFormationSuccessful { get; set; } = true;
        public bool GovernanceStartSuccessful { get; set; } = true;

        // Mock state tracking
        public bool IsInitialized { get; private set; } = false;
        public bool IsElectionProcessed { get; private set; } = false;
        public bool IsCoalitionFormationStarted { get; private set; } = false;
        public bool IsGovernanceStarted { get; private set; } = false;

        // Mock delay settings for testing async behavior
        public int InitializationDelayMs { get; set; } = 0;
        public int ElectionProcessingDelayMs { get; set; } = 0;
        public int CoalitionFormationDelayMs { get; set; } = 0;

        // Call counters for test verification
        public int InitializeCallCount { get; private set; } = 0;
        public int ProcessElectionCallCount { get; private set; } = 0;
        public int StartCoalitionFormationCallCount { get; private set; } = 0;
        public int StartGovernanceCallCount { get; private set; } = 0;

        // Mock Dutch political parties data
        public PoliticalParty[] MockParties { get; private set; }

        // Mock election results
        public ElectionResult MockElectionResult { get; set; }

        private void Awake()
        {
            CreateMockPoliticalData();
        }

        public async Task Initialize()
        {
            InitializeCallCount++;

            if (InitializationDelayMs > 0)
            {
                await Task.Delay(InitializationDelayMs);
            }

            if (!InitializationSuccessful)
            {
                throw new System.Exception("Mock initialization failure");
            }

            IsInitialized = true;
            Debug.Log("[MockPoliticalSystem] Initialized with mock Dutch political parties");
        }

        public void ProcessElection()
        {
            ProcessElectionCallCount++;

            if (!ElectionProcessingSuccessful)
            {
                throw new System.Exception("Mock election processing failure");
            }

            IsElectionProcessed = true;

            // Simulate realistic Dutch election results
            MockElectionResult = CreateMockElectionResult();

            Debug.Log("[MockPoliticalSystem] Mock election processed using D'Hondt method");
        }

        public void StartCoalitionFormation()
        {
            StartCoalitionFormationCallCount++;

            if (!CoalitionFormationSuccessful)
            {
                throw new System.Exception("Mock coalition formation failure");
            }

            IsCoalitionFormationStarted = true;
            Debug.Log("[MockPoliticalSystem] Mock coalition formation negotiations started");
        }

        public void StartGovernance()
        {
            StartGovernanceCallCount++;

            if (!GovernanceStartSuccessful)
            {
                throw new System.Exception("Mock governance start failure");
            }

            IsGovernanceStarted = true;
            Debug.Log("[MockPoliticalSystem] Mock governance phase initiated");
        }

        public void Reset()
        {
            IsInitialized = false;
            IsElectionProcessed = false;
            IsCoalitionFormationStarted = false;
            IsGovernanceStarted = false;

            InitializeCallCount = 0;
            ProcessElectionCallCount = 0;
            StartCoalitionFormationCallCount = 0;
            StartGovernanceCallCount = 0;
        }

        private void CreateMockPoliticalData()
        {
            // Create realistic mock Dutch political parties for testing
            MockParties = new PoliticalParty[]
            {
                CreateMockParty("VVD", "Volkspartij voor Vrijheid en Democratie", "Liberal", 15.2f),
                CreateMockParty("PVV", "Partij voor de Vrijheid", "Right-wing populist", 13.1f),
                CreateMockParty("CDA", "Christen-Democratisch Appèl", "Christian democratic", 12.4f),
                CreateMockParty("D66", "Democraten 66", "Social liberal", 11.8f),
                CreateMockParty("GL", "GroenLinks", "Green politics", 9.2f),
                CreateMockParty("SP", "Socialistische Partij", "Democratic socialist", 8.7f),
                CreateMockParty("PvdA", "Partij van de Arbeid", "Social democratic", 7.9f),
                CreateMockParty("CU", "ChristenUnie", "Christian democratic", 4.2f),
                CreateMockParty("PvdD", "Partij voor de Dieren", "Animal rights", 3.8f),
                CreateMockParty("50PLUS", "50PLUS", "Pensioners' interests", 2.1f)
            };
        }

        private PoliticalParty CreateMockParty(string abbreviation, string fullName, string ideology, float supportPercentage)
        {
            var party = ScriptableObject.CreateInstance<PoliticalParty>();
            // Note: This is a simplified mock - actual PoliticalParty fields may differ
            return party;
        }

        private ElectionResult CreateMockElectionResult()
        {
            return new ElectionResult
            {
                TotalVotes = 10500000,
                Turnout = 82.6f,
                PartyResults = MockParties,
                ElectionDate = System.DateTime.Now
            };
        }
    }

    /// <summary>
    /// Mock election result data structure for testing
    /// </summary>
    public class ElectionResult
    {
        public int TotalVotes { get; set; }
        public float Turnout { get; set; }
        public PoliticalParty[] PartyResults { get; set; }
        public System.DateTime ElectionDate { get; set; }
    }
}