using System;
using System.Collections.Generic;
using UnityEngine;

namespace Coalition.Demo
{
    /// <summary>
    /// Configuration settings for the COALITION demo system
    /// Centralizes all demo parameters and scenarios
    /// </summary>
    [CreateAssetMenu(fileName = "DemoConfiguration", menuName = "Coalition/Demo/Demo Configuration")]
    public class DemoConfiguration : ScriptableObject
    {
        [Header("Demo Settings")]
        public float demoTimeoutMinutes = 30f;
        public bool enablePerformanceMonitoring = true;
        public bool enableUserTesting = true;
        public bool enableDetailedLogging = false;

        [Header("Performance Targets")]
        public float targetFrameRate = 60f;
        public float maxCalculationTimeSeconds = 5f;
        public long maxMemoryUsageMB = 1024;

        [Header("Scenario Configuration")]
        public DemoScenarioConfig[] availableScenarios;

        [Header("Quality Assurance")]
        public QualityThreshold[] qualityThresholds;

        [Header("User Testing")]
        public string[] userTestingMetrics;
        public float feedbackCollectionIntervalMinutes = 5f;

        public DemoScenarioConfig GetScenario(string scenarioName)
        {
            return Array.Find(availableScenarios, s =>
                string.Equals(s.scenarioName, scenarioName, StringComparison.OrdinalIgnoreCase));
        }

        public QualityThreshold GetQualityThreshold(string metricName)
        {
            return Array.Find(qualityThresholds, t =>
                string.Equals(t.metricName, metricName, StringComparison.OrdinalIgnoreCase));
        }

        public bool IsMetricWithinThreshold(string metricName, float value)
        {
            var threshold = GetQualityThreshold(metricName);
            if (threshold == null) return true;

            return value >= threshold.minimumValue && value <= threshold.maximumValue;
        }
    }

    [Serializable]
    public class DemoScenarioConfig
    {
        public string scenarioName;
        public string displayName;
        public string description;
        public float estimatedDurationMinutes;
        public string[] requiredSystems;
        public WindowType[] windowsToOpen;
        public bool requiresTutorial;
        public ScenarioPhase[] phases;
    }

    [Serializable]
    public class ScenarioPhase
    {
        public string phaseName;
        public string description;
        public float timeoutSeconds;
        public string[] requiredActions;
        public WindowType[] windowsToShow;
        public bool isOptional;
    }

    [Serializable]
    public class QualityThreshold
    {
        public string metricName;
        public float minimumValue;
        public float maximumValue;
        public string unit;
        public bool isRequired;
    }

    /// <summary>
    /// Tutorial configuration for guided demo experiences
    /// </summary>
    [CreateAssetMenu(fileName = "TutorialConfiguration", menuName = "Coalition/Demo/Tutorial Configuration")]
    public class TutorialConfiguration : ScriptableObject
    {
        [Header("Tutorial Settings")]
        public bool enableSkipOption = true;
        public float defaultStepTimeoutSeconds = 60f;
        public bool showProgressIndicator = true;

        [Header("Tutorial Content")]
        public TutorialStepData[] tutorialSteps;

        [Header("Localization")]
        public SystemLanguage[] supportedLanguages = { SystemLanguage.English, SystemLanguage.Dutch };
        public SystemLanguage defaultLanguage = SystemLanguage.English;

        public TutorialStepData[] GetTutorialSteps()
        {
            return tutorialSteps;
        }

        public TutorialStepData GetTutorialStep(string stepId)
        {
            return Array.Find(tutorialSteps, s => s.stepId == stepId);
        }

        public TutorialStepData[] GetStepsForScenario(string scenarioName)
        {
            return Array.FindAll(tutorialSteps, s => s.applicableScenarios.Contains(scenarioName));
        }
    }

    [Serializable]
    public class TutorialStepData
    {
        public string stepId;
        public string title;
        [TextArea(3, 6)]
        public string description;
        public float timeoutSeconds;
        public WindowType[] windowsToOpen;
        public string[] requiredActions;
        public string[] applicableScenarios;
        public bool isOptional;
        public TutorialMediaContent[] mediaContent;
    }

    [Serializable]
    public class TutorialMediaContent
    {
        public string contentType; // "image", "video", "audio"
        public string resourcePath;
        public string altText;
        public bool isRequired;
    }

    /// <summary>
    /// Performance monitoring configuration
    /// </summary>
    [CreateAssetMenu(fileName = "PerformanceMonitoringConfig", menuName = "Coalition/Demo/Performance Monitoring")]
    public class PerformanceMonitoringConfig : ScriptableObject
    {
        [Header("Monitoring Settings")]
        public bool enableRealTimeMonitoring = true;
        public float monitoringIntervalSeconds = 1f;
        public bool enableMemoryProfiling = true;
        public bool enableFrameTimeAnalysis = true;

        [Header("Performance Targets")]
        public PerformanceTarget[] performanceTargets;

        [Header("Alert Settings")]
        public bool enablePerformanceAlerts = true;
        public float alertThresholdMultiplier = 1.5f;

        public PerformanceTarget GetTarget(string metricName)
        {
            return Array.Find(performanceTargets, t => t.metricName == metricName);
        }

        public bool IsPerformanceAcceptable(string metricName, float value)
        {
            var target = GetTarget(metricName);
            if (target == null) return true;

            return value <= target.targetValue * alertThresholdMultiplier;
        }
    }

    [Serializable]
    public class PerformanceTarget
    {
        public string metricName;
        public float targetValue;
        public string unit;
        public bool isHardRequirement;
        public string description;
    }

    /// <summary>
    /// User testing framework configuration
    /// </summary>
    [CreateAssetMenu(fileName = "UserTestingConfig", menuName = "Coalition/Demo/User Testing")]
    public class UserTestingConfig : ScriptableObject
    {
        [Header("Testing Configuration")]
        public bool enableAnonymousMode = true;
        public bool enableDetailedTracking = false;
        public float sessionTimeoutMinutes = 45f;

        [Header("Data Collection")]
        public UserMetric[] trackedMetrics;
        public InteractionEvent[] trackedInteractions;

        [Header("Feedback Collection")]
        public FeedbackPrompt[] feedbackPrompts;
        public float feedbackIntervalMinutes = 10f;

        [Header("Export Settings")]
        public bool enableDataExport = true;
        public string exportFormat = "JSON";
        public bool includePersonalData = false;

        public UserMetric GetMetric(string metricName)
        {
            return Array.Find(trackedMetrics, m => m.metricName == metricName);
        }

        public InteractionEvent GetInteractionEvent(string eventName)
        {
            return Array.Find(trackedInteractions, e => e.eventName == eventName);
        }
    }

    [Serializable]
    public class UserMetric
    {
        public string metricName;
        public string displayName;
        public string description;
        public string dataType; // "float", "int", "bool", "string"
        public bool isRequired;
        public float? minimumValue;
        public float? maximumValue;
    }

    [Serializable]
    public class InteractionEvent
    {
        public string eventName;
        public string displayName;
        public string description;
        public string[] parameters;
        public bool trackTimestamp;
        public bool trackDuration;
    }

    [Serializable]
    public class FeedbackPrompt
    {
        public string promptId;
        public string question;
        public FeedbackType feedbackType;
        public string[] options; // For multiple choice
        public float minValue; // For scale
        public float maxValue; // For scale
        public bool isRequired;
        public TriggerCondition[] triggerConditions;
    }

    [Serializable]
    public class TriggerCondition
    {
        public string eventName;
        public string comparisonOperator; // "equals", "greater", "less", "contains"
        public string comparisonValue;
    }

    public enum FeedbackType
    {
        Text,
        Scale,
        MultipleChoice,
        YesNo,
        Rating
    }
}