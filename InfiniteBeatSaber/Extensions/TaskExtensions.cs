using System;
using System.Threading;
using System.Threading.Tasks;

namespace InfiniteBeatSaber.Extensions
{
    internal static class TaskExtensions
    {
        public static void LogOnFailure(this Task task)
        {
            if (task == null) throw new ArgumentNullException(nameof(task));

            task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    var aggregateException = t.Exception;
                    foreach (var exception in aggregateException.InnerExceptions)
                    {
                        Plugin.Log.Error(exception);
                    }
                }
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        public static async Task Cancelable(this Task task, CancellationToken cancellationToken)
        {
            var cancellationTask = Task.Delay(-1, cancellationToken);
            var result = await Task.WhenAny(task, cancellationTask);
            if (result == task)
            {
                await task;
            }
            else
            {
                throw new OperationCanceledException(cancellationToken);
            }
        }
    }
}
