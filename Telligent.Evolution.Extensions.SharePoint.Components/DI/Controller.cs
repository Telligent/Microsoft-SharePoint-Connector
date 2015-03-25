using System;
using System.Collections.Generic;

namespace Telligent.Evolution.Extensions.SharePoint.Components.DI
{
    public interface IBindingSyntax
    {
        void Bind<T, TC>() where TC : T;
    }

    public interface IGetSyntax
    {
        T Get<T>() where T : class;
    }

    public class Controller : IBindingSyntax, IGetSyntax
    {
        private readonly Dictionary<Type, Type> _interfaceImplementation = new Dictionary<Type, Type>();

        public void Bind<T, TC>() where TC : T
        {
            _interfaceImplementation.Add(typeof(T), typeof(TC));
        }

        public T Get<T>() where T : class
        {
            if (!_interfaceImplementation.ContainsKey(typeof(T)))
            {
                throw new NotImplementedException(string.Format("The type {0} has not been implemented.", typeof(T)));
            }
            return (T)Activator.CreateInstance(_interfaceImplementation[typeof(T)], nonPublic: true);
        }
    }
}
