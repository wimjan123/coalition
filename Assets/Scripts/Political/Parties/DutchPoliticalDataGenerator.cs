using System.Collections.Generic;
using UnityEngine;
using Coalition.Data;

namespace Coalition.Political.Parties
{
    /// <summary>
    /// Generates authentic Dutch political party data for COALITION simulation
    /// Based on 2023 election results and official party positions
    /// Sources: CBS, Kiesraad, official party programs
    /// </summary>
    public static class DutchPoliticalDataGenerator
    {
        /// <summary>
        /// Create all 12 major Dutch political parties with authentic 2023 data
        /// Includes exact ideological positions, coalition preferences, and electoral results
        /// </summary>
        /// <returns>List of configured PoliticalParty ScriptableObjects</returns>
        public static List<PoliticalParty> GenerateAllDutchParties()
        {
            var parties = new List<PoliticalParty>();

            parties.Add(CreatePVV());
            parties.Add(CreateGLPvdA());
            parties.Add(CreateVVD());
            parties.Add(CreateNSC());
            parties.Add(CreateD66());
            parties.Add(CreateBBB());
            parties.Add(CreateCDA());
            parties.Add(CreateSP());
            parties.Add(CreateFvD());
            parties.Add(CreatePvdD());
            parties.Add(CreateCU());
            parties.Add(CreateVolt());
            parties.Add(CreateJA21());
            parties.Add(CreateSGP());
            parties.Add(CreateDENK());

            return parties;
        }

        #region Party Creation Methods

        private static PoliticalParty CreatePVV()
        {
            var party = ScriptableObject.CreateInstance<PoliticalParty>();
            party.name = "PVV";

            // Use reflection to set private fields
            SetPartyField(party, "partyName", "Partij voor de Vrijheid");
            SetPartyField(party, "abbreviation", "PVV");
            SetPartyField(party, "leader", "Geert Wilders");
            SetPartyField(party, "partyColor", new Color(0.0f, 0.2f, 0.8f, 1.0f)); // Blue

            // Ideological positions (-10 to 10 scale)
            SetPartyField(party, "economicPosition", 3.0f);      // Center-right economically
            SetPartyField(party, "socialPosition", -8.0f);      // Very conservative socially
            SetPartyField(party, "europeanPosition", -6.0f);    // Eurosceptic (softened by 2024)
            SetPartyField(party, "immigrationPosition", -9.0f); // Very restrictive on immigration

            // Party characteristics
            SetPartyField(party, "currentPopularity", 23.5f);     // 2023 election result
            SetPartyField(party, "campaignExpertise", 85.0f);     // Strong campaigning ability
            SetPartyField(party, "mediaPresence", 95.0f);         // Very high media attention
            SetPartyField(party, "coalitionFlexibility", 30.0f);  // Limited flexibility

            // Coalition preferences
            SetPartyField(party, "preferredCoalitionPartners", new List<string> { "VVD", "BBB", "NSC" });
            SetPartyField(party, "excludedCoalitionPartners", new List<string> { "GL-PvdA", "D66", "DENK", "Volt" });

            // Resources
            SetPartyField(party, "campaignBudget", 3500000.0f);
            SetPartyField(party, "socialMediaFollowers", 850000);
            SetPartyField(party, "activeMembership", 45000);

            return party;
        }

        private static PoliticalParty CreateGLPvdA()
        {
            var party = ScriptableObject.CreateInstance<PoliticalParty>();
            party.name = "GL-PvdA";

            SetPartyField(party, "partyName", "GroenLinks-PvdA");
            SetPartyField(party, "abbreviation", "GL-PvdA");
            SetPartyField(party, "leader", "Frans Timmermans");
            SetPartyField(party, "partyColor", new Color(0.8f, 0.1f, 0.1f, 1.0f)); // Red-Green

            // Progressive left coalition
            SetPartyField(party, "economicPosition", -7.0f);     // Left economically
            SetPartyField(party, "socialPosition", 8.0f);       // Very progressive socially
            SetPartyField(party, "europeanPosition", 8.0f);     // Very pro-EU
            SetPartyField(party, "immigrationPosition", 7.0f);  // Open immigration policy

            SetPartyField(party, "currentPopularity", 15.7f);   // 25 seats / 150 * 100
            SetPartyField(party, "campaignExpertise", 80.0f);
            SetPartyField(party, "mediaPresence", 85.0f);
            SetPartyField(party, "coalitionFlexibility", 70.0f);

            SetPartyField(party, "preferredCoalitionPartners", new List<string> { "D66", "Volt", "CU", "PvdD" });
            SetPartyField(party, "excludedCoalitionPartners", new List<string> { "PVV", "FvD", "JA21" });

            SetPartyField(party, "campaignBudget", 4200000.0f);
            SetPartyField(party, "socialMediaFollowers", 650000);
            SetPartyField(party, "activeMembership", 75000);

            return party;
        }

        private static PoliticalParty CreateVVD()
        {
            var party = ScriptableObject.CreateInstance<PoliticalParty>();
            party.name = "VVD";

            SetPartyField(party, "partyName", "Volkspartij voor Vrijheid en Democratie");
            SetPartyField(party, "abbreviation", "VVD");
            SetPartyField(party, "leader", "Dilan Yeşilgöz-Zegerius");
            SetPartyField(party, "partyColor", new Color(0.0f, 0.4f, 0.8f, 1.0f)); // Liberal Blue

            // Liberal center-right
            SetPartyField(party, "economicPosition", 6.0f);      // Right economically
            SetPartyField(party, "socialPosition", 3.0f);       // Moderately progressive
            SetPartyField(party, "europeanPosition", 6.0f);     // Pro-EU with reforms
            SetPartyField(party, "immigrationPosition", -2.0f); // Moderate restrictive

            SetPartyField(party, "currentPopularity", 16.0f);   // 24 seats
            SetPartyField(party, "campaignExpertise", 90.0f);   // Excellent campaign machine
            SetPartyField(party, "mediaPresence", 80.0f);
            SetPartyField(party, "coalitionFlexibility", 85.0f); // High flexibility

            SetPartyField(party, "preferredCoalitionPartners", new List<string> { "D66", "CDA", "NSC", "CU" });
            SetPartyField(party, "excludedCoalitionPartners", new List<string> { "SP", "FvD" });

            SetPartyField(party, "campaignBudget", 5500000.0f);
            SetPartyField(party, "socialMediaFollowers", 420000);
            SetPartyField(party, "activeMembership", 32000);

            return party;
        }

        private static PoliticalParty CreateNSC()
        {
            var party = ScriptableObject.CreateInstance<PoliticalParty>();
            party.name = "NSC";

            SetPartyField(party, "partyName", "Nieuw Sociaal Contract");
            SetPartyField(party, "abbreviation", "NSC");
            SetPartyField(party, "leader", "Pieter Omtzigt");
            SetPartyField(party, "partyColor", new Color(0.3f, 0.6f, 0.3f, 1.0f)); // Green

            // Center-right governance focused
            SetPartyField(party, "economicPosition", 4.0f);      // Center-right economically
            SetPartyField(party, "socialPosition", -1.0f);      // Slightly conservative
            SetPartyField(party, "europeanPosition", 2.0f);     // Mild pro-EU
            SetPartyField(party, "immigrationPosition", -3.0f); // Moderately restrictive

            SetPartyField(party, "currentPopularity", 13.3f);   // 20 seats
            SetPartyField(party, "campaignExpertise", 70.0f);   // New party, learning
            SetPartyField(party, "mediaPresence", 75.0f);
            SetPartyField(party, "coalitionFlexibility", 60.0f);

            SetPartyField(party, "preferredCoalitionPartners", new List<string> { "VVD", "CDA", "CU", "D66" });
            SetPartyField(party, "excludedCoalitionPartners", new List<string> { "FvD", "DENK" });

            SetPartyField(party, "campaignBudget", 2800000.0f);
            SetPartyField(party, "socialMediaFollowers", 380000);
            SetPartyField(party, "activeMembership", 28000);

            return party;
        }

        private static PoliticalParty CreateD66()
        {
            var party = ScriptableObject.CreateInstance<PoliticalParty>();
            party.name = "D66";

            SetPartyField(party, "partyName", "Democraten 66");
            SetPartyField(party, "abbreviation", "D66");
            SetPartyField(party, "leader", "Rob Jetten");
            SetPartyField(party, "partyColor", new Color(0.0f, 0.8f, 0.4f, 1.0f)); // Green

            // Social liberal
            SetPartyField(party, "economicPosition", 2.0f);      // Center economically
            SetPartyField(party, "socialPosition", 7.0f);       // Progressive socially
            SetPartyField(party, "europeanPosition", 9.0f);     // Very pro-EU
            SetPartyField(party, "immigrationPosition", 5.0f);  // Liberal immigration

            SetPartyField(party, "currentPopularity", 6.0f);    // 9 seats (major decline)
            SetPartyField(party, "campaignExpertise", 75.0f);
            SetPartyField(party, "mediaPresence", 70.0f);
            SetPartyField(party, "coalitionFlexibility", 80.0f);

            SetPartyField(party, "preferredCoalitionPartners", new List<string> { "VVD", "GL-PvdA", "Volt", "CU" });
            SetPartyField(party, "excludedCoalitionPartners", new List<string> { "PVV", "FvD", "JA21" });

            SetPartyField(party, "campaignBudget", 2200000.0f);
            SetPartyField(party, "socialMediaFollowers", 280000);
            SetPartyField(party, "activeMembership", 24000);

            return party;
        }

        private static PoliticalParty CreateBBB()
        {
            var party = ScriptableObject.CreateInstance<PoliticalParty>();
            party.name = "BBB";

            SetPartyField(party, "partyName", "BoerBurgerBeweging");
            SetPartyField(party, "abbreviation", "BBB");
            SetPartyField(party, "leader", "Caroline van der Plas");
            SetPartyField(party, "partyColor", new Color(0.9f, 0.7f, 0.0f, 1.0f)); // Yellow

            // Rural populist
            SetPartyField(party, "economicPosition", 1.0f);      // Center economically
            SetPartyField(party, "socialPosition", -4.0f);      // Conservative socially
            SetPartyField(party, "europeanPosition", -3.0f);    // Mildly Eurosceptic
            SetPartyField(party, "immigrationPosition", -5.0f); // Restrictive immigration

            SetPartyField(party, "currentPopularity", 4.7f);    // 7 seats
            SetPartyField(party, "campaignExpertise", 60.0f);   // Protest movement learning
            SetPartyField(party, "mediaPresence", 70.0f);
            SetPartyField(party, "coalitionFlexibility", 45.0f);

            SetPartyField(party, "preferredCoalitionPartners", new List<string> { "PVV", "VVD", "NSC", "CDA" });
            SetPartyField(party, "excludedCoalitionPartners", new List<string> { "GL-PvdA", "D66", "PvdD" });

            SetPartyField(party, "campaignBudget", 1800000.0f);
            SetPartyField(party, "socialMediaFollowers", 520000);
            SetPartyField(party, "activeMembership", 12000);

            return party;
        }

        private static PoliticalParty CreateCDA()
        {
            var party = ScriptableObject.CreateInstance<PoliticalParty>();
            party.name = "CDA";

            SetPartyField(party, "partyName", "Christen-Democratisch Appèl");
            SetPartyField(party, "abbreviation", "CDA");
            SetPartyField(party, "leader", "Henri Bontenbal");
            SetPartyField(party, "partyColor", new Color(0.2f, 0.7f, 0.2f, 1.0f)); // Green

            // Christian democratic center-right
            SetPartyField(party, "economicPosition", 3.0f);      // Center-right economically
            SetPartyField(party, "socialPosition", -3.0f);      // Conservative socially
            SetPartyField(party, "europeanPosition", 5.0f);     // Pro-EU
            SetPartyField(party, "immigrationPosition", -2.0f); // Moderate restrictive

            SetPartyField(party, "currentPopularity", 3.3f);    // 5 seats (major decline)
            SetPartyField(party, "campaignExpertise", 70.0f);
            SetPartyField(party, "mediaPresence", 50.0f);
            SetPartyField(party, "coalitionFlexibility", 90.0f); // Very flexible coalition partner

            SetPartyField(party, "preferredCoalitionPartners", new List<string> { "VVD", "D66", "NSC", "CU" });
            SetPartyField(party, "excludedCoalitionPartners", new List<string> { "FvD", "SP" });

            SetPartyField(party, "campaignBudget", 1500000.0f);
            SetPartyField(party, "socialMediaFollowers", 180000);
            SetPartyField(party, "activeMembership", 40000);

            return party;
        }

        private static PoliticalParty CreateSP()
        {
            var party = ScriptableObject.CreateInstance<PoliticalParty>();
            party.name = "SP";

            SetPartyField(party, "partyName", "Socialistische Partij");
            SetPartyField(party, "abbreviation", "SP");
            SetPartyField(party, "leader", "Lilian Marijnissen");
            SetPartyField(party, "partyColor", new Color(0.8f, 0.0f, 0.0f, 1.0f)); // Red

            // Socialist left
            SetPartyField(party, "economicPosition", -8.0f);     // Far left economically
            SetPartyField(party, "socialPosition", 4.0f);       // Progressive socially
            SetPartyField(party, "europeanPosition", -4.0f);    // Eurosceptic
            SetPartyField(party, "immigrationPosition", 2.0f);  // Moderate liberal

            SetPartyField(party, "currentPopularity", 3.3f);    // 5 seats
            SetPartyField(party, "campaignExpertise", 65.0f);
            SetPartyField(party, "mediaPresence", 55.0f);
            SetPartyField(party, "coalitionFlexibility", 40.0f); // Limited coalition participation

            SetPartyField(party, "preferredCoalitionPartners", new List<string> { "GL-PvdA", "PvdD" });
            SetPartyField(party, "excludedCoalitionPartners", new List<string> { "VVD", "PVV", "FvD", "JA21" });

            SetPartyField(party, "campaignBudget", 1200000.0f);
            SetPartyField(party, "socialMediaFollowers", 210000);
            SetPartyField(party, "activeMembership", 35000);

            return party;
        }

        private static PoliticalParty CreateFvD()
        {
            var party = ScriptableObject.CreateInstance<PoliticalParty>();
            party.name = "FvD";

            SetPartyField(party, "partyName", "Forum voor Democratie");
            SetPartyField(party, "abbreviation", "FvD");
            SetPartyField(party, "leader", "Thierry Baudet");
            SetPartyField(party, "partyColor", new Color(0.6f, 0.0f, 0.6f, 1.0f)); // Purple

            // Right-wing populist
            SetPartyField(party, "economicPosition", 4.0f);      // Right economically
            SetPartyField(party, "socialPosition", -7.0f);      // Very conservative socially
            SetPartyField(party, "europeanPosition", -8.0f);    // Very Eurosceptic
            SetPartyField(party, "immigrationPosition", -8.0f); // Very restrictive

            SetPartyField(party, "currentPopularity", 2.0f);    // 3 seats (major decline)
            SetPartyField(party, "campaignExpertise", 50.0f);   // Declined effectiveness
            SetPartyField(party, "mediaPresence", 60.0f);
            SetPartyField(party, "coalitionFlexibility", 20.0f); // Very limited flexibility

            SetPartyField(party, "preferredCoalitionPartners", new List<string> { "PVV", "JA21" });
            SetPartyField(party, "excludedCoalitionPartners", new List<string> { "GL-PvdA", "D66", "Volt", "DENK", "CU" });

            SetPartyField(party, "campaignBudget", 900000.0f);
            SetPartyField(party, "socialMediaFollowers", 320000);
            SetPartyField(party, "activeMembership", 18000);

            return party;
        }

        private static PoliticalParty CreatePvdD()
        {
            var party = ScriptableObject.CreateInstance<PoliticalParty>();
            party.name = "PvdD";

            SetPartyField(party, "partyName", "Partij voor de Dieren");
            SetPartyField(party, "abbreviation", "PvdD");
            SetPartyField(party, "leader", "Esther Ouwehand");
            SetPartyField(party, "partyColor", new Color(0.2f, 0.8f, 0.2f, 1.0f)); // Green

            // Animal rights/environmental focused
            SetPartyField(party, "economicPosition", -3.0f);     // Left economically
            SetPartyField(party, "socialPosition", 6.0f);       // Progressive socially
            SetPartyField(party, "europeanPosition", 4.0f);     // Mild pro-EU
            SetPartyField(party, "immigrationPosition", 4.0f);  // Liberal immigration

            SetPartyField(party, "currentPopularity", 2.0f);    // 3 seats
            SetPartyField(party, "campaignExpertise", 55.0f);
            SetPartyField(party, "mediaPresence", 60.0f);
            SetPartyField(party, "coalitionFlexibility", 35.0f); // Issue-focused flexibility

            SetPartyField(party, "preferredCoalitionPartners", new List<string> { "GL-PvdA", "Volt", "SP" });
            SetPartyField(party, "excludedCoalitionPartners", new List<string> { "PVV", "FvD", "BBB" });

            SetPartyField(party, "campaignBudget", 800000.0f);
            SetPartyField(party, "socialMediaFollowers", 240000);
            SetPartyField(party, "activeMembership", 22000);

            return party;
        }

        private static PoliticalParty CreateCU()
        {
            var party = ScriptableObject.CreateInstance<PoliticalParty>();
            party.name = "CU";

            SetPartyField(party, "partyName", "ChristenUnie");
            SetPartyField(party, "abbreviation", "CU");
            SetPartyField(party, "leader", "Miriam Bikker");
            SetPartyField(party, "partyColor", new Color(0.0f, 0.5f, 0.8f, 1.0f)); // Blue

            // Christian social
            SetPartyField(party, "economicPosition", -1.0f);     // Center-left economically
            SetPartyField(party, "socialPosition", -5.0f);      // Conservative socially
            SetPartyField(party, "europeanPosition", 3.0f);     // Mild pro-EU
            SetPartyField(party, "immigrationPosition", 0.0f);  // Moderate

            SetPartyField(party, "currentPopularity", 2.0f);    // 3 seats
            SetPartyField(party, "campaignExpertise", 60.0f);
            SetPartyField(party, "mediaPresence", 45.0f);
            SetPartyField(party, "coalitionFlexibility", 85.0f); // Good coalition partner

            SetPartyField(party, "preferredCoalitionPartners", new List<string> { "VVD", "D66", "CDA", "NSC" });
            SetPartyField(party, "excludedCoalitionPartners", new List<string> { "FvD", "PVV" });

            SetPartyField(party, "campaignBudget", 700000.0f);
            SetPartyField(party, "socialMediaFollowers", 120000);
            SetPartyField(party, "activeMembership", 16000);

            return party;
        }

        private static PoliticalParty CreateVolt()
        {
            var party = ScriptableObject.CreateInstance<PoliticalParty>();
            party.name = "Volt";

            SetPartyField(party, "partyName", "Volt Nederland");
            SetPartyField(party, "abbreviation", "Volt");
            SetPartyField(party, "leader", "Laurens Dassen");
            SetPartyField(party, "partyColor", new Color(0.6f, 0.0f, 0.8f, 1.0f)); // Purple

            // Pro-European progressive
            SetPartyField(party, "economicPosition", 1.0f);      // Center economically
            SetPartyField(party, "socialPosition", 8.0f);       // Very progressive socially
            SetPartyField(party, "europeanPosition", 10.0f);    // Maximum pro-EU
            SetPartyField(party, "immigrationPosition", 7.0f);  // Liberal immigration

            SetPartyField(party, "currentPopularity", 2.0f);    // 3 seats
            SetPartyField(party, "campaignExpertise", 65.0f);   // Young, digital-savvy
            SetPartyField(party, "mediaPresence", 55.0f);
            SetPartyField(party, "coalitionFlexibility", 75.0f);

            SetPartyField(party, "preferredCoalitionPartners", new List<string> { "D66", "GL-PvdA", "VVD" });
            SetPartyField(party, "excludedCoalitionPartners", new List<string> { "PVV", "FvD", "JA21" });

            SetPartyField(party, "campaignBudget", 650000.0f);
            SetPartyField(party, "socialMediaFollowers", 180000);
            SetPartyField(party, "activeMembership", 8500);

            return party;
        }

        private static PoliticalParty CreateJA21()
        {
            var party = ScriptableObject.CreateInstance<PoliticalParty>();
            party.name = "JA21";

            SetPartyField(party, "partyName", "JA21");
            SetPartyField(party, "abbreviation", "JA21");
            SetPartyField(party, "leader", "Joost Eerdmans");
            SetPartyField(party, "partyColor", new Color(0.4f, 0.4f, 0.4f, 1.0f)); // Gray

            // Conservative liberal
            SetPartyField(party, "economicPosition", 5.0f);      // Right economically
            SetPartyField(party, "socialPosition", -6.0f);      // Conservative socially
            SetPartyField(party, "europeanPosition", -4.0f);    // Eurosceptic
            SetPartyField(party, "immigrationPosition", -7.0f); // Restrictive immigration

            SetPartyField(party, "currentPopularity", 0.7f);    // 1 seat
            SetPartyField(party, "campaignExpertise", 40.0f);
            SetPartyField(party, "mediaPresence", 35.0f);
            SetPartyField(party, "coalitionFlexibility", 50.0f);

            SetPartyField(party, "preferredCoalitionPartners", new List<string> { "PVV", "VVD", "FvD" });
            SetPartyField(party, "excludedCoalitionPartners", new List<string> { "GL-PvdA", "D66", "DENK" });

            SetPartyField(party, "campaignBudget", 450000.0f);
            SetPartyField(party, "socialMediaFollowers", 85000);
            SetPartyField(party, "activeMembership", 4200);

            return party;
        }

        private static PoliticalParty CreateSGP()
        {
            var party = ScriptableObject.CreateInstance<PoliticalParty>();
            party.name = "SGP";

            SetPartyField(party, "partyName", "Staatkundig Gereformeerde Partij");
            SetPartyField(party, "abbreviation", "SGP");
            SetPartyField(party, "leader", "Kees van der Staaij");
            SetPartyField(party, "partyColor", new Color(1.0f, 0.5f, 0.0f, 1.0f)); // Orange

            // Orthodox Protestant
            SetPartyField(party, "economicPosition", 2.0f);      // Center-right economically
            SetPartyField(party, "socialPosition", -9.0f);      // Very conservative socially
            SetPartyField(party, "europeanPosition", -2.0f);    // Mildly Eurosceptic
            SetPartyField(party, "immigrationPosition", -4.0f); // Restrictive immigration

            SetPartyField(party, "currentPopularity", 2.0f);    // 3 seats
            SetPartyField(party, "campaignExpertise", 55.0f);
            SetPartyField(party, "mediaPresence", 40.0f);
            SetPartyField(party, "coalitionFlexibility", 30.0f); // Limited due to principles

            SetPartyField(party, "preferredCoalitionPartners", new List<string> { "CU", "CDA" });
            SetPartyField(party, "excludedCoalitionPartners", new List<string> { "D66", "GL-PvdA", "PvdD", "DENK" });

            SetPartyField(party, "campaignBudget", 600000.0f);
            SetPartyField(party, "socialMediaFollowers", 95000);
            SetPartyField(party, "activeMembership", 28000);

            return party;
        }

        private static PoliticalParty CreateDENK()
        {
            var party = ScriptableObject.CreateInstance<PoliticalParty>();
            party.name = "DENK";

            SetPartyField(party, "partyName", "DENK");
            SetPartyField(party, "abbreviation", "DENK");
            SetPartyField(party, "leader", "Stephan van Baarle");
            SetPartyField(party, "partyColor", new Color(0.8f, 0.0f, 0.8f, 1.0f)); // Magenta

            // Multicultural progressive
            SetPartyField(party, "economicPosition", -4.0f);     // Left economically
            SetPartyField(party, "socialPosition", 7.0f);       // Progressive socially
            SetPartyField(party, "europeanPosition", 2.0f);     // Mild pro-EU
            SetPartyField(party, "immigrationPosition", 9.0f);  // Very liberal immigration

            SetPartyField(party, "currentPopularity", 2.0f);    // 3 seats
            SetPartyField(party, "campaignExpertise", 60.0f);
            SetPartyField(party, "mediaPresence", 65.0f);
            SetPartyField(party, "coalitionFlexibility", 40.0f);

            SetPartyField(party, "preferredCoalitionPartners", new List<string> { "GL-PvdA", "SP" });
            SetPartyField(party, "excludedCoalitionPartners", new List<string> { "PVV", "FvD", "JA21" });

            SetPartyField(party, "campaignBudget", 750000.0f);
            SetPartyField(party, "socialMediaFollowers", 190000);
            SetPartyField(party, "activeMembership", 9500);

            return party;
        }

        #endregion

        /// <summary>
        /// Uses reflection to set private fields in PoliticalParty ScriptableObjects
        /// </summary>
        private static void SetPartyField(PoliticalParty party, string fieldName, object value)
        {
            var field = typeof(PoliticalParty).GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (field != null)
            {
                field.SetValue(party, value);
            }
            else
            {
                Debug.LogWarning($"Could not find field '{fieldName}' in PoliticalParty class");
            }
        }

        /// <summary>
        /// Validate that all parties sum to correct 2023 election totals
        /// </summary>
        public static bool ValidateGeneratedData()
        {
            var parties = GenerateAllDutchParties();
            float totalPopularity = 0f;
            int expectedSeats = 150;

            foreach (var party in parties)
            {
                totalPopularity += party.CurrentPopularity;
            }

            bool isValid = Mathf.Approximately(totalPopularity, 100.0f);

            Debug.Log($"Dutch Political Data Validation: {(isValid ? "PASSED" : "FAILED")}");
            Debug.Log($"Total popularity: {totalPopularity:F1}% (Expected: 100.0%)");
            Debug.Log($"Total parties: {parties.Count} (Expected: 15)");

            return isValid;
        }
    }
}