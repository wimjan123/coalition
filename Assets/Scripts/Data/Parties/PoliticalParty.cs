using UnityEngine;
using System.Collections.Generic;

namespace Coalition.Data
{
    /// <summary>
    /// ScriptableObject representing a Dutch political party
    /// Contains authentic party data for accurate political simulation
    /// </summary>
    [CreateAssetMenu(fileName = "New Political Party", menuName = "Coalition/Political Party")]
    public class PoliticalParty : ScriptableObject
    {
        [Header("Basic Information")]
        [SerializeField] private string partyName;
        [SerializeField] private string abbreviation;
        [SerializeField] private string leader;
        [SerializeField] private Color partyColor = Color.blue;
        [SerializeField] private Sprite partyLogo;

        [Header("Political Positioning")]
        [Range(-10, 10)] [SerializeField] private float economicPosition; // Left (-10) to Right (10)
        [Range(-10, 10)] [SerializeField] private float socialPosition;   // Conservative (-10) to Progressive (10)
        [Range(-10, 10)] [SerializeField] private float europeanPosition; // Eurosceptic (-10) to Pro-EU (10)
        [Range(-10, 10)] [SerializeField] private float immigrationPosition; // Restrictive (-10) to Open (10)

        [Header("Party Characteristics")]
        [Range(0, 100)] [SerializeField] private float currentPopularity = 10.0f;
        [Range(0, 100)] [SerializeField] private float campaignExpertise = 50.0f;
        [Range(0, 100)] [SerializeField] private float mediaPresence = 50.0f;
        [Range(0, 100)] [SerializeField] private float coalitionFlexibility = 50.0f;

        [Header("Coalition Preferences")]
        [SerializeField] private List<string> preferredCoalitionPartners;
        [SerializeField] private List<string> excludedCoalitionPartners;
        [SerializeField] private List<PoliticalIssue> coreIssues;

        [Header("Campaign Resources")]
        [SerializeField] private float campaignBudget = 1000000.0f; // In euros
        [SerializeField] private int socialMediaFollowers = 10000;
        [SerializeField] private int activeMembership = 5000;

        // Runtime state (not serialized)
        private float currentApprovalRating;
        private Dictionary<string, float> issuePositions;
        private bool isInGovernment = false;
        private bool isInCoalition = false;

        // Properties
        public string PartyName => partyName;
        public string Abbreviation => abbreviation;
        public string Leader => leader;
        public Color PartyColor => partyColor;
        public Sprite PartyLogo => partyLogo;

        public float EconomicPosition => economicPosition;
        public float SocialPosition => socialPosition;
        public float EuropeanPosition => europeanPosition;
        public float ImmigrationPosition => immigrationPosition;

        public float CurrentPopularity => currentPopularity;
        public float CampaignExpertise => campaignExpertise;
        public float MediaPresence => mediaPresence;
        public float CoalitionFlexibility => coalitionFlexibility;

        public List<string> PreferredCoalitionPartners => preferredCoalitionPartners;
        public List<string> ExcludedCoalitionPartners => excludedCoalitionPartners;
        public List<PoliticalIssue> CoreIssues => coreIssues;

        public float CampaignBudget => campaignBudget;
        public int SocialMediaFollowers => socialMediaFollowers;
        public int ActiveMembership => activeMembership;

        public float CurrentApprovalRating => currentApprovalRating;
        public bool IsInGovernment => isInGovernment;
        public bool IsInCoalition => isInCoalition;

        private void OnEnable()
        {
            InitializeParty();
        }

        private void InitializeParty()
        {
            currentApprovalRating = currentPopularity;
            issuePositions = new Dictionary<string, float>();

            // Initialize issue positions based on core issues
            foreach (var issue in coreIssues)
            {
                if (issue != null)
                {
                    issuePositions[issue.IssueName] = issue.PartyPosition;
                }
            }
        }

        public float GetIssuePosition(string issueName)
        {
            return issuePositions.ContainsKey(issueName) ? issuePositions[issueName] : 0.0f;
        }

        public void SetIssuePosition(string issueName, float position)
        {
            issuePositions[issueName] = Mathf.Clamp(position, -10.0f, 10.0f);
        }

        public void UpdateApprovalRating(float change, string reason)
        {
            currentApprovalRating = Mathf.Clamp(currentApprovalRating + change, 0.0f, 100.0f);
            Debug.Log($"[{partyName}] Approval rating changed by {change:F1}% to {currentApprovalRating:F1}% ({reason})");
        }

        public void SetGovernmentStatus(bool inGovernment, bool inCoalition = false)
        {
            isInGovernment = inGovernment;
            isInCoalition = inCoalition;
        }

        public float CalculateCoalitionCompatibility(PoliticalParty otherParty)
        {
            if (otherParty == null) return 0.0f;

            // Check exclusions first
            if (excludedCoalitionPartners.Contains(otherParty.partyName))
                return 0.0f;

            // Calculate ideological distance
            float economicDistance = Mathf.Abs(economicPosition - otherParty.economicPosition);
            float socialDistance = Mathf.Abs(socialPosition - otherParty.socialPosition);
            float europeanDistance = Mathf.Abs(europeanPosition - otherParty.europeanPosition);
            float immigrationDistance = Mathf.Abs(immigrationPosition - otherParty.immigrationPosition);

            float averageDistance = (economicDistance + socialDistance + europeanDistance + immigrationDistance) / 4.0f;

            // Convert distance to compatibility (closer = more compatible)
            float baseCompatibility = Mathf.Max(0, 10.0f - averageDistance) / 10.0f;

            // Bonus for preferred partners
            if (preferredCoalitionPartners.Contains(otherParty.partyName))
                baseCompatibility += 0.2f;

            // Factor in coalition flexibility
            float flexibilityFactor = (coalitionFlexibility + otherParty.coalitionFlexibility) / 200.0f;

            return Mathf.Clamp01(baseCompatibility * (0.7f + 0.3f * flexibilityFactor));
        }

        public string GetPoliticalPositionDescription()
        {
            string economic = economicPosition < -3 ? "Left" : economicPosition > 3 ? "Right" : "Center";
            string social = socialPosition < -3 ? "Conservative" : socialPosition > 3 ? "Progressive" : "Moderate";
            string european = europeanPosition < -3 ? "Eurosceptic" : europeanPosition > 3 ? "Pro-EU" : "EU-Neutral";

            return $"{economic}, {social}, {european}";
        }

        // Validation for editor
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(partyName))
                partyName = "New Party";

            if (string.IsNullOrEmpty(abbreviation))
                abbreviation = partyName.Substring(0, Mathf.Min(3, partyName.Length)).ToUpper();

            currentPopularity = Mathf.Clamp(currentPopularity, 0, 100);
            campaignExpertise = Mathf.Clamp(campaignExpertise, 0, 100);
            mediaPresence = Mathf.Clamp(mediaPresence, 0, 100);
            coalitionFlexibility = Mathf.Clamp(coalitionFlexibility, 0, 100);
        }
    }
}