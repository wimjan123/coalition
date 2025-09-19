using UnityEngine;
using UnityEngine.UIElements;
using COALITION.Core;

namespace COALITION.UI
{
    /// <summary>
    /// Binds UI events from UXML to game logic systems
    /// Connects button clicks, data population, and UI state management
    /// </summary>
    public class UIEventBinder : MonoBehaviour
    {
        [Header("System References")]
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private GameManager gameManager;
        [SerializeField] private PoliticalSystem politicalSystem;

        private VisualElement rootElement;
        private Button startButton;
        private Button saveButton;
        private Button settingsButton;
        private VisualElement partyList;
        private VisualElement coalitionArea;
        private Label seatCount;
        private Label compatibilityScore;
        private Label currentPhase;

        private void Start()
        {
            InitializeUIReferences();
            BindUIEvents();
            PopulateInitialData();
        }

        private void InitializeUIReferences()
        {
            if (uiDocument == null) return;

            rootElement = uiDocument.rootVisualElement;

            // Get UI element references
            startButton = rootElement.Q<Button>("StartButton");
            saveButton = rootElement.Q<Button>("SaveButton");
            settingsButton = rootElement.Q<Button>("SettingsButton");
            partyList = rootElement.Q<VisualElement>("PartyList");
            coalitionArea = rootElement.Q<VisualElement>("CoalitionArea");
            seatCount = rootElement.Q<Label>("SeatCount");
            compatibilityScore = rootElement.Q<Label>("CompatibilityScore");
            currentPhase = rootElement.Q<Label>("CurrentPhase");
        }

        private void BindUIEvents()
        {
            // Bind button events
            if (startButton != null)
            {
                startButton.clicked += OnStartButtonClicked;
            }

            if (saveButton != null)
            {
                saveButton.clicked += OnSaveButtonClicked;
            }

            if (settingsButton != null)
            {
                settingsButton.clicked += OnSettingsButtonClicked;
            }

            // Subscribe to game events
            if (gameManager != null)
            {
                gameManager.OnPhaseChanged += UpdatePhaseDisplay;
            }
        }

        private void PopulateInitialData()
        {
            PopulatePoliticalParties();
            UpdatePhaseDisplay(GamePhase.Formation);
            UpdateCoalitionInfo();
        }

        private void PopulatePoliticalParties()
        {
            if (politicalSystem == null || partyList == null) return;

            // Clear existing party cards
            partyList.Clear();

            // Create party cards for each political party
            foreach (var party in politicalSystem.GetAllParties())
            {
                var partyCard = CreatePartyCard(party);
                partyList.Add(partyCard);
            }
        }

        private VisualElement CreatePartyCard(PoliticalParty party)
        {
            // Create the main card container
            var cardRoot = new VisualElement();
            cardRoot.AddToClassList("party-card");
            cardRoot.style.backgroundColor = new Color(party.partyColor.r, party.partyColor.g, party.partyColor.b, 0.8f);

            // Party header with name and seats
            var header = new VisualElement();
            header.AddToClassList("party-header");

            var colorIndicator = new VisualElement();
            colorIndicator.AddToClassList("party-color-indicator");
            colorIndicator.style.backgroundColor = party.partyColor;

            var nameLabel = new Label(party.partyName);
            nameLabel.AddToClassList("party-name");

            var seatLabel = new Label($"{party.currentPopularity:F1}%");
            seatLabel.AddToClassList("seat-count");

            header.Add(colorIndicator);
            header.Add(nameLabel);
            header.Add(seatLabel);

            // Ideology description
            var ideologyLabel = new Label($"{party.abbreviation} - {party.economicPosition:F1}E {party.socialPosition:F1}S");
            ideologyLabel.AddToClassList("ideology-text");

            // Action buttons
            var actionsContainer = new VisualElement();
            actionsContainer.style.flexDirection = FlexDirection.Row;
            actionsContainer.style.justifyContent = Justify.SpaceBetween;

            var detailsButton = new Button(() => ShowPartyDetails(party)) { text = "Details" };
            detailsButton.AddToClassList("button-secondary");
            detailsButton.AddToClassList("button-small");

            var addButton = new Button(() => AddToCoalition(party)) { text = "Add to Coalition" };
            addButton.AddToClassList("button-success");
            addButton.AddToClassList("button-small");

            actionsContainer.Add(detailsButton);
            actionsContainer.Add(addButton);

            // Assemble the card
            cardRoot.Add(header);
            cardRoot.Add(ideologyLabel);
            cardRoot.Add(actionsContainer);

            return cardRoot;
        }

        private void OnStartButtonClicked()
        {
            Debug.Log("Start button clicked - Beginning coalition formation");

            if (gameManager != null)
            {
                gameManager.SetGamePhase(GamePhase.Formation);
            }

            // Switch to coalition formation view
            var welcomeView = rootElement.Q<VisualElement>("WelcomeView");
            var formationView = rootElement.Q<VisualElement>("CoalitionFormationView");

            if (welcomeView != null) welcomeView.style.display = DisplayStyle.None;
            if (formationView != null) formationView.style.display = DisplayStyle.Flex;
        }

        private void OnSaveButtonClicked()
        {
            Debug.Log("Save button clicked - Saving game state");
            // Implement save functionality
        }

        private void OnSettingsButtonClicked()
        {
            Debug.Log("Settings button clicked - Opening settings");
            // Implement settings panel
        }

        private void ShowPartyDetails(PoliticalParty party)
        {
            Debug.Log($"Showing details for {party.partyName}");
            // Implement party details modal
        }

        private void AddToCoalition(PoliticalParty party)
        {
            Debug.Log($"Adding {party.partyName} to coalition");

            if (politicalSystem != null)
            {
                // Add party to current coalition
                politicalSystem.AddPartyToCoalition(party);
                UpdateCoalitionInfo();
            }
        }

        private void UpdatePhaseDisplay(GamePhase phase)
        {
            if (currentPhase != null)
            {
                currentPhase.text = $"Phase: {phase}";

                // Apply phase-specific styling
                currentPhase.RemoveFromClassList("phase-formation");
                currentPhase.RemoveFromClassList("phase-negotiation");
                currentPhase.RemoveFromClassList("phase-agreement");

                switch (phase)
                {
                    case GamePhase.Formation:
                        currentPhase.AddToClassList("phase-formation");
                        break;
                    case GamePhase.Negotiation:
                        currentPhase.AddToClassList("phase-negotiation");
                        break;
                    case GamePhase.Agreement:
                        currentPhase.AddToClassList("phase-agreement");
                        break;
                }
            }
        }

        private void UpdateCoalitionInfo()
        {
            if (politicalSystem == null) return;

            var currentCoalition = politicalSystem.GetCurrentCoalition();
            var totalSeats = politicalSystem.CalculateCoalitionSeats(currentCoalition);
            var compatibility = politicalSystem.CalculateCoalitionCompatibility(currentCoalition);

            if (seatCount != null)
            {
                seatCount.text = $"Total Seats: {totalSeats} / 76 needed for majority";

                // Color code based on majority status
                if (totalSeats >= 76)
                {
                    seatCount.AddToClassList("status-ready");
                }
                else
                {
                    seatCount.RemoveFromClassList("status-ready");
                }
            }

            if (compatibilityScore != null)
            {
                if (currentCoalition.Count > 1)
                {
                    compatibilityScore.text = $"Compatibility Score: {compatibility:F1}/10";

                    // Apply compatibility styling
                    compatibilityScore.RemoveFromClassList("compatibility-high");
                    compatibilityScore.RemoveFromClassList("compatibility-medium");
                    compatibilityScore.RemoveFromClassList("compatibility-low");

                    if (compatibility >= 7.0f)
                        compatibilityScore.AddToClassList("compatibility-high");
                    else if (compatibility >= 4.0f)
                        compatibilityScore.AddToClassList("compatibility-medium");
                    else
                        compatibilityScore.AddToClassList("compatibility-low");
                }
                else
                {
                    compatibilityScore.text = "Compatibility Score: N/A";
                }
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (gameManager != null)
            {
                gameManager.OnPhaseChanged -= UpdatePhaseDisplay;
            }

            if (startButton != null)
            {
                startButton.clicked -= OnStartButtonClicked;
            }

            if (saveButton != null)
            {
                saveButton.clicked -= OnSaveButtonClicked;
            }

            if (settingsButton != null)
            {
                settingsButton.clicked -= OnSettingsButtonClicked;
            }
        }
    }
}