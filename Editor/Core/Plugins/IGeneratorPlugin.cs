using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AuraLiteWorldGenerator.Editor.Core
{
    public interface IGeneratorPlugin
    {
        string Name { get; }
        void RegisterServices(IServiceContainer container);
    }

    public static class PluginLoader
    {
        public static List<IGeneratorPlugin> LoadPlugins()
        {
            var plugins = new List<IGeneratorPlugin>();
            var pluginTypes = TypeCache.GetTypesDerivedFrom<IGeneratorPlugin>();
            
            foreach (var type in pluginTypes)
            {
                if (type.IsInterface || type.IsAbstract) continue;
                try
                {
                    var plugin = (IGeneratorPlugin)Activator.CreateInstance(type);
                    plugins.Add(plugin);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to load plugin {type.Name}: {ex.Message}");
                }
            }
            return plugins;
        }
    }
}
