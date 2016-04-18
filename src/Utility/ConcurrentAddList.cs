using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ENTM.Utility
{
    class ConcurrentAddList<T> : IEnumerable<T>
    {
        private IList<T> _list = new List<T>();

        public IList<T> List => _list;

        private readonly object _lock = new object();

        public void Add(T t)
        {
            lock (_lock)
            {
                _list.Add(t);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
