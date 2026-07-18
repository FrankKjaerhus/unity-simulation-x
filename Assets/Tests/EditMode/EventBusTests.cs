using System;
using NUnit.Framework;
using UnitySimulationX.Core;
using UnitySimulationX.Editing;

namespace UnitySimulationX.Tests.EditMode
{
    public sealed class EventBusTests
    {
        [Test]
        public void Dispose_UnsubscribesHandler()
        {
            var calls = 0;
            var bus = new EventBus(_ => { });
            var subscription = bus.Subscribe<SceneChangedEvent>(_ => calls++);
            subscription.Dispose();
            bus.Publish(new SceneChangedEvent());
            Assert.AreEqual(0, calls);
        }

        [Test]
        public void Publish_IsolatesSubscriberException()
        {
            Exception reported = null;
            var secondCalled = false;
            var bus = new EventBus(ex => reported = ex);
            bus.Subscribe<SceneChangedEvent>(_ => throw new InvalidOperationException("boom"));
            bus.Subscribe<SceneChangedEvent>(_ => secondCalled = true);

            bus.Publish(new SceneChangedEvent());

            Assert.IsInstanceOf<InvalidOperationException>(reported);
            Assert.IsTrue(secondCalled);
        }
    }
}
