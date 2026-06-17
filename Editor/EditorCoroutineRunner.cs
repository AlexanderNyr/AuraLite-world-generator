using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AuraLiteWorldGenerator.Editor
{
    public class EditorCoroutine
    {
        private Stack<IEnumerator> _stack = new Stack<IEnumerator>();
        public bool IsRunning { get; private set; }

        public EditorCoroutine(IEnumerator routine)
        {
            if (routine == null) throw new ArgumentNullException(nameof(routine));
            _stack.Push(routine);
        }

        public void Start()
        {
            IsRunning = true;
            EditorApplication.update += OnUpdate;
        }

        public void Stop()
        {
            EditorApplication.update -= OnUpdate;
            _stack.Clear();
            IsRunning = false;
        }

        private void OnUpdate()
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

    public static class EditorCoroutineRunner
    {
        private static EditorCoroutine _legacyCoroutine;

        public static bool IsRunning => _legacyCoroutine != null && _legacyCoroutine.IsRunning;

        public static void Start(IEnumerator routine)
        {
            if (IsRunning)
                throw new InvalidOperationException("EditorCoroutineRunner legacy instance is already running.");
            _legacyCoroutine = new EditorCoroutine(routine);
            _legacyCoroutine.Start();
        }

        public static void Stop()
        {
            if (_legacyCoroutine != null)
            {
                _legacyCoroutine.Stop();
                _legacyCoroutine = null;
            }
            EditorUtility.ClearProgressBar();
        }
        
        public static EditorCoroutine StartNew(IEnumerator routine)
        {
            var coroutine = new EditorCoroutine(routine);
            coroutine.Start();
            return coroutine;
        }
    }
}
