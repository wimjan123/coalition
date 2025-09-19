using System.Collections.Generic;
using System.Linq;
using COALITION.Core;

namespace COALITION.Extensions
{
    /// <summary>
    /// Extension methods for PoliticalSystem to support UI integration
    /// Provides coalition management and compatibility calculation methods
    /// </summary>
    public static class PoliticalSystemExtensions
    {
        private static List<PoliticalParty> currentCoalition = new List<PoliticalParty>();

        /// <summary>
        /// Get all available political parties from the system
        /// </summary>
        public static List<PoliticalParty> GetAllParties(this PoliticalSystem system)
        {
            return system.politicalParties?.ToList() ?? new List<PoliticalParty>();
        }

        /// <summary>
        /// Add a party to the current coalition being formed
        /// </summary>
        public static void AddPartyToCoalition(this PoliticalSystem system, PoliticalParty party)
        {
            if (party == null) return;

            if (!currentCoalition.Contains(party))
            {
                currentCoalition.Add(party);
                UnityEngine.Debug.Log($"Added {party.partyName} to coalition. Total parties: {currentCoalition.Count}");
            }
        }

        /// <summary>
        /// Remove a party from the current coalition
        /// </summary>
        public static void RemovePartyFromCoalition(this PoliticalSystem system, PoliticalParty party)
        {
            if (party == null) return;

            if (currentCoalition.Contains(party))
            {
                currentCoalition.Remove(party);
                UnityEngine.Debug.Log($"Removed {party.partyName} from coalition. Total parties: {currentCoalition.Count}");
            }
        }

        /// <summary>
        /// Get the current coalition being formed
        /// </summary>
        public static List<PoliticalParty> GetCurrentCoalition(this PoliticalSystem system)
        {
            return new List<PoliticalParty>(currentCoalition);
        }

        /// <summary>
        /// Calculate total seats for a given coalition
        /// Uses the party's popularity percentage as a proxy for seats
        /// </summary>
        public static int CalculateCoalitionSeats(this PoliticalSystem system, List<PoliticalParty> coalition)
        {
            if (coalition == null || coalition.Count == 0) return 0;

            // Convert popularity percentage to approximate seats (out of 150 total)
            float totalPopularity = coalition.Sum(party => party.currentPopularity);

            // Dutch parliament has 150 seats, so calculate proportional representation
            int totalSeats = UnityEngine.Mathf.RoundToInt((totalPopularity / 100f) * 150f);

            return totalSeats;
        }

        /// <summary>
        /// Calculate compatibility score for a coalition (0-10 scale)
        /// Based on policy alignment across multiple dimensions
        /// </summary>
        public static float CalculateCoalitionCompatibility(this PoliticalSystem system, List<PoliticalParty> coalition)
        {
            if (coalition == null || coalition.Count <= 1) return 10.0f;

            float totalCompatibility = 0f;
            int comparisons = 0;

            // Calculate pairwise compatibility between all parties in coalition
            for (int i = 0; i < coalition.Count; i++)
            {
                for (int j = i + 1; j < coalition.Count; j++)
                {
                    var party1 = coalition[i];
                    var party2 = coalition[j];

                    float compatibility = CalculatePairwiseCompatibility(party1, party2);
                    totalCompatibility += compatibility;
                    comparisons++;
                }
            }

            return comparisons > 0 ? totalCompatibility / comparisons : 10.0f;
        }

        /// <summary>
        /// Calculate compatibility between two specific parties
        /// </summary>
        private static float CalculatePairwiseCompatibility(PoliticalParty party1, PoliticalParty party2)
        {
            if (party1 == null || party2 == null) return 0f;

            // Calculate distance across multiple policy dimensions
            float economicDistance = UnityEngine.Mathf.Abs(party1.economicPosition - party2.economicPosition);
            float socialDistance = UnityEngine.Mathf.Abs(party1.socialPosition - party2.socialPosition);
            float europeanDistance = UnityEngine.Mathf.Abs(party1.europeanPosition - party2.europeanPosition);

            // Weight the distances (economic and European positions are more important for coalition formation)
            float weightedDistance = (economicDistance * 0.4f) + (socialDistance * 0.3f) + (europeanDistance * 0.3f);

            // Convert distance to compatibility score (0-10 scale)
            // Maximum possible distance is 20 (from -10 to +10), so normalize
            float compatibility = UnityEngine.Mathf.Clamp01(1f - (weightedDistance / 20f)) * 10f;

            return compatibility;
        }

        /// <summary>
        /// Check if current coalition has majority (76+ seats)
        /// </summary>
        public static bool HasMajority(this PoliticalSystem system)
        {
            int totalSeats = system.CalculateCoalitionSeats(currentCoalition);
            return totalSeats >= 76; // Majority threshold in Dutch parliament
        }

        /// <summary>
        /// Clear the current coalition
        /// </summary>
        public static void ClearCoalition(this PoliticalSystem system)
        {
            currentCoalition.Clear();
            UnityEngine.Debug.Log("Coalition cleared");
        }

        /// <summary>
        /// Get parties that would be compatible with current coalition
        /// </summary>
        public static List<PoliticalParty> GetCompatibleParties(this PoliticalSystem system, float minCompatibility = 5.0f)
        {
            var availableParties = system.GetAllParties();
            var compatibleParties = new List<PoliticalParty>();

            foreach (var party in availableParties)
            {
                if (currentCoalition.Contains(party)) continue; // Skip parties already in coalition

                // Test compatibility if this party were added
                var testCoalition = new List<PoliticalParty>(currentCoalition) { party };
                float compatibility = system.CalculateCoalitionCompatibility(testCoalition);

                if (compatibility >= minCompatibility)
                {
                    compatibleParties.Add(party);
                }
            }

            return compatibleParties;
        }

        /// <summary>
        /// Get summary statistics for current coalition
        /// </summary>
        public static CoalitionSummary GetCoalitionSummary(this PoliticalSystem system)
        {
            return new CoalitionSummary
            {
                PartyCount = currentCoalition.Count,
                TotalSeats = system.CalculateCoalitionSeats(currentCoalition),
                Compatibility = system.CalculateCoalitionCompatibility(currentCoalition),
                HasMajority = system.HasMajority(),
                AverageEconomicPosition = currentCoalition.Count > 0 ? currentCoalition.Average(p => p.economicPosition) : 0f,
                AverageSocialPosition = currentCoalition.Count > 0 ? currentCoalition.Average(p => p.socialPosition) : 0f,
                AverageEuropeanPosition = currentCoalition.Count > 0 ? currentCoalition.Average(p => p.europeanPosition) : 0f
            };
        }
    }

    /// <summary>
    /// Summary data structure for coalition information
    /// </summary>
    [System.Serializable]
    public struct CoalitionSummary
    {
        public int PartyCount;
        public int TotalSeats;
        public float Compatibility;
        public bool HasMajority;
        public float AverageEconomicPosition;
        public float AverageSocialPosition;
        public float AverageEuropeanPosition;
    }
}