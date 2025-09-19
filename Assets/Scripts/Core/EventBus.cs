using System;
using System.Collections.Generic;
using UnityEngine;

namespace Coalition.Core
{
    /// <summary>
    /// Global event system for loose coupling between COALITION systems
    /// Enables political, campaign, and AI systems to communicate without direct dependencies
    /// </summary>
    public static class EventBus
    {
        private static Dictionary<Type, List<object>> eventListeners = new Dictionary<Type, List<object>>();

        // Subscribe to an event type
        public static void Subscribe<T>(Action<T> listener) where T : IGameEvent
        {
            Type eventType = typeof(T);

            if (!eventListeners.ContainsKey(eventType))
            {
                eventListeners[eventType] = new List<object>();
            }

            eventListeners[eventType].Add(listener);
            Debug.Log($"[EventBus] Subscribed to {eventType.Name}");
        }

        // Unsubscribe from an event type
        public static void Unsubscribe<T>(Action<T> listener) where T : IGameEvent
        {
            Type eventType = typeof(T);

            if (eventListeners.ContainsKey(eventType))
            {
                eventListeners[eventType].Remove(listener);
                Debug.Log($"[EventBus] Unsubscribed from {eventType.Name}");
            }
        }

        // Publish an event to all subscribers
        public static void Publish<T>(T gameEvent) where T : IGameEvent
        {
            Type eventType = typeof(T);

            if (eventListeners.ContainsKey(eventType))
            {
                var listeners = eventListeners[eventType];
                foreach (var listener in listeners)
                {
                    try
                    {
                        ((Action<T>)listener)?.Invoke(gameEvent);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[EventBus] Error handling {eventType.Name}: {e.Message}");
                    }
                }
                Debug.Log($"[EventBus] Published {eventType.Name} to {listeners.Count} listeners");
            }
        }

        // Clear all event listeners (useful for cleanup)
        public static void Clear()
        {
            eventListeners.Clear();
            Debug.Log("[EventBus] All event listeners cleared");
        }
    }

    // Base interface for all game events
    public interface IGameEvent
    {
        DateTime Timestamp { get; }
    }

    // Political Events
    public struct ElectionResultEvent : IGameEvent
    {
        public DateTime Timestamp { get; }
        public Dictionary<string, int> PartySeats { get; }
        public float Turnout { get; }

        public ElectionResultEvent(Dictionary<string, int> partySeats, float turnout)
        {
            Timestamp = DateTime.Now;
            PartySeats = partySeats;
            Turnout = turnout;
        }
    }

    public struct CoalitionFormedEvent : IGameEvent
    {
        public DateTime Timestamp { get; }
        public List<string> CoalitionParties { get; }
        public string PrimeMinisterParty { get; }

        public CoalitionFormedEvent(List<string> coalitionParties, string primeMinisterParty)
        {
            Timestamp = DateTime.Now;
            CoalitionParties = coalitionParties;
            PrimeMinisterParty = primeMinisterParty;
        }
    }

    public struct PublicOpinionChangeEvent : IGameEvent
    {
        public DateTime Timestamp { get; }
        public string PartyName { get; }
        public float OpinionChange { get; }
        public string Reason { get; }

        public PublicOpinionChangeEvent(string partyName, float opinionChange, string reason)
        {
            Timestamp = DateTime.Now;
            PartyName = partyName;
            OpinionChange = opinionChange;
            Reason = reason;
        }
    }

    // Campaign Events
    public struct SocialMediaPostEvent : IGameEvent
    {
        public DateTime Timestamp { get; }
        public string PartyName { get; }
        public string Platform { get; }
        public string Content { get; }
        public int Engagement { get; }

        public SocialMediaPostEvent(string partyName, string platform, string content, int engagement)
        {
            Timestamp = DateTime.Now;
            PartyName = partyName;
            Platform = platform;
            Content = content;
            Engagement = engagement;
        }
    }

    public struct DebateEvent : IGameEvent
    {
        public DateTime Timestamp { get; }
        public string DebateTopic { get; }
        public List<string> Participants { get; }
        public Dictionary<string, float> PerformanceScores { get; }

        public DebateEvent(string debateTopic, List<string> participants, Dictionary<string, float> performanceScores)
        {
            Timestamp = DateTime.Now;
            DebateTopic = debateTopic;
            Participants = participants;
            PerformanceScores = performanceScores;
        }
    }

    public struct CampaignRallyEvent : IGameEvent
    {
        public DateTime Timestamp { get; }
        public string PartyName { get; }
        public string Location { get; }
        public int Attendance { get; }
        public float SuccessRating { get; }

        public CampaignRallyEvent(string partyName, string location, int attendance, float successRating)
        {
            Timestamp = DateTime.Now;
            PartyName = partyName;
            Location = location;
            Attendance = attendance;
            SuccessRating = successRating;
        }
    }

    // AI Events
    public struct AIResponseGeneratedEvent : IGameEvent
    {
        public DateTime Timestamp { get; }
        public string PromptType { get; }
        public string PartyName { get; }
        public string Response { get; }
        public float GenerationTime { get; }

        public AIResponseGeneratedEvent(string promptType, string partyName, string response, float generationTime)
        {
            Timestamp = DateTime.Now;
            PromptType = promptType;
            PartyName = partyName;
            Response = response;
            GenerationTime = generationTime;
        }
    }

    // UI Events
    public struct WindowOpenedEvent : IGameEvent
    {
        public DateTime Timestamp { get; }
        public string WindowType { get; }
        public bool IsModal { get; }

        public WindowOpenedEvent(string windowType, bool isModal)
        {
            Timestamp = DateTime.Now;
            WindowType = windowType;
            IsModal = isModal;
        }
    }
}