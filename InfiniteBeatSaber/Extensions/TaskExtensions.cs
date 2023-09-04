using System;
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
    }
}
