# AI Response Optimization Strategy
## Request Batching and Smart Caching for NVIDIA NIM Integration

### 🎯 **OBJECTIVE**
Optimize AI response performance through intelligent request batching, smart caching strategies, semantic similarity detection, and adaptive timeout handling to reduce latency and improve user experience in the COALITION game.

---

## 🔍 **CURRENT AI SYSTEM ANALYSIS**

### Existing Implementation Issues
1. **Sequential Processing**: Each AI request processed individually
2. **No Request Deduplication**: Similar prompts sent multiple times
3. **Basic Caching**: Simple response storage without semantic awareness
4. **Fixed Timeouts**: No adaptive timeout strategies
5. **No Batch Processing**: Missed opportunities for efficiency gains

### Performance Bottlenecks
```csharp
// Current NIMClient implementation issues:
public async Task<string> SendRequestAsync(string prompt)
{
    // Issues:
    // 1. Each request is individual HTTP call
    // 2. No deduplication of similar prompts
    // 3. Fixed timeout regardless of request complexity
    // 4. No batch processing capabilities
    // 5. Simple cache without semantic understanding
}
```

---

## 🏗️ **AI OPTIMIZATION ARCHITECTURE**

### Core Components
1. **Request Batching Engine**: Intelligent batching with size and time optimization
2. **Semantic Cache Manager**: Context-aware caching with similarity detection
3. **Request Deduplication**: Eliminate redundant requests
4. **Adaptive Timeout Controller**: Dynamic timeout based on request complexity
5. **Response Distribution System**: Efficient routing of batch responses
6. **Priority Queue Manager**: Handle urgent vs. background requests

---

## 📋 **IMPLEMENTATION COMPONENTS**

### Component 1: AI Request Batching Engine
```csharp
/// <summary>
/// Intelligent AI request batching with optimization for throughput and latency
/// </summary>
public class AIRequestBatchingEngine : IDisposable
{
    private readonly ConcurrentQueue<AIRequest> _pendingRequests = new();
    private readonly ConcurrentDictionary<string, TaskCompletionSource<AIResponse>> _responseWaiters = new();
    private readonly SemaphoreSlim _processingLock = new(1, 1);
    private readonly Timer _batchTimer;

    [Header("Batching Configuration")]
    [SerializeField] private int maxBatchSize = 5;
    [SerializeField] private float batchTimeoutMs = 100f; // 100ms batch window
    [SerializeField] private int maxConcurrentBatches = 3;
    [SerializeField] private float priorityRequestTimeoutMs = 50f; // Faster batching for priority

    private volatile bool _isProcessing = false;
    private int _activeBatches = 0;

    // Performance metrics
    private long _totalRequests = 0;
    private long _batchedRequests = 0;
    private long _immediateRequests = 0;
    private readonly List<float> _batchSizeHistory = new();

    public AIRequestBatchingEngine()
    {
        _batchTimer = new Timer(ProcessBatchTimer, null,
            TimeSpan.FromMilliseconds(batchTimeoutMs),
            TimeSpan.FromMilliseconds(batchTimeoutMs));
    }

    /// <summary>
    /// Submit AI request for batched processing
    /// </summary>
    public async Task<AIResponse> SubmitRequestAsync(AIRequest request)
    {
        Interlocked.Increment(ref _totalRequests);

        using (COALITIONProfilerMarkers.AIRequest.Auto())
        {
            // Check for immediate processing conditions
            if (ShouldProcessImmediately(request))
            {
                Interlocked.Increment(ref _immediateRequests);
                return await ProcessSingleRequestAsync(request);
            }

            // Add to batch queue
            var tcs = new TaskCompletionSource<AIResponse>();
            _responseWaiters[request.RequestId] = tcs;
            _pendingRequests.Enqueue(request);

            Interlocked.Increment(ref _batchedRequests);

            // Trigger immediate batch processing if conditions are met
            if (_pendingRequests.Count >= maxBatchSize || request.Priority == RequestPriority.High)
            {
                _ = Task.Run(ProcessBatchAsync);
            }

            return await tcs.Task;
        }
    }

    private bool ShouldProcessImmediately(AIRequest request)
    {
        // Process immediately if:
        // 1. Critical priority requests
        // 2. Too many active batches
        // 3. System under memory pressure
        return request.Priority == RequestPriority.Critical ||
               _activeBatches >= maxConcurrentBatches ||
               MemoryPressureMonitor.Instance.GetMemoryStatistics().IsHighPressure;
    }

    private async Task<AIResponse> ProcessSingleRequestAsync(AIRequest request)
    {
        using (COALITIONProfilerMarkers.AIProcessing.Auto())
        {
            // Use existing NIM client for single requests
            var nimClient = ServiceLocator.Get<NIMClient>();
            var response = await nimClient.SendRequestAsync(request.Prompt, request.GetAdaptiveTimeout());

            return new AIResponse
            {
                RequestId = request.RequestId,
                Content = response,
                ProcessingTime = DateTime.UtcNow - request.Timestamp,
                IsBatched = false
            };
        }
    }

    private void ProcessBatchTimer(object state)
    {
        if (_pendingRequests.Count > 0 && !_isProcessing)
        {
            _ = Task.Run(ProcessBatchAsync);
        }
    }

    private async Task ProcessBatchAsync()
    {
        if (_isProcessing || _pendingRequests.IsEmpty)
            return;

        await _processingLock.WaitAsync();
        try
        {
            _isProcessing = true;
            Interlocked.Increment(ref _activeBatches);

            var batch = CollectBatch();
            if (batch.Count == 0)
                return;

            _batchSizeHistory.Add(batch.Count);
            if (_batchSizeHistory.Count > 100) // Keep last 100 batch sizes
                _batchSizeHistory.RemoveAt(0);

            using (COALITIONProfilerMarkers.AIBatchRequest.Auto())
            {
                var responses = await ProcessBatchRequestsAsync(batch);
                DistributeBatchResponses(batch, responses);
            }
        }
        finally
        {
            _isProcessing = false;
            Interlocked.Decrement(ref _activeBatches);
            _processingLock.Release();
        }
    }

    private List<AIRequest> CollectBatch()
    {
        var batch = new List<AIRequest>();
        var batchSizeLimit = GetOptimalBatchSize();

        while (batch.Count < batchSizeLimit && _pendingRequests.TryDequeue(out var request))
        {
            batch.Add(request);
        }

        return batch;
    }

    private int GetOptimalBatchSize()
    {
        // Adaptive batch sizing based on historical performance
        if (_batchSizeHistory.Count < 10)
            return maxBatchSize;

        var recentAverage = _batchSizeHistory.TakeLast(10).Average();
        var optimalSize = (int)Math.Min(maxBatchSize, Math.Max(1, recentAverage * 1.2f));

        return optimalSize;
    }

    private async Task<List<AIResponse>> ProcessBatchRequestsAsync(List<AIRequest> requests)
    {
        var responses = new List<AIResponse>();
        var nimClient = ServiceLocator.Get<NIMClient>();

        // Group requests by similarity for further optimization
        var groupedRequests = GroupRequestsBySimilarity(requests);

        foreach (var group in groupedRequests)
        {
            if (group.Count == 1)
            {
                // Single request in group
                var request = group[0];
                var response = await ProcessSingleRequestInBatch(nimClient, request);
                responses.Add(response);
            }
            else
            {
                // Multiple similar requests - use template approach
                var templateResponse = await ProcessTemplateRequest(nimClient, group);
                responses.AddRange(templateResponse);
            }
        }

        return responses;
    }

    private List<List<AIRequest>> GroupRequestsBySimilarity(List<AIRequest> requests)
    {
        var groups = new List<List<AIRequest>>();
        var processed = new HashSet<string>();

        foreach (var request in requests)
        {
            if (processed.Contains(request.RequestId))
                continue;

            var similarGroup = new List<AIRequest> { request };
            processed.Add(request.RequestId);

            // Find similar requests
            foreach (var otherRequest in requests)
            {
                if (processed.Contains(otherRequest.RequestId))
                    continue;

                if (CalculateSimilarity(request.Prompt, otherRequest.Prompt) > 0.8f)
                {
                    similarGroup.Add(otherRequest);
                    processed.Add(otherRequest.RequestId);
                }
            }

            groups.Add(similarGroup);
        }

        return groups;
    }

    private float CalculateSimilarity(string prompt1, string prompt2)
    {
        // Simplified similarity calculation
        // In production, use more sophisticated similarity algorithms
        var words1 = prompt1.ToLowerInvariant().Split(' ').ToHashSet();
        var words2 = prompt2.ToLowerInvariant().Split(' ').ToHashSet();

        var intersection = words1.Intersect(words2).Count();
        var union = words1.Union(words2).Count();

        return union > 0 ? (float)intersection / union : 0f;
    }

    private async Task<AIResponse> ProcessSingleRequestInBatch(NIMClient client, AIRequest request)
    {
        var startTime = DateTime.UtcNow;
        var response = await client.SendRequestAsync(request.Prompt, request.GetAdaptiveTimeout());

        return new AIResponse
        {
            RequestId = request.RequestId,
            Content = response,
            ProcessingTime = DateTime.UtcNow - startTime,
            IsBatched = true
        };
    }

    private async Task<List<AIResponse>> ProcessTemplateRequest(NIMClient client, List<AIRequest> similarRequests)
    {
        // Use the first request as template and adapt for others
        var templateRequest = similarRequests[0];
        var templatePrompt = CreateTemplatePrompt(similarRequests);

        var startTime = DateTime.UtcNow;
        var templateResponse = await client.SendRequestAsync(templatePrompt, templateRequest.GetAdaptiveTimeout());
        var processingTime = DateTime.UtcNow - startTime;

        // Adapt template response for each request
        var responses = new List<AIResponse>();
        foreach (var request in similarRequests)
        {
            var adaptedResponse = AdaptResponseForRequest(templateResponse, request);
            responses.Add(new AIResponse
            {
                RequestId = request.RequestId,
                Content = adaptedResponse,
                ProcessingTime = processingTime,
                IsBatched = true,
                IsTemplateAdapted = true
            });
        }

        return responses;
    }

    private string CreateTemplatePrompt(List<AIRequest> requests)
    {
        // Create a generic prompt that can serve multiple similar requests
        var commonElements = ExtractCommonElements(requests.Select(r => r.Prompt));
        return $"Provide a comprehensive response covering: {string.Join(", ", commonElements)}";
    }

    private List<string> ExtractCommonElements(IEnumerable<string> prompts)
    {
        // Extract common themes/keywords from prompts
        var allWords = prompts.SelectMany(p => p.ToLowerInvariant().Split(' '))
            .Where(w => w.Length > 3) // Filter short words
            .GroupBy(w => w)
            .Where(g => g.Count() > 1) // Words that appear in multiple prompts
            .OrderByDescending(g => g.Count())
            .Take(5) // Top 5 common elements
            .Select(g => g.Key)
            .ToList();

        return allWords;
    }

    private string AdaptResponseForRequest(string templateResponse, AIRequest request)
    {
        // Adapt the template response to be more specific for the individual request
        // This is a simplified adaptation - in production, use more sophisticated methods
        var adaptationPrefix = $"Specifically regarding '{request.GetContext()}': ";
        return adaptationPrefix + templateResponse;
    }

    private void DistributeBatchResponses(List<AIRequest> requests, List<AIResponse> responses)
    {
        for (int i = 0; i < requests.Count && i < responses.Count; i++)
        {
            var request = requests[i];
            var response = responses[i];

            if (_responseWaiters.TryRemove(request.RequestId, out var tcs))
            {
                tcs.SetResult(response);
            }
        }
    }

    /// <summary>
    /// Get batching performance statistics
    /// </summary>
    public BatchingStatistics GetStatistics()
    {
        return new BatchingStatistics
        {
            TotalRequests = _totalRequests,
            BatchedRequests = _batchedRequests,
            ImmediateRequests = _immediateRequests,
            BatchingEfficiency = _totalRequests > 0 ? (float)_batchedRequests / _totalRequests : 0f,
            AverageBatchSize = _batchSizeHistory.Count > 0 ? _batchSizeHistory.Average() : 0f,
            ActiveBatches = _activeBatches,
            PendingRequests = _pendingRequests.Count
        };
    }

    public void Dispose()
    {
        _batchTimer?.Dispose();
        _processingLock?.Dispose();

        // Complete any pending requests
        while (_responseWaiters.TryGetValue("", out var tcs))
        {
            tcs.SetCanceled();
        }
    }
}

[System.Serializable]
public class BatchingStatistics
{
    public long TotalRequests;
    public long BatchedRequests;
    public long ImmediateRequests;
    public float BatchingEfficiency;
    public float AverageBatchSize;
    public int ActiveBatches;
    public int PendingRequests;
}
```

### Component 2: Enhanced AI Request Model
```csharp
/// <summary>
/// Enhanced AI request with context, priority, and adaptive timeout capabilities
/// </summary>
public class AIRequest
{
    public string RequestId { get; set; }
    public string Prompt { get; set; }
    public RequestPriority Priority { get; set; }
    public string Context { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
    public int ExpectedResponseLength { get; set; }
    public string RequestType { get; set; }

    public AIRequest()
    {
        RequestId = Guid.NewGuid().ToString();
        Timestamp = DateTime.UtcNow;
        Metadata = new Dictionary<string, object>();
        Priority = RequestPriority.Normal;
        ExpectedResponseLength = 500; // Default expected length
    }

    /// <summary>
    /// Calculate adaptive timeout based on request complexity
    /// </summary>
    public TimeSpan GetAdaptiveTimeout()
    {
        var baseTimeout = 30000; // 30 seconds base

        // Adjust based on prompt length
        var lengthMultiplier = Math.Max(1.0f, Prompt.Length / 1000.0f);

        // Adjust based on expected response length
        var responseMultiplier = Math.Max(1.0f, ExpectedResponseLength / 500.0f);

        // Adjust based on priority
        var priorityMultiplier = Priority switch
        {
            RequestPriority.Critical => 0.5f,
            RequestPriority.High => 0.7f,
            RequestPriority.Normal => 1.0f,
            RequestPriority.Low => 1.5f,
            _ => 1.0f
        };

        var adaptiveTimeout = (int)(baseTimeout * lengthMultiplier * responseMultiplier * priorityMultiplier);
        return TimeSpan.FromMilliseconds(Math.Min(adaptiveTimeout, 120000)); // Max 2 minutes
    }

    /// <summary>
    /// Get request context for template adaptation
    /// </summary>
    public string GetContext()
    {
        return !string.IsNullOrEmpty(Context) ? Context : ExtractContextFromPrompt();
    }

    private string ExtractContextFromPrompt()
    {
        // Extract key context from prompt (simplified)
        var sentences = Prompt.Split('.', '!', '?');
        return sentences.Length > 0 ? sentences[0].Trim() : Prompt.Substring(0, Math.Min(100, Prompt.Length));
    }

    /// <summary>
    /// Calculate request complexity score
    /// </summary>
    public float GetComplexityScore()
    {
        var score = 0f;

        // Prompt length factor
        score += Math.Min(1.0f, Prompt.Length / 2000.0f) * 0.4f;

        // Expected response length factor
        score += Math.Min(1.0f, ExpectedResponseLength / 1000.0f) * 0.3f;

        // Request type factor
        score += RequestType switch
        {
            "Analysis" => 0.3f,
            "Generation" => 0.2f,
            "Simple" => 0.1f,
            _ => 0.15f
        };

        return Math.Min(1.0f, score);
    }
}

public enum RequestPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Critical = 3
}

[System.Serializable]
public class AIResponse
{
    public string RequestId { get; set; }
    public string Content { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsBatched { get; set; }
    public bool IsTemplateAdapted { get; set; }
    public bool IsCached { get; set; }
    public float CacheConfidence { get; set; }
    public Dictionary<string, object> Metadata { get; set; }

    public AIResponse()
    {
        Timestamp = DateTime.UtcNow;
        Metadata = new Dictionary<string, object>();
    }
}
```

### Component 3: Semantic AI Cache Manager
```csharp
/// <summary>
/// Advanced AI response cache with semantic similarity detection and context awareness
/// </summary>
public class SemanticAICacheManager : IDisposable
{
    private readonly LRUCache<string, CachedAIResponse> _exactCache;
    private readonly LRUCache<string, List<CachedAIResponse>> _semanticCache;
    private readonly SemaphoreSlim _cacheLock = new(1, 1);

    // Semantic similarity configuration
    [SerializeField] private float semanticThreshold = 0.75f;
    [SerializeField] private int maxSemanticMatches = 5;
    [SerializeField] private float contextWeight = 0.3f;
    [SerializeField] private float promptWeight = 0.7f;

    // Cache performance metrics
    private long _exactHits = 0;
    private long _semanticHits = 0;
    private long _misses = 0;

    public SemanticAICacheManager(int exactCacheSize = 500, int semanticCacheSize = 1000)
    {
        _exactCache = new LRUCache<string, CachedAIResponse>(exactCacheSize);
        _semanticCache = new LRUCache<string, List<CachedAIResponse>>(semanticCacheSize);
    }

    /// <summary>
    /// Attempt to get cached response for AI request
    /// </summary>
    public async Task<AIResponse> GetCachedResponseAsync(AIRequest request)
    {
        using (COALITIONProfilerMarkers.AICacheHit.Auto())
        {
            // Try exact match first
            var exactKey = GenerateExactKey(request);
            if (_exactCache.TryGet(exactKey, out var exactMatch))
            {
                Interlocked.Increment(ref _exactHits);
                Debug.Log("[SemanticAICache] Exact cache hit");
                return ConvertToAIResponse(exactMatch, request.RequestId, true, 1.0f);
            }

            // Try semantic match
            var semanticMatch = await FindSemanticMatchAsync(request);
            if (semanticMatch != null)
            {
                Interlocked.Increment(ref _semanticHits);
                Debug.Log($"[SemanticAICache] Semantic cache hit (confidence: {semanticMatch.Confidence:F2})");
                return ConvertToAIResponse(semanticMatch.Response, request.RequestId, true, semanticMatch.Confidence);
            }

            Interlocked.Increment(ref _misses);
            return null;
        }
    }

    /// <summary>
    /// Cache AI response with semantic indexing
    /// </summary>
    public async Task CacheResponseAsync(AIRequest request, AIResponse response)
    {
        await _cacheLock.WaitAsync();
        try
        {
            var cachedResponse = new CachedAIResponse
            {
                OriginalRequest = request,
                Response = response,
                CacheTime = DateTime.UtcNow,
                AccessCount = 1,
                SemanticVector = await GenerateSemanticVectorAsync(request)
            };

            // Store in exact cache
            var exactKey = GenerateExactKey(request);
            _exactCache.Set(exactKey, cachedResponse);

            // Store in semantic cache
            await IndexForSemanticSearchAsync(cachedResponse);

            Debug.Log($"[SemanticAICache] Cached response for request: {request.RequestId}");
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    private string GenerateExactKey(AIRequest request)
    {
        // Create exact match key including context
        var keyComponents = new[]
        {
            request.Prompt.Trim(),
            request.Context ?? "",
            request.RequestType ?? ""
        };

        var combinedKey = string.Join("|", keyComponents);
        return ComputeHash(combinedKey);
    }

    private async Task<SemanticMatch> FindSemanticMatchAsync(AIRequest request)
    {
        var requestVector = await GenerateSemanticVectorAsync(request);
        var bestMatch = await FindBestSemanticMatchAsync(requestVector, request);

        return bestMatch?.Confidence >= semanticThreshold ? bestMatch : null;
    }

    private async Task<float[]> GenerateSemanticVectorAsync(AIRequest request)
    {
        // Simplified semantic vector generation
        // In production, use advanced embeddings (e.g., sentence transformers)

        var promptVector = GenerateTextVector(request.Prompt);
        var contextVector = GenerateTextVector(request.Context ?? "");

        // Combine vectors with weights
        var combinedVector = new float[promptVector.Length];
        for (int i = 0; i < promptVector.Length; i++)
        {
            combinedVector[i] = promptVector[i] * promptWeight + contextVector[i] * contextWeight;
        }

        return combinedVector;
    }

    private float[] GenerateTextVector(string text)
    {
        // Simplified text vectorization using word frequency
        var words = text.ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 2)
            .ToList();

        var vector = new float[100]; // Fixed size vector

        // Hash words to vector positions and count frequencies
        foreach (var word in words)
        {
            var hash = Math.Abs(word.GetHashCode()) % vector.Length;
            vector[hash] += 1.0f;
        }

        // Normalize vector
        var magnitude = (float)Math.Sqrt(vector.Sum(v => v * v));
        if (magnitude > 0)
        {
            for (int i = 0; i < vector.Length; i++)
            {
                vector[i] /= magnitude;
            }
        }

        return vector;
    }

    private async Task<SemanticMatch> FindBestSemanticMatchAsync(float[] requestVector, AIRequest request)
    {
        SemanticMatch bestMatch = null;
        var bestSimilarity = 0f;

        // Search through semantic cache
        var semanticKey = GenerateSemanticKey(request);
        var candidateKeys = GenerateSemanticSearchKeys(semanticKey);

        foreach (var key in candidateKeys)
        {
            if (_semanticCache.TryGet(key, out var candidates))
            {
                foreach (var candidate in candidates)
                {
                    var similarity = CalculateCosineSimilarity(requestVector, candidate.SemanticVector);

                    if (similarity > bestSimilarity)
                    {
                        bestSimilarity = similarity;
                        bestMatch = new SemanticMatch
                        {
                            Response = candidate,
                            Confidence = similarity
                        };
                    }
                }
            }
        }

        return bestMatch;
    }

    private float CalculateCosineSimilarity(float[] vector1, float[] vector2)
    {
        if (vector1.Length != vector2.Length)
            return 0f;

        var dotProduct = 0f;
        var magnitude1 = 0f;
        var magnitude2 = 0f;

        for (int i = 0; i < vector1.Length; i++)
        {
            dotProduct += vector1[i] * vector2[i];
            magnitude1 += vector1[i] * vector1[i];
            magnitude2 += vector2[i] * vector2[i];
        }

        var denominator = (float)(Math.Sqrt(magnitude1) * Math.Sqrt(magnitude2));
        return denominator > 0 ? dotProduct / denominator : 0f;
    }

    private async Task IndexForSemanticSearchAsync(CachedAIResponse cachedResponse)
    {
        var semanticKey = GenerateSemanticKey(cachedResponse.OriginalRequest);

        if (!_semanticCache.TryGet(semanticKey, out var existingList))
        {
            existingList = new List<CachedAIResponse>();
        }

        existingList.Add(cachedResponse);

        // Limit list size to prevent unbounded growth
        if (existingList.Count > maxSemanticMatches)
        {
            existingList = existingList.OrderByDescending(r => r.AccessCount).Take(maxSemanticMatches).ToList();
        }

        _semanticCache.Set(semanticKey, existingList);
    }

    private string GenerateSemanticKey(AIRequest request)
    {
        // Generate key based on request type and major themes
        var themes = ExtractMajorThemes(request.Prompt);
        var keyComponents = new[] { request.RequestType ?? "general" }.Concat(themes.Take(3));
        return string.Join("_", keyComponents);
    }

    private List<string> GenerateSemanticSearchKeys(string primaryKey)
    {
        // Generate variations of the semantic key for broader search
        var keys = new List<string> { primaryKey };

        var parts = primaryKey.Split('_');
        if (parts.Length > 1)
        {
            // Add partial keys
            keys.Add(parts[0]); // Request type only
            keys.Add(string.Join("_", parts.Take(2))); // Type + first theme
        }

        return keys;
    }

    private List<string> ExtractMajorThemes(string text)
    {
        // Extract major themes/keywords from text
        var words = text.ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 4) // Longer words more likely to be themes
            .GroupBy(w => w)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => g.Key)
            .ToList();

        return words;
    }

    private string ComputeHash(string input)
    {
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(hashBytes);
        }
    }

    private AIResponse ConvertToAIResponse(CachedAIResponse cached, string requestId, bool isCached, float confidence)
    {
        cached.AccessCount++;

        return new AIResponse
        {
            RequestId = requestId,
            Content = cached.Response.Content,
            ProcessingTime = TimeSpan.FromMilliseconds(1), // Cache hit is very fast
            Timestamp = DateTime.UtcNow,
            IsCached = isCached,
            CacheConfidence = confidence,
            Metadata = new Dictionary<string, object>
            {
                ["OriginalRequestId"] = cached.OriginalRequest.RequestId,
                ["CacheTime"] = cached.CacheTime,
                ["AccessCount"] = cached.AccessCount
            }
        };
    }

    /// <summary>
    /// Get cache performance statistics
    /// </summary>
    public CacheStatistics GetStatistics()
    {
        var totalRequests = _exactHits + _semanticHits + _misses;

        return new CacheStatistics
        {
            ExactHits = _exactHits,
            SemanticHits = _semanticHits,
            Misses = _misses,
            TotalRequests = totalRequests,
            ExactHitRate = totalRequests > 0 ? (float)_exactHits / totalRequests : 0f,
            SemanticHitRate = totalRequests > 0 ? (float)_semanticHits / totalRequests : 0f,
            OverallHitRate = totalRequests > 0 ? (float)(_exactHits + _semanticHits) / totalRequests : 0f,
            ExactCacheSize = _exactCache.GetStatistics().Count,
            SemanticCacheSize = _semanticCache.GetStatistics().Count
        };
    }

    public void Dispose()
    {
        _exactCache?.Dispose();
        _semanticCache?.Dispose();
        _cacheLock?.Dispose();
    }
}

[System.Serializable]
public class CachedAIResponse
{
    public AIRequest OriginalRequest { get; set; }
    public AIResponse Response { get; set; }
    public DateTime CacheTime { get; set; }
    public int AccessCount { get; set; }
    public float[] SemanticVector { get; set; }
}

public class SemanticMatch
{
    public CachedAIResponse Response { get; set; }
    public float Confidence { get; set; }
}

[System.Serializable]
public class CacheStatistics
{
    public long ExactHits;
    public long SemanticHits;
    public long Misses;
    public long TotalRequests;
    public float ExactHitRate;
    public float SemanticHitRate;
    public float OverallHitRate;
    public int ExactCacheSize;
    public int SemanticCacheSize;
}
```

---

## 🎯 **INTEGRATION WITH EXISTING SYSTEMS**

### Enhanced NIM Client with Optimization
```csharp
/// <summary>
/// Enhanced NIM client integrated with batching and semantic caching
/// </summary>
public class OptimizedNIMClient : MonoBehaviour
{
    [Header("AI Optimization Configuration")]
    [SerializeField] private bool enableBatching = true;
    [SerializeField] private bool enableSemanticCaching = true;
    [SerializeField] private bool enableAdaptiveTimeouts = true;

    private AIRequestBatchingEngine _batchingEngine;
    private SemanticAICacheManager _cacheManager;
    private NIMClient _baseClient;

    private void Awake()
    {
        _baseClient = GetComponent<NIMClient>();

        if (enableBatching)
            _batchingEngine = new AIRequestBatchingEngine();

        if (enableSemanticCaching)
            _cacheManager = new SemanticAICacheManager();
    }

    /// <summary>
    /// Send optimized AI request with batching and caching
    /// </summary>
    public async Task<string> SendOptimizedRequestAsync(string prompt, string context = null, RequestPriority priority = RequestPriority.Normal)
    {
        var request = new AIRequest
        {
            Prompt = prompt,
            Context = context,
            Priority = priority,
            RequestType = DetermineRequestType(prompt)
        };

        using (COALITIONProfilerMarkers.AIRequest.Auto())
        {
            // Try cache first
            if (enableSemanticCaching)
            {
                var cachedResponse = await _cacheManager.GetCachedResponseAsync(request);
                if (cachedResponse != null)
                {
                    Debug.Log($"[OptimizedNIMClient] Cache hit (confidence: {cachedResponse.CacheConfidence:F2})");
                    return cachedResponse.Content;
                }
            }

            // Process request (batched or immediate)
            AIResponse response;
            if (enableBatching)
            {
                response = await _batchingEngine.SubmitRequestAsync(request);
            }
            else
            {
                response = await ProcessSingleRequestAsync(request);
            }

            // Cache the response
            if (enableSemanticCaching && response != null)
            {
                await _cacheManager.CacheResponseAsync(request, response);
            }

            return response?.Content ?? "";
        }
    }

    private async Task<AIResponse> ProcessSingleRequestAsync(AIRequest request)
    {
        var timeout = enableAdaptiveTimeouts ? request.GetAdaptiveTimeout() : TimeSpan.FromSeconds(30);
        var response = await _baseClient.SendRequestAsync(request.Prompt, timeout);

        return new AIResponse
        {
            RequestId = request.RequestId,
            Content = response,
            ProcessingTime = DateTime.UtcNow - request.Timestamp,
            IsBatched = false
        };
    }

    private string DetermineRequestType(string prompt)
    {
        // Simple request type classification
        if (prompt.Contains("analyze") || prompt.Contains("analysis"))
            return "Analysis";
        if (prompt.Contains("generate") || prompt.Contains("create"))
            return "Generation";
        if (prompt.Length < 100)
            return "Simple";

        return "General";
    }

    /// <summary>
    /// Get comprehensive optimization statistics
    /// </summary>
    public AIOptimizationStatistics GetOptimizationStatistics()
    {
        var batchingStats = _batchingEngine?.GetStatistics();
        var cacheStats = _cacheManager?.GetStatistics();

        return new AIOptimizationStatistics
        {
            BatchingEnabled = enableBatching,
            CachingEnabled = enableSemanticCaching,
            BatchingStats = batchingStats,
            CacheStats = cacheStats,
            Timestamp = DateTime.UtcNow
        };
    }

    private void OnDestroy()
    {
        _batchingEngine?.Dispose();
        _cacheManager?.Dispose();
    }
}

[System.Serializable]
public class AIOptimizationStatistics
{
    public bool BatchingEnabled;
    public bool CachingEnabled;
    public BatchingStatistics BatchingStats;
    public CacheStatistics CacheStats;
    public DateTime Timestamp;
}
```

---

## 📊 **PERFORMANCE TESTING**

### AI Optimization Test Suite
```csharp
[TestFixture]
public class AIOptimizationTests
{
    [Test, Performance]
    public void AIBatching_Efficiency_Test()
    {
        var batchingEngine = new AIRequestBatchingEngine();
        var requests = new List<AIRequest>();

        // Create test requests
        for (int i = 0; i < 50; i++)
        {
            requests.Add(new AIRequest
            {
                Prompt = $"Test prompt {i}",
                Priority = i < 10 ? RequestPriority.High : RequestPriority.Normal
            });
        }

        using (Measure.Scope("AIBatching_Performance"))
        {
            var tasks = requests.Select(r => batchingEngine.SubmitRequestAsync(r)).ToArray();
            Task.WaitAll(tasks);
        }

        var stats = batchingEngine.GetStatistics();
        Assert.Greater(stats.BatchingEfficiency, 0.7f, "Batching efficiency should be > 70%");
        Assert.Greater(stats.AverageBatchSize, 2f, "Average batch size should be > 2");

        Debug.Log($"Batching efficiency: {stats.BatchingEfficiency:P2}, Avg batch size: {stats.AverageBatchSize:F1}");
    }

    [Test, Performance]
    public void SemanticCache_Accuracy_Test()
    {
        var cacheManager = new SemanticAICacheManager();
        var testRequests = CreateSimilarRequests();

        // Cache first request
        var firstRequest = testRequests[0];
        var firstResponse = new AIResponse { Content = "Test response content" };
        cacheManager.CacheResponseAsync(firstRequest, firstResponse).Wait();

        // Test semantic matching
        var cacheHits = 0;
        for (int i = 1; i < testRequests.Count; i++)
        {
            var cachedResponse = cacheManager.GetCachedResponseAsync(testRequests[i]).Result;
            if (cachedResponse != null)
                cacheHits++;
        }

        var hitRate = (float)cacheHits / (testRequests.Count - 1);
        Assert.Greater(hitRate, 0.6f, "Semantic cache hit rate should be > 60% for similar requests");

        Debug.Log($"Semantic cache hit rate: {hitRate:P2}");
    }

    private List<AIRequest> CreateSimilarRequests()
    {
        return new List<AIRequest>
        {
            new AIRequest { Prompt = "Analyze the political situation in country X", Context = "politics" },
            new AIRequest { Prompt = "Provide analysis of political conditions in nation X", Context = "politics" },
            new AIRequest { Prompt = "What is the political status in country X?", Context = "politics" },
            new AIRequest { Prompt = "Examine political developments in state X", Context = "politics" },
            new AIRequest { Prompt = "Review political circumstances in territory X", Context = "politics" }
        };
    }
}
```

---

## 📈 **EXPECTED PERFORMANCE IMPROVEMENTS**

### Quantitative Targets
- **Overall Response Time**: 40%+ reduction in average response latency
- **Cache Hit Rate**: 75%+ for exact matches, 60%+ for semantic matches
- **Batch Efficiency**: 70%+ of requests processed in batches
- **Resource Utilization**: 50%+ reduction in API calls through caching
- **Adaptive Timeouts**: 30%+ reduction in timeout-related errors

### Qualitative Benefits
- **Improved User Experience**: Faster AI responses and reduced waiting times
- **Cost Optimization**: Fewer API calls reduce usage costs
- **Better Scalability**: System handles higher request volumes efficiently
- **Smart Caching**: Context-aware responses improve relevance
- **Adaptive Performance**: System adjusts to varying request patterns

This comprehensive AI response optimization strategy provides intelligent batching, semantic caching, and adaptive performance management for the COALITION game's AI integration, ensuring optimal performance and user experience.