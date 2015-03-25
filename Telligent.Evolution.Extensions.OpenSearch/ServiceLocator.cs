using System;
using System.Collections.Generic;

namespace Telligent.Evolution.Extensions.OpenSearch
{
    internal static class ServiceLocator
    {
        private static readonly object _lockObject = new object();
        private static volatile Dictionary<Type, object> _instances;

        public static Dictionary<Type, object> Initialize()
        {
            return new Dictionary<Type, object>
            {
                {typeof(ScriptedExtension.IOpenSearch), new ScriptedExtension.OpenSearch()}
            };
        }

        public static T Get<T>()
        {
            EnsureInitialized();
            return (T)_instances[typeof(T)];
        }

        private static void EnsureInitialized()
        {
            if (_instances == null)
            {
                lock (_lockObject)
                {
                    if (_instances == null)
                    {
                        _instances = Initialize();
                    }
                }
            }
        }
    }
}
