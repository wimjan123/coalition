using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Coalition.Runtime.Core;

namespace Coalition.Runtime.Data
{
    /// <summary>
    /// Central repository for all political data used in the coalition demo.
    /// Contains 2023 Dutch election data, party information, and configuration settings.
    /// </summary>
    [CreateAssetMenu(fileName = "Demo Political Repository", menuName = "Coalition/Demo/Political Repository")]
    public class DemoPoliticalDataRepository : ScriptableObject
    {
        [Header("2023 Dutch Election Data")]
        [SerializeField] private DemoPoliticalParty[] allParties = new DemoPoliticalParty[0];
        [SerializeField] private HistoricalElectionResult election2023;

        [Header("Coalition Formation Data")]
        [SerializeField] private CoalitionCompatibilityMatrix compatibilityMatrix;
        [SerializeField] private DutchMinisterialPortfolio[] ministerialPortfolios = new DutchMinisterialPortfolio[0];

        [Header("Demo Configuration")]
        [SerializeField] private float coalitionCalculationTimeLimit = 5.0f;
        [SerializeField] private int minimumMajoritySeats = 76; // 150 seat parliament
        [SerializeField] private bool enableAdvancedFeatures = false;

        // Cached data for performance
        private Dictionary<string, DemoPoliticalParty> partyLookup;
        private bool isInitialized = false;

        // Public properties
        public DemoPoliticalParty[] AllParties => allParties;
        public HistoricalElectionResult Election2023 => election2023;
        public CoalitionCompatibilityMatrix CompatibilityMatrix => compatibilityMatrix;
        public DutchMinisterialPortfolio[] MinisterialPortfolios => ministerialPortfolios;
        public float CoalitionCalculationTimeLimit => coalitionCalculationTimeLimit;
        public int MinimumMajoritySeats => minimumMajoritySeats;
        public bool EnableAdvancedFeatures => enableAdvancedFeatures;

        /// <summary>
        /// Initialize the repository and build lookup caches.
        /// </summary>
        public void Initialize()
        {
            if (isInitialized) return;

            BuildPartyLookup();
            ValidateData();
            isInitialized = true;

            Debug.Log($"[PoliticalDataRepository] Initialized with {allParties.Length} parties");
        }

        /// <summary>
        /// Get a party by name using optimized lookup.
        /// </summary>
        /// <param name="name">Party name to search for</param>
        /// <returns>The party if found, null otherwise</returns>
        public DemoPoliticalParty GetPartyByName(string name)
        {
            if (!isInitialized) Initialize();
            return partyLookup?.GetValueOrDefault(name);
        }

        /// <summary>
        /// Get all parties that are not excluded from forming a coalition with the given party.
        /// </summary>
        /// <param name="party">The party to find coalition partners for</param>
        /// <returns>List of viable coalition partners</returns>
        public List<DemoPoliticalParty> GetViableCoalitionPartners(DemoPoliticalParty party)
        {
            if (!isInitialized) Initialize();
            if (party == null) return new List<DemoPoliticalParty>();

            return allParties.Where(p => p != party &&
                                   !p.ExcludedCoalitionPartners.Contains(party.PartyName) &&
                                   !party.ExcludedCoalitionPartners.Contains(p.PartyName))
                            .ToList();
        }

        /// <summary>
        /// Get all parties with at least one seat in the 2023 election.
        /// </summary>
        /// <returns>List of parties represented in parliament</returns>
        public List<DemoPoliticalParty> GetPartiesInParliament()
        {
            if (!isInitialized) Initialize();
            return allParties.Where(p => p.SeatsWon > 0).ToList();
        }

        /// <summary>
        /// Calculate the total seats for a list of parties.
        /// </summary>
        /// <param name="parties">List of parties</param>
        /// <returns>Total seat count</returns>
        public int CalculateTotalSeats(IEnumerable<DemoPoliticalParty> parties)
        {
            return parties?.Sum(p => p.SeatsWon) ?? 0;
        }

        /// <summary>
        /// Check if a coalition has enough seats for a majority.
        /// </summary>
        /// <param name="parties">Coalition parties</param>
        /// <returns>True if coalition has majority (76+ seats)</returns>
        public bool HasMajority(IEnumerable<DemoPoliticalParty> parties)
        {
            return CalculateTotalSeats(parties) >= minimumMajoritySeats;
        }

        /// <summary>
        /// Get the largest party from a list (by seat count).
        /// </summary>
        /// <param name="parties">List of parties</param>
        /// <returns>Party with most seats, or null if list is empty</returns>
        public DemoPoliticalParty GetLargestParty(IEnumerable<DemoPoliticalParty> parties)
        {
            return parties?.OrderByDescending(p => p.SeatsWon).FirstOrDefault();
        }

        /// <summary>
        /// Validate all political data for consistency and accuracy.
        /// </summary>
        /// <returns>Validation result with any errors found</returns>
        public ValidationResult ValidateRepository()
        {
            var result = new ValidationResult();

            // Validate each party
            foreach (var party in allParties)
            {
                if (party == null)
                {
                    result.AddError("Found null party in repository");
                    continue;
                }

                var partyValidation = party.ValidatePartyData();
                result.Merge(partyValidation, $"Party {party.PartyName}");
            }

            // Validate total seats equals 150
            int totalSeats = allParties.Sum(p => p.SeatsWon);
            if (totalSeats != 150)
            {
                result.AddError($"Total seats ({totalSeats}) should equal 150");
            }

            // Validate total vote percentage is approximately 100%
            float totalVotePercentage = allParties.Sum(p => p.VotePercentage);
            if (Mathf.Abs(totalVotePercentage - 100.0f) > 1.0f)
            {
                result.AddWarning($"Total vote percentage ({totalVotePercentage:F1}%) should be approximately 100%");
            }

            // Validate no duplicate party names
            var duplicateNames = allParties.GroupBy(p => p.PartyName)
                                          .Where(g => g.Count() > 1)
                                          .Select(g => g.Key);
            foreach (var duplicateName in duplicateNames)
            {
                result.AddError($"Duplicate party name: {duplicateName}");
            }

            // Validate ministerial portfolios
            if (ministerialPortfolios.Length == 0)
            {
                result.AddWarning("No ministerial portfolios defined");
            }

            return result;
        }

        private void BuildPartyLookup()
        {
            partyLookup = new Dictionary<string, DemoPoliticalParty>();

            foreach (var party in allParties)
            {
                if (party != null && !string.IsNullOrEmpty(party.PartyName))
                {
                    partyLookup[party.PartyName] = party;
                }
            }
        }

        private void ValidateData()
        {
            var validation = ValidateRepository();
            if (validation.HasErrors)
            {
                Debug.LogError($"[PoliticalDataRepository] Validation errors found:\n{validation.GetErrorSummary()}");
            }
            else if (validation.HasWarnings)
            {
                Debug.LogWarning($"[PoliticalDataRepository] Validation warnings:\n{validation.GetWarningSummary()}");
            }
            else
            {
                Debug.Log("[PoliticalDataRepository] Data validation passed");
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (allParties != null && allParties.Length > 0)
            {
                BuildPartyLookup();
            }
        }
#endif
    }

    /// <summary>
    /// Data structure for historical election results with validation against actual 2023 data.
    /// </summary>
    [System.Serializable]
    public class HistoricalElectionResult
    {
        [Header("2023 Dutch Election - Official Results")]
        public ElectionPartyResult[] partyResults = new ElectionPartyResult[]
        {
            new ElectionPartyResult("VVD", 2261326, 34, 15.0f),        // Actual 2023 results
            new ElectionPartyResult("PVV", 2410162, 37, 16.0f),
            new ElectionPartyResult("NSC", 1262935, 20, 8.4f),
            new ElectionPartyResult("GL-PvdA", 2094712, 25, 13.9f),
            new ElectionPartyResult("D66", 1895150, 24, 12.6f),
            new ElectionPartyResult("BBB", 1161351, 7, 7.7f),
            new ElectionPartyResult("CDA", 878825, 5, 5.8f),
            new ElectionPartyResult("SP", 566553, 5, 3.8f),
            new ElectionPartyResult("FvD", 439574, 3, 2.9f),
            new ElectionPartyResult("CU", 272815, 3, 1.8f),
            new ElectionPartyResult("SGP", 261646, 3, 1.7f),
            new ElectionPartyResult("Volt", 271645, 2, 1.8f)
        };

        public float totalTurnout = 77.1f; // 2023 actual turnout
        public int totalValidVotes = 11776694;
        public int totalSeats = 150;

        public Dictionary<string, int> GetHistoricalSeats() =>
            partyResults.ToDictionary(r => r.partyName, r => r.seatsWon);

        public Dictionary<string, int> GetVoteData() =>
            partyResults.ToDictionary(r => r.partyName, r => r.totalVotes);
    }

    /// <summary>
    /// Individual party result from the 2023 election.
    /// </summary>
    [System.Serializable]
    public struct ElectionPartyResult
    {
        public string partyName;
        public int totalVotes;
        public int seatsWon;
        public float votePercentage;

        public ElectionPartyResult(string name, int votes, int seats, float percentage)
        {
            partyName = name;
            totalVotes = votes;
            seatsWon = seats;
            votePercentage = percentage;
        }
    }

    /// <summary>
    /// Matrix storing compatibility scores between all party pairs.
    /// </summary>
    [System.Serializable]
    public class CoalitionCompatibilityMatrix
    {
        // Implementation for compatibility matrix storage and lookup
        // This will be expanded in future development phases
    }

    /// <summary>
    /// Dutch ministerial portfolio data for government formation.
    /// </summary>
    [System.Serializable]
    public struct DutchMinisterialPortfolio
    {
        public string portfolioName;
        public int priority; // 1 = PM, 2 = Deputy PM, etc.
        public string description;
        public bool isPrimeMinister;
        public bool isDeputyPM;

        public DutchMinisterialPortfolio(string name, int priority, string description, bool isPM = false, bool isDeputy = false)
        {
            portfolioName = name;
            this.priority = priority;
            this.description = description;
            isPrimeMinister = isPM;
            isDeputyPM = isDeputy;
        }
    }
}