using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Coalition.Demo
{
    /// <summary>
    /// Manages scheduling and coordination of user testing sessions for the Coalition Demo
    /// Tracks participants, schedules, and testing progress
    /// </summary>
    public class UserTestingScheduler : MonoBehaviour
    {
        [Header("Session Configuration")]
        [SerializeField] private int targetParticipantCount = 15;
        [SerializeField] private float sessionDurationMinutes = 60f;
        [SerializeField] private bool enableRemoteTesting = true;

        [Header("Diversity Tracking")]
        [SerializeField] private bool enforceGeographicBalance = true;
        [SerializeField] private bool enforcePoliticalBalance = true;
        [SerializeField] private bool enforceExpertiseBalance = true;

        private List<TestingParticipant> participants = new List<TestingParticipant>();
        private List<TestingSession> scheduledSessions = new List<TestingSession>();
        private List<TestingSession> completedSessions = new List<TestingSession>();
        private string dataDirectory;

        public static UserTestingScheduler Instance { get; private set; }

        // Events
        public event Action<TestingParticipant> OnParticipantRegistered;
        public event Action<TestingSession> OnSessionScheduled;
        public event Action<TestingSession> OnSessionCompleted;
        public event Action<RecruitmentStatus> OnRecruitmentStatusUpdated;

        // Status Properties
        public int RegisteredParticipants => participants.Count;
        public int ScheduledSessions => scheduledSessions.Count;
        public int CompletedSessions => completedSessions.Count;
        public float RecruitmentProgress => (float)RegisteredParticipants / targetParticipantCount;
        public bool RecruitmentComplete => RegisteredParticipants >= targetParticipantCount;

        [Serializable]
        public class TestingParticipant
        {
            public string id;
            public string name;
            public string email;
            public ParticipantProfile profile;
            public ContactInformation contact;
            public AvailabilitySchedule availability;
            public DateTime registrationDate;
            public ParticipantStatus status;
            public string notes;
            public List<string> sessionIds = new List<string>();
        }

        [Serializable]
        public class ParticipantProfile
        {
            public int age;
            public string region; // Geographic region in Netherlands
            public string politicalKnowledge; // "Beginner", "Intermediate", "Expert"
            public string politicalPreference; // "Left", "Center-Left", "Center", "Center-Right", "Right"
            public string occupation; // "Student", "Journalist", "Academic", "Professional", "Party Member"
            public string institution; // University, media outlet, party, etc.
            public bool hasPlayedPoliticalGames;
            public int techComfortLevel; // 1-10 scale
            public string primaryLanguage; // "Dutch", "English"
            public List<string> specialties; // Areas of political expertise
            public string recruitmentSource; // How they heard about the study
        }

        [Serializable]
        public class ContactInformation
        {
            public string phone;
            public string alternateEmail;
            public string preferredContactMethod; // "Email", "Phone", "Text"
            public string timeZone;
            public bool acceptsReminders;
        }

        [Serializable]
        public class AvailabilitySchedule
        {
            public List<TimeSlot> availableSlots = new List<TimeSlot>();
            public List<string> preferredDays; // "Monday", "Tuesday", etc.
            public List<string> preferredTimes; // "Morning", "Afternoon", "Evening"
            public bool weekendsAvailable;
            public bool eveningsAvailable;
            public string timeZonePreference;
        }

        [Serializable]
        public class TimeSlot
        {
            public DateTime startTime;
            public DateTime endTime;
            public string location; // "Remote", "Amsterdam", etc.
            public bool isConfirmed;
        }

        [Serializable]
        public class TestingSession
        {
            public string sessionId;
            public string participantId;
            public DateTime scheduledTime;
            public float durationMinutes;
            public string location; // "Remote", physical address
            public string moderator;
            public SessionType type;
            public SessionStatus status;
            public List<string> objectives;
            public string specialInstructions;
            public SessionResults results;
            public DateTime actualStartTime;
            public DateTime actualEndTime;
            public string notes;
        }

        [Serializable]
        public class SessionResults
        {
            public float authenticityRating;
            public float usabilityRating;
            public float engagementLevel;
            public bool completedAllTasks;
            public List<string> keyFindings;
            public List<string> technicalIssues;
            public List<string> suggestions;
            public string participantFeedback;
        }

        [Serializable]
        public class RecruitmentStatus
        {
            public int totalRegistered;
            public int targetCount;
            public GeographicDistribution geographic;
            public PoliticalDistribution political;
            public ExpertiseDistribution expertise;
            public DemographicBalance demographics;
            public List<string> recruitmentGaps;
            public float completionPercentage;
        }

        [Serializable]
        public class GeographicDistribution
        {
            public int randstad; // Amsterdam, Rotterdam, The Hague, Utrecht
            public int north; // Groningen, Friesland, Drenthe
            public int east; // Overijssel, Gelderland
            public int south; // North Brabant, Limburg
            public int central; // Other regions
        }

        [Serializable]
        public class PoliticalDistribution
        {
            public int left; // SP, GL, PvdA
            public int centerLeft; // D66, PvdA
            public int center; // VVD, D66
            public int centerRight; // VVD, CDA
            public int right; // FvD, other
        }

        [Serializable]
        public class ExpertiseDistribution
        {
            public int expert; // Professional/academic
            public int high; // Active participation
            public int intermediate; // Regular followers
            public int moderate; // Casual followers
        }

        [Serializable]
        public class DemographicBalance
        {
            public int age18to30;
            public int age31to45;
            public int age46to60;
            public int age60plus;
            public int students;
            public int journalists;
            public int academics;
            public int professionals;
            public int partyMembers;
        }

        public enum ParticipantStatus
        {
            Registered,
            Screened,
            Scheduled,
            Completed,
            Cancelled,
            NoShow
        }

        public enum SessionType
        {
            Individual,
            Group,
            Expert,
            Remote,
            InPerson
        }

        public enum SessionStatus
        {
            Scheduled,
            Confirmed,
            InProgress,
            Completed,
            Cancelled,
            Rescheduled
        }

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeScheduler();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeScheduler()
        {
            string baseDir = Path.Combine(Application.persistentDataPath, "CoalitionDemo", "UserTesting");
            dataDirectory = baseDir;
            Directory.CreateDirectory(dataDirectory);

            LoadParticipantData();
            Debug.Log("User Testing Scheduler initialized");
        }

        public string RegisterParticipant(ParticipantProfile profile, ContactInformation contact, AvailabilitySchedule availability)
        {
            var participant = new TestingParticipant
            {
                id = Guid.NewGuid().ToString(),
                profile = profile,
                contact = contact,
                availability = availability,
                registrationDate = DateTime.Now,
                status = ParticipantStatus.Registered
            };

            participants.Add(participant);
            OnParticipantRegistered?.Invoke(participant);

            SaveParticipantData();
            UpdateRecruitmentStatus();

            Debug.Log($"Participant registered: {participant.id}");
            return participant.id;
        }

        public void UpdateParticipantProfile(string participantId, ParticipantProfile newProfile)
        {
            var participant = participants.Find(p => p.id == participantId);
            if (participant != null)
            {
                participant.profile = newProfile;
                SaveParticipantData();
                UpdateRecruitmentStatus();
            }
        }

        public string ScheduleSession(string participantId, DateTime scheduledTime, string location = "Remote", SessionType type = SessionType.Individual)
        {
            var participant = participants.Find(p => p.id == participantId);
            if (participant == null)
            {
                Debug.LogError($"Participant not found: {participantId}");
                return null;
            }

            var session = new TestingSession
            {
                sessionId = Guid.NewGuid().ToString(),
                participantId = participantId,
                scheduledTime = scheduledTime,
                durationMinutes = sessionDurationMinutes,
                location = location,
                type = type,
                status = SessionStatus.Scheduled,
                objectives = GetSessionObjectives(participant.profile),
                results = new SessionResults()
            };

            scheduledSessions.Add(session);
            participant.sessionIds.Add(session.sessionId);
            participant.status = ParticipantStatus.Scheduled;

            OnSessionScheduled?.Invoke(session);
            SaveSessionData();

            Debug.Log($"Session scheduled: {session.sessionId} for participant {participantId}");
            return session.sessionId;
        }

        private List<string> GetSessionObjectives(ParticipantProfile profile)
        {
            var objectives = new List<string>();

            // Base objectives for all participants
            objectives.Add("Evaluate political authenticity and accuracy");
            objectives.Add("Assess user interface and experience");
            objectives.Add("Test core coalition formation scenarios");

            // Expertise-specific objectives
            switch (profile.politicalKnowledge)
            {
                case "Expert":
                    objectives.Add("Validate advanced political mechanics");
                    objectives.Add("Assess educational value for teaching");
                    objectives.Add("Evaluate potential for academic use");
                    break;
                case "Intermediate":
                    objectives.Add("Test learning curve and accessibility");
                    objectives.Add("Evaluate engagement and interest maintenance");
                    break;
                case "Beginner":
                    objectives.Add("Assess onboarding and tutorial effectiveness");
                    objectives.Add("Test basic concept comprehension");
                    break;
            }

            // Occupation-specific objectives
            switch (profile.occupation)
            {
                case "Journalist":
                    objectives.Add("Evaluate potential as news/education tool");
                    objectives.Add("Assess accuracy for public communication");
                    break;
                case "Academic":
                    objectives.Add("Validate theoretical frameworks");
                    objectives.Add("Assess research and teaching applications");
                    break;
                case "Student":
                    objectives.Add("Test educational effectiveness");
                    objectives.Add("Evaluate engagement for learning");
                    break;
                case "Party Member":
                    objectives.Add("Validate party representation accuracy");
                    objectives.Add("Assess coalition formation realism");
                    break;
            }

            return objectives;
        }

        public void CompleteSession(string sessionId, SessionResults results)
        {
            var session = scheduledSessions.Find(s => s.sessionId == sessionId);
            if (session == null)
            {
                Debug.LogError($"Session not found: {sessionId}");
                return;
            }

            session.status = SessionStatus.Completed;
            session.results = results;
            session.actualEndTime = DateTime.Now;

            var participant = participants.Find(p => p.id == session.participantId);
            if (participant != null)
            {
                participant.status = ParticipantStatus.Completed;
            }

            // Move to completed sessions
            scheduledSessions.Remove(session);
            completedSessions.Add(session);

            OnSessionCompleted?.Invoke(session);
            SaveSessionData();

            Debug.Log($"Session completed: {sessionId}");
        }

        public RecruitmentStatus GetRecruitmentStatus()
        {
            var status = new RecruitmentStatus
            {
                totalRegistered = participants.Count,
                targetCount = targetParticipantCount,
                completionPercentage = RecruitmentProgress * 100f,
                geographic = CalculateGeographicDistribution(),
                political = CalculatePoliticalDistribution(),
                expertise = CalculateExpertiseDistribution(),
                demographics = CalculateDemographicBalance(),
                recruitmentGaps = IdentifyRecruitmentGaps()
            };

            return status;
        }

        private GeographicDistribution CalculateGeographicDistribution()
        {
            var distribution = new GeographicDistribution();

            foreach (var participant in participants)
            {
                switch (participant.profile.region?.ToLower())
                {
                    case "randstad":
                    case "amsterdam":
                    case "rotterdam":
                    case "the hague":
                    case "utrecht":
                        distribution.randstad++;
                        break;
                    case "north":
                    case "groningen":
                    case "friesland":
                    case "drenthe":
                        distribution.north++;
                        break;
                    case "east":
                    case "overijssel":
                    case "gelderland":
                        distribution.east++;
                        break;
                    case "south":
                    case "north brabant":
                    case "limburg":
                        distribution.south++;
                        break;
                    default:
                        distribution.central++;
                        break;
                }
            }

            return distribution;
        }

        private PoliticalDistribution CalculatePoliticalDistribution()
        {
            var distribution = new PoliticalDistribution();

            foreach (var participant in participants)
            {
                switch (participant.profile.politicalPreference?.ToLower())
                {
                    case "left":
                        distribution.left++;
                        break;
                    case "center-left":
                        distribution.centerLeft++;
                        break;
                    case "center":
                        distribution.center++;
                        break;
                    case "center-right":
                        distribution.centerRight++;
                        break;
                    case "right":
                        distribution.right++;
                        break;
                }
            }

            return distribution;
        }

        private ExpertiseDistribution CalculateExpertiseDistribution()
        {
            var distribution = new ExpertiseDistribution();

            foreach (var participant in participants)
            {
                switch (participant.profile.politicalKnowledge?.ToLower())
                {
                    case "expert":
                        distribution.expert++;
                        break;
                    case "high":
                        distribution.high++;
                        break;
                    case "intermediate":
                        distribution.intermediate++;
                        break;
                    case "moderate":
                    case "beginner":
                        distribution.moderate++;
                        break;
                }
            }

            return distribution;
        }

        private DemographicBalance CalculateDemographicBalance()
        {
            var balance = new DemographicBalance();

            foreach (var participant in participants)
            {
                // Age distribution
                if (participant.profile.age >= 18 && participant.profile.age <= 30)
                    balance.age18to30++;
                else if (participant.profile.age >= 31 && participant.profile.age <= 45)
                    balance.age31to45++;
                else if (participant.profile.age >= 46 && participant.profile.age <= 60)
                    balance.age46to60++;
                else if (participant.profile.age > 60)
                    balance.age60plus++;

                // Occupation distribution
                switch (participant.profile.occupation?.ToLower())
                {
                    case "student":
                        balance.students++;
                        break;
                    case "journalist":
                        balance.journalists++;
                        break;
                    case "academic":
                        balance.academics++;
                        break;
                    case "professional":
                        balance.professionals++;
                        break;
                    case "party member":
                        balance.partyMembers++;
                        break;
                }
            }

            return balance;
        }

        private List<string> IdentifyRecruitmentGaps()
        {
            var gaps = new List<string>();
            var status = GetRecruitmentStatus();

            // Check geographic balance
            if (enforceGeographicBalance)
            {
                int totalGeo = status.geographic.randstad + status.geographic.north + status.geographic.east + status.geographic.south;
                if (totalGeo > 0)
                {
                    float randstadPercent = (float)status.geographic.randstad / totalGeo;
                    if (randstadPercent > 0.5f) gaps.Add("Need more participants from outside Randstad");
                    if (status.geographic.north == 0) gaps.Add("Need participants from Northern Netherlands");
                    if (status.geographic.south < 2) gaps.Add("Need more participants from Southern Netherlands");
                }
            }

            // Check political balance
            if (enforcePoliticalBalance)
            {
                int totalPol = status.political.left + status.political.centerLeft + status.political.center + status.political.centerRight + status.political.right;
                if (totalPol > 0)
                {
                    if (status.political.left == 0) gaps.Add("Need left-wing political perspectives");
                    if (status.political.right == 0) gaps.Add("Need right-wing political perspectives");
                    if (status.political.center < 2) gaps.Add("Need more centrist political perspectives");
                }
            }

            // Check expertise balance
            if (enforceExpertiseBalance)
            {
                if (status.expertise.expert < 3) gaps.Add("Need more political experts");
                if (status.expertise.high < 5) gaps.Add("Need more highly knowledgeable participants");
                if (status.demographics.students < 3) gaps.Add("Need more student participants");
                if (status.demographics.journalists < 2) gaps.Add("Need more journalist participants");
            }

            return gaps;
        }

        public List<TestingParticipant> GetParticipants() => new List<TestingParticipant>(participants);

        public List<TestingSession> GetScheduledSessions() => new List<TestingSession>(scheduledSessions);

        public List<TestingSession> GetCompletedSessions() => new List<TestingSession>(completedSessions);

        public TestingParticipant GetParticipant(string participantId) => participants.Find(p => p.id == participantId);

        public TestingSession GetSession(string sessionId) =>
            scheduledSessions.Find(s => s.sessionId == sessionId) ??
            completedSessions.Find(s => s.sessionId == sessionId);

        public List<TimeSlot> GetAvailableTimeSlots()
        {
            var availableSlots = new List<TimeSlot>();

            foreach (var participant in participants)
            {
                if (participant.status == ParticipantStatus.Registered || participant.status == ParticipantStatus.Screened)
                {
                    availableSlots.AddRange(participant.availability.availableSlots);
                }
            }

            return availableSlots;
        }

        public void GenerateSchedulingReport()
        {
            try
            {
                string reportFile = Path.Combine(dataDirectory, $"scheduling_report_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt");
                string report = CreateSchedulingReport();
                File.WriteAllText(reportFile, report);

                Debug.Log($"Scheduling report generated: {reportFile}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to generate scheduling report: {ex.Message}");
            }
        }

        private string CreateSchedulingReport()
        {
            var report = new System.Text.StringBuilder();
            var status = GetRecruitmentStatus();

            report.AppendLine("=== COALITION DEMO - USER TESTING SCHEDULING REPORT ===");
            report.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine();

            report.AppendLine("=== RECRUITMENT OVERVIEW ===");
            report.AppendLine($"Target Participants: {targetParticipantCount}");
            report.AppendLine($"Registered: {status.totalRegistered}");
            report.AppendLine($"Completion: {status.completionPercentage:F1}%");
            report.AppendLine($"Scheduled Sessions: {ScheduledSessions}");
            report.AppendLine($"Completed Sessions: {CompletedSessions}");
            report.AppendLine();

            report.AppendLine("=== GEOGRAPHIC DISTRIBUTION ===");
            report.AppendLine($"Randstad: {status.geographic.randstad}");
            report.AppendLine($"North: {status.geographic.north}");
            report.AppendLine($"East: {status.geographic.east}");
            report.AppendLine($"South: {status.geographic.south}");
            report.AppendLine($"Central: {status.geographic.central}");
            report.AppendLine();

            report.AppendLine("=== POLITICAL DISTRIBUTION ===");
            report.AppendLine($"Left: {status.political.left}");
            report.AppendLine($"Center-Left: {status.political.centerLeft}");
            report.AppendLine($"Center: {status.political.center}");
            report.AppendLine($"Center-Right: {status.political.centerRight}");
            report.AppendLine($"Right: {status.political.right}");
            report.AppendLine();

            report.AppendLine("=== EXPERTISE DISTRIBUTION ===");
            report.AppendLine($"Expert: {status.expertise.expert}");
            report.AppendLine($"High: {status.expertise.high}");
            report.AppendLine($"Intermediate: {status.expertise.intermediate}");
            report.AppendLine($"Moderate: {status.expertise.moderate}");
            report.AppendLine();

            report.AppendLine("=== RECRUITMENT GAPS ===");
            if (status.recruitmentGaps.Count == 0)
            {
                report.AppendLine("No significant recruitment gaps identified.");
            }
            else
            {
                foreach (var gap in status.recruitmentGaps)
                {
                    report.AppendLine($"- {gap}");
                }
            }

            return report.ToString();
        }

        private void SaveParticipantData()
        {
            try
            {
                string fileName = "participants.json";
                string filePath = Path.Combine(dataDirectory, fileName);
                string jsonData = JsonUtility.ToJson(new SerializableList<TestingParticipant>(participants), true);
                File.WriteAllText(filePath, jsonData);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save participant data: {ex.Message}");
            }
        }

        private void SaveSessionData()
        {
            try
            {
                string scheduledFile = Path.Combine(dataDirectory, "scheduled_sessions.json");
                string scheduledData = JsonUtility.ToJson(new SerializableList<TestingSession>(scheduledSessions), true);
                File.WriteAllText(scheduledFile, scheduledData);

                string completedFile = Path.Combine(dataDirectory, "completed_sessions.json");
                string completedData = JsonUtility.ToJson(new SerializableList<TestingSession>(completedSessions), true);
                File.WriteAllText(completedFile, completedData);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save session data: {ex.Message}");
            }
        }

        private void LoadParticipantData()
        {
            try
            {
                string filePath = Path.Combine(dataDirectory, "participants.json");
                if (File.Exists(filePath))
                {
                    string jsonData = File.ReadAllText(filePath);
                    var loadedParticipants = JsonUtility.FromJson<SerializableList<TestingParticipant>>(jsonData);
                    participants = loadedParticipants.items;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load participant data: {ex.Message}");
            }
        }

        private void UpdateRecruitmentStatus()
        {
            var status = GetRecruitmentStatus();
            OnRecruitmentStatusUpdated?.Invoke(status);
        }

        [Serializable]
        private class SerializableList<T>
        {
            public List<T> items;
            public SerializableList(List<T> items) { this.items = items; }
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                SaveParticipantData();
                SaveSessionData();
                GenerateSchedulingReport();
            }
        }
    }
}