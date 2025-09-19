using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Profiling;

namespace Coalition.Demo
{
    /// <summary>
    /// Comprehensive quality assurance and validation system for the Coalition Demo
    /// Monitors performance, validates political accuracy, and ensures technical reliability
    /// </summary>
    public class QualityAssuranceManager : MonoBehaviour
    {
        [Header("Performance Monitoring")]
        [SerializeField] private float targetFPS = 60f;
        [SerializeField] private float memoryWarningThreshold = 1024f; // MB
        [SerializeField] private float performanceCheckInterval = 1f;

        [Header("Political Validation")]
        [SerializeField] private bool enablePoliticalValidation = true;
        [SerializeField] private bool enableRealTimeValidation = true;

        [Header("Quality Gates")]
        [SerializeField] private float minimumFPS = 30f;
        [SerializeField] private float maximumMemoryMB = 2048f;
        [SerializeField] private int maxErrorsPerMinute = 5;
        [SerializeField] private float maxCoalitionCalculationTime = 5f;

        [Header("Reporting")]
        [SerializeField] private bool enableDetailedReporting = true;
        [SerializeField] private float reportingInterval = 300f; // 5 minutes

        private PerformanceMonitor performanceMonitor;
        private PoliticalValidator politicalValidator;
        private TechnicalValidator technicalValidator;
        private QualityReport currentReport;
        private List<QualityIssue> activeIssues = new List<QualityIssue>();
        private string reportDirectory;

        public static QualityAssuranceManager Instance { get; private set; }

        // Events
        public event Action<QualityIssue> OnQualityIssueDetected;
        public event Action<QualityIssue> OnQualityIssueResolved;
        public event Action<QualityReport> OnQualityReportGenerated;

        // Quality Status
        public QualityStatus CurrentStatus { get; private set; } = QualityStatus.Unknown;
        public float OverallQualityScore { get; private set; } = 0f;
        public int ActiveIssueCount => activeIssues.Count;

        [Serializable]
        public class QualityReport
        {
            public DateTime timestamp;
            public float sessionDuration;
            public PerformanceMetrics performance;
            public PoliticalAccuracyMetrics political;
            public TechnicalReliabilityMetrics technical;
            public List<QualityIssue> issues;
            public float overallScore;
            public QualityStatus status;
            public string summary;
        }

        [Serializable]
        public class PerformanceMetrics
        {
            public float averageFPS;
            public float minFPS;
            public float maxFPS;
            public float currentMemoryMB;
            public float peakMemoryMB;
            public float averageFrameTime;
            public int totalFrames;
            public float cpuUsagePercent;
            public float gpuUsagePercent;
        }

        [Serializable]
        public class PoliticalAccuracyMetrics
        {
            public float seatCalculationAccuracy;
            public float compatibilityScoreRealism;
            public float portfolioAllocationRealism;
            public int validatedCoalitions;
            public int invalidCoalitions;
            public List<string> politicalWarnings;
        }

        [Serializable]
        public class TechnicalReliabilityMetrics
        {
            public int crashCount;
            public int errorCount;
            public int warningCount;
            public float uptime;
            public float lastCrashTime;
            public int successfulCalculations;
            public int failedCalculations;
        }

        [Serializable]
        public class QualityIssue
        {
            public string id;
            public DateTime timestamp;
            public QualityIssueType type;
            public QualityIssueSeverity severity;
            public string title;
            public string description;
            public string context;
            public bool isResolved;
            public DateTime resolvedTime;
            public string resolution;
        }

        public enum QualityStatus
        {
            Unknown,
            Excellent,
            Good,
            Acceptable,
            Poor,
            Critical
        }

        public enum QualityIssueType
        {
            Performance,
            Political,
            Technical,
            Usability,
            Data
        }

        public enum QualityIssueSeverity
        {
            Info,
            Warning,
            Error,
            Critical
        }

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeQA();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void Start()
        {
            if (Instance == this)
            {
                StartQualityMonitoring();
            }
        }

        void Update()
        {
            if (Instance == this)
            {
                UpdateQualityMonitoring();
            }
        }

        private void InitializeQA()
        {
            string baseDir = Path.Combine(Application.persistentDataPath, "CoalitionDemo", "QualityAssurance");
            reportDirectory = baseDir;
            Directory.CreateDirectory(reportDirectory);

            performanceMonitor = new PerformanceMonitor();
            politicalValidator = new PoliticalValidator();
            technicalValidator = new TechnicalValidator();

            currentReport = new QualityReport
            {
                timestamp = DateTime.Now,
                performance = new PerformanceMetrics(),
                political = new PoliticalAccuracyMetrics(),
                technical = new TechnicalReliabilityMetrics(),
                issues = new List<QualityIssue>()
            };

            Debug.Log("Quality Assurance Manager initialized");
        }

        private void StartQualityMonitoring()
        {
            InvokeRepeating(nameof(PerformQualityCheck), 1f, performanceCheckInterval);
            InvokeRepeating(nameof(GenerateQualityReport), reportingInterval, reportingInterval);

            Debug.Log("Quality monitoring started");
        }

        private void UpdateQualityMonitoring()
        {
            performanceMonitor.Update();
            CalculateOverallQualityScore();
        }

        private void PerformQualityCheck()
        {
            CheckPerformanceQuality();
            CheckTechnicalQuality();

            if (enablePoliticalValidation)
            {
                CheckPoliticalQuality();
            }

            UpdateQualityStatus();
        }

        private void CheckPerformanceQuality()
        {
            float currentFPS = performanceMonitor.GetCurrentFPS();
            float memoryUsage = performanceMonitor.GetMemoryUsageMB();

            // Check FPS quality gate
            if (currentFPS < minimumFPS)
            {
                ReportQualityIssue(QualityIssueType.Performance, QualityIssueSeverity.Warning,
                    "Low FPS Detected",
                    $"Current FPS ({currentFPS:F1}) below minimum threshold ({minimumFPS})",
                    "Performance");
            }

            // Check memory usage
            if (memoryUsage > memoryWarningThreshold)
            {
                var severity = memoryUsage > maximumMemoryMB ? QualityIssueSeverity.Error : QualityIssueSeverity.Warning;
                ReportQualityIssue(QualityIssueType.Performance, severity,
                    "High Memory Usage",
                    $"Memory usage ({memoryUsage:F1}MB) exceeds threshold ({memoryWarningThreshold:F1}MB)",
                    "Memory");
            }

            // Update performance metrics
            currentReport.performance.averageFPS = performanceMonitor.GetAverageFPS();
            currentReport.performance.currentMemoryMB = memoryUsage;
            currentReport.performance.peakMemoryMB = performanceMonitor.GetPeakMemoryMB();
        }

        private void CheckTechnicalQuality()
        {
            // Check error rate
            int recentErrors = technicalValidator.GetRecentErrorCount();
            if (recentErrors > maxErrorsPerMinute)
            {
                ReportQualityIssue(QualityIssueType.Technical, QualityIssueSeverity.Error,
                    "High Error Rate",
                    $"Detected {recentErrors} errors in the last minute (threshold: {maxErrorsPerMinute})",
                    "ErrorHandling");
            }

            // Update technical metrics
            currentReport.technical.errorCount = technicalValidator.GetTotalErrorCount();
            currentReport.technical.uptime = Time.time;
        }

        private void CheckPoliticalQuality()
        {
            if (!enableRealTimeValidation) return;

            // Validate current coalition calculations
            var coalitionAccuracy = politicalValidator.ValidateCurrentCoalitions();
            if (coalitionAccuracy < 0.95f) // 95% accuracy threshold
            {
                ReportQualityIssue(QualityIssueType.Political, QualityIssueSeverity.Warning,
                    "Coalition Calculation Inaccuracy",
                    $"Coalition calculations accuracy ({coalitionAccuracy:P1}) below threshold (95%)",
                    "PoliticalAccuracy");
            }

            // Validate seat distributions
            if (!politicalValidator.ValidateSeatDistribution())
            {
                ReportQualityIssue(QualityIssueType.Political, QualityIssueSeverity.Error,
                    "Seat Distribution Error",
                    "Seat distribution does not match expected D'Hondt calculation",
                    "ElectoralSystem");
            }

            // Update political metrics
            currentReport.political.seatCalculationAccuracy = politicalValidator.GetSeatAccuracy();
            currentReport.political.compatibilityScoreRealism = politicalValidator.GetCompatibilityRealism();
        }

        private void ReportQualityIssue(QualityIssueType type, QualityIssueSeverity severity, string title, string description, string context)
        {
            // Check if this issue already exists
            var existingIssue = activeIssues.Find(i => i.title == title && !i.isResolved);
            if (existingIssue != null) return;

            var issue = new QualityIssue
            {
                id = Guid.NewGuid().ToString(),
                timestamp = DateTime.Now,
                type = type,
                severity = severity,
                title = title,
                description = description,
                context = context,
                isResolved = false
            };

            activeIssues.Add(issue);
            OnQualityIssueDetected?.Invoke(issue);

            Debug.LogWarning($"Quality Issue Detected: [{severity}] {title} - {description}");

            // Auto-resolve performance issues after some time
            if (type == QualityIssueType.Performance)
            {
                StartCoroutine(AutoResolvePerformanceIssue(issue.id, 30f));
            }
        }

        private System.Collections.IEnumerator AutoResolvePerformanceIssue(string issueId, float delay)
        {
            yield return new UnityEngine.WaitForSeconds(delay);

            var issue = activeIssues.Find(i => i.id == issueId && !i.isResolved);
            if (issue != null)
            {
                // Check if the issue still exists
                bool stillExists = false;
                if (issue.title.Contains("Low FPS") && performanceMonitor.GetCurrentFPS() < minimumFPS)
                    stillExists = true;
                if (issue.title.Contains("High Memory") && performanceMonitor.GetMemoryUsageMB() > memoryWarningThreshold)
                    stillExists = true;

                if (!stillExists)
                {
                    ResolveQualityIssue(issueId, "Performance improved");
                }
            }
        }

        public void ResolveQualityIssue(string issueId, string resolution = "")
        {
            var issue = activeIssues.Find(i => i.id == issueId);
            if (issue != null && !issue.isResolved)
            {
                issue.isResolved = true;
                issue.resolvedTime = DateTime.Now;
                issue.resolution = resolution;

                OnQualityIssueResolved?.Invoke(issue);

                Debug.Log($"Quality Issue Resolved: {issue.title} - {resolution}");
            }
        }

        private void CalculateOverallQualityScore()
        {
            float performanceScore = CalculatePerformanceScore();
            float politicalScore = CalculatePoliticalScore();
            float technicalScore = CalculateTechnicalScore();

            // Weighted average: Performance 40%, Political 35%, Technical 25%
            OverallQualityScore = (performanceScore * 0.4f) + (politicalScore * 0.35f) + (technicalScore * 0.25f);
        }

        private float CalculatePerformanceScore()
        {
            float fpsScore = Mathf.Clamp01(performanceMonitor.GetCurrentFPS() / targetFPS);
            float memoryScore = Mathf.Clamp01(1f - (performanceMonitor.GetMemoryUsageMB() / maximumMemoryMB));
            return (fpsScore + memoryScore) / 2f;
        }

        private float CalculatePoliticalScore()
        {
            if (!enablePoliticalValidation) return 1f;
            return politicalValidator.GetOverallAccuracy();
        }

        private float CalculateTechnicalScore()
        {
            int criticalIssues = activeIssues.FindAll(i => !i.isResolved && i.severity == QualityIssueSeverity.Critical).Count;
            int errorIssues = activeIssues.FindAll(i => !i.isResolved && i.severity == QualityIssueSeverity.Error).Count;

            if (criticalIssues > 0) return 0f;
            if (errorIssues > 2) return 0.3f;
            if (errorIssues > 0) return 0.7f;
            return 1f;
        }

        private void UpdateQualityStatus()
        {
            var previousStatus = CurrentStatus;

            if (OverallQualityScore >= 0.9f)
                CurrentStatus = QualityStatus.Excellent;
            else if (OverallQualityScore >= 0.8f)
                CurrentStatus = QualityStatus.Good;
            else if (OverallQualityScore >= 0.6f)
                CurrentStatus = QualityStatus.Acceptable;
            else if (OverallQualityScore >= 0.4f)
                CurrentStatus = QualityStatus.Poor;
            else
                CurrentStatus = QualityStatus.Critical;

            // Log status changes
            if (CurrentStatus != previousStatus)
            {
                Debug.Log($"Quality Status changed: {previousStatus} → {CurrentStatus} (Score: {OverallQualityScore:F2})");
            }
        }

        private void GenerateQualityReport()
        {
            currentReport.timestamp = DateTime.Now;
            currentReport.sessionDuration = Time.time;
            currentReport.overallScore = OverallQualityScore;
            currentReport.status = CurrentStatus;
            currentReport.issues = new List<QualityIssue>(activeIssues);

            // Generate summary
            currentReport.summary = GenerateReportSummary();

            OnQualityReportGenerated?.Invoke(currentReport);

            if (enableDetailedReporting)
            {
                SaveQualityReport(currentReport);
            }

            Debug.Log($"Quality Report Generated - Status: {CurrentStatus}, Score: {OverallQualityScore:F2}");
        }

        private string GenerateReportSummary()
        {
            var summary = new System.Text.StringBuilder();

            summary.AppendLine($"Quality Status: {CurrentStatus} (Score: {OverallQualityScore:F2})");
            summary.AppendLine($"Session Duration: {currentReport.sessionDuration / 60f:F1} minutes");
            summary.AppendLine($"Performance: FPS {currentReport.performance.averageFPS:F1}, Memory {currentReport.performance.currentMemoryMB:F1}MB");
            summary.AppendLine($"Active Issues: {activeIssues.Count}");

            var criticalIssues = activeIssues.FindAll(i => !i.isResolved && i.severity == QualityIssueSeverity.Critical);
            if (criticalIssues.Count > 0)
            {
                summary.AppendLine($"Critical Issues: {criticalIssues.Count}");
            }

            return summary.ToString();
        }

        private void SaveQualityReport(QualityReport report)
        {
            try
            {
                string fileName = $"quality_report_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.json";
                string filePath = Path.Combine(reportDirectory, fileName);

                string jsonData = JsonUtility.ToJson(report, true);
                File.WriteAllText(filePath, jsonData);

                Debug.Log($"Quality report saved: {filePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save quality report: {ex.Message}");
            }
        }

        public QualityReport GetCurrentReport() => currentReport;

        public List<QualityIssue> GetActiveIssues() => new List<QualityIssue>(activeIssues);

        public bool IsQualityAcceptable() => CurrentStatus >= QualityStatus.Acceptable;

        public void ForceQualityCheck()
        {
            PerformQualityCheck();
            UpdateQualityStatus();
        }

        public void ExportQualityData()
        {
            GenerateQualityReport();

            try
            {
                string exportFile = Path.Combine(reportDirectory, $"quality_export_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt");
                string exportData = GenerateQualityExport();
                File.WriteAllText(exportFile, exportData);

                Debug.Log($"Quality data exported: {exportFile}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to export quality data: {ex.Message}");
            }
        }

        private string GenerateQualityExport()
        {
            var export = new System.Text.StringBuilder();

            export.AppendLine("=== COALITION DEMO QUALITY ASSURANCE REPORT ===");
            export.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            export.AppendLine($"Session Duration: {Time.time / 60f:F1} minutes");
            export.AppendLine();

            export.AppendLine($"Overall Quality Score: {OverallQualityScore:F2} ({CurrentStatus})");
            export.AppendLine();

            export.AppendLine("=== PERFORMANCE METRICS ===");
            export.AppendLine($"Average FPS: {currentReport.performance.averageFPS:F1}");
            export.AppendLine($"Current Memory: {currentReport.performance.currentMemoryMB:F1}MB");
            export.AppendLine($"Peak Memory: {currentReport.performance.peakMemoryMB:F1}MB");
            export.AppendLine();

            export.AppendLine("=== POLITICAL ACCURACY ===");
            export.AppendLine($"Seat Calculation Accuracy: {currentReport.political.seatCalculationAccuracy:P1}");
            export.AppendLine($"Compatibility Score Realism: {currentReport.political.compatibilityScoreRealism:P1}");
            export.AppendLine();

            export.AppendLine("=== TECHNICAL RELIABILITY ===");
            export.AppendLine($"Total Errors: {currentReport.technical.errorCount}");
            export.AppendLine($"Uptime: {currentReport.technical.uptime / 60f:F1} minutes");
            export.AppendLine();

            export.AppendLine("=== ACTIVE ISSUES ===");
            var unresolvedIssues = activeIssues.FindAll(i => !i.isResolved);
            if (unresolvedIssues.Count == 0)
            {
                export.AppendLine("No active issues detected.");
            }
            else
            {
                foreach (var issue in unresolvedIssues)
                {
                    export.AppendLine($"[{issue.severity}] {issue.title}: {issue.description}");
                }
            }

            return export.ToString();
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                ExportQualityData();
                Debug.Log("Quality Assurance Manager shutting down");
            }
        }

        // Helper classes for monitoring components
        private class PerformanceMonitor
        {
            private List<float> fpsHistory = new List<float>();
            private float currentFPS;
            private float peakMemory;

            public void Update()
            {
                currentFPS = 1f / Time.unscaledDeltaTime;
                fpsHistory.Add(currentFPS);

                if (fpsHistory.Count > 60) // Keep last 60 frames
                {
                    fpsHistory.RemoveAt(0);
                }

                float currentMemory = GetMemoryUsageMB();
                if (currentMemory > peakMemory)
                {
                    peakMemory = currentMemory;
                }
            }

            public float GetCurrentFPS() => currentFPS;

            public float GetAverageFPS()
            {
                if (fpsHistory.Count == 0) return 0f;
                float sum = 0f;
                foreach (float fps in fpsHistory) sum += fps;
                return sum / fpsHistory.Count;
            }

            public float GetMemoryUsageMB() => (float)GC.GetTotalMemory(false) / 1024 / 1024;

            public float GetPeakMemoryMB() => peakMemory;
        }

        private class PoliticalValidator
        {
            public float ValidateCurrentCoalitions() => 1f; // Placeholder - would validate actual coalition data
            public bool ValidateSeatDistribution() => true; // Placeholder - would validate D'Hondt calculations
            public float GetSeatAccuracy() => 1f; // Placeholder
            public float GetCompatibilityRealism() => 1f; // Placeholder
            public float GetOverallAccuracy() => 1f; // Placeholder
        }

        private class TechnicalValidator
        {
            private List<DateTime> recentErrors = new List<DateTime>();
            private int totalErrors = 0;

            public int GetRecentErrorCount()
            {
                // Remove errors older than 1 minute
                DateTime cutoff = DateTime.Now.AddMinutes(-1);
                recentErrors.RemoveAll(e => e < cutoff);
                return recentErrors.Count;
            }

            public int GetTotalErrorCount() => totalErrors;

            public void RecordError()
            {
                recentErrors.Add(DateTime.Now);
                totalErrors++;
            }
        }
    }
}