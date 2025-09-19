using UnityEngine;
using UnityEngine.UI;
using Coalition.Runtime.Core;
using Coalition.Runtime.Data;

namespace Coalition.Runtime.UI
{
    /// <summary>
    /// Parliament visualization window for the desktop environment.
    /// Contains the 150-seat Tweede Kamer display with election results and coalition highlighting.
    /// </summary>
    public class ParliamentWindow : Window
    {
        [Header("Parliament Window Components")]
        [SerializeField] private ParliamentVisualization parliamentVisualization;
        [SerializeField] private RectTransform controlsPanel;
        [SerializeField] private Button resetViewButton;
        [SerializeField] private Button toggleSeatNumbersButton;
        [SerializeField] private Slider zoomSlider;
        [SerializeField] private Toggle coalitionHighlightToggle;

        [Header("Information Panel")]
        [SerializeField] private RectTransform infoPanel;
        [SerializeField] private Text totalSeatsText;
        [SerializeField] private Text coalitionSeatsText;
        [SerializeField] private Text majorityStatusText;
        [SerializeField] private Text selectedPartyText;

        [Header("Legend")]
        [SerializeField] private RectTransform legendContainer;
        [SerializeField] private GameObject partyLegendItemPrefab;
        [SerializeField] private bool showLegend = true;

        // Parliament data
        private ElectionResult currentElectionResult;
        private Coalition currentCoalition;
        private bool showSeatNumbers = false;

        // Legend management
        private System.Collections.Generic.List<PartyLegendItem> legendItems;

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            InitializeParliamentWindow();
        }

        protected override void Start()
        {
            base.Start();
            SetupEventHandlers();
            InitializeEventSubscriptions();
            UpdateWindowDisplay();
        }

        protected override void OnDestroy()
        {
            CleanupEventSubscriptions();
            base.OnDestroy();
        }

        #endregion

        #region Initialization

        private void InitializeParliamentWindow()
        {
            legendItems = new System.Collections.Generic.List<PartyLegendItem>();

            // Set default window properties
            WindowTitle = "Parliament - Tweede Kamer";

            // Find parliament visualization if not assigned
            if (parliamentVisualization == null)
                parliamentVisualization = GetComponentInChildren<ParliamentVisualization>();

            Debug.Log("[ParliamentWindow] Parliament window initialized");
        }

        private void SetupEventHandlers()
        {
            if (resetViewButton != null)
                resetViewButton.onClick.AddListener(ResetView);

            if (toggleSeatNumbersButton != null)
                toggleSeatNumbersButton.onClick.AddListener(ToggleSeatNumbers);

            if (zoomSlider != null)
                zoomSlider.onValueChanged.AddListener(OnZoomChanged);

            if (coalitionHighlightToggle != null)
                coalitionHighlightToggle.onValueChanged.AddListener(OnCoalitionHighlightToggled);
        }

        private void InitializeEventSubscriptions()
        {
            EventBus.Subscribe<ElectionResultsUpdatedEvent>(OnElectionResultsUpdated);
            EventBus.Subscribe<CoalitionChangedEvent>(OnCoalitionChanged);
            EventBus.Subscribe<ParliamentSeatClickedEvent>(OnParliamentSeatClicked);
            EventBus.Subscribe<GovernmentFormedEvent>(OnGovernmentFormed);
        }

        private void CleanupEventSubscriptions()
        {
            EventBus.Unsubscribe<ElectionResultsUpdatedEvent>(OnElectionResultsUpdated);
            EventBus.Unsubscribe<CoalitionChangedEvent>(OnCoalitionChanged);
            EventBus.Unsubscribe<ParliamentSeatClickedEvent>(OnParliamentSeatClicked);
            EventBus.Unsubscribe<GovernmentFormedEvent>(OnGovernmentFormed);
        }

        #endregion

        #region Window Management

        protected override void OnOpenComplete()
        {
            base.OnOpenComplete();

            // Refresh parliament visualization when window opens
            if (parliamentVisualization != null)
            {
                parliamentVisualization.ResetView();
            }

            UpdateWindowDisplay();
        }

        private void UpdateWindowDisplay()
        {
            UpdateParliamentInfo();
            UpdateLegend();
            UpdateControls();
        }

        #endregion

        #region Parliament Integration

        private void UpdateParliamentInfo()
        {
            if (parliamentVisualization == null) return;

            var stats = parliamentVisualization.GetStats();

            // Update seat counts
            if (totalSeatsText != null)
                totalSeatsText.text = $"Total Seats: {stats.TotalSeats}";

            if (coalitionSeatsText != null)
                coalitionSeatsText.text = $"Coalition: {stats.CoalitionSeats}";

            // Update majority status
            if (majorityStatusText != null)
            {
                var majorityStatus = stats.HasMajority ? "MAJORITY" : "MINORITY";
                var seatsNeeded = stats.HasMajority ? 0 : (76 - stats.CoalitionSeats);

                majorityStatusText.text = stats.HasMajority
                    ? $"{majorityStatus} (+{stats.CoalitionSeats - 75})"
                    : $"{majorityStatus} (need {seatsNeeded} more)";

                majorityStatusText.color = stats.HasMajority ? Color.green : Color.red;
            }
        }

        private void UpdateLegend()
        {
            if (!showLegend || legendContainer == null || currentElectionResult == null) return;

            // Clear existing legend items
            ClearLegend();

            // Create legend items for each party
            foreach (var partyResult in currentElectionResult.PartyResults)
            {
                CreateLegendItem(partyResult);
            }
        }

        private void ClearLegend()
        {
            foreach (var item in legendItems)
            {
                if (item != null && item.gameObject != null)
                    Destroy(item.gameObject);
            }
            legendItems.Clear();
        }

        private void CreateLegendItem(PartyResult partyResult)
        {
            if (partyLegendItemPrefab == null) return;

            var itemObject = Instantiate(partyLegendItemPrefab, legendContainer);
            var legendItem = itemObject.GetComponent<PartyLegendItem>();

            if (legendItem == null)
            {
                legendItem = itemObject.AddComponent<PartyLegendItem>();
            }

            legendItem.Initialize(partyResult, GetPartyColor(partyResult.PartyName));
            legendItems.Add(legendItem);
        }

        private Color GetPartyColor(string partyName)
        {
            var theme = uiManager?.GetCurrentTheme();
            return theme?.GetPartyColor(partyName) ?? Color.gray;
        }

        #endregion

        #region Controls

        private void UpdateControls()
        {
            // Update zoom slider
            if (zoomSlider != null && parliamentVisualization != null)
            {
                zoomSlider.value = parliamentVisualization.ZoomLevel;
            }

            // Update toggle states
            if (coalitionHighlightToggle != null)
            {
                coalitionHighlightToggle.isOn = currentCoalition?.PartyNames?.Count > 0;
            }
        }

        private void ResetView()
        {
            if (parliamentVisualization != null)
            {
                parliamentVisualization.ResetView();

                if (zoomSlider != null)
                    zoomSlider.value = 1f;
            }

            Debug.Log("[ParliamentWindow] Reset parliament view");
        }

        private void ToggleSeatNumbers()
        {
            showSeatNumbers = !showSeatNumbers;

            // Update all parliament seats to show/hide numbers
            if (parliamentVisualization != null)
            {
                var seats = FindObjectsOfType<ParliamentSeat>();
                foreach (var seat in seats)
                {
                    seat.ShowSeatNumber(showSeatNumbers);
                }
            }

            // Update button text
            if (toggleSeatNumbersButton != null)
            {
                var buttonText = toggleSeatNumbersButton.GetComponentInChildren<Text>();
                if (buttonText != null)
                {
                    buttonText.text = showSeatNumbers ? "Hide Numbers" : "Show Numbers";
                }
            }

            Debug.Log($"[ParliamentWindow] Toggled seat numbers: {showSeatNumbers}");
        }

        private void OnZoomChanged(float zoomLevel)
        {
            if (parliamentVisualization != null)
            {
                parliamentVisualization.ZoomLevel = zoomLevel;
            }
        }

        private void OnCoalitionHighlightToggled(bool enabled)
        {
            // This could control coalition highlighting visibility
            // For now, coalition highlighting is automatic when coalition exists
            Debug.Log($"[ParliamentWindow] Coalition highlighting: {enabled}");
        }

        #endregion

        #region Event Handlers

        private void OnElectionResultsUpdated(ElectionResultsUpdatedEvent eventData)
        {
            currentElectionResult = eventData.ElectionResult;

            if (parliamentVisualization != null)
            {
                parliamentVisualization.ElectionResult = currentElectionResult;
            }

            UpdateWindowDisplay();

            Debug.Log($"[ParliamentWindow] Updated with election results: {currentElectionResult.PartyResults.Count} parties");
        }

        private void OnCoalitionChanged(CoalitionChangedEvent eventData)
        {
            currentCoalition = eventData.Coalition;

            if (parliamentVisualization != null)
            {
                parliamentVisualization.CoalitionParties = currentCoalition?.PartyNames ?? new System.Collections.Generic.List<string>();
            }

            UpdateWindowDisplay();

            Debug.Log($"[ParliamentWindow] Coalition changed: {currentCoalition?.PartyNames?.Count ?? 0} parties");
        }

        private void OnParliamentSeatClicked(ParliamentSeatClickedEvent eventData)
        {
            if (selectedPartyText != null)
            {
                selectedPartyText.text = $"Selected: {eventData.PartyName ?? "Empty Seat"}";
            }

            // Flash the window to indicate interaction
            FlashButton();

            Debug.Log($"[ParliamentWindow] Seat clicked: {eventData.PartyName} (Index: {eventData.SeatIndex})");
        }

        private void OnGovernmentFormed(GovernmentFormedEvent eventData)
        {
            // Update window title to reflect government formation
            WindowTitle = $"Parliament - Government Formed ({eventData.FormationDate:MMM dd})";

            // Flash the window to celebrate
            FlashButton();

            Debug.Log($"[ParliamentWindow] Government formed with {eventData.Coalition.PartyNames.Count} parties");
        }

        #endregion

        #region Theme Support

        public override void ApplyTheme(UITheme theme)
        {
            base.ApplyTheme(theme);

            if (theme == null) return;

            // Apply theme to parliament visualization
            if (parliamentVisualization != null)
            {
                parliamentVisualization.ApplyTheme(theme);
            }

            // Apply theme to controls
            ApplyThemeToControls(theme);

            // Apply theme to info panel
            ApplyThemeToInfoPanel(theme);

            // Update legend with new theme
            UpdateLegend();
        }

        private void ApplyThemeToControls(UITheme theme)
        {
            // Update buttons
            var buttons = new[] { resetViewButton, toggleSeatNumbersButton };
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

            // Update slider
            if (zoomSlider != null)
            {
                var fillImage = zoomSlider.fillRect?.GetComponent<Image>();
                if (fillImage != null)
                    fillImage.color = theme.AccentColor;
            }
        }

        private void ApplyThemeToInfoPanel(UITheme theme)
        {
            var textComponents = new[] { totalSeatsText, coalitionSeatsText, selectedPartyText };
            foreach (var text in textComponents)
            {
                if (text != null)
                {
                    text.color = theme.TextColor;
                    if (theme.Font != null)
                        text.font = theme.Font;
                }
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Focus on a specific party's seats in the parliament.
        /// </summary>
        /// <param name="partyName">Party name to focus on</param>
        public void FocusOnParty(string partyName)
        {
            if (parliamentVisualization == null) return;

            // This could highlight specific party seats or zoom to their area
            if (selectedPartyText != null)
            {
                selectedPartyText.text = $"Focused: {partyName}";
            }

            Debug.Log($"[ParliamentWindow] Focused on party: {partyName}");
        }

        /// <summary>
        /// Set the election result to display.
        /// </summary>
        /// <param name="electionResult">Election result data</param>
        public void SetElectionResult(ElectionResult electionResult)
        {
            currentElectionResult = electionResult;

            if (parliamentVisualization != null)
            {
                parliamentVisualization.ElectionResult = electionResult;
            }

            UpdateWindowDisplay();
        }

        /// <summary>
        /// Set the coalition to highlight.
        /// </summary>
        /// <param name="coalition">Coalition data</param>
        public void SetCoalition(Coalition coalition)
        {
            currentCoalition = coalition;

            if (parliamentVisualization != null)
            {
                parliamentVisualization.CoalitionParties = coalition?.PartyNames ?? new System.Collections.Generic.List<string>();
            }

            UpdateWindowDisplay();
        }

        /// <summary>
        /// Get parliament statistics.
        /// </summary>
        /// <returns>Parliament statistics</returns>
        public ParliamentStats GetParliamentStats()
        {
            return parliamentVisualization?.GetStats() ?? new ParliamentStats();
        }

        #endregion

#if UNITY_EDITOR
        [ContextMenu("Test Focus Random Party")]
        private void TestFocusRandomParty()
        {
            if (currentElectionResult?.PartyResults?.Count > 0)
            {
                var randomIndex = UnityEngine.Random.Range(0, currentElectionResult.PartyResults.Count);
                var randomParty = currentElectionResult.PartyResults[randomIndex].PartyName;
                FocusOnParty(randomParty);
            }
        }

        [ContextMenu("Toggle Seat Numbers")]
        private void TestToggleSeatNumbers()
        {
            ToggleSeatNumbers();
        }

        [ContextMenu("Reset View")]
        private void TestResetView()
        {
            ResetView();
        }

        [ContextMenu("Debug Parliament Stats")]
        private void DebugParliamentStats()
        {
            var stats = GetParliamentStats();
            Debug.Log($"Parliament Stats:\n" +
                     $"  Total Seats: {stats.TotalSeats}\n" +
                     $"  Occupied: {stats.OccupiedSeats}\n" +
                     $"  Coalition: {stats.CoalitionSeats}\n" +
                     $"  Has Majority: {stats.HasMajority}\n" +
                     $"  Parties: {stats.PartyCount}");
        }
#endif
    }

    /// <summary>
    /// Simple party legend item component.
    /// </summary>
    public class PartyLegendItem : MonoBehaviour
    {
        [SerializeField] private Text partyNameText;
        [SerializeField] private Text seatCountText;
        [SerializeField] private Image colorIndicator;

        public void Initialize(PartyResult partyResult, Color partyColor)
        {
            if (partyNameText != null)
                partyNameText.text = partyResult.PartyName;

            if (seatCountText != null)
                seatCountText.text = partyResult.Seats.ToString();

            if (colorIndicator != null)
                colorIndicator.color = partyColor;
        }

        public void ApplyTheme(UITheme theme)
        {
            if (theme == null) return;

            var textComponents = new[] { partyNameText, seatCountText };
            foreach (var text in textComponents)
            {
                if (text != null)
                {
                    text.color = theme.TextColor;
                    if (theme.Font != null)
                        text.font = theme.Font;
                }
            }
        }
    }
}