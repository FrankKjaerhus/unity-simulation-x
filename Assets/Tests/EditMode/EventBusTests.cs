using System;
using NUnit.Framework;
using UnitySimulationX.Core;

namespace UnitySimulationX.Tests.EditMode
{
    public sealed class EventBusTests
    {
        [Test]
        public void Dispose_UnsubscribesHandler()
        {
            var calls = 0;
            var bus = new EventBus(_ => { });
            var subscription = bus.Subscribe<HierarchyChangedEvent>(_ => calls++);
            subscription.Dispose();
            bus.Publish(new HierarchyChangedEvent());
            Assert.AreEqual(0, calls);
        }

        [Test]
        public void Publish_IsolatesSubscriberException()
        {
            Exception reported = null;
            var secondCalled = false;
            var bus = new EventBus(ex => reported = ex);
            bus.Subscribe<HierarchyChangedEvent>(_ => throw new InvalidOperationException("boom"));
            bus.Subscribe<HierarchyChangedEvent>(_ => secondCalled = true);

            bus.Publish(new HierarchyChangedEvent());

            Assert.IsInstanceOf<InvalidOperationException>(reported);
            Assert.IsTrue(secondCalled);
        }
    }
}
