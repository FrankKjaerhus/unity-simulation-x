using System;
using System.Collections.Generic;

namespace UnitySimulationX.Core
{
    public static class ServiceLocator
    {
        static readonly Dictionary<Type, object> Services = new();

        public static void Register<T>(T instance) where T : class
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            Services[typeof(T)] = instance;
        }

        public static T Resolve<T>() where T : class
        {
            if (Services.TryGetValue(typeof(T), out var service))
                return (T)service;

            throw new InvalidOperationException($"Service not registered: {typeof(T).Name}");
        }

        public static bool TryResolve<T>(out T service) where T : class
        {
            if (Services.TryGetValue(typeof(T), out var value))
            {
                service = (T)value;
                return true;
            }

            service = null;
            return false;
        }

        public static void Clear() => Services.Clear();
    }
}
