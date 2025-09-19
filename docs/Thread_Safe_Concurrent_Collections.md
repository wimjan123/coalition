# Thread-Safe Concurrent Collections Implementation Guide
## High-Performance Concurrent Programming Patterns for COALITION

### 🎯 **OBJECTIVE**
Implement comprehensive thread-safe concurrent collections and synchronization patterns to replace unsafe Dictionary usage throughout the COALITION game, ensuring data integrity and performance under multi-threaded scenarios.

---

## 🔍 **CURRENT THREAD SAFETY ISSUES**

### Identified Problems
1. **EventBus Dictionary**: Non-thread-safe `Dictionary<Type, List<object>>`
2. **Coalition Cache**: Concurrent access to calculation results
3. **AI Response Storage**: Potential race conditions in caching
4. **Memory Pool Access**: Object pools accessed from multiple threads
5. **State Management**: Game state updates from various systems

### Risk Assessment
```csharp
// Current problematic patterns:
private static Dictionary<Type, List<object>> eventListeners = new(); // NOT THREAD-SAFE

// Issues:
// 1. Concurrent reads/writes can corrupt dictionary
// 2. List modifications not atomic
// 3. No synchronization between add/remove operations
// 4. Iterator invalidation during enumeration
// 5. Memory visibility issues across threads
```

---

## 🏗️ **THREAD-SAFE ARCHITECTURE**

### Core Principles
1. **Lock-Free Data Structures**: Use concurrent collections where possible
2. **Immutable Data Patterns**: Reduce mutation points
3. **Reader-Writer Optimization**: Optimize for read-heavy scenarios
4. **Memory Barriers**: Ensure proper memory ordering
5. **Deadlock Prevention**: Avoid nested locking scenarios

### Thread Safety Hierarchy
```
Level 1: Lock-Free (ConcurrentDictionary, ConcurrentQueue)
Level 2: Reader-Writer Locks (ReaderWriterLockSlim)
Level 3: Mutex/Semaphore (SemaphoreSlim, Mutex)
Level 4: Basic Locks (lock statement, Monitor)
```

---

## 📋 **IMPLEMENTATION COMPONENTS**

### Component 1: Thread-Safe EventBus Collections
```csharp
/// <summary>
/// Thread-safe event listener management using concurrent collections
/// </summary>
public static class ThreadSafeEventStorage
{
    // Primary storage: Lock-free concurrent dictionary
    private static readonly ConcurrentDictionary<Type, ConcurrentBag<WeakEventHandler>> _eventHandlers = new();

    // Cleanup tracking: Periodically remove dead references
    private static readonly ConcurrentDictionary<Type, long> _lastCleanupTime = new();
    private static readonly long CleanupIntervalTicks = TimeSpan.FromMinutes(5).Ticks;

    /// <summary>
    /// Add event handler in thread-safe manner
    /// </summary>
    public static void AddHandler<T>(Action<T> handler, object target = null) where T : IGameEvent
    {
        var eventType = typeof(T);
        var weakHandler = new WeakEventHandler<T>(handler, target);

        _eventHandlers.AddOrUpdate(eventType,
            new ConcurrentBag<WeakEventHandler> { weakHandler },
            (key, existing) =>
            {
                existing.Add(weakHandler);
                return existing;
            });

        // Schedule cleanup if needed
        ScheduleCleanupIfNeeded(eventType);
    }

    /// <summary>
    /// Remove event handler in thread-safe manner
    /// </summary>
    public static void RemoveHandler<T>(Action<T> handler) where T : IGameEvent
    {
        var eventType = typeof(T);

        if (!_eventHandlers.TryGetValue(eventType, out var handlers))
            return;

        // Mark for removal (cleanup will handle actual removal)
        foreach (var weakHandler in handlers)
        {
            if (weakHandler is WeakEventHandler<T> typedHandler &&
                typedHandler.HandlerEquals(handler))
            {
                typedHandler.MarkForRemoval();
            }
        }

        // Force cleanup for this type
        ScheduleImmediateCleanup(eventType);
    }

    /// <summary>
    /// Get active handlers for event type (thread-safe enumeration)
    /// </summary>
    public static IEnumerable<Action<T>> GetHandlers<T>() where T : IGameEvent
    {
        var eventType = typeof(T);

        if (!_eventHandlers.TryGetValue(eventType, out var handlers))
            yield break;

        // Create snapshot to avoid enumeration issues
        var handlerSnapshot = handlers.ToArray();

        foreach (var weakHandler in handlerSnapshot)
        {
            if (weakHandler is WeakEventHandler<T> typedHandler &&
                typedHandler.TryGetHandler(out var handler))
            {
                yield return handler;
            }
        }
    }

    /// <summary>
    /// Get handler count for monitoring
    /// </summary>
    public static int GetHandlerCount<T>() where T : IGameEvent
    {
        var eventType = typeof(T);
        return _eventHandlers.TryGetValue(eventType, out var handlers) ? handlers.Count : 0;
    }

    /// <summary>
    /// Clear all handlers (for cleanup/testing)
    /// </summary>
    public static void Clear()
    {
        _eventHandlers.Clear();
        _lastCleanupTime.Clear();
    }

    private static void ScheduleCleanupIfNeeded(Type eventType)
    {
        var currentTime = DateTime.UtcNow.Ticks;
        var lastCleanup = _lastCleanupTime.GetOrAdd(eventType, currentTime);

        if (currentTime - lastCleanup > CleanupIntervalTicks)
        {
            Task.Run(() => PerformCleanup(eventType));
        }
    }

    private static void ScheduleImmediateCleanup(Type eventType)
    {
        Task.Run(() => PerformCleanup(eventType));
    }

    private static void PerformCleanup(Type eventType)
    {
        if (!_eventHandlers.TryGetValue(eventType, out var handlers))
            return;

        var aliveHandlers = new ConcurrentBag<WeakEventHandler>();
        var removedCount = 0;

        foreach (var handler in handlers)
        {
            if (handler.IsAlive && !handler.IsMarkedForRemoval)
            {
                aliveHandlers.Add(handler);
            }
            else
            {
                removedCount++;
            }
        }

        // Replace with cleaned collection
        if (removedCount > 0)
        {
            _eventHandlers.TryUpdate(eventType, aliveHandlers, handlers);
            Debug.Log($"[ThreadSafeEventStorage] Cleaned {removedCount} dead handlers for {eventType.Name}");
        }

        // Update cleanup time
        _lastCleanupTime.TryUpdate(eventType, DateTime.UtcNow.Ticks, _lastCleanupTime[eventType]);
    }
}

/// <summary>
/// Weak reference wrapper for event handlers to prevent memory leaks
/// </summary>
public abstract class WeakEventHandler
{
    protected WeakReference _targetRef;
    protected WeakReference _handlerRef;
    private volatile bool _markedForRemoval;

    public bool IsAlive => _targetRef?.IsAlive != false && _handlerRef?.IsAlive != false;
    public bool IsMarkedForRemoval => _markedForRemoval;

    public void MarkForRemoval() => _markedForRemoval = true;
}

public class WeakEventHandler<T> : WeakEventHandler where T : IGameEvent
{
    private readonly string _handlerMethodName;

    public WeakEventHandler(Action<T> handler, object target)
    {
        _handlerRef = new WeakReference(handler);
        _targetRef = target != null ? new WeakReference(target) : null;
        _handlerMethodName = handler.Method.Name;
    }

    public bool TryGetHandler(out Action<T> handler)
    {
        handler = null;

        if (_handlerRef.Target is Action<T> strongHandler && IsAlive)
        {
            handler = strongHandler;
            return true;
        }

        return false;
    }

    public bool HandlerEquals(Action<T> handler)
    {
        return _handlerRef.Target is Action<T> storedHandler &&
               ReferenceEquals(storedHandler.Target, handler.Target) &&
               storedHandler.Method.Name == handler.Method.Name;
    }
}
```

### Component 2: Concurrent Coalition Cache
```csharp
/// <summary>
/// Thread-safe coalition calculation cache with concurrent access optimization
/// </summary>
public class ConcurrentCoalitionCache : IDisposable
{
    private readonly ConcurrentDictionary<string, CoalitionCacheEntry> _cache = new();
    private readonly ReaderWriterLockSlim _cleanupLock = new();
    private readonly Timer _cleanupTimer;

    [Header("Cache Configuration")]
    [SerializeField] private int maxCacheSize = 1000;
    [SerializeField] private TimeSpan entryLifetime = TimeSpan.FromMinutes(30);
    [SerializeField] private TimeSpan cleanupInterval = TimeSpan.FromMinutes(5);

    // Performance metrics
    private long _hits = 0;
    private long _misses = 0;
    private long _invalidations = 0;

    public ConcurrentCoalitionCache()
    {
        _cleanupTimer = new Timer(PerformCleanup, null, cleanupInterval, cleanupInterval);
    }

    /// <summary>
    /// Get cached coalition result (thread-safe)
    /// </summary>
    public bool TryGetCoalition(CoalitionKey key, out CoalitionResult result)
    {
        result = null;
        var cacheKey = key.ToString();

        using (COALITIONProfilerMarkers.CoalitionCacheHit.Auto())
        {
            if (_cache.TryGetValue(cacheKey, out var entry) && !entry.IsExpired)
            {
                // Update access time atomically
                var newEntry = entry with { LastAccessTime = DateTime.UtcNow, AccessCount = entry.AccessCount + 1 };
                _cache.TryUpdate(cacheKey, newEntry, entry);

                result = entry.Result;
                Interlocked.Increment(ref _hits);
                return true;
            }

            Interlocked.Increment(ref _misses);
            return false;
        }
    }

    /// <summary>
    /// Cache coalition result (thread-safe)
    /// </summary>
    public void CacheCoalition(CoalitionKey key, CoalitionResult result)
    {
        var cacheKey = key.ToString();
        var entry = new CoalitionCacheEntry
        {
            Key = key,
            Result = result,
            CreationTime = DateTime.UtcNow,
            LastAccessTime = DateTime.UtcNow,
            AccessCount = 1
        };

        _cache.AddOrUpdate(cacheKey, entry, (k, existing) => entry);

        // Trigger cleanup if cache is getting too large
        if (_cache.Count > maxCacheSize * 1.2) // 20% buffer before aggressive cleanup
        {
            Task.Run(() => PerformCleanup(null));
        }
    }

    /// <summary>
    /// Invalidate cache entries based on political changes
    /// </summary>
    public void InvalidateRelatedEntries(PoliticalParty changedParty)
    {
        var invalidatedKeys = new List<string>();

        // Find all cache entries that involve the changed party
        foreach (var kvp in _cache)
        {
            if (kvp.Value.Key.InvolvesParty(changedParty))
            {
                invalidatedKeys.Add(kvp.Key);
            }
        }

        // Remove invalidated entries
        foreach (var key in invalidatedKeys)
        {
            if (_cache.TryRemove(key, out _))
            {
                Interlocked.Increment(ref _invalidations);
            }
        }

        Debug.Log($"[ConcurrentCoalitionCache] Invalidated {invalidatedKeys.Count} entries for party: {changedParty.Name}");
    }

    /// <summary>
    /// Get cache statistics
    /// </summary>
    public CoalitionCacheStatistics GetStatistics()
    {
        var totalRequests = _hits + _misses;

        return new CoalitionCacheStatistics
        {
            TotalEntries = _cache.Count,
            Hits = _hits,
            Misses = _misses,
            Invalidations = _invalidations,
            HitRate = totalRequests > 0 ? (float)_hits / totalRequests : 0f,
            AverageAccessCount = _cache.Values.Count > 0 ? _cache.Values.Average(e => e.AccessCount) : 0f
        };
    }

    private void PerformCleanup(object state)
    {
        _cleanupLock.EnterWriteLock();
        try
        {
            var currentTime = DateTime.UtcNow;
            var expiredKeys = new List<string>();
            var lruCandidates = new List<(string Key, CoalitionCacheEntry Entry)>();

            // Identify expired and LRU candidates
            foreach (var kvp in _cache)
            {
                if (kvp.Value.IsExpired)
                {
                    expiredKeys.Add(kvp.Key);
                }
                else
                {
                    lruCandidates.Add((kvp.Key, kvp.Value));
                }
            }

            // Remove expired entries
            foreach (var key in expiredKeys)
            {
                _cache.TryRemove(key, out _);
            }

            // If still over capacity, remove LRU entries
            if (_cache.Count > maxCacheSize)
            {
                var lruEntries = lruCandidates
                    .OrderBy(c => c.Entry.LastAccessTime)
                    .ThenBy(c => c.Entry.AccessCount)
                    .Take(_cache.Count - maxCacheSize)
                    .ToList();

                foreach (var (key, _) in lruEntries)
                {
                    _cache.TryRemove(key, out _);
                }
            }

            var totalRemoved = expiredKeys.Count + (_cache.Count > maxCacheSize ? _cache.Count - maxCacheSize : 0);
            if (totalRemoved > 0)
            {
                Debug.Log($"[ConcurrentCoalitionCache] Cleanup removed {totalRemoved} entries");
            }
        }
        finally
        {
            _cleanupLock.ExitWriteLock();
        }
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
        _cleanupLock?.Dispose();
    }
}

/// <summary>
/// Immutable cache entry to prevent race conditions
/// </summary>
public record CoalitionCacheEntry
{
    public CoalitionKey Key { get; init; }
    public CoalitionResult Result { get; init; }
    public DateTime CreationTime { get; init; }
    public DateTime LastAccessTime { get; init; }
    public int AccessCount { get; init; }

    public bool IsExpired => DateTime.UtcNow - CreationTime > TimeSpan.FromMinutes(30);
}

/// <summary>
/// Immutable coalition key for consistent hashing
/// </summary>
public record CoalitionKey
{
    public IReadOnlyList<string> PartyIds { get; init; }
    public string PoliticalContext { get; init; }
    public DateTime ContextTimestamp { get; init; }

    public bool InvolvesParty(PoliticalParty party)
    {
        return PartyIds.Contains(party.PartyId);
    }

    public override string ToString()
    {
        var sortedIds = PartyIds.OrderBy(id => id);
        return $"{string.Join(",", sortedIds)}|{PoliticalContext}|{ContextTimestamp:yyyyMMddHH}";
    }
}

[System.Serializable]
public class CoalitionCacheStatistics
{
    public int TotalEntries;
    public long Hits;
    public long Misses;
    public long Invalidations;
    public float HitRate;
    public float AverageAccessCount;
}
```

### Component 3: Thread-Safe Object Pool Manager
```csharp
/// <summary>
/// Thread-safe object pool with lock-free operations and monitoring
/// </summary>
public class ConcurrentObjectPool<T> : IDisposable where T : class, new()
{
    private readonly ConcurrentQueue<T> _objects = new();
    private readonly Func<T> _factory;
    private readonly Action<T> _resetAction;
    private readonly int _maxSize;

    // Thread-safe counters
    private long _totalCreated = 0;
    private long _totalGets = 0;
    private long _totalReturns = 0;
    private long _currentCount = 0;

    // Pool monitoring
    private readonly Timer _monitoringTimer;
    private volatile float _utilizationRate = 0f;

    public ConcurrentObjectPool(int maxSize = 100, Func<T> factory = null, Action<T> resetAction = null)
    {
        _maxSize = maxSize;
        _factory = factory ?? (() => new T());
        _resetAction = resetAction ?? (obj => { });

        // Monitor pool utilization every 30 seconds
        _monitoringTimer = new Timer(UpdateUtilizationMetrics, null,
            TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// Get object from pool (lock-free)
    /// </summary>
    public T Get()
    {
        using (COALITIONProfilerMarkers.ObjectPoolGet.Auto())
        {
            Interlocked.Increment(ref _totalGets);

            if (_objects.TryDequeue(out var item))
            {
                Interlocked.Decrement(ref _currentCount);
                return item;
            }

            // Create new object if pool is empty
            var newItem = _factory();
            Interlocked.Increment(ref _totalCreated);
            return newItem;
        }
    }

    /// <summary>
    /// Return object to pool (lock-free)
    /// </summary>
    public void Return(T item)
    {
        if (item == null)
            return;

        using (COALITIONProfilerMarkers.ObjectPoolReturn.Auto())
        {
            Interlocked.Increment(ref _totalReturns);

            // Check capacity before adding
            if (Interlocked.Read(ref _currentCount) >= _maxSize)
            {
                // Pool is full, just let the object be GC'd
                return;
            }

            try
            {
                // Reset object state
                _resetAction(item);

                _objects.Enqueue(item);
                Interlocked.Increment(ref _currentCount);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ConcurrentObjectPool] Error resetting object: {e.Message}");
                // Don't return corrupted object to pool
            }
        }
    }

    /// <summary>
    /// Get pool statistics (thread-safe)
    /// </summary>
    public ConcurrentPoolStatistics GetStatistics()
    {
        var totalGets = Interlocked.Read(ref _totalGets);
        var totalReturns = Interlocked.Read(ref _totalReturns);
        var totalCreated = Interlocked.Read(ref _totalCreated);
        var currentCount = Interlocked.Read(ref _currentCount);

        return new ConcurrentPoolStatistics
        {
            TotalGets = totalGets,
            TotalReturns = totalReturns,
            TotalCreated = totalCreated,
            CurrentCount = (int)currentCount,
            MaxSize = _maxSize,
            HitRate = totalGets > 0 ? 1.0f - (float)totalCreated / totalGets : 0f,
            ReturnRate = totalGets > 0 ? (float)totalReturns / totalGets : 0f,
            UtilizationRate = _utilizationRate
        };
    }

    /// <summary>
    /// Pre-warm pool with objects
    /// </summary>
    public void PreWarm(int count)
    {
        var warmCount = Math.Min(count, _maxSize);

        for (int i = 0; i < warmCount; i++)
        {
            var item = _factory();
            _objects.Enqueue(item);
            Interlocked.Increment(ref _currentCount);
            Interlocked.Increment(ref _totalCreated);
        }

        Debug.Log($"[ConcurrentObjectPool] Pre-warmed pool with {warmCount} objects");
    }

    private void UpdateUtilizationMetrics(object state)
    {
        var currentCount = Interlocked.Read(ref _currentCount);
        _utilizationRate = _maxSize > 0 ? (float)currentCount / _maxSize : 0f;
    }

    public void Dispose()
    {
        _monitoringTimer?.Dispose();

        // Dispose all objects in pool if they implement IDisposable
        while (_objects.TryDequeue(out var item))
        {
            if (item is IDisposable disposable)
                disposable.Dispose();
        }
    }
}

[System.Serializable]
public class ConcurrentPoolStatistics
{
    public long TotalGets;
    public long TotalReturns;
    public long TotalCreated;
    public int CurrentCount;
    public int MaxSize;
    public float HitRate;
    public float ReturnRate;
    public float UtilizationRate;
}
```

### Component 4: Thread-Safe State Manager
```csharp
/// <summary>
/// Thread-safe game state management with atomic updates and snapshots
/// </summary>
public class ConcurrentGameStateManager : MonoBehaviour
{
    private volatile GameState _currentState;
    private readonly ReaderWriterLockSlim _stateLock = new();
    private readonly ConcurrentQueue<StateTransition> _pendingTransitions = new();
    private readonly SemaphoreSlim _transitionSemaphore = new(1, 1);

    // State change notifications
    private readonly ConcurrentDictionary<string, Action<GameState, GameState>> _stateChangeHandlers = new();

    [Header("State Management Configuration")]
    [SerializeField] private float stateUpdateInterval = 0.1f; // 10 updates per second
    [SerializeField] private int maxPendingTransitions = 100;

    private Coroutine _stateUpdateCoroutine;

    private void Start()
    {
        _currentState = new GameState(); // Initialize with default state
        _stateUpdateCoroutine = StartCoroutine(ProcessStateTransitions());
    }

    /// <summary>
    /// Get current game state (thread-safe read)
    /// </summary>
    public GameState GetCurrentState()
    {
        _stateLock.EnterReadLock();
        try
        {
            // Return defensive copy to prevent external modification
            return _currentState.CreateCopy();
        }
        finally
        {
            _stateLock.ExitReadLock();
        }
    }

    /// <summary>
    /// Queue state transition (thread-safe)
    /// </summary>
    public bool QueueStateTransition(StateTransition transition)
    {
        if (_pendingTransitions.Count >= maxPendingTransitions)
        {
            Debug.LogWarning("[ConcurrentGameStateManager] Transition queue full, dropping transition");
            return false;
        }

        transition.QueueTime = DateTime.UtcNow;
        _pendingTransitions.Enqueue(transition);
        return true;
    }

    /// <summary>
    /// Apply immediate state change (use sparingly)
    /// </summary>
    public async Task<bool> ApplyImmediateStateChangeAsync(Func<GameState, GameState> stateModifier, string reason)
    {
        await _transitionSemaphore.WaitAsync();
        try
        {
            _stateLock.EnterWriteLock();
            try
            {
                var oldState = _currentState;
                var newState = stateModifier(oldState.CreateCopy());

                if (ValidateStateTransition(oldState, newState))
                {
                    _currentState = newState;
                    NotifyStateChange(oldState, newState);

                    Debug.Log($"[ConcurrentGameStateManager] Immediate state change: {reason}");
                    return true;
                }

                Debug.LogError($"[ConcurrentGameStateManager] Invalid immediate state transition: {reason}");
                return false;
            }
            finally
            {
                _stateLock.ExitWriteLock();
            }
        }
        finally
        {
            _transitionSemaphore.Release();
        }
    }

    /// <summary>
    /// Register for state change notifications
    /// </summary>
    public void RegisterStateChangeHandler(string handlerId, Action<GameState, GameState> handler)
    {
        _stateChangeHandlers.AddOrUpdate(handlerId, handler, (key, existing) => handler);
    }

    /// <summary>
    /// Unregister state change handler
    /// </summary>
    public void UnregisterStateChangeHandler(string handlerId)
    {
        _stateChangeHandlers.TryRemove(handlerId, out _);
    }

    private IEnumerator ProcessStateTransitions()
    {
        while (true)
        {
            yield return new WaitForSeconds(stateUpdateInterval);

            if (_pendingTransitions.IsEmpty)
                continue;

            yield return StartCoroutine(ProcessPendingTransitions());
        }
    }

    private IEnumerator ProcessPendingTransitions()
    {
        var processedTransitions = new List<StateTransition>();
        var maxTransitionsPerFrame = 10; // Prevent frame rate impact

        // Collect transitions to process
        while (processedTransitions.Count < maxTransitionsPerFrame &&
               _pendingTransitions.TryDequeue(out var transition))
        {
            processedTransitions.Add(transition);
        }

        if (processedTransitions.Count == 0)
            yield break;

        // Process transitions atomically
        yield return StartCoroutine(ApplyTransitionBatch(processedTransitions));
    }

    private IEnumerator ApplyTransitionBatch(List<StateTransition> transitions)
    {
        await _transitionSemaphore.WaitAsync();
        try
        {
            _stateLock.EnterWriteLock();
            try
            {
                var currentState = _currentState;
                var newState = currentState.CreateCopy();

                // Apply all transitions
                foreach (var transition in transitions)
                {
                    newState = transition.Apply(newState);
                    yield return null; // Yield between transitions
                }

                // Validate final state
                if (ValidateStateTransition(currentState, newState))
                {
                    _currentState = newState;
                    NotifyStateChange(currentState, newState);

                    Debug.Log($"[ConcurrentGameStateManager] Applied {transitions.Count} state transitions");
                }
                else
                {
                    Debug.LogError($"[ConcurrentGameStateManager] Invalid state transition batch");
                }
            }
            finally
            {
                _stateLock.ExitWriteLock();
            }
        }
        finally
        {
            _transitionSemaphore.Release();
        }
    }

    private bool ValidateStateTransition(GameState oldState, GameState newState)
    {
        // Implement state validation logic
        // For example: check for valid political configurations, budget constraints, etc.

        if (newState == null)
            return false;

        // Add specific validation rules here
        return true;
    }

    private void NotifyStateChange(GameState oldState, GameState newState)
    {
        // Notify all registered handlers concurrently
        var notificationTasks = _stateChangeHandlers.Values
            .Select(handler => Task.Run(() =>
            {
                try
                {
                    handler(oldState, newState);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ConcurrentGameStateManager] State change handler error: {e.Message}");
                }
            }))
            .ToArray();

        // Don't await - fire and forget for performance
        Task.Run(async () => await Task.WhenAll(notificationTasks));
    }

    private void OnDestroy()
    {
        if (_stateUpdateCoroutine != null)
            StopCoroutine(_stateUpdateCoroutine);

        _stateLock?.Dispose();
        _transitionSemaphore?.Dispose();
    }
}

/// <summary>
/// Immutable state transition for thread-safe queuing
/// </summary>
public abstract class StateTransition
{
    public string TransitionId { get; init; } = Guid.NewGuid().ToString();
    public string Reason { get; init; }
    public DateTime QueueTime { get; set; }
    public int Priority { get; init; } = 0; // Higher values = higher priority

    public abstract GameState Apply(GameState currentState);
}

/// <summary>
/// Game state with copy semantics for thread safety
/// </summary>
[System.Serializable]
public class GameState
{
    // Immutable properties
    public string StateId { get; init; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    // Game data (should be immutable or use defensive copying)
    public GamePhase CurrentPhase { get; set; }
    public IReadOnlyList<PoliticalParty> Parties { get; set; }
    public IReadOnlyDictionary<string, object> GameData { get; set; }

    public GameState CreateCopy()
    {
        return new GameState
        {
            CurrentPhase = CurrentPhase,
            Parties = Parties?.ToList(),
            GameData = GameData?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        };
    }
}
```

---

## 🧪 **THREAD SAFETY TESTING**

### Comprehensive Concurrency Test Suite
```csharp
[TestFixture]
public class ThreadSafetyTests
{
    [Test, Performance]
    public void ThreadSafeEventStorage_ConcurrencyTest()
    {
        const int threadCount = 10;
        const int operationsPerThread = 1000;
        var exceptions = new ConcurrentBag<Exception>();

        var tasks = Enumerable.Range(0, threadCount)
            .Select(threadId => Task.Run(() =>
            {
                try
                {
                    for (int i = 0; i < operationsPerThread; i++)
                    {
                        // Add handler
                        ThreadSafeEventStorage.AddHandler<TestGameEvent>(e => { });

                        // Get handlers
                        var handlers = ThreadSafeEventStorage.GetHandlers<TestGameEvent>().ToList();

                        // Remove handler occasionally
                        if (i % 10 == 0)
                        {
                            ThreadSafeEventStorage.RemoveHandler<TestGameEvent>(e => { });
                        }
                    }
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }
            }))
            .ToArray();

        Task.WaitAll(tasks);

        Assert.IsEmpty(exceptions, $"Thread safety violations detected: {exceptions.Count}");

        var finalHandlerCount = ThreadSafeEventStorage.GetHandlerCount<TestGameEvent>();
        Assert.Greater(finalHandlerCount, 0, "Some handlers should remain after concurrent operations");

        Debug.Log($"Concurrent operations completed. Final handler count: {finalHandlerCount}");
    }

    [Test, Performance]
    public void ConcurrentCoalitionCache_StressTest()
    {
        var cache = new ConcurrentCoalitionCache();
        const int threadCount = 8;
        const int operationsPerThread = 500;
        var exceptions = new ConcurrentBag<Exception>();

        var tasks = Enumerable.Range(0, threadCount)
            .Select(threadId => Task.Run(() =>
            {
                try
                {
                    for (int i = 0; i < operationsPerThread; i++)
                    {
                        var key = new CoalitionKey
                        {
                            PartyIds = new[] { $"party{threadId}", $"party{i % 5}" },
                            PoliticalContext = "test",
                            ContextTimestamp = DateTime.UtcNow
                        };

                        var result = new CoalitionResult
                        {
                            CompatibilityScore = threadId * i % 100,
                            IsViable = i % 2 == 0
                        };

                        // Cache result
                        cache.CacheCoalition(key, result);

                        // Try to retrieve
                        cache.TryGetCoalition(key, out var retrieved);

                        // Invalidate occasionally
                        if (i % 20 == 0)
                        {
                            var party = new PoliticalParty { PartyId = $"party{threadId}" };
                            cache.InvalidateRelatedEntries(party);
                        }
                    }
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }
            }))
            .ToArray();

        Task.WaitAll(tasks);

        Assert.IsEmpty(exceptions, $"Thread safety violations in cache: {exceptions.Count}");

        var stats = cache.GetStatistics();
        Assert.Greater(stats.HitRate, 0f, "Cache should have some hits");

        Debug.Log($"Cache stress test completed. Hit rate: {stats.HitRate:P2}");
    }

    [Test, Performance]
    public void ConcurrentObjectPool_PerformanceTest()
    {
        var pool = new ConcurrentObjectPool<TestObject>(maxSize: 50);
        const int threadCount = 12;
        const int operationsPerThread = 1000;
        var exceptions = new ConcurrentBag<Exception>();

        // Pre-warm pool
        pool.PreWarm(25);

        var tasks = Enumerable.Range(0, threadCount)
            .Select(threadId => Task.Run(() =>
            {
                try
                {
                    var localObjects = new List<TestObject>();

                    for (int i = 0; i < operationsPerThread; i++)
                    {
                        // Get objects
                        var obj = pool.Get();
                        localObjects.Add(obj);

                        // Return objects occasionally
                        if (localObjects.Count > 10)
                        {
                            pool.Return(localObjects[0]);
                            localObjects.RemoveAt(0);
                        }
                    }

                    // Return remaining objects
                    foreach (var obj in localObjects)
                    {
                        pool.Return(obj);
                    }
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }
            }))
            .ToArray();

        Task.WaitAll(tasks);

        Assert.IsEmpty(exceptions, $"Thread safety violations in object pool: {exceptions.Count}");

        var stats = pool.GetStatistics();
        Assert.Greater(stats.HitRate, 0.5f, "Pool hit rate should be > 50%");

        Debug.Log($"Object pool performance test completed. Hit rate: {stats.HitRate:P2}");
    }

    private class TestGameEvent : IGameEvent
    {
        public string Message { get; set; } = "Test Event";
    }

    private class TestObject
    {
        public int Value { get; set; } = Random.Range(0, 1000);
        public string Data { get; set; } = "Test Data";
    }
}
```

---

## 📈 **PERFORMANCE EXPECTATIONS**

### Thread Safety Benefits
- **Data Integrity**: 100% elimination of race conditions
- **Concurrent Performance**: Near-linear scaling with CPU cores
- **Memory Safety**: No data corruption under concurrent access
- **Deadlock Prevention**: Lock-free patterns prevent deadlocks
- **Predictable Latency**: Consistent performance under load

### Performance Targets
- **EventBus Operations**: < 1ms under concurrent load
- **Coalition Cache**: 95%+ hit rate with thread safety
- **Object Pool**: 90%+ hit rate under concurrent access
- **State Management**: < 10ms for complex state transitions
- **Memory Consistency**: Zero memory visibility issues

This comprehensive thread-safe concurrent collections implementation ensures data integrity and high performance under multi-threaded scenarios in the COALITION game.