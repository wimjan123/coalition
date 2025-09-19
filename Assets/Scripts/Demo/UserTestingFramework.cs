using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Coalition.Demo
{
    /// <summary>
    /// Comprehensive framework for gathering user feedback during demo testing sessions
    /// </summary>
    public class UserTestingFramework : MonoBehaviour
    {
        [Header("Session Configuration")]
        [SerializeField] private bool enableSessionRecording = true;
        [SerializeField] private bool enableBehaviorTracking = true;
        [SerializeField] private bool enableInAppFeedback = true;
        [SerializeField] private float maxSessionDuration = 3600f; // 1 hour

        [Header("Feedback Collection")]
        [SerializeField] private bool autoSaveInterval = true;
        [SerializeField] private float saveIntervalSeconds = 60f;

        private UserSession currentSession;
        private List<UserAction> sessionActions = new List<UserAction>();
        private List<FeedbackEntry> feedbackEntries = new List<FeedbackEntry>();
        private string sessionDirectory;
        private float sessionStartTime;
        private int participantId;

        public static UserTestingFramework Instance { get; private set; }

        // Events for external systems to hook into
        public event Action<UserAction> OnUserActionRecorded;
        public event Action<FeedbackEntry> OnFeedbackReceived;
        public event Action<UserSession> OnSessionStarted;
        public event Action<UserSession> OnSessionEnded;

        public UserSession CurrentSession => currentSession;
        public bool IsSessionActive => currentSession != null;
        public float SessionDuration => IsSessionActive ? Time.time - sessionStartTime : 0f;

        [Serializable]
        public class UserSession
        {
            public int participantId;
            public DateTime startTime;
            public DateTime endTime;
            public string platform;
            public string buildVersion;
            public UserProfile userProfile;
            public List<UserAction> actions = new List<UserAction>();
            public List<FeedbackEntry> feedback = new List<FeedbackEntry>();
            public SessionMetrics metrics = new SessionMetrics();
            public bool completedSuccessfully;
            public string sessionNotes;
        }

        [Serializable]
        public class UserProfile
        {
            public int age;
            public string politicalKnowledge; // "Beginner", "Intermediate", "Expert"
            public string politicalPreference; // "Left", "Center", "Right", "Mixed"
            public string occupation;
            public bool hasPlayedPoliticalGames;
            public int techComfortLevel; // 1-10 scale
            public string primaryLanguage;
            public string region; // Dutch region
        }

        [Serializable]
        public class UserAction
        {
            public float timestamp;
            public string actionType; // "Click", "Drag", "Navigate", "Form", "Scenario"
            public string targetObject;
            public Vector2 screenPosition;
            public string scenarioContext;
            public Dictionary<string, object> parameters = new Dictionary<string, object>();
            public float responseTime; // Time from UI prompt to action
        }

        [Serializable]
        public class FeedbackEntry
        {
            public float timestamp;
            public string feedbackType; // "Rating", "OpenText", "MultipleChoice", "Task"
            public string question;
            public string response;
            public int rating; // 1-10 scale where applicable
            public string category; // "Authenticity", "Usability", "Performance", "Content"
            public bool isPositive;
            public string context; // What they were doing when feedback was given
        }

        [Serializable]
        public class SessionMetrics
        {
            public int totalActions;
            public int errorsEncountered;
            public int scenariosCompleted;
            public int scenariosAttempted;
            public float averageResponseTime;
            public float timeOnAuthenticityRating;
            public float timeOnCoalitionBuilding;
            public float timeOnAnalysisTools;
            public bool exploredAdvancedFeatures;
            public int helpRequestsCount;
            public float taskCompletionRate;
        }

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeFramework();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void Update()
        {
            if (IsSessionActive)
            {
                // Auto-save session data
                if (autoSaveInterval && Time.time % saveIntervalSeconds < Time.deltaTime)
                {
                    SaveSessionData();
                }

                // Check for session timeout
                if (SessionDuration > maxSessionDuration)
                {
                    EndSession("Session timeout");
                }
            }
        }

        private void InitializeFramework()
        {
            string baseDir = Path.Combine(Application.persistentDataPath, "CoalitionDemo", "UserTesting");
            sessionDirectory = baseDir;
            Directory.CreateDirectory(sessionDirectory);

            Debug.Log("User Testing Framework initialized");
        }

        public void StartSession(int participantId, UserProfile userProfile = null)
        {
            if (IsSessionActive)
            {
                EndSession("Previous session terminated");
            }

            this.participantId = participantId;
            sessionStartTime = Time.time;

            currentSession = new UserSession
            {
                participantId = participantId,
                startTime = DateTime.Now,
                platform = Application.platform.ToString(),
                buildVersion = DemoBuildManager.Instance?.BuildVersion ?? "Unknown",
                userProfile = userProfile ?? new UserProfile(),
                sessionNotes = ""
            };

            sessionActions.Clear();
            feedbackEntries.Clear();

            OnSessionStarted?.Invoke(currentSession);

            RecordAction("SessionStart", "UserTestingFramework", Vector2.zero, "Session initiated");

            Debug.Log($"User testing session started for participant {participantId}");
        }

        public void EndSession(string reason = "Normal completion")
        {
            if (!IsSessionActive) return;

            currentSession.endTime = DateTime.Now;
            currentSession.actions = new List<UserAction>(sessionActions);
            currentSession.feedback = new List<FeedbackEntry>(feedbackEntries);
            currentSession.completedSuccessfully = reason == "Normal completion";
            currentSession.sessionNotes += $"\nSession ended: {reason}";

            CalculateSessionMetrics();
            SaveSessionData(isFinal: true);

            OnSessionEnded?.Invoke(currentSession);

            Debug.Log($"User testing session ended for participant {participantId}: {reason}");

            currentSession = null;
        }

        public void RecordAction(string actionType, string targetObject, Vector2 screenPosition, string context = "", float responseTime = 0f)
        {
            if (!IsSessionActive) return;

            var action = new UserAction
            {
                timestamp = Time.time - sessionStartTime,
                actionType = actionType,
                targetObject = targetObject,
                screenPosition = screenPosition,
                scenarioContext = context,
                responseTime = responseTime
            };

            sessionActions.Add(action);
            OnUserActionRecorded?.Invoke(action);
        }

        public void RecordFeedback(string feedbackType, string question, string response, int rating = 0, string category = "General")
        {
            if (!IsSessionActive) return;

            var feedback = new FeedbackEntry
            {
                timestamp = Time.time - sessionStartTime,
                feedbackType = feedbackType,
                question = question,
                response = response,
                rating = rating,
                category = category,
                isPositive = rating >= 6 || response.ToLower().Contains("good") || response.ToLower().Contains("like"),
                context = GetCurrentContext()
            };

            feedbackEntries.Add(feedback);
            OnFeedbackReceived?.Invoke(feedback);
        }

        public void RecordTaskCompletion(string taskName, bool completed, float timeToComplete)
        {
            if (!IsSessionActive) return;

            RecordAction("TaskCompletion", taskName, Vector2.zero, $"Completed: {completed}, Time: {timeToComplete:F1}s");

            var feedback = new FeedbackEntry
            {
                timestamp = Time.time - sessionStartTime,
                feedbackType = "TaskCompletion",
                question = $"Task: {taskName}",
                response = completed ? "Completed" : "Failed",
                rating = completed ? 10 : 0,
                category = "Task",
                isPositive = completed,
                context = $"Time to complete: {timeToComplete:F1}s"
            };

            feedbackEntries.Add(feedback);
        }

        public void ShowFeedbackForm(string title, List<FeedbackQuestion> questions, Action<List<FeedbackEntry>> onComplete = null)
        {
            if (!enableInAppFeedback) return;

            // This would typically show a UI panel with the feedback form
            // For now, we'll create a simple implementation
            StartCoroutine(ShowFeedbackFormCoroutine(title, questions, onComplete));
        }

        private System.Collections.IEnumerator ShowFeedbackFormCoroutine(string title, List<FeedbackQuestion> questions, Action<List<FeedbackEntry>> onComplete)
        {
            RecordAction("FeedbackFormShown", "FeedbackUI", Vector2.zero, title);

            // Simulate form display time
            yield return new UnityEngine.WaitForSeconds(1f);

            var responses = new List<FeedbackEntry>();

            foreach (var question in questions)
            {
                // In a real implementation, this would wait for user input
                // For now, we'll record the question as shown
                RecordFeedback("FormQuestion", question.text, "Pending", 0, question.category);
            }

            onComplete?.Invoke(responses);
        }

        public void RecordError(string errorType, string errorMessage, string context = "")
        {
            if (!IsSessionActive) return;

            RecordAction("Error", errorType, Vector2.zero, $"{errorMessage} | Context: {context}");

            var feedback = new FeedbackEntry
            {
                timestamp = Time.time - sessionStartTime,
                feedbackType = "Error",
                question = "Error encountered",
                response = errorMessage,
                rating = 1,
                category = "Technical",
                isPositive = false,
                context = context
            };

            feedbackEntries.Add(feedback);
        }

        private void CalculateSessionMetrics()
        {
            if (!IsSessionActive) return;

            var metrics = currentSession.metrics;

            metrics.totalActions = sessionActions.Count;
            metrics.errorsEncountered = sessionActions.FindAll(a => a.actionType == "Error").Count;

            // Calculate scenarios
            var scenarioActions = sessionActions.FindAll(a => a.actionType == "TaskCompletion");
            metrics.scenariosAttempted = scenarioActions.Count;
            metrics.scenariosCompleted = scenarioActions.FindAll(a => a.parameters.ContainsKey("Completed") && (bool)a.parameters["Completed"]).Count;

            // Calculate response times
            var timedActions = sessionActions.FindAll(a => a.responseTime > 0);
            if (timedActions.Count > 0)
            {
                float totalTime = 0f;
                foreach (var action in timedActions)
                {
                    totalTime += action.responseTime;
                }
                metrics.averageResponseTime = totalTime / timedActions.Count;
            }

            // Calculate time spent in different areas
            metrics.timeOnCoalitionBuilding = CalculateTimeInContext("CoalitionBuilder");
            metrics.timeOnAnalysisTools = CalculateTimeInContext("Analysis");
            metrics.timeOnAuthenticityRating = CalculateTimeInContext("AuthenticityRating");

            // Check advanced feature usage
            metrics.exploredAdvancedFeatures = sessionActions.Exists(a =>
                a.targetObject.Contains("Analysis") ||
                a.targetObject.Contains("Advanced") ||
                a.actionType == "AdvancedFeature");

            // Help requests
            metrics.helpRequestsCount = sessionActions.FindAll(a => a.actionType == "Help").Count;

            // Task completion rate
            metrics.taskCompletionRate = metrics.scenariosAttempted > 0 ?
                (float)metrics.scenariosCompleted / metrics.scenariosAttempted : 0f;
        }

        private float CalculateTimeInContext(string context)
        {
            var contextActions = sessionActions.FindAll(a => a.scenarioContext.Contains(context));
            if (contextActions.Count < 2) return 0f;

            float startTime = contextActions[0].timestamp;
            float endTime = contextActions[contextActions.Count - 1].timestamp;
            return endTime - startTime;
        }

        private string GetCurrentContext()
        {
            // This would typically get the current UI state or active window
            return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        }

        private void SaveSessionData(bool isFinal = false)
        {
            if (!IsSessionActive) return;

            try
            {
                string fileName = $"session_{participantId}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
                if (isFinal) fileName += "_final";
                fileName += ".json";

                string filePath = Path.Combine(sessionDirectory, fileName);

                // Update current session data
                currentSession.actions = new List<UserAction>(sessionActions);
                currentSession.feedback = new List<FeedbackEntry>(feedbackEntries);

                if (isFinal)
                {
                    CalculateSessionMetrics();
                }

                string jsonData = JsonUtility.ToJson(currentSession, true);
                File.WriteAllText(filePath, jsonData);

                Debug.Log($"Session data saved: {filePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save session data: {ex.Message}");
            }
        }

        public List<UserSession> LoadAllSessions()
        {
            var sessions = new List<UserSession>();

            try
            {
                string[] files = Directory.GetFiles(sessionDirectory, "*_final.json");

                foreach (string file in files)
                {
                    string jsonData = File.ReadAllText(file);
                    UserSession session = JsonUtility.FromJson<UserSession>(jsonData);
                    sessions.Add(session);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load sessions: {ex.Message}");
            }

            return sessions;
        }

        public void ExportSessionSummary(int participantId)
        {
            var sessions = LoadAllSessions();
            var participantSessions = sessions.FindAll(s => s.participantId == participantId);

            if (participantSessions.Count == 0)
            {
                Debug.LogWarning($"No sessions found for participant {participantId}");
                return;
            }

            try
            {
                string summaryFile = Path.Combine(sessionDirectory, $"participant_{participantId}_summary.txt");
                string summary = GenerateParticipantSummary(participantSessions);
                File.WriteAllText(summaryFile, summary);

                Debug.Log($"Participant summary exported: {summaryFile}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to export participant summary: {ex.Message}");
            }
        }

        private string GenerateParticipantSummary(List<UserSession> sessions)
        {
            var summary = new System.Text.StringBuilder();

            summary.AppendLine("=== COALITION DEMO - PARTICIPANT SUMMARY ===");
            summary.AppendLine($"Participant ID: {sessions[0].participantId}");
            summary.AppendLine($"Total Sessions: {sessions.Count}");
            summary.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            summary.AppendLine();

            foreach (var session in sessions)
            {
                summary.AppendLine($"--- Session: {session.startTime:yyyy-MM-dd HH:mm:ss} ---");
                summary.AppendLine($"Duration: {(session.endTime - session.startTime).TotalMinutes:F1} minutes");
                summary.AppendLine($"Completed: {session.completedSuccessfully}");
                summary.AppendLine($"Platform: {session.platform}");
                summary.AppendLine();

                summary.AppendLine("User Profile:");
                summary.AppendLine($"  Political Knowledge: {session.userProfile.politicalKnowledge}");
                summary.AppendLine($"  Political Preference: {session.userProfile.politicalPreference}");
                summary.AppendLine($"  Tech Comfort: {session.userProfile.techComfortLevel}/10");
                summary.AppendLine($"  Has Played Political Games: {session.userProfile.hasPlayedPoliticalGames}");
                summary.AppendLine();

                summary.AppendLine("Session Metrics:");
                summary.AppendLine($"  Total Actions: {session.metrics.totalActions}");
                summary.AppendLine($"  Errors: {session.metrics.errorsEncountered}");
                summary.AppendLine($"  Scenarios Completed: {session.metrics.scenariosCompleted}/{session.metrics.scenariosAttempted}");
                summary.AppendLine($"  Task Completion Rate: {session.metrics.taskCompletionRate:P1}");
                summary.AppendLine($"  Average Response Time: {session.metrics.averageResponseTime:F2}s");
                summary.AppendLine($"  Used Advanced Features: {session.metrics.exploredAdvancedFeatures}");
                summary.AppendLine();

                // Feedback summary
                var positiveFeedback = session.feedback.FindAll(f => f.isPositive).Count;
                var totalFeedback = session.feedback.Count;
                summary.AppendLine($"Feedback: {positiveFeedback}/{totalFeedback} positive responses");

                // Authenticity ratings
                var authenticityFeedback = session.feedback.FindAll(f => f.category == "Authenticity");
                if (authenticityFeedback.Count > 0)
                {
                    float avgRating = 0f;
                    foreach (var feedback in authenticityFeedback)
                    {
                        avgRating += feedback.rating;
                    }
                    avgRating /= authenticityFeedback.Count;
                    summary.AppendLine($"Average Authenticity Rating: {avgRating:F1}/10");
                }

                summary.AppendLine();
            }

            return summary.ToString();
        }

        [Serializable]
        public class FeedbackQuestion
        {
            public string text;
            public string type; // "Rating", "Text", "MultipleChoice"
            public string category;
            public List<string> options; // For multiple choice
            public int minRating = 1;
            public int maxRating = 10;
        }

        void OnApplicationPause(bool pauseStatus)
        {
            if (IsSessionActive)
            {
                RecordAction("ApplicationPause", "System", Vector2.zero, $"Paused: {pauseStatus}");
            }
        }

        void OnDestroy()
        {
            if (IsSessionActive)
            {
                EndSession("Application shutdown");
            }
        }
    }
}