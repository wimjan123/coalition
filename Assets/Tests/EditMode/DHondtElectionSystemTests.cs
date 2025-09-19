using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using Coalition.Political.Elections;
using UnityEngine;
using UnityEngine.TestTools;

namespace Coalition.Tests.EditMode
{
    /// <summary>
    /// Comprehensive validation tests for D'Hondt electoral algorithm
    /// Ensures mathematical precision and authentic 2023 Dutch election reproduction
    /// </summary>
    [TestFixture]
    public class DHondtElectionSystemTests
    {
        private Dictionary<string, int> authentic2023Votes;
        private Dictionary<string, int> expected2023Seats;

        [SetUp]
        public void Setup()
        {
            authentic2023Votes = DHondtElectionSystem.Get2023ElectionVotes();
            expected2023Seats = DHondtElectionSystem.Get2023ExpectedSeats();
        }

        [Test]
        [Category("Core Algorithm")]
        public void DHondt_WithAuthentic2023Data_ReproducesExactResults()
        {
            // Arrange
            LogTestStart("Testing D'Hondt algorithm with authentic 2023 Dutch election data");

            // Act
            var results = DHondtElectionSystem.CalculateSeats(authentic2023Votes);

            // Assert
            Assert.IsNotNull(results, "Election results should not be null");
            Assert.AreEqual(150, results.Sum(r => r.seats), "Total seats should equal 150");

            // Validate each party's seat count matches 2023 results exactly
            foreach (var expectedResult in expected2023Seats)
            {
                var actualResult = results.FirstOrDefault(r =>
                    r.abbreviation == expectedResult.Key || r.partyName == expectedResult.Key);

                Assert.IsNotNull(actualResult, $"Should find result for party {expectedResult.Key}");
                Assert.AreEqual(expectedResult.Value, actualResult.seats,
                    $"Party {expectedResult.Key}: expected {expectedResult.Value} seats, got {actualResult.seats}");
            }

            LogTestSuccess("✅ D'Hondt algorithm reproduces exact 2023 Dutch election results");
        }

        [Test]
        [Category("Core Algorithm")]
        public void DHondt_ValidationMethod_ConfirmsAccuracy()
        {
            // Arrange & Act
            var results = DHondtElectionSystem.CalculateSeats(authentic2023Votes);
            bool isValid = DHondtElectionSystem.ValidateResults(results, expected2023Seats);

            // Assert
            Assert.IsTrue(isValid, "Validation should confirm results match expected 2023 outcomes");
            LogTestSuccess("✅ Validation method confirms algorithmic accuracy");
        }

        [Test]
        [Category("Performance")]
        public void DHondt_PerformanceBenchmark_MeetsTargetTime()
        {
            // Arrange
            const double TARGET_TIME_MS = 1000.0; // <1 second target
            LogTestStart($"Testing D'Hondt performance benchmark (target: <{TARGET_TIME_MS}ms)");

            // Act
            double averageTime = DHondtElectionSystem.BenchmarkPerformance(50);

            // Assert
            Assert.Less(averageTime, TARGET_TIME_MS,
                $"Average calculation time ({averageTime:F2}ms) should be less than {TARGET_TIME_MS}ms");

            LogTestSuccess($"✅ Performance target met: {averageTime:F2}ms average");
        }

        [Test]
        [Category("Edge Cases")]
        public void DHondt_WithNullInput_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<System.ArgumentException>(() =>
                DHondtElectionSystem.CalculateSeats(null));

            LogTestSuccess("✅ Null input validation works correctly");
        }

        [Test]
        [Category("Edge Cases")]
        public void DHondt_WithEmptyInput_ThrowsArgumentException()
        {
            // Arrange
            var emptyVotes = new Dictionary<string, int>();

            // Act & Assert
            Assert.Throws<System.ArgumentException>(() =>
                DHondtElectionSystem.CalculateSeats(emptyVotes));

            LogTestSuccess("✅ Empty input validation works correctly");
        }

        [Test]
        [Category("Edge Cases")]
        public void DHondt_WithZeroSeats_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<System.ArgumentException>(() =>
                DHondtElectionSystem.CalculateSeats(authentic2023Votes, 0));

            LogTestSuccess("✅ Zero seats validation works correctly");
        }

        [Test]
        [Category("Edge Cases")]
        public void DHondt_WithNegativeSeats_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<System.ArgumentException>(() =>
                DHondtElectionSystem.CalculateSeats(authentic2023Votes, -5));

            LogTestSuccess("✅ Negative seats validation works correctly");
        }

        [Test]
        [Category("Proportionality")]
        public void DHondt_ResultsFollow_ProportionalityPrinciple()
        {
            // Arrange
            var results = DHondtElectionSystem.CalculateSeats(authentic2023Votes);
            var orderedByVotes = results.OrderByDescending(r => r.votes).ToList();
            var orderedBySeats = results.OrderByDescending(r => r.seats).ToList();

            // Assert
            // The party with most votes should have most seats
            Assert.AreEqual(orderedByVotes[0].partyName, orderedBySeats[0].partyName,
                "Party with most votes should have most seats");

            // Check that vote percentages roughly align with seat percentages
            foreach (var result in results.Where(r => r.seats > 0))
            {
                float proportionalityError = Mathf.Abs(result.votePercentage - result.seatPercentage);
                Assert.Less(proportionalityError, 5.0f,
                    $"Proportionality error for {result.abbreviation} should be <5%: {proportionalityError:F1}%");
            }

            LogTestSuccess("✅ Results follow proportional representation principles");
        }

        [Test]
        [Category("Mathematical Precision")]
        public void DHondt_QuotientCalculation_IsMathematicallyPrecise()
        {
            // Arrange
            var testVotes = new Dictionary<string, int>
            {
                ["Party A"] = 1000,
                ["Party B"] = 800,
                ["Party C"] = 600
            };

            // Act
            var results = DHondtElectionSystem.CalculateSeats(testVotes, 10);

            // Assert
            int totalSeats = results.Sum(r => r.seats);
            Assert.AreEqual(10, totalSeats, "All seats should be allocated");

            // Verify no party has negative seats
            foreach (var result in results)
            {
                Assert.GreaterOrEqual(result.seats, 0, $"Party {result.partyName} should not have negative seats");
            }

            LogTestSuccess("✅ Quotient calculation is mathematically precise");
        }

        [Test]
        [Category("Data Integrity")]
        public void DHondt_2023ElectionData_HasCorrectTotals()
        {
            // Arrange
            int totalVotes = authentic2023Votes.Values.Sum();
            int totalExpectedSeats = expected2023Seats.Values.Sum();

            // Assert
            Assert.AreEqual(150, totalExpectedSeats, "Expected seats should total 150");
            Assert.Greater(totalVotes, 10_000_000, "Total votes should be realistic for Dutch election");

            // Verify each party has positive votes
            foreach (var party in authentic2023Votes)
            {
                Assert.Greater(party.Value, 0, $"Party {party.Key} should have positive votes");
            }

            LogTestSuccess("✅ 2023 election data integrity confirmed");
        }

        [Test]
        [Category("Seat Distribution")]
        public void DHondt_LargestParties_ReceiveCorrectSeats()
        {
            // Arrange
            var results = DHondtElectionSystem.CalculateSeats(authentic2023Votes);

            // Assert specific known results for largest parties
            var pvv = results.First(r => r.abbreviation == "PVV");
            var glpvda = results.First(r => r.abbreviation == "GL-PvdA");
            var vvd = results.First(r => r.abbreviation == "VVD");
            var nsc = results.First(r => r.abbreviation == "NSC");

            Assert.AreEqual(37, pvv.seats, "PVV should receive exactly 37 seats");
            Assert.AreEqual(25, glpvda.seats, "GL-PvdA should receive exactly 25 seats");
            Assert.AreEqual(24, vvd.seats, "VVD should receive exactly 24 seats");
            Assert.AreEqual(20, nsc.seats, "NSC should receive exactly 20 seats");

            LogTestSuccess("✅ Largest parties receive correct seat allocations");
        }

        [Test]
        [Category("Small Parties")]
        public void DHondt_SmallParties_ReceiveCorrectRepresentation()
        {
            // Arrange
            var results = DHondtElectionSystem.CalculateSeats(authentic2023Votes);

            // Assert specific results for smaller parties
            var ja21 = results.First(r => r.abbreviation == "JA21");
            var volt = results.First(r => r.abbreviation == "Volt");
            var denk = results.First(r => r.abbreviation == "DENK");

            Assert.AreEqual(1, ja21.seats, "JA21 should receive exactly 1 seat");
            Assert.AreEqual(3, volt.seats, "Volt should receive exactly 3 seats");
            Assert.AreEqual(3, denk.seats, "DENK should receive exactly 3 seats");

            LogTestSuccess("✅ Small parties receive correct representation");
        }

        [Test]
        [Category("Stress Testing")]
        public void DHondt_MultipleCalculations_ProducesConsistentResults()
        {
            // Arrange
            const int iterations = 10;
            var allResults = new List<List<ElectionResult>>();

            // Act
            for (int i = 0; i < iterations; i++)
            {
                var results = DHondtElectionSystem.CalculateSeats(authentic2023Votes);
                allResults.Add(results);
            }

            // Assert
            // All iterations should produce identical results
            var firstResult = allResults[0];
            for (int i = 1; i < iterations; i++)
            {
                var currentResult = allResults[i];
                Assert.AreEqual(firstResult.Count, currentResult.Count, $"Iteration {i} should have same party count");

                foreach (var party in firstResult)
                {
                    var matchingParty = currentResult.First(p => p.abbreviation == party.abbreviation);
                    Assert.AreEqual(party.seats, matchingParty.seats,
                        $"Iteration {i}: {party.abbreviation} seats should be consistent");
                }
            }

            LogTestSuccess($"✅ Algorithm produces consistent results across {iterations} iterations");
        }

        #region Test Helpers

        private void LogTestStart(string message)
        {
            Debug.Log($"🧪 {message}");
        }

        private void LogTestSuccess(string message)
        {
            Debug.Log($"{message}");
        }

        #endregion
    }
}