using System;
using System.Diagnostics;
using System.Threading;

namespace InoSoft.Tools
{
    /// <summary>
    /// Helper class for maintaining minimum iteration time.
    /// </summary>
    public class IterationWaiter
    {
        private Stopwatch _stopwatch;
        private readonly int _iterationTime;

        /// <summary>
        /// Gets desired minimum iteration time in milliseconds.
        /// </summary>
        public int IterationTime
        {
            get { return _iterationTime; }
        }

        /// <summary>
        /// Gets the time elapsed since the iteration was started.
        /// </summary>
        public TimeSpan Elapsed
        {
            get { return _stopwatch.Elapsed; }
        }

        /// <summary>
        /// Creates IterationWaiter with specified iteration time.
        /// </summary>
        /// <param name="iterationTime">Desired minimum iteration time in milliseconds.</param>
        public IterationWaiter(int iterationTime)
        {
            _iterationTime = iterationTime;
        }

        /// <summary>
        /// Starts the iteration timer.
        /// </summary>
        public void Start()
        {
            _stopwatch = Stopwatch.StartNew();
        }

        /// <summary>
        /// Sleeps until minimum iteration time has elapsed. If it has already elapsed, does nothing.
        /// </summary>
        public void Wait()
        {
            if (_stopwatch == null)
                throw new InvalidOperationException("Iteration must be started before calling Wait().");

            var timeout = (int)(IterationTime - _stopwatch.ElapsedMilliseconds);
            if (timeout > 0)
            {
                Thread.Sleep(timeout);
            }
        }

        /// <summary>
        /// Creates and starts a new IterationWaiter with specified iteration time.
        /// Equivalent to creating an instance using constructor and calling Start().
        /// </summary>
        /// <param name="iterationTime">Desired minimum iteration time in milliseconds.</param>
        /// <returns>Created IterationWaiter instance.</returns>
        public static IterationWaiter StartNew(int iterationTime)
        {
            var waiter = new IterationWaiter(iterationTime);
            waiter.Start();
            return waiter;
        }
    }
}