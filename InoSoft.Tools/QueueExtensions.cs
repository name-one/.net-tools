﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace InoSoft.Tools
{
    /// <summary>
    /// Provides extension methods for collection-based operations upon <see cref="Queue{T}"/>.
    /// </summary>
    public static class QueueExtensions
    {
        /// <summary>
        /// Removes and returns the specified number of objects at the beginning of the <see cref="Queue{T}"/>.
        /// </summary>
        /// <typeparam name="T">Type of objects contained in the <see cref="Queue{T}"/>.</typeparam>
        /// <param name="queue"><see cref="Queue{T}"/> to dequeue objects from.</param>
        /// <param name="count">Number of objects to dequeue from the <see cref="Queue{T}"/>.</param>
        /// <returns>
        /// An array of objects dequeued from the <see cref="Queue{T}"/>. If there are less objects
        /// than <paramref name="count"/>, all the objects are dequeued and returned.
        /// </returns>
        public static T[] DequeueRange<T>(this Queue<T> queue, int count)
        {
            count = Math.Min(count, queue.Count);
            var result = new T[count];
            for (int i = 0; i < count; ++i)
            {
                result[i] = queue.Dequeue();
            }
            return result;
        }

        /// <summary>
        /// Adds a collection of objects to the end of the <see cref="Queue{T}"/>.
        /// </summary>
        /// <typeparam name="T">Type of objects contained in the <see cref="Queue{T}"/>.</typeparam>
        /// <param name="queue"><see cref="Queue{T}"/> to add objects to.</param>
        /// <param name="collection">Objects to add to the <see cref="Queue{T}"/>.</param>
        public static void EnqueueRange<T>(this Queue<T> queue, IEnumerable<T> collection)
        {
            foreach (T item in collection)
            {
                queue.Enqueue(item);
            }
        }

        /// <summary>
        /// Returns the specified number of objects from the head of the <see cref="Queue{T}"/>
        /// without removing them.
        /// </summary>
        /// <typeparam name="T">Type of objects contained in the <see cref="Queue{T}"/>.</typeparam>
        /// <param name="queue"><see cref="Queue{T}"/> to dequeue objects from.</param>
        /// <param name="count">Number of objects to peek at.</param>
        /// <returns>
        /// An array of objects from the head of the <see cref="Queue{T}"/>. If there are less objects
        /// than <paramref name="count"/>, all the objects are returned.
        /// </returns>
        public static T[] PeekRange<T>(this Queue<T> queue, int count)
        {
            return queue.Take(count).ToArray();
        }
    }
}