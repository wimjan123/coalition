# Memory Management Optimization Guide
## LRU Caching, Object Pooling, and Memory Pressure Management

### 🎯 **OBJECTIVE**
Implement comprehensive memory management optimizations including LRU caching for AI responses, object pooling for frequent allocations, weak references for large objects, and proactive memory pressure monitoring.

---

## 🔍 **CURRENT MEMORY ISSUES**

### Identified Problems
1. **Unbounded Growth**: No memory cleanup in long-running sessions
2. **Frequent Allocations**: Event objects created/destroyed repeatedly
3. **AI Response Duplication**: Same API responses cached without size limits
4. **GC Pressure**: Large object allocations triggering frequent collections
5. **Memory Leaks**: EventBus subscribers not properly cleaned up

### Memory Usage Analysis
```csharp
// Current problematic patterns:
public void PublishEvent()
{
    // Creates new object every time - GC pressure
    var gameEvent = new GameStateChangedEvent { State = currentState };
    EventBus.Publish(gameEvent);
}

public async Task<string> GetAIResponse(string prompt)
{
    // No caching - repeated API calls for same content
    return await nimClient.SendRequestAsync(prompt);
}
```

---

## 🏗️ **MEMORY OPTIMIZATION ARCHITECTURE**

### Core Components
1. **Object Pool Manager**: Generic pooling for frequent allocations
2. **LRU Cache System**: Size-limited caching with intelligent eviction
3. **Memory Pressure Monitor**: Proactive memory management
4. **Weak Reference Manager**: Automatic cleanup for large objects
5. **GC Optimization Controller**: Smart garbage collection triggering

---

## 📋 **IMPLEMENTATION COMPONENTS**

### Component 1: Generic Object Pool System
```csharp
/// <summary>
/// High-performance generic object pool with thread safety
/// </summary>
public class ObjectPool<T> : IDisposable where T : class, new()
{
    private readonly ConcurrentQueue<T> _objects = new();
    private readonly Func<T> _factory;
    private readonly Action<T> _resetAction;
    private readonly int _maxSize;
    private volatile int _currentCount;

    // Performance metrics
    private long _totalGets;
    private long _poolHits;
    private long _poolMisses;

    public ObjectPool(int maxSize = 100, Func<T> factory = null, Action<T> resetAction = null)
    {
        _maxSize = maxSize;
        _factory = factory ?? (() => new T());
        _resetAction = resetAction ?? (obj => { });
    }

    /// <summary>
    /// Get object from pool or create new one
    /// </summary>
    public T Get()
    {
        Interlocked.Increment(ref _totalGets);

        if (_objects.TryDequeue(out var item))
        {
            Interlocked.Decrement(ref _currentCount);
            Interlocked.Increment(ref _poolHits);
            return item;
        }

        Interlocked.Increment(ref _poolMisses);
        return _factory();
    }

    /// <summary>
    /// Return object to pool for reuse
    /// </summary>
    public void Return(T item)
    {
        if (item == null || _currentCount >= _maxSize)
            return;

        // Reset object state
        _resetAction(item);

        _objects.Enqueue(item);
        Interlocked.Increment(ref _currentCount);
    }

    /// <summary>
    /// Get pool efficiency statistics
    /// </summary>
    public PoolStatistics GetStatistics()
    {
        return new PoolStatistics
        {
            TotalGets = _totalGets,
            PoolHits = _poolHits,
            PoolMisses = _poolMisses,
            CurrentCount = _currentCount,
            HitRate = _totalGets > 0 ? (double)_poolHits / _totalGets : 0.0
        };
    }

    public void Dispose()
    {
        while (_objects.TryDequeue(out var item))
        {
            if (item is IDisposable disposable)
                disposable.Dispose();
        }
    }
}

public class PoolStatistics
{
    public long TotalGets { get; set; }
    public long PoolHits { get; set; }
    public long PoolMisses { get; set; }
    public int CurrentCount { get; set; }
    public double HitRate { get; set; }
}
```

### Component 2: Object Pool Manager
```csharp
/// <summary>
/// Centralized management of all object pools
/// </summary>
public static class ObjectPoolManager
{
    private static readonly ConcurrentDictionary<Type, object> _pools = new();
    private static readonly Timer _cleanupTimer;

    static ObjectPoolManager()
    {
        // Cleanup unused pools every 5 minutes
        _cleanupTimer = new Timer(CleanupUnusedPools, null,
            TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    /// <summary>
    /// Get or create object pool for type T
    /// </summary>
    public static ObjectPool<T> GetPool<T>() where T : class, new()
    {
        return (ObjectPool<T>)_pools.GetOrAdd(typeof(T), _ => CreatePool<T>());
    }

    /// <summary>
    /// Get object from appropriate pool
    /// </summary>
    public static T Get<T>() where T : class, new()
    {
        return GetPool<T>().Get();
    }

    /// <summary>
    /// Return object to appropriate pool
    /// </summary>
    public static void Return<T>(T item) where T : class, new()
    {
        if (item != null)
            GetPool<T>().Return(item);
    }

    /// <summary>
    /// Get comprehensive pool statistics
    /// </summary>
    public static Dictionary<Type, PoolStatistics> GetAllStatistics()
    {
        var stats = new Dictionary<Type, PoolStatistics>();
        foreach (var kvp in _pools)
        {
            var poolType = kvp.Value.GetType();
            var getStatsMethod = poolType.GetMethod("GetStatistics");
            if (getStatsMethod != null)
            {
                var poolStats = (PoolStatistics)getStatsMethod.Invoke(kvp.Value, null);
                stats[kvp.Key] = poolStats;
            }
        }
        return stats;
    }

    private static ObjectPool<T> CreatePool<T>() where T : class, new()
    {
        // Configure pool based on type
        var poolConfig = GetPoolConfiguration<T>();
        return new ObjectPool<T>(
            poolConfig.MaxSize,
            poolConfig.Factory,
            poolConfig.ResetAction);
    }

    private static PoolConfiguration<T> GetPoolConfiguration<T>() where T : class, new()
    {
        // Type-specific pool configurations
        var type = typeof(T);

        if (type.IsSubclassOf(typeof(MonoBehaviour)) || type.GetInterface(nameof(IGameEvent)) != null)
        {
            return new PoolConfiguration<T>
            {
                MaxSize = 50,
                Factory = () => new T(),
                ResetAction = ResetGameObject<T>
            };
        }

        // Default configuration
        return new PoolConfiguration<T>
        {
            MaxSize = 100,
            Factory = () => new T(),
            ResetAction = obj => { }
        };
    }

    private static void ResetGameObject<T>(T obj) where T : class
    {
        // Reset common game object properties
        if (obj is IResettable resettable)
            resettable.Reset();
    }

    private static void CleanupUnusedPools(object state)
    {
        // Remove pools with low hit rates that haven't been used recently
        var toRemove = new List<Type>();

        foreach (var kvp in _pools)
        {
            var poolType = kvp.Value.GetType();
            var getStatsMethod = poolType.GetMethod("GetStatistics");
            if (getStatsMethod != null)
            {
                var stats = (PoolStatistics)getStatsMethod.Invoke(kvp.Value, null);
                if (stats.HitRate < 0.1 && stats.TotalGets > 100)
                {
                    toRemove.Add(kvp.Key);
                }
            }
        }

        foreach (var type in toRemove)
        {
            if (_pools.TryRemove(type, out var pool) && pool is IDisposable disposable)
            {
                disposable.Dispose();
                Debug.Log($"[ObjectPoolManager] Removed unused pool for {type.Name}");
            }
        }
    }
}

public class PoolConfiguration<T> where T : class
{
    public int MaxSize { get; set; } = 100;
    public Func<T> Factory { get; set; }
    public Action<T> ResetAction { get; set; }
}

public interface IResettable
{
    void Reset();
}
```

### Component 3: LRU Cache Implementation
```csharp
/// <summary>
/// Thread-safe LRU cache with size limits and memory pressure awareness
/// </summary>
public class LRUCache<TKey, TValue> : IDisposable
{
    private readonly int _maxSize;
    private readonly ConcurrentDictionary<TKey, LinkedListNode<CacheItem>> _cache = new();
    private readonly LinkedList<CacheItem> _lruList = new();
    private readonly ReaderWriterLockSlim _lock = new();

    // Memory pressure monitoring
    private readonly MemoryPressureMonitor _memoryMonitor;
    private volatile bool _memoryPressureMode = false;

    // Performance metrics
    private long _hits;
    private long _misses;
    private long _evictions;

    public LRUCache(int maxSize = 1000, MemoryPressureMonitor memoryMonitor = null)
    {
        _maxSize = maxSize;
        _memoryMonitor = memoryMonitor ?? MemoryPressureMonitor.Instance;
        _memoryMonitor.MemoryPressureChanged += OnMemoryPressureChanged;
    }

    private class CacheItem
    {
        public TKey Key { get; set; }
        public TValue Value { get; set; }
        public DateTime AccessTime { get; set; }
        public long AccessCount { get; set; }
    }

    /// <summary>
    /// Get value from cache
    /// </summary>
    public bool TryGet(TKey key, out TValue value)
    {
        value = default(TValue);

        if (!_cache.TryGetValue(key, out var node))
        {
            Interlocked.Increment(ref _misses);
            return false;
        }

        _lock.EnterWriteLock();
        try
        {
            // Move to front (most recently used)
            _lruList.Remove(node);
            _lruList.AddFirst(node);

            // Update access statistics
            node.Value.AccessTime = DateTime.UtcNow;
            node.Value.AccessCount++;

            value = node.Value.Value;
            Interlocked.Increment(ref _hits);
            return true;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Add or update value in cache
    /// </summary>
    public void Set(TKey key, TValue value)
    {
        _lock.EnterWriteLock();
        try
        {
            if (_cache.TryGetValue(key, out var existingNode))
            {
                // Update existing item
                existingNode.Value.Value = value;
                existingNode.Value.AccessTime = DateTime.UtcNow;
                existingNode.Value.AccessCount++;

                // Move to front
                _lruList.Remove(existingNode);
                _lruList.AddFirst(existingNode);
                return;
            }

            // Create new item
            var newItem = new CacheItem
            {
                Key = key,
                Value = value,
                AccessTime = DateTime.UtcNow,
                AccessCount = 1
            };

            var newNode = _lruList.AddFirst(newItem);
            _cache[key] = newNode;

            // Check for eviction
            var currentMaxSize = _memoryPressureMode ? _maxSize / 2 : _maxSize;
            while (_lruList.Count > currentMaxSize)
            {
                EvictLeastRecentlyUsed();
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Remove item from cache
    /// </summary>
    public bool Remove(TKey key)
    {
        _lock.EnterWriteLock();
        try
        {
            if (_cache.TryRemove(key, out var node))
            {
                _lruList.Remove(node);
                DisposeValue(node.Value.Value);
                return true;
            }
            return false;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Clear all items from cache
    /// </summary>
    public void Clear()
    {
        _lock.EnterWriteLock();
        try
        {
            foreach (var node in _lruList)
            {
                DisposeValue(node.Value);
            }

            _cache.Clear();
            _lruList.Clear();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Get cache statistics
    /// </summary>
    public CacheStatistics GetStatistics()
    {
        _lock.EnterReadLock();
        try
        {
            return new CacheStatistics
            {
                Count = _cache.Count,
                MaxSize = _maxSize,
                Hits = _hits,
                Misses = _misses,
                Evictions = _evictions,
                HitRate = (_hits + _misses) > 0 ? (double)_hits / (_hits + _misses) : 0.0,
                MemoryPressureMode = _memoryPressureMode
            };
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    private void EvictLeastRecentlyUsed()
    {
        if (_lruList.Count == 0)
            return;

        var lastNode = _lruList.Last;
        _lruList.RemoveLast();
        _cache.TryRemove(lastNode.Value.Key, out _);

        DisposeValue(lastNode.Value.Value);
        Interlocked.Increment(ref _evictions);
    }

    private void DisposeValue(TValue value)
    {
        if (value is IDisposable disposable)
            disposable.Dispose();
    }

    private void OnMemoryPressureChanged(object sender, MemoryPressureEventArgs e)
    {
        _memoryPressureMode = e.IsHighPressure;

        if (e.IsHighPressure)
        {
            // Aggressively evict items during memory pressure
            _lock.EnterWriteLock();
            try
            {
                var targetSize = _maxSize / 3; // Reduce to 1/3 size
                while (_lruList.Count > targetSize)
                {
                    EvictLeastRecentlyUsed();
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            Debug.LogWarning($"[LRUCache] Memory pressure detected. Reduced cache size to {_lruList.Count}");
        }
    }

    public void Dispose()
    {
        _memoryMonitor.MemoryPressureChanged -= OnMemoryPressureChanged;
        Clear();
        _lock?.Dispose();
    }
}

public class CacheStatistics
{
    public int Count { get; set; }
    public int MaxSize { get; set; }
    public long Hits { get; set; }
    public long Misses { get; set; }
    public long Evictions { get; set; }
    public double HitRate { get; set; }
    public bool MemoryPressureMode { get; set; }
}
```

### Component 4: Memory Pressure Monitor
```csharp
/// <summary>
/// Monitors system memory pressure and triggers optimizations
/// </summary>
public class MemoryPressureMonitor : IDisposable
{
    private static readonly Lazy<MemoryPressureMonitor> _instance =
        new(() => new MemoryPressureMonitor());
    public static MemoryPressureMonitor Instance => _instance.Value;

    private readonly Timer _monitorTimer;
    private volatile bool _isHighPressure = false;
    private long _lastGCMemory = 0;
    private DateTime _lastGCTime = DateTime.UtcNow;

    // Configurable thresholds
    public long HighPressureThreshold { get; set; } = 500 * 1024 * 1024; // 500MB
    public long CriticalPressureThreshold { get; set; } = 1024 * 1024 * 1024; // 1GB
    public TimeSpan MonitoringInterval { get; set; } = TimeSpan.FromSeconds(10);

    public event EventHandler<MemoryPressureEventArgs> MemoryPressureChanged;

    private MemoryPressureMonitor()
    {
        _monitorTimer = new Timer(CheckMemoryPressure, null,
            MonitoringInterval, MonitoringInterval);
    }

    private void CheckMemoryPressure(object state)
    {
        var currentMemory = GC.GetTotalMemory(false);
        var memoryDelta = currentMemory - _lastGCMemory;
        var timeDelta = DateTime.UtcNow - _lastGCTime;

        // Calculate memory growth rate
        var growthRate = timeDelta.TotalMinutes > 0 ?
            memoryDelta / timeDelta.TotalMinutes : 0;

        var wasHighPressure = _isHighPressure;
        _isHighPressure = currentMemory > HighPressureThreshold ||
                         growthRate > (HighPressureThreshold / 10); // 10% of threshold per minute

        // Check for critical pressure
        var isCriticalPressure = currentMemory > CriticalPressureThreshold;

        if (_isHighPressure != wasHighPressure || isCriticalPressure)
        {
            var args = new MemoryPressureEventArgs
            {
                IsHighPressure = _isHighPressure,
                IsCriticalPressure = isCriticalPressure,
                CurrentMemory = currentMemory,
                GrowthRate = growthRate,
                Timestamp = DateTime.UtcNow
            };

            MemoryPressureChanged?.Invoke(this, args);

            if (isCriticalPressure)
            {
                // Force garbage collection during critical pressure
                Debug.LogWarning($"[MemoryPressureMonitor] Critical memory pressure detected: {currentMemory / (1024 * 1024)}MB");
                GC.Collect(2, GCCollectionMode.Forced, true);
                GC.WaitForPendingFinalizers();
            }
            else if (_isHighPressure)
            {
                Debug.LogWarning($"[MemoryPressureMonitor] High memory pressure detected: {currentMemory / (1024 * 1024)}MB");
            }
        }

        // Update tracking variables
        if (timeDelta.TotalMinutes >= 1) // Update baseline every minute
        {
            _lastGCMemory = currentMemory;
            _lastGCTime = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Get current memory statistics
    /// </summary>
    public MemoryStatistics GetMemoryStatistics()
    {
        return new MemoryStatistics
        {
            TotalMemory = GC.GetTotalMemory(false),
            IsHighPressure = _isHighPressure,
            HighPressureThreshold = HighPressureThreshold,
            CriticalPressureThreshold = CriticalPressureThreshold,
            Gen0Collections = GC.CollectionCount(0),
            Gen1Collections = GC.CollectionCount(1),
            Gen2Collections = GC.CollectionCount(2)
        };
    }

    /// <summary>
    /// Force memory pressure check
    /// </summary>
    public void ForceCheck()
    {
        CheckMemoryPressure(null);
    }

    public void Dispose()
    {
        _monitorTimer?.Dispose();
    }
}

public class MemoryPressureEventArgs : EventArgs
{
    public bool IsHighPressure { get; set; }
    public bool IsCriticalPressure { get; set; }
    public long CurrentMemory { get; set; }
    public double GrowthRate { get; set; }
    public DateTime Timestamp { get; set; }
}

public class MemoryStatistics
{
    public long TotalMemory { get; set; }
    public bool IsHighPressure { get; set; }
    public long HighPressureThreshold { get; set; }
    public long CriticalPressureThreshold { get; set; }
    public int Gen0Collections { get; set; }
    public int Gen1Collections { get; set; }
    public int Gen2Collections { get; set; }
}
```

---

## 🎮 **GAME-SPECIFIC IMPLEMENTATIONS**

### AI Response Cache Manager
```csharp
/// <summary>
/// Specialized cache for AI responses with semantic similarity detection
/// </summary>
public class AIResponseCacheManager : IDisposable
{
    private readonly LRUCache<string, AIResponse> _responseCache;
    private readonly LRUCache<string, string> _semanticCache;
    private readonly SemaphoreSlim _processingLock = new(1, 1);

    public AIResponseCacheManager(int maxResponses = 500, int maxSemanticMappings = 1000)
    {
        _responseCache = new LRUCache<string, AIResponse>(maxResponses);
        _semanticCache = new LRUCache<string, string>(maxSemanticMappings);
    }

    /// <summary>
    /// Get cached AI response or null if not found
    /// </summary>
    public async Task<AIResponse> GetCachedResponseAsync(string prompt)
    {
        // Try exact match first
        if (_responseCache.TryGet(prompt, out var response))
        {
            Debug.Log("[AIResponseCache] Exact cache hit");
            return response;
        }

        // Try semantic similarity match
        var semanticKey = await ComputeSemanticKeyAsync(prompt);
        if (_semanticCache.TryGet(semanticKey, out var originalPrompt) &&
            _responseCache.TryGet(originalPrompt, out response))
        {
            Debug.Log("[AIResponseCache] Semantic cache hit");
            return response;
        }

        return null;
    }

    /// <summary>
    /// Cache AI response with semantic indexing
    /// </summary>
    public async Task CacheResponseAsync(string prompt, AIResponse response)
    {
        await _processingLock.WaitAsync();
        try
        {
            // Cache the response
            _responseCache.Set(prompt, response);

            // Create semantic mapping
            var semanticKey = await ComputeSemanticKeyAsync(prompt);
            _semanticCache.Set(semanticKey, prompt);

            Debug.Log($"[AIResponseCache] Cached response for prompt length: {prompt.Length}");
        }
        finally
        {
            _processingLock.Release();
        }
    }

    private async Task<string> ComputeSemanticKeyAsync(string prompt)
    {
        // Simplified semantic key generation
        // In production, use more sophisticated similarity detection
        var normalized = prompt.ToLowerInvariant()
            .Replace(" ", "")
            .Replace("\n", "")
            .Replace("\t", "");

        // Take first 100 characters as semantic key
        return normalized.Length > 100 ? normalized.Substring(0, 100) : normalized;
    }

    /// <summary>
    /// Get cache statistics
    /// </summary>
    public (CacheStatistics Responses, CacheStatistics Semantic) GetStatistics()
    {
        return (_responseCache.GetStatistics(), _semanticCache.GetStatistics());
    }

    public void Dispose()
    {
        _responseCache?.Dispose();
        _semanticCache?.Dispose();
        _processingLock?.Dispose();
    }
}

public class AIResponse
{
    public string Content { get; set; }
    public DateTime Timestamp { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public string ModelUsed { get; set; }
    public int TokenCount { get; set; }
}
```

### Game Event Object Pools
```csharp
/// <summary>
/// Pre-configured object pools for common game events
/// </summary>
public static class GameEventPools
{
    // Event object pools
    public static readonly ObjectPool<GameStateChangedEvent> GameStateChanged;
    public static readonly ObjectPool<CoalitionFormedEvent> CoalitionFormed;
    public static readonly ObjectPool<PoliticalActionEvent> PoliticalAction;
    public static readonly ObjectPool<AIResponseReceivedEvent> AIResponseReceived;

    static GameEventPools()
    {
        GameStateChanged = new ObjectPool<GameStateChangedEvent>(
            maxSize: 50,
            factory: () => new GameStateChangedEvent(),
            resetAction: evt => evt.Reset());

        CoalitionFormed = new ObjectPool<CoalitionFormedEvent>(
            maxSize: 30,
            factory: () => new CoalitionFormedEvent(),
            resetAction: evt => evt.Reset());

        PoliticalAction = new ObjectPool<PoliticalActionEvent>(
            maxSize: 100,
            factory: () => new PoliticalActionEvent(),
            resetAction: evt => evt.Reset());

        AIResponseReceived = new ObjectPool<AIResponseReceivedEvent>(
            maxSize: 20,
            factory: () => new AIResponseReceivedEvent(),
            resetAction: evt => evt.Reset());
    }

    /// <summary>
    /// Get comprehensive statistics for all game event pools
    /// </summary>
    public static Dictionary<string, PoolStatistics> GetAllStatistics()
    {
        return new Dictionary<string, PoolStatistics>
        {
            ["GameStateChanged"] = GameStateChanged.GetStatistics(),
            ["CoalitionFormed"] = CoalitionFormed.GetStatistics(),
            ["PoliticalAction"] = PoliticalAction.GetStatistics(),
            ["AIResponseReceived"] = AIResponseReceived.GetStatistics()
        };
    }
}

// Enhanced event classes with pooling support
public class GameStateChangedEvent : IGameEvent, IResettable
{
    public GameState PreviousState { get; set; }
    public GameState NewState { get; set; }
    public DateTime Timestamp { get; set; }
    public string Reason { get; set; }

    public void Reset()
    {
        PreviousState = null;
        NewState = null;
        Timestamp = default;
        Reason = null;
    }
}

public class CoalitionFormedEvent : IGameEvent, IResettable
{
    public List<PoliticalParty> Parties { get; set; } = new List<PoliticalParty>();
    public float CompatibilityScore { get; set; }
    public DateTime Timestamp { get; set; }
    public string FormationReason { get; set; }

    public void Reset()
    {
        Parties.Clear();
        CompatibilityScore = 0f;
        Timestamp = default;
        FormationReason = null;
    }
}
```

---

## 📊 **MEMORY OPTIMIZATION INTEGRATION**

### Memory-Aware Game Manager
```csharp
/// <summary>
/// Enhanced GameManager with memory optimization integration
/// </summary>
public class MemoryOptimizedGameManager : MonoBehaviour
{
    [SerializeField] private float memoryCheckInterval = 30f;

    private MemoryPressureMonitor _memoryMonitor;
    private AIResponseCacheManager _aiCacheManager;
    private Coroutine _memoryManagementCoroutine;

    private void Awake()
    {
        // Initialize memory management systems
        _memoryMonitor = MemoryPressureMonitor.Instance;
        _aiCacheManager = new AIResponseCacheManager();

        // Subscribe to memory pressure events
        _memoryMonitor.MemoryPressureChanged += OnMemoryPressureChanged;
    }

    private void Start()
    {
        _memoryManagementCoroutine = StartCoroutine(MemoryManagementLoop());
    }

    private IEnumerator MemoryManagementLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(memoryCheckInterval);

            // Log memory statistics
            var memStats = _memoryMonitor.GetMemoryStatistics();
            var poolStats = ObjectPoolManager.GetAllStatistics();
            var (responseStats, semanticStats) = _aiCacheManager.GetStatistics();

            Debug.Log($"[MemoryManager] Memory: {memStats.TotalMemory / (1024 * 1024)}MB, " +
                     $"High Pressure: {memStats.IsHighPressure}, " +
                     $"Pool Hit Rate: {poolStats.Values.Average(s => s.HitRate):P2}, " +
                     $"Cache Hit Rate: {responseStats.HitRate:P2}");

            // Trigger optimizations if needed
            if (memStats.IsHighPressure)
            {
                yield return StartCoroutine(PerformMemoryOptimizations());
            }
        }
    }

    private IEnumerator PerformMemoryOptimizations()
    {
        Debug.Log("[MemoryManager] Performing memory optimizations...");

        // Force garbage collection
        GC.Collect(2, GCCollectionMode.Optimized, true);
        yield return null;

        // Clear unnecessary caches
        EventBusV2.CleanupDeadReferences();
        yield return null;

        // Reduce cache sizes temporarily
        // (This would be implemented based on specific cache manager APIs)
        yield return null;

        Debug.Log("[MemoryManager] Memory optimizations completed");
    }

    private void OnMemoryPressureChanged(object sender, MemoryPressureEventArgs e)
    {
        if (e.IsCriticalPressure)
        {
            // Emergency memory cleanup
            StartCoroutine(EmergencyMemoryCleanup());
        }
    }

    private IEnumerator EmergencyMemoryCleanup()
    {
        Debug.LogWarning("[MemoryManager] Emergency memory cleanup initiated!");

        // Aggressively clear all non-essential caches
        EventBusV2.Clear();
        yield return null;

        // Force immediate garbage collection
        GC.Collect(2, GCCollectionMode.Forced, true);
        GC.WaitForPendingFinalizers();
        yield return null;

        // Restart memory monitoring with reduced thresholds
        _memoryMonitor.HighPressureThreshold = _memoryMonitor.HighPressureThreshold / 2;

        Debug.LogWarning("[MemoryManager] Emergency cleanup completed");
    }

    private void OnDestroy()
    {
        if (_memoryManagementCoroutine != null)
            StopCoroutine(_memoryManagementCoroutine);

        _memoryMonitor.MemoryPressureChanged -= OnMemoryPressureChanged;
        _aiCacheManager?.Dispose();
    }
}
```

---

## 🧪 **PERFORMANCE TESTING**

### Memory Optimization Test Suite
```csharp
[TestFixture]
public class MemoryOptimizationTests
{
    [Test, Performance]
    public void ObjectPool_Memory_Efficiency()
    {
        var pool = new ObjectPool<TestObject>(maxSize: 100);
        var allocated = new List<TestObject>();

        var beforeMemory = GC.GetTotalMemory(true);

        using (Measure.Scope("ObjectPool_Allocation"))
        {
            // Allocate and return objects
            for (int i = 0; i < 1000; i++)
            {
                var obj = pool.Get();
                allocated.Add(obj);

                if (i % 10 == 0) // Return every 10th object
                {
                    pool.Return(obj);
                    allocated.Remove(obj);
                }
            }
        }

        var afterMemory = GC.GetTotalMemory(true);
        var memoryUsed = afterMemory - beforeMemory;

        var stats = pool.GetStatistics();

        Assert.Greater(stats.HitRate, 0.8, "Pool hit rate should be > 80%");
        Assert.Less(memoryUsed, 50000, "Memory usage should be < 50KB");

        Debug.Log($"Pool hit rate: {stats.HitRate:P2}, Memory used: {memoryUsed} bytes");
    }

    [Test, Performance]
    public void LRUCache_Performance_Test()
    {
        var cache = new LRUCache<string, TestData>(maxSize: 100);
        var testData = new List<TestData>();

        // Populate test data
        for (int i = 0; i < 1000; i++)
        {
            testData.Add(new TestData { Value = $"Test{i}" });
        }

        using (Measure.Scope("LRUCache_Operations"))
        {
            // Cache operations
            for (int i = 0; i < testData.Count; i++)
            {
                cache.Set($"key{i}", testData[i]);

                // Random access pattern
                if (i > 50 && Random.Range(0, 2) == 0)
                {
                    var randomKey = $"key{Random.Range(0, i)}";
                    cache.TryGet(randomKey, out _);
                }
            }
        }

        var stats = cache.GetStatistics();
        Assert.Greater(stats.HitRate, 0.3, "Cache hit rate should be reasonable");
        Assert.LessOrEqual(stats.Count, 100, "Cache should not exceed max size");

        Debug.Log($"Cache hit rate: {stats.HitRate:P2}, Final size: {stats.Count}");
    }

    private class TestObject
    {
        public string Data { get; set; } = "Test data";
        public int Value { get; set; } = Random.Range(0, 1000);
    }

    private class TestData
    {
        public string Value { get; set; }
        public byte[] LargeData { get; set; } = new byte[1024]; // 1KB each
    }
}
```

---

## 📈 **EXPECTED IMPROVEMENTS**

### Memory Usage Targets
- **Memory Growth Rate**: < 50MB/hour (down from unbounded)
- **GC Pressure**: 80%+ reduction in allocations
- **Object Pool Hit Rate**: > 90% for frequent objects
- **Cache Hit Rate**: > 75% for AI responses
- **Memory Leak Prevention**: Zero leaks in 8+ hour sessions

### Performance Benefits
- **Reduced GC Pauses**: Fewer and shorter garbage collection cycles
- **Improved Frame Rates**: More consistent performance during memory pressure
- **Better Scalability**: System handles longer sessions gracefully
- **Automatic Recovery**: Memory pressure detection and automatic optimization

This comprehensive memory management system provides robust optimization for the COALITION game, ensuring stable performance during extended play sessions while maximizing cache efficiency and minimizing memory-related performance issues.