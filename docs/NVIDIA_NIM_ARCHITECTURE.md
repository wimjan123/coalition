# COALITION - NVIDIA NIM Local LLM Integration Architecture

## Executive Summary

**Architecture Goal**: Integrate NVIDIA NIM for local LLM inference in Coalition's Unity 6 political simulation, enabling cost-effective real-time social media AI generation while maintaining performance and reliability through hybrid local/cloud deployment strategies.

**Key Components**:
- **Local NVIDIA NIM Deployment**: RTX/workstation GPU-accelerated LLM inference
- **Unity 6 HTTP Client**: C# integration layer for seamless API communication
- **Hybrid Architecture**: Local-primary with cloud failover for reliability
- **Intelligent Caching**: Multi-tier content caching with AI-assisted generation
- **Cost Optimization**: 80%+ local processing with strategic cloud usage

**Strategic Benefits**:
- **Cost Efficiency**: ~90% reduction in AI API costs vs cloud-only
- **Low Latency**: <500ms response times for real-time social media generation
- **Privacy Control**: Sensitive political content processed locally
- **Scalability**: Hybrid approach handles demand spikes and hardware limitations

## NVIDIA NIM Local Deployment Architecture

### Hardware Requirements & Specifications

#### Minimum Workstation Configuration
```yaml
GPU Requirements:
  primary: "RTX 4090 (24GB VRAM)"
  alternative: "RTX 4080 Super (16GB VRAM)"
  minimum: "RTX 4070 Ti (12GB VRAM)"

Memory: "32GB RAM (64GB recommended)"
Storage: "1TB NVMe SSD (for model storage)"
CPU: "Intel i7-13700K / AMD Ryzen 7 7700X"

Model Size Support:
  RTX_4090: "13B parameter models (optimal), 7B (high performance)"
  RTX_4080_Super: "7B parameter models (optimal), 13B (reduced batch)"
  RTX_4070_Ti: "7B parameter models (optimal), 3B (high performance)"
```

#### Enterprise/Server Configuration
```yaml
Server_Grade:
  GPU: "RTX 6000 Ada (48GB) / A6000 (48GB)"
  Memory: "128GB RAM"
  Models: "30B+ parameter models, multiple concurrent instances"

Multi_GPU_Setup:
  configuration: "2x RTX 4090 (NVLink)"
  capability: "Parallel inference, load balancing, failover"
  performance: "2x throughput, model sharding support"
```

### NVIDIA NIM Container Deployment

#### Docker Container Setup
```dockerfile
# NVIDIA NIM LLM Container Configuration
FROM nvcr.io/nim/meta/llama3-8b-instruct:1.0.0

# Environment Configuration
ENV CUDA_VISIBLE_DEVICES=0
ENV NIM_CACHE_PATH=/opt/nim/cache
ENV NIM_MODEL_PROFILE=throughput

# Model Configuration
VOLUME ["/opt/nim/cache", "/opt/nim/models"]

# API Configuration
EXPOSE 8000
ENV NIM_SERVER_PORT=8000
ENV NIM_OPENAI_API_KEY=coalition-local-key

# Health Check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s \
  CMD curl -f http://localhost:8000/v1/health || exit 1

# Performance Optimization
ENV NIM_TENSOR_PARALLEL_SIZE=1
ENV NIM_MAX_BATCH_SIZE=32
ENV NIM_MAX_INPUT_LEN=4096
ENV NIM_MAX_OUTPUT_LEN=1024
```

#### Deployment Scripts
```bash
#!/bin/bash
# coalition-nim-deploy.sh - NVIDIA NIM deployment automation

# System Validation
check_gpu_requirements() {
    nvidia-smi --query-gpu=memory.total --format=csv,noheader,nounits | \
    awk '{if ($1 < 12000) exit 1}'
    if [ $? -eq 1 ]; then
        echo "ERROR: Insufficient GPU memory. 12GB+ required."
        exit 1
    fi
}

# Model Download and Optimization
setup_nim_models() {
    # Coalition-optimized models for Dutch political content
    docker run --gpus all \
        -v nim_cache:/opt/nim/cache \
        nvcr.io/nim/meta/llama3-8b-instruct:1.0.0 \
        --download-model --optimize-for=coalition
}

# Production Deployment
deploy_coalition_nim() {
    docker run -d \
        --name coalition-nim \
        --gpus all \
        --restart unless-stopped \
        -p 8000:8000 \
        -v nim_cache:/opt/nim/cache \
        -v $(pwd)/config:/opt/nim/config \
        -e NIM_MODEL_PROFILE=coalition_optimized \
        nvcr.io/nim/meta/llama3-8b-instruct:1.0.0

    echo "Coalition NIM deployed at http://localhost:8000"
}

check_gpu_requirements
setup_nim_models
deploy_coalition_nim
```

### Model Selection & Optimization

#### Coalition-Specific Model Requirements
```yaml
Primary_Models:
  dutch_political_content:
    model: "meta/llama3-8b-instruct"
    optimization: "Dutch political knowledge fine-tuning"
    use_case: "News articles, political analysis"

  social_media_generation:
    model: "meta/llama3-8b-instruct"
    optimization: "Social media style, brevity"
    use_case: "Twitter responses, social media posts"

  coalition_negotiations:
    model: "meta/llama3-13b-instruct"
    optimization: "Political strategy, negotiation language"
    use_case: "Complex political dialogue generation"

Fallback_Models:
  efficiency_mode:
    model: "meta/llama3-7b-instruct"
    use_case: "Resource-constrained scenarios"

  speed_mode:
    model: "phi-3-mini-4k-instruct"
    use_case: "Real-time responses, low latency"
```

#### Model Performance Profiles
```yaml
throughput_profile:
  batch_size: 32
  concurrent_requests: 8
  target_latency: "500ms"
  memory_usage: "18GB VRAM"

low_latency_profile:
  batch_size: 4
  concurrent_requests: 2
  target_latency: "200ms"
  memory_usage: "14GB VRAM"

balanced_profile:
  batch_size: 16
  concurrent_requests: 4
  target_latency: "350ms"
  memory_usage: "16GB VRAM"
```

## Unity 6 HTTP Client Integration

### C# API Client Architecture

#### NIMClient Core Implementation
```csharp
// Unity 6 NVIDIA NIM Integration Layer
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

namespace Coalition.AI
{
    [Serializable]
    public class NIMRequest
    {
        public string model = "meta/llama3-8b-instruct";
        public Message[] messages;
        public float temperature = 0.7f;
        public int max_tokens = 512;
        public bool stream = false;

        [Serializable]
        public class Message
        {
            public string role; // "system", "user", "assistant"
            public string content;
        }
    }

    [Serializable]
    public class NIMResponse
    {
        public string id;
        public Choice[] choices;
        public Usage usage;

        [Serializable]
        public class Choice
        {
            public Message message;
            public string finish_reason;
        }

        [Serializable]
        public class Usage
        {
            public int prompt_tokens;
            public int completion_tokens;
            public int total_tokens;
        }
    }

    public class NIMClient : MonoBehaviour
    {
        [SerializeField] private string nimEndpoint = "http://localhost:8000";
        [SerializeField] private string apiKey = "coalition-local-key";
        [SerializeField] private int timeoutSeconds = 30;
        [SerializeField] private int retryAttempts = 3;

        private static NIMClient _instance;
        public static NIMClient Instance => _instance;

        void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public async Task<string> GenerateContentAsync(string systemPrompt, string userPrompt)
        {
            var request = new NIMRequest
            {
                messages = new[]
                {
                    new NIMRequest.Message { role = "system", content = systemPrompt },
                    new NIMRequest.Message { role = "user", content = userPrompt }
                },
                temperature = 0.7f,
                max_tokens = 512
            };

            return await SendRequestAsync(request);
        }

        private async Task<string> SendRequestAsync(NIMRequest request)
        {
            for (int attempt = 0; attempt < retryAttempts; attempt++)
            {
                try
                {
                    string jsonData = JsonConvert.SerializeObject(request);

                    using (UnityWebRequest webRequest = UnityWebRequest.Post($"{nimEndpoint}/v1/chat/completions", jsonData, "application/json"))
                    {
                        webRequest.SetRequestHeader("Authorization", $"Bearer {apiKey}");
                        webRequest.timeout = timeoutSeconds;

                        var operation = webRequest.SendWebRequest();

                        while (!operation.isDone)
                        {
                            await Task.Yield();
                        }

                        if (webRequest.result == UnityWebRequest.Result.Success)
                        {
                            var response = JsonConvert.DeserializeObject<NIMResponse>(webRequest.downloadHandler.text);
                            return response.choices[0].message.content;
                        }
                        else
                        {
                            Debug.LogWarning($"NIM request attempt {attempt + 1} failed: {webRequest.error}");
                            if (attempt == retryAttempts - 1)
                                throw new Exception($"NIM request failed after {retryAttempts} attempts: {webRequest.error}");
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"NIM request error on attempt {attempt + 1}: {e.Message}");
                    if (attempt == retryAttempts - 1)
                        throw;

                    await Task.Delay(1000 * (attempt + 1)); // Exponential backoff
                }
            }

            return null;
        }
    }
}
```

#### Specialized Political Content Generators
```csharp
// Dutch Political News Generation
namespace Coalition.AI.Generators
{
    public class DutchNewsGenerator : MonoBehaviour
    {
        [SerializeField] private NIMClient nimClient;

        public async Task<string> GenerateNewsArticleAsync(PoliticalEvent politicalEvent, MediaOutlet outlet)
        {
            string systemPrompt = CreateOutletSystemPrompt(outlet);
            string userPrompt = CreateEventPrompt(politicalEvent);

            return await nimClient.GenerateContentAsync(systemPrompt, userPrompt);
        }

        private string CreateOutletSystemPrompt(MediaOutlet outlet)
        {
            return outlet.Type switch
            {
                MediaOutletType.NOS =>
                    "You are a journalist for NOS, the trusted Dutch public broadcaster. " +
                    "Write balanced, factual news articles in formal Dutch journalistic style. " +
                    "Focus on democratic process, institutional respect, and measured analysis. " +
                    "Avoid sensationalism. Use neutral, professional language. 150-250 words.",

                MediaOutletType.RTL =>
                    "You are a journalist for RTL, a commercial Dutch broadcaster. " +
                    "Write engaging news articles with slightly more dramatic headlines. " +
                    "Focus on conflict, personality dynamics, and viewer engagement. " +
                    "Maintain journalistic standards while emphasizing compelling narratives. 150-250 words.",

                MediaOutletType.Volkskrant =>
                    "You are a journalist for de Volkskrant, a quality Dutch newspaper. " +
                    "Write in-depth analytical articles with editorial perspective. " +
                    "Focus on policy implications, historical context, and progressive viewpoints. " +
                    "Use sophisticated language and comprehensive analysis. 200-300 words.",

                _ => "Write a neutral Dutch news article about the political event."
            };
        }

        private string CreateEventPrompt(PoliticalEvent politicalEvent)
        {
            return $"Write a news article about: {politicalEvent.Description}\n" +
                   $"Key parties involved: {string.Join(", ", politicalEvent.Parties)}\n" +
                   $"Policy areas: {string.Join(", ", politicalEvent.PolicyAreas)}\n" +
                   $"Political context: {politicalEvent.Context}\n" +
                   $"Current date in simulation: {politicalEvent.Date:yyyy-MM-dd}";
        }
    }

    public class SocialMediaGenerator : MonoBehaviour
    {
        [SerializeField] private NIMClient nimClient;

        public async Task<string> GenerateTweetAsync(PoliticalEvent politicalEvent, PoliticalParty party)
        {
            string systemPrompt = $"You are the social media manager for {party.Name}, a Dutch political party. " +
                                 $"Write a Twitter/X post (max 280 characters) responding to recent political events. " +
                                 $"Maintain the party's political perspective: {party.IdeologicalPosition}. " +
                                 $"Use contemporary Dutch political language and hashtags. Be authentic but professional.";

            string userPrompt = $"React to this political development: {politicalEvent.Description}";

            return await nimClient.GenerateContentAsync(systemPrompt, userPrompt);
        }

        public async Task<string> GenerateInstagramPostAsync(PoliticalEvent politicalEvent, PoliticalParty party)
        {
            string systemPrompt = $"You are creating Instagram content for {party.Name}. " +
                                 $"Write an engaging post (150-300 characters) with emojis and hashtags. " +
                                 $"Target younger demographics while maintaining political messaging. " +
                                 $"Party position: {party.IdeologicalPosition}";

            string userPrompt = $"Create Instagram content about: {politicalEvent.Description}";

            return await nimClient.GenerateContentAsync(systemPrompt, userPrompt);
        }
    }
}
```

### Asynchronous Processing Architecture

#### Background Content Generation
```csharp
// Async content generation manager
namespace Coalition.AI.Processing
{
    public class ContentGenerationManager : MonoBehaviour
    {
        [SerializeField] private int maxConcurrentRequests = 4;
        [SerializeField] private float generationDelayMin = 2f;
        [SerializeField] private float generationDelayMax = 8f;

        private Queue<ContentRequest> requestQueue = new Queue<ContentRequest>();
        private List<Task> activeGenerationTasks = new List<Task>();

        public async Task<string> RequestContentAsync(ContentType type, PoliticalEvent politicalEvent)
        {
            var request = new ContentRequest(type, politicalEvent);

            // Immediate return if cached
            if (ContentCache.Instance.HasCachedContent(request))
            {
                return ContentCache.Instance.GetCachedContent(request);
            }

            // Queue for background generation
            requestQueue.Enqueue(request);
            ProcessQueue();

            // Return placeholder content while generating
            return GeneratePlaceholderContent(type, politicalEvent);
        }

        private async void ProcessQueue()
        {
            while (requestQueue.Count > 0 && activeGenerationTasks.Count < maxConcurrentRequests)
            {
                var request = requestQueue.Dequeue();
                var task = GenerateContentAsync(request);
                activeGenerationTasks.Add(task);

                // Clean up completed tasks
                activeGenerationTasks.RemoveAll(t => t.IsCompleted);
            }
        }

        private async Task GenerateContentAsync(ContentRequest request)
        {
            // Realistic timing simulation
            float delay = UnityEngine.Random.Range(generationDelayMin, generationDelayMax);
            await Task.Delay((int)(delay * 1000));

            try
            {
                string content = await NIMClient.Instance.GenerateContentAsync(
                    request.SystemPrompt,
                    request.UserPrompt
                );

                ContentCache.Instance.CacheContent(request, content);
                NotifyContentReady(request, content);
            }
            catch (Exception e)
            {
                Debug.LogError($"Content generation failed for {request.Type}: {e.Message}");
                FallbackToTemplate(request);
            }
        }

        private void NotifyContentReady(ContentRequest request, string content)
        {
            // Notify UI systems that new content is available
            EventBus.Instance.Publish(new ContentReadyEvent(request, content));
        }
    }
}
```

## Hybrid Local/Cloud Architecture

### Intelligent Request Routing

#### Smart Load Balancer
```csharp
// Hybrid local/cloud request routing
namespace Coalition.AI.Routing
{
    public enum ProcessingTier
    {
        Local,      // NVIDIA NIM local inference
        CloudFast,  // OpenAI/Anthropic API for speed
        CloudSmart, // Claude/GPT-4 for complex tasks
        Template    // Fallback template system
    }

    public class HybridAIRouter : MonoBehaviour
    {
        [SerializeField] private float localSuccessThreshold = 0.95f;
        [SerializeField] private int maxLocalQueueSize = 10;
        [SerializeField] private float dailyCloudBudget = 50.0f; // USD

        private AIMetrics metrics = new AIMetrics();
        private CostTracker costTracker = new CostTracker();

        public async Task<string> RouteRequestAsync(ContentRequest request)
        {
            ProcessingTier tier = DetermineOptimalTier(request);

            return tier switch
            {
                ProcessingTier.Local => await ProcessLocalAsync(request),
                ProcessingTier.CloudFast => await ProcessCloudAsync(request, CloudProvider.OpenAI),
                ProcessingTier.CloudSmart => await ProcessCloudAsync(request, CloudProvider.Anthropic),
                ProcessingTier.Template => ProcessTemplate(request),
                _ => ProcessTemplate(request)
            };
        }

        private ProcessingTier DetermineOptimalTier(ContentRequest request)
        {
            // Local processing priority checks
            if (IsLocalSystemHealthy() && HasLocalCapacity())
            {
                return ProcessingTier.Local;
            }

            // Cloud processing decision matrix
            if (costTracker.GetDailySpent() < dailyCloudBudget * 0.8f)
            {
                return request.Complexity switch
                {
                    ContentComplexity.Simple => ProcessingTier.CloudFast,
                    ContentComplexity.Complex => ProcessingTier.CloudSmart,
                    _ => ProcessingTier.CloudFast
                };
            }

            // Fallback to templates when budget exhausted
            return ProcessingTier.Template;
        }

        private bool IsLocalSystemHealthy()
        {
            return metrics.LocalSuccessRate > localSuccessThreshold &&
                   metrics.AverageLocalLatency < 1000 && // ms
                   !IsGPUOverheated();
        }

        private bool HasLocalCapacity()
        {
            return NIMClient.Instance.GetQueueSize() < maxLocalQueueSize;
        }

        private async Task<string> ProcessLocalAsync(ContentRequest request)
        {
            var startTime = DateTime.Now;

            try
            {
                string result = await NIMClient.Instance.GenerateContentAsync(
                    request.SystemPrompt,
                    request.UserPrompt
                );

                metrics.RecordLocalSuccess((DateTime.Now - startTime).TotalMilliseconds);
                return result;
            }
            catch (Exception e)
            {
                metrics.RecordLocalFailure();
                Debug.LogWarning($"Local processing failed, falling back to cloud: {e.Message}");

                return await ProcessCloudAsync(request, CloudProvider.OpenAI);
            }
        }

        private async Task<string> ProcessCloudAsync(ContentRequest request, CloudProvider provider)
        {
            float estimatedCost = EstimateCloudCost(request, provider);

            if (!costTracker.CanAfford(estimatedCost))
            {
                Debug.LogWarning("Cloud budget exhausted, using template");
                return ProcessTemplate(request);
            }

            try
            {
                string result = await CloudAIClient.Instance.GenerateAsync(request, provider);
                costTracker.RecordCost(estimatedCost);
                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"Cloud processing failed: {e.Message}");
                return ProcessTemplate(request);
            }
        }
    }
}
```

### Performance Monitoring & Health Checks

#### System Health Dashboard
```csharp
// Real-time performance monitoring
namespace Coalition.AI.Monitoring
{
    public class AISystemMonitor : MonoBehaviour
    {
        [SerializeField] private float healthCheckInterval = 30f;
        [SerializeField] private bool displayDebugUI = true;

        private AISystemHealth health = new AISystemHealth();

        void Start()
        {
            InvokeRepeating(nameof(PerformHealthCheck), 0f, healthCheckInterval);
        }

        private void PerformHealthCheck()
        {
            health.LocalSystemStatus = CheckLocalSystemHealth();
            health.CloudSystemStatus = CheckCloudSystemHealth();
            health.OverallPerformance = CalculateOverallPerformance();

            // Auto-adjust routing based on health
            if (health.LocalSystemStatus == SystemStatus.Degraded)
            {
                HybridAIRouter.Instance.IncreaseCloudUsage();
            }
        }

        private SystemStatus CheckLocalSystemHealth()
        {
            var nimStatus = NIMClient.Instance.GetHealthStatus();
            var gpuTemp = GetGPUTemperature();
            var memoryUsage = GetGPUMemoryUsage();

            if (!nimStatus.IsResponding)
                return SystemStatus.Critical;

            if (gpuTemp > 85 || memoryUsage > 0.95f)
                return SystemStatus.Degraded;

            if (nimStatus.AverageResponseTime > 1000)
                return SystemStatus.Warning;

            return SystemStatus.Healthy;
        }

        void OnGUI()
        {
            if (!displayDebugUI) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("AI System Health Monitor");
            GUILayout.Label($"Local System: {health.LocalSystemStatus}");
            GUILayout.Label($"GPU Temperature: {GetGPUTemperature()}°C");
            GUILayout.Label($"GPU Memory: {GetGPUMemoryUsage():P}");
            GUILayout.Label($"Daily Cloud Cost: ${CostTracker.Instance.GetDailySpent():F2}");
            GUILayout.Label($"Queue Size: {NIMClient.Instance.GetQueueSize()}");
            GUILayout.EndArea();
        }
    }

    [Serializable]
    public class AISystemHealth
    {
        public SystemStatus LocalSystemStatus = SystemStatus.Unknown;
        public SystemStatus CloudSystemStatus = SystemStatus.Unknown;
        public float OverallPerformance = 0f;
        public DateTime LastUpdated = DateTime.Now;
    }

    public enum SystemStatus
    {
        Unknown,
        Healthy,
        Warning,
        Degraded,
        Critical
    }
}
```

## Caching & Performance Optimization

### Multi-Tier Content Caching System

#### Intelligent Cache Architecture
```csharp
// Multi-tier content caching for AI responses
namespace Coalition.AI.Caching
{
    public class ContentCache : MonoBehaviour
    {
        [SerializeField] private int memoryCache Maxsize = 100;
        [SerializeField] private int diskCacheMaxSize = 1000;
        [SerializeField] private string cacheDirectory = "AIContentCache";

        private Dictionary<string, CachedContent> memoryCache = new Dictionary<string, CachedContent>();
        private LRUCache<string, string> diskCache;

        public static ContentCache Instance { get; private set; }

        void Awake()
        {
            Instance = this;
            diskCache = new LRUCache<string, string>(diskCacheMaxSize, cacheDirectory);
        }

        public bool HasCachedContent(ContentRequest request)
        {
            string key = GenerateCacheKey(request);

            // Check memory cache first (fastest)
            if (memoryCache.ContainsKey(key) && !IsExpired(memoryCache[key]))
            {
                return true;
            }

            // Check disk cache (slower but persistent)
            return diskCache.Contains(key);
        }

        public string GetCachedContent(ContentRequest request)
        {
            string key = GenerateCacheKey(request);

            // Try memory cache first
            if (memoryCache.TryGetValue(key, out CachedContent cached) && !IsExpired(cached))
            {
                cached.AccessCount++;
                cached.LastAccessed = DateTime.Now;
                return cached.Content;
            }

            // Try disk cache
            if (diskCache.TryGet(key, out string diskContent))
            {
                // Promote to memory cache
                CacheInMemory(key, diskContent);
                return diskContent;
            }

            return null;
        }

        public void CacheContent(ContentRequest request, string content)
        {
            string key = GenerateCacheKey(request);

            // Cache in memory for fast access
            CacheInMemory(key, content);

            // Cache on disk for persistence
            diskCache.Set(key, content);
        }

        private string GenerateCacheKey(ContentRequest request)
        {
            // Create deterministic hash based on request parameters
            var keyData = $"{request.Type}|{request.EventHash}|{request.OutletType}|{request.PartyId}";
            return ComputeHash(keyData);
        }

        private bool IsExpired(CachedContent cached)
        {
            var expirationTime = cached.Type switch
            {
                ContentType.NewsArticle => TimeSpan.FromHours(2),
                ContentType.SocialMediaPost => TimeSpan.FromMinutes(30),
                ContentType.PoliticalAnalysis => TimeSpan.FromHours(6),
                _ => TimeSpan.FromHours(1)
            };

            return DateTime.Now - cached.Created > expirationTime;
        }

        // Smart cache warming for predicted content needs
        public async Task WarmCacheAsync(List<ContentRequest> predictedRequests)
        {
            var warmingTasks = predictedRequests
                .Where(r => !HasCachedContent(r))
                .Take(5) // Limit concurrent warming
                .Select(r => WarmSingleContentAsync(r));

            await Task.WhenAll(warmingTasks);
        }

        private async Task WarmSingleContentAsync(ContentRequest request)
        {
            try
            {
                string content = await NIMClient.Instance.GenerateContentAsync(
                    request.SystemPrompt,
                    request.UserPrompt
                );
                CacheContent(request, content);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Cache warming failed for {request.Type}: {e.Message}");
            }
        }
    }

    [Serializable]
    public class CachedContent
    {
        public string Content;
        public ContentType Type;
        public DateTime Created;
        public DateTime LastAccessed;
        public int AccessCount;
        public float QualityScore;
    }
}
```

### Predictive Content Generation

#### Smart Prefetching System
```csharp
// Predictive content generation based on political simulation state
namespace Coalition.AI.Prediction
{
    public class ContentPredictor : MonoBehaviour
    {
        [SerializeField] private float predictionInterval = 60f; // seconds
        [SerializeField] private int maxPredictions = 10;

        private PoliticalSimulationState lastState;
        private List<ContentPrediction> activePredictions = new List<ContentPrediction>();

        void Start()
        {
            InvokeRepeating(nameof(PredictUpcomingContent), predictionInterval, predictionInterval);
        }

        private void PredictUpcomingContent()
        {
            var currentState = CoalitionGame.Instance.GetCurrentState();
            var predictions = AnalyzePotentialContent(currentState);

            // Pre-generate high-probability content
            foreach (var prediction in predictions.Where(p => p.Probability > 0.7f))
            {
                _ = PreGenerateContentAsync(prediction);
            }
        }

        private List<ContentPrediction> AnalyzePotentialContent(PoliticalSimulationState state)
        {
            var predictions = new List<ContentPrediction>();

            // Coalition negotiation progress updates
            if (state.IsInNegotiation)
            {
                predictions.Add(new ContentPrediction
                {
                    Type = ContentType.NewsArticle,
                    Event = new PoliticalEvent("Coalition Negotiation Progress", state.NegotiatingParties),
                    Probability = 0.9f,
                    EstimatedTiming = DateTime.Now.AddMinutes(UnityEngine.Random.Range(5, 15))
                });
            }

            // Party response to recent events
            foreach (var party in state.PoliticalParties)
            {
                if (party.ShouldRespondToRecentEvents())
                {
                    predictions.Add(new ContentPrediction
                    {
                        Type = ContentType.SocialMediaPost,
                        Event = new PoliticalEvent("Party Response", new[] { party }),
                        Probability = 0.8f,
                        EstimatedTiming = DateTime.Now.AddMinutes(UnityEngine.Random.Range(2, 10))
                    });
                }
            }

            // Media analysis of policy proposals
            if (state.HasRecentPolicyProposals())
            {
                predictions.Add(new ContentPrediction
                {
                    Type = ContentType.PoliticalAnalysis,
                    Event = new PoliticalEvent("Policy Analysis", state.RecentPolicyProposals),
                    Probability = 0.75f,
                    EstimatedTiming = DateTime.Now.AddMinutes(UnityEngine.Random.Range(10, 30))
                });
            }

            return predictions.OrderByDescending(p => p.Probability).Take(maxPredictions).ToList();
        }

        private async Task PreGenerateContentAsync(ContentPrediction prediction)
        {
            var request = CreateContentRequest(prediction);

            try
            {
                string content = await HybridAIRouter.Instance.RouteRequestAsync(request);
                ContentCache.Instance.CacheContent(request, content);

                Debug.Log($"Pre-generated content for {prediction.Type}: {prediction.Event.Description}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Pre-generation failed: {e.Message}");
            }
        }
    }

    [Serializable]
    public class ContentPrediction
    {
        public ContentType Type;
        public PoliticalEvent Event;
        public float Probability;
        public DateTime EstimatedTiming;
        public bool Generated;
    }
}
```

## Cost Management & Rate Limiting

### Intelligent Cost Control System

#### Budget Management
```csharp
// Comprehensive cost tracking and budget management
namespace Coalition.AI.CostManagement
{
    public class CostTracker : MonoBehaviour
    {
        [SerializeField] private float dailyBudgetUSD = 10.0f;
        [SerializeField] private float weeklyBudgetUSD = 50.0f;
        [SerializeField] private float monthlyBudgetUSD = 150.0f;

        [SerializeField] private CostConfig costConfig;

        private BudgetPeriod currentDay;
        private BudgetPeriod currentWeek;
        private BudgetPeriod currentMonth;

        public static CostTracker Instance { get; private set; }

        void Awake()
        {
            Instance = this;
            LoadBudgetData();
        }

        public bool CanAfford(float estimatedCost)
        {
            UpdateCurrentPeriods();

            return currentDay.SpentAmount + estimatedCost <= dailyBudgetUSD &&
                   currentWeek.SpentAmount + estimatedCost <= weeklyBudgetUSD &&
                   currentMonth.SpentAmount + estimatedCost <= monthlyBudgetUSD;
        }

        public void RecordCost(float cost, AIProvider provider, ContentType contentType)
        {
            UpdateCurrentPeriods();

            var transaction = new CostTransaction
            {
                Amount = cost,
                Provider = provider,
                ContentType = contentType,
                Timestamp = DateTime.Now
            };

            currentDay.RecordTransaction(transaction);
            currentWeek.RecordTransaction(transaction);
            currentMonth.RecordTransaction(transaction);

            SaveBudgetData();

            // Trigger budget alerts if necessary
            CheckBudgetAlerts();
        }

        public float EstimateCost(ContentRequest request, AIProvider provider)
        {
            return provider switch
            {
                AIProvider.NVIDIALocal => 0.0f, // Local processing is "free"
                AIProvider.OpenAI => EstimateOpenAICost(request),
                AIProvider.Anthropic => EstimateAnthropicCost(request),
                _ => 0.0f
            };
        }

        private float EstimateOpenAICost(ContentRequest request)
        {
            int estimatedTokens = EstimateTokenCount(request);

            return request.Model switch
            {
                "gpt-3.5-turbo" => (estimatedTokens / 1000f) * 0.002f, // $0.002 per 1K tokens
                "gpt-4" => (estimatedTokens / 1000f) * 0.03f,         // $0.03 per 1K tokens
                "gpt-4-turbo" => (estimatedTokens / 1000f) * 0.01f,    // $0.01 per 1K tokens
                _ => (estimatedTokens / 1000f) * 0.002f
            };
        }

        private void CheckBudgetAlerts()
        {
            float dailyUsage = currentDay.SpentAmount / dailyBudgetUSD;
            float weeklyUsage = currentWeek.SpentAmount / weeklyBudgetUSD;
            float monthlyUsage = currentMonth.SpentAmount / monthlyBudgetUSD;

            if (dailyUsage > 0.8f)
            {
                EventBus.Instance.Publish(new BudgetAlertEvent(BudgetPeriodType.Daily, dailyUsage));
            }

            if (weeklyUsage > 0.7f)
            {
                EventBus.Instance.Publish(new BudgetAlertEvent(BudgetPeriodType.Weekly, weeklyUsage));
            }

            if (monthlyUsage > 0.6f)
            {
                EventBus.Instance.Publish(new BudgetAlertEvent(BudgetPeriodType.Monthly, monthlyUsage));
            }
        }

        // Automatic cost optimization strategies
        public void OptimizeCosts()
        {
            float currentUsage = GetDailyUsagePercentage();

            if (currentUsage > 0.8f)
            {
                // Switch to more aggressive local processing
                HybridAIRouter.Instance.SetLocalPreference(0.95f);
                Debug.Log("Activated aggressive cost saving mode");
            }
            else if (currentUsage > 0.6f)
            {
                // Reduce cloud model complexity
                HybridAIRouter.Instance.SetCloudModelTier(CloudModelTier.Economy);
                Debug.Log("Reduced cloud model complexity for cost savings");
            }
        }
    }

    [Serializable]
    public class CostConfig
    {
        [Header("OpenAI Pricing (per 1K tokens)")]
        public float gpt35TurboPrice = 0.002f;
        public float gpt4Price = 0.03f;
        public float gpt4TurboPrice = 0.01f;

        [Header("Anthropic Pricing (per 1K tokens)")]
        public float claudeHaikuPrice = 0.00025f;
        public float claudeSonnetPrice = 0.003f;
        public float claudeOpusPrice = 0.015f;

        [Header("Local Processing")]
        public float electricityCostPerkWh = 0.12f;
        public float gpuPowerConsumption = 450f; // Watts for RTX 4090
    }
}
```

### Rate Limiting & Quality Control

#### Adaptive Rate Limiting
```csharp
// Intelligent rate limiting to prevent system overload
namespace Coalition.AI.RateLimit
{
    public class AdaptiveRateLimiter : MonoBehaviour
    {
        [SerializeField] private int baseRequestsPerMinute = 30;
        [SerializeField] private int maxRequestsPerMinute = 60;
        [SerializeField] private float adaptationSpeed = 0.1f;

        private int currentRequestsPerMinute;
        private Queue<DateTime> requestTimes = new Queue<DateTime>();
        private float systemHealthScore = 1.0f;

        public async Task<bool> WaitForRateLimit()
        {
            CleanOldRequests();

            if (requestTimes.Count >= currentRequestsPerMinute)
            {
                var oldestRequest = requestTimes.Peek();
                var waitTime = 60 - (DateTime.Now - oldestRequest).TotalSeconds;

                if (waitTime > 0)
                {
                    await Task.Delay((int)(waitTime * 1000));
                }
            }

            requestTimes.Enqueue(DateTime.Now);
            return true;
        }

        void Update()
        {
            UpdateSystemHealth();
            AdaptRateLimit();
        }

        private void UpdateSystemHealth()
        {
            var health = AISystemMonitor.Instance.GetSystemHealth();

            systemHealthScore = health.LocalSystemStatus switch
            {
                SystemStatus.Healthy => 1.0f,
                SystemStatus.Warning => 0.8f,
                SystemStatus.Degraded => 0.5f,
                SystemStatus.Critical => 0.2f,
                _ => 0.5f
            };
        }

        private void AdaptRateLimit()
        {
            int targetRate = Mathf.RoundToInt(
                Mathf.Lerp(baseRequestsPerMinute, maxRequestsPerMinute, systemHealthScore)
            );

            currentRequestsPerMinute = Mathf.RoundToInt(
                Mathf.Lerp(currentRequestsPerMinute, targetRate, adaptationSpeed * Time.deltaTime)
            );
        }

        private void CleanOldRequests()
        {
            var cutoffTime = DateTime.Now.AddMinutes(-1);

            while (requestTimes.Count > 0 && requestTimes.Peek() < cutoffTime)
            {
                requestTimes.Dequeue();
            }
        }
    }
}
```

## Development Workflow Integration

### AI-Assisted Development Patterns

#### Development Automation Tools
```csharp
// AI-powered development assistance for Coalition project
namespace Coalition.Development.AI
{
    public class AIDevAssistant : MonoBehaviour
    {
        [SerializeField] private string devAssistantEndpoint = "http://localhost:8001";
        [SerializeField] private bool enableAutoTesting = true;
        [SerializeField] private bool enableCodeReview = true;

        public async Task<string> GenerateUnitTestAsync(string sourceCode, string className)
        {
            var request = new NIMRequest
            {
                messages = new[]
                {
                    new NIMRequest.Message
                    {
                        role = "system",
                        content = "You are a Unity C# test generation expert. Generate comprehensive unit tests using NUnit framework."
                    },
                    new NIMRequest.Message
                    {
                        role = "user",
                        content = $"Generate unit tests for this C# class:\n\n{sourceCode}"
                    }
                }
            };

            return await SendDevAssistantRequest(request);
        }

        public async Task<string> ReviewCodeAsync(string sourceCode)
        {
            var request = new NIMRequest
            {
                messages = new[]
                {
                    new NIMRequest.Message
                    {
                        role = "system",
                        content = "You are a senior Unity C# developer. Review code for best practices, performance, and maintainability."
                    },
                    new NIMRequest.Message
                    {
                        role = "user",
                        content = $"Review this C# code:\n\n{sourceCode}"
                    }
                }
            };

            return await SendDevAssistantRequest(request);
        }

        public async Task<string> OptimizePerformanceAsync(string sourceCode)
        {
            var request = new NIMRequest
            {
                messages = new[]
                {
                    new NIMRequest.Message
                    {
                        role = "system",
                        content = "You are a Unity performance optimization expert. Suggest specific optimizations for better performance."
                    },
                    new NIMRequest.Message
                    {
                        role = "user",
                        content = $"Optimize this Unity C# code:\n\n{sourceCode}"
                    }
                }
            };

            return await SendDevAssistantRequest(request);
        }

        private async Task<string> SendDevAssistantRequest(NIMRequest request)
        {
            // Use dedicated development assistant instance
            // This could be a different model or endpoint optimized for code
            using (UnityWebRequest webRequest = UnityWebRequest.Post($"{devAssistantEndpoint}/v1/chat/completions", JsonConvert.SerializeObject(request), "application/json"))
            {
                webRequest.SetRequestHeader("Authorization", "Bearer dev-assistant-key");

                var operation = webRequest.SendWebRequest();
                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    var response = JsonConvert.DeserializeObject<NIMResponse>(webRequest.downloadHandler.text);
                    return response.choices[0].message.content;
                }
                else
                {
                    throw new Exception($"Dev assistant request failed: {webRequest.error}");
                }
            }
        }
    }

    // Integration with Unity Editor for development workflow
    #if UNITY_EDITOR
    using UnityEditor;

    public class AIDevMenu : MonoBehaviour
    {
        [MenuItem("Coalition AI/Generate Tests for Selection")]
        public static async void GenerateTestsForSelection()
        {
            var selectedScript = Selection.activeObject as MonoScript;
            if (selectedScript != null)
            {
                string sourceCode = selectedScript.text;
                string className = selectedScript.GetClass().Name;

                var assistant = FindObjectOfType<AIDevAssistant>();
                if (assistant != null)
                {
                    try
                    {
                        string tests = await assistant.GenerateUnitTestAsync(sourceCode, className);

                        // Save tests to appropriate location
                        string testPath = $"Assets/Tests/{className}Tests.cs";
                        System.IO.File.WriteAllText(testPath, tests);
                        AssetDatabase.Refresh();

                        Debug.Log($"Generated tests for {className} at {testPath}");
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Test generation failed: {e.Message}");
                    }
                }
            }
        }

        [MenuItem("Coalition AI/Review Selected Code")]
        public static async void ReviewSelectedCode()
        {
            var selectedScript = Selection.activeObject as MonoScript;
            if (selectedScript != null)
            {
                string sourceCode = selectedScript.text;

                var assistant = FindObjectOfType<AIDevAssistant>();
                if (assistant != null)
                {
                    try
                    {
                        string review = await assistant.ReviewCodeAsync(sourceCode);

                        // Display review in console and/or custom window
                        Debug.Log($"Code Review for {selectedScript.name}:\n{review}");
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Code review failed: {e.Message}");
                    }
                }
            }
        }
    }
    #endif
}
```

### Continuous Integration Support

#### CI/CD Pipeline Integration
```yaml
# .github/workflows/coalition-ai-pipeline.yml
name: Coalition AI Development Pipeline

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  nvidia-nim-tests:
    runs-on: ubuntu-latest
    services:
      nim-container:
        image: nvcr.io/nim/meta/llama3-8b-instruct:1.0.0
        ports:
          - 8000:8000
        env:
          NIM_MODEL_PROFILE: ci_testing
          NIM_MAX_BATCH_SIZE: 4
        options: --gpus all

    steps:
    - uses: actions/checkout@v4

    - name: Setup Unity Test Runner
      uses: game-ci/unity-test-runner@v4
      with:
        projectPath: CoalitionGame
        testMode: EditMode

    - name: Run AI Integration Tests
      run: |
        # Wait for NIM container to be ready
        timeout 300 bash -c 'until curl -f http://localhost:8000/v1/health; do sleep 5; done'

        # Run Unity tests with AI integration
        unity-test-runner \
          --testCategories "AIIntegration" \
          --environment NIM_ENDPOINT=http://localhost:8000

    - name: Performance Benchmarks
      run: |
        # Run AI performance benchmarks
        python scripts/benchmark_ai_performance.py \
          --nim-endpoint http://localhost:8000 \
          --output reports/ai_benchmarks.json

    - name: Upload Test Results
      uses: actions/upload-artifact@v4
      with:
        name: ai-test-results
        path: |
          reports/
          TestResults/
```

## Implementation Roadmap

### Phase 1: Foundation Setup (Weeks 1-2)
```yaml
Week_1:
  nvidia_nim_setup:
    - Install NVIDIA Container Toolkit
    - Deploy Llama3-8B-Instruct container
    - Validate GPU acceleration and performance
    - Configure local endpoint security

  unity_integration_basic:
    - Implement NIMClient base class
    - Create HTTP request/response handling
    - Add basic error handling and retries
    - Implement health check system

Week_2:
  content_generation_mvp:
    - Create DutchNewsGenerator class
    - Implement basic media outlet personas
    - Add simple social media generation
    - Test with Coalition political events

  testing_framework:
    - Create AI integration test suite
    - Add performance benchmarking tools
    - Implement automated testing pipeline
    - Validate content quality metrics
```

### Phase 2: Hybrid Architecture (Weeks 3-4)
```yaml
Week_3:
  hybrid_routing:
    - Implement HybridAIRouter
    - Add local/cloud decision logic
    - Create fallback mechanisms
    - Build cost tracking system

  caching_system:
    - Implement ContentCache with LRU
    - Add persistent disk caching
    - Create cache warming strategies
    - Build cache invalidation logic

Week_4:
  performance_optimization:
    - Add adaptive rate limiting
    - Implement background processing
    - Create predictive content generation
    - Optimize memory usage patterns

  monitoring_dashboard:
    - Build AISystemMonitor
    - Add GPU temperature monitoring
    - Create cost visualization
    - Implement alert system
```

### Phase 3: Advanced Features (Weeks 5-6)
```yaml
Week_5:
  content_sophistication:
    - Enhanced political persona prompts
    - Multi-turn conversation support
    - Cross-platform content coordination
    - Dynamic bias application system

  development_integration:
    - AI-assisted code generation
    - Automated test creation
    - Performance optimization suggestions
    - Code review automation

Week_6:
  production_readiness:
    - Security hardening
    - Production deployment scripts
    - Monitoring and logging
    - Documentation completion

  optimization_polish:
    - Performance profiling and tuning
    - Cost optimization strategies
    - Quality assurance automation
    - User experience refinement
```

### Success Metrics & Validation

#### Technical Performance KPIs
```yaml
Response_Time:
  local_inference: "<500ms average"
  cloud_fallback: "<2000ms average"
  cache_hits: "<50ms average"

Resource_Utilization:
  gpu_memory: "<90% peak usage"
  system_memory: "<16GB typical"
  cpu_usage: "<30% average"

Cost_Management:
  daily_budget_adherence: ">95%"
  local_processing_ratio: ">80%"
  cost_per_content_piece: "<$0.02"

Quality_Metrics:
  content_authenticity: ">90% human evaluation"
  dutch_political_accuracy: ">95% fact-check"
  media_style_consistency: ">85% recognition"
```

#### Content Quality Standards
```yaml
Authenticity_Validation:
  dutch_media_style: "Recognizable by Dutch journalism professionals"
  political_accuracy: "Factually correct Dutch political context"
  language_quality: "Natural Dutch political communication"

Consistency_Metrics:
  media_outlet_personality: "Consistent voice across generated content"
  political_party_positions: "Aligned with established party ideologies"
  temporal_coherence: "Content reflects current simulation state"
```

This comprehensive architecture provides Coalition with a robust, cost-effective, and scalable AI integration that leverages NVIDIA NIM's local processing capabilities while maintaining the flexibility and reliability needed for a complex political simulation game. The hybrid approach ensures optimal performance, cost management, and content quality while supporting the game's real-time social media generation requirements.