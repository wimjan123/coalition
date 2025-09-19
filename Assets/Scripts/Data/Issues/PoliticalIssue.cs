using UnityEngine;

namespace Coalition.Data
{
    /// <summary>
    /// ScriptableObject representing a political issue in Dutch politics
    /// Used to define party positions and voter preferences on key topics
    /// </summary>
    [CreateAssetMenu(fileName = "New Political Issue", menuName = "Coalition/Political Issue")]
    public class PoliticalIssue : ScriptableObject
    {
        [Header("Issue Information")]
        [SerializeField] private string issueName;
        [SerializeField] private string displayName;
        [SerializeField] [TextArea(3, 6)] private string description;
        [SerializeField] private IssueCategory category;

        [Header("Political Positioning")]
        [Range(-10, 10)] [SerializeField] private float partyPosition; // Party's stance on this issue
        [Range(0, 100)] [SerializeField] private float voterImportance = 50.0f; // How much voters care about this issue
        [Range(0, 100)] [SerializeField] private float mediaAttention = 50.0f; // Current media focus on this issue

        [Header("Issue Dynamics")]
        [SerializeField] private bool isActivelyDebated = true; // Currently in political discourse
        [SerializeField] private bool isCampaignRelevant = true; // Relevant during campaigns
        [SerializeField] private float controversyLevel = 0.5f; // How divisive this issue is

        [Header("Position Labels")]
        [SerializeField] private string leftPositionLabel = "Progressive";
        [SerializeField] private string rightPositionLabel = "Conservative";
        [SerializeField] private string centerPositionLabel = "Moderate";

        // Properties
        public string IssueName => issueName;
        public string DisplayName => displayName;
        public string Description => description;
        public IssueCategory Category => category;
        public float PartyPosition => partyPosition;
        public float VoterImportance => voterImportance;
        public float MediaAttention => mediaAttention;
        public bool IsActivelyDebated => isActivelyDebated;
        public bool IsCampaignRelevant => isCampaignRelevant;
        public float ControversyLevel => controversyLevel;

        public string GetPositionLabel()
        {
            if (partyPosition < -3.0f)
                return leftPositionLabel;
            else if (partyPosition > 3.0f)
                return rightPositionLabel;
            else
                return centerPositionLabel;
        }

        public string GetPositionDescription()
        {
            float absPosition = Mathf.Abs(partyPosition);
            string intensity = absPosition > 7 ? "Strong" : absPosition > 4 ? "Moderate" : "Slight";
            string direction = partyPosition < 0 ? leftPositionLabel : partyPosition > 0 ? rightPositionLabel : centerPositionLabel;

            return partyPosition == 0 ? centerPositionLabel : $"{intensity} {direction}";
        }

        // Calculate how much this issue affects public opinion changes
        public float CalculateOpinionImpact(float positionDifference)
        {
            // Base impact scaled by voter importance and media attention
            float baseImpact = (voterImportance / 100.0f) * (mediaAttention / 100.0f);

            // Controversy level amplifies the impact of position differences
            float controversyMultiplier = 1.0f + (controversyLevel * 0.5f);

            // Position difference creates the direction and magnitude
            return baseImpact * controversyMultiplier * positionDifference;
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(issueName))
                issueName = "New Issue";

            if (string.IsNullOrEmpty(displayName))
                displayName = issueName;

            voterImportance = Mathf.Clamp(voterImportance, 0, 100);
            mediaAttention = Mathf.Clamp(mediaAttention, 0, 100);
            controversyLevel = Mathf.Clamp01(controversyLevel);
        }
    }

    [System.Serializable]
    public enum IssueCategory
    {
        Economy,
        Healthcare,
        Education,
        Environment,
        Immigration,
        Security,
        Social,
        European,
        Housing,
        Transportation,
        Technology,
        Agriculture,
        Culture,
        Defense,
        Justice
    }
}