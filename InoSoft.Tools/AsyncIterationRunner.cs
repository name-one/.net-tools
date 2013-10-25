using System;
using System.Threading;

namespace InoSoft.Tools
{
    /// <summary>
    /// Provides a mechanism for asynchronous periodical execution of some work.
    /// </summary>
    public abstract class AsyncIterationRunner
    {
        private readonly int _iterationTime;
        private bool _isRunning;
        private Thread _thread;

        /// <summary>
        /// Creates an instance of <see cref="AsyncIterationRunner"/> with the specified minimum iteration time.
        /// </summary>
        /// <param name="iterationTime">Minimum time between the start of each iteration in milliseconds.</param>
        protected AsyncIterationRunner(int iterationTime)
        {
            _iterationTime = iterationTime;
        }

        /// <summary>
        /// Gets a value indicating whether this instance is currently running.
        /// </summary>
        public bool IsRunning
        {
            get { return _isRunning; }
        }

        /// <summary>
        /// Gets the minimum time between the start of each iteration in milliseconds.
        /// </summary>
        public int IterationTime
        {
            get { return _iterationTime; }
        }

        /// <summary>
        /// Starts this instance if it is not already running.
        /// </summary>
        public void Start()
        {
            if (_isRunning)
                return;

            if (_thread != null)
            {
                _thread.Abort();
            }

            _isRunning = true;
            _thread = new Thread(Execute) { IsBackground = true };
            _thread.Start();
        }

        /// <summary>
        /// Stops this instance if it is currently running.
        /// </summary>
        public void Stop()
        {
            _isRunning = false;
        }

        /// <summary>
        /// Performs user-defined actions when an unhandled exception occurs in the <see cref="RunIteration"/> method.
        /// </summary>
        /// <param name="ex">Exception that was not handled during the iteration execution.</param>
        protected virtual void OnRunIterationException(Exception ex)
        {
        }

        /// <summary>
        /// Performs the work required every iteration.
        /// </summary>
        protected abstract void RunIteration();

        /// <summary>
        /// Executes the <see cref="RunIteration"/> method periodically until stopped.
        /// The minimum time between the executions is <see cref="IterationTime"/>.
        /// </summary>
        private void Execute()
        {
            var waiter = new IterationWaiter(_iterationTime);
            while (_isRunning)
            {
                waiter.Start();

                try
                {
                    RunIteration();
                }
                catch (Exception ex)
                {
                    OnRunIterationException(ex);
                }

                waiter.Wait();
            }
        }
    }
}