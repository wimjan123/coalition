# COALITION Development Roadmap
## Unity 6 C# Political Simulation Project

**Version:** 1.0
**Target Platform:** Desktop (Windows, macOS, Linux)
**Unity Version:** 2023.3+ (Unity 6)
**Language:** C# 9.0+

---

## Project Overview

COALITION is a comprehensive Dutch political simulation that combines:
- **Authentic Dutch Political System**: All major parties, proportional representation, coalition formation
- **Campaign Mechanics**: Social media, debates, rallies, advertising systems
- **AI Integration**: NVIDIA NIM local LLM for dynamic political responses
- **Desktop UI Framework**: Multi-window interface with authentic desktop application feel
- **Real-time Simulation**: Public opinion, media cycles, political events

### Core Design Principles
- **Modular Architecture**: Each system can be developed and tested independently
- **Event-Driven Communication**: Loose coupling between political, campaign, and UI systems
- **Data-Driven Design**: ScriptableObjects for all political entities and parameters
- **AI-First Implementation**: Every task designed for AI agent completion
- **Incremental Demos**: Each phase produces working, demonstrable functionality

---

## Technical Architecture

### Unity 6 Framework Patterns

```csharp
// Core architectural interfaces
public interface IPoliticalSystem
{
    void ProcessElection();
    Coalition FormCoalition(List<Party> availableParties);
    void UpdatePublicOpinion(PoliticalEvent politicalEvent);
}

public interface ICampaignSystem
{
    void ExecuteCampaignAction(Party party, CampaignAction action);
    PublicOpinionEffect ProcessMediaEvent(MediaEvent mediaEvent);
    void UpdateSocialMediaMetrics();
}

public interface IAIResponseSystem
{
    Task<string> GeneratePoliticalResponse(string prompt, Party party);
    Task<List<string>> GenerateDebateResponses(DebatePrompt prompt);
    void CacheResponse(string key, string response);
}
```

### Project File Structure

```
Assets/
├── Scripts/
│   ├── Core/                    # Core game systems and managers
│   │   ├── GameManager.cs       # Main game controller
│   │   ├── EventBus.cs         # Global event system
│   │   ├── SaveSystem.cs       # Game state persistence
│   │   └── Settings/           # Game configuration
│   ├── Political/              # Political simulation logic
│   │   ├── Parties/            # Party definitions and behaviors
│   │   ├── Coalitions/         # Coalition formation logic
│   │   ├── Elections/          # Voting and election mechanics
│   │   ├── PublicOpinion/      # Opinion polling and sentiment
│   │   └── Parliament/         # Parliamentary procedures
│   ├── Campaign/               # Campaign mechanics
│   │   ├── SocialMedia/        # Twitter, Facebook, Instagram simulation
│   │   ├── Debates/            # Debate system and AI responses
│   │   ├── Rallies/            # Campaign events and crowd simulation
│   │   ├── Advertising/        # TV, radio, digital ad systems
│   │   └── MediaCycle/         # News cycle and media attention
│   ├── AI/                     # NVIDIA NIM integration
│   │   ├── NIMClient.cs        # HTTP client for NIM API
│   │   ├── ResponseCache.cs    # LLM response caching
│   │   ├── PromptTemplates/    # Political prompt engineering
│   │   └── Fallbacks/          # Offline response system
│   ├── UI/                     # Desktop UI framework
│   │   ├── WindowManager.cs    # Multi-window management
│   │   ├── DesktopShell.cs     # Desktop-style interface
│   │   ├── Panels/             # Individual UI panels
│   │   ├── Menus/              # Context menus and navigation
│   │   └── Themes/             # Visual styling system
│   └── Data/                   # ScriptableObjects and data
│       ├── Parties/            # Dutch political party data
│       ├── Issues/             # Political issues and positions
│       ├── Events/             # Scripted political events
│       ├── Campaigns/          # Campaign templates
│       └── Configurations/     # System parameters
├── Scenes/
│   ├── MainMenu.unity          # Game entry point
│   ├── PoliticalDemo.unity     # Political system demo
│   ├── CampaignDemo.unity      # Campaign mechanics demo
│   ├── AIDemo.unity            # AI integration demo
│   ├── UIDemo.unity            # Desktop UI demo
│   └── FullSimulation.unity    # Complete game integration
├── Resources/                  # Runtime loadable assets
│   ├── PartyLogos/            # Political party branding
│   ├── PoliticianPhotos/      # Leader portraits
│   ├── EventImages/           # Political event imagery
│   └── AudioClips/            # Speech and sound effects
├── StreamingAssets/           # Configuration files
│   ├── DutchParties.json      # Party definitions
│   ├── PoliticalIssues.json   # Issue positions
│   ├── EventSchedule.json     # Scripted events timeline
│   └── NIMConfig.json         # AI integration settings
└── Plugins/                   # Third-party integrations
    ├── NVIDIA_NIM/            # NIM client libraries
    └── DesktopIntegration/    # OS-specific desktop features
```

---

## Development Phases

## Phase 1: Foundation & Core Systems
**Timeline:** 2-3 weeks
**Demo Milestone:** Basic Unity project with Dutch political party data display

### 1.1 Unity Project Setup
**Complexity:** Simple
**Prerequisites:** Unity 6 installation
**Parallel Opportunity:** ✅ Can be done independently

#### Tasks:
1. **Create Unity 6 Project**
   - File → New → 3D Core project
   - Project name: "Coalition"
   - Configure for desktop platforms (Windows, macOS, Linux)
   - **Deliverable:** `Coalition.unity` project file
   - **Success Criteria:** Project opens without errors, builds successfully

2. **Install Required Packages**
   - UI Toolkit (for modern desktop UI)
   - Addressables (for dynamic content loading)
   - Newtonsoft JSON (for data serialization)
   - Unity Test Framework (for automated testing)
   - **Deliverable:** `Packages/manifest.json` with dependencies
   - **Testing:** All packages import successfully, no console errors

3. **Create Folder Structure**
   - Implement the full folder hierarchy as specified above
   - Add `.gitkeep` files to preserve empty directories
   - **Deliverable:** Complete folder structure
   - **Success Criteria:** All folders present and organized correctly

### 1.2 Core Data Architecture
**Complexity:** Medium
**Prerequisites:** Project setup completed
**Parallel Opportunity:** ✅ Can develop data classes simultaneously

#### Tasks:
1. **Political Party ScriptableObject**
   ```csharp
   // File: Assets/Scripts/Data/Parties/PoliticalParty.cs
   [CreateAssetMenu(fileName = "New Party", menuName = "Coalition/Political Party")]
   public class PoliticalParty : ScriptableObject
   {
       [Header("Basic Information")]
       public string partyName;
       public string abbreviation;
       public Color partyColor;
       public Sprite partyLogo;

       [Header("Leadership")]
       public string leaderName;
       public Sprite leaderPhoto;
       public int leaderPopularity; // 0-100

       [Header("Political Positions")]
       public List<IssuePosition> issuePositions;
       public PoliticalSpectrum economicPosition; // -10 to +10
       public PoliticalSpectrum socialPosition; // -10 to +10

       [Header("Electoral Data")]
       public float currentPollingPercentage;
       public int seatsInParliament;
       public bool isGoverningParty;
       public List<string> coalitionCompatibility; // Party abbreviations
   }
   ```
   - **Deliverable:** Complete PoliticalParty class with all Dutch parties created
   - **Success Criteria:** All 12 major Dutch parties created as ScriptableObjects

2. **Dutch Political Issues System**
   ```csharp
   // File: Assets/Scripts/Data/Issues/PoliticalIssue.cs
   [CreateAssetMenu(fileName = "New Issue", menuName = "Coalition/Political Issue")]
   public class PoliticalIssue : ScriptableObject
   {
       public string issueName;
       public string description;
       public IssueCategory category; // Economy, Social, Environment, Immigration, etc.
       public float publicImportance; // 0-100, how much the public cares
       public List<IssuePosition> partyPositions; // Where each party stands
   }
   ```
   - **Deliverable:** 20+ major Dutch political issues defined
   - **Success Criteria:** Issues cover all major Dutch political topics (immigration, climate, economy, healthcare, etc.)

3. **Event Bus System**
   ```csharp
   // File: Assets/Scripts/Core/EventBus.cs
   public static class EventBus
   {
       private static Dictionary<Type, List<object>> eventCallbacks = new();

       public static void Subscribe<T>(System.Action<T> callback) where T : IEvent
       public static void Unsubscribe<T>(System.Action<T> callback) where T : IEvent
       public static void Publish<T>(T eventData) where T : IEvent
   }

   // Example events
   public interface IEvent { }
   public struct PartyPopularityChanged : IEvent
   {
       public PoliticalParty Party;
       public float OldValue;
       public float NewValue;
   }
   ```
   - **Deliverable:** Complete event system with 10+ political event types
   - **Success Criteria:** Event system tested with sample party popularity changes

### 1.3 Game Manager and Core Systems
**Complexity:** Medium
**Prerequisites:** Data architecture completed
**Parallel Opportunity:** ❌ Depends on data architecture

#### Tasks:
1. **GameManager Implementation**
   ```csharp
   // File: Assets/Scripts/Core/GameManager.cs
   public class GameManager : MonoBehaviour
   {
       public static GameManager Instance { get; private set; }

       [Header("Game State")]
       public GamePhase currentPhase = GamePhase.PreCampaign;
       public DateTime gameDate = new DateTime(2025, 1, 1);
       public float gameSpeed = 1.0f;

       [Header("Systems")]
       public PoliticalSystemManager politicalSystem;
       public CampaignSystemManager campaignSystem;
       public AIResponseManager aiSystem;
       public UIManager uiManager;

       public void AdvanceTime(TimeSpan timespan)
       public void ChangeGamePhase(GamePhase newPhase)
       public void SaveGameState()
       public void LoadGameState()
   }
   ```
   - **Deliverable:** Complete GameManager with singleton pattern
   - **Success Criteria:** GameManager persists between scenes, manages game state

2. **Settings and Configuration System**
   ```csharp
   // File: Assets/Scripts/Core/Settings/GameSettings.cs
   [CreateAssetMenu(fileName = "Game Settings", menuName = "Coalition/Settings")]
   public class GameSettings : ScriptableObject
   {
       [Header("Simulation Parameters")]
       public float publicOpinionVolatility = 0.1f;
       public float mediaInfluenceFactor = 0.3f;
       public int daysBetweenPolls = 7;

       [Header("AI Configuration")]
       public string nimServerUrl = "http://localhost:8000";
       public int maxResponseLength = 500;
       public bool useAICaching = true;

       [Header("UI Preferences")]
       public bool enableDesktopMode = true;
       public float uiAnimationSpeed = 1.0f;
       public Theme defaultTheme;
   }
   ```
   - **Deliverable:** Comprehensive settings system with validation
   - **Success Criteria:** Settings persist between sessions, validation prevents invalid configurations

### 1.4 Foundation Demo Scene
**Complexity:** Simple
**Prerequisites:** All foundation systems completed
**Parallel Opportunity:** ❌ Requires all previous tasks

#### Tasks:
1. **Create Foundation Demo Scene**
   - Scene: `Assets/Scenes/FoundationDemo.unity`
   - Display all Dutch political parties in a scrollable list
   - Show party details (logo, leader, polling data, positions)
   - Demonstrate event system with real-time polling changes
   - **Deliverable:** Working Unity scene
   - **Success Criteria:** All 12 parties display correctly, polling data updates in real-time

2. **Basic UI Implementation**
   ```csharp
   // File: Assets/Scripts/UI/Panels/PartyListPanel.cs
   public class PartyListPanel : MonoBehaviour
   {
       [Header("UI Components")]
       public Transform partyListContainer;
       public GameObject partyItemPrefab;
       public Button refreshButton;

       private List<PoliticalParty> allParties;

       void Start()
       {
           LoadAllParties();
           CreatePartyListItems();
           SubscribeToEvents();
       }

       void CreatePartyListItems()
       void UpdatePartyDisplay(PoliticalParty party)
       void OnPartyPopularityChanged(PartyPopularityChanged eventData)
   }
   ```
   - **Deliverable:** Interactive UI showing all political parties
   - **Success Criteria:** Smooth scrolling, real-time updates, no UI errors

**Demo Milestone 1 Success Criteria:**
- ✅ Unity project builds without errors
- ✅ All 12 Dutch political parties display with correct data
- ✅ Real-time polling data updates visible
- ✅ Event system working (popularity changes propagate to UI)
- ✅ Clean console (no errors or warnings)
- ✅ Stable 60+ FPS performance

---

## Phase 2: Political Engine Core
**Timeline:** 3-4 weeks
**Demo Milestone:** Interactive coalition formation with election simulation

### 2.1 Election System
**Complexity:** Complex
**Prerequisites:** Foundation phase completed
**Parallel Opportunity:** ✅ Can develop voting algorithms independently of UI

#### Tasks:
1. **Dutch Electoral System Implementation**
   ```csharp
   // File: Assets/Scripts/Political/Elections/DutchElectoralSystem.cs
   public class DutchElectoralSystem : MonoBehaviour
   {
       [Header("Electoral Parameters")]
       public int totalSeats = 150; // Tweede Kamer
       public float electoralThreshold = 0.67f; // ~1 seat threshold

       public ElectionResults ConductElection(List<PoliticalParty> parties)
       {
           // Implement D'Hondt proportional representation system
           var results = new ElectionResults();
           var totalVotes = CalculateTotalVotes(parties);

           foreach (var party in parties)
           {
               var seats = CalculateSeatsUsingDHondt(party, totalVotes);
               results.seatAllocations[party] = seats;
           }

           return results;
       }

       private int CalculateSeatsUsingDHondt(PoliticalParty party, long totalVotes)
       private bool MeetsElectoralThreshold(PoliticalParty party, long totalVotes)
   }
   ```
   - **Deliverable:** Accurate D'Hondt electoral system implementation
   - **Success Criteria:** Results match real Dutch election mathematics, handles edge cases

2. **Voting Behavior Simulation**
   ```csharp
   // File: Assets/Scripts/Political/Elections/VotingBehavior.cs
   public class VotingBehavior : MonoBehaviour
   {
       [Header("Voter Demographics")]
       public List<VoterSegment> voterSegments;
       public AnimationCurve volatilityCurve; // How much opinion can swing

       public Dictionary<PoliticalParty, float> SimulateVoting()
       {
           var results = new Dictionary<PoliticalParty, float>();

           foreach (var segment in voterSegments)
           {
               var segmentVotes = CalculateSegmentVoting(segment);
               MergeVotingResults(results, segmentVotes);
           }

           return ApplyRandomVolatility(results);
       }

       private Dictionary<PoliticalParty, float> CalculateSegmentVoting(VoterSegment segment)
       private void ApplyIssueInfluence(VoterSegment segment, Dictionary<PoliticalParty, float> votes)
   }
   ```
   - **Deliverable:** Realistic voter behavior with demographic segments
   - **Success Criteria:** Vote share changes based on issues, demographics, and campaign events

3. **Polling System**
   ```csharp
   // File: Assets/Scripts/Political/PublicOpinion/PollingSystem.cs
   public class PollingSystem : MonoBehaviour
   {
       [Header("Polling Configuration")]
       public int pollingSampleSize = 1200;
       public float pollingMarginOfError = 3.0f; // +/- percentage
       public List<PollingCompany> pollingCompanies;

       public Poll ConductPoll(PollingCompany company)
       {
           var actualSupport = votingBehavior.SimulateVoting();
           var poll = new Poll
           {
               date = GameManager.Instance.gameDate,
               company = company,
               sampleSize = pollingSampleSize,
               marginOfError = pollingMarginOfError
           };

           // Add realistic polling error and house effects
           foreach (var party in actualSupport.Keys)
           {
               var polledSupport = ApplyPollingError(actualSupport[party], company);
               poll.partySupport[party] = polledSupport;
           }

           return poll;
       }

       private float ApplyPollingError(float actualSupport, PollingCompany company)
   }
   ```
   - **Deliverable:** Realistic polling with company-specific biases and statistical error
   - **Success Criteria:** Polls show realistic variance, trending matches actual support changes

### 2.2 Coalition Formation System
**Complexity:** Complex
**Prerequisites:** Election system completed
**Parallel Opportunity:** ❌ Depends on election results

#### Tasks:
1. **Coalition Mathematics**
   ```csharp
   // File: Assets/Scripts/Political/Coalitions/CoalitionFormation.cs
   public class CoalitionFormation : MonoBehaviour
   {
       [Header("Formation Rules")]
       public int minimumSeatsForMajority = 76; // 150 / 2 + 1
       public float ideologicalToleranceRange = 3.0f; // Maximum policy distance

       public List<CoalitionOption> FindViableCoalitions(ElectionResults results)
       {
           var parties = results.seatAllocations.Keys.OrderByDescending(p => results.seatAllocations[p]).ToList();
           var coalitionOptions = new List<CoalitionOption>();

           // Generate all possible combinations that reach majority
           var combinations = GenerateCombinations(parties, minimumSeatsForMajority, results);

           foreach (var combination in combinations)
           {
               var compatibility = CalculateCoalitionCompatibility(combination);
               if (compatibility > 0.3f) // Minimum viability threshold
               {
                   coalitionOptions.Add(new CoalitionOption
                   {
                       parties = combination,
                       totalSeats = combination.Sum(p => results.seatAllocations[p]),
                       compatibility = compatibility,
                       formationDifficulty = CalculateFormationDifficulty(combination)
                   });
               }
           }

           return coalitionOptions.OrderByDescending(c => c.compatibility).ToList();
       }

       private float CalculateCoalitionCompatibility(List<PoliticalParty> parties)
       private float CalculateFormationDifficulty(List<PoliticalParty> parties)
   }
   ```
   - **Deliverable:** Intelligent coalition formation algorithm matching Dutch politics
   - **Success Criteria:** Generates realistic coalitions, excludes incompatible combinations

2. **Coalition Negotiation System**
   ```csharp
   // File: Assets/Scripts/Political/Coalitions/CoalitionNegotiation.cs
   public class CoalitionNegotiation : MonoBehaviour
   {
       [Header("Negotiation Parameters")]
       public int maxNegotiationDays = 120; // Realistic formation timeline
       public List<NegotiationIssue> negotiationIssues;

       public IEnumerator NegotiateCoalition(CoalitionOption option)
       {
           var negotiationState = new NegotiationState(option.parties);
           var daysElapsed = 0;

           while (daysElapsed < maxNegotiationDays && !negotiationState.isComplete)
           {
               // Daily negotiation progress
               ProcessNegotiationDay(negotiationState);

               // Random events that can disrupt negotiations
               if (Random.Range(0f, 1f) < 0.05f) // 5% chance per day
               {
                   var disruptiveEvent = GenerateNegotiationEvent();
                   ApplyNegotiationEvent(negotiationState, disruptiveEvent);
               }

               daysElapsed++;
               yield return new WaitForGameDay(); // Custom time advancement
           }

           if (negotiationState.isComplete)
           {
               var coalition = CreateCoalitionAgreement(negotiationState);
               EventBus.Publish(new CoalitionFormed { Coalition = coalition });
           }
           else
           {
               EventBus.Publish(new CoalitionNegotiationFailed { Parties = option.parties });
           }
       }

       private void ProcessNegotiationDay(NegotiationState state)
       private NegotiationEvent GenerateNegotiationEvent()
   }
   ```
   - **Deliverable:** Realistic coalition negotiation process with random events
   - **Success Criteria:** Negotiations take realistic time, can fail authentically

3. **Government Formation**
   ```csharp
   // File: Assets/Scripts/Political/Parliament/Government.cs
   public class Government : MonoBehaviour
   {
       [Header("Cabinet Structure")]
       public List<MinisterialPost> ministerialPosts;
       public PoliticalParty primeMinisterParty;
       public List<PoliticalParty> coalitionParties;

       [Header("Government Stability")]
       public float stabilityRating = 100f; // 0-100
       public List<CoalitionAgreementPoint> coalitionAgreement;

       public void FormGovernment(Coalition coalition)
       {
           coalitionParties = coalition.parties;
           primeMinisterParty = coalition.parties.OrderByDescending(p => p.seatsInParliament).First();

           AllocateMinisterialPosts();
           CalculateInitialStability();

           EventBus.Publish(new GovernmentFormed { Government = this });
       }

       public void UpdateStability(PoliticalEvent politicalEvent)
       {
           var stabilityChange = CalculateStabilityImpact(politicalEvent);
           stabilityRating = Mathf.Clamp(stabilityRating + stabilityChange, 0f, 100f);

           if (stabilityRating < 20f)
           {
               TriggerConfidenceVote();
           }

           EventBus.Publish(new GovernmentStabilityChanged { NewStability = stabilityRating });
       }

       private void AllocateMinisterialPosts()
       private float CalculateStabilityImpact(PoliticalEvent politicalEvent)
   }
   ```
   - **Deliverable:** Complete government formation and stability system
   - **Success Criteria:** Realistic cabinet allocation, government can fall and trigger elections

### 2.3 Political Engine Demo Scene
**Complexity:** Medium
**Prerequisites:** All political engine tasks completed
**Parallel Opportunity:** ❌ Requires all systems integration

#### Tasks:
1. **Interactive Election Simulation**
   - Scene: `Assets/Scenes/PoliticalEngineDemo.unity`
   - Allow user to trigger elections with current polling data
   - Display D'Hondt seat allocation in real-time
   - Show viable coalition combinations
   - **Deliverable:** Interactive election simulator
   - **Success Criteria:** Accurate electoral math, smooth transitions

2. **Coalition Formation Interface**
   ```csharp
   // File: Assets/Scripts/UI/Panels/CoalitionFormationPanel.cs
   public class CoalitionFormationPanel : MonoBehaviour
   {
       [Header("UI Elements")]
       public Transform coalitionOptionsContainer;
       public GameObject coalitionOptionPrefab;
       public Button formCoalitionButton;
       public Slider compatibilitySlider;

       private List<CoalitionOption> currentOptions;
       private CoalitionOption selectedOption;

       public void DisplayCoalitionOptions(List<CoalitionOption> options)
       {
           ClearExistingOptions();
           currentOptions = options;

           foreach (var option in options)
           {
               var optionGO = Instantiate(coalitionOptionPrefab, coalitionOptionsContainer);
               var optionUI = optionGO.GetComponent<CoalitionOptionUI>();
               optionUI.Initialize(option, OnCoalitionOptionSelected);
           }
       }

       private void OnCoalitionOptionSelected(CoalitionOption option)
       public void StartCoalitionNegotiation()
   }
   ```
   - **Deliverable:** User-friendly coalition formation interface
   - **Success Criteria:** Clear visualization of options, intuitive selection process

**Demo Milestone 2 Success Criteria:**
- ✅ Accurate Dutch electoral system (D'Hondt method)
- ✅ Realistic polling with statistical variance
- ✅ Viable coalition combinations generated automatically
- ✅ Coalition negotiations proceed with realistic timeline
- ✅ Government formation creates stable cabinets
- ✅ Interactive UI for all political processes
- ✅ Event system connects all political components

---

## Phase 3: Campaign Mechanics
**Timeline:** 4-5 weeks
**Demo Milestone:** Full campaign system with social media, debates, and public opinion dynamics

### 3.1 Social Media Simulation
**Complexity:** Complex
**Prerequisites:** Political engine completed
**Parallel Opportunity:** ✅ Can develop platform simulations independently

#### Tasks:
1. **Twitter/X Platform Simulation**
   ```csharp
   // File: Assets/Scripts/Campaign/SocialMedia/TwitterSimulation.cs
   public class TwitterSimulation : SocialMediaPlatform
   {
       [Header("Twitter Configuration")]
       public int maxTweetLength = 280;
       public float viralityThreshold = 1000f; // Retweets needed for viral status
       public AnimationCurve engagementCurve; // Engagement over time

       public override SocialMediaPost CreatePost(PoliticalParty party, string content, PostType type)
       {
           var tweet = new Tweet
           {
               party = party,
               content = TruncateContent(content, maxTweetLength),
               timestamp = GameManager.Instance.gameDate,
               postType = type,
               platform = SocialMediaPlatform.Twitter
           };

           // Initial engagement simulation
           tweet.likes = CalculateInitialLikes(party, type);
           tweet.retweets = CalculateInitialRetweets(party, type);
           tweet.replies = CalculateInitialReplies(party, type);

           ProcessPost(tweet);
           return tweet;
       }

       public override void SimulateEngagement(SocialMediaPost post, float deltaTime)
       {
           if (post is Tweet tweet)
           {
               // Viral potential calculation
               var viralChance = CalculateViralPotential(tweet);
               if (viralChance > viralityThreshold)
               {
                   TriggerViralSpread(tweet);
               }

               // Organic engagement growth
               UpdateEngagementMetrics(tweet, deltaTime);
           }
       }

       private float CalculateViralPotential(Tweet tweet)
       private void TriggerViralSpread(Tweet tweet)
   }
   ```
   - **Deliverable:** Realistic Twitter simulation with viral mechanics
   - **Success Criteria:** Posts engage audiences, viral content spreads realistically

2. **Facebook Platform Simulation**
   ```csharp
   // File: Assets/Scripts/Campaign/SocialMedia/FacebookSimulation.cs
   public class FacebookSimulation : SocialMediaPlatform
   {
       [Header("Facebook Configuration")]
       public int maxPostLength = 63206; // Facebook's character limit
       public float algorithmVisibilityFactor = 0.3f; // Organic reach percentage
       public List<DemographicGroup> targetDemographics;

       public override SocialMediaPost CreatePost(PoliticalParty party, string content, PostType type)
       {
           var facebookPost = new FacebookPost
           {
               party = party,
               content = content,
               timestamp = GameManager.Instance.gameDate,
               postType = type,
               platform = SocialMediaPlatform.Facebook,
               targetedDemographics = DetermineTargetDemographics(party, type)
           };

           // Facebook's algorithm simulation
           facebookPost.organicReach = CalculateOrganicReach(facebookPost);
           facebookPost.engagement = SimulateAlgorithmicEngagement(facebookPost);

           ProcessPost(facebookPost);
           return facebookPost;
       }

       private List<DemographicGroup> DetermineTargetDemographics(PoliticalParty party, PostType type)
       private float CalculateOrganicReach(FacebookPost post)
       private EngagementMetrics SimulateAlgorithmicEngagement(FacebookPost post)
   }
   ```
   - **Deliverable:** Facebook simulation with demographic targeting and algorithm effects
   - **Success Criteria:** Different engagement patterns from Twitter, demographic influence visible

3. **Social Media Analytics Dashboard**
   ```csharp
   // File: Assets/Scripts/Campaign/SocialMedia/SocialMediaAnalytics.cs
   public class SocialMediaAnalytics : MonoBehaviour
   {
       [Header("Analytics Configuration")]
       public float trackingPeriod = 30f; // Days to track
       public List<AnalyticsMetric> trackedMetrics;

       private Dictionary<PoliticalParty, List<SocialMediaPost>> postHistory;
       private Dictionary<PoliticalParty, EngagementTrends> engagementTrends;

       public AnalyticsReport GenerateReport(PoliticalParty party, float periodDays)
       {
           var report = new AnalyticsReport
           {
               party = party,
               reportPeriod = periodDays,
               generatedAt = GameManager.Instance.gameDate
           };

           // Calculate key performance indicators
           report.totalReach = CalculateTotalReach(party, periodDays);
           report.engagementRate = CalculateEngagementRate(party, periodDays);
           report.sentimentAnalysis = CalculateSentimentAnalysis(party, periodDays);
           report.viralPosts = GetViralPosts(party, periodDays);
           report.demographicBreakdown = CalculateDemographicEngagement(party, periodDays);

           return report;
       }

       public void TrackPostPerformance(SocialMediaPost post)
       public List<TrendingTopic> GetTrendingTopics()
       private SentimentAnalysis CalculateSentimentAnalysis(PoliticalParty party, float periodDays)
   }
   ```
   - **Deliverable:** Comprehensive social media analytics system
   - **Success Criteria:** Clear performance metrics, trending analysis, demographic insights

### 3.2 Debate System
**Complexity:** Complex
**Prerequisites:** Social media foundation, AI integration prep
**Parallel Opportunity:** ✅ Can develop debate mechanics while AI integration is in progress

#### Tasks:
1. **Debate Format Implementation**
   ```csharp
   // File: Assets/Scripts/Campaign/Debates/DebateSystem.cs
   public class DebateSystem : MonoBehaviour
   {
       [Header("Debate Configuration")]
       public List<DebateFormat> availableFormats;
       public float responseTimeLimit = 60f; // Seconds per response
       public int maxRebuttals = 2;

       public IEnumerator ConductDebate(List<PoliticalParty> participants, DebateFormat format)
       {
           var debate = new Debate
           {
               participants = participants,
               format = format,
               startTime = GameManager.Instance.gameDate,
               topic = SelectDebateTopic()
           };

           // Opening statements
           foreach (var party in participants)
           {
               yield return StartCoroutine(GetOpeningStatement(party, debate.topic));
           }

           // Main debate rounds
           for (int round = 0; round < format.numberOfRounds; round++)
           {
               var question = SelectDebateQuestion(debate.topic, round);
               yield return StartCoroutine(ProcessDebateRound(participants, question));
           }

           // Closing statements
           foreach (var party in participants)
           {
               yield return StartCoroutine(GetClosingStatement(party, debate));
           }

           ProcessDebateResults(debate);
       }

       private IEnumerator ProcessDebateRound(List<PoliticalParty> participants, DebateQuestion question)
       private DebatePerformance EvaluateDebatePerformance(PoliticalParty party, List<DebateResponse> responses)
   }
   ```
   - **Deliverable:** Flexible debate system supporting multiple formats
   - **Success Criteria:** Debates run smoothly, realistic timing, clear winner determination

2. **Debate Performance Analysis**
   ```csharp
   // File: Assets/Scripts/Campaign/Debates/DebateAnalysis.cs
   public class DebateAnalysis : MonoBehaviour
   {
       [Header("Analysis Criteria")]
       public List<DebateMetric> performanceMetrics;
       public float factualnessWeight = 0.3f;
       public float charismaWeight = 0.4f;
       public float policyCoherenceWeight = 0.3f;

       public DebateResults AnalyzeDebatePerformance(Debate debate)
       {
           var results = new DebateResults();

           foreach (var participant in debate.participants)
           {
               var performance = new DebatePerformance
               {
                   party = participant,
                   overallScore = CalculateOverallScore(participant, debate),
                   strengths = IdentifyStrengths(participant, debate),
                   weaknesses = IdentifyWeaknesses(participant, debate),
                   memorableMoments = FindMemorableMoments(participant, debate)
               };

               results.performances[participant] = performance;
           }

           // Determine debate winner and impact on polls
           results.winner = DetermineDebateWinner(results.performances);
           results.pollImpact = CalculatePollImpact(results);

           return results;
       }

       private float CalculateOverallScore(PoliticalParty party, Debate debate)
       private List<string> IdentifyStrengths(PoliticalParty party, Debate debate)
       private Dictionary<PoliticalParty, float> CalculatePollImpact(DebateResults results)
   }
   ```
   - **Deliverable:** Comprehensive debate performance analysis
   - **Success Criteria:** Realistic performance evaluation, poll impact reflects debate outcomes

3. **Live Debate UI System**
   ```csharp
   // File: Assets/Scripts/UI/Panels/DebateViewerPanel.cs
   public class DebateViewerPanel : MonoBehaviour
   {
       [Header("UI Components")]
       public Transform participantContainer;
       public GameObject participantPrefab;
       public TextMeshProUGUI currentQuestionText;
       public Slider timeRemainingSlider;
       public Button nextResponseButton;

       [Header("Visual Effects")]
       public ParticleSystem applauseEffect;
       public AudioSource audienceReaction;
       public Animator cameraAnimator;

       private Debate currentDebate;
       private List<DebateParticipantUI> participantUIs;

       public void StartDebateViewer(Debate debate)
       {
           currentDebate = debate;
           SetupParticipantUIs();
           StartCoroutine(PlayDebateSequence());
       }

       private IEnumerator PlayDebateSequence()
       {
           foreach (var round in currentDebate.rounds)
           {
               DisplayQuestion(round.question);

               foreach (var response in round.responses)
               {
                   yield return StartCoroutine(PlayResponse(response));
                   ShowAudienceReaction(response.reactionScore);
               }
           }

           DisplayDebateResults();
       }

       private void ShowAudienceReaction(float reactionScore)
       private void DisplayDebateResults()
   }
   ```
   - **Deliverable:** Engaging live debate viewing experience
   - **Success Criteria:** Smooth presentation, clear participant tracking, audience reactions

### 3.3 Rally and Campaign Events
**Complexity:** Medium
**Prerequisites:** Social media and debate systems
**Parallel Opportunity:** ✅ Can develop independently of other campaign systems

#### Tasks:
1. **Rally System Implementation**
   ```csharp
   // File: Assets/Scripts/Campaign/Rallies/RallySystem.cs
   public class RallySystem : MonoBehaviour
   {
       [Header("Rally Configuration")]
       public List<RallyVenue> availableVenues;
       public float baseCostPerAttendee = 5.0f;
       public AnimationCurve attendanceDeclineCurve; // Over-booking penalty

       public Rally OrganizeRally(PoliticalParty party, RallyVenue venue, DateTime rallyDate)
       {
           var rally = new Rally
           {
               organizingParty = party,
               venue = venue,
               scheduledDate = rallyDate,
               cost = CalculateRallyCost(party, venue),
               expectedAttendance = PredictAttendance(party, venue, rallyDate)
           };

           // Weather impact simulation
           ApplyWeatherEffects(rally);

           // Counter-protest possibility
           CheckForCounterProtests(rally);

           ProcessRallyEvent(rally);
           return rally;
       }

       private void ProcessRallyEvent(Rally rally)
       {
           // Generate attendance based on party popularity and venue capacity
           var actualAttendance = SimulateAttendance(rally);
           rally.actualAttendance = actualAttendance;

           // Calculate rally success metrics
           rally.energyLevel = CalculateRallyEnergy(rally);
           rally.mediaAttention = CalculateMediaAttention(rally);
           rally.localImpact = CalculateLocalPoliticalImpact(rally);

           // Apply effects to party popularity
           var popularityChange = CalculatePopularityImpact(rally);
           ApplyPopularityChange(rally.organizingParty, popularityChange);

           EventBus.Publish(new RallyCompleted { Rally = rally });
       }

       private float CalculatePopularityImpact(Rally rally)
       private void ApplyWeatherEffects(Rally rally)
   }
   ```
   - **Deliverable:** Realistic rally system with venue management and crowd simulation
   - **Success Criteria:** Rallies impact popularity, realistic attendance patterns

2. **Campaign Event Scheduler**
   ```csharp
   // File: Assets/Scripts/Campaign/Events/CampaignScheduler.cs
   public class CampaignScheduler : MonoBehaviour
   {
       [Header("Scheduling Configuration")]
       public int maxEventsPerDay = 3;
       public float travelTimeBetweenCities = 4f; // Hours
       public List<CampaignEventType> availableEventTypes;

       private Dictionary<PoliticalParty, List<ScheduledEvent>> partySchedules;

       public bool ScheduleEvent(PoliticalParty party, CampaignEvent campaignEvent, DateTime proposedDate)
       {
           // Check schedule conflicts
           if (HasScheduleConflict(party, proposedDate, campaignEvent.duration))
           {
               return false;
           }

           // Check travel logistics
           if (!IsTravelLogisticsFeasible(party, campaignEvent, proposedDate))
           {
               return false;
           }

           // Check budget availability
           if (!CanAffordEvent(party, campaignEvent))
           {
               return false;
           }

           var scheduledEvent = new ScheduledEvent
           {
               party = party,
               campaignEvent = campaignEvent,
               scheduledTime = proposedDate,
               status = EventStatus.Scheduled
           };

           AddToSchedule(party, scheduledEvent);
           EventBus.Publish(new EventScheduled { Event = scheduledEvent });

           return true;
       }

       public List<ScheduledEvent> GetUpcomingEvents(PoliticalParty party, int daysAhead = 7)
       public void CancelEvent(PoliticalParty party, ScheduledEvent eventToCancel)
       private bool IsTravelLogisticsFeasible(PoliticalParty party, CampaignEvent event, DateTime proposedDate)
   }
   ```
   - **Deliverable:** Complete campaign event scheduling system
   - **Success Criteria:** Realistic scheduling constraints, no impossible travel times

3. **Media Coverage System**
   ```csharp
   // File: Assets/Scripts/Campaign/MediaCycle/MediaCoverage.cs
   public class MediaCoverage : MonoBehaviour
   {
       [Header("Media Configuration")]
       public List<MediaOutlet> mediaOutlets;
       public float newsWorthinessBias = 0.1f; // Preference for sensational stories
       public AnimationCurve attentionDecayCurve; // How stories lose relevance

       public void CoverCampaignEvent(CampaignEvent campaignEvent)
       {
           foreach (var outlet in mediaOutlets)
           {
               var coverageDecision = DecideCoverage(outlet, campaignEvent);
               if (coverageDecision.willCover)
               {
                   var story = GenerateNewsStory(outlet, campaignEvent, coverageDecision.angle);
                   PublishStory(outlet, story);

                   // Calculate impact on public opinion
                   var impact = CalculateStoryImpact(story);
                   ApplyPublicOpinionChange(impact);
               }
           }
       }

       public void ProcessNewsDay()
       {
           // Daily news cycle processing
           var topStories = SelectTopStories();

           foreach (var story in topStories)
           {
               // Stories lose relevance over time
               story.relevance *= attentionDecayCurve.Evaluate(story.daysSincePublication);

               // Continued coverage for major stories
               if (story.relevance > 0.5f && story.followUpPotential > 0.7f)
               {
                   GenerateFollowUpCoverage(story);
               }
           }

           // Remove stories that are no longer relevant
           CleanupOldStories();
       }

       private CoverageDecision DecideCoverage(MediaOutlet outlet, CampaignEvent campaignEvent)
       private NewsStory GenerateNewsStory(MediaOutlet outlet, CampaignEvent event, CoverageAngle angle)
   }
   ```
   - **Deliverable:** Dynamic media coverage system with realistic news cycles
   - **Success Criteria:** Media attention affects public opinion, stories have realistic lifecycles

### 3.4 Campaign Demo Scene
**Complexity:** Medium
**Prerequisites:** All campaign systems completed
**Parallel Opportunity:** ❌ Requires integration of all campaign mechanics

#### Tasks:
1. **Campaign Management Interface**
   - Scene: `Assets/Scenes/CampaignDemo.unity`
   - Allow players to schedule rallies, debates, social media posts
   - Show real-time social media feeds and analytics
   - Display upcoming campaign events and their impact
   - **Deliverable:** Complete campaign management interface
   - **Success Criteria:** All campaign actions accessible, real-time feedback visible

2. **Public Opinion Dashboard**
   ```csharp
   // File: Assets/Scripts/UI/Panels/PublicOpinionDashboard.cs
   public class PublicOpinionDashboard : MonoBehaviour
   {
       [Header("Dashboard Components")]
       public LineChart pollChart;
       public Transform trendingTopicsContainer;
       public TextMeshProUGUI currentLeaderText;
       public Slider coalitionProbabilitySlider;

       [Header("Real-time Updates")]
       public float updateInterval = 5f; // Seconds
       public AnimationCurve smoothingCurve;

       private Coroutine updateCoroutine;

       void Start()
       {
           InitializeDashboard();
           updateCoroutine = StartCoroutine(UpdateDashboardRegularly());
       }

       private IEnumerator UpdateDashboardRegularly()
       {
           while (true)
           {
               UpdatePollChart();
               UpdateTrendingTopics();
               UpdateCoalitionProbabilities();
               UpdateCurrentLeader();

               yield return new WaitForSeconds(updateInterval);
           }
       }

       private void UpdatePollChart()
       {
           var currentPolls = PollingSystem.Instance.GetLatestPolls();
           var chartData = ConvertPollsToChartData(currentPolls);
           pollChart.UpdateData(chartData);
       }

       private void UpdateTrendingTopics()
       private void UpdateCoalitionProbabilities()
   }
   ```
   - **Deliverable:** Real-time public opinion monitoring system
   - **Success Criteria:** Smooth updates, clear data visualization, responsive to campaign actions

**Demo Milestone 3 Success Criteria:**
- ✅ Social media posts generate realistic engagement
- ✅ Debates affect polling data appropriately
- ✅ Rally attendance correlates with party popularity
- ✅ Media coverage influences public opinion
- ✅ Campaign events show immediate and delayed effects
- ✅ Public opinion dashboard updates in real-time
- ✅ All campaign systems interact cohesively

---

## Phase 4: NVIDIA NIM AI Integration
**Timeline:** 3-4 weeks
**Demo Milestone:** AI-powered political responses and dynamic campaign content

### 4.1 NIM Client Setup
**Complexity:** Complex
**Prerequisites:** Campaign systems completed for AI integration context
**Parallel Opportunity:** ✅ NIM setup can be developed independently

#### Tasks:
1. **NIM HTTP Client Implementation**
   ```csharp
   // File: Assets/Scripts/AI/NIMClient.cs
   using System.Threading.Tasks;
   using UnityEngine;
   using UnityEngine.Networking;
   using Newtonsoft.Json;

   public class NIMClient : MonoBehaviour
   {
       [Header("NIM Configuration")]
       public string nimServerUrl = "http://localhost:8000";
       public string apiVersion = "v1";
       public int timeoutSeconds = 30;
       public int maxRetries = 3;

       [Header("Model Settings")]
       public string defaultModel = "llama2-7b-chat";
       public int maxTokens = 500;
       public float temperature = 0.7f;
       public float topP = 0.9f;

       private readonly HttpClient httpClient = new HttpClient();

       public async Task<NIMResponse> GenerateResponse(NIMRequest request)
       {
           var requestJson = JsonConvert.SerializeObject(request);
           var attempts = 0;

           while (attempts < maxRetries)
           {
               try
               {
                   var response = await SendRequestAsync(requestJson);
                   if (response.IsSuccess)
                   {
                       return response;
                   }
               }
               catch (Exception ex)
               {
                   Debug.LogWarning($"NIM request attempt {attempts + 1} failed: {ex.Message}");
               }

               attempts++;
               await Task.Delay(1000 * attempts); // Exponential backoff
           }

           return CreateFailureResponse("Max retries exceeded");
       }

       private async Task<NIMResponse> SendRequestAsync(string requestJson)
       private NIMResponse CreateFailureResponse(string error)
       public async Task<bool> TestConnection()
   }

   [System.Serializable]
   public class NIMRequest
   {
       public string model;
       public string prompt;
       public int max_tokens;
       public float temperature;
       public float top_p;
       public List<string> stop_sequences;
   }

   [System.Serializable]
   public class NIMResponse
   {
       public bool IsSuccess;
       public string GeneratedText;
       public string Error;
       public float ResponseTime;
       public int TokensGenerated;
   }
   ```
   - **Deliverable:** Robust NIM client with error handling and retry logic
   - **Success Criteria:** Successfully connects to local NIM instance, handles network failures gracefully

2. **Response Caching System**
   ```csharp
   // File: Assets/Scripts/AI/ResponseCache.cs
   public class ResponseCache : MonoBehaviour
   {
       [Header("Cache Configuration")]
       public int maxCacheSize = 1000;
       public float cacheExpirationHours = 24f;
       public bool enablePersistentCache = true;
       public string cacheFilePath = "ai_response_cache.json";

       private Dictionary<string, CachedResponse> responseCache;
       private Queue<string> cacheOrderQueue; // For LRU eviction

       public bool TryGetCachedResponse(string promptHash, out string cachedResponse)
       {
           cachedResponse = null;

           if (responseCache.TryGetValue(promptHash, out var cached))
           {
               // Check if cache entry is still valid
               if ((DateTime.Now - cached.timestamp).TotalHours < cacheExpirationHours)
               {
                   cachedResponse = cached.response;

                   // Update access time for LRU
                   cached.lastAccessed = DateTime.Now;
                   return true;
               }
               else
               {
                   // Remove expired entry
                   RemoveCacheEntry(promptHash);
               }
           }

           return false;
       }

       public void CacheResponse(string promptHash, string response)
       {
           // Ensure cache size limit
           while (responseCache.Count >= maxCacheSize)
           {
               var oldestKey = cacheOrderQueue.Dequeue();
               RemoveCacheEntry(oldestKey);
           }

           var cachedResponse = new CachedResponse
           {
               response = response,
               timestamp = DateTime.Now,
               lastAccessed = DateTime.Now
           };

           responseCache[promptHash] = cachedResponse;
           cacheOrderQueue.Enqueue(promptHash);

           if (enablePersistentCache)
           {
               SaveCacheToDisk();
           }
       }

       private void LoadCacheFromDisk()
       private void SaveCacheToDisk()
       private void RemoveCacheEntry(string key)
   }
   ```
   - **Deliverable:** Efficient response caching with persistence and LRU eviction
   - **Success Criteria:** Reduces API calls by 60%+, cache persists between sessions

3. **Local NIM Installation Guide**
   ```markdown
   # File: Assets/StreamingAssets/NIMSetupGuide.md
   # NVIDIA NIM Local Installation Guide

   ## Prerequisites
   - NVIDIA GPU with 8GB+ VRAM
   - Docker Desktop installed
   - NVIDIA Container Toolkit
   - 50GB+ free disk space

   ## Installation Steps

   ### 1. Pull NIM Container
   ```bash
   docker pull nvcr.io/nim/meta/llama2-7b-chat:latest
   ```

   ### 2. Start NIM Server
   ```bash
   docker run -d --gpus all \
     --name nim-llama2-server \
     -p 8000:8000 \
     -v ~/nim-cache:/opt/nim/.cache \
     nvcr.io/nim/meta/llama2-7b-chat:latest
   ```

   ### 3. Verify Installation
   ```bash
   curl -X POST "http://localhost:8000/v1/chat/completions" \
     -H "Content-Type: application/json" \
     -d '{
       "model": "llama2-7b-chat",
       "messages": [{"role": "user", "content": "Hello!"}],
       "max_tokens": 100
     }'
   ```

   ## Troubleshooting
   - GPU Memory Issues: Reduce model size or use CPU fallback
   - Network Connectivity: Check firewall settings
   - Container Issues: Verify Docker and NVIDIA runtime
   ```
   - **Deliverable:** Complete setup documentation with troubleshooting
   - **Success Criteria:** Clear instructions, covers common issues

### 4.2 Political Response Generation
**Complexity:** Complex
**Prerequisites:** NIM client setup completed
**Parallel Opportunity:** ❌ Requires working NIM connection

#### Tasks:
1. **Political Prompt Templates**
   ```csharp
   // File: Assets/Scripts/AI/PromptTemplates/PoliticalPromptTemplate.cs
   [CreateAssetMenu(fileName = "Political Prompt Template", menuName = "Coalition/AI/Prompt Template")]
   public class PoliticalPromptTemplate : ScriptableObject
   {
       [Header("Template Configuration")]
       public string templateName;
       public PromptType promptType;
       public string systemPrompt;
       public string userPromptTemplate;

       [Header("Template Variables")]
       public List<string> requiredVariables; // {party_name}, {issue}, etc.
       public Dictionary<string, string> defaultValues;

       [Header("Response Constraints")]
       public int maxResponseLength = 280; // For Twitter-style responses
       public float formalityLevel = 0.7f; // 0-1, casual to formal
       public List<string> forbiddenWords; // Political sensitivity

       public string GeneratePrompt(Dictionary<string, string> variables)
       {
           var prompt = systemPrompt + "\n\n" + userPromptTemplate;

           // Replace template variables
           foreach (var variable in variables)
           {
               prompt = prompt.Replace($"{{{variable.Key}}}", variable.Value);
           }

           // Apply any missing default values
           foreach (var defaultVar in defaultValues)
           {
               if (!variables.ContainsKey(defaultVar.Key))
               {
                   prompt = prompt.Replace($"{{{defaultVar.Key}}}", defaultVar.Value);
               }
           }

           return prompt;
       }

       public bool ValidateVariables(Dictionary<string, string> variables)
       {
           return requiredVariables.All(req => variables.ContainsKey(req));
       }
   }

   public enum PromptType
   {
       DebateResponse,
       SocialMediaPost,
       PressStatement,
       PolicyExplanation,
       CampaignSlogan,
       CoalitionNegotiation
   }
   ```
   - **Deliverable:** Flexible prompt template system for various political contexts
   - **Success Criteria:** Templates generate contextually appropriate prompts

2. **Political Response Generator**
   ```csharp
   // File: Assets/Scripts/AI/PoliticalResponseGenerator.cs
   public class PoliticalResponseGenerator : MonoBehaviour
   {
       [Header("Generator Configuration")]
       public List<PoliticalPromptTemplate> promptTemplates;
       public NIMClient nimClient;
       public ResponseCache responseCache;

       [Header("Response Quality Control")]
       public ProfanityFilter profanityFilter;
       public PoliticalBiasDetector biasDetector;
       public float minResponseQuality = 0.7f;

       public async Task<PoliticalResponse> GenerateResponse(PoliticalResponseRequest request)
       {
           // Find appropriate template
           var template = FindBestTemplate(request.responseType, request.context);
           if (template == null)
           {
               return CreateErrorResponse("No suitable template found");
           }

           // Prepare variables for template
           var variables = PrepareTemplateVariables(request);
           if (!template.ValidateVariables(variables))
           {
               return CreateErrorResponse("Missing required template variables");
           }

           // Generate prompt
           var prompt = template.GeneratePrompt(variables);
           var promptHash = CalculatePromptHash(prompt, request.party);

           // Check cache first
           if (responseCache.TryGetCachedResponse(promptHash, out var cachedResponse))
           {
               return new PoliticalResponse
               {
                   responseText = cachedResponse,
                   isFromCache = true,
                   party = request.party,
                   generatedAt = DateTime.Now
               };
           }

           // Generate new response
           var nimRequest = new NIMRequest
           {
               model = "llama2-7b-chat",
               prompt = prompt,
               max_tokens = template.maxResponseLength,
               temperature = CalculateTemperature(request.party, request.context),
               top_p = 0.9f
           };

           var nimResponse = await nimClient.GenerateResponse(nimRequest);
           if (!nimResponse.IsSuccess)
           {
               return CreateErrorResponse($"NIM generation failed: {nimResponse.Error}");
           }

           // Quality control and filtering
           var filteredResponse = ApplyQualityControl(nimResponse.GeneratedText, request);

           // Cache successful response
           responseCache.CacheResponse(promptHash, filteredResponse);

           return new PoliticalResponse
           {
               responseText = filteredResponse,
               isFromCache = false,
               party = request.party,
               generatedAt = DateTime.Now,
               qualityScore = EvaluateResponseQuality(filteredResponse, request)
           };
       }

       private PoliticalPromptTemplate FindBestTemplate(PromptType responseType, string context)
       private Dictionary<string, string> PrepareTemplateVariables(PoliticalResponseRequest request)
       private string ApplyQualityControl(string rawResponse, PoliticalResponseRequest request)
   }
   ```
   - **Deliverable:** Comprehensive AI response generation with quality control
   - **Success Criteria:** Generates contextually appropriate political responses, maintains party consistency

3. **Fallback Response System**
   ```csharp
   // File: Assets/Scripts/AI/Fallbacks/FallbackResponseSystem.cs
   public class FallbackResponseSystem : MonoBehaviour
   {
       [Header("Fallback Configuration")]
       public List<ResponseTemplate> genericResponses;
       public List<ResponseTemplate> partySpecificResponses;
       public bool enableMarkovChain = true;
       public string trainingDataPath = "political_speeches.txt";

       private MarkovChainGenerator markovGenerator;
       private Dictionary<PoliticalParty, List<string>> partyResponseBank;

       public string GenerateFallbackResponse(PoliticalResponseRequest request)
       {
           // Try party-specific responses first
           if (TryGetPartySpecificResponse(request, out var partyResponse))
           {
               return CustomizeResponse(partyResponse, request);
           }

           // Try Markov chain generation
           if (enableMarkovChain && markovGenerator != null)
           {
               var markovResponse = markovGenerator.GenerateResponse(request.context, request.party);
               if (IsResponseAcceptable(markovResponse))
               {
                   return markovResponse;
               }
           }

           // Fall back to generic responses
           var genericTemplate = SelectGenericResponse(request.responseType);
           return CustomizeResponse(genericTemplate.template, request);
       }

       public void TrainMarkovChain()
       {
           if (!File.Exists(trainingDataPath))
           {
               Debug.LogWarning($"Training data file not found: {trainingDataPath}");
               return;
           }

           var trainingText = File.ReadAllText(trainingDataPath);
           markovGenerator = new MarkovChainGenerator();
           markovGenerator.Train(trainingText);

           Debug.Log("Markov chain training completed");
       }

       private bool TryGetPartySpecificResponse(PoliticalResponseRequest request, out string response)
       private string CustomizeResponse(string template, PoliticalResponseRequest request)
       private bool IsResponseAcceptable(string response)
   }
   ```
   - **Deliverable:** Robust fallback system for when NIM is unavailable
   - **Success Criteria:** Always provides political responses, maintains quality standards

### 4.3 AI Demo Scene
**Complexity:** Medium
**Prerequisites:** All AI systems completed
**Parallel Opportunity:** ❌ Requires complete AI integration

#### Tasks:
1. **AI Response Testing Interface**
   - Scene: `Assets/Scenes/AIDemo.unity`
   - Interactive prompt testing with different political parties
   - Real-time response generation and caching demonstration
   - Fallback system testing when NIM is offline
   - **Deliverable:** Complete AI testing environment
   - **Success Criteria:** Can generate responses for all parties, shows fallback behavior

2. **Live Debate AI Integration**
   ```csharp
   // File: Assets/Scripts/Campaign/Debates/AIDebateIntegration.cs
   public class AIDebateIntegration : MonoBehaviour
   {
       [Header("AI Debate Configuration")]
       public PoliticalResponseGenerator responseGenerator;
       public float responseGenerationTimeout = 15f;
       public int maxRetriesPerResponse = 2;

       public async Task<DebateResponse> GenerateDebateResponse(
           PoliticalParty party,
           DebateQuestion question,
           List<DebateResponse> previousResponses)
       {
           var context = BuildDebateContext(question, previousResponses);
           var request = new PoliticalResponseRequest
           {
               party = party,
               responseType = PromptType.DebateResponse,
               context = context,
               maxLength = 200, // Debate response length limit
               urgency = ResponseUrgency.RealTime
           };

           try
           {
               var response = await responseGenerator.GenerateResponse(request);

               return new DebateResponse
               {
                   party = party,
                   responseText = response.responseText,
                   isAIGenerated = true,
                   confidence = response.qualityScore,
                   generatedAt = DateTime.Now
               };
           }
           catch (TimeoutException)
           {
               // Use fallback for time-sensitive debate responses
               return GenerateFallbackDebateResponse(party, question);
           }
       }

       private string BuildDebateContext(DebateQuestion question, List<DebateResponse> previousResponses)
       private DebateResponse GenerateFallbackDebateResponse(PoliticalParty party, DebateQuestion question)
   }
   ```
   - **Deliverable:** Real-time AI integration for debate responses
   - **Success Criteria:** AI generates appropriate debate responses within time limits

**Demo Milestone 4 Success Criteria:**
- ✅ NIM client connects to local NIM instance
- ✅ AI generates contextually appropriate political responses
- ✅ Response caching reduces API calls significantly
- ✅ Fallback system works when NIM is unavailable
- ✅ Debate system uses AI for real-time responses
- ✅ Quality control filters inappropriate content
- ✅ Different parties have distinct AI voices

---

## Phase 5: Desktop UI Framework
**Timeline:** 3-4 weeks
**Demo Milestone:** Multi-window desktop application interface with authentic desktop feel

### 5.1 Window Management System
**Complexity:** Complex
**Prerequisites:** All core systems for UI integration
**Parallel Opportunity:** ✅ Can develop window system independently

#### Tasks:
1. **Core Window Manager**
   ```csharp
   // File: Assets/Scripts/UI/WindowManager.cs
   using UnityEngine;
   using UnityEngine.UIElements;
   using System.Collections.Generic;
   using System.Linq;

   public class WindowManager : MonoBehaviour
   {
       [Header("Window Configuration")]
       public VisualTreeAsset windowTemplate;
       public StyleSheet desktopStyleSheet;
       public int maxWindows = 10;
       public Vector2 defaultWindowSize = new Vector2(800, 600);

       [Header("Desktop Settings")]
       public bool enableWindowSnapping = true;
       public float snapDistance = 20f;
       public bool enableWindowAnimation = true;
       public float animationDuration = 0.3f;

       private UIDocument uiDocument;
       private VisualElement desktop;
       private List<DesktopWindow> activeWindows = new List<DesktopWindow>();
       private DesktopWindow focusedWindow;
       private int nextWindowId = 1;

       void Start()
       {
           InitializeDesktop();
           SetupEventHandlers();
       }

       public DesktopWindow CreateWindow(WindowConfiguration config)
       {
           if (activeWindows.Count >= maxWindows)
           {
               Debug.LogWarning("Maximum window limit reached");
               return null;
           }

           var windowElement = windowTemplate.Instantiate();
           var window = new DesktopWindow
           {
               id = nextWindowId++,
               element = windowElement,
               configuration = config,
               isVisible = true,
               position = CalculateNewWindowPosition(),
               size = config.defaultSize != Vector2.zero ? config.defaultSize : defaultWindowSize
           };

           SetupWindow(window);
           desktop.Add(windowElement);
           activeWindows.Add(window);

           BringWindowToFront(window);

           return window;
       }

       public void CloseWindow(DesktopWindow window)
       {
           if (window == null) return;

           activeWindows.Remove(window);
           desktop.Remove(window.element);

           if (focusedWindow == window)
           {
               FocusTopWindow();
           }

           window.configuration.onWindowClosed?.Invoke();
       }

       public void BringWindowToFront(DesktopWindow window)
       {
           if (window == null || !activeWindows.Contains(window)) return;

           // Update z-order
           var maxZOrder = activeWindows.Max(w => w.zOrder);
           window.zOrder = maxZOrder + 1;
           window.element.style.zIndex = window.zOrder;

           // Update focus
           if (focusedWindow != null)
           {
               focusedWindow.element.RemoveFromClassList("focused-window");
           }

           focusedWindow = window;
           window.element.AddToClassList("focused-window");

           window.configuration.onWindowFocused?.Invoke();
       }

       private void InitializeDesktop()
       private void SetupWindow(DesktopWindow window)
       private Vector2 CalculateNewWindowPosition()
       private void FocusTopWindow()
   }

   [System.Serializable]
   public class WindowConfiguration
   {
       public string title;
       public string content;
       public Vector2 defaultSize = new Vector2(800, 600);
       public Vector2 minSize = new Vector2(400, 300);
       public bool isResizable = true;
       public bool isMovable = true;
       public bool hasCloseButton = true;
       public bool hasMinimizeButton = true;
       public bool hasMaximizeButton = true;
       public System.Action onWindowClosed;
       public System.Action onWindowFocused;
   }

   [System.Serializable]
   public class DesktopWindow
   {
       public int id;
       public VisualElement element;
       public WindowConfiguration configuration;
       public bool isVisible;
       public Vector2 position;
       public Vector2 size;
       public int zOrder;
       public bool isMinimized;
       public bool isMaximized;
       public Vector2 restorePosition;
       public Vector2 restoreSize;
   }
   ```
   - **Deliverable:** Complete multi-window management system
   - **Success Criteria:** Windows can be created, moved, resized, focused, and closed like desktop apps

2. **Window Interaction System**
   ```csharp
   // File: Assets/Scripts/UI/WindowInteraction.cs
   public class WindowInteraction : MonoBehaviour
   {
       [Header("Interaction Configuration")]
       public float doubleClickTime = 0.5f;
       public float dragThreshold = 5f;
       public bool enableWindowSnapping = true;
       public float snapDistance = 20f;

       private WindowManager windowManager;
       private DesktopWindow currentlyDragging;
       private Vector2 dragStartPosition;
       private Vector2 dragOffset;
       private float lastClickTime;

       void Start()
       {
           windowManager = GetComponent<WindowManager>();
       }

       public void RegisterWindowEvents(DesktopWindow window)
       {
           var titleBar = window.element.Q<VisualElement>("title-bar");
           var closeButton = window.element.Q<Button>("close-button");
           var minimizeButton = window.element.Q<Button>("minimize-button");
           var maximizeButton = window.element.Q<Button>("maximize-button");
           var resizeHandle = window.element.Q<VisualElement>("resize-handle");

           // Title bar interactions
           titleBar.RegisterCallback<MouseDownEvent>(evt => OnTitleBarMouseDown(evt, window));
           titleBar.RegisterCallback<MouseMoveEvent>(evt => OnTitleBarMouseMove(evt, window));
           titleBar.RegisterCallback<MouseUpEvent>(evt => OnTitleBarMouseUp(evt, window));

           // Double-click to maximize/restore
           titleBar.RegisterCallback<ClickEvent>(evt => OnTitleBarClick(evt, window));

           // Button interactions
           closeButton?.RegisterCallback<ClickEvent>(evt => windowManager.CloseWindow(window));
           minimizeButton?.RegisterCallback<ClickEvent>(evt => MinimizeWindow(window));
           maximizeButton?.RegisterCallback<ClickEvent>(evt => ToggleMaximizeWindow(window));

           // Resize handle
           if (window.configuration.isResizable && resizeHandle != null)
           {
               RegisterResizeEvents(resizeHandle, window);
           }

           // General window focus
           window.element.RegisterCallback<MouseDownEvent>(evt => windowManager.BringWindowToFront(window));
       }

       private void OnTitleBarMouseDown(MouseDownEvent evt, DesktopWindow window)
       {
           if (!window.configuration.isMovable) return;

           currentlyDragging = window;
           dragStartPosition = evt.mousePosition;
           dragOffset = evt.mousePosition - window.position;

           window.element.CaptureMouse();
       }

       private void OnTitleBarMouseMove(MouseMoveEvent evt, DesktopWindow window)
       {
           if (currentlyDragging != window) return;

           var newPosition = evt.mousePosition - dragOffset;

           // Apply window snapping
           if (enableWindowSnapping)
           {
               newPosition = ApplyWindowSnapping(newPosition, window);
           }

           // Keep window within desktop bounds
           newPosition = ClampToDesktopBounds(newPosition, window);

           window.position = newPosition;
           window.element.style.left = newPosition.x;
           window.element.style.top = newPosition.y;
       }

       private void OnTitleBarClick(ClickEvent evt, DesktopWindow window)
       {
           var currentTime = Time.time;
           if (currentTime - lastClickTime < doubleClickTime)
           {
               // Double-click detected
               ToggleMaximizeWindow(window);
           }
           lastClickTime = currentTime;
       }

       private Vector2 ApplyWindowSnapping(Vector2 position, DesktopWindow window)
       private Vector2 ClampToDesktopBounds(Vector2 position, DesktopWindow window)
       private void RegisterResizeEvents(VisualElement resizeHandle, DesktopWindow window)
   }
   ```
   - **Deliverable:** Complete window interaction system with drag, resize, and snapping
   - **Success Criteria:** Windows behave like native desktop applications

3. **Desktop Theme System**
   ```csharp
   // File: Assets/Scripts/UI/Themes/DesktopTheme.cs
   [CreateAssetMenu(fileName = "Desktop Theme", menuName = "Coalition/UI/Desktop Theme")]
   public class DesktopTheme : ScriptableObject
   {
       [Header("Theme Information")]
       public string themeName;
       public string themeDescription;
       public Sprite themePreview;

       [Header("Color Scheme")]
       public Color primaryColor = new Color(0.2f, 0.4f, 0.8f);
       public Color secondaryColor = new Color(0.8f, 0.8f, 0.8f);
       public Color backgroundColor = new Color(0.95f, 0.95f, 0.95f);
       public Color textColor = Color.black;
       public Color accentColor = new Color(1f, 0.6f, 0f);

       [Header("Window Styling")]
       public Color windowBackgroundColor = Color.white;
       public Color windowBorderColor = new Color(0.7f, 0.7f, 0.7f);
       public Color titleBarColor = new Color(0.9f, 0.9f, 0.9f);
       public Color titleBarTextColor = Color.black;
       public float windowBorderWidth = 1f;
       public float windowCornerRadius = 5f;

       [Header("Typography")]
       public Font primaryFont;
       public Font secondaryFont;
       public int titleFontSize = 14;
       public int bodyFontSize = 12;
       public int smallFontSize = 10;

       [Header("Effects")]
       public bool enableDropShadows = true;
       public Color shadowColor = new Color(0f, 0f, 0f, 0.3f);
       public Vector2 shadowOffset = new Vector2(2f, 2f);
       public float shadowBlur = 4f;

       public void ApplyToDesktop(WindowManager windowManager)
       {
           var rootElement = windowManager.GetComponent<UIDocument>().rootVisualElement;

           // Apply theme colors and styles
           ApplyRootStyles(rootElement);

           // Update all existing windows
           foreach (var window in windowManager.GetActiveWindows())
           {
               ApplyToWindow(window);
           }
       }

       public void ApplyToWindow(DesktopWindow window)
       {
           var windowElement = window.element;

           // Window background and border
           windowElement.style.backgroundColor = windowBackgroundColor;
           windowElement.style.borderTopColor = windowBorderColor;
           windowElement.style.borderRightColor = windowBorderColor;
           windowElement.style.borderBottomColor = windowBorderColor;
           windowElement.style.borderLeftColor = windowBorderColor;
           windowElement.style.borderTopWidth = windowBorderWidth;
           windowElement.style.borderRightWidth = windowBorderWidth;
           windowElement.style.borderBottomWidth = windowBorderWidth;
           windowElement.style.borderLeftWidth = windowBorderWidth;
           windowElement.style.borderTopLeftRadius = windowCornerRadius;
           windowElement.style.borderTopRightRadius = windowCornerRadius;
           windowElement.style.borderBottomLeftRadius = windowCornerRadius;
           windowElement.style.borderBottomRightRadius = windowCornerRadius;

           // Title bar styling
           var titleBar = windowElement.Q<VisualElement>("title-bar");
           if (titleBar != null)
           {
               titleBar.style.backgroundColor = titleBarColor;
               var titleText = titleBar.Q<Label>("title-text");
               if (titleText != null)
               {
                   titleText.style.color = titleBarTextColor;
                   titleText.style.unityFont = primaryFont;
                   titleText.style.fontSize = titleFontSize;
               }
           }

           // Drop shadow effect
           if (enableDropShadows)
           {
               ApplyDropShadow(windowElement);
           }
       }

       private void ApplyRootStyles(VisualElement rootElement)
       private void ApplyDropShadow(VisualElement element)
   }
   ```
   - **Deliverable:** Comprehensive desktop theming system
   - **Success Criteria:** Multiple themes available, smooth theme switching, professional appearance

### 5.2 Desktop Application Shell
**Complexity:** Medium
**Prerequisites:** Window manager completed
**Parallel Opportunity:** ✅ Can develop shell independently of specific application panels

#### Tasks:
1. **Desktop Shell Implementation**
   ```csharp
   // File: Assets/Scripts/UI/DesktopShell.cs
   public class DesktopShell : MonoBehaviour
   {
       [Header("Shell Configuration")]
       public WindowManager windowManager;
       public List<ApplicationDefinition> availableApplications;
       public DesktopTheme defaultTheme;

       [Header("Taskbar")]
       public bool showTaskbar = true;
       public TaskbarPosition taskbarPosition = TaskbarPosition.Bottom;
       public int taskbarHeight = 40;

       [Header("Desktop")]
       public bool showDesktopIcons = true;
       public Vector2 iconSize = new Vector2(64, 64);
       public float iconSpacing = 20f;

       private UIDocument uiDocument;
       private VisualElement desktop;
       private VisualElement taskbar;
       private List<ApplicationWindow> runningApplications;

       void Start()
       {
           InitializeDesktopShell();
           CreateTaskbar();
           CreateDesktopIcons();
           ApplyTheme(defaultTheme);
       }

       public void LaunchApplication(ApplicationDefinition app)
       {
           // Check if application allows multiple instances
           if (!app.allowMultipleInstances)
           {
               var existing = runningApplications.FirstOrDefault(a => a.definition.applicationId == app.applicationId);
               if (existing != null)
               {
                   windowManager.BringWindowToFront(existing.window);
                   return;
               }
           }

           var windowConfig = new WindowConfiguration
           {
               title = app.displayName,
               defaultSize = app.defaultWindowSize,
               minSize = app.minimumWindowSize,
               isResizable = app.isResizable,
               onWindowClosed = () => OnApplicationClosed(app),
               onWindowFocused = () => OnApplicationFocused(app)
           };

           var window = windowManager.CreateWindow(windowConfig);
           if (window != null)
           {
               var appWindow = new ApplicationWindow
               {
                   definition = app,
                   window = window,
                   startTime = DateTime.Now
               };

               runningApplications.Add(appWindow);

               // Load application content
               LoadApplicationContent(appWindow);

               // Add to taskbar
               AddToTaskbar(appWindow);
           }
       }

       private void InitializeDesktopShell()
       private void CreateTaskbar()
       private void CreateDesktopIcons()
       private void LoadApplicationContent(ApplicationWindow appWindow)
   }

   [System.Serializable]
   public class ApplicationDefinition
   {
       public string applicationId;
       public string displayName;
       public Sprite applicationIcon;
       public string description;
       public Vector2 defaultWindowSize = new Vector2(800, 600);
       public Vector2 minimumWindowSize = new Vector2(400, 300);
       public bool isResizable = true;
       public bool allowMultipleInstances = false;
       public string contentPrefabPath; // Resources path to UI content
       public List<string> requiredPermissions;
   }

   public enum TaskbarPosition
   {
       Top,
       Bottom,
       Left,
       Right
   }
   ```
   - **Deliverable:** Complete desktop shell with taskbar and application launcher
   - **Success Criteria:** Applications launch in windows, taskbar shows running apps

2. **Context Menu System**
   ```csharp
   // File: Assets/Scripts/UI/ContextMenuSystem.cs
   public class ContextMenuSystem : MonoBehaviour
   {
       [Header("Context Menu Configuration")]
       public VisualTreeAsset contextMenuTemplate;
       public float menuFadeTime = 0.2f;
       public bool closeOnClickAway = true;

       private VisualElement activeContextMenu;
       private UIDocument uiDocument;

       public void ShowContextMenu(Vector2 position, List<ContextMenuItem> menuItems)
       {
           HideContextMenu(); // Close any existing menu

           var menuElement = contextMenuTemplate.Instantiate();
           var menuContainer = menuElement.Q<VisualElement>("menu-container");

           // Position the menu
           menuElement.style.left = position.x;
           menuElement.style.top = position.y;
           menuElement.style.position = Position.Absolute;

           // Create menu items
           foreach (var item in menuItems)
           {
               var menuItemElement = CreateMenuItem(item);
               menuContainer.Add(menuItemElement);
           }

           // Add to UI and show
           uiDocument.rootVisualElement.Add(menuElement);
           activeContextMenu = menuElement;

           // Animate in
           StartCoroutine(AnimateContextMenu(menuElement, true));

           // Set up click-away handler
           if (closeOnClickAway)
           {
               uiDocument.rootVisualElement.RegisterCallback<MouseDownEvent>(OnClickAway, TrickleDown.TrickleDown);
           }
       }

       public void HideContextMenu()
       {
           if (activeContextMenu != null)
           {
               StartCoroutine(AnimateContextMenu(activeContextMenu, false, () =>
               {
                   uiDocument.rootVisualElement.Remove(activeContextMenu);
                   activeContextMenu = null;
               }));

               uiDocument.rootVisualElement.UnregisterCallback<MouseDownEvent>(OnClickAway, TrickleDown.TrickleDown);
           }
       }

       private VisualElement CreateMenuItem(ContextMenuItem item)
       {
           var itemElement = new VisualElement();
           itemElement.AddToClassList("context-menu-item");

           if (item.isSeparator)
           {
               itemElement.AddToClassList("separator");
               return itemElement;
           }

           var label = new Label(item.text);
           label.AddToClassList("menu-item-label");
           itemElement.Add(label);

           if (item.icon != null)
           {
               var icon = new VisualElement();
               icon.AddToClassList("menu-item-icon");
               icon.style.backgroundImage = new StyleBackground(item.icon);
               itemElement.Insert(0, icon);
           }

           if (!item.isEnabled)
           {
               itemElement.AddToClassList("disabled");
           }
           else
           {
               itemElement.RegisterCallback<ClickEvent>(evt =>
               {
                   item.action?.Invoke();
                   HideContextMenu();
               });
           }

           return itemElement;
       }

       private IEnumerator AnimateContextMenu(VisualElement menu, bool fadeIn, System.Action onComplete = null)
       private void OnClickAway(MouseDownEvent evt)
   }

   [System.Serializable]
   public class ContextMenuItem
   {
       public string text;
       public Sprite icon;
       public bool isEnabled = true;
       public bool isSeparator = false;
       public System.Action action;

       public static ContextMenuItem Separator() => new ContextMenuItem { isSeparator = true };
   }
   ```
   - **Deliverable:** Professional context menu system with animations
   - **Success Criteria:** Context menus appear on right-click, animate smoothly, support all standard features

### 5.3 Desktop UI Demo Scene
**Complexity:** Medium
**Prerequisites:** All desktop UI systems completed
**Parallel Opportunity:** ❌ Requires integration of all UI components

#### Tasks:
1. **Complete Desktop Interface Demo**
   - Scene: `Assets/Scenes/DesktopUIDemo.unity`
   - Multiple application windows running simultaneously
   - Taskbar with running applications
   - Context menus on right-click
   - Theme switching functionality
   - **Deliverable:** Full desktop environment simulation
   - **Success Criteria:** Feels like a native desktop application

2. **Political Application Integration**
   ```csharp
   // File: Assets/Scripts/UI/Applications/PoliticalDashboardApp.cs
   public class PoliticalDashboardApp : DesktopApplication
   {
       [Header("Dashboard Configuration")]
       public Transform pollChartContainer;
       public Transform partyListContainer;
       public Transform eventTimelineContainer;

       private PollingSystem pollingSystem;
       private EventBus eventBus;

       public override void Initialize(ApplicationWindow appWindow)
       {
           base.Initialize(appWindow);

           pollingSystem = FindObjectOfType<PollingSystem>();
           SetupDashboardPanels();
           SubscribeToEvents();
           StartRealTimeUpdates();
       }

       private void SetupDashboardPanels()
       {
           // Create polling chart
           var pollChart = CreateLineChart(pollChartContainer);
           pollChart.Initialize(pollingSystem.GetHistoricalPolls());

           // Create party list
           var partyList = CreatePartyListPanel(partyListContainer);
           partyList.PopulateParties(GameData.Instance.GetAllParties());

           // Create event timeline
           var timeline = CreateEventTimeline(eventTimelineContainer);
           timeline.LoadRecentEvents();
       }

       private void StartRealTimeUpdates()
       {
           InvokeRepeating(nameof(UpdateDashboard), 0f, 5f);
       }

       private void UpdateDashboard()
       {
           // Update charts and displays with latest data
           RefreshPollChart();
           UpdatePartyPopularity();
           AddRecentEvents();
       }

       public override void OnApplicationFocused()
       {
           // Refresh data when window gains focus
           UpdateDashboard();
       }
   }
   ```
   - **Deliverable:** Political dashboard as desktop application
   - **Success Criteria:** Dashboard runs in desktop window, updates in real-time

**Demo Milestone 5 Success Criteria:**
- ✅ Multiple windows can be opened, moved, resized, and closed
- ✅ Taskbar shows running applications
- ✅ Context menus work throughout the interface
- ✅ Themes can be switched dynamically
- ✅ Political systems integrate seamlessly into desktop interface
- ✅ Performance remains smooth with multiple windows
- ✅ Interface feels like native desktop application

---

## Phase 6: Full Integration & Final Demo
**Timeline:** 2-3 weeks
**Demo Milestone:** Complete political simulation with all systems integrated

### 6.1 System Integration
**Complexity:** Complex
**Prerequisites:** All previous phases completed
**Parallel Opportunity:** ❌ Requires all systems for integration

#### Tasks:
1. **Master Scene Creation**
   - Scene: `Assets/Scenes/FullSimulation.unity`
   - All systems initialized and connected
   - Desktop environment with political applications
   - Real-time data flow between all components
   - **Deliverable:** Complete integrated simulation
   - **Success Criteria:** All systems work together seamlessly

2. **End-to-End Data Flow Validation**
   ```csharp
   // File: Assets/Scripts/Core/SystemIntegrationValidator.cs
   public class SystemIntegrationValidator : MonoBehaviour
   {
       [Header("Integration Tests")]
       public bool runOnStart = false;
       public float testInterval = 30f; // Run integration tests every 30 seconds

       private Dictionary<string, SystemHealthCheck> systemChecks;

       void Start()
       {
           if (runOnStart)
           {
               StartCoroutine(RunContinuousIntegrationTests());
           }
       }

       private IEnumerator RunContinuousIntegrationTests()
       {
           while (true)
           {
               yield return new WaitForSeconds(testInterval);
               RunAllIntegrationTests();
           }
       }

       public IntegrationTestResults RunAllIntegrationTests()
       {
           var results = new IntegrationTestResults();

           // Test political system integration
           results.AddResult("PoliticalSystem", TestPoliticalSystemIntegration());

           // Test campaign system integration
           results.AddResult("CampaignSystem", TestCampaignSystemIntegration());

           // Test AI system integration
           results.AddResult("AISystem", TestAISystemIntegration());

           // Test UI system integration
           results.AddResult("UISystem", TestUISystemIntegration());

           // Test cross-system data flow
           results.AddResult("DataFlow", TestCrossSystemDataFlow());

           if (results.HasFailures)
           {
               Debug.LogError($"Integration test failures detected: {results.GetFailureSummary()}");
           }

           return results;
       }

       private bool TestCrossSystemDataFlow()
       {
           try
           {
               // Create a test political event and verify it propagates through all systems
               var testEvent = new PoliticalEvent
               {
                   eventType = PoliticalEventType.PolicyAnnouncement,
                   affectedParty = GameData.Instance.GetRandomParty(),
                   magnitude = 0.5f,
                   description = "Integration test policy announcement"
               };

               EventBus.Publish(new PoliticalEventOccurred { Event = testEvent });

               // Wait for systems to process
               yield return new WaitForSeconds(1f);

               // Verify each system responded appropriately
               var pollingSystemResponse = PollingSystem.Instance.GetLastEventResponse();
               var campaignSystemResponse = CampaignSystem.Instance.GetLastEventResponse();
               var uiSystemResponse = UIManager.Instance.GetLastEventResponse();

               return pollingSystemResponse != null &&
                      campaignSystemResponse != null &&
                      uiSystemResponse != null;
           }
           catch (System.Exception ex)
           {
               Debug.LogError($"Cross-system data flow test failed: {ex.Message}");
               return false;
           }
       }
   }
   ```
   - **Deliverable:** Comprehensive integration testing system
   - **Success Criteria:** All systems communicate correctly, no data flow issues

### 6.2 Performance Optimization
**Complexity:** Medium
**Prerequisites:** Full integration completed
**Parallel Opportunity:** ✅ Performance optimization can be done in parallel with polish tasks

#### Tasks:
1. **Performance Profiling and Optimization**
   ```csharp
   // File: Assets/Scripts/Core/PerformanceManager.cs
   public class PerformanceManager : MonoBehaviour
   {
       [Header("Performance Monitoring")]
       public bool enableProfiling = true;
       public float profilingInterval = 1f;
       public int maxFrameTimeHistory = 100;

       [Header("Performance Targets")]
       public float targetFrameRate = 60f;
       public float maxMemoryUsageMB = 2000f;
       public float maxGCAllocationsPerFrame = 1000f;

       private Queue<float> frameTimeHistory;
       private PerformanceMetrics currentMetrics;

       void Update()
       {
           if (enableProfiling)
           {
               UpdatePerformanceMetrics();
               CheckPerformanceThresholds();
           }
       }

       private void UpdatePerformanceMetrics()
       {
           currentMetrics.frameTime = Time.deltaTime;
           currentMetrics.fps = 1f / Time.deltaTime;
           currentMetrics.memoryUsage = Profiler.GetTotalAllocatedMemory(false) / (1024f * 1024f);

           // Track frame time history
           frameTimeHistory.Enqueue(currentMetrics.frameTime);
           if (frameTimeHistory.Count > maxFrameTimeHistory)
           {
               frameTimeHistory.Dequeue();
           }

           // Calculate average frame time
           currentMetrics.averageFrameTime = frameTimeHistory.Average();
       }

       private void CheckPerformanceThresholds()
       {
           // Frame rate check
           if (currentMetrics.fps < targetFrameRate * 0.8f) // 80% of target
           {
               TriggerPerformanceOptimization("Low frame rate detected");
           }

           // Memory usage check
           if (currentMetrics.memoryUsage > maxMemoryUsageMB)
           {
               TriggerMemoryOptimization("High memory usage detected");
           }
       }

       private void TriggerPerformanceOptimization(string reason)
       {
           Debug.LogWarning($"Performance optimization triggered: {reason}");

           // Reduce quality settings temporarily
           ReduceQualitySettings();

           // Force garbage collection
           System.GC.Collect();

           // Reduce update frequencies for non-critical systems
           OptimizeUpdateFrequencies();
       }

       private void ReduceQualitySettings()
       private void OptimizeUpdateFrequencies()
   }
   ```
   - **Deliverable:** Performance monitoring and automatic optimization
   - **Success Criteria:** Maintains 60+ FPS with all systems running

2. **Memory Management Optimization**
   ```csharp
   // File: Assets/Scripts/Core/MemoryManager.cs
   public class MemoryManager : MonoBehaviour
   {
       [Header("Memory Management")]
       public float garbageCollectionInterval = 60f; // Force GC every minute
       public int maxCachedResponses = 500;
       public int maxHistoricalPolls = 1000;

       private Coroutine garbageCollectionCoroutine;

       void Start()
       {
           garbageCollectionCoroutine = StartCoroutine(PeriodicGarbageCollection());
           OptimizeCacheSizes();
       }

       private IEnumerator PeriodicGarbageCollection()
       {
           while (true)
           {
               yield return new WaitForSeconds(garbageCollectionInterval);

               // Only run GC if memory usage is high
               var memoryUsage = Profiler.GetTotalAllocatedMemory(false);
               if (memoryUsage > 1024 * 1024 * 1024) // 1GB threshold
               {
                   System.GC.Collect();
                   Resources.UnloadUnusedAssets();
               }
           }
       }

       private void OptimizeCacheSizes()
       {
           // Optimize AI response cache
           var responseCache = FindObjectOfType<ResponseCache>();
           responseCache?.SetMaxCacheSize(maxCachedResponses);

           // Optimize polling history
           var pollingSystem = FindObjectOfType<PollingSystem>();
           pollingSystem?.SetMaxHistorySize(maxHistoricalPolls);

           // Clean up old social media posts
           var socialMediaSystems = FindObjectsOfType<SocialMediaPlatform>();
           foreach (var platform in socialMediaSystems)
           {
               platform.CleanupOldPosts();
           }
       }
   }
   ```
   - **Deliverable:** Efficient memory management system
   - **Success Criteria:** Memory usage remains stable over extended play sessions

### 6.3 Final Polish and Quality Assurance
**Complexity:** Medium
**Prerequisites:** Performance optimization completed
**Parallel Opportunity:** ✅ Polish tasks can be done in parallel

#### Tasks:
1. **UI Polish and Accessibility**
   ```csharp
   // File: Assets/Scripts/UI/AccessibilityManager.cs
   public class AccessibilityManager : MonoBehaviour
   {
       [Header("Accessibility Settings")]
       public bool enableHighContrast = false;
       public bool enableLargeText = false;
       public bool enableScreenReader = false;
       public float animationSpeed = 1f;

       [Header("Color Blind Support")]
       public ColorBlindnessType colorBlindnessType = ColorBlindnessType.None;
       public bool showColorBlindFriendlyPalette = false;

       public void ApplyAccessibilitySettings()
       {
           ApplyContrastSettings();
           ApplyTextSizeSettings();
           ApplyAnimationSettings();
           ApplyColorBlindnessAdjustments();
       }

       private void ApplyContrastSettings()
       {
           if (enableHighContrast)
           {
               // Apply high contrast theme
               var themeManager = FindObjectOfType<DesktopTheme>();
               // Apply high contrast colors
           }
       }

       private void ApplyColorBlindnessAdjustments()
       {
           if (colorBlindnessType != ColorBlindnessType.None)
           {
               // Adjust color palette for color blindness
               AdjustPoliticalPartyColors();
               AdjustChartColors();
               AdjustUIColors();
           }
       }

       private void AdjustPoliticalPartyColors()
       private void AdjustChartColors()
   }

   public enum ColorBlindnessType
   {
       None,
       Protanopia,    // Red-blind
       Deuteranopia,  // Green-blind
       Tritanopia     // Blue-blind
   }
   ```
   - **Deliverable:** Comprehensive accessibility support
   - **Success Criteria:** Application usable by users with various accessibility needs

2. **Quality Assurance Testing Suite**
   ```csharp
   // File: Assets/Scripts/Testing/QualityAssuranceTests.cs
   public class QualityAssuranceTests : MonoBehaviour
   {
       [Header("Test Configuration")]
       public bool runAutomatedTests = false;
       public bool runStressTests = false;
       public int stressTestDuration = 300; // 5 minutes

       public IEnumerator RunFullQualityAssuranceSuite()
       {
           var results = new QATestResults();

           // Functionality tests
           yield return StartCoroutine(TestPoliticalSystemFunctionality(results));
           yield return StartCoroutine(TestCampaignSystemFunctionality(results));
           yield return StartCoroutine(TestAISystemFunctionality(results));
           yield return StartCoroutine(TestUISystemFunctionality(results));

           // Performance tests
           yield return StartCoroutine(TestPerformanceUnderLoad(results));

           // Stability tests
           yield return StartCoroutine(TestLongRunningStability(results));

           // Integration tests
           yield return StartCoroutine(TestSystemIntegration(results));

           // Generate final report
           GenerateQAReport(results);
       }

       private IEnumerator TestPoliticalSystemFunctionality(QATestResults results)
       {
           var testResults = new List<TestResult>();

           // Test election system
           testResults.Add(TestElectionAccuracy());
           yield return new WaitForSeconds(1f);

           // Test coalition formation
           testResults.Add(TestCoalitionFormation());
           yield return new WaitForSeconds(1f);

           // Test polling system
           testResults.Add(TestPollingSystem());
           yield return new WaitForSeconds(1f);

           results.politicalSystemTests = testResults;
       }

       private TestResult TestElectionAccuracy()
       {
           try
           {
               // Run test election with known input
               var testParties = CreateTestParties();
               var results = ElectoralSystem.Instance.ConductElection(testParties);

               // Verify mathematical accuracy
               var expectedResults = CalculateExpectedResults(testParties);
               var isAccurate = CompareResults(results, expectedResults, 0.01f); // 1% tolerance

               return new TestResult
               {
                   testName = "Election Accuracy",
                   passed = isAccurate,
                   message = isAccurate ? "Election calculations accurate" : "Election calculations inaccurate"
               };
           }
           catch (System.Exception ex)
           {
               return new TestResult
               {
                   testName = "Election Accuracy",
                   passed = false,
                   message = $"Test failed with exception: {ex.Message}"
               };
           }
       }

       private void GenerateQAReport(QATestResults results)
   }
   ```
   - **Deliverable:** Comprehensive automated testing suite
   - **Success Criteria:** All critical functionality tests pass, performance meets targets

**Final Demo Success Criteria:**
- ✅ Complete political simulation runs without errors
- ✅ All systems integrate seamlessly
- ✅ Performance remains stable at 60+ FPS
- ✅ Memory usage stays within acceptable limits
- ✅ UI is polished and accessible
- ✅ AI integration works reliably
- ✅ Desktop interface feels native and professional
- ✅ Campaign mechanics affect political outcomes realistically
- ✅ Dutch political system is authentically represented

---

## Demo Milestone Tracking

### Milestone 1: Foundation (Week 2-3)
- **Core Achievement:** Basic Unity project with Dutch political parties displayed
- **Key Features:** Party data system, event bus, basic UI
- **Success Metrics:** 12 parties display correctly, real-time updates work
- **Performance Target:** 60+ FPS, no console errors

### Milestone 2: Political Engine (Week 5-6)
- **Core Achievement:** Interactive election and coalition formation
- **Key Features:** D'Hondt electoral system, coalition mathematics, government formation
- **Success Metrics:** Accurate electoral calculations, realistic coalitions formed
- **Performance Target:** Elections complete in <5 seconds

### Milestone 3: Campaign Systems (Week 9-10)
- **Core Achievement:** Full campaign system with social media and debates
- **Key Features:** Multi-platform social media, debate system, rally mechanics
- **Success Metrics:** Campaign actions affect polling, realistic media coverage
- **Performance Target:** Real-time social media updates, smooth debate playback

### Milestone 4: AI Integration (Week 13-14)
- **Core Achievement:** AI-powered political responses via NVIDIA NIM
- **Key Features:** NIM client, response generation, fallback systems
- **Success Metrics:** Contextual political responses, 60%+ cache hit rate
- **Performance Target:** AI responses in <10 seconds

### Milestone 5: Desktop UI (Week 17-18)
- **Core Achievement:** Multi-window desktop application interface
- **Key Features:** Window management, taskbar, themes, context menus
- **Success Metrics:** Native desktop feel, multiple windows supported
- **Performance Target:** Smooth window operations, no UI lag

### Milestone 6: Full Integration (Week 20-21)
- **Core Achievement:** Complete political simulation with all systems
- **Key Features:** End-to-end integration, performance optimization, QA testing
- **Success Metrics:** All systems work together, automated tests pass
- **Performance Target:** Stable 60+ FPS with all systems active

---

## Implementation Guidelines for AI Agents

### Code Standards
- **Unity Conventions:** Follow Unity's C# coding standards and naming conventions
- **Architecture Patterns:** Use ScriptableObjects for data, singletons for managers, events for communication
- **Error Handling:** Always include try-catch blocks for external API calls and file operations
- **Documentation:** Include XML documentation comments for all public methods and classes
- **Testing:** Write unit tests for core logic, integration tests for system interactions

### File Organization Rules
- **Absolute Paths:** Always use absolute paths in task descriptions
- **Consistent Naming:** Use PascalCase for classes, camelCase for methods and variables
- **Logical Grouping:** Group related functionality in appropriate folders
- **Resource Management:** Use Resources.Load() for runtime assets, Addressables for dynamic content

### Quality Gates
- **Compilation:** Code must compile without errors or warnings
- **Performance:** Maintain 60+ FPS during normal operation
- **Memory:** No memory leaks, efficient garbage collection
- **Integration:** New systems must integrate with existing event bus
- **Testing:** All critical paths must have automated tests

### Common Pitfalls to Avoid
- **Unity-Specific:** Don't use Update() for expensive operations, use coroutines or timers
- **Memory Management:** Avoid creating garbage in Update() methods
- **Threading:** Use Unity's main thread for all Unity API calls
- **Serialization:** Use Unity's serialization for ScriptableObjects, JSON for external data
- **Performance:** Profile before optimizing, measure impact of changes

This comprehensive roadmap provides AI agents with clear, implementable tasks that build toward a complete political simulation. Each phase produces working functionality while maintaining professional code quality and performance standards.