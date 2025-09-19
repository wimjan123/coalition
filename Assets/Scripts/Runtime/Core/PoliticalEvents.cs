using System;
using System.Collections.Generic;
using Coalition.Runtime.Data;

namespace Coalition.Runtime.Core
{
    /// <summary>
    /// Base class for all political events in the coalition demo.
    /// </summary>
    public abstract class PoliticalEvent : IEvent
    {
        public DateTime Timestamp { get; }

        protected PoliticalEvent()
        {
            Timestamp = DateTime.Now;
        }
    }

    // Election Events

    /// <summary>
    /// Published when election results are calculated using D'Hondt algorithm.
    /// </summary>
    public class ElectionCalculatedEvent : PoliticalEvent
    {
        public ElectionResult Result { get; }
        public float CalculationTimeSeconds { get; }

        public ElectionCalculatedEvent(ElectionResult result, float calculationTime)
        {
            Result = result;
            CalculationTimeSeconds = calculationTime;
        }
    }

    /// <summary>
    /// Published when election results are displayed to the user.
    /// </summary>
    public class ElectionResultsDisplayedEvent : PoliticalEvent
    {
        public ElectionResult Result { get; }
        public bool ShowComparison { get; }

        public ElectionResultsDisplayedEvent(ElectionResult result, bool showComparison = false)
        {
            Result = result;
            ShowComparison = showComparison;
        }
    }

    // Party Events

    /// <summary>
    /// Published when a political party is selected in the UI.
    /// </summary>
    public class PartySelectedEvent : PoliticalEvent
    {
        public string PartyName { get; }
        public int SeatIndex { get; }

        public PartySelectedEvent(string partyName, int seatIndex = -1)
        {
            PartyName = partyName;
            SeatIndex = seatIndex;
        }
    }

    /// <summary>
    /// Published when party information is requested or displayed.
    /// </summary>
    public class PartyInfoRequestedEvent : PoliticalEvent
    {
        public string PartyName { get; }
        public bool ShowDetailed { get; }

        public PartyInfoRequestedEvent(string partyName, bool showDetailed = true)
        {
            PartyName = partyName;
            ShowDetailed = showDetailed;
        }
    }

    // Coalition Events

    /// <summary>
    /// Published when viable coalitions are found and calculated.
    /// </summary>
    public class ViableCoalitionsFoundEvent : PoliticalEvent
    {
        public List<ViableCoalition> Coalitions { get; }
        public int TotalCombinationsChecked { get; }

        public ViableCoalitionsFoundEvent(List<ViableCoalition> coalitions, int totalChecked = 0)
        {
            Coalitions = coalitions;
            TotalCombinationsChecked = totalChecked;
        }
    }

    /// <summary>
    /// Published when a coalition is being formed or modified.
    /// </summary>
    public class CoalitionFormationEvent : PoliticalEvent
    {
        public List<string> PartyNames { get; }
        public CoalitionFormationState State { get; }
        public string Message { get; }

        public CoalitionFormationEvent(List<string> partyNames, CoalitionFormationState state, string message = "")
        {
            PartyNames = partyNames;
            State = state;
            Message = message;
        }
    }

    /// <summary>
    /// Published when a coalition formation is completed successfully.
    /// </summary>
    public class CoalitionFormedEvent : PoliticalEvent
    {
        public ViableCoalition Coalition { get; }
        public PortfolioAllocation PortfolioAllocation { get; }
        public float FormationDurationSeconds { get; }

        public CoalitionFormedEvent(ViableCoalition coalition, PortfolioAllocation allocation, float duration)
        {
            Coalition = coalition;
            PortfolioAllocation = allocation;
            FormationDurationSeconds = duration;
        }
    }

    /// <summary>
    /// Published when coalition formation fails.
    /// </summary>
    public class CoalitionFormationFailedEvent : PoliticalEvent
    {
        public List<string> AttemptedParties { get; }
        public string FailureReason { get; }
        public CoalitionFailureType FailureType { get; }

        public CoalitionFormationFailedEvent(List<string> attemptedParties, string reason, CoalitionFailureType type)
        {
            AttemptedParties = attemptedParties;
            FailureReason = reason;
            FailureType = type;
        }
    }

    // UI Events

    /// <summary>
    /// Published when a window is opened in the desktop environment.
    /// </summary>
    public class WindowOpenedEvent : PoliticalEvent
    {
        public string WindowType { get; }
        public bool BringToFront { get; }

        public WindowOpenedEvent(string windowType, bool bringToFront = true)
        {
            WindowType = windowType;
            BringToFront = bringToFront;
        }
    }

    /// <summary>
    /// Published when a window is closed in the desktop environment.
    /// </summary>
    public class WindowClosedEvent : PoliticalEvent
    {
        public string WindowType { get; }
        public string WindowId { get; }

        public WindowClosedEvent(string windowType, string windowId)
        {
            WindowType = windowType;
            WindowId = windowId;
        }
    }

    /// <summary>
    /// Published when parliament seats need to be highlighted for coalition visualization.
    /// </summary>
    public class HighlightCoalitionEvent : PoliticalEvent
    {
        public List<string> CoalitionParties { get; }
        public bool ClearExisting { get; }

        public HighlightCoalitionEvent(List<string> coalitionParties, bool clearExisting = true)
        {
            CoalitionParties = coalitionParties;
            ClearExisting = clearExisting;
        }
    }

    // System Events

    /// <summary>
    /// Published when the political data repository is initialized.
    /// </summary>
    public class PoliticalDataInitializedEvent : PoliticalEvent
    {
        public int PartyCount { get; }
        public bool ValidationPassed { get; }
        public string ValidationSummary { get; }

        public PoliticalDataInitializedEvent(int partyCount, bool validationPassed, string validationSummary)
        {
            PartyCount = partyCount;
            ValidationPassed = validationPassed;
            ValidationSummary = validationSummary;
        }
    }

    /// <summary>
    /// Published when the demo enters a new phase.
    /// </summary>
    public class DemoPhaseChangedEvent : PoliticalEvent
    {
        public DemoPhase PreviousPhase { get; }
        public DemoPhase NewPhase { get; }
        public string PhaseName { get; }

        public DemoPhaseChangedEvent(DemoPhase previousPhase, DemoPhase newPhase, string phaseName)
        {
            PreviousPhase = previousPhase;
            NewPhase = newPhase;
            PhaseName = phaseName;
        }
    }

    /// <summary>
    /// Published when a performance threshold is exceeded.
    /// </summary>
    public class PerformanceWarningEvent : PoliticalEvent
    {
        public string Operation { get; }
        public float ActualTimeSeconds { get; }
        public float ThresholdSeconds { get; }
        public string Details { get; }

        public PerformanceWarningEvent(string operation, float actualTime, float threshold, string details = "")
        {
            Operation = operation;
            ActualTimeSeconds = actualTime;
            ThresholdSeconds = threshold;
            Details = details;
        }
    }

    // Enums for event data

    /// <summary>
    /// States of coalition formation process.
    /// </summary>
    /// <summary>
    /// Event fired when coalition compatibility analysis starts
    /// </summary>
    public class CoalitionAnalysisStartedEvent : PoliticalEvent
    {
        public List<string> SelectedParties { get; }
        public DateTime AnalysisStartTime { get; }
        
        public CoalitionAnalysisStartedEvent(List<string> parties)
        {
            SelectedParties = parties ?? new List<string>();
            AnalysisStartTime = DateTime.Now;
        }
    }

    /// <summary>
    /// Event fired when coalition compatibility analysis completes
    /// </summary>
    public class CoalitionAnalysisCompletedEvent : PoliticalEvent
    {
        public CoalitionFormationSystem.CoalitionAnalysis Analysis { get; }
        public float AnalysisTimeMs { get; }
        public bool HasViableCoalitions { get; }
        
        public CoalitionAnalysisCompletedEvent(CoalitionFormationSystem.CoalitionAnalysis analysisResult)
        {
            Analysis = analysisResult;
            AnalysisTimeMs = analysisResult?.AnalysisTimeMs ?? 0f;
            HasViableCoalitions = analysisResult?.ViableCoalitions?.Count > 0;
        }
    }

    /// <summary>
    /// Event fired when coalition compatibility score is calculated in real-time
    /// </summary>
    public class CoalitionCompatibilityUpdatedEvent : PoliticalEvent
    {
        public List<string> SelectedParties { get; }
        public float CompatibilityScore { get; }
        public int TotalSeats { get; }
        public bool IsViable { get; }
        public List<string> RedLineViolations { get; }
        public bool HasRedLineViolations { get; }
        
        public CoalitionCompatibilityUpdatedEvent(List<string> parties, float score, int seats, 
            bool viable, List<string> violations)
        {
            SelectedParties = parties ?? new List<string>();
            CompatibilityScore = score;
            TotalSeats = seats;
            IsViable = viable;
            RedLineViolations = violations ?? new List<string>();
            HasRedLineViolations = RedLineViolations.Count > 0;
        }
    }

    /// <summary>
    /// Event fired when red-line violations are detected
    /// </summary>
    public class RedLineViolationDetectedEvent : PoliticalEvent
    {
        public string ViolatingParty { get; }
        public string ExcludedParty { get; }
        public string ViolationReason { get; }
        public float PenaltyApplied { get; }
        
        public RedLineViolationDetectedEvent(string violator, string excluded, string reason, float penalty)
        {
            ViolatingParty = violator;
            ExcludedParty = excluded;
            ViolationReason = reason;
            PenaltyApplied = penalty;
        }
    }

    /// <summary>
    /// Event fired when specific coalition scenario is requested
    /// </summary>
    public class CoalitionScenarioRequestedEvent : PoliticalEvent
    {
        public string ScenarioName { get; }
        public List<string> PartyAbbreviations { get; }
        public bool IsHistoricalScenario { get; }
        
        public CoalitionScenarioRequestedEvent(string scenario, List<string> parties, bool historical = false)
        {
            ScenarioName = scenario;
            PartyAbbreviations = parties ?? new List<string>();
            IsHistoricalScenario = historical;
        }
    }
    public enum CoalitionFormationState
    {
        Started,
        PartyAdded,
        PartyRemoved,
        CalculatingCompatibility,
        CheckingViability,
        NegotiatingPortfolios,
        Completed,
        Failed
    }

    /// <summary>
    /// Types of coalition formation failures.
    /// </summary>
    public enum CoalitionFailureType
    {
        InsufficientSeats,
        IncompatibleParties,
        MutualExclusions,
        RedLineViolations,
        PortfolioDisagreement,
        TimeoutExpired,
        UserCancelled
    }

    /// <summary>
    /// Demo phases for the 6-week implementation.
    /// </summary>
    public enum DemoPhase
    {
        Initialization,
        ElectionResults,
        PartyExploration,
        CoalitionBuilding,
        GovernmentFormation,
        Analysis,
        Tutorial,
        UserTesting
    }

    // Supporting data structures referenced by events

    /// <summary>
    /// Results of an election calculation.
    /// </summary>
    public struct ElectionResult
    {
        public Dictionary<string, int> PartySeats;
        public Dictionary<string, float> VotePercentages;
        public float TotalTurnout;
        public DateTime CalculationTimestamp;
    }

    /// <summary>
    /// A viable coalition with calculated metrics.
    /// </summary>
    public class ViableCoalition
    {
        public List<DemoPoliticalParty> Parties { get; private set; }
        public int TotalSeats { get; private set; }
        public float AverageCompatibility { get; private set; }
        public float StabilityScore { get; private set; }
        public float ViabilityScore => (TotalSeats / 150.0f) * 0.6f + AverageCompatibility * 0.4f;

        public ViableCoalition(List<DemoPoliticalParty> parties)
        {
            Parties = parties;
            TotalSeats = parties?.Sum(p => p.SeatsWon) ?? 0;
            CalculateMetrics();
        }

        public DemoPoliticalParty PrimeMinisterParty =>
            Parties?.OrderByDescending(p => p.SeatsWon).FirstOrDefault();

        public float CalculateAverageCompatibility()
        {
            if (Parties == null || Parties.Count < 2) return 0.0f;

            float totalCompatibility = 0.0f;
            int comparisons = 0;

            for (int i = 0; i < Parties.Count; i++)
            {
                for (int j = i + 1; j < Parties.Count; j++)
                {
                    totalCompatibility += Parties[i].CalculateCompatibilityWith(Parties[j]);
                    comparisons++;
                }
            }

            return comparisons > 0 ? totalCompatibility / comparisons : 0.0f;
        }

        private void CalculateMetrics()
        {
            AverageCompatibility = CalculateAverageCompatibility();
            // Stability calculation will be implemented in future phases
            StabilityScore = AverageCompatibility; // Placeholder
        }
    }

    /// <summary>
    /// Portfolio allocation for a formed government.
    /// </summary>
    public class PortfolioAllocation
    {
        public DemoPoliticalParty PrimeMinister { get; set; }
        public Dictionary<string, DemoPoliticalParty> PortfolioAssignments { get; set; } = new Dictionary<string, DemoPoliticalParty>();

        public void AssignPortfolio(DutchMinisterialPortfolio portfolio, DemoPoliticalParty party)
        {
            PortfolioAssignments[portfolio.portfolioName] = party;
        }

        public DemoPoliticalParty GetPortfolioParty(string portfolioName)
        {
            return PortfolioAssignments.GetValueOrDefault(portfolioName);
        }
    }
}