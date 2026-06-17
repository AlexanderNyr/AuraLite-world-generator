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

        /// <summary>
        /// Starts an EditorCoroutine, replacing any previous (already-stopped) instance.
        /// Throws if a coroutine is genuinely still running.
        /// </summary>
        /// <remarks>
        /// RunInternal signals the task completion source (tcs.SetResult / SetCanceled /
        /// SetException) from inside the coroutine body, but EditorCoroutine.Stop() is
        /// only called from OnUpdate after the iterator's MoveNext returns false. That
        /// creates a transient window where the previous coroutine has logically
        /// completed (tcs.Task is done) but _legacyCoroutine.IsRunning is still true
        /// until OnUpdate processes the final yield break. If the awaiter resumes
        /// during that window and calls Start() again, the IsRunning guard would
        /// trip and throw "EditorCoroutineRunner legacy instance is already running".
        /// To make this resilient we proactively drop the stale reference whenever it
        /// is not actively running, so the next Start() call always gets a clean slate.
        /// </remarks>
        public static void Start(IEnumerator routine)
        {
            // Defensive cleanup: if the previous coroutine has already stopped
            // (IsRunning == false), drop the reference. This is the common case
            // after a coroutine completes, so we always take this branch.
            if (_legacyCoroutine != null && !_legacyCoroutine.IsRunning)
            {
                _legacyCoroutine = null;
            }

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

        /// <summary>
        /// Emergency recovery: drops any stale legacy reference without touching
        /// EditorApplication.update subscriptions. Useful if the static state
        /// somehow got out of sync with reality (e.g., after a domain reload or
        /// if an editor crash left the runner in a stuck state).
        /// </summary>
        public static void Reset()
        {
            _legacyCoroutine = null;
        }

        public static EditorCoroutine StartNew(IEnumerator routine)
        {
            var coroutine = new EditorCoroutine(routine);
            coroutine.Start();
            return coroutine;
        }
    }
}
