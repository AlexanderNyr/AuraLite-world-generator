using System;
using System.Collections.Generic;

namespace AuraLiteWorldGenerator.Editor.Core
{
    public class ServiceContainer : IServiceContainer
    {
        private readonly Dictionary<Type, Func<object>> _factories = new Dictionary<Type, Func<object>>();
        private readonly Dictionary<Type, object> _singletons = new Dictionary<Type, object>();

        public void Register<TInterface, TImpl>(bool singleton = true) where TImpl : TInterface
        {
            if (singleton)
            {
                _factories[typeof(TInterface)] = () => 
                {
                    if (!_singletons.TryGetValue(typeof(TInterface), out var instance))
                    {
                        instance = Activator.CreateInstance<TImpl>();
                        _singletons[typeof(TInterface)] = instance;
                    }
                    return instance;
                };
            }
            else
            {
                _factories[typeof(TInterface)] = () => Activator.CreateInstance<TImpl>();
            }
        }

        public void RegisterInstance<TInterface>(TInterface instance)
        {
            _singletons[typeof(TInterface)] = instance;
            _factories[typeof(TInterface)] = () => instance;
        }

        public T Resolve<T>()
        {
            if (_factories.TryGetValue(typeof(T), out var factory))
            {
                return (T)factory();
            }
            throw new Exception($"Service {typeof(T).Name} not registered.");
        }
    }
}
