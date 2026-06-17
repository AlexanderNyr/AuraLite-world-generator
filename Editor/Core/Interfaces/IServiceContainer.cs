using System;

namespace AuraLiteWorldGenerator.Editor.Core
{
    public interface IServiceContainer
    {
        void Register<TInterface, TImpl>(bool singleton = true) where TImpl : TInterface;
        void RegisterInstance<TInterface>(TInterface instance);
        T Resolve<T>();
    }
}
