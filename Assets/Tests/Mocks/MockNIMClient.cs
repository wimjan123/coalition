using System;
using System.Threading.Tasks;
using UnityEngine;
using Coalition.AI;

namespace Coalition.Tests.Mocks
{
    /// <summary>
    /// Mock implementation of NIMClient for testing AI integration
    /// Provides controlled responses and behavior simulation for comprehensive testing
    /// </summary>
    public class MockNIMClient : MonoBehaviour
    {
        // Test control settings
        public bool ShouldSucceed { get; set; } = true;
        public bool ShouldTimeout { get; set; } = false;
        public bool ShouldReturnBiasedResponse { get; set; } = false;
        public int ResponseDelayMs { get; set; } = 100;

        // Mock response data
        public string MockResponse { get; set; } = "Mock AI response for political simulation testing";
        public float MockConfidenceScore { get; set; } = 0.85f;

        // Test tracking
        public int CallCount { get; private set; } = 0;
        public string LastPrompt { get; private set; }
        public DateTime LastCallTime { get; private set; }

        // Predefined political responses for testing
        private readonly string[] mockPoliticalResponses = new[]
        {
            "The coalition formation process requires careful negotiation between parties with different ideological positions.",
            "Public opinion polling suggests a shift towards environmental concerns among Dutch voters.",
            "Economic policy differences between liberal and social democratic parties may complicate negotiations.",
            "The role of smaller parties in coalition building has become increasingly important in recent elections.",
            "Immigration policy remains a significant point of contention in Dutch political discourse."
        };

        // Biased responses for bias detection testing
        private readonly string[] biasedResponses = new[]
        {
            "Party X is clearly the best choice for all Dutch citizens without exception.",
            "Anyone who disagrees with this policy is completely wrong and misguided.",
            "This political party represents the only valid perspective on this issue.",
            "All members of opposing parties are uninformed and should not be trusted."
        };

        public async Task<AIResponse> GenerateResponseAsync(string prompt, AIRequestType requestType = AIRequestType.General)
        {
            CallCount++;
            LastPrompt = prompt;
            LastCallTime = DateTime.Now;

            // Simulate network delay
            if (ResponseDelayMs > 0)
            {
                await Task.Delay(ResponseDelayMs);
            }

            // Simulate timeout
            if (ShouldTimeout)
            {
                throw new TimeoutException("Mock timeout for testing");
            }

            // Simulate failure
            if (!ShouldSucceed)
            {
                throw new Exception("Mock AI service failure for testing");
            }

            // Generate appropriate response based on request type
            string response = GenerateContextualResponse(prompt, requestType);

            return new AIResponse
            {
                Content = response,
                ConfidenceScore = MockConfidenceScore,
                ResponseTime = TimeSpan.FromMilliseconds(ResponseDelayMs),
                RequestType = requestType,
                Timestamp = DateTime.Now,
                IsCached = false
            };
        }

        public async Task<PolicyAnalysis> AnalyzePolicyAsync(string policyText)
        {
            var response = await GenerateResponseAsync(policyText, AIRequestType.PolicyAnalysis);

            return new PolicyAnalysis
            {
                PolicySummary = $"Mock analysis of policy: {policyText.Substring(0, Math.Min(50, policyText.Length))}...",
                ImpactAssessment = "Moderate positive impact expected on affected demographics",
                PoliticalFeasibility = 0.7f,
                PublicSupportPrediction = 0.65f,
                ImplementationComplexity = PolicyComplexity.Medium,
                RecommendedApproach = "Gradual implementation with stakeholder consultation"
            };
        }

        public async Task<CoalitionAnalysis> AnalyzeCoalitionViabilityAsync(string[] partyNames)
        {
            await Task.Delay(ResponseDelayMs / 2); // Shorter delay for coalition analysis

            return new CoalitionAnalysis
            {
                ViabilityScore = CalculateMockViabilityScore(partyNames),
                KeyChallenges = GenerateMockChallenges(partyNames),
                PotentialAgreements = GenerateMockAgreements(partyNames),
                EstimatedNegotiationTime = TimeSpan.FromDays(14 + (partyNames.Length * 3)),
                SuccessProbability = 0.72f
            };
        }

        public void Reset()
        {
            CallCount = 0;
            LastPrompt = null;
            LastCallTime = default;
            ShouldSucceed = true;
            ShouldTimeout = false;
            ShouldReturnBiasedResponse = false;
            ResponseDelayMs = 100;
        }

        private string GenerateContextualResponse(string prompt, AIRequestType requestType)
        {
            if (ShouldReturnBiasedResponse)
            {
                return biasedResponses[UnityEngine.Random.Range(0, biasedResponses.Length)];
            }

            return requestType switch
            {
                AIRequestType.PolicyAnalysis => GeneratePolicyResponse(prompt),
                AIRequestType.CoalitionAnalysis => GenerateCoalitionResponse(prompt),
                AIRequestType.PublicOpinion => GeneratePublicOpinionResponse(prompt),
                AIRequestType.DebatePreparation => GenerateDebateResponse(prompt),
                _ => mockPoliticalResponses[UnityEngine.Random.Range(0, mockPoliticalResponses.Length)]
            };
        }

        private string GeneratePolicyResponse(string prompt)
        {
            return $"Policy analysis for '{prompt}': This policy proposal shows potential for implementation within the Dutch political framework. Key considerations include constitutional compliance, budget implications, and stakeholder support. Recommended consultation period: 3-6 months.";
        }

        private string GenerateCoalitionResponse(string prompt)
        {
            return $"Coalition analysis: The proposed coalition configuration presents moderate viability. Historical precedents suggest successful negotiations are possible with compromise on 2-3 key policy areas. Expected formation time: 3-5 weeks.";
        }

        private string GeneratePublicOpinionResponse(string prompt)
        {
            return $"Public opinion assessment: Current polling indicates mixed support for this initiative. Regional variations are significant, with urban areas showing 65% support and rural areas at 45%. Demographics aged 25-45 are most supportive.";
        }

        private string GenerateDebateResponse(string prompt)
        {
            return $"Debate preparation: Key talking points include economic impact, social implications, and implementation timeline. Potential counterarguments focus on budget concerns and constitutional questions. Recommend preparing 3-4 supporting statistics.";
        }

        private float CalculateMockViabilityScore(string[] partyNames)
        {
            // Simple mock calculation based on party count
            float baseScore = 0.8f;
            float penalty = (partyNames.Length - 2) * 0.1f; // More parties = more complex
            return Mathf.Clamp01(baseScore - penalty);
        }

        private string[] GenerateMockChallenges(string[] partyNames)
        {
            return new[]
            {
                "Disagreement on taxation policy",
                "Different approaches to immigration",
                "Environmental regulation priorities",
                "Healthcare system reform scope"
            };
        }

        private string[] GenerateMockAgreements(string[] partyNames)
        {
            return new[]
            {
                "Infrastructure investment priorities",
                "Education system improvements",
                "Digital transformation initiatives",
                "International cooperation frameworks"
            };
        }
    }

    // Mock data structures for AI testing
    public class AIResponse
    {
        public string Content { get; set; }
        public float ConfidenceScore { get; set; }
        public TimeSpan ResponseTime { get; set; }
        public AIRequestType RequestType { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsCached { get; set; }
    }

    public class PolicyAnalysis
    {
        public string PolicySummary { get; set; }
        public string ImpactAssessment { get; set; }
        public float PoliticalFeasibility { get; set; }
        public float PublicSupportPrediction { get; set; }
        public PolicyComplexity ImplementationComplexity { get; set; }
        public string RecommendedApproach { get; set; }
    }

    public class CoalitionAnalysis
    {
        public float ViabilityScore { get; set; }
        public string[] KeyChallenges { get; set; }
        public string[] PotentialAgreements { get; set; }
        public TimeSpan EstimatedNegotiationTime { get; set; }
        public float SuccessProbability { get; set; }
    }

    public enum AIRequestType
    {
        General,
        PolicyAnalysis,
        CoalitionAnalysis,
        PublicOpinion,
        DebatePreparation
    }

    public enum PolicyComplexity
    {
        Low,
        Medium,
        High,
        VeryHigh
    }
}