using System;
using System.Collections.Generic;
using System.Threading;

namespace InoSoft.Tools
{
    /// <summary>
    /// Provides a mechanism for asynchronous processing of <see cref="T"/> objects in a queued manner.
    /// </summary>
    /// <typeparam name="T">Specifies the type of the items that are processed.</typeparam>
    public abstract class AsyncProcessor<T>
    {
        private readonly bool _isBackground;
        private readonly object _processItemLock = new object();
        private readonly Queue<T> _queue;
        private readonly EventWaitHandle _queueHasItemsEvent;
        private Thread _dispatcherThread;
        private bool _isRunning;

        /// <summary>
        /// Creates an instance of <see cref="AsyncProcessor{T}"/>.
        /// </summary>
        /// <param name="isBackground">
        /// Specifies whether the <see cref="AsyncProcessor{T}"/> instance will be executed
        /// in a background thread and Stop() will not block.
        /// Default is <c>true</c>.
        /// </param>
        protected AsyncProcessor(bool isBackground = true)
        {
            _isBackground = isBackground;
            _queue = new Queue<T>();
            _queueHasItemsEvent = new EventWaitHandle(false, EventResetMode.ManualReset);
        }

        /// <summary>
        /// Gets a value indicating whether this instance is running.
        /// </summary>
        public bool IsRunning
        {
            get { return _isRunning; }
        }

        /// <summary>
        /// Enqueues a <see cref="T"/> item to be processed.
        /// </summary>
        /// <param name="item">Item to enqueue.</param>
        public void EnqueueItem(T item)
        {
            lock (_queue)
            {
                if (_isRunning)
                {
                    _queue.Enqueue(item);
                    _queueHasItemsEvent.Set();
                }
            }
        }

        /// <summary>
        /// Starts this instance if it is not already running.
        /// </summary>
        public void Start()
        {
            if (_isRunning)
                return;

            _isRunning = true;
            _dispatcherThread = new Thread(RunDispatcher) { IsBackground = _isBackground };
            _dispatcherThread.Start();

            OnStart();
        }

        /// <summary>
        /// Stops this instance if it is currently running.
        /// </summary>
        public void Stop()
        {
            if (!_isRunning)
                return;

            _isRunning = false;
            lock (_queue)
            {
                _queue.Clear();
                _queueHasItemsEvent.Set();
            }
            if (!_isBackground)
            {
                _dispatcherThread.Join();
            }

            OnStop();
        }

        /// <summary>
        /// Performs user-defined actions when an unhandled exception occurs in the <see cref="ProcessItem"/> method.
        /// </summary>
        /// <param name="item">The item that was being processed when the exception occured.</param>
        /// <param name="ex">Exception that was not handled during the item processing.</param>
        protected virtual void OnProcessItemException(T item, Exception ex)
        {
        }

        /// <summary>
        /// Performs user-defined actions after the instance starts.
        /// </summary>
        protected virtual void OnStart()
        {
        }

        /// <summary>
        /// Performs user-defined actions after the instance stops.
        /// </summary>
        protected virtual void OnStop()
        {
        }

        /// <summary>
        /// Executes user-defined actions to process an item from the item queue.
        /// </summary>
        /// <param name="item">Item to process.</param>
        protected abstract void ProcessItem(T item);

        /// <summary>
        /// Runs the dispatcher thread.
        /// </summary>
        private void RunDispatcher()
        {
            while (_isRunning)
            {
                _queueHasItemsEvent.WaitOne();
                T item = default(T);
                bool hasItem = false;
                lock (_queue)
                {
                    if (_queue.Count > 0)
                    {
                        // If there are items in the queue, get the first of them.
                        item = _queue.Dequeue();
                        hasItem = true;
                    }
                    else
                    {
                        // If there are no items, wait until one is added.
                        _queueHasItemsEvent.Reset();
                    }
                }

                lock (_processItemLock)
                {
                    // If the queue did not yield any items initially, and an item was added afterwards,
                    // continue and get the added item.
                    if (!hasItem)
                        continue;

                    try
                    {
                        // Process the item.
                        ProcessItem(item);
                    }
                    catch (Exception ex)
                    {
                        // Perform user-defined actions for unhandled exception.
                        OnProcessItemException(item, ex);
                    }
                }
            }
        }
    }
}