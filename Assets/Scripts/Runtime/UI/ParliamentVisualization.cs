using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Coalition.Runtime.Core;
using Coalition.Runtime.Data;

namespace Coalition.Runtime.UI
{
    /// <summary>
    /// Accurate visualization of the Dutch Tweede Kamer (150 seats) in semicircle layout.
    /// Provides interactive seat display with party colors, hover information, and zoom functionality.
    /// </summary>
    public class ParliamentVisualization : UIPanel, IPointerClickHandler, IScrollHandler
    {
        [Header("Parliament Configuration")]
        [SerializeField] private int totalSeats = 150;
        [SerializeField] private float semicircleRadius = 300f;
        [SerializeField] private float innerRadius = 180f;
        [SerializeField] private int rowCount = 8; // Typical Tweede Kamer row configuration

        [Header("Seat Visualization")]
        [SerializeField] private GameObject seatPrefab;
        [SerializeField] private float seatSize = 12f;
        [SerializeField] private float seatSpacing = 2f;
        [SerializeField] private Color emptySeatColor = Color.gray;
        [SerializeField] private Color hoveredSeatColor = Color.white;

        [Header("Zoom and Navigation")]
        [SerializeField] private float minZoom = 0.5f;
        [SerializeField] private float maxZoom = 3f;
        [SerializeField] private float zoomSpeed = 0.1f;
        [SerializeField] private bool enablePanning = true;

        [Header("Information Display")]
        [SerializeField] private RectTransform infoPanel;
        [SerializeField] private Text partyNameText;
        [SerializeField] private Text seatCountText;
        [SerializeField] private Text memberNameText;
        [SerializeField] private Image partyColorImage;

        [Header("Coalition Highlighting")]
        [SerializeField] private bool highlightCoalition = true;
        [SerializeField] private float coalitionGlowIntensity = 1.5f;
        [SerializeField] private Color coalitionBorderColor = Color.yellow;

        // Parliament data
        private List<ParliamentSeat> parliamentSeats;
        private Dictionary<string, List<ParliamentSeat>> partySeats;
        private ElectionResult currentElectionResult;
        private List<string> coalitionParties;

        // Visualization components
        private RectTransform parliamentContainer;
        private Canvas parliamentCanvas;
        private ScrollRect scrollRect;
        private ParliamentSeat hoveredSeat;
        private UITheme currentTheme;

        // Zoom and pan state
        private float currentZoom = 1f;
        private Vector2 panOffset = Vector2.zero;
        private bool isPanning = false;
        private Vector2 lastPanPosition;

        // Performance optimization
        private bool isDirty = true;
        private float lastUpdateTime;
        private const float UpdateThreshold = 0.016f; // ~60 FPS

        #region Properties

        public ElectionResult ElectionResult
        {
            get => currentElectionResult;
            set => SetElectionResult(value);
        }

        public List<string> CoalitionParties
        {
            get => coalitionParties;
            set => SetCoalitionParties(value);
        }

        public float ZoomLevel
        {
            get => currentZoom;
            set => SetZoom(value);
        }

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            InitializeParliamentVisualization();
        }

        protected override void Start()
        {
            base.Start();
            InitializeEventSubscriptions();
            GenerateParliamentLayout();
        }

        protected override void Update()
        {
            base.Update();
            HandleInput();
            UpdateVisualization();
        }

        protected override void OnDestroy()
        {
            CleanupEventSubscriptions();
            base.OnDestroy();
        }

        #endregion

        #region Initialization

        private void InitializeParliamentVisualization()
        {
            parliamentSeats = new List<ParliamentSeat>();
            partySeats = new Dictionary<string, List<ParliamentSeat>>();
            coalitionParties = new List<string>();

            // Create parliament container
            var containerObject = new GameObject("ParliamentContainer");
            containerObject.transform.SetParent(transform, false);
            parliamentContainer = containerObject.AddComponent<RectTransform>();
            parliamentContainer.anchorMin = Vector2.zero;
            parliamentContainer.anchorMax = Vector2.one;
            parliamentContainer.offsetMin = Vector2.zero;
            parliamentContainer.offsetMax = Vector2.zero;

            // Setup scroll rect for zoom/pan
            scrollRect = gameObject.AddComponent<ScrollRect>();
            scrollRect.content = parliamentContainer;
            scrollRect.horizontal = true;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Unrestricted;

            Debug.Log("[ParliamentVisualization] Parliament visualization initialized");
        }

        private void InitializeEventSubscriptions()
        {
            EventBus.Subscribe<ElectionResultsUpdatedEvent>(OnElectionResultsUpdated);
            EventBus.Subscribe<CoalitionFormedEvent>(OnCoalitionFormed);
            EventBus.Subscribe<PartyDataUpdatedEvent>(OnPartyDataUpdated);
        }

        private void CleanupEventSubscriptions()
        {
            EventBus.Unsubscribe<ElectionResultsUpdatedEvent>(OnElectionResultsUpdated);
            EventBus.Unsubscribe<CoalitionFormedEvent>(OnCoalitionFormed);
            EventBus.Unsubscribe<PartyDataUpdatedEvent>(OnPartyDataUpdated);
        }

        #endregion

        #region Parliament Layout Generation

        private void GenerateParliamentLayout()
        {
            ClearExistingSeats();

            var seatPositions = CalculateSeatPositions();
            CreateSeats(seatPositions);

            Debug.Log($"[ParliamentVisualization] Generated parliament layout with {seatPositions.Count} seats");
        }

        private List<Vector2> CalculateSeatPositions()
        {
            var positions = new List<Vector2>();
            var seatsPerRow = CalculateSeatsPerRow();

            for (int row = 0; row < rowCount; row++)
            {
                var rowRadius = innerRadius + (row * (semicircleRadius - innerRadius) / (rowCount - 1));
                var seatsInThisRow = seatsPerRow[row];
                var angleStep = Mathf.PI / (seatsInThisRow - 1); // Semicircle spans π radians
                var startAngle = 0f; // Start from right side of semicircle

                for (int seat = 0; seat < seatsInThisRow; seat++)
                {
                    var angle = startAngle + (seat * angleStep);
                    var x = rowRadius * Mathf.Cos(angle);
                    var y = rowRadius * Mathf.Sin(angle);

                    positions.Add(new Vector2(x, y));
                }
            }

            // Ensure we have exactly 150 seats
            while (positions.Count < totalSeats)
            {
                // Add extra seats to outer rows if needed
                var lastRowRadius = semicircleRadius;
                var extraSeats = totalSeats - positions.Count;
                var angleStep = Mathf.PI / (extraSeats + 1);

                for (int i = 0; i < extraSeats; i++)
                {
                    var angle = angleStep * (i + 1);
                    var x = lastRowRadius * Mathf.Cos(angle);
                    var y = lastRowRadius * Mathf.Sin(angle);
                    positions.Add(new Vector2(x, y));
                }
            }

            // Remove excess seats if any
            while (positions.Count > totalSeats)
            {
                positions.RemoveAt(positions.Count - 1);
            }

            return positions;
        }

        private List<int> CalculateSeatsPerRow()
        {
            var seatsPerRow = new List<int>();
            var remainingSeats = totalSeats;

            // Distribute seats across rows (more in outer rows)
            for (int row = 0; row < rowCount; row++)
            {
                var proportion = (float)(row + 1) / rowCount; // Outer rows get more seats
                var baseSeats = 12 + (int)(proportion * 8); // Range from ~12 to ~20 seats per row

                // Adjust for remaining seats
                if (row == rowCount - 1)
                {
                    seatsPerRow.Add(remainingSeats);
                }
                else
                {
                    var seatsInRow = Mathf.Min(baseSeats, remainingSeats);
                    seatsPerRow.Add(seatsInRow);
                    remainingSeats -= seatsInRow;
                }
            }

            return seatsPerRow;
        }

        private void CreateSeats(List<Vector2> positions)
        {
            if (seatPrefab == null)
            {
                Debug.LogError("[ParliamentVisualization] Seat prefab not assigned");
                return;
            }

            for (int i = 0; i < positions.Count; i++)
            {
                var seatObject = Instantiate(seatPrefab, parliamentContainer);
                var seatRectTransform = seatObject.GetComponent<RectTransform>();

                if (seatRectTransform != null)
                {
                    seatRectTransform.anchoredPosition = positions[i];
                    seatRectTransform.sizeDelta = Vector2.one * seatSize;
                }

                var parliamentSeat = seatObject.GetComponent<ParliamentSeat>();
                if (parliamentSeat == null)
                {
                    parliamentSeat = seatObject.AddComponent<ParliamentSeat>();
                }

                parliamentSeat.Initialize(i, this);
                parliamentSeats.Add(parliamentSeat);
            }

            isDirty = true;
        }

        private void ClearExistingSeats()
        {
            foreach (var seat in parliamentSeats)
            {
                if (seat != null && seat.gameObject != null)
                {
                    Destroy(seat.gameObject);
                }
            }

            parliamentSeats.Clear();
            partySeats.Clear();
        }

        #endregion

        #region Election Data Integration

        public void SetElectionResult(ElectionResult electionResult)
        {
            currentElectionResult = electionResult;
            UpdateSeatAssignments();
            isDirty = true;

            Debug.Log($"[ParliamentVisualization] Updated election result with {electionResult?.PartyResults?.Count ?? 0} parties");
        }

        private void UpdateSeatAssignments()
        {
            if (currentElectionResult == null) return;

            // Clear previous assignments
            foreach (var seat in parliamentSeats)
            {
                seat.AssignedParty = null;
                seat.SetColor(emptySeatColor);
            }

            partySeats.Clear();

            // Assign seats based on election results
            var seatIndex = 0;
            foreach (var partyResult in currentElectionResult.PartyResults.OrderByDescending(p => p.Seats))
            {
                var partyName = partyResult.PartyName;
                var seatCount = partyResult.Seats;

                if (!partySeats.ContainsKey(partyName))
                {
                    partySeats[partyName] = new List<ParliamentSeat>();
                }

                // Assign consecutive seats to each party
                for (int i = 0; i < seatCount && seatIndex < parliamentSeats.Count; i++)
                {
                    var seat = parliamentSeats[seatIndex];
                    seat.AssignedParty = partyName;

                    // Get party color from theme or use default
                    var partyColor = GetPartyColor(partyName);
                    seat.SetColor(partyColor);

                    partySeats[partyName].Add(seat);
                    seatIndex++;
                }
            }

            UpdateCoalitionHighlighting();
        }

        private Color GetPartyColor(string partyName)
        {
            if (currentTheme != null)
            {
                return currentTheme.GetPartyColor(partyName);
            }

            // Fallback colors
            var colorMap = new Dictionary<string, Color>
            {
                ["VVD"] = new Color(0.0f, 0.4f, 0.8f, 1f),
                ["PVV"] = new Color(1.0f, 0.8f, 0.0f, 1f),
                ["NSC"] = new Color(0.0f, 0.6f, 0.4f, 1f),
                ["GL-PvdA"] = new Color(0.6f, 0.8f, 0.2f, 1f),
                ["D66"] = new Color(0.0f, 0.8f, 0.2f, 1f),
                ["BBB"] = new Color(1.0f, 0.6f, 0.0f, 1f),
                ["CDA"] = new Color(0.2f, 0.6f, 0.8f, 1f),
                ["SP"] = new Color(0.8f, 0.2f, 0.2f, 1f),
                ["FvD"] = new Color(0.5f, 0.3f, 0.7f, 1f),
                ["CU"] = new Color(0.3f, 0.5f, 0.8f, 1f),
                ["SGP"] = new Color(1.0f, 0.4f, 0.0f, 1f),
                ["Volt"] = new Color(0.8f, 0.0f, 0.8f, 1f)
            };

            return colorMap.GetValueOrDefault(partyName, Color.gray);
        }

        #endregion

        #region Coalition Management

        public void SetCoalitionParties(List<string> parties)
        {
            coalitionParties = new List<string>(parties ?? new List<string>());
            UpdateCoalitionHighlighting();
            isDirty = true;

            Debug.Log($"[ParliamentVisualization] Updated coalition with {coalitionParties.Count} parties");
        }

        private void UpdateCoalitionHighlighting()
        {
            if (!highlightCoalition) return;

            foreach (var seat in parliamentSeats)
            {
                var isCoalitionSeat = coalitionParties.Contains(seat.AssignedParty);
                seat.SetCoalitionStatus(isCoalitionSeat, coalitionBorderColor, coalitionGlowIntensity);
            }
        }

        #endregion

        #region Interaction Handling

        public void OnPointerClick(PointerEventData eventData)
        {
            // Handle clicks on parliament background
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (eventData.scrollDelta.y != 0)
            {
                var zoomDelta = eventData.scrollDelta.y * zoomSpeed;
                SetZoom(currentZoom + zoomDelta);
            }
        }

        public void OnSeatHovered(ParliamentSeat seat, bool isHovered)
        {
            if (isHovered)
            {
                hoveredSeat = seat;
                ShowSeatInformation(seat);
            }
            else if (hoveredSeat == seat)
            {
                hoveredSeat = null;
                HideSeatInformation();
            }
        }

        public void OnSeatClicked(ParliamentSeat seat)
        {
            if (seat.AssignedParty != null)
            {
                Debug.Log($"[ParliamentVisualization] Clicked seat assigned to: {seat.AssignedParty}");

                // Publish seat click event
                EventBus.Publish(new ParliamentSeatClickedEvent
                {
                    SeatIndex = seat.SeatIndex,
                    PartyName = seat.AssignedParty
                });
            }
        }

        #endregion

        #region Information Display

        private void ShowSeatInformation(ParliamentSeat seat)
        {
            if (infoPanel == null) return;

            infoPanel.gameObject.SetActive(true);

            if (seat.AssignedParty != null)
            {
                // Show party information
                if (partyNameText != null)
                    partyNameText.text = seat.AssignedParty;

                if (seatCountText != null && partySeats.ContainsKey(seat.AssignedParty))
                    seatCountText.text = $"{partySeats[seat.AssignedParty].Count} seats";

                if (partyColorImage != null)
                    partyColorImage.color = GetPartyColor(seat.AssignedParty);

                // Show member name (placeholder - could be populated from actual data)
                if (memberNameText != null)
                    memberNameText.text = $"Member {seat.SeatIndex + 1}";
            }
            else
            {
                // Show empty seat information
                if (partyNameText != null)
                    partyNameText.text = "Empty Seat";

                if (seatCountText != null)
                    seatCountText.text = "";

                if (memberNameText != null)
                    memberNameText.text = "";

                if (partyColorImage != null)
                    partyColorImage.color = emptySeatColor;
            }
        }

        private void HideSeatInformation()
        {
            if (infoPanel != null)
            {
                infoPanel.gameObject.SetActive(false);
            }
        }

        #endregion

        #region Zoom and Pan

        public void SetZoom(float zoom)
        {
            currentZoom = Mathf.Clamp(zoom, minZoom, maxZoom);
            UpdateZoom();
        }

        private void UpdateZoom()
        {
            if (parliamentContainer != null)
            {
                parliamentContainer.localScale = Vector3.one * currentZoom;
            }
        }

        private void HandleInput()
        {
            // Handle pan input
            if (enablePanning)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    isPanning = true;
                    lastPanPosition = Input.mousePosition;
                }
                else if (Input.GetMouseButtonUp(0))
                {
                    isPanning = false;
                }
                else if (isPanning && Input.GetMouseButton(0))
                {
                    var currentPosition = (Vector2)Input.mousePosition;
                    var deltaPosition = (currentPosition - lastPanPosition) / currentZoom;
                    panOffset += deltaPosition;

                    if (parliamentContainer != null)
                    {
                        parliamentContainer.anchoredPosition = panOffset;
                    }

                    lastPanPosition = currentPosition;
                }
            }

            // Handle keyboard zoom
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                if (Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.Equals))
                {
                    SetZoom(currentZoom + zoomSpeed * 5);
                }
                else if (Input.GetKeyDown(KeyCode.Minus))
                {
                    SetZoom(currentZoom - zoomSpeed * 5);
                }
                else if (Input.GetKeyDown(KeyCode.Alpha0))
                {
                    ResetView();
                }
            }
        }

        public void ResetView()
        {
            currentZoom = 1f;
            panOffset = Vector2.zero;
            UpdateZoom();

            if (parliamentContainer != null)
            {
                parliamentContainer.anchoredPosition = Vector2.zero;
            }
        }

        #endregion

        #region Visualization Updates

        private void UpdateVisualization()
        {
            if (!isDirty || Time.time - lastUpdateTime < UpdateThreshold) return;

            lastUpdateTime = Time.time;
            isDirty = false;

            // Update seat visuals based on current state
            foreach (var seat in parliamentSeats)
            {
                seat.UpdateVisual();
            }
        }

        #endregion

        #region Theme Support

        public override void ApplyTheme(UITheme theme)
        {
            base.ApplyTheme(theme);
            currentTheme = theme;

            if (theme == null) return;

            // Update seat colors with theme
            UpdateSeatAssignments();

            // Update info panel styling
            if (infoPanel != null)
            {
                var panelImage = infoPanel.GetComponent<Image>();
                if (panelImage != null)
                {
                    panelImage.color = theme.PanelBackgroundColor;
                }
            }

            // Update text colors
            var textComponents = new[] { partyNameText, seatCountText, memberNameText };
            foreach (var text in textComponents)
            {
                if (text != null)
                {
                    text.color = theme.TextColor;
                    if (theme.Font != null)
                        text.font = theme.Font;
                }
            }

            isDirty = true;
        }

        #endregion

        #region Event Handlers

        private void OnElectionResultsUpdated(ElectionResultsUpdatedEvent eventData)
        {
            SetElectionResult(eventData.ElectionResult);
        }

        private void OnCoalitionFormed(CoalitionFormedEvent eventData)
        {
            var partyNames = eventData.Coalition?.PartyNames ?? new List<string>();
            SetCoalitionParties(partyNames);
        }

        private void OnPartyDataUpdated(PartyDataUpdatedEvent eventData)
        {
            // Refresh visualization when party data changes
            isDirty = true;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get seat count by party.
        /// </summary>
        /// <param name="partyName">Party name</param>
        /// <returns>Number of seats</returns>
        public int GetPartySeatCount(string partyName)
        {
            return partySeats.GetValueOrDefault(partyName)?.Count ?? 0;
        }

        /// <summary>
        /// Get total coalition seat count.
        /// </summary>
        /// <returns>Coalition seat count</returns>
        public int GetCoalitionSeatCount()
        {
            return coalitionParties.Sum(party => GetPartySeatCount(party));
        }

        /// <summary>
        /// Check if current coalition has majority.
        /// </summary>
        /// <returns>True if coalition has majority (76+ seats)</returns>
        public bool HasMajority()
        {
            return GetCoalitionSeatCount() >= 76;
        }

        /// <summary>
        /// Get parliament statistics.
        /// </summary>
        /// <returns>Parliament statistics</returns>
        public ParliamentStats GetStats()
        {
            return new ParliamentStats
            {
                TotalSeats = totalSeats,
                OccupiedSeats = parliamentSeats.Count(s => s.AssignedParty != null),
                CoalitionSeats = GetCoalitionSeatCount(),
                HasMajority = HasMajority(),
                PartyCount = partySeats.Count,
                ZoomLevel = currentZoom
            };
        }

        #endregion

#if UNITY_EDITOR
        [ContextMenu("Debug Parliament Status")]
        private void DebugParliamentStatus()
        {
            var stats = GetStats();
            Debug.Log($"Parliament Status:\n" +
                     $"  Total Seats: {stats.TotalSeats}\n" +
                     $"  Occupied: {stats.OccupiedSeats}\n" +
                     $"  Coalition: {stats.CoalitionSeats}\n" +
                     $"  Has Majority: {stats.HasMajority}\n" +
                     $"  Parties: {stats.PartyCount}\n" +
                     $"  Zoom: {stats.ZoomLevel:F2}");
        }

        [ContextMenu("Reset View")]
        private void TestResetView()
        {
            ResetView();
        }

        [ContextMenu("Regenerate Layout")]
        private void TestRegenerateLayout()
        {
            GenerateParliamentLayout();
        }
#endif
    }

    /// <summary>
    /// Parliament statistics data structure.
    /// </summary>
    [System.Serializable]
    public struct ParliamentStats
    {
        public int TotalSeats;
        public int OccupiedSeats;
        public int CoalitionSeats;
        public bool HasMajority;
        public int PartyCount;
        public float ZoomLevel;
    }

    /// <summary>
    /// Event published when a parliament seat is clicked.
    /// </summary>
    public class ParliamentSeatClickedEvent
    {
        public int SeatIndex { get; set; }
        public string PartyName { get; set; }
        public System.DateTime Timestamp { get; } = System.DateTime.Now;
    }

    /// <summary>
    /// Event published when party data is updated.
    /// </summary>
    public class PartyDataUpdatedEvent
    {
        public string PartyName { get; set; }
        public System.DateTime Timestamp { get; } = System.DateTime.Now;
    }
}