using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using Coalition.Political.Parties;
using Coalition.Data;
using UnityEngine;

namespace Coalition.Tests.EditMode
{
    /// <summary>
    /// Comprehensive tests for Dutch political party data generation
    /// Validates authentic 2023 data accuracy and proper ScriptableObject configuration
    /// </summary>
    [TestFixture]
    public class DutchPoliticalDataTests
    {
        private List<PoliticalParty> dutchParties;

        [SetUp]
        public void Setup()
        {
            dutchParties = DutchPoliticalDataGenerator.GenerateAllDutchParties();
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up ScriptableObjects
            foreach (var party in dutchParties)
            {
                if (party != null)
                {
                    Object.DestroyImmediate(party);
                }
            }
        }

        [Test]
        [Category("Data Completeness")]
        public void DutchParties_GenerateAll_Creates15Parties()
        {
            // Assert
            Assert.AreEqual(15, dutchParties.Count, "Should generate exactly 15 major Dutch parties");

            var expectedParties = new string[]
            {
                "PVV", "GL-PvdA", "VVD", "NSC", "D66", "BBB", "CDA", "SP",
                "FvD", "PvdD", "CU", "Volt", "JA21", "SGP", "DENK"
            };

            foreach (var expected in expectedParties)
            {
                var party = dutchParties.FirstOrDefault(p => p.Abbreviation == expected);
                Assert.IsNotNull(party, $"Should include party {expected}");
            }

            Debug.Log("✅ All 15 major Dutch parties generated correctly");
        }

        [Test]
        [Category("Data Validation")]
        public void DutchParties_ValidateGeneratedData_PassesValidation()
        {
            // Act
            bool isValid = DutchPoliticalDataGenerator.ValidateGeneratedData();

            // Assert
            Assert.IsTrue(isValid, "Generated data should pass validation");
            Debug.Log("✅ Dutch political data validation passed");
        }

        [Test]
        [Category("2023 Election Data")]
        public void DutchParties_2023PopularityTotals_Sum100Percent()
        {
            // Arrange
            float totalPopularity = dutchParties.Sum(p => p.CurrentPopularity);

            // Assert
            Assert.That(totalPopularity, Is.EqualTo(100.0f).Within(0.1f),
                $"Total popularity should sum to 100%, got {totalPopularity:F2}%");

            Debug.Log($"✅ Total popularity sums correctly: {totalPopularity:F2}%");
        }

        [Test]
        [Category("2023 Election Data")]
        public void DutchParties_LargestParties_HaveCorrectPopularity()
        {
            // Arrange & Assert
            var pvv = GetParty("PVV");
            var glpvda = GetParty("GL-PvdA");
            var vvd = GetParty("VVD");
            var nsc = GetParty("NSC");

            Assert.That(pvv.CurrentPopularity, Is.EqualTo(23.5f).Within(0.1f), "PVV should have ~23.5% popularity");
            Assert.That(glpvda.CurrentPopularity, Is.EqualTo(15.7f).Within(0.5f), "GL-PvdA should have ~15.7% popularity");
            Assert.That(vvd.CurrentPopularity, Is.EqualTo(16.0f).Within(0.5f), "VVD should have ~16.0% popularity");
            Assert.That(nsc.CurrentPopularity, Is.EqualTo(13.3f).Within(0.5f), "NSC should have ~13.3% popularity");

            Debug.Log("✅ Largest parties have correct popularity percentages");
        }

        [Test]
        [Category("Ideological Positioning")]
        public void DutchParties_IdeologicalPositions_AreAuthentic()
        {
            // Test PVV (far-right populist)
            var pvv = GetParty("PVV");
            Assert.Greater(pvv.EconomicPosition, 0, "PVV should be economically right-wing");
            Assert.Less(pvv.SocialPosition, -5, "PVV should be very socially conservative");
            Assert.Less(pvv.ImmigrationPosition, -7, "PVV should be very restrictive on immigration");

            // Test GL-PvdA (progressive left)
            var glpvda = GetParty("GL-PvdA");
            Assert.Less(glpvda.EconomicPosition, -5, "GL-PvdA should be economically left-wing");
            Assert.Greater(glpvda.SocialPosition, 5, "GL-PvdA should be very socially progressive");
            Assert.Greater(glpvda.EuropeanPosition, 5, "GL-PvdA should be very pro-EU");

            // Test VVD (liberal center-right)
            var vvd = GetParty("VVD");
            Assert.Greater(vvd.EconomicPosition, 3, "VVD should be economically right-wing");
            Assert.Greater(vvd.SocialPosition, 0, "VVD should be moderately progressive socially");
            Assert.Greater(vvd.EuropeanPosition, 3, "VVD should be pro-EU");

            Debug.Log("✅ Ideological positions are authentic to party characteristics");
        }

        [Test]
        [Category("Coalition Preferences")]
        public void DutchParties_CoalitionPreferences_ReflectRealPolitics()
        {
            // Test 2023 coalition partners
            var pvv = GetParty("PVV");
            var vvd = GetParty("VVD");
            var nsc = GetParty("NSC");
            var bbb = GetParty("BBB");

            // These formed the 2023 coalition
            Assert.Contains("VVD", pvv.PreferredCoalitionPartners.ToArray(), "PVV should prefer VVD as partner");
            Assert.Contains("NSC", pvv.PreferredCoalitionPartners.ToArray(), "PVV should prefer NSC as partner");
            Assert.Contains("BBB", pvv.PreferredCoalitionPartners.ToArray(), "PVV should prefer BBB as partner");

            // Test exclusions
            var glpvda = GetParty("GL-PvdA");
            Assert.Contains("GL-PvdA", pvv.ExcludedCoalitionPartners.ToArray(), "PVV should exclude GL-PvdA");
            Assert.Contains("PVV", glpvda.ExcludedCoalitionPartners.ToArray(), "GL-PvdA should exclude PVV");

            Debug.Log("✅ Coalition preferences reflect authentic political relationships");
        }

        [Test]
        [Category("Party Characteristics")]
        public void DutchParties_MediaPresence_ReflectsRealInfluence()
        {
            var pvv = GetParty("PVV");
            var vvd = GetParty("VVD");
            var sgp = GetParty("SGP");

            // PVV should have very high media presence due to controversial positions
            Assert.Greater(pvv.MediaPresence, 80, "PVV should have very high media presence");

            // VVD as establishment party should have high media presence
            Assert.Greater(vvd.MediaPresence, 70, "VVD should have high media presence");

            // SGP as small religious party should have lower media presence
            Assert.Less(sgp.MediaPresence, 50, "SGP should have lower media presence");

            Debug.Log("✅ Media presence values reflect real political influence");
        }

        [Test]
        [Category("Campaign Resources")]
        public void DutchParties_CampaignResources_AreRealistic()
        {
            // Test that larger parties have more resources
            var vvd = GetParty("VVD");
            var glpvda = GetParty("GL-PvdA");
            var ja21 = GetParty("JA21");

            Assert.Greater(vvd.CampaignBudget, glpvda.CampaignBudget * 0.8f,
                "VVD should have substantial campaign budget");
            Assert.Greater(glpvda.CampaignBudget, ja21.CampaignBudget * 3,
                "GL-PvdA should have much more budget than JA21");

            // Test social media followers are realistic
            Assert.Greater(vvd.SocialMediaFollowers, 100000, "VVD should have substantial social media following");
            Assert.Less(ja21.SocialMediaFollowers, 200000, "JA21 should have limited social media following");

            Debug.Log("✅ Campaign resources are realistic for party sizes");
        }

        [Test]
        [Category("Party Flexibility")]
        public void DutchParties_CoalitionFlexibility_ReflectsNegotiationHistory()
        {
            var vvd = GetParty("VVD");
            var cda = GetParty("CDA");
            var cu = GetParty("CU");
            var pvv = GetParty("PVV");
            var fvd = GetParty("FvD");

            // Traditional coalition parties should have high flexibility
            Assert.Greater(vvd.CoalitionFlexibility, 70, "VVD should have high coalition flexibility");
            Assert.Greater(cda.CoalitionFlexibility, 80, "CDA should have very high coalition flexibility");
            Assert.Greater(cu.CoalitionFlexibility, 70, "CU should have high coalition flexibility");

            // Populist parties should have lower flexibility
            Assert.Less(pvv.CoalitionFlexibility, 50, "PVV should have limited coalition flexibility");
            Assert.Less(fvd.CoalitionFlexibility, 40, "FvD should have very limited coalition flexibility");

            Debug.Log("✅ Coalition flexibility reflects parties' negotiation history");
        }

        [Test]
        [Category("Data Integrity")]
        public void DutchParties_AllFieldsPopulated_NoMissingData()
        {
            foreach (var party in dutchParties)
            {
                // Test required string fields
                Assert.IsNotEmpty(party.PartyName, $"Party name should not be empty for {party.name}");
                Assert.IsNotEmpty(party.Abbreviation, $"Abbreviation should not be empty for {party.name}");
                Assert.IsNotEmpty(party.Leader, $"Leader should not be empty for {party.name}");

                // Test ideological positions are within valid range
                Assert.That(party.EconomicPosition, Is.InRange(-10f, 10f),
                    $"Economic position should be in range [-10, 10] for {party.Abbreviation}");
                Assert.That(party.SocialPosition, Is.InRange(-10f, 10f),
                    $"Social position should be in range [-10, 10] for {party.Abbreviation}");
                Assert.That(party.EuropeanPosition, Is.InRange(-10f, 10f),
                    $"European position should be in range [-10, 10] for {party.Abbreviation}");
                Assert.That(party.ImmigrationPosition, Is.InRange(-10f, 10f),
                    $"Immigration position should be in range [-10, 10] for {party.Abbreviation}");

                // Test characteristics are within valid range
                Assert.That(party.CurrentPopularity, Is.InRange(0f, 100f),
                    $"Popularity should be in range [0, 100] for {party.Abbreviation}");
                Assert.That(party.CampaignExpertise, Is.InRange(0f, 100f),
                    $"Campaign expertise should be in range [0, 100] for {party.Abbreviation}");
                Assert.That(party.MediaPresence, Is.InRange(0f, 100f),
                    $"Media presence should be in range [0, 100] for {party.Abbreviation}");
                Assert.That(party.CoalitionFlexibility, Is.InRange(0f, 100f),
                    $"Coalition flexibility should be in range [0, 100] for {party.Abbreviation}");

                // Test resources are positive
                Assert.Greater(party.CampaignBudget, 0, $"Campaign budget should be positive for {party.Abbreviation}");
                Assert.Greater(party.SocialMediaFollowers, 0, $"Social media followers should be positive for {party.Abbreviation}");
                Assert.Greater(party.ActiveMembership, 0, $"Active membership should be positive for {party.Abbreviation}");
            }

            Debug.Log("✅ All party data fields are properly populated and within valid ranges");
        }

        [Test]
        [Category("ScriptableObject")]
        public void DutchParties_ScriptableObjects_AreProperlyConfigured()
        {
            foreach (var party in dutchParties)
            {
                Assert.IsNotNull(party, "Party ScriptableObject should not be null");
                Assert.IsInstanceOf<PoliticalParty>(party, "Should be instance of PoliticalParty");
                Assert.IsNotEmpty(party.name, "ScriptableObject name should not be empty");

                // Test that compatibility calculation works
                var otherParty = dutchParties.FirstOrDefault(p => p != party);
                if (otherParty != null)
                {
                    float compatibility = party.CalculateCoalitionCompatibility(otherParty);
                    Assert.That(compatibility, Is.InRange(0f, 1f),
                        $"Coalition compatibility should be in range [0, 1] for {party.Abbreviation}");
                }
            }

            Debug.Log("✅ All ScriptableObjects are properly configured and functional");
        }

        #region Helper Methods

        private PoliticalParty GetParty(string abbreviation)
        {
            var party = dutchParties.FirstOrDefault(p => p.Abbreviation == abbreviation);
            Assert.IsNotNull(party, $"Should find party with abbreviation {abbreviation}");
            return party;
        }

        #endregion
    }
}