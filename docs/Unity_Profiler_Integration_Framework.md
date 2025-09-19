# Unity Profiler Integration Framework
## Comprehensive Performance Monitoring and Analysis System

### 🎯 **OBJECTIVE**
Create a comprehensive Unity profiler integration framework that provides real-time performance monitoring, automated profiling sessions, performance hotspot identification, and continuous performance regression detection for the COALITION game.

---

## 🔬 **PROFILER INTEGRATION ARCHITECTURE**

### Core Components
1. **Custom Profiler Markers**: Comprehensive coverage of all performance-critical systems
2. **Automated Profiling Sessions**: Scheduled and event-driven profiling
3. **Performance Data Analysis**: Real-time analysis and reporting
4. **Hotspot Detection**: Automatic identification of performance bottlenecks
5. **Regression Monitoring**: Continuous tracking of performance changes
6. **Dashboard Integration**: Real-time performance visualization

---

## 📊 **CUSTOM PROFILER MARKERS**

### System-Wide Profiler Marker Definitions
```csharp
/// <summary>
/// Centralized profiler markers for all COALITION systems
/// </summary>
public static class COALITIONProfilerMarkers
{
    // EventBus System Markers
    public static readonly ProfilerMarker EventBusSubscribe =
        new ProfilerMarker("COALITION.EventBus.Subscribe");
    public static readonly ProfilerMarker EventBusUnsubscribe =
        new ProfilerMarker("COALITION.EventBus.Unsubscribe");
    public static readonly ProfilerMarker EventBusPublish =
        new ProfilerMarker("COALITION.EventBus.Publish");
    public static readonly ProfilerMarker EventBusBatchPublish =
        new ProfilerMarker("COALITION.EventBus.BatchPublish");
    public static readonly ProfilerMarker EventBusCleanup =
        new ProfilerMarker("COALITION.EventBus.Cleanup");

    // Coalition Calculation Markers
    public static readonly ProfilerMarker CoalitionCalculation =
        new ProfilerMarker("COALITION.Coalition.Calculate");
    public static readonly ProfilerMarker CompatibilityMatrix =
        new ProfilerMarker("COALITION.Coalition.CompatibilityMatrix");
    public static readonly ProfilerMarker CoalitionValidation =
        new ProfilerMarker("COALITION.Coalition.Validation");
    public static readonly ProfilerMarker CoalitionCacheHit =
        new ProfilerMarker("COALITION.Coalition.CacheHit");
    public static readonly ProfilerMarker CoalitionCacheMiss =
        new ProfilerMarker("COALITION.Coalition.CacheMiss");

    // AI System Markers
    public static readonly ProfilerMarker AIRequest =
        new ProfilerMarker("COALITION.AI.Request");
    public static readonly ProfilerMarker AIResponse =
        new ProfilerMarker("COALITION.AI.Response");
    public static readonly ProfilerMarker AIBatchRequest =
        new ProfilerMarker("COALITION.AI.BatchRequest");
    public static readonly ProfilerMarker AICacheHit =
        new ProfilerMarker("COALITION.AI.CacheHit");
    public static readonly ProfilerMarker AICacheMiss =
        new ProfilerMarker("COALITION.AI.CacheMiss");
    public static readonly ProfilerMarker AIProcessing =
        new ProfilerMarker("COALITION.AI.Processing");

    // Memory Management Markers
    public static readonly ProfilerMarker ObjectPoolGet =
        new ProfilerMarker("COALITION.Memory.ObjectPool.Get");
    public static readonly ProfilerMarker ObjectPoolReturn =
        new ProfilerMarker("COALITION.Memory.ObjectPool.Return");
    public static readonly ProfilerMarker LRUCacheHit =
        new ProfilerMarker("COALITION.Memory.LRUCache.Hit");
    public static readonly ProfilerMarker LRUCacheMiss =
        new ProfilerMarker("COALITION.Memory.LRUCache.Miss");
    public static readonly ProfilerMarker MemoryPressureCheck =
        new ProfilerMarker("COALITION.Memory.PressureCheck");
    public static readonly ProfilerMarker GarbageCollection =
        new ProfilerMarker("COALITION.Memory.GC");

    // Game Logic Markers
    public static readonly ProfilerMarker GameStateUpdate =
        new ProfilerMarker("COALITION.Game.StateUpdate");
    public static readonly ProfilerMarker PoliticalActionProcess =
        new ProfilerMarker("COALITION.Game.PoliticalAction");
    public static readonly ProfilerMarker UIUpdate =
        new ProfilerMarker("COALITION.UI.Update");
    public static readonly ProfilerMarker SceneTransition =
        new ProfilerMarker("COALITION.Game.SceneTransition");

    // Network/IO Markers
    public static readonly ProfilerMarker NetworkRequest =
        new ProfilerMarker("COALITION.Network.Request");
    public static readonly ProfilerMarker NetworkResponse =
        new ProfilerMarker("COALITION.Network.Response");
    public static readonly ProfilerMarker FileIO =
        new ProfilerMarker("COALITION.IO.File");
    public static readonly ProfilerMarker DataSerialization =
        new ProfilerMarker("COALITION.Data.Serialization");

    /// <summary>
    /// Get all profiler markers for comprehensive monitoring
    /// </summary>
    public static ProfilerMarker[] GetAllMarkers()
    {
        return new[]
        {
            EventBusSubscribe, EventBusUnsubscribe, EventBusPublish, EventBusBatchPublish, EventBusCleanup,
            CoalitionCalculation, CompatibilityMatrix, CoalitionValidation, CoalitionCacheHit, CoalitionCacheMiss,
            AIRequest, AIResponse, AIBatchRequest, AICacheHit, AICacheMiss, AIProcessing,
            ObjectPoolGet, ObjectPoolReturn, LRUCacheHit, LRUCacheMiss, MemoryPressureCheck, GarbageCollection,
            GameStateUpdate, PoliticalActionProcess, UIUpdate, SceneTransition,
            NetworkRequest, NetworkResponse, FileIO, DataSerialization
        };
    }
}
```

### Profiler Marker Extensions and Utilities
```csharp
/// <summary>
/// Extension methods for easier profiler marker usage
/// </summary>
public static class ProfilerMarkerExtensions
{
    /// <summary>
    /// Create a disposable profiler scope for using statements
    /// </summary>
    public static ProfilerScope Auto(this ProfilerMarker marker)
    {
        return new ProfilerScope(marker);
    }

    /// <summary>
    /// Begin profiler marker with custom sample metadata
    /// </summary>
    public static void BeginWithMetadata<T>(this ProfilerMarker marker, string metadataName, T metadataValue)
    {
        marker.Begin();
        // Add custom metadata if needed for detailed analysis
        if (typeof(T) == typeof(int))
            Profiler.BeginSample($"{marker.Name}_{metadataName}_{metadataValue}");
        else if (typeof(T) == typeof(string))
            Profiler.BeginSample($"{marker.Name}_{metadataName}_{metadataValue}");
    }

    /// <summary>
    /// End profiler marker with metadata
    /// </summary>
    public static void EndWithMetadata(this ProfilerMarker marker)
    {
        Profiler.EndSample();
        marker.End();
    }
}

/// <summary>
/// Disposable profiler scope for automatic begin/end
/// </summary>
public readonly struct ProfilerScope : IDisposable
{
    private readonly ProfilerMarker _marker;

    public ProfilerScope(ProfilerMarker marker)
    {
        _marker = marker;
        _marker.Begin();
    }

    public void Dispose()
    {
        _marker.End();
    }
}
```

---

## 🏗️ **AUTOMATED PROFILING SYSTEM**

### Profiling Session Manager
```csharp
/// <summary>
/// Manages automated profiling sessions with configurable triggers and analysis
/// </summary>
public class ProfilingSessionManager : MonoBehaviour
{
    [Header("Profiling Configuration")]
    [SerializeField] private bool enableAutomaticProfiling = true;
    [SerializeField] private float profilingInterval = 300f; // 5 minutes
    [SerializeField] private int sessionDuration = 60; // 60 seconds
    [SerializeField] private string profilingOutputPath = "ProfilingData";

    [Header("Performance Thresholds")]
    [SerializeField] private float frameTimeThreshold = 16.67f; // 60 FPS threshold
    [SerializeField] private long memoryThreshold = 500 * 1024 * 1024; // 500MB
    [SerializeField] private float cpuUsageThreshold = 80f; // 80% CPU

    private ProfilingSession _currentSession;
    private List<ProfilingSession> _completedSessions = new();
    private Coroutine _profilingCoroutine;
    private PerformanceTracker _performanceTracker;

    private void Awake()
    {
        _performanceTracker = new PerformanceTracker();

        // Subscribe to performance events
        _performanceTracker.PerformanceThresholdExceeded += OnPerformanceThresholdExceeded;

        // Ensure output directory exists
        Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, profilingOutputPath));
    }

    private void Start()
    {
        if (enableAutomaticProfiling)
        {
            _profilingCoroutine = StartCoroutine(AutomaticProfilingLoop());
        }
    }

    private IEnumerator AutomaticProfilingLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(profilingInterval);

            if (!IsCurrentlyProfiling())
            {
                yield return StartCoroutine(StartProfilingSession("Automatic"));
            }
        }
    }

    /// <summary>
    /// Start a new profiling session
    /// </summary>
    public IEnumerator StartProfilingSession(string sessionName, float duration = -1)
    {
        if (IsCurrentlyProfiling())
        {
            Debug.LogWarning("[ProfilingSessionManager] Profiling session already in progress");
            yield break;
        }

        var actualDuration = duration > 0 ? duration : sessionDuration;
        _currentSession = new ProfilingSession
        {
            SessionName = sessionName,
            StartTime = DateTime.UtcNow,
            Duration = actualDuration,
            MarkerData = new Dictionary<string, List<float>>()
        };

        Debug.Log($"[ProfilingSessionManager] Starting profiling session: {sessionName} ({actualDuration}s)");

        // Enable deep profiling
        Profiler.enabled = true;
        Profiler.enableBinaryLog = true;
        Profiler.logFile = GetSessionLogPath(_currentSession);

        // Start collecting custom metrics
        StartCustomMetricsCollection();

        yield return new WaitForSeconds(actualDuration);

        yield return StartCoroutine(EndProfilingSession());
    }

    /// <summary>
    /// End current profiling session and analyze results
    /// </summary>
    public IEnumerator EndProfilingSession()
    {
        if (_currentSession == null)
            yield break;

        Debug.Log($"[ProfilingSessionManager] Ending profiling session: {_currentSession.SessionName}");

        // Stop profiling
        Profiler.enabled = false;
        Profiler.enableBinaryLog = false;

        // Stop custom metrics collection
        StopCustomMetricsCollection();

        // Analyze session results
        yield return StartCoroutine(AnalyzeProfilingSession(_currentSession));

        // Save session data
        SaveProfilingSession(_currentSession);

        // Add to completed sessions
        _completedSessions.Add(_currentSession);
        _currentSession = null;

        // Trigger regression analysis if we have enough data
        if (_completedSessions.Count >= 3)
        {
            yield return StartCoroutine(PerformRegressionAnalysis());
        }
    }

    private void StartCustomMetricsCollection()
    {
        InvokeRepeating(nameof(CollectCustomMetrics), 0f, 1f); // Collect every second
    }

    private void StopCustomMetricsCollection()
    {
        CancelInvoke(nameof(CollectCustomMetrics));
    }

    private void CollectCustomMetrics()
    {
        if (_currentSession == null)
            return;

        // Collect frame time
        RecordMetric("FrameTime", Time.unscaledDeltaTime * 1000f); // Convert to milliseconds

        // Collect memory usage
        var memoryUsage = Profiler.GetTotalAllocatedMemoryLong();
        RecordMetric("MemoryUsage", memoryUsage / (1024f * 1024f)); // Convert to MB

        // Collect custom performance counters
        RecordEventBusMetrics();
        RecordCoalitionCalculationMetrics();
        RecordAISystemMetrics();
        RecordMemorySystemMetrics();
    }

    private void RecordEventBusMetrics()
    {
        // Get EventBus performance data
        var subscriberCounts = EventBusV2.GetTotalSubscriberCount();
        RecordMetric("EventBus.SubscriberCount", subscriberCounts);

        var publishCount = EventBusProfiler.GetTotalPublishCount();
        RecordMetric("EventBus.PublishCount", publishCount);
    }

    private void RecordCoalitionCalculationMetrics()
    {
        // Record coalition calculation performance
        var calculationCount = CoalitionCalculationProfiler.GetCalculationCount();
        RecordMetric("Coalition.CalculationCount", calculationCount);

        var averageCalculationTime = CoalitionCalculationProfiler.GetAverageCalculationTime();
        RecordMetric("Coalition.AverageCalculationTime", averageCalculationTime);
    }

    private void RecordAISystemMetrics()
    {
        // Record AI system metrics
        var requestCount = AISystemProfiler.GetRequestCount();
        RecordMetric("AI.RequestCount", requestCount);

        var cacheHitRate = AISystemProfiler.GetCacheHitRate();
        RecordMetric("AI.CacheHitRate", cacheHitRate * 100f);
    }

    private void RecordMemorySystemMetrics()
    {
        // Record memory system metrics
        var poolStats = ObjectPoolManager.GetAllStatistics();
        var totalHitRate = poolStats.Values.Average(s => s.HitRate);
        RecordMetric("Memory.PoolHitRate", (float)(totalHitRate * 100f));

        var memoryPressure = MemoryPressureMonitor.Instance.GetMemoryStatistics();
        RecordMetric("Memory.IsHighPressure", memoryPressure.IsHighPressure ? 1f : 0f);
    }

    private void RecordMetric(string metricName, float value)
    {
        if (_currentSession?.MarkerData == null)
            return;

        if (!_currentSession.MarkerData.ContainsKey(metricName))
            _currentSession.MarkerData[metricName] = new List<float>();

        _currentSession.MarkerData[metricName].Add(value);
    }

    /// <summary>
    /// Analyze profiling session results
    /// </summary>
    private IEnumerator AnalyzeProfilingSession(ProfilingSession session)
    {
        Debug.Log($"[ProfilingSessionManager] Analyzing session: {session.SessionName}");

        var analysis = new ProfilingAnalysis
        {
            SessionName = session.SessionName,
            AnalysisTime = DateTime.UtcNow,
            Metrics = new Dictionary<string, MetricAnalysis>()
        };

        // Analyze each metric
        foreach (var kvp in session.MarkerData)
        {
            var metricAnalysis = AnalyzeMetric(kvp.Key, kvp.Value);
            analysis.Metrics[kvp.Key] = metricAnalysis;
            yield return null; // Yield to prevent frame drops
        }

        // Identify performance issues
        analysis.PerformanceIssues = IdentifyPerformanceIssues(analysis);

        // Generate recommendations
        analysis.Recommendations = GenerateRecommendations(analysis);

        session.Analysis = analysis;

        Debug.Log($"[ProfilingSessionManager] Analysis complete. Found {analysis.PerformanceIssues.Count} issues");
    }

    private MetricAnalysis AnalyzeMetric(string metricName, List<float> values)
    {
        if (values.Count == 0)
            return new MetricAnalysis { MetricName = metricName };

        var analysis = new MetricAnalysis
        {
            MetricName = metricName,
            SampleCount = values.Count,
            Average = values.Average(),
            Minimum = values.Min(),
            Maximum = values.Max(),
            StandardDeviation = CalculateStandardDeviation(values)
        };

        // Calculate percentiles
        var sortedValues = values.OrderBy(x => x).ToList();
        analysis.Percentile50 = GetPercentile(sortedValues, 0.5f);
        analysis.Percentile95 = GetPercentile(sortedValues, 0.95f);
        analysis.Percentile99 = GetPercentile(sortedValues, 0.99f);

        return analysis;
    }

    private float CalculateStandardDeviation(List<float> values)
    {
        var average = values.Average();
        var variance = values.Select(x => Math.Pow(x - average, 2)).Average();
        return (float)Math.Sqrt(variance);
    }

    private float GetPercentile(List<float> sortedValues, float percentile)
    {
        var index = (int)((sortedValues.Count - 1) * percentile);
        return sortedValues[index];
    }

    private List<PerformanceIssue> IdentifyPerformanceIssues(ProfilingAnalysis analysis)
    {
        var issues = new List<PerformanceIssue>();

        foreach (var metric in analysis.Metrics.Values)
        {
            // Check frame time issues
            if (metric.MetricName == "FrameTime" && metric.Average > frameTimeThreshold)
            {
                issues.Add(new PerformanceIssue
                {
                    Type = PerformanceIssueType.FrameTime,
                    Severity = metric.Average > frameTimeThreshold * 2 ? IssueSeverity.Critical : IssueSeverity.High,
                    Description = $"Average frame time ({metric.Average:F2}ms) exceeds threshold ({frameTimeThreshold:F2}ms)",
                    MetricName = metric.MetricName,
                    Value = metric.Average
                });
            }

            // Check memory issues
            if (metric.MetricName == "MemoryUsage" && metric.Maximum > memoryThreshold / (1024f * 1024f))
            {
                issues.Add(new PerformanceIssue
                {
                    Type = PerformanceIssueType.Memory,
                    Severity = IssueSeverity.High,
                    Description = $"Peak memory usage ({metric.Maximum:F2}MB) exceeds threshold ({memoryThreshold / (1024f * 1024f):F2}MB)",
                    MetricName = metric.MetricName,
                    Value = metric.Maximum
                });
            }

            // Check cache hit rate issues
            if (metric.MetricName.Contains("HitRate") && metric.Average < 70f)
            {
                issues.Add(new PerformanceIssue
                {
                    Type = PerformanceIssueType.CacheEfficiency,
                    Severity = metric.Average < 50f ? IssueSeverity.High : IssueSeverity.Medium,
                    Description = $"Low cache hit rate ({metric.Average:F1}%) for {metric.MetricName}",
                    MetricName = metric.MetricName,
                    Value = metric.Average
                });
            }
        }

        return issues;
    }

    private List<string> GenerateRecommendations(ProfilingAnalysis analysis)
    {
        var recommendations = new List<string>();

        foreach (var issue in analysis.PerformanceIssues)
        {
            switch (issue.Type)
            {
                case PerformanceIssueType.FrameTime:
                    recommendations.Add("Consider optimizing game logic or reducing visual complexity");
                    recommendations.Add("Check for expensive operations in Update() methods");
                    break;

                case PerformanceIssueType.Memory:
                    recommendations.Add("Implement more aggressive memory cleanup");
                    recommendations.Add("Consider reducing cache sizes or implementing memory pressure handling");
                    break;

                case PerformanceIssueType.CacheEfficiency:
                    recommendations.Add($"Optimize caching strategy for {issue.MetricName}");
                    recommendations.Add("Consider adjusting cache size or eviction policies");
                    break;
            }
        }

        return recommendations.Distinct().ToList();
    }

    /// <summary>
    /// Perform regression analysis comparing recent sessions
    /// </summary>
    private IEnumerator PerformRegressionAnalysis()
    {
        Debug.Log("[ProfilingSessionManager] Performing regression analysis...");

        var recentSessions = _completedSessions.TakeLast(5).ToList();
        var regressionReport = new RegressionAnalysisReport
        {
            AnalysisTime = DateTime.UtcNow,
            SessionsAnalyzed = recentSessions.Count,
            Regressions = new List<PerformanceRegression>()
        };

        // Compare metrics across sessions
        var allMetrics = recentSessions.SelectMany(s => s.Analysis?.Metrics?.Keys ?? new string[0]).Distinct();

        foreach (var metricName in allMetrics)
        {
            var metricValues = recentSessions
                .Where(s => s.Analysis?.Metrics?.ContainsKey(metricName) == true)
                .Select(s => s.Analysis.Metrics[metricName].Average)
                .ToList();

            if (metricValues.Count >= 3)
            {
                var trend = CalculateTrend(metricValues);
                if (trend.IsRegression)
                {
                    regressionReport.Regressions.Add(new PerformanceRegression
                    {
                        MetricName = metricName,
                        TrendPercentage = trend.Percentage,
                        Severity = Math.Abs(trend.Percentage) > 20 ? IssueSeverity.High : IssueSeverity.Medium,
                        Description = $"{metricName} shows {Math.Abs(trend.Percentage):F1}% performance regression"
                    });
                }
            }

            yield return null;
        }

        // Save regression report
        SaveRegressionReport(regressionReport);

        Debug.Log($"[ProfilingSessionManager] Regression analysis complete. Found {regressionReport.Regressions.Count} regressions");
    }

    private (bool IsRegression, float Percentage) CalculateTrend(List<float> values)
    {
        if (values.Count < 2)
            return (false, 0f);

        var oldAverage = values.Take(values.Count / 2).Average();
        var newAverage = values.Skip(values.Count / 2).Average();

        var changePercentage = ((newAverage - oldAverage) / oldAverage) * 100f;

        // Consider it a regression if performance worsened by more than 5%
        var isRegression = changePercentage > 5f;

        return (isRegression, changePercentage);
    }

    private void OnPerformanceThresholdExceeded(object sender, PerformanceThresholdEventArgs e)
    {
        // Trigger emergency profiling session for performance investigation
        if (!IsCurrentlyProfiling())
        {
            StartCoroutine(StartProfilingSession($"Emergency_{e.ThresholdType}", 30f));
        }
    }

    private bool IsCurrentlyProfiling()
    {
        return _currentSession != null;
    }

    private string GetSessionLogPath(ProfilingSession session)
    {
        var fileName = $"profiling_{session.SessionName}_{session.StartTime:yyyyMMdd_HHmmss}.data";
        return Path.Combine(Application.persistentDataPath, profilingOutputPath, fileName);
    }

    private void SaveProfilingSession(ProfilingSession session)
    {
        var json = JsonUtility.ToJson(session, true);
        var filePath = Path.Combine(Application.persistentDataPath, profilingOutputPath,
            $"session_{session.SessionName}_{session.StartTime:yyyyMMdd_HHmmss}.json");
        File.WriteAllText(filePath, json);
    }

    private void SaveRegressionReport(RegressionAnalysisReport report)
    {
        var json = JsonUtility.ToJson(report, true);
        var filePath = Path.Combine(Application.persistentDataPath, profilingOutputPath,
            $"regression_report_{report.AnalysisTime:yyyyMMdd_HHmmss}.json");
        File.WriteAllText(filePath, json);
    }

    private void OnDestroy()
    {
        if (_profilingCoroutine != null)
            StopCoroutine(_profilingCoroutine);

        if (_performanceTracker != null)
        {
            _performanceTracker.PerformanceThresholdExceeded -= OnPerformanceThresholdExceeded;
            _performanceTracker.Dispose();
        }
    }
}
```

---

## 📊 **DATA STRUCTURES AND CLASSES**

### Profiling Data Models
```csharp
[System.Serializable]
public class ProfilingSession
{
    public string SessionName;
    public DateTime StartTime;
    public float Duration;
    public Dictionary<string, List<float>> MarkerData;
    public ProfilingAnalysis Analysis;
}

[System.Serializable]
public class ProfilingAnalysis
{
    public string SessionName;
    public DateTime AnalysisTime;
    public Dictionary<string, MetricAnalysis> Metrics;
    public List<PerformanceIssue> PerformanceIssues;
    public List<string> Recommendations;
}

[System.Serializable]
public class MetricAnalysis
{
    public string MetricName;
    public int SampleCount;
    public float Average;
    public float Minimum;
    public float Maximum;
    public float StandardDeviation;
    public float Percentile50;
    public float Percentile95;
    public float Percentile99;
}

[System.Serializable]
public class PerformanceIssue
{
    public PerformanceIssueType Type;
    public IssueSeverity Severity;
    public string Description;
    public string MetricName;
    public float Value;
}

[System.Serializable]
public class RegressionAnalysisReport
{
    public DateTime AnalysisTime;
    public int SessionsAnalyzed;
    public List<PerformanceRegression> Regressions;
}

[System.Serializable]
public class PerformanceRegression
{
    public string MetricName;
    public float TrendPercentage;
    public IssueSeverity Severity;
    public string Description;
}

public enum PerformanceIssueType
{
    FrameTime,
    Memory,
    CacheEfficiency,
    NetworkLatency,
    DiskIO,
    CPU,
    GPU
}

public enum IssueSeverity
{
    Low,
    Medium,
    High,
    Critical
}
```

---

## 🎮 **PERFORMANCE TRACKER**

### Real-Time Performance Monitoring
```csharp
/// <summary>
/// Real-time performance monitoring and threshold detection
/// </summary>
public class PerformanceTracker : IDisposable
{
    private readonly Timer _monitoringTimer;
    private readonly List<float> _frameTimeHistory = new();
    private readonly List<float> _memoryHistory = new();

    public event EventHandler<PerformanceThresholdEventArgs> PerformanceThresholdExceeded;

    // Configuration
    public float FrameTimeThreshold { get; set; } = 16.67f; // 60 FPS
    public long MemoryThreshold { get; set; } = 500 * 1024 * 1024; // 500MB
    public int HistorySize { get; set; } = 300; // 5 minutes at 1 sample per second

    public PerformanceTracker()
    {
        _monitoringTimer = new Timer(MonitorPerformance, null,
            TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }

    private void MonitorPerformance(object state)
    {
        // Monitor frame time
        var frameTime = Time.unscaledDeltaTime * 1000f;
        AddToHistory(_frameTimeHistory, frameTime);

        // Monitor memory usage
        var memoryUsage = Profiler.GetTotalAllocatedMemoryLong();
        AddToHistory(_memoryHistory, memoryUsage / (1024f * 1024f));

        // Check thresholds
        CheckFrameTimeThreshold(frameTime);
        CheckMemoryThreshold(memoryUsage);
        CheckTrendThresholds();
    }

    private void AddToHistory<T>(List<T> history, T value)
    {
        history.Add(value);
        if (history.Count > HistorySize)
            history.RemoveAt(0);
    }

    private void CheckFrameTimeThreshold(float frameTime)
    {
        if (frameTime > FrameTimeThreshold * 2) // 2x threshold for immediate alert
        {
            OnThresholdExceeded(PerformanceThresholdType.FrameTime, frameTime);
        }
    }

    private void CheckMemoryThreshold(long memoryUsage)
    {
        if (memoryUsage > MemoryThreshold)
        {
            OnThresholdExceeded(PerformanceThresholdType.Memory, memoryUsage);
        }
    }

    private void CheckTrendThresholds()
    {
        // Check for sustained performance degradation
        if (_frameTimeHistory.Count >= 60) // 1 minute of data
        {
            var recentAverage = _frameTimeHistory.TakeLast(30).Average();
            var olderAverage = _frameTimeHistory.Skip(_frameTimeHistory.Count - 60).Take(30).Average();

            if (recentAverage > olderAverage * 1.2f) // 20% increase
            {
                OnThresholdExceeded(PerformanceThresholdType.FrameTimeTrend, recentAverage);
            }
        }

        if (_memoryHistory.Count >= 60)
        {
            var recentAverage = _memoryHistory.TakeLast(30).Average();
            var olderAverage = _memoryHistory.Skip(_memoryHistory.Count - 60).Take(30).Average();

            if (recentAverage > olderAverage * 1.1f) // 10% increase
            {
                OnThresholdExceeded(PerformanceThresholdType.MemoryTrend, recentAverage);
            }
        }
    }

    private void OnThresholdExceeded(PerformanceThresholdType type, float value)
    {
        PerformanceThresholdExceeded?.Invoke(this, new PerformanceThresholdEventArgs
        {
            ThresholdType = type,
            Value = value,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Get current performance statistics
    /// </summary>
    public PerformanceStatistics GetCurrentStatistics()
    {
        return new PerformanceStatistics
        {
            AverageFrameTime = _frameTimeHistory.Count > 0 ? _frameTimeHistory.Average() : 0f,
            AverageMemoryUsage = _memoryHistory.Count > 0 ? _memoryHistory.Average() : 0f,
            FrameTimeHistory = _frameTimeHistory.ToList(),
            MemoryHistory = _memoryHistory.ToList(),
            Timestamp = DateTime.UtcNow
        };
    }

    public void Dispose()
    {
        _monitoringTimer?.Dispose();
    }
}

public class PerformanceThresholdEventArgs : EventArgs
{
    public PerformanceThresholdType ThresholdType { get; set; }
    public float Value { get; set; }
    public DateTime Timestamp { get; set; }
}

public enum PerformanceThresholdType
{
    FrameTime,
    Memory,
    FrameTimeTrend,
    MemoryTrend,
    CPU,
    GPU
}

[System.Serializable]
public class PerformanceStatistics
{
    public float AverageFrameTime;
    public float AverageMemoryUsage;
    public List<float> FrameTimeHistory;
    public List<float> MemoryHistory;
    public DateTime Timestamp;
}
```

---

## 🎯 **INTEGRATION EXAMPLES**

### Using Profiler Markers in Game Systems
```csharp
// Example: Enhanced EventBusV2 with profiling
public static class EventBusV2
{
    public static void Publish<T>(T gameEvent) where T : IGameEvent
    {
        using (COALITIONProfilerMarkers.EventBusPublish.Auto())
        {
            // Existing EventBus logic with profiling
            if (!_eventDelegates.TryGetValue(typeof(T), out var eventDelegate))
                return;

            var handler = eventDelegate as Action<T>;
            if (handler == null)
                return;

            try
            {
                handler.Invoke(gameEvent);
            }
            catch (Exception e)
            {
                Debug.LogError($"[EventBusV2] Error publishing {typeof(T).Name}: {e.Message}");
            }
        }
    }
}

// Example: Coalition calculation with profiling
public class CoalitionCalculator
{
    public CoalitionResult CalculateCoalition(List<PoliticalParty> parties)
    {
        using (COALITIONProfilerMarkers.CoalitionCalculation.Auto())
        {
            // Check cache first
            var cacheKey = GenerateCacheKey(parties);
            if (_cache.TryGet(cacheKey, out var cachedResult))
            {
                using (COALITIONProfilerMarkers.CoalitionCacheHit.Auto())
                {
                    return cachedResult;
                }
            }

            using (COALITIONProfilerMarkers.CoalitionCacheMiss.Auto())
            {
                // Perform calculation
                var result = PerformCalculation(parties);
                _cache.Set(cacheKey, result);
                return result;
            }
        }
    }
}
```

This comprehensive Unity profiler integration framework provides detailed performance monitoring, automated analysis, and continuous regression detection for the COALITION game, enabling proactive performance optimization and maintaining high-quality user experience.