using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Coalition.Runtime.Core;
using Coalition.Political.Coalitions;

namespace Coalition.Demo
{
    /// <summary>
    /// Demonstration script for Coalition Formation Core system
    /// Showcases multi-dimensional compatibility scoring, viable coalition detection,
    /// and authentic Dutch political scenarios based on 2023 election results
    ///
    /// Usage: Attach to GameObject and run demo methods through Unity Inspector or code
    /// </summary>
    public class CoalitionFormationDemo : MonoBehaviour
    {
        [Header("Demo Configuration")]
        [SerializeField] private bool runDemoOnStart = true;
        [SerializeField] private bool enableDetailedLogging = true;
        [SerializeField] private bool showPerformanceMetrics = true;

        [Header("Demo Scenarios")]
        [SerializeField] private bool demoCompatibilityCalculation = true;
        [SerializeField] private bool demoViableCoalitionDetection = true;
        [SerializeField] private bool demoAuthenticScenarios = true;
        [SerializeField] private bool demoHistoricalValidation = true;
        [SerializeField] private bool demoRealTimeUpdates = true;

        private List<PoliticalParty> _dutchParties;
        private List<ElectionResult> _2023Results;
        private CoalitionFormationManager _coalitionManager;

        #region Unity Lifecycle

        private void Start()
        {
            InitializeDemo();

            if (runDemoOnStart)
            {
                RunCompleteDemo();
            }
        }

        #endregion

        #region Demo Initialization

        private void InitializeDemo()
        {
            Debug.Log("=== COALITION FORMATION CORE SYSTEM DEMO ===");
            Debug.Log("Initializing Dutch political data and election results...\n");

            // Initialize Dutch political parties
            _dutchParties = DutchPoliticalDataGenerator.GenerateAllDutchParties();
            Debug.Log($"✓ Loaded {_dutchParties.Count} Dutch political parties");

            // Calculate 2023 election results
            var votes = DHondtElectionSystem.Get2023ElectionVotes();
            _2023Results = DHondtElectionSystem.CalculateSeats(votes);
            Debug.Log($"✓ Calculated 2023 election results using D'Hondt system");

            // Initialize coalition formation manager
            var managerObject = new GameObject("CoalitionFormationManager");
            managerObject.transform.SetParent(transform);
            _coalitionManager = managerObject.AddComponent<CoalitionFormationManager>();
            Debug.Log($"✓ Coalition Formation Manager initialized");

            // Validate data integrity
            if (DutchPoliticalDataGenerator.ValidateGeneratedData())
            {
                Debug.Log($"✓ Dutch political data validation: PASSED\n");
            }
            else
            {
                Debug.LogWarning($"⚠ Dutch political data validation: FAILED\n");
            }
        }

        #endregion

        #region Complete Demo

        /// <summary>
        /// Run the complete demonstration of all coalition formation features
        /// </summary>
        [ContextMenu("Run Complete Demo")]
        public void RunCompleteDemo()
        {
            if (demoCompatibilityCalculation)
                DemoCompatibilityCalculation();

            if (demoViableCoalitionDetection)
                DemoViableCoalitionDetection();

            if (demoAuthenticScenarios)
                DemoAuthenticDutchScenarios();

            if (demoHistoricalValidation)
                DemoHistoricalValidation();

            if (demoRealTimeUpdates)
                DemoRealTimeUpdates();

            Debug.Log("=== COALITION FORMATION DEMO COMPLETE ===\n");
        }

        #endregion

        #region Compatibility Calculation Demo

        [ContextMenu("Demo: Compatibility Calculation")]
        public void DemoCompatibilityCalculation()
        {
            Debug.Log("--- MULTI-DIMENSIONAL COMPATIBILITY DEMO ---");

            // Demo 1: High compatibility (VVD-D66 Purple Coalition)
            var vvd = GetParty("VVD");
            var d66 = GetParty("D66");
            var purpleParties = new List<PoliticalParty> { vvd, d66 };

            var startTime = DateTime.Now;
            float purpleCompatibility = CoalitionFormationSystem.CalculateCompatibility(purpleParties);
            var calculationTime = (DateTime.Now - startTime).TotalMilliseconds;

            Debug.Log($"🟢 Purple Coalition (VVD-D66):");
            Debug.Log($"   Compatibility Score: {purpleCompatibility:F3}");
            Debug.Log($"   Calculation Time: {calculationTime:F2}ms");
            Debug.Log($"   Historical Context: Traditional liberal partnership");

            // Demo 2: Low compatibility (PVV-GL/PvdA)
            var pvv = GetParty("PVV");
            var glpvda = GetParty("GL-PvdA");
            var oppositeParties = new List<PoliticalParty> { pvv, glpvda };

            startTime = DateTime.Now;
            float oppositeCompatibility = CoalitionFormationSystem.CalculateCompatibility(oppositeParties);
            calculationTime = (DateTime.Now - startTime).TotalMilliseconds;

            Debug.Log($"🔴 Ideological Opposites (PVV - GL/PvdA):");
            Debug.Log($"   Compatibility Score: {oppositeCompatibility:F3}");
            Debug.Log($"   Calculation Time: {calculationTime:F2}ms");
            Debug.Log($"   Analysis: Fundamental ideological incompatibility");

            // Demo 3: Christian partnership (CDA-CU)
            var cda = GetParty("CDA");
            var cu = GetParty("CU");
            var christianParties = new List<PoliticalParty> { cda, cu };

            startTime = DateTime.Now;
            float christianCompatibility = CoalitionFormationSystem.CalculateCompatibility(christianParties);
            calculationTime = (DateTime.Now - startTime).TotalMilliseconds;

            Debug.Log($"⛪ Christian Partnership (CDA-CU):");
            Debug.Log($"   Compatibility Score: {christianCompatibility:F3}");
            Debug.Log($"   Calculation Time: {calculationTime:F2}ms");
            Debug.Log($"   Analysis: Strong historical and ideological alignment");

            if (enableDetailedLogging)
            {
                Debug.Log($"\n📊 Ideological Analysis Details:");
                LogIdeologicalPositions(vvd, "VVD");
                LogIdeologicalPositions(d66, "D66");
                LogIdeologicalPositions(pvv, "PVV");
                LogIdeologicalPositions(glpvda, "GL-PvdA");
            }

            Debug.Log("");
        }

        #endregion

        #region Viable Coalition Detection Demo

        [ContextMenu("Demo: Viable Coalition Detection")]
        public void DemoViableCoalitionDetection()
        {
            Debug.Log("--- VIABLE COALITION DETECTION DEMO ---");

            var startTime = DateTime.Now;
            var analysis = CoalitionFormationSystem.DetectViableCoalitions(_2023Results);
            var analysisTime = (DateTime.Now - startTime).TotalMilliseconds;

            Debug.Log($"🔍 Coalition Analysis Results:");
            Debug.Log($"   Analysis Time: {analysisTime:F0}ms");
            Debug.Log($"   Combinations Analyzed: {analysis.TotalCombinationsAnalyzed}");
            Debug.Log($"   Viable Coalitions Found: {analysis.ViableCoalitions.Count}");
            Debug.Log($"   Minority Options: {analysis.MinorityOptions.Count}");
            Debug.Log($"   Blocked Coalitions: {analysis.BlockedCoalitions.Count}");

            // Show top 5 most compatible coalitions
            Debug.Log($"\n🏆 Top 5 Most Compatible Coalitions:");
            for (int i = 0; i < Math.Min(5, analysis.ViableCoalitions.Count); i++)
            {
                var coalition = analysis.ViableCoalitions[i];
                Debug.Log($"   #{i + 1}: {string.Join("-", coalition.PartyNames)}");
                Debug.Log($"        Seats: {coalition.TotalSeats}, Compatibility: {coalition.CompatibilityScore:F3}");
                Debug.Log($"        Type: {coalition.Type}, Stability: {coalition.StabilityFactor:F3}");

                if (coalition.RedLineViolations.Count > 0)
                {
                    Debug.Log($"        ⚠ Red-line violations: {string.Join("; ", coalition.RedLineViolations)}");
                }
            }

            // Highlight special coalitions
            if (analysis.MostCompatible != null)
            {
                Debug.Log($"\n🎯 Most Compatible Coalition:");
                Debug.Log($"   {string.Join("-", analysis.MostCompatible.PartyNames)}");
                Debug.Log($"   Compatibility: {analysis.MostCompatible.CompatibilityScore:F3}");
            }

            if (analysis.MostStable != null)
            {
                Debug.Log($"\n🛡️ Most Stable Coalition:");
                Debug.Log($"   {string.Join("-", analysis.MostStable.PartyNames)}");
                Debug.Log($"   Stability Factor: {analysis.MostStable.StabilityFactor:F3}");
            }

            // Performance validation
            if (showPerformanceMetrics)
            {
                Debug.Log($"\n⚡ Performance Metrics:");
                Debug.Log($"   Target: <5 seconds for full analysis");
                Debug.Log($"   Actual: {analysisTime / 1000f:F2} seconds");
                Debug.Log($"   Result: {(analysisTime < 5000 ? "✓ PASSED" : "✗ FAILED")}");
                Debug.Log($"   Analysis Rate: {analysis.TotalCombinationsAnalyzed / (analysisTime / 1000f):F0} combinations/second");
            }

            Debug.Log("");
        }

        #endregion

        #region Authentic Dutch Scenarios Demo

        [ContextMenu("Demo: Authentic Dutch Scenarios")]
        public void DemoAuthenticDutchScenarios()
        {
            Debug.Log("--- AUTHENTIC DUTCH COALITION SCENARIOS ---");

            var scenarios = CoalitionFormationSystem.GenerateAuthenticScenarios(_2023Results);

            Debug.Log($"📋 Generated {scenarios.Count} authentic scenarios based on Dutch political history:");

            foreach (var scenario in scenarios)
            {
                var coalition = scenario.Value;
                Debug.Log($"\n🏛️ {scenario.Key}:");
                Debug.Log($"   Parties: {string.Join(", ", coalition.PartyNames)}");
                Debug.Log($"   Total Seats: {coalition.TotalSeats}/150 ({(float)coalition.TotalSeats / 150 * 100:F1}%)");
                Debug.Log($"   Compatibility: {coalition.CompatibilityScore:F3}");
                Debug.Log($"   Viability: {(coalition.IsViable ? "✓ Majority" : "✗ Minority")}");
                Debug.Log($"   Coalition Type: {coalition.Type}");

                if (coalition.RedLineViolations.Count > 0)
                {
                    Debug.Log($"   ⚠ Political Challenges: {string.Join("; ", coalition.RedLineViolations)}");
                }

                // Add historical context
                AddHistoricalContext(scenario.Key, coalition);
            }

            Debug.Log("");
        }

        #endregion

        #region Historical Validation Demo

        [ContextMenu("Demo: Historical Validation")]
        public void DemoHistoricalValidation()
        {
            Debug.Log("--- HISTORICAL VALIDATION DEMO ---");

            var analysis = CoalitionFormationSystem.DetectViableCoalitions(_2023Results);
            float accuracy = CoalitionFormationSystem.ValidateAgainstHistory(analysis);

            Debug.Log($"📚 Historical Validation Results:");
            Debug.Log($"   Accuracy: {accuracy:F1}%");
            Debug.Log($"   Target: >90% match with expert assessments");
            Debug.Log($"   Result: {(accuracy >= 90.0f ? "✓ PASSED" : "✗ NEEDS IMPROVEMENT")}");

            // Validate specific historical patterns
            Debug.Log($"\n🔍 Validating Known Historical Patterns:");

            // Rutte coalition patterns
            ValidateRutteCoalitions();

            // Purple coalition history
            ValidatePurpleCoalitions();

            // Christian coalition patterns
            ValidateChristianCoalitions();

            Debug.Log("");
        }

        #endregion

        #region Real-Time Updates Demo

        [ContextMenu("Demo: Real-Time Updates")]
        public void DemoRealTimeUpdates()
        {
            Debug.Log("--- REAL-TIME COALITION UPDATES DEMO ---");

            // Subscribe to events for demonstration
            bool compatibilityReceived = false;
            bool redLineReceived = false;

            EventBus.Subscribe<CoalitionCompatibilityUpdatedEvent>(evt => {
                compatibilityReceived = true;
                Debug.Log($"📊 Real-time Update:");
                Debug.Log($"   Selected: {string.Join(", ", evt.SelectedParties)}");
                Debug.Log($"   Compatibility: {evt.CompatibilityScore:F3}");
                Debug.Log($"   Seats: {evt.TotalSeats}/150");
                Debug.Log($"   Viable: {(evt.IsViable ? "✓ Yes" : "✗ No")}");

                if (evt.HasRedLineViolations)
                {
                    Debug.Log($"   ⚠ Red-line violations detected");
                }
            });

            EventBus.Subscribe<RedLineViolationDetectedEvent>(evt => {
                redLineReceived = true;
                Debug.Log($"🚨 Red-line Violation: {evt.ViolatingParty} excludes {evt.ExcludedParty}");
            });

            // Simulate party selection sequence
            Debug.Log($"🎮 Simulating Interactive Party Selection:");

            Debug.Log($"\n1. Selecting VVD (liberal center-right)...");
            _coalitionManager.HandlePartySelection("VVD");

            Debug.Log($"\n2. Adding D66 (progressive liberal)...");
            _coalitionManager.HandlePartySelection("D66");

            Debug.Log($"\n3. Adding PVV (right-wing populist) - should trigger warnings...");
            _coalitionManager.HandlePartySelection("PVV");

            Debug.Log($"\n4. Adding GL-PvdA (progressive left) - major incompatibility...");
            _coalitionManager.HandlePartySelection("GL-PvdA");

            // Show current state
            var currentParties = _coalitionManager.GetSelectedParties();
            var currentScore = _coalitionManager.GetCurrentCompatibilityScore();
            var isViable = _coalitionManager.IsCurrentCoalitionViable();

            Debug.Log($"\n📈 Final Coalition State:");
            Debug.Log($"   Parties: {string.Join(", ", currentParties)}");
            Debug.Log($"   Compatibility: {currentScore:F3}");
            Debug.Log($"   Viable: {(isViable ? "✓ Yes" : "✗ No")}");

            // Clear coalition
            Debug.Log($"\n🧹 Clearing coalition...");
            _coalitionManager.ClearCoalition();

            // Unsubscribe from events
            EventBus.Unsubscribe<CoalitionCompatibilityUpdatedEvent>(evt => compatibilityReceived = true);
            EventBus.Unsubscribe<RedLineViolationDetectedEvent>(evt => redLineReceived = true);

            Debug.Log($"\n✓ Real-time updates demo completed");
            Debug.Log($"   Compatibility events: {(compatibilityReceived ? "✓ Received" : "✗ Not received")}");
            Debug.Log($"   Red-line events: {(redLineReceived ? "✓ Received" : "✗ Not received")}");

            Debug.Log("");
        }

        #endregion

        #region Helper Methods

        private PoliticalParty GetParty(string abbreviation)
        {
            return _dutchParties.FirstOrDefault(p => p.Abbreviation == abbreviation);
        }

        private void LogIdeologicalPositions(PoliticalParty party, string name)
        {
            Debug.Log($"   {name}: Economic={party.EconomicPosition:F1}, Social={party.SocialPosition:F1}, " +
                     $"Europe={party.EuropeanPosition:F1}, Immigration={party.ImmigrationPosition:F1}");
        }

        private void AddHistoricalContext(string scenarioName, CoalitionFormationSystem.Coalition coalition)
        {
            switch (scenarioName)
            {
                case "Current Government":
                    Debug.Log($"   📖 Context: 2023-2025 coalition, first PVV-led government, collapsed June 2025");
                    break;
                case "Purple Coalition":
                    Debug.Log($"   📖 Context: Historic liberal-social democrat partnership (1994-2002, 2010-2012)");
                    break;
                case "Left Coalition":
                    Debug.Log($"   📖 Context: Progressive alternative, would be first left-majority since 1994");
                    break;
                case "Right Coalition":
                    Debug.Log($"   📖 Context: Conservative bloc, significant ideological alignment on key issues");
                    break;
                case "Center Coalition":
                    Debug.Log($"   📖 Context: Traditional centrist governance, excludes populist extremes");
                    break;
                case "Grand Coalition":
                    Debug.Log($"   📖 Context: Broad consensus government, rare in Dutch politics");
                    break;
                case "Minority Government":
                    Debug.Log($"   📖 Context: Would require external support, unusual for Netherlands");
                    break;
            }
        }

        private void ValidateRutteCoalitions()
        {
            Debug.Log($"   🏛️ Rutte Coalition Patterns:");

            // Rutte I (VVD-CDA with PVV support)
            var rutteI = new[] { "VVD", "CDA" };
            var parties = rutteI.Select(GetParty).Where(p => p != null).ToList();
            float compatibility = CoalitionFormationSystem.CalculateCompatibility(parties);
            Debug.Log($"     Rutte I pattern (VVD-CDA): {compatibility:F3} compatibility");

            // Rutte III (VVD-CDA-D66-CU)
            var rutteIII = new[] { "VVD", "CDA", "D66", "CU" };
            parties = rutteIII.Select(GetParty).Where(p => p != null).ToList();
            compatibility = CoalitionFormationSystem.CalculateCompatibility(parties);
            Debug.Log($"     Rutte III pattern (VVD-CDA-D66-CU): {compatibility:F3} compatibility");
        }

        private void ValidatePurpleCoalitions()
        {
            Debug.Log($"   💜 Purple Coalition Patterns:");

            var purpleParties = new[] { "VVD", "D66" };
            var parties = purpleParties.Select(GetParty).Where(p => p != null).ToList();
            float compatibility = CoalitionFormationSystem.CalculateCompatibility(parties);
            Debug.Log($"     Liberal core (VVD-D66): {compatibility:F3} compatibility");
        }

        private void ValidateChristianCoalitions()
        {
            Debug.Log($"   ⛪ Christian Coalition Patterns:");

            var christianParties = new[] { "CDA", "CU" };
            var parties = christianParties.Select(GetParty).Where(p => p != null).ToList();
            float compatibility = CoalitionFormationSystem.CalculateCompatibility(parties);
            Debug.Log($"     Christian partnership (CDA-CU): {compatibility:F3} compatibility");
        }

        #endregion

        #region Public API for Inspector

        [ContextMenu("Show Performance Stats")]
        public void ShowPerformanceStats()
        {
            if (_coalitionManager != null)
            {
                Debug.Log(_coalitionManager.GetPerformanceStats());
            }
        }

        [ContextMenu("List Available Scenarios")]
        public void ListAvailableScenarios()
        {
            if (_coalitionManager != null)
            {
                var scenarios = _coalitionManager.GetAvailableScenarios();
                Debug.Log($"Available Coalition Scenarios: {string.Join(", ", scenarios)}");
            }
        }

        [ContextMenu("Test Full Analysis Performance")]
        public void TestFullAnalysisPerformance()
        {
            if (_coalitionManager != null)
            {
                _coalitionManager.PerformFullAnalysis();
            }
        }

        #endregion
    }
}