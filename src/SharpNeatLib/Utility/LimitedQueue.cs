using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpNeat.Utility
{
    /// <summary>
    /// A queue limited to a given size. If the limit is exceeded, the oldest item in queueue is automatically dequeueueued
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class LimitedQueue<T> : IEnumerable<T>
    {
        private readonly Queue<T> _queue = new Queue<T>();
        
        public int Limit { get; set; }

        public int Count => _queue.Count;

        public LimitedQueue(int limit)
        {
            Limit = limit;
        }

        public void Enqueue(T item)
        {
            _queue.Enqueue(item);

            if (Limit > 0 && _queue.Count > Limit)
            {
                _queue.Dequeue();
            }
        }

        public T Dequeue()
        {
            return _queue.Dequeue();
        }

        public List<T> ToList()
        {
            return _queue.ToList();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _queue.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
