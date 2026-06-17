using System;
using System.Collections.Generic;
using UnityEngine;

namespace AuraLiteWorldGenerator.Editor.Core.Logging
{
    public interface ILogger
    {
        void Info(string message, UnityEngine.Object context = null);
        void Warning(string message, UnityEngine.Object context = null);
        void Error(string message, Exception ex = null, UnityEngine.Object context = null);
    }

    public class UnityLogger : ILogger
    {
        public void Info(string message, UnityEngine.Object context = null) => Debug.Log($"[AuraLite] {message}", context);
        public void Warning(string message, UnityEngine.Object context = null) => Debug.LogWarning($"[AuraLite] {message}", context);
        public void Error(string message, Exception ex = null, UnityEngine.Object context = null)
        {
            if (ex != null) Debug.LogError($"[AuraLite] {message}\n{ex}", context);
            else Debug.LogError($"[AuraLite] {message}", context);
        }
    }

    [Serializable]
    public class GenerationReport
    {
        public string Seed;
        public string Timestamp;
        public float TotalTimeSeconds;
        public List<ModuleReport> Modules = new List<ModuleReport>();
    }

    [Serializable]
    public class ModuleReport
    {
        public string Id;
        public string Status;
        public float DurationSeconds;
        public List<string> Warnings = new List<string>();
        public List<string> Errors = new List<string>();
    }
}
