using System;
using System.Collections.Generic;
using UnityEngine;

namespace Coalition.Runtime.Core
{
    /// <summary>
    /// Global event bus for loose coupling between systems in the coalition demo.
    /// Provides publish-subscribe pattern for system communication.
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, List<IEventHandler>> eventHandlers = new Dictionary<Type, List<IEventHandler>>();
        private static readonly Dictionary<Type, List<object>> cachedEvents = new Dictionary<Type, List<object>>();
        private static readonly object lockObject = new object();

        // Configuration
        private static int maxCachedEventsPerType = 100;
        private static bool enableDebugLogging = false;

        /// <summary>
        /// Subscribe to events of type T.
        /// </summary>
        /// <typeparam name="T">Event type</typeparam>
        /// <param name="handler">Handler action</param>
        public static void Subscribe<T>(Action<T> handler) where T : class, IEvent
        {
            lock (lockObject)
            {
                var eventType = typeof(T);
                if (!eventHandlers.ContainsKey(eventType))
                {
                    eventHandlers[eventType] = new List<IEventHandler>();
                }

                var wrapper = new EventHandlerWrapper<T>(handler);
                eventHandlers[eventType].Add(wrapper);

                if (enableDebugLogging)
                    Debug.Log($"[EventBus] Subscribed to {eventType.Name}. Total handlers: {eventHandlers[eventType].Count}");
            }
        }

        /// <summary>
        /// Unsubscribe from events of type T.
        /// </summary>
        /// <typeparam name="T">Event type</typeparam>
        /// <param name="handler">Handler action to remove</param>
        public static void Unsubscribe<T>(Action<T> handler) where T : class, IEvent
        {
            lock (lockObject)
            {
                var eventType = typeof(T);
                if (!eventHandlers.ContainsKey(eventType)) return;

                var handlersToRemove = new List<IEventHandler>();
                foreach (var eventHandler in eventHandlers[eventType])
                {
                    if (eventHandler is EventHandlerWrapper<T> wrapper && wrapper.Handler.Equals(handler))
                    {
                        handlersToRemove.Add(eventHandler);
                    }
                }

                foreach (var handlerToRemove in handlersToRemove)
                {
                    eventHandlers[eventType].Remove(handlerToRemove);
                }

                if (enableDebugLogging)
                    Debug.Log($"[EventBus] Unsubscribed from {eventType.Name}. Remaining handlers: {eventHandlers[eventType].Count}");
            }
        }

        /// <summary>
        /// Publish an event to all subscribers.
        /// </summary>
        /// <typeparam name="T">Event type</typeparam>
        /// <param name="eventData">Event data</param>
        public static void Publish<T>(T eventData) where T : class, IEvent
        {
            if (eventData == null) return;

            lock (lockObject)
            {
                var eventType = typeof(T);

                // Cache the event for late subscribers
                CacheEvent(eventType, eventData);

                // Publish to current subscribers
                if (eventHandlers.ContainsKey(eventType))
                {
                    var handlers = new List<IEventHandler>(eventHandlers[eventType]); // Copy to avoid modification during iteration

                    foreach (var handler in handlers)
                    {
                        try
                        {
                            handler.Handle(eventData);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"[EventBus] Error handling event {eventType.Name}: {ex.Message}\n{ex.StackTrace}");
                        }
                    }

                    if (enableDebugLogging)
                        Debug.Log($"[EventBus] Published {eventType.Name} to {handlers.Count} handlers");
                }
                else if (enableDebugLogging)
                {
                    Debug.Log($"[EventBus] Published {eventType.Name} but no handlers registered");
                }
            }
        }

        /// <summary>
        /// Get the last cached event of type T, if any.
        /// </summary>
        /// <typeparam name="T">Event type</typeparam>
        /// <returns>Last cached event or null</returns>
        public static T GetLastEvent<T>() where T : class, IEvent
        {
            lock (lockObject)
            {
                var eventType = typeof(T);
                if (cachedEvents.ContainsKey(eventType) && cachedEvents[eventType].Count > 0)
                {
                    var events = cachedEvents[eventType];
                    return events[events.Count - 1] as T;
                }
                return null;
            }
        }

        /// <summary>
        /// Get all cached events of type T.
        /// </summary>
        /// <typeparam name="T">Event type</typeparam>
        /// <returns>List of cached events</returns>
        public static List<T> GetCachedEvents<T>() where T : class, IEvent
        {
            lock (lockObject)
            {
                var eventType = typeof(T);
                var result = new List<T>();

                if (cachedEvents.ContainsKey(eventType))
                {
                    foreach (var evt in cachedEvents[eventType])
                    {
                        if (evt is T typedEvent)
                            result.Add(typedEvent);
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// Clear all cached events.
        /// </summary>
        public static void ClearExpiredEvents()
        {
            lock (lockObject)
            {
                var currentTime = DateTime.Now;
                var typesToClear = new List<Type>();

                foreach (var kvp in cachedEvents)
                {
                    var eventType = kvp.Key;
                    var events = kvp.Value;
                    var eventsToRemove = new List<object>();

                    foreach (var evt in events)
                    {
                        if (evt is IEvent eventWithTime &&
                            (currentTime - eventWithTime.Timestamp).TotalMinutes > 5) // Clear events older than 5 minutes
                        {
                            eventsToRemove.Add(evt);
                        }
                    }

                    foreach (var eventToRemove in eventsToRemove)
                    {
                        events.Remove(eventToRemove);
                    }

                    if (events.Count == 0)
                    {
                        typesToClear.Add(eventType);
                    }
                }

                foreach (var typeToC
lear in typesToClear)
                {
                    cachedEvents.Remove(typeToC
lear);
                }

                if (enableDebugLogging)
                    Debug.Log($"[EventBus] Cleared expired events. Active event types: {cachedEvents.Count}");
            }
        }

        /// <summary>
        /// Clear all events and handlers. Use with caution.
        /// </summary>
        public static void Clear()
        {
            lock (lockObject)
            {
                eventHandlers.Clear();
                cachedEvents.Clear();

                if (enableDebugLogging)
                    Debug.Log("[EventBus] Cleared all events and handlers");
            }
        }

        /// <summary>
        /// Get statistics about current event bus state.
        /// </summary>
        public static EventBusStats GetStats()
        {
            lock (lockObject)
            {
                return new EventBusStats
                {
                    RegisteredEventTypes = eventHandlers.Count,
                    TotalHandlers = GetTotalHandlerCount(),
                    CachedEventTypes = cachedEvents.Count,
                    TotalCachedEvents = GetTotalCachedEventCount()
                };
            }
        }

        /// <summary>
        /// Enable or disable debug logging.
        /// </summary>
        /// <param name="enabled">True to enable debug logging</param>
        public static void SetDebugLogging(bool enabled)
        {
            enableDebugLogging = enabled;
        }

        private static void CacheEvent(Type eventType, object eventData)
        {
            if (!cachedEvents.ContainsKey(eventType))
            {
                cachedEvents[eventType] = new List<object>();
            }

            var events = cachedEvents[eventType];
            events.Add(eventData);

            // Limit cached events to prevent memory growth
            if (events.Count > maxCachedEventsPerType)
            {
                events.RemoveAt(0); // Remove oldest event
            }
        }

        private static int GetTotalHandlerCount()
        {
            int total = 0;
            foreach (var handlers in eventHandlers.Values)
            {
                total += handlers.Count;
            }
            return total;
        }

        private static int GetTotalCachedEventCount()
        {
            int total = 0;
            foreach (var events in cachedEvents.Values)
            {
                total += events.Count;
            }
            return total;
        }

        private interface IEventHandler
        {
            void Handle(object eventData);
        }

        private class EventHandlerWrapper<T> : IEventHandler where T : class, IEvent
        {
            public Action<T> Handler { get; }

            public EventHandlerWrapper(Action<T> handler)
            {
                Handler = handler;
            }

            public void Handle(object eventData)
            {
                if (eventData is T typedEvent)
                {
                    Handler(typedEvent);
                }
            }
        }
    }

    /// <summary>
    /// Base interface for all events in the system.
    /// </summary>
    public interface IEvent
    {
        DateTime Timestamp { get; }
    }

    /// <summary>
    /// Statistics about the current state of the event bus.
    /// </summary>
    public struct EventBusStats
    {
        public int RegisteredEventTypes;
        public int TotalHandlers;
        public int CachedEventTypes;
        public int TotalCachedEvents;

        public override string ToString()
        {
            return $"EventBus Stats - Types: {RegisteredEventTypes}, Handlers: {TotalHandlers}, " +
                   $"Cached Types: {CachedEventTypes}, Cached Events: {TotalCachedEvents}";
        }
    }
}