using System.Collections.Generic;
using UnityEngine;
using Coalition.Runtime.Core;

namespace Coalition.Runtime.Data
{
    /// <summary>
    /// ScriptableObject representing a Dutch political party for the coalition demo.
    /// Contains 2023 election data and ideological positioning on 4-axis system.
    /// </summary>
    [CreateAssetMenu(fileName = "New Demo Party", menuName = "Coalition/Demo/Political Party")]
    public class DemoPoliticalParty : ScriptableObject
    {
        [Header("Basic Party Information")]
        [SerializeField] private string partyName = "";
        [SerializeField] private string abbreviation = "";
        [SerializeField] private string leader = "";
        [SerializeField] private Color partyColor = Color.white;
        [SerializeField] private Sprite partyLogo;

        [Header("2023 Election Results")]
        [SerializeField] private int seatsWon = 0;
        [SerializeField] private float votePercentage = 0.0f;
        [SerializeField] private int totalVotes = 0;

        [Header("Political Positioning (4-Axis System)")]
        [SerializeField] [Range(-10, 10)] private float economicPosition;      // Left-Right economics
        [SerializeField] [Range(-10, 10)] private float socialPosition;        // Conservative-Progressive
        [SerializeField] [Range(-10, 10)] private float europeanPosition;      // Eurosceptic-Pro EU
        [SerializeField] [Range(-10, 10)] private float immigrationPosition;   // Restrictive-Open

        [Header("Coalition Behavior")]
        [SerializeField] [Range(0, 100)] private float coalitionFlexibility = 50.0f;
        [SerializeField] private List<string> preferredCoalitionPartners = new List<string>();
        [SerializeField] private List<string> excludedCoalitionPartners = new List<string>();
        [SerializeField] private List<string> redLineIssues = new List<string>(); // Non-negotiable policy positions

        // Public properties
        public string PartyName => partyName;
        public string Abbreviation => abbreviation;
        public string Leader => leader;
        public Color PartyColor => partyColor;
        public Sprite PartyLogo => partyLogo;
        public int SeatsWon => seatsWon;
        public float VotePercentage => votePercentage;
        public int TotalVotes => totalVotes;
        public float EconomicPosition => economicPosition;
        public float SocialPosition => socialPosition;
        public float EuropeanPosition => europeanPosition;
        public float ImmigrationPosition => immigrationPosition;
        public float CoalitionFlexibility => coalitionFlexibility;
        public List<string> PreferredCoalitionPartners => preferredCoalitionPartners;
        public List<string> ExcludedCoalitionPartners => excludedCoalitionPartners;
        public List<string> RedLineIssues => redLineIssues;

        /// <summary>
        /// Calculate compatibility with another party based on ideological distance,
        /// exclusions, and historical partnership patterns.
        /// </summary>
        /// <param name="other">The other party to calculate compatibility with</param>
        /// <returns>Compatibility score from 0.0 to 1.0</returns>
        public float CalculateCompatibilityWith(DemoPoliticalParty other)
        {
            if (other == null) return 0.0f;
            if (excludedCoalitionPartners.Contains(other.partyName)) return 0.0f;
            if (other.excludedCoalitionPartners.Contains(partyName)) return 0.0f;

            float ideologicalDistance = CalculateIdeologicalDistance(other);
            float flexibilityBonus = (coalitionFlexibility + other.coalitionFlexibility) / 200.0f;
            float preferenceBonus = preferredCoalitionPartners.Contains(other.partyName) ? 0.2f : 0.0f;

            // Base compatibility calculation: higher scores for closer ideological positions
            float baseCompatibility = Mathf.Clamp01((10.0f - ideologicalDistance) / 10.0f);

            return Mathf.Clamp01(baseCompatibility + flexibilityBonus + preferenceBonus);
        }

        /// <summary>
        /// Calculate ideological distance using 4-axis political positioning system.
        /// </summary>
        /// <param name="other">The other party</param>
        /// <returns>Average distance across all axes (0-10 scale)</returns>
        private float CalculateIdeologicalDistance(DemoPoliticalParty other)
        {
            return (Mathf.Abs(economicPosition - other.economicPosition) +
                    Mathf.Abs(socialPosition - other.socialPosition) +
                    Mathf.Abs(europeanPosition - other.europeanPosition) +
                    Mathf.Abs(immigrationPosition - other.immigrationPosition)) / 4.0f;
        }

        /// <summary>
        /// Validate that all required party data is properly configured.
        /// </summary>
        /// <returns>Validation result with any errors found</returns>
        public ValidationResult ValidatePartyData()
        {
            var result = new ValidationResult();

            if (string.IsNullOrEmpty(partyName))
                result.AddError("Party name is required");

            if (string.IsNullOrEmpty(abbreviation))
                result.AddError("Party abbreviation is required");

            if (string.IsNullOrEmpty(leader))
                result.AddError("Party leader name is required");

            if (seatsWon < 0 || seatsWon > 150)
                result.AddError($"Seats won ({seatsWon}) must be between 0 and 150");

            if (votePercentage < 0 || votePercentage > 100)
                result.AddError($"Vote percentage ({votePercentage}) must be between 0 and 100");

            if (totalVotes < 0)
                result.AddError($"Total votes ({totalVotes}) cannot be negative");

            // Validate ideological positions are within range
            if (economicPosition < -10 || economicPosition > 10)
                result.AddError($"Economic position ({economicPosition}) must be between -10 and 10");

            return result;
        }

        /// <summary>
        /// Get a summary string of the party's key information.
        /// </summary>
        public string GetPartySummary()
        {
            return $"{partyName} ({abbreviation}) - Leader: {leader}, Seats: {seatsWon}, Votes: {votePercentage:F1}%";
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Ensure abbreviation is uppercase
            if (!string.IsNullOrEmpty(abbreviation))
                abbreviation = abbreviation.ToUpper();

            // Clamp values to valid ranges
            seatsWon = Mathf.Clamp(seatsWon, 0, 150);
            votePercentage = Mathf.Clamp(votePercentage, 0, 100);
            totalVotes = Mathf.Max(0, totalVotes);
        }
#endif
    }
}