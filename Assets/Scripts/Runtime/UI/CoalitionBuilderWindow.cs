using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Coalition.Runtime.Core;
using Coalition.Runtime.Data;

namespace Coalition.Runtime.UI
{
    /// <summary>
    /// Coalition builder window for the desktop environment.
    /// Contains the interactive coalition formation interface with drag-and-drop functionality.
    /// </summary>
    public class CoalitionBuilderWindow : Window
    {
        [Header("Coalition Builder Components")]
        [SerializeField] private CoalitionBuilder coalitionBuilder;
        [SerializeField] private RectTransform statusPanel;
        [SerializeField] private RectTransform analysisPanel;

        [Header("Status Display")]
        [SerializeField] private Text currentStatusText;
        [SerializeField] private Text goalText;
        [SerializeField] private ProgressBar progressBar;
        [SerializeField] private Image statusIndicator;

        [Header("Quick Actions")]
        [SerializeField] private Button suggestCoalitionButton;
        [SerializeField] private Button loadPresetButton;
        [SerializeField] private Button saveCoalitionButton;
        [SerializeField] private Button shareCoalitionButton;

        [Header("Analysis Display")]
        [SerializeField] private Text compatibilityAnalysisText;
        [SerializeField] private Text stabilityAnalysisText;
        [SerializeField] private Text policyAnalysisText;
        [SerializeField] private RectTransform warningsContainer;

        [Header("Tutorial and Help")]
        [SerializeField] private Button helpButton;
        [SerializeField] private GameObject tutorialOverlay;
        [SerializeField] private bool showTutorialOnFirstOpen = true;

        // Window state
        private bool isFirstOpen = true;
        private PoliticalCoalition currentCoalition;
        private ElectionResult currentElectionResult;
        private System.Collections.Generic.List<GameObject> warningItems;

        // Analysis state
        private float lastAnalysisUpdate;
        private const float AnalysisUpdateInterval = 1f;

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            InitializeCoalitionBuilderWindow();
        }

        protected override void Start()
        {
            base.Start();
            SetupEventHandlers();
            InitializeEventSubscriptions();
        }

        protected override void OnDestroy()
        {
            CleanupEventSubscriptions();
            base.OnDestroy();
        }

        protected override void Update()
        {
            base.Update();
            UpdateAnalysisDisplay();
        }

        #endregion

        #region Initialization

        private void InitializeCoalitionBuilderWindow()
        {
            warningItems = new System.Collections.Generic.List<GameObject>();

            // Set default window properties
            WindowTitle = "Coalition Builder";

            // Find coalition builder if not assigned
            if (coalitionBuilder == null)
                coalitionBuilder = GetComponentInChildren<CoalitionBuilder>();

            Debug.Log("[CoalitionBuilderWindow] Coalition builder window initialized");
        }

        private void SetupEventHandlers()
        {
            if (suggestCoalitionButton != null)
                suggestCoalitionButton.onClick.AddListener(SuggestCoalition);

            if (loadPresetButton != null)
                loadPresetButton.onClick.AddListener(LoadPreset);

            if (saveCoalitionButton != null)
                saveCoalitionButton.onClick.AddListener(SaveCoalition);

            if (shareCoalitionButton != null)
                shareCoalitionButton.onClick.AddListener(ShareCoalition);

            if (helpButton != null)
                helpButton.onClick.AddListener(ShowHelp);
        }

        private void InitializeEventSubscriptions()
        {
            EventBus.Subscribe<ElectionResultsUpdatedEvent>(OnElectionResultsUpdated);
            EventBus.Subscribe<CoalitionChangedEvent>(OnCoalitionChanged);
            EventBus.Subscribe<CoalitionRedLineWarningEvent>(OnRedLineWarning);
            EventBus.Subscribe<GovernmentFormedEvent>(OnGovernmentFormed);
        }

        private void CleanupEventSubscriptions()
        {
            EventBus.Unsubscribe<ElectionResultsUpdatedEvent>(OnElectionResultsUpdated);
            EventBus.Unsubscribe<CoalitionChangedEvent>(OnCoalitionChanged);
            EventBus.Unsubscribe<CoalitionRedLineWarningEvent>(OnRedLineWarning);
            EventBus.Unsubscribe<GovernmentFormedEvent>(OnGovernmentFormed);
        }

        #endregion

        #region Window Management

        protected override void OnOpenComplete()
        {
            base.OnOpenComplete();

            // Show tutorial on first open
            if (isFirstOpen && showTutorialOnFirstOpen)
            {
                ShowTutorial();
                isFirstOpen = false;
            }

            UpdateStatusDisplay();
        }

        #endregion

        #region Status and Progress

        private void UpdateStatusDisplay()
        {
            if (coalitionBuilder == null) return;

            var totalSeats = coalitionBuilder.TotalCoalitionSeats;
            var hasLimajority = coalitionBuilder.HasMajority;
            var compatibility = coalitionBuilder.CompatibilityScore;

            // Update status text
            if (currentStatusText != null)
            {
                var status = hasLimajority ? "Coalition Viable" : "Building Coalition";
                currentStatusText.text = $"Status: {status}";
                currentStatusText.color = hasLimajority ? Color.green : Color.yellow;
            }

            // Update goal text
            if (goalText != null)
            {
                var seatsNeeded = hasLimajority ? 0 : (76 - totalSeats);
                goalText.text = hasLimajority
                    ? $"Majority achieved! (+{totalSeats - 75} seats)"
                    : $"Need {seatsNeeded} more seats for majority";
            }

            // Update progress bar
            if (progressBar != null)
            {
                var progress = Mathf.Clamp01(totalSeats / 76f);
                progressBar.SetProgress(progress);
                progressBar.SetColor(hasLimajority ? Color.green : Color.yellow);
            }

            // Update status indicator
            if (statusIndicator != null)
            {
                statusIndicator.color = hasLimajority ? Color.green : Color.yellow;
            }

            // Update button states
            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            var hasCoalition = currentCoalition?.PartyNames?.Count > 0;
            var hasViableCoalition = coalitionBuilder?.HasMajority == true;

            if (saveCoalitionButton != null)
                saveCoalitionButton.interactable = hasCoalition;

            if (shareCoalitionButton != null)
                shareCoalitionButton.interactable = hasViableCoalition;
        }

        #endregion

        #region Analysis Display

        private void UpdateAnalysisDisplay()
        {
            if (Time.time - lastAnalysisUpdate < AnalysisUpdateInterval) return;
            lastAnalysisUpdate = Time.time;

            if (coalitionBuilder == null || currentCoalition == null) return;

            UpdateCompatibilityAnalysis();
            UpdateStabilityAnalysis();
            UpdatePolicyAnalysis();
        }

        private void UpdateCompatibilityAnalysis()
        {
            if (compatibilityAnalysisText == null) return;

            var compatibility = coalitionBuilder.CompatibilityScore;
            var compatibilityPercent = Mathf.RoundToInt(compatibility * 100);

            var analysisText = $"Party Compatibility: {compatibilityPercent}%\n";

            if (compatibility >= 0.8f)
                analysisText += "Excellent - Strong ideological alignment";
            else if (compatibility >= 0.6f)
                analysisText += "Good - Some common ground";
            else if (compatibility >= 0.4f)
                analysisText += "Moderate - Significant compromises needed";
            else
                analysisText += "Poor - Major ideological conflicts";

            compatibilityAnalysisText.text = analysisText;

            // Color coding
            if (compatibility >= 0.7f)
                compatibilityAnalysisText.color = Color.green;
            else if (compatibility >= 0.5f)
                compatibilityAnalysisText.color = Color.yellow;
            else
                compatibilityAnalysisText.color = Color.red;
        }

        private void UpdateStabilityAnalysis()
        {
            if (stabilityAnalysisText == null || currentCoalition == null) return;

            var partyCount = currentCoalition.PartyNames.Count;
            var totalSeats = coalitionBuilder.TotalCoalitionSeats;

            var stabilityScore = CalculateStabilityScore(partyCount, totalSeats);
            var stabilityPercent = Mathf.RoundToInt(stabilityScore * 100);

            var analysisText = $"Coalition Stability: {stabilityPercent}%\n";

            if (partyCount <= 2)
                analysisText += "High - Simple coalition structure";
            else if (partyCount <= 4)
                analysisText += "Moderate - Complex negotiations required";
            else
                analysisText += "Low - Highly complex, unstable coalition";

            stabilityAnalysisText.text = analysisText;

            // Color coding
            if (stabilityScore >= 0.7f)
                stabilityAnalysisText.color = Color.green;
            else if (stabilityScore >= 0.5f)
                stabilityAnalysisText.color = Color.yellow;
            else
                stabilityAnalysisText.color = Color.red;
        }

        private void UpdatePolicyAnalysis()
        {
            if (policyAnalysisText == null || currentCoalition == null) return;

            // Simplified policy analysis
            var policyAgreement = coalitionBuilder.CompatibilityScore * UnityEngine.Random.Range(0.8f, 1.2f);
            policyAgreement = Mathf.Clamp01(policyAgreement);
            var policyPercent = Mathf.RoundToInt(policyAgreement * 100);

            var analysisText = $"Policy Agreement: {policyPercent}%\n";

            if (policyAgreement >= 0.8f)
                analysisText += "Strong consensus on key issues";
            else if (policyAgreement >= 0.6f)
                analysisText += "Agreement on most core policies";
            else if (policyAgreement >= 0.4f)
                analysisText += "Some policy conflicts to resolve";
            else
                analysisText += "Major policy disagreements";

            policyAnalysisText.text = analysisText;

            // Color coding
            if (policyAgreement >= 0.7f)
                policyAnalysisText.color = Color.green;
            else if (policyAgreement >= 0.5f)
                policyAnalysisText.color = Color.yellow;
            else
                policyAnalysisText.color = Color.red;
        }

        private float CalculateStabilityScore(int partyCount, int totalSeats)
        {
            // Simple stability calculation based on party count and seat distribution
            var countPenalty = Mathf.Max(0, (partyCount - 2) * 0.15f);
            var seatBonus = Mathf.Min(0.2f, (totalSeats - 76) * 0.01f);

            return Mathf.Clamp01(0.8f - countPenalty + seatBonus);
        }

        #endregion

        #region Quick Actions

        private void SuggestCoalition()
        {
            if (currentElectionResult == null)
            {
                Debug.LogWarning("[CoalitionBuilderWindow] No election result available for suggestions");
                return;
            }

            // Clear current coalition
            if (coalitionBuilder != null)
            {
                coalitionBuilder.ClearCoalition();
            }

            // Suggest a viable coalition (simplified algorithm)
            var suggestion = GenerateCoalitionSuggestion();

            if (suggestion.Count > 0)
            {
                // Add suggested parties to coalition
                foreach (var partyName in suggestion)
                {
                    // This would need to interact with the coalition builder's party cards
                    Debug.Log($"[CoalitionBuilderWindow] Suggesting party: {partyName}");
                }

                // Show suggestion notification
                ShowNotification($"Suggested coalition: {string.Join(", ", suggestion)}");
            }
            else
            {
                ShowNotification("No viable coalition suggestions available");
            }

            Debug.Log("[CoalitionBuilderWindow] Generated coalition suggestion");
        }

        private System.Collections.Generic.List<string> GenerateCoalitionSuggestion()
        {
            var suggestion = new System.Collections.Generic.List<string>();

            if (currentElectionResult?.PartyResults == null) return suggestion;

            // Simple suggestion algorithm: largest parties first until majority
            var sortedParties = new System.Collections.Generic.List<PartyResult>(currentElectionResult.PartyResults);
            sortedParties.Sort((a, b) => b.Seats.CompareTo(a.Seats));

            var totalSeats = 0;
            foreach (var party in sortedParties)
            {
                suggestion.Add(party.PartyName);
                totalSeats += party.Seats;

                if (totalSeats >= 76) break;
            }

            return suggestion;
        }

        private void LoadPreset()
        {
            // This could open a preset selector dialog
            Debug.Log("[CoalitionBuilderWindow] Loading coalition preset");

            // Example: Load a historical coalition
            var presetCoalition = new System.Collections.Generic.List<string> { "VVD", "D66", "CDA", "CU" };

            ShowNotification($"Loaded preset: {string.Join(", ", presetCoalition)}");
        }

        private void SaveCoalition()
        {
            if (currentCoalition?.PartyNames?.Count == 0)
            {
                ShowNotification("No coalition to save");
                return;
            }

            // This could open a save dialog or save to a preset
            var coalitionName = $"Coalition_{System.DateTime.Now:yyyyMMdd_HHmm}";

            Debug.Log($"[CoalitionBuilderWindow] Saved coalition: {coalitionName}");
            ShowNotification($"Coalition saved as: {coalitionName}");
        }

        private void ShareCoalition()
        {
            if (!coalitionBuilder.HasMajority)
            {
                ShowNotification("Can only share viable coalitions with majority");
                return;
            }

            // This could generate a shareable link or export data
            var shareText = $"Coalition: {string.Join(", ", currentCoalition.PartyNames)} " +
                           $"({coalitionBuilder.TotalCoalitionSeats} seats, {Mathf.RoundToInt(coalitionBuilder.CompatibilityScore * 100)}% compatibility)";

            Debug.Log($"[CoalitionBuilderWindow] Shared coalition: {shareText}");
            ShowNotification("Coalition shared successfully!");
        }

        #endregion

        #region Help and Tutorial

        private void ShowHelp()
        {
            ShowTutorial();
            Debug.Log("[CoalitionBuilderWindow] Showing help");
        }

        private void ShowTutorial()
        {
            if (tutorialOverlay != null)
            {
                tutorialOverlay.SetActive(true);
            }
            else
            {
                ShowNotification("Tutorial: Drag parties from left to right panel to form a coalition. Need 76+ seats for majority.");
            }

            Debug.Log("[CoalitionBuilderWindow] Showing tutorial");
        }

        public void HideTutorial()
        {
            if (tutorialOverlay != null)
            {
                tutorialOverlay.SetActive(false);
            }
        }

        #endregion

        #region Warnings

        private void ShowWarning(string message)
        {
            if (warningsContainer == null) return;

            // Create warning item
            var warningObject = new GameObject("Warning");
            warningObject.transform.SetParent(warningsContainer, false);

            var warningText = warningObject.AddComponent<Text>();
            warningText.text = $"⚠ {message}";
            warningText.color = Color.red;
            warningText.fontSize = 12;

            // Use theme font if available
            var theme = uiManager?.GetCurrentTheme();
            if (theme?.Font != null)
                warningText.font = theme.Font;

            warningItems.Add(warningObject);

            // Auto-remove after delay
            StartCoroutine(RemoveWarningAfterDelay(warningObject, 5f));
        }

        private System.Collections.IEnumerator RemoveWarningAfterDelay(GameObject warningObject, float delay)
        {
            yield return new WaitForSeconds(delay);

            if (warningObject != null)
            {
                warningItems.Remove(warningObject);
                Destroy(warningObject);
            }
        }

        private void ClearWarnings()
        {
            foreach (var warning in warningItems)
            {
                if (warning != null)
                    Destroy(warning);
            }
            warningItems.Clear();
        }

        #endregion

        #region Notifications

        private void ShowNotification(string message)
        {
            Debug.Log($"[CoalitionBuilderWindow] Notification: {message}");

            // This could show a temporary notification UI element
            // For now, we'll use the window title temporarily
            var originalTitle = WindowTitle;
            WindowTitle = message;

            // Revert title after delay
            StartCoroutine(RevertTitleAfterDelay(originalTitle, 3f));
        }

        private System.Collections.IEnumerator RevertTitleAfterDelay(string originalTitle, float delay)
        {
            yield return new WaitForSeconds(delay);
            WindowTitle = originalTitle;
        }

        #endregion

        #region Event Handlers

        private void OnElectionResultsUpdated(ElectionResultsUpdatedEvent eventData)
        {
            currentElectionResult = eventData.ElectionResult;

            if (coalitionBuilder != null)
            {
                coalitionBuilder.SetElectionResult(currentElectionResult);
            }

            UpdateStatusDisplay();

            Debug.Log($"[CoalitionBuilderWindow] Updated with election results: {currentElectionResult.PartyResults.Count} parties");
        }

        private void OnCoalitionChanged(CoalitionChangedEvent eventData)
        {
            currentCoalition = eventData.Coalition;
            UpdateStatusDisplay();

            Debug.Log($"[CoalitionBuilderWindow] Coalition changed: {currentCoalition?.PartyNames?.Count ?? 0} parties, {eventData.TotalSeats} seats");
        }

        private void OnRedLineWarning(CoalitionRedLineWarningEvent eventData)
        {
            ShowWarning(eventData.Message);
            Debug.Log($"[CoalitionBuilderWindow] Red line warning: {eventData.Message}");
        }

        private void OnGovernmentFormed(GovernmentFormedEvent eventData)
        {
            WindowTitle = "Coalition Builder - Government Formed!";
            ShowNotification("Government successfully formed!");

            // Flash the window to celebrate
            FlashButton();

            Debug.Log($"[CoalitionBuilderWindow] Government formed with {eventData.Coalition.PartyNames.Count} parties");
        }

        #endregion

        #region Theme Support

        public override void ApplyTheme(UITheme theme)
        {
            base.ApplyTheme(theme);

            if (theme == null) return;

            // Apply theme to coalition builder
            if (coalitionBuilder != null)
            {
                coalitionBuilder.ApplyTheme(theme);
            }

            // Apply theme to status texts
            var textComponents = new[] { currentStatusText, goalText, compatibilityAnalysisText, stabilityAnalysisText, policyAnalysisText };
            foreach (var text in textComponents)
            {
                if (text != null)
                {
                    if (theme.Font != null)
                        text.font = theme.Font;
                }
            }

            // Apply theme to buttons
            var buttons = new[] { suggestCoalitionButton, loadPresetButton, saveCoalitionButton, shareCoalitionButton, helpButton };
            foreach (var button in buttons)
            {
                if (button != null)
                {
                    var colors = button.colors;
                    colors.normalColor = theme.ButtonNormalColor;
                    colors.highlightedColor = theme.ButtonHighlightColor;
                    colors.pressedColor = theme.ButtonPressedColor;
                    colors.disabledColor = theme.ButtonDisabledColor;
                    button.colors = colors;
                }
            }

            // Update progress bar colors
            if (progressBar != null)
            {
                progressBar.SetColor(theme.AccentColor);
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get the current coalition from the builder.
        /// </summary>
        /// <returns>Current coalition</returns>
        public PoliticalCoalition GetCurrentCoalition()
        {
            return coalitionBuilder?.CurrentCoalition;
        }

        /// <summary>
        /// Set election result data.
        /// </summary>
        /// <param name="electionResult">Election result to load</param>
        public void SetElectionResult(ElectionResult electionResult)
        {
            currentElectionResult = electionResult;

            if (coalitionBuilder != null)
            {
                coalitionBuilder.SetElectionResult(electionResult);
            }

            UpdateStatusDisplay();
        }

        /// <summary>
        /// Clear the current coalition.
        /// </summary>
        public void ClearCoalition()
        {
            coalitionBuilder?.ClearCoalition();
        }

        #endregion

#if UNITY_EDITOR
        [ContextMenu("Test Suggest Coalition")]
        private void TestSuggestCoalition()
        {
            SuggestCoalition();
        }

        [ContextMenu("Test Show Tutorial")]
        private void TestShowTutorial()
        {
            ShowTutorial();
        }

        [ContextMenu("Test Save Coalition")]
        private void TestSaveCoalition()
        {
            SaveCoalition();
        }

        [ContextMenu("Debug Coalition Builder Status")]
        private void DebugCoalitionBuilderStatus()
        {
            if (coalitionBuilder != null)
            {
                Debug.Log($"Coalition Builder Status:\n" +
                         $"  Total Seats: {coalitionBuilder.TotalCoalitionSeats}/150\n" +
                         $"  Has Majority: {coalitionBuilder.HasMajority}\n" +
                         $"  Compatibility: {coalitionBuilder.CompatibilityScore:F2}\n" +
                         $"  Parties: {currentCoalition?.PartyNames?.Count ?? 0}");
            }
        }
#endif
    }

    /// <summary>
    /// Simple progress bar component.
    /// </summary>
    [System.Serializable]
    public class ProgressBar
    {
        [SerializeField] private Image fillImage;
        [SerializeField] private Image backgroundImage;

        public void SetProgress(float progress)
        {
            if (fillImage != null)
                fillImage.fillAmount = Mathf.Clamp01(progress);
        }

        public void SetColor(Color color)
        {
            if (fillImage != null)
                fillImage.color = color;
        }
    }
}