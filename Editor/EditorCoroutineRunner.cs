using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AuraLiteWorldGenerator.Editor
{
    /// <summary>
    /// Minimal editor coroutine runner backed by EditorApplication.update.
    /// Supports nested coroutines (yield return another IEnumerator) so the Unity Editor
    /// stays responsive during long procedural generation.
    /// </summary>
    public static class EditorCoroutineRunner
    {
        private static readonly Stack<IEnumerator> _stack = new Stack<IEnumerator>();
        private static bool _isRunning;

        public static bool IsRunning => _isRunning;

        public static void Start(IEnumerator routine)
        {
            if (_isRunning)
                throw new InvalidOperationException("EditorCoroutineRunner is already running a coroutine.");
            _stack.Clear();
            _stack.Push(routine ?? throw new ArgumentNullException(nameof(routine)));
            _isRunning = true;
            EditorApplication.update += OnUpdate;
        }

        public static void Stop()
        {
            EditorApplication.update -= OnUpdate;
            _stack.Clear();
            _isRunning = false;
            EditorUtility.ClearProgressBar();
        }

        private static void OnUpdate()
        {
            if (_stack.Count == 0)
            {
                Stop();
                return;
            }

            IEnumerator current = _stack.Peek();
            bool advanced;
            try
            {
                advanced = current.MoveNext();
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning("AAA Rural World generation cancelled.");
                EditorUtility.DisplayDialog("Generation Cancelled", "The generation was cancelled by the user.", "OK");
                Stop();
                return;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("Generation Failed", ex.Message, "OK");
                Stop();
                return;
            }

            if (!advanced)
            {
                _stack.Pop();
                if (_stack.Count == 0)
                    Stop();
                return;
            }

            if (current.Current is IEnumerator nested)
            {
                _stack.Push(nested);
            }
        }
    }
}
