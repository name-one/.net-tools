using System.Threading;

namespace InoSoft.Tools
{
    /// <summary>
    /// Base class for asynchronous execution of some work with a minimum iteration time.
    /// </summary>
    public abstract class AsyncIterationRunner
    {
        private readonly int _iterationTime;
        private Thread _thread;

        /// <summary>
        /// Creates an instance of <see cref="AsyncIterationRunner"/> with the specified minimum iteration time.
        /// </summary>
        /// <param name="iterationTime">Minimum iteration time in milliseconds.</param>
        protected AsyncIterationRunner(int iterationTime)
        {
            _iterationTime = iterationTime;
        }

        private bool _isRunning;

        /// <summary>
        /// True if <see cref="AsyncIterationRunner"/> is currently running.
        /// </summary>
        public bool IsRunning
        {
            get { return _isRunning; }
        }

        /// <summary>
        /// Minimum iteration time in milliseconds.
        /// </summary>
        public int IterationTime
        {
            get { return _iterationTime; }
        }

        /// <summary>
        /// Starts <see cref="AsyncIterationRunner"/> if it is not already running.
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
        /// Stops <see cref="AsyncIterationRunner"/> if it is currently running.
        /// </summary>
        public void Stop()
        {
            _isRunning = false;
        }

        /// <summary>
        /// Performs the work required every iteration.
        /// </summary>
        protected abstract void RunIteration();

        /// <summary>
        /// Runs the <see cref="RunIteration"/> method until stopped.
        /// </summary>
        private void Execute()
        {
            var waiter = new IterationWaiter(_iterationTime);
            while (_isRunning)
            {
                waiter.Start();

                RunIteration();

                waiter.Wait();
            }
        }
    }
}