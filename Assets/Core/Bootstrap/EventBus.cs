using System;
using System.Collections.Generic;

namespace UnitySimulationX.Core
{
    public static class EventBus
    {
        static readonly Dictionary<Type, List<Delegate>> Subscribers = new();

        public static void Subscribe<T>(Action<T> handler)
        {
            var type = typeof(T);
            if (!Subscribers.TryGetValue(type, out var list))
            {
                list = new List<Delegate>();
                Subscribers[type] = list;
            }

            list.Add(handler);
        }

        public static void Unsubscribe<T>(Action<T> handler)
        {
            if (!Subscribers.TryGetValue(typeof(T), out var list))
                return;

            list.Remove(handler);
        }

        public static void Publish<T>(T eventData)
        {
            if (!Subscribers.TryGetValue(typeof(T), out var list))
                return;

            var snapshot = list.ToArray();
            foreach (var handler in snapshot)
            {
                if (handler is Action<T> action)
                    action(eventData);
            }
        }

        public static void Clear() => Subscribers.Clear();
    }
}
