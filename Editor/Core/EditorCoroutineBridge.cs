using System.Collections;
using System.Threading;
using System.Threading.Tasks;

namespace AuraLiteWorldGenerator.Editor.Core
{
    public static class EditorCoroutineBridge
    {
        public static void Run(IEnumerator routine, TaskCompletionSource<bool> tcs, CancellationToken ct)
        {
            EditorCoroutineRunner.Start(RunInternal(routine, tcs, ct));
        }

        private static IEnumerator RunInternal(IEnumerator routine, TaskCompletionSource<bool> tcs, CancellationToken ct)
        {
            while (true)
            {
                if (ct.IsCancellationRequested)
                {
                    tcs.SetCanceled();
                    yield break;
                }

                bool hasNext;
                try
                {
                    hasNext = routine.MoveNext();
                }
                catch (System.Exception ex)
                {
                    tcs.SetException(ex);
                    yield break;
                }

                if (!hasNext)
                {
                    tcs.SetResult(true);
                    yield break;
                }

                yield return routine.Current;
            }
        }
    }
}
