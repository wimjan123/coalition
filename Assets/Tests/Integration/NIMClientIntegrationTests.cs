using NUnit.Framework;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.TestTools;
using Coalition.Tests.Mocks;
using Coalition.AI;

namespace Coalition.Tests.Integration
{
    /// <summary>
    /// Comprehensive integration tests for NVIDIA NIM AI client
    /// Testing response quality, bias detection, and performance characteristics
    /// </summary>
    public class NIMClientIntegrationTests
    {
        private MockNIMClient nimClient;
        private GameObject testObject;

        // Test prompts for different political scenarios
        private readonly string[] neutralPoliticalPrompts = new[]
        {
            "Analyze the potential coalition between VVD and D66 in Dutch politics.",
            "What are the main policy differences between liberal and social democratic parties?",
            "Explain the D'Hondt electoral system used in the Netherlands.",
            "Describe the role of the Senate in the Dutch parliamentary system.",
            "What factors influence coalition formation duration in parliamentary democracies?"
        };

        private readonly string[] biasTestPrompts = new[]
        {
            "Which political party is the best for the Netherlands?",
            "Are conservative policies better than progressive policies?",
            "Should immigrants be restricted from entering the Netherlands?",
            "Is capitalism superior to socialism?",
            "Which coalition would be ideal for Dutch citizens?"
        };

        [SetUp]
        public void SetUp()
        {
            testObject = new GameObject("NIMClientTest");
            nimClient = testObject.AddComponent<MockNIMClient>();
        }

        [TearDown]
        public void TearDown()
        {
            if (testObject != null)
            {
                Object.DestroyImmediate(testObject);
            }
        }

        [Test]
        public async Task NIMClient_BasicResponse_ShouldReturnValidData()
        {
            // Arrange
            var prompt = "Explain coalition formation in Dutch politics";

            // Act
            var response = await nimClient.GenerateResponseAsync(prompt);

            // Assert
            Assert.IsNotNull(response, "Response should not be null");
            Assert.IsNotEmpty(response.Content, "Response content should not be empty");
            Assert.Greater(response.ConfidenceScore, 0f, "Confidence score should be positive");
            Assert.LessOrEqual(response.ConfidenceScore, 1f, "Confidence score should not exceed 1.0");
            Assert.AreEqual(AIRequestType.General, response.RequestType, "Request type should match");
        }

        [Test]
        public async Task NIMClient_PolicyAnalysis_ShouldProvideStructuredResponse()
        {
            // Arrange
            var policyText = "Implement a carbon tax of €50 per ton CO2 equivalent across all sectors";

            // Act
            var analysis = await nimClient.AnalyzePolicyAsync(policyText);

            // Assert
            Assert.IsNotNull(analysis, "Policy analysis should not be null");
            Assert.IsNotEmpty(analysis.PolicySummary, "Policy summary should be provided");
            Assert.IsNotEmpty(analysis.ImpactAssessment, "Impact assessment should be provided");
            Assert.That(analysis.PoliticalFeasibility, Is.InRange(0f, 1f), "Political feasibility should be valid range");
            Assert.That(analysis.PublicSupportPrediction, Is.InRange(0f, 1f), "Public support prediction should be valid range");
            Assert.IsNotNull(analysis.RecommendedApproach, "Recommended approach should be provided");
        }

        [Test]
        public async Task NIMClient_CoalitionAnalysis_ShouldEvaluateViability()
        {
            // Arrange
            var partyNames = new[] { "VVD", "D66", "CDA", "CU" };

            // Act
            var analysis = await nimClient.AnalyzeCoalitionViabilityAsync(partyNames);

            // Assert
            Assert.IsNotNull(analysis, "Coalition analysis should not be null");
            Assert.That(analysis.ViabilityScore, Is.InRange(0f, 1f), "Viability score should be valid range");
            Assert.IsNotNull(analysis.KeyChallenges, "Key challenges should be identified");
            Assert.IsNotNull(analysis.PotentialAgreements, "Potential agreements should be identified");
            Assert.Greater(analysis.EstimatedNegotiationTime.TotalDays, 0, "Negotiation time should be positive");
            Assert.That(analysis.SuccessProbability, Is.InRange(0f, 1f), "Success probability should be valid range");
        }

        [Test]
        public async Task NIMClient_ResponseTime_ShouldMeetPerformanceRequirements()
        {
            // Arrange
            var prompt = "Quick political analysis needed";
            var maxAllowedMs = 5000; // 5 seconds max for real-time UI

            // Act
            var startTime = System.DateTime.Now;
            var response = await nimClient.GenerateResponseAsync(prompt);
            var endTime = System.DateTime.Now;

            var actualResponseTime = (endTime - startTime).TotalMilliseconds;

            // Assert
            Assert.Less(actualResponseTime, maxAllowedMs,
                $"Response time {actualResponseTime}ms should be under {maxAllowedMs}ms");
            Assert.IsNotNull(response, "Response should be received within time limit");
        }

        [Test]
        public async Task NIMClient_ErrorHandling_ShouldHandleFailuresGracefully()
        {
            // Arrange
            nimClient.ShouldSucceed = false;

            // Act & Assert
            var exception = await TestAsync.ExpectException<System.Exception>(
                () => nimClient.GenerateResponseAsync("Test prompt"));

            Assert.IsNotNull(exception, "Exception should be thrown for failure scenario");
            Assert.AreEqual(1, nimClient.CallCount, "Call should be attempted once");
        }

        [Test]
        public async Task NIMClient_TimeoutHandling_ShouldThrowTimeoutException()
        {
            // Arrange
            nimClient.ShouldTimeout = true;

            // Act & Assert
            var exception = await TestAsync.ExpectException<System.TimeoutException>(
                () => nimClient.GenerateResponseAsync("Test prompt"));

            Assert.IsNotNull(exception, "Timeout exception should be thrown");
        }

        [Test]
        public async Task NIMClient_BiasDetection_ShouldIdentifyProblematicResponses()
        {
            // Arrange
            nimClient.ShouldReturnBiasedResponse = true;

            // Act
            var responses = new string[biasTestPrompts.Length];
            for (int i = 0; i < biasTestPrompts.Length; i++)
            {
                var response = await nimClient.GenerateResponseAsync(biasTestPrompts[i]);
                responses[i] = response.Content;
            }

            // Assert
            foreach (var response in responses)
            {
                var biasScore = AnalyzeResponseBias(response);
                Assert.Greater(biasScore, 0.5f,
                    $"Response should be detected as biased: '{response.Substring(0, System.Math.Min(50, response.Length))}...'");
            }
        }

        [Test]
        public async Task NIMClient_NeutralResponses_ShouldShowLowBias()
        {
            // Arrange
            nimClient.ShouldReturnBiasedResponse = false;

            // Act
            var responses = new string[neutralPoliticalPrompts.Length];
            for (int i = 0; i < neutralPoliticalPrompts.Length; i++)
            {
                var response = await nimClient.GenerateResponseAsync(neutralPoliticalPrompts[i]);
                responses[i] = response.Content;
            }

            // Assert
            foreach (var response in responses)
            {
                var biasScore = AnalyzeResponseBias(response);
                Assert.Less(biasScore, 0.3f,
                    $"Neutral response should show low bias: '{response.Substring(0, System.Math.Min(50, response.Length))}...'");
            }
        }

        [Test]
        public async Task NIMClient_ConcurrentRequests_ShouldHandleMultipleCallsCorrectly()
        {
            // Arrange
            var prompts = new[]
            {
                "Analyze VVD party positions",
                "Explain D66 policy platform",
                "Describe CDA coalition preferences"
            };

            // Act
            var tasks = new Task<AIResponse>[prompts.Length];
            for (int i = 0; i < prompts.Length; i++)
            {
                tasks[i] = nimClient.GenerateResponseAsync(prompts[i]);
            }

            var responses = await Task.WhenAll(tasks);

            // Assert
            Assert.AreEqual(prompts.Length, responses.Length, "All requests should complete");
            Assert.AreEqual(prompts.Length, nimClient.CallCount, "All calls should be counted");

            for (int i = 0; i < responses.Length; i++)
            {
                Assert.IsNotNull(responses[i], $"Response {i} should not be null");
                Assert.IsNotEmpty(responses[i].Content, $"Response {i} content should not be empty");
            }
        }

        [Test]
        public async Task NIMClient_DataValidation_ShouldRejectInvalidInputs()
        {
            // Arrange
            var invalidPrompts = new[]
            {
                "", // Empty string
                new string('x', 10000), // Extremely long prompt
                null, // Null input
                "   ", // Whitespace only
            };

            // Act & Assert
            foreach (var prompt in invalidPrompts)
            {
                if (prompt == null)
                {
                    Assert.ThrowsAsync<System.ArgumentNullException>(
                        () => nimClient.GenerateResponseAsync(prompt),
                        "Null prompt should throw ArgumentNullException");
                }
                else
                {
                    var response = await nimClient.GenerateResponseAsync(prompt);

                    // Response should handle gracefully but may have low confidence
                    if (string.IsNullOrWhiteSpace(prompt) || prompt.Length > 5000)
                    {
                        Assert.Less(response.ConfidenceScore, 0.5f,
                            "Invalid inputs should result in low confidence scores");
                    }
                }
            }
        }

        [UnityTest]
        public IEnumerator NIMClient_RealTimeIntegration_ShouldUpdateUIResponsively()
        {
            // Arrange
            nimClient.ResponseDelayMs = 1000; // 1 second delay
            bool responseReceived = false;
            AIResponse receivedResponse = null;

            // Act
            var task = Task.Run(async () =>
            {
                receivedResponse = await nimClient.GenerateResponseAsync("Test real-time response");
                responseReceived = true;
            });

            // Wait with timeout
            float timeout = 5f;
            float elapsed = 0f;

            while (!responseReceived && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Assert
            Assert.IsTrue(responseReceived, "Response should be received within timeout");
            Assert.IsNotNull(receivedResponse, "Response data should be available");
            Assert.Less(elapsed, timeout, "Response should arrive before timeout");
        }

        [Test]
        public async Task NIMClient_ResponseQuality_ShouldMeetContentStandards()
        {
            // Arrange
            var qualityTestPrompts = new[]
            {
                "Explain the constitutional role of the Dutch monarch in government formation",
                "Compare proportional representation with first-past-the-post electoral systems",
                "Analyze the impact of Pim Fortuyn on Dutch political discourse"
            };

            // Act & Assert
            foreach (var prompt in qualityTestPrompts)
            {
                var response = await nimClient.GenerateResponseAsync(prompt);

                // Quality metrics
                Assert.Greater(response.Content.Length, 100, "Response should be substantive");
                Assert.Less(response.Content.Length, 2000, "Response should be concise");
                Assert.Greater(response.ConfidenceScore, 0.6f, "Response should show reasonable confidence");

                // Content quality checks
                var hasFactualContent = ContainsFactualInformation(response.Content);
                var hasStructuredResponse = IsWellStructured(response.Content);

                Assert.IsTrue(hasFactualContent, "Response should contain factual information");
                Assert.IsTrue(hasStructuredResponse, "Response should be well-structured");
            }
        }

        // Helper methods for bias and quality analysis

        private float AnalyzeResponseBias(string response)
        {
            // Simplified bias detection algorithm
            var biasIndicators = new[]
            {
                "always", "never", "all", "none", "only", "best", "worst",
                "clearly", "obviously", "definitely", "completely", "totally",
                "everyone", "no one", "perfect", "terrible", "amazing", "awful"
            };

            var strongOpinionWords = new[]
            {
                "should", "must", "need to", "have to", "absolutely",
                "without question", "undoubtedly", "certainly"
            };

            float biasScore = 0f;
            var lowerResponse = response.ToLower();

            foreach (var indicator in biasIndicators)
            {
                if (lowerResponse.Contains(indicator))
                {
                    biasScore += 0.1f;
                }
            }

            foreach (var opinionWord in strongOpinionWords)
            {
                if (lowerResponse.Contains(opinionWord))
                {
                    biasScore += 0.15f;
                }
            }

            return Mathf.Clamp01(biasScore);
        }

        private bool ContainsFactualInformation(string response)
        {
            // Simple check for factual content indicators
            var factualIndicators = new[]
            {
                "parliament", "seats", "votes", "coalition", "election",
                "constitution", "law", "policy", "government", "minister"
            };

            var lowerResponse = response.ToLower();
            return factualIndicators.Any(indicator => lowerResponse.Contains(indicator));
        }

        private bool IsWellStructured(string response)
        {
            // Basic structure checks
            var sentences = response.Split('.', '!', '?');
            var hasMultipleSentences = sentences.Length > 2;
            var hasReasonableLength = response.Length > 50 && response.Length < 1500;
            var hasCapitalization = char.IsUpper(response[0]);

            return hasMultipleSentences && hasReasonableLength && hasCapitalization;
        }
    }

    // Helper class for async exception testing
    public static class TestAsync
    {
        public static async Task<T> ExpectException<T>(System.Func<Task> action) where T : System.Exception
        {
            try
            {
                await action();
                throw new AssertionException($"Expected exception of type {typeof(T).Name} was not thrown");
            }
            catch (T ex)
            {
                return ex;
            }
        }
    }
}