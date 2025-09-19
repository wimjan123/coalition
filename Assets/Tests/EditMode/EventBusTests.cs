using NUnit.Framework;
using System;
using Coalition.Core;

namespace Coalition.Tests.EditMode
{
    /// <summary>
    /// Comprehensive unit tests for the EventBus system
    /// Testing event subscription, publishing, unsubscription, and error handling
    /// </summary>
    public class EventBusTests
    {
        // Test event types for validation
        public class TestEvent : IGameEvent
        {
            public string Message { get; set; }
            public int Value { get; set; }
        }

        public class AnotherTestEvent : IGameEvent
        {
            public bool Flag { get; set; }
        }

        [SetUp]
        public void SetUp()
        {
            // Clear EventBus before each test to ensure isolation
            EventBus.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up after each test
            EventBus.Clear();
        }

        [Test]
        public void Subscribe_SingleListener_ShouldAddListener()
        {
            // Arrange
            bool eventReceived = false;
            Action<TestEvent> listener = (e) => eventReceived = true;

            // Act
            EventBus.Subscribe(listener);
            EventBus.Publish(new TestEvent { Message = "Test", Value = 42 });

            // Assert
            Assert.IsTrue(eventReceived, "Event should be received by subscribed listener");
        }

        [Test]
        public void Subscribe_MultipleListeners_ShouldNotifyAll()
        {
            // Arrange
            int callCount = 0;
            Action<TestEvent> listener1 = (e) => callCount++;
            Action<TestEvent> listener2 = (e) => callCount++;
            Action<TestEvent> listener3 = (e) => callCount++;

            // Act
            EventBus.Subscribe(listener1);
            EventBus.Subscribe(listener2);
            EventBus.Subscribe(listener3);
            EventBus.Publish(new TestEvent { Message = "Test", Value = 42 });

            // Assert
            Assert.AreEqual(3, callCount, "All three listeners should receive the event");
        }

        [Test]
        public void Publish_WithEventData_ShouldPassCorrectData()
        {
            // Arrange
            TestEvent receivedEvent = null;
            Action<TestEvent> listener = (e) => receivedEvent = e;
            var testEvent = new TestEvent { Message = "Hello Political Simulation", Value = 123 };

            // Act
            EventBus.Subscribe(listener);
            EventBus.Publish(testEvent);

            // Assert
            Assert.IsNotNull(receivedEvent, "Event data should be received");
            Assert.AreEqual("Hello Political Simulation", receivedEvent.Message);
            Assert.AreEqual(123, receivedEvent.Value);
        }

        [Test]
        public void Unsubscribe_ExistingListener_ShouldRemoveListener()
        {
            // Arrange
            bool eventReceived = false;
            Action<TestEvent> listener = (e) => eventReceived = true;

            EventBus.Subscribe(listener);

            // Act
            EventBus.Unsubscribe(listener);
            EventBus.Publish(new TestEvent { Message = "Test", Value = 42 });

            // Assert
            Assert.IsFalse(eventReceived, "Event should not be received after unsubscription");
        }

        [Test]
        public void Unsubscribe_NonExistentListener_ShouldNotThrow()
        {
            // Arrange
            Action<TestEvent> listener = (e) => { };

            // Act & Assert
            Assert.DoesNotThrow(() => EventBus.Unsubscribe(listener),
                "Unsubscribing non-existent listener should not throw exception");
        }

        [Test]
        public void Publish_NoListeners_ShouldNotThrow()
        {
            // Arrange
            var testEvent = new TestEvent { Message = "Test", Value = 42 };

            // Act & Assert
            Assert.DoesNotThrow(() => EventBus.Publish(testEvent),
                "Publishing event with no listeners should not throw exception");
        }

        [Test]
        public void EventBus_DifferentEventTypes_ShouldIsolateListeners()
        {
            // Arrange
            bool testEventReceived = false;
            bool anotherTestEventReceived = false;

            Action<TestEvent> testListener = (e) => testEventReceived = true;
            Action<AnotherTestEvent> anotherListener = (e) => anotherTestEventReceived = true;

            // Act
            EventBus.Subscribe(testListener);
            EventBus.Subscribe(anotherListener);

            EventBus.Publish(new TestEvent { Message = "Test", Value = 42 });

            // Assert
            Assert.IsTrue(testEventReceived, "TestEvent listener should receive TestEvent");
            Assert.IsFalse(anotherTestEventReceived, "AnotherTestEvent listener should not receive TestEvent");
        }

        [Test]
        public void Clear_WithSubscribedListeners_ShouldRemoveAllListeners()
        {
            // Arrange
            bool eventReceived = false;
            Action<TestEvent> listener = (e) => eventReceived = true;

            EventBus.Subscribe(listener);

            // Act
            EventBus.Clear();
            EventBus.Publish(new TestEvent { Message = "Test", Value = 42 });

            // Assert
            Assert.IsFalse(eventReceived, "Event should not be received after clearing EventBus");
        }

        [Test]
        public void EventBus_ExceptionInListener_ShouldNotAffectOtherListeners()
        {
            // Arrange
            bool goodListenerCalled = false;
            Action<TestEvent> throwingListener = (e) => throw new Exception("Test exception");
            Action<TestEvent> goodListener = (e) => goodListenerCalled = true;

            // Act
            EventBus.Subscribe(throwingListener);
            EventBus.Subscribe(goodListener);
            EventBus.Publish(new TestEvent { Message = "Test", Value = 42 });

            // Assert
            Assert.IsTrue(goodListenerCalled, "Good listener should still be called despite exception in other listener");
        }

        [Test]
        public void Subscribe_SameListenerTwice_ShouldAddTwice()
        {
            // Arrange
            int callCount = 0;
            Action<TestEvent> listener = (e) => callCount++;

            // Act
            EventBus.Subscribe(listener);
            EventBus.Subscribe(listener); // Subscribe same listener twice
            EventBus.Publish(new TestEvent { Message = "Test", Value = 42 });

            // Assert
            Assert.AreEqual(2, callCount, "Same listener subscribed twice should be called twice");
        }

        [Test]
        public void EventBus_MemoryManagement_ShouldNotLeakSubscriptions()
        {
            // Arrange
            Action<TestEvent> listener = (e) => { };

            // Act
            EventBus.Subscribe(listener);
            EventBus.Unsubscribe(listener);

            // Simulate garbage collection scenario
            listener = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();

            // Assert
            Assert.DoesNotThrow(() => EventBus.Publish(new TestEvent { Message = "Test", Value = 42 }),
                "EventBus should handle cleanup properly");
        }

        /// <summary>
        /// Performance test to ensure EventBus scales well with many listeners
        /// </summary>
        [Test]
        public void EventBus_ManyListeners_ShouldPerformWell()
        {
            // Arrange
            const int listenerCount = 1000;
            int totalCalls = 0;

            for (int i = 0; i < listenerCount; i++)
            {
                Action<TestEvent> listener = (e) => totalCalls++;
                EventBus.Subscribe(listener);
            }

            var startTime = DateTime.Now;

            // Act
            EventBus.Publish(new TestEvent { Message = "Performance Test", Value = 1 });

            var endTime = DateTime.Now;
            var duration = endTime - startTime;

            // Assert
            Assert.AreEqual(listenerCount, totalCalls, $"All {listenerCount} listeners should be called");
            Assert.Less(duration.TotalMilliseconds, 100, "EventBus should handle many listeners efficiently");
        }
    }
}