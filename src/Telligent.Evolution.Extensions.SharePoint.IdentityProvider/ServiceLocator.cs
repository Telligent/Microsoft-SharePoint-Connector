using System;
using System.Collections.Generic;
using Telligent.Evolution.Extensions.SharePoint.IdentityProvider.ScriptedExtension;

namespace Telligent.Evolution.Extensions.SharePoint.IdentityProvider
{
    public static class ServiceLocator
    {
        private static readonly object _lockObject = new object();
        private static volatile Dictionary<Type, object> _instances;

        public static Dictionary<Type, object> Initialize()
        {
            return new Dictionary<Type, object>
            {
                // Internal Api
                {typeof(ISAMLAuthentication), new SAMLAuthentication()}
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
