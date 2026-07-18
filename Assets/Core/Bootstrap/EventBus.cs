using System;
using System.Collections.Generic;

namespace UnitySimulationX.Core
{
    public sealed class EventBus : IEventBus
    {
        readonly Dictionary<Type, List<Delegate>> _subscribers = new();
        readonly Action<Exception> _reportSubscriberError;

        public EventBus(Action<Exception> reportSubscriberError)
        {
            _reportSubscriberError = reportSubscriberError ?? (_ => { });
        }

        public IDisposable Subscribe<T>(Action<T> handler)
        {
            var type = typeof(T);
            if (!_subscribers.TryGetValue(type, out var list))
            {
                list = new List<Delegate>();
                _subscribers[type] = list;
            }

            list.Add(handler);
            return new Subscription(() =>
            {
                if (_subscribers.TryGetValue(type, out var current))
                    current.Remove(handler);
            });
        }

        public void Publish<T>(T message)
        {
            if (!_subscribers.TryGetValue(typeof(T), out var list))
                return;

            var snapshot = list.ToArray();
            foreach (var handler in snapshot)
            {
                if (handler is not Action<T> action)
                    continue;

                try
                {
                    action(message);
                }
                catch (Exception ex)
                {
                    _reportSubscriberError(ex);
                }
            }
        }

        sealed class Subscription : IDisposable
        {
            Action _dispose;

            public Subscription(Action dispose) => _dispose = dispose;

            public void Dispose()
            {
                _dispose?.Invoke();
                _dispose = null;
            }
        }
    }
}
