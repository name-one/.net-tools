using System;
using System.Collections.Generic;
using System.Threading;

namespace InoSoft.Tools
{
    public abstract class AsyncProcessor<T>
    {
        private readonly bool _isBackground;
        private readonly object _processItemLock = new object();
        private readonly Queue<T> _queue;
        private readonly EventWaitHandle _queueHasItemsEvent;
        private Thread _dispatcherThread;
        private bool _isRunning;

        /// <summary>
        /// Creates AsyncProcessor instance.
        /// </summary>
        /// <param name="isBackground">
        /// Specifies whether the AsyncProcessor instance will be executed in a background thread and Stop() will not block.
        /// Default is true.
        /// </param>
        protected AsyncProcessor(bool isBackground = true)
        {
            _isBackground = isBackground;
            _queue = new Queue<T>();
            _queueHasItemsEvent = new EventWaitHandle(false, EventResetMode.ManualReset);
        }

        public bool IsRunning
        {
            get { return _isRunning; }
        }

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

        public void Start()
        {
            _isRunning = true;
            _dispatcherThread = new Thread(RunDispatcher) { IsBackground = _isBackground };
            _dispatcherThread.Start();

            OnStart();
        }

        public void Stop()
        {
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

        protected virtual void OnProcessItemException(T item, Exception ex)
        {
        }

        protected virtual void OnStart()
        {
        }

        protected virtual void OnStop()
        {
        }

        protected abstract void ProcessItem(T item);

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
                        item = _queue.Dequeue();
                        hasItem = true;
                    }
                    else
                    {
                        _queueHasItemsEvent.Reset();
                    }
                }

                lock (_processItemLock)
                {
                    if (hasItem)
                    {
                        try
                        {
                            ProcessItem(item);
                        }
                        catch (Exception ex)
                        {
                            OnProcessItemException(item, ex);
                        }
                    }
                }
            }
        }
    }
}