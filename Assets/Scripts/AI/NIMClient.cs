using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;

namespace Coalition.AI
{
    /// <summary>
    /// HTTP client for NVIDIA NIM local LLM integration
    /// Handles political content generation with caching and fallback systems
    /// </summary>
    public class NIMClient : MonoBehaviour
    {
        [Header("NVIDIA NIM Configuration")]
        [SerializeField] private string nimBaseUrl = "http://localhost:8000/v1";
        [SerializeField] private string modelName = "meta/llama3-8b-instruct";
        [SerializeField] private float requestTimeout = 30.0f;
        [SerializeField] private int maxRetries = 3;

        [Header("Performance Settings")]
        [SerializeField] private int maxTokens = 500;
        [SerializeField] private float temperature = 0.7f;
        [SerializeField] private float topP = 0.9f;
        [SerializeField] private bool enableCaching = true;

        [Header("Rate Limiting")]
        [SerializeField] private int maxRequestsPerMinute = 30;
        [SerializeField] private int maxRequestsPerHour = 1000;

        private HttpClient httpClient;
        private ResponseCache responseCache;
        private Queue<DateTime> recentRequests;
        private bool isInitialized = false;

        // Events
        public event Action<string> OnConnectionStatusChanged;
        public event Action<float> OnResponseTimeRecorded;

        private void Awake()
        {
            InitializeClient();
        }

        private void InitializeClient()
        {
            // Setup HTTP client with timeout
            httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(requestTimeout);
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Coalition-Political-Simulation/1.0");

            // Initialize response cache
            if (enableCaching)
            {
                responseCache = GetComponent<ResponseCache>();
                if (responseCache == null)
                {
                    responseCache = gameObject.AddComponent<ResponseCache>();
                }
            }

            // Initialize rate limiting
            recentRequests = new Queue<DateTime>();

            Debug.Log("[NIMClient] NVIDIA NIM client initialized");
        }

        public async Task<bool> TestConnection()
        {
            try
            {
                var healthCheck = new
                {
                    model = modelName,
                    messages = new[]
                    {
                        new { role = "user", content = "Test connection" }
                    },
                    max_tokens = 10
                };

                string jsonPayload = JsonConvert.SerializeObject(healthCheck);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                using var response = await httpClient.PostAsync($"{nimBaseUrl}/chat/completions", content);
                isInitialized = response.IsSuccessStatusCode;

                string status = isInitialized ? "Connected" : $"Failed ({response.StatusCode})";
                OnConnectionStatusChanged?.Invoke(status);

                Debug.Log($"[NIMClient] Connection test: {status}");
                return isInitialized;
            }
            catch (Exception e)
            {
                isInitialized = false;
                OnConnectionStatusChanged?.Invoke($"Error: {e.Message}");
                Debug.LogError($"[NIMClient] Connection test failed: {e.Message}");
                return false;
            }
        }

        public async Task<string> GeneratePoliticalResponse(string prompt, string partyContext = "", string responseType = "social_media")
        {
            if (!isInitialized)
            {
                Debug.LogWarning("[NIMClient] Client not initialized, attempting connection...");
                await TestConnection();
            }

            // Check rate limits
            if (!CheckRateLimit())
            {
                Debug.LogWarning("[NIMClient] Rate limit exceeded, queuing request...");
                await Task.Delay(1000); // Wait 1 second and retry
                return await GeneratePoliticalResponse(prompt, partyContext, responseType);
            }

            // Check cache first
            string cacheKey = GenerateCacheKey(prompt, partyContext, responseType);
            if (enableCaching && responseCache != null)
            {
                string cachedResponse = responseCache.GetCachedResponse(cacheKey);
                if (!string.IsNullOrEmpty(cachedResponse))
                {
                    Debug.Log($"[NIMClient] Using cached response for {responseType}");
                    return cachedResponse;
                }
            }

            try
            {
                DateTime startTime = DateTime.UtcNow;

                // Build the complete prompt with political context
                string fullPrompt = BuildPoliticalPrompt(prompt, partyContext, responseType);

                var requestBody = new
                {
                    model = modelName,
                    messages = new[]
                    {
                        new { role = "system", content = GetSystemPrompt(responseType) },
                        new { role = "user", content = fullPrompt }
                    },
                    max_tokens = maxTokens,
                    temperature = temperature,
                    top_p = topP,
                    stream = false
                };

                string jsonPayload = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                // Make the request with retry logic
                string response = await MakeRequestWithRetry(content);

                // Record response time
                float responseTime = (float)(DateTime.UtcNow - startTime).TotalSeconds;
                OnResponseTimeRecorded?.Invoke(responseTime);

                // Cache the response
                if (enableCaching && responseCache != null && !string.IsNullOrEmpty(response))
                {
                    responseCache.CacheResponse(cacheKey, response);
                }

                Debug.Log($"[NIMClient] Generated {responseType} response in {responseTime:F2}s");
                return response;
            }
            catch (Exception e)
            {
                Debug.LogError($"[NIMClient] Error generating political response: {e.Message}");
                return GetFallbackResponse(responseType, partyContext);
            }
        }

        private async Task<string> MakeRequestWithRetry(StringContent content)
        {
            Exception lastException = null;

            for (int retry = 0; retry < maxRetries; retry++)
            {
                try
                {
                    using var response = await httpClient.PostAsync($"{nimBaseUrl}/chat/completions", content);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        var responseData = JsonConvert.DeserializeObject<NIMResponse>(responseBody);

                        return responseData?.choices?[0]?.message?.content ?? "No response generated";
                    }
                    else
                    {
                        throw new HttpRequestException($"Request failed with status {response.StatusCode}");
                    }
                }
                catch (Exception e)
                {
                    lastException = e;
                    if (retry < maxRetries - 1)
                    {
                        int delay = (int)Math.Pow(2, retry) * 1000; // Exponential backoff
                        Debug.LogWarning($"[NIMClient] Request attempt {retry + 1} failed, retrying in {delay}ms...");
                        await Task.Delay(delay);
                    }
                }
            }

            throw lastException ?? new Exception("Unknown error during request");
        }

        private bool CheckRateLimit()
        {
            DateTime now = DateTime.UtcNow;

            // Remove requests older than 1 hour
            while (recentRequests.Count > 0 && (now - recentRequests.Peek()).TotalHours > 1)
            {
                recentRequests.Dequeue();
            }

            // Check hourly limit
            if (recentRequests.Count >= maxRequestsPerHour)
            {
                return false;
            }

            // Check per-minute limit
            int recentMinuteRequests = 0;
            foreach (DateTime requestTime in recentRequests)
            {
                if ((now - requestTime).TotalMinutes <= 1)
                {
                    recentMinuteRequests++;
                }
            }

            if (recentMinuteRequests >= maxRequestsPerMinute)
            {
                return false;
            }

            // Add current request to queue
            recentRequests.Enqueue(now);
            return true;
        }

        private string BuildPoliticalPrompt(string prompt, string partyContext, string responseType)
        {
            StringBuilder fullPrompt = new StringBuilder();

            if (!string.IsNullOrEmpty(partyContext))
            {
                fullPrompt.AppendLine($"Political Party Context: {partyContext}");
                fullPrompt.AppendLine();
            }

            fullPrompt.AppendLine("Context: Dutch political system with multi-party democracy and coalition government formation.");
            fullPrompt.AppendLine($"Response Type: {responseType}");
            fullPrompt.AppendLine();
            fullPrompt.Append($"Request: {prompt}");

            return fullPrompt.ToString();
        }

        private string GetSystemPrompt(string responseType)
        {
            switch (responseType.ToLower())
            {
                case "social_media":
                    return "You are a political communications expert for Dutch political parties. Generate authentic, professional social media content that reflects Dutch political discourse. Keep responses concise and engaging.";
                case "debate_response":
                    return "You are a Dutch political leader participating in a televised debate. Respond thoughtfully and professionally, representing your party's positions while engaging constructively with other viewpoints.";
                case "news_article":
                    return "You are a Dutch political journalist writing objective news articles about political developments. Maintain journalistic neutrality and accuracy.";
                case "press_release":
                    return "You are a political communications specialist writing official press releases for Dutch political parties. Use formal, professional language appropriate for official statements.";
                default:
                    return "You are a Dutch political expert providing informed commentary on political developments in the Netherlands.";
            }
        }

        private string GenerateCacheKey(string prompt, string partyContext, string responseType)
        {
            string combined = $"{prompt}|{partyContext}|{responseType}";
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(combined));
        }

        private string GetFallbackResponse(string responseType, string partyContext)
        {
            // Provide generic fallback responses when LLM is unavailable
            switch (responseType.ToLower())
            {
                case "social_media":
                    return "We continue to work for the interests of all Dutch citizens. #Netherlands #Politics";
                case "debate_response":
                    return "This is an important issue that requires careful consideration and dialogue between all parties.";
                case "news_article":
                    return "Political developments continue to unfold in the Dutch parliament.";
                default:
                    return "Political statement temporarily unavailable.";
            }
        }

        private void OnDestroy()
        {
            httpClient?.Dispose();
        }

        // Data classes for JSON serialization
        [Serializable]
        private class NIMResponse
        {
            public Choice[] choices;
        }

        [Serializable]
        private class Choice
        {
            public Message message;
        }

        [Serializable]
        private class Message
        {
            public string content;
        }
    }
}