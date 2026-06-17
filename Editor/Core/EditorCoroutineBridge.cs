using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;

namespace AuraLiteWorldGenerator.Editor.Core
{
    public static class EditorCoroutineBridge
    {
        /// <summary>
        /// Drives <paramref name="routine"/> through <see cref="EditorCoroutineRunner"/>
        /// and signals <paramref name="tcs"/> once the routine finishes (or fails /
        /// is cancelled).
        /// </summary>
        /// <remarks>
        /// Completion is signaled via <see cref="EditorApplication.delayCall"/> rather
        /// than from inside the coroutine body. This avoids a race with the
        /// <c>EditorCoroutine.OnUpdate</c> cleanup: the coroutine is logically done
        /// when its MoveNext returns false, but the runner only flips IsRunning to
        /// false from OnUpdate after the yield break has been processed. If we
        /// signaled the tcs synchronously here, an awaiter that resumed on the same
        /// EditorApplication.update tick could try to call EditorCoroutineRunner.Start
        /// while IsRunning was still true and get "EditorCoroutineRunner legacy
        /// instance is already running". delayCall runs after the current
        /// EditorApplication.update callback completes, guaranteeing Stop() has
        /// already fired.
        /// </remarks>
        public static void Run(IEnumerator routine, TaskCompletionSource<bool> tcs, CancellationToken ct)
        {
            EditorCoroutineRunner.Start(RunInternal(routine, tcs, ct));
        }

        private static IEnumerator RunInternal(IEnumerator routine, TaskCompletionSource<bool> tcs, CancellationToken ct)
        {
            Exception caught = null;
            bool cancelled = false;

            try
            {
                while (true)
                {
                    if (ct.IsCancellationRequested)
                    {
                        cancelled = true;
                        break;
                    }

                    bool hasNext;
                    try
                    {
                        hasNext = routine.MoveNext();
                    }
                    catch (Exception ex)
                    {
                        caught = ex;
                        break;
                    }

                    if (!hasNext)
                        break;

                    yield return routine.Current;
                }
            }
            finally
            {
                // Schedule completion signaling on delayCall so it runs AFTER
                // EditorCoroutine.OnUpdate has finished its current iteration
                // (and called Stop() on the runner). delayCall fires once, on the
                // next editor update tick.
                EditorApplication.delayCall += () =>
                {
                    if (caught != null)
                        tcs.TrySetException(caught);
                    else if (cancelled)
                        tcs.TrySetCanceled();
                    else
                        tcs.TrySetResult(true);
                };
            }
        }
    }
}
