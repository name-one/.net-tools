using System.Collections;
using System.Collections.Generic;

namespace InoSoft.Tools
{
    /// <summary>
    /// Represents a first-in, first-out collection of objects with limited capacity
    /// that drops first elements when the limit is exceeded.
    /// </summary>
    /// <typeparam name="T">Specifies the type of elements in the queue.</typeparam>
    public class CacheQueue<T> : IEnumerable<T>
    {
        private readonly int _capacity;
        private readonly Queue<T> _queue;

        /// <summary>
        /// Creates an instance of <see cref="CacheQueue{T}"/>.
        /// </summary>
        /// <param name="capacity">
        /// Maximum number of elements that the <see cref="CacheQueue{T}"/> can contain.
        /// </param>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// <paramref name="capacity"/> is less than zero.
        /// </exception>
        public CacheQueue(int capacity)
        {
            _capacity = capacity;
            _queue = new Queue<T>(_capacity);
        }

        /// <summary>
        /// Gets the maximum number of elements that the <see cref="CacheQueue{T}"/> can contain.
        /// </summary>
        public int Capacity
        {
            get { return _capacity; }
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="CacheQueue{T}"/>.
        /// </summary>
        public int Count
        {
            get { return _queue.Count; }
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _queue.GetEnumerator();
        }

        /// <summary>
        /// Removes and returns the object at the beginning of the <see cref="CacheQueue{T}"/>.
        /// </summary>
        /// <returns>
        /// The object that is removed from the beginning of the <see cref="CacheQueue{T}"/>.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">The <see cref="CacheQueue{T}"/> is empty.</exception>
        public T Dequeue()
        {
            return _queue.Dequeue();
        }

        /// <summary>
        /// Removes and returns the specified number of objects at the beginning of the <see cref="CacheQueue{T}"/>.
        /// </summary>
        /// <param name="count">Number of objects to dequeue from the <see cref="CacheQueue{T}"/>.</param>
        /// <returns>
        /// An array of objects dequeued from the <see cref="CacheQueue{T}"/>. If there are less objects
        /// than <paramref name="count"/>, all the objects are dequeued and returned.
        /// </returns>
        public T[] DequeueRange(int count)
        {
            return _queue.DequeueRange(count);
        }

        /// <summary>
        /// Adds an object to the end of the <see cref="CacheQueue{T}"/>.
        /// </summary>
        /// <param name="item">
        /// The object to add to the <see cref="CacheQueue{T}"/>. The value can be null for reference types.
        /// </param>
        public void Enqueue(T item)
        {
            if (_queue.Count == _capacity)
            {
                _queue.Dequeue();
            }
            _queue.Enqueue(item);
        }

        /// <summary>
        /// Adds a collection of objects to the end of the <see cref="CacheQueue{T}"/>.
        /// </summary>
        /// <param name="collection">Objects to add to the <see cref="CacheQueue{T}"/>.</param>
        public void EnqueueRange(IEnumerable<T> collection)
        {
            foreach (T item in collection)
            {
                Enqueue(item);
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<T> GetEnumerator()
        {
            return _queue.GetEnumerator();
        }

        /// <summary>
        /// Returns the object at the beginning of the <see cref="CacheQueue{T}"/> without removing it.
        /// </summary>
        /// <returns>
        /// The object at the beginning of the <see cref="CacheQueue{T}"/>.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">The <see cref="CacheQueue{T}"/> is empty.</exception>
        public T Peek()
        {
            return _queue.Peek();
        }

        /// <summary>
        /// Returns the specified number of objects from the head of the <see cref="CacheQueue{T}"/>
        /// without removing them.
        /// </summary>
        /// <param name="count">Number of objects to peek at.</param>
        /// <returns>
        /// An array of objects from the head of the <see cref="CacheQueue{T}"/>. If there are less objects
        /// than <paramref name="count"/>, all the objects are returned.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">The <see cref="CacheQueue{T}"/> is empty.</exception>
        public T[] PeekRange(int count)
        {
            return _queue.PeekRange(count);
        }

        /// <summary>
        /// Copies the <see cref="CacheQueue{T}"/> elements to a new array.
        /// </summary>
        /// <returns>
        /// A new array containing elements copied from the <see cref="CacheQueue{T}"/>.
        /// </returns>
        public T[] ToArray()
        {
            return _queue.ToArray();
        }
    }
}