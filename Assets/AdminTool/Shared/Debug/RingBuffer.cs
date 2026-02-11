using System.Collections.Generic;

namespace HumbleBeginnings.Debugging
{
    public class RingBuffer<T>
    {
        private readonly T[] _buffer;
        private int _index;
        private bool _wrapped;

        public RingBuffer(int capacity)
        {
            _buffer = new T[capacity];
        }

        public void Add(T item)
        {
            _buffer[_index++] = item;

            if (_index >= _buffer.Length)
            {
                _index = 0;
                _wrapped = true;
            }
        }

        public IReadOnlyList<T> Snapshot()
        {
            var list = new List<T>(_buffer.Length);

            if (!_wrapped)
            {
                for (int i = 0; i < _index; i++)
                    list.Add(_buffer[i]);
            }
            else
            {
                for (int i = _index; i < _buffer.Length; i++)
                    list.Add(_buffer[i]);
                for (int i = 0; i < _index; i++)
                    list.Add(_buffer[i]);
            }

            return list;
        }
    }
}
