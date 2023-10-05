using System.Collections.Generic;
using System.Threading.Tasks;

namespace InfiniteBeatSaber
{
    /// <summary>
    /// Like `Queue` but provides `HasItemsAsync` which enables you to be
    /// notified when an empty `Queue` gets an item added to it.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class AsyncQueue<T>
    {
        private readonly Queue<T> _queue = new Queue<T>();
        private TaskCompletionSource<object> _tcs = new TaskCompletionSource<object>();

        public void Enqueue(T item)
        {
            _queue.Enqueue(item);
            TryNotifyHasItems();
        }

        public void EnqueueAll(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                _queue.Enqueue(item);
            }

            TryNotifyHasItems();
        }

        public T Peek() => _queue.Peek();
        public T Dequeue() => _queue.Dequeue();
        public bool HasItems() => _queue.Count > 0;

        public Task HasItemsAsync()
        {
            if (_queue.Count == 0)
            {
                if (_tcs == null)
                {
                    _tcs = new TaskCompletionSource<object>();
                }
                return _tcs.Task;
            }
            else
            {
                return Task.CompletedTask;
            }
        }

        private void TryNotifyHasItems()
        {
            if (_tcs != null)
            {
                var tcs = _tcs;
                _tcs = null;
                tcs.SetResult(null);
            }
        }
    }
}
