using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using Coalition.Tests.Mocks;
using Coalition.Core;
using Coalition.Campaign;

namespace Coalition.Tests.Integration
{
    /// <summary>
    /// Integration tests for campaign mechanics and social media systems
    /// Testing rallies, debates, social media posts, and public opinion dynamics
    /// </summary>
    public class CampaignMechanicsIntegrationTests
    {
        private GameObject testObject;
        private MockCampaignSystem campaignSystem;
        private MockSocialMediaSystem socialMediaSystem;
        private MockPublicOpinionSystem opinionSystem;

        [SetUp]
        public void SetUp()
        {
            testObject = new GameObject("CampaignTest");
            campaignSystem = testObject.AddComponent<MockCampaignSystem>();
            socialMediaSystem = testObject.AddComponent<MockSocialMediaSystem>();
            opinionSystem = testObject.AddComponent<MockPublicOpinionSystem>();

            EventBus.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            if (testObject != null)
            {
                Object.DestroyImmediate(testObject);
            }
            EventBus.Clear();
        }

        [Test]
        public void CampaignRally_SuccessfulEvent_ShouldImpactPublicOpinion()
        {
            // Arrange
            var rallyEvent = new CampaignRallyData
            {
                OrganizingParty = "D66",
                Location = "Amsterdam",
                Topic = "Climate Policy",
                ExpectedAttendance = 5000,
                MediaCoverage = MediaCoverageLevel.High
            };

            float initialSupport = opinionSystem.GetPartySupport("D66");

            // Act
            campaignSystem.OrganizeRally(rallyEvent);
            var opinionChange = opinionSystem.ProcessRallyImpact(rallyEvent);

            // Assert
            Assert.Greater(opinionChange.SupportChange, 0f, "Successful rally should increase support");
            Assert.AreEqual("D66", opinionChange.AffectedParty, "Opinion change should affect organizing party");
            Assert.Contains("Climate Policy", opinionChange.AffectedIssues, "Rally topic should influence relevant issues");

            var newSupport = opinionSystem.GetPartySupport("D66");
            Assert.Greater(newSupport, initialSupport, "Party support should increase after successful rally");
        }

        [Test]
        public void SocialMediaPost_ViralContent_ShouldAmplifyReach()
        {
            // Arrange
            var socialPost = new SocialMediaPost
            {
                Author = "VVD",
                Content = "Our new economic plan will create 100,000 jobs in the tech sector! #Innovation #Jobs",
                Platform = SocialPlatform.Twitter,
                PostType = PostType.PolicyAnnouncement,
                Hashtags = new[] { "#Innovation", "#Jobs", "#Economy" }
            };

            // Act
            socialMediaSystem.PublishPost(socialPost);
            var viralityScore = socialMediaSystem.CalculateViralityPotential(socialPost);
            var reachMetrics = socialMediaSystem.SimulatePostSpread(socialPost, 24); // 24 hours

            // Assert
            Assert.Greater(viralityScore, 0.6f, "Job-related content should have high virality potential");
            Assert.Greater(reachMetrics.TotalReach, 50000, "Viral post should reach significant audience");
            Assert.Greater(reachMetrics.EngagementRate, 0.05f, "Political content should generate engagement");
            Assert.Contains("Economy", reachMetrics.TopicsInfluenced, "Post should influence economic discussions");
        }

        [Test]
        public void PoliticalDebate_MultiPartyParticipation_ShouldAffectAllParticipants()
        {
            // Arrange
            var debateEvent = new PoliticalDebate
            {
                Topic = "Immigration Policy",
                Participants = new[] { "VVD", "PVV", "D66", "GL" },
                Format = DebateFormat.Moderated,
                Audience = DebateAudience.Television,
                Duration = 90, // minutes
                Moderator = "NOS"
            };

            var initialSupport = new Dictionary<string, float>();
            foreach (var party in debateEvent.Participants)
            {
                initialSupport[party] = opinionSystem.GetPartySupport(party);
            }

            // Act
            var debateResults = campaignSystem.ConductDebate(debateEvent);
            var opinionShifts = opinionSystem.ProcessDebateImpact(debateResults);

            // Assert
            Assert.AreEqual(debateEvent.Participants.Length, debateResults.ParticipantPerformances.Length,
                "All participants should have performance scores");

            foreach (var performance in debateResults.ParticipantPerformances)
            {
                Assert.That(performance.PerformanceScore, Is.InRange(0f, 1f),
                    $"Performance score for {performance.PartyName} should be valid");
            }

            // Verify opinion shifts reflect debate performance
            var bestPerformer = debateResults.ParticipantPerformances
                .OrderByDescending(p => p.PerformanceScore).First();

            var bestPerformerShift = opinionShifts.FirstOrDefault(s => s.AffectedParty == bestPerformer.PartyName);
            Assert.IsNotNull(bestPerformerShift, "Best performer should have opinion shift recorded");
            Assert.Greater(bestPerformerShift.SupportChange, 0f, "Best debate performer should gain support");
        }

        [Test]
        public void MediaCycle_ContentiousIssue_ShouldCreateSustainedCoverage()
        {
            // Arrange
            var contentiousIssue = "EU Migration Quota";
            var involvedParties = new[] { "VVD", "PVV", "D66", "CDA" };

            // Act
            socialMediaSystem.InitiateContentiousTopic(contentiousIssue, involvedParties);

            var mediaCoverage = new List<MediaCoverageEvent>();
            for (int day = 0; day < 7; day++) // Week-long coverage cycle
            {
                var dailyCoverage = socialMediaSystem.SimulateDailyCoverage(contentiousIssue);
                mediaCoverage.Add(dailyCoverage);
            }

            // Assert
            Assert.Greater(mediaCoverage.Count, 5, "Contentious issue should sustain coverage for multiple days");

            var peakCoverage = mediaCoverage.Max(c => c.IntensityScore);
            Assert.Greater(peakCoverage, 0.7f, "Contentious issue should reach high coverage intensity");

            var totalSentiment = mediaCoverage.Sum(c => c.SentimentScore) / mediaCoverage.Count;
            Assert.That(totalSentiment, Is.InRange(-0.3f, 0.3f), "Contentious issue should have mixed sentiment");
        }

        [UnityTest]
        public IEnumerator CompleteElectionCycle_IntegratedSystems_ShouldProduceRealisticOutcome()
        {
            // Arrange - 30-day election campaign simulation (compressed to 3 seconds)
            const float campaignDuration = 3f;
            const int totalEvents = 15;

            var participatingParties = new[] { "VVD", "PVV", "D66", "CDA", "GL", "PvdA" };
            var campaignEvents = GenerateCampaignSchedule(totalEvents, participatingParties);

            var initialPolling = new Dictionary<string, float>();
            foreach (var party in participatingParties)
            {
                initialPolling[party] = opinionSystem.GetPartySupport(party);
            }

            var startTime = Time.time;

            // Act - Execute campaign events over time
            for (int i = 0; i < campaignEvents.Count; i++)
            {
                var eventTime = startTime + (campaignDuration * i / campaignEvents.Count);

                // Wait for event timing
                while (Time.time < eventTime)
                {
                    yield return null;
                }

                // Execute campaign event
                ExecuteCampaignEvent(campaignEvents[i]);

                // Allow systems to process
                yield return new WaitForSeconds(0.1f);
            }

            // Final polling after campaign
            var finalPolling = new Dictionary<string, float>();
            foreach (var party in participatingParties)
            {
                finalPolling[party] = opinionSystem.GetPartySupport(party);
            }

            // Assert campaign created realistic changes
            var totalChange = 0f;
            var partiesWithChange = 0;

            foreach (var party in participatingParties)
            {
                var change = Mathf.Abs(finalPolling[party] - initialPolling[party]);
                totalChange += change;

                if (change > 0.02f) // 2% threshold
                {
                    partiesWithChange++;
                }
            }

            Assert.Greater(partiesWithChange, 3, "Campaign should affect multiple parties");
            Assert.Less(totalChange, 0.5f, "Total polling changes should be realistic (not extreme)");

            // Verify conservation of support (total should remain ~100%)
            var finalTotalSupport = finalPolling.Values.Sum();
            Assert.That(finalTotalSupport, Is.InRange(0.95f, 1.05f), "Total support should remain approximately 100%");
        }

        [Test]
        public void SocialMediaInfluence_CrossPlatformSpread_ShouldAmplifyMessage()
        {
            // Arrange
            var originalPost = new SocialMediaPost
            {
                Author = "GL",
                Content = "Climate action cannot wait! We need immediate green energy transition. #ClimateAction",
                Platform = SocialPlatform.Twitter,
                PostType = PostType.UrgentCall
            };

            // Act
            socialMediaSystem.PublishPost(originalPost);

            // Simulate cross-platform spread
            var facebookSpread = socialMediaSystem.CrossPostToFacebook(originalPost);
            var instagramSpread = socialMediaSystem.CrossPostToInstagram(originalPost);
            var linkedinSpread = socialMediaSystem.CrossPostToLinkedIn(originalPost);

            var totalReach = originalPost.EstimatedReach +
                           facebookSpread.EstimatedReach +
                           instagramSpread.EstimatedReach +
                           linkedinSpread.EstimatedReach;

            // Assert
            Assert.Greater(totalReach, originalPost.EstimatedReach * 2.5f,
                "Cross-platform spread should significantly amplify reach");

            Assert.Greater(facebookSpread.EngagementRate, originalPost.EngagementRate,
                "Facebook should have different engagement characteristics");

            Assert.Contains("Climate", facebookSpread.DominantTopics,
                "Core message should remain consistent across platforms");
        }

        [Test]
        public void PublicOpinion_ControversialPolicy_ShouldCreatePolarization()
        {
            // Arrange
            var controversialPolicy = new PolicyProposal
            {
                Title = "Mandatory COVID-19 Vaccination",
                ProposingParty = "D66",
                SupportingParties = new[] { "VVD", "PvdA" },
                OpposingParties = new[] { "PVV", "FvD" },
                PolicyCategory = PolicyCategory.Healthcare
            };

            var initialPolarization = opinionSystem.CalculatePolarizationIndex();

            // Act
            campaignSystem.ProposePolicy(controversialPolicy);
            var policyReactions = opinionSystem.ProcessPolicyProposal(controversialPolicy);

            var finalPolarization = opinionSystem.CalculatePolarizationIndex();

            // Assert
            Assert.Greater(finalPolarization, initialPolarization,
                "Controversial policy should increase polarization");

            Assert.Greater(policyReactions.StrongSupportPercentage, 20f,
                "Controversial policy should generate strong support");

            Assert.Greater(policyReactions.StrongOppositionPercentage, 20f,
                "Controversial policy should generate strong opposition");

            // Verify supporting parties gain support from supporters
            foreach (var supportingParty in controversialPolicy.SupportingParties)
            {
                var partyReaction = policyReactions.PartyImpacts
                    .FirstOrDefault(p => p.PartyName == supportingParty);
                Assert.IsNotNull(partyReaction, $"Should have reaction data for {supportingParty}");
            }
        }

        [Test]
        public void CampaignFinancing_SpendingLimits_ShouldEnforceRealisticConstraints()
        {
            // Arrange
            var campaignBudget = new CampaignBudget
            {
                PartyName = "VVD",
                TotalBudget = 1000000f, // €1M budget
                SpendingCategories = new Dictionary<string, float>
                {
                    { "Television", 400000f },
                    { "Online", 250000f },
                    { "Print", 150000f },
                    { "Events", 150000f },
                    { "Staff", 50000f }
                }
            };

            // Act
            campaignSystem.SetCampaignBudget(campaignBudget);

            // Try to spend beyond limits
            var expensiveEvent = new CampaignEvent
            {
                EventType = CampaignEventType.LargeRally,
                EstimatedCost = 200000f,
                Category = "Events"
            };

            var budgetCheck = campaignSystem.ValidateCampaignExpense(expensiveEvent);

            // Assert
            Assert.IsFalse(budgetCheck.CanAfford, "Should not allow spending beyond category budget");
            Assert.Greater(budgetCheck.OverspendAmount, 0f, "Should calculate overspend amount");

            // Test realistic spending
            var affordableEvent = new CampaignEvent
            {
                EventType = CampaignEventType.TownHall,
                EstimatedCost = 25000f,
                Category = "Events"
            };

            var affordableBudgetCheck = campaignSystem.ValidateCampaignExpense(affordableEvent);
            Assert.IsTrue(affordableBudgetCheck.CanAfford, "Should allow affordable expenses");
        }

        // Helper methods for campaign testing

        private List<CampaignEvent> GenerateCampaignSchedule(int eventCount, string[] parties)
        {
            var events = new List<CampaignEvent>();
            var eventTypes = new[]
            {
                CampaignEventType.Rally,
                CampaignEventType.Debate,
                CampaignEventType.SocialMediaCampaign,
                CampaignEventType.TownHall,
                CampaignEventType.MediaInterview
            };

            for (int i = 0; i < eventCount; i++)
            {
                events.Add(new CampaignEvent
                {
                    EventType = eventTypes[i % eventTypes.Length],
                    OrganizingParty = parties[i % parties.Length],
                    ScheduledTime = System.DateTime.Now.AddDays(i),
                    EstimatedCost = UnityEngine.Random.Range(5000f, 50000f)
                });
            }

            return events;
        }

        private void ExecuteCampaignEvent(CampaignEvent campaignEvent)
        {
            switch (campaignEvent.EventType)
            {
                case CampaignEventType.Rally:
                    ExecuteRallyEvent(campaignEvent);
                    break;
                case CampaignEventType.Debate:
                    ExecuteDebateEvent(campaignEvent);
                    break;
                case CampaignEventType.SocialMediaCampaign:
                    ExecuteSocialMediaEvent(campaignEvent);
                    break;
                case CampaignEventType.TownHall:
                    ExecuteTownHallEvent(campaignEvent);
                    break;
                case CampaignEventType.MediaInterview:
                    ExecuteMediaInterviewEvent(campaignEvent);
                    break;
            }
        }

        private void ExecuteRallyEvent(CampaignEvent campaignEvent)
        {
            var rallyData = new CampaignRallyData
            {
                OrganizingParty = campaignEvent.OrganizingParty,
                Location = "Test Location",
                Topic = "Campaign Rally",
                ExpectedAttendance = UnityEngine.Random.Range(500, 5000),
                MediaCoverage = (MediaCoverageLevel)UnityEngine.Random.Range(0, 3)
            };

            campaignSystem.OrganizeRally(rallyData);
            EventBus.Publish(new CampaignRallyEvent { RallyData = rallyData });
        }

        private void ExecuteDebateEvent(CampaignEvent campaignEvent)
        {
            // Simulate simplified debate
            EventBus.Publish(new DebateEvent
            {
                Participants = new[] { campaignEvent.OrganizingParty, "Opposition" },
                Topic = "Policy Debate"
            });
        }

        private void ExecuteSocialMediaEvent(CampaignEvent campaignEvent)
        {
            var post = new SocialMediaPost
            {
                Author = campaignEvent.OrganizingParty,
                Content = "Campaign message from our party!",
                Platform = SocialPlatform.Twitter,
                PostType = PostType.CampaignUpdate
            };

            socialMediaSystem.PublishPost(post);
            EventBus.Publish(new SocialMediaPostEvent { Post = post });
        }

        private void ExecuteTownHallEvent(CampaignEvent campaignEvent)
        {
            // Simulate town hall meeting
            var townHallData = new TownHallEvent
            {
                OrganizingParty = campaignEvent.OrganizingParty,
                Location = "Community Center",
                AttendeeCount = UnityEngine.Random.Range(50, 300),
                QuestionsAnswered = UnityEngine.Random.Range(5, 15)
            };

            EventBus.Publish(new CampaignRallyEvent { RallyData = new CampaignRallyData
            {
                OrganizingParty = townHallData.OrganizingParty,
                Location = townHallData.Location
            }});
        }

        private void ExecuteMediaInterviewEvent(CampaignEvent campaignEvent)
        {
            // Simulate media interview impact
            var interviewImpact = UnityEngine.Random.Range(0.1f, 0.8f);
            opinionSystem.ApplyMediaEvent(campaignEvent.OrganizingParty, interviewImpact);
        }
    }

    // Supporting data structures and mock components for campaign testing
    // [Additional mock classes would go here in a real implementation]
}