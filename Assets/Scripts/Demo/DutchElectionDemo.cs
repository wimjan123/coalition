using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Coalition.Political.Elections;
using Coalition.Political.Parties;

namespace Coalition.Demo
{
    /// <summary>
    /// Demonstration script showcasing the Dutch Election System implementation
    /// Validates Steps 1.2-1.3 from COALITION Demo Plan
    /// </summary>
    public class DutchElectionDemo : MonoBehaviour
    {
        [Header("Demo Configuration")]
        [SerializeField] private bool runOnStart = true;
        [SerializeField] private bool validateAgainst2023 = true;
        [SerializeField] private bool showCoalitionAnalysis = true;

        private void Start()
        {
            if (runOnStart)
            {
                RunCompleteDemonstration();
            }
        }

        /// <summary>
        /// Run complete demonstration of Dutch Election System
        /// </summary>
        [ContextMenu("Run Complete Demonstration")]
        public void RunCompleteDemonstration()
        {
            Debug.Log("🇳🇱 === COALITION DEMO: Dutch Election System Demonstration ===");
            Debug.Log("");

            // Step 1: Demonstrate Political Party Data Generation
            DemonstratePartyDataGeneration();

            // Step 2: Demonstrate D'Hondt Algorithm
            DemonstrateDHondtAlgorithm();

            if (validateAgainst2023)
            {
                // Step 3: Validate Against 2023 Results
                ValidateAgainst2023Election();
            }

            if (showCoalitionAnalysis)
            {
                // Step 4: Demonstrate Coalition Analysis
                DemonstrateCoalitionAnalysis();
            }

            // Step 5: Performance Summary
            ShowPerformanceSummary();

            Debug.Log("✅ === COALITION DEMO: Complete Dutch Election System Validated ===");
        }

        private void DemonstratePartyDataGeneration()
        {
            Debug.Log("📋 Step 1.2: Dutch Political Party Data Generation");

            var startTime = Time.realtimeSinceStartup;
            var parties = DutchPoliticalDataGenerator.GenerateAllDutchParties();
            var generationTime = (Time.realtimeSinceStartup - startTime) * 1000f;

            Debug.Log($"✅ Generated {parties.Count} Dutch political parties in {generationTime:F2}ms");

            // Show sample parties
            var sampleParties = parties.Take(5).ToList();
            foreach (var party in sampleParties)
            {
                Debug.Log($"   • {party.Abbreviation} ({party.PartyName}): {party.Leader}");
                Debug.Log($"     Ideology: Economic {party.EconomicPosition:+0;-0}, Social {party.SocialPosition:+0;-0}, EU {party.EuropeanPosition:+0;-0}, Immigration {party.ImmigrationPosition:+0;-0}");
                Debug.Log($"     Popularity: {party.CurrentPopularity:F1}%, Coalition Flexibility: {party.CoalitionFlexibility:F0}%");
            }

            Debug.Log($"   ... and {parties.Count - 5} more parties");

            // Validate data integrity
            bool dataValid = DutchPoliticalDataGenerator.ValidateGeneratedData();
            Debug.Log($"📊 Data Validation: {(dataValid ? "PASSED" : "FAILED")}");

            // Cleanup
            foreach (var party in parties)
            {
                if (party != null) DestroyImmediate(party);
            }

            Debug.Log("");
        }

        private void DemonstrateDHondtAlgorithm()
        {
            Debug.Log("🗳️ Step 1.3: D'Hondt Electoral Algorithm Demonstration");

            var startTime = Time.realtimeSinceStartup;
            var votes2023 = DHondtElectionSystem.Get2023ElectionVotes();
            var results = DHondtElectionSystem.CalculateSeats(votes2023);
            var calculationTime = (Time.realtimeSinceStartup - startTime) * 1000f;

            Debug.Log($"✅ Calculated 150-seat distribution in {calculationTime:F2}ms");
            Debug.Log($"📊 Processed {votes2023.Count} parties with {votes2023.Values.Sum():N0} total votes");

            // Show top results
            var topResults = results.OrderByDescending(r => r.seats).Take(8).ToList();
            Debug.Log("🏆 Top Election Results:");
            foreach (var result in topResults)
            {
                Debug.Log($"   {result.abbreviation}: {result.seats} seats ({result.votePercentage:F1}% votes → {result.seatPercentage:F1}% seats)");
            }

            // Verify total seats
            int totalSeats = results.Sum(r => r.seats);
            Debug.Log($"🔢 Total Seats Allocated: {totalSeats}/150 {(totalSeats == 150 ? "✅" : "❌")}");

            Debug.Log("");
        }

        private void ValidateAgainst2023Election()
        {
            Debug.Log("🔍 Validation: 2023 Dutch Election Results Reproduction");

            var votes = DHondtElectionSystem.Get2023ElectionVotes();
            var results = DHondtElectionSystem.CalculateSeats(votes);
            var expectedSeats = DHondtElectionSystem.Get2023ExpectedSeats();

            bool isValid = DHondtElectionSystem.ValidateResults(results, expectedSeats);
            Debug.Log($"📋 Algorithm Validation: {(isValid ? "PASSED ✅" : "FAILED ❌")}");

            if (isValid)
            {
                Debug.Log("🎯 Perfect Match: All parties received exact 2023 seat counts");

                // Show validation details for key parties
                var keyParties = new string[] { "PVV", "GL-PvdA", "VVD", "NSC", "D66" };
                foreach (var partyCode in keyParties)
                {
                    var result = results.FirstOrDefault(r => r.abbreviation == partyCode);
                    var expected = expectedSeats.ContainsKey(partyCode) ? expectedSeats[partyCode] : 0;
                    if (result != null)
                    {
                        Debug.Log($"   {partyCode}: {result.seats} seats (expected: {expected}) ✅");
                    }
                }
            }
            else
            {
                Debug.LogError("❌ Validation failed - algorithm does not reproduce exact 2023 results");
            }

            Debug.Log("");
        }

        private void DemonstrateCoalitionAnalysis()
        {
            Debug.Log("🤝 Coalition Formation Analysis");

            var gameObject = new GameObject("TempElectionManager");
            var manager = gameObject.AddComponent<DutchElectionManager>();

            // Run election and analyze coalitions
            var results = manager.RunAuthentic2023Election();
            var coalitions = manager.GetCoalitionPossibilities();

            Debug.Log($"🔍 Found {coalitions.Count} viable coalition possibilities");

            // Show top coalition possibilities
            var topCoalitions = coalitions.Take(3).ToList();
            for (int i = 0; i < topCoalitions.Count; i++)
            {
                var coalition = topCoalitions[i];
                Debug.Log($"   #{i + 1}: {string.Join("-", coalition.Parties)} ({coalition.TotalSeats} seats, {coalition.CompatibilityScore:F2} compatibility)");
            }

            // Show actual 2023 coalition
            var actual2023 = coalitions.FirstOrDefault(c =>
                c.Parties.Contains("PVV") && c.Parties.Contains("VVD") &&
                c.Parties.Contains("NSC") && c.Parties.Contains("BBB"));

            if (actual2023 != null)
            {
                Debug.Log($"🏛️ Actual 2023 Coalition: {string.Join("-", actual2023.Parties)} (Rank: {coalitions.IndexOf(actual2023) + 1}, Compatibility: {actual2023.CompatibilityScore:F2})");
            }

            DestroyImmediate(gameObject);
            Debug.Log("");
        }

        private void ShowPerformanceSummary()
        {
            Debug.Log("⚡ Performance Summary");

            // Run performance benchmark
            double averageTime = DHondtElectionSystem.BenchmarkPerformance(25);
            bool meetsTarget = averageTime < 1000.0;

            Debug.Log($"🎯 Performance Target (<1000ms): {(meetsTarget ? "ACHIEVED ✅" : "MISSED ❌")}");
            Debug.Log($"📊 Average Calculation Time: {averageTime:F2}ms over 25 iterations");
            Debug.Log($"🚀 Performance Margin: {((1000.0 - averageTime) / 1000.0 * 100):F0}% faster than target");

            // Memory efficiency test
            long initialMemory = System.GC.GetTotalMemory(true);

            for (int i = 0; i < 10; i++)
            {
                var votes = DHondtElectionSystem.Get2023ElectionVotes();
                var results = DHondtElectionSystem.CalculateSeats(votes);
            }

            System.GC.Collect();
            long finalMemory = System.GC.GetTotalMemory(false);
            long memoryIncrease = finalMemory - initialMemory;

            Debug.Log($"💾 Memory Efficiency: {memoryIncrease / 1024}KB increase over 10 calculations");

            Debug.Log("");
        }
    }
}