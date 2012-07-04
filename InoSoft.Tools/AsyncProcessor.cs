using System;
using System.Collections.Generic;
using System.Threading;

namespace InoSoft.Tools
{
    public abstract class AsyncProcessor<T>
    {
        private Thread _dispatcherThread;
        private readonly EventWaitHandle _queueHasItemsEvent;
        private readonly Queue<T> _queue;
        private bool _isRunning;

        protected AsyncProcessor()
        {
            _queue = new Queue<T>();
            _queueHasItemsEvent = new EventWaitHandle(false, EventResetMode.ManualReset);
        }

        public void Start()
        {
            _isRunning = true;
            _dispatcherThread = new Thread(RunDispatcher) { IsBackground = true };
            _dispatcherThread.Start();
        }

        public void Stop()
        {
            _isRunning = false;
            _dispatcherThread.Join();
            lock (_queue)
            {
                _queue.Clear();
                _queueHasItemsEvent.Reset();
            }
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

        protected abstract void ProcessItem(T item);

        protected virtual void OnProcessItemException(T item, Exception ex)
        {
        }

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