using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Coalition.Demo
{
    /// <summary>
    /// Manages demo build configuration, error logging, and crash reporting for user testing
    /// </summary>
    public class DemoBuildManager : MonoBehaviour
    {
        [Header("Build Configuration")]
        [SerializeField] private bool enableDebugMode = false;
        [SerializeField] private bool enableCrashReporting = true;
        [SerializeField] private bool enableUserAnalytics = true;
        [SerializeField] private string buildVersion = "1.0.0-demo";

        [Header("Performance Monitoring")]
        [SerializeField] private float targetFrameRate = 60f;
        [SerializeField] private bool enablePerformanceLogging = true;
        [SerializeField] private float performanceLogInterval = 5f;

        [Header("Error Logging")]
        [SerializeField] private bool enableDetailedLogging = true;
        [SerializeField] private bool logToFile = true;
        [SerializeField] private int maxLogFileSize = 10; // MB
        [SerializeField] private int maxLogFiles = 5;

        private string logDirectory;
        private string crashDirectory;
        private Queue<string> logBuffer = new Queue<string>();
        private float lastPerformanceLog = 0f;
        private bool isInitialized = false;

        public static DemoBuildManager Instance { get; private set; }

        // Build information
        public string BuildVersion => buildVersion;
        public bool IsDebugBuild => enableDebugMode;
        public DateTime BuildTime { get; private set; }
        public string Platform { get; private set; }

        // Performance metrics
        public float CurrentFPS { get; private set; }
        public float MemoryUsageMB { get; private set; }
        public int CrashCount { get; private set; }
        public int ErrorCount { get; private set; }

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeBuildManager();
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
                SetupLogging();
                SetupPerformanceMonitoring();
                LogSystemInfo();
            }
        }

        void Update()
        {
            if (!isInitialized) return;

            UpdatePerformanceMetrics();

            if (enablePerformanceLogging && Time.time - lastPerformanceLog > performanceLogInterval)
            {
                LogPerformanceMetrics();
                lastPerformanceLog = Time.time;
            }
        }

        private void InitializeBuildManager()
        {
            BuildTime = DateTime.Now;
            Platform = Application.platform.ToString();

            // Setup directories
            string baseDir = Path.Combine(Application.persistentDataPath, "CoalitionDemo");
            logDirectory = Path.Combine(baseDir, "Logs");
            crashDirectory = Path.Combine(baseDir, "Crashes");

            Directory.CreateDirectory(logDirectory);
            Directory.CreateDirectory(crashDirectory);

            // Set target frame rate
            Application.targetFrameRate = (int)targetFrameRate;

            isInitialized = true;

            LogInfo($"Coalition Demo Build Manager initialized - Version: {buildVersion}");
        }

        private void SetupLogging()
        {
            if (enableDetailedLogging)
            {
                Application.logMessageReceived += OnLogMessageReceived;
            }
        }

        private void SetupPerformanceMonitoring()
        {
            if (enablePerformanceLogging)
            {
                LogInfo("Performance monitoring enabled");
            }
        }

        private void OnLogMessageReceived(string logString, string stackTrace, LogType type)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string formattedLog = $"[{timestamp}] [{type}] {logString}";

            if (type == LogType.Error || type == LogType.Exception)
            {
                ErrorCount++;
                formattedLog += $"\nStackTrace: {stackTrace}";

                if (type == LogType.Exception)
                {
                    HandleCrash(logString, stackTrace);
                }
            }

            if (logToFile)
            {
                WriteToLogFile(formattedLog);
            }

            // Keep in memory buffer for immediate access
            logBuffer.Enqueue(formattedLog);
            if (logBuffer.Count > 1000) // Limit memory usage
            {
                logBuffer.Dequeue();
            }
        }

        private void WriteToLogFile(string message)
        {
            try
            {
                string logFile = Path.Combine(logDirectory, $"demo_log_{DateTime.Now:yyyy-MM-dd}.txt");

                // Check file size and rotate if necessary
                if (File.Exists(logFile))
                {
                    FileInfo fileInfo = new FileInfo(logFile);
                    if (fileInfo.Length > maxLogFileSize * 1024 * 1024)
                    {
                        RotateLogFiles();
                        logFile = Path.Combine(logDirectory, $"demo_log_{DateTime.Now:yyyy-MM-dd}.txt");
                    }
                }

                File.AppendAllText(logFile, message + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to write to log file: {ex.Message}");
            }
        }

        private void RotateLogFiles()
        {
            try
            {
                string[] logFiles = Directory.GetFiles(logDirectory, "demo_log_*.txt");
                Array.Sort(logFiles);

                // Remove old files if we exceed the limit
                if (logFiles.Length >= maxLogFiles)
                {
                    for (int i = 0; i < logFiles.Length - maxLogFiles + 1; i++)
                    {
                        File.Delete(logFiles[i]);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to rotate log files: {ex.Message}");
            }
        }

        private void HandleCrash(string error, string stackTrace)
        {
            CrashCount++;

            try
            {
                string crashFile = Path.Combine(crashDirectory, $"crash_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt");
                string crashReport = GenerateCrashReport(error, stackTrace);
                File.WriteAllText(crashFile, crashReport);

                LogError($"Crash detected and saved to: {crashFile}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save crash report: {ex.Message}");
            }
        }

        private string GenerateCrashReport(string error, string stackTrace)
        {
            var report = new System.Text.StringBuilder();

            report.AppendLine("=== COALITION DEMO CRASH REPORT ===");
            report.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"Build Version: {buildVersion}");
            report.AppendLine($"Platform: {Platform}");
            report.AppendLine($"Scene: {SceneManager.GetActiveScene().name}");
            report.AppendLine();

            report.AppendLine("=== SYSTEM INFO ===");
            report.AppendLine($"OS: {SystemInfo.operatingSystem}");
            report.AppendLine($"Device: {SystemInfo.deviceModel}");
            report.AppendLine($"Memory: {SystemInfo.systemMemorySize} MB");
            report.AppendLine($"Graphics: {SystemInfo.graphicsDeviceName}");
            report.AppendLine($"Graphics Memory: {SystemInfo.graphicsMemorySize} MB");
            report.AppendLine();

            report.AppendLine("=== PERFORMANCE METRICS ===");
            report.AppendLine($"Current FPS: {CurrentFPS:F1}");
            report.AppendLine($"Memory Usage: {MemoryUsageMB:F1} MB");
            report.AppendLine($"Previous Error Count: {ErrorCount - 1}");
            report.AppendLine();

            report.AppendLine("=== ERROR DETAILS ===");
            report.AppendLine($"Error: {error}");
            report.AppendLine();
            report.AppendLine("Stack Trace:");
            report.AppendLine(stackTrace);
            report.AppendLine();

            // Add recent log entries
            report.AppendLine("=== RECENT LOG ENTRIES ===");
            var recentLogs = new List<string>(logBuffer);
            int startIndex = Math.Max(0, recentLogs.Count - 20);
            for (int i = startIndex; i < recentLogs.Count; i++)
            {
                report.AppendLine(recentLogs[i]);
            }

            return report.ToString();
        }

        private void UpdatePerformanceMetrics()
        {
            CurrentFPS = 1.0f / Time.unscaledDeltaTime;
            MemoryUsageMB = (float)GC.GetTotalMemory(false) / 1024 / 1024;
        }

        private void LogPerformanceMetrics()
        {
            LogInfo($"Performance - FPS: {CurrentFPS:F1}, Memory: {MemoryUsageMB:F1}MB, Errors: {ErrorCount}");
        }

        private void LogSystemInfo()
        {
            LogInfo("=== SYSTEM INFORMATION ===");
            LogInfo($"Platform: {Platform}");
            LogInfo($"OS: {SystemInfo.operatingSystem}");
            LogInfo($"Device: {SystemInfo.deviceModel}");
            LogInfo($"Memory: {SystemInfo.systemMemorySize} MB");
            LogInfo($"Graphics: {SystemInfo.graphicsDeviceName}");
            LogInfo($"Graphics Memory: {SystemInfo.graphicsMemorySize} MB");
            LogInfo($"Unity Version: {Application.unityVersion}");
            LogInfo("=== END SYSTEM INFORMATION ===");
        }

        public void LogInfo(string message)
        {
            Debug.Log($"[DemoBuild] {message}");
        }

        public void LogWarning(string message)
        {
            Debug.LogWarning($"[DemoBuild] {message}");
        }

        public void LogError(string message)
        {
            Debug.LogError($"[DemoBuild] {message}");
        }

        public List<string> GetRecentLogs(int count = 50)
        {
            var logs = new List<string>(logBuffer);
            int startIndex = Math.Max(0, logs.Count - count);
            return logs.GetRange(startIndex, logs.Count - startIndex);
        }

        public string GetLogDirectory()
        {
            return logDirectory;
        }

        public string GetCrashDirectory()
        {
            return crashDirectory;
        }

        public void ExportSystemReport()
        {
            try
            {
                string reportFile = Path.Combine(logDirectory, $"system_report_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt");
                string report = GenerateSystemReport();
                File.WriteAllText(reportFile, report);
                LogInfo($"System report exported to: {reportFile}");
            }
            catch (Exception ex)
            {
                LogError($"Failed to export system report: {ex.Message}");
            }
        }

        private string GenerateSystemReport()
        {
            var report = new System.Text.StringBuilder();

            report.AppendLine("=== COALITION DEMO SYSTEM REPORT ===");
            report.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"Build Version: {buildVersion}");
            report.AppendLine($"Platform: {Platform}");
            report.AppendLine($"Session Duration: {Time.time:F1} seconds");
            report.AppendLine();

            report.AppendLine("=== PERFORMANCE SUMMARY ===");
            report.AppendLine($"Average FPS: {CurrentFPS:F1}");
            report.AppendLine($"Memory Usage: {MemoryUsageMB:F1} MB");
            report.AppendLine($"Total Errors: {ErrorCount}");
            report.AppendLine($"Total Crashes: {CrashCount}");
            report.AppendLine();

            report.AppendLine("=== SYSTEM SPECIFICATIONS ===");
            report.AppendLine($"OS: {SystemInfo.operatingSystem}");
            report.AppendLine($"Device: {SystemInfo.deviceModel}");
            report.AppendLine($"Processor: {SystemInfo.processorType}");
            report.AppendLine($"Memory: {SystemInfo.systemMemorySize} MB");
            report.AppendLine($"Graphics: {SystemInfo.graphicsDeviceName}");
            report.AppendLine($"Graphics Memory: {SystemInfo.graphicsMemorySize} MB");
            report.AppendLine($"Unity Version: {Application.unityVersion}");

            return report.ToString();
        }

        void OnApplicationPause(bool pauseStatus)
        {
            LogInfo($"Application pause state changed: {pauseStatus}");
        }

        void OnApplicationFocus(bool hasFocus)
        {
            LogInfo($"Application focus state changed: {hasFocus}");
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                if (enableDetailedLogging)
                {
                    Application.logMessageReceived -= OnLogMessageReceived;
                }

                LogInfo("Demo Build Manager shutting down");
                ExportSystemReport();
            }
        }
    }
}