using System;
using System.Collections.Generic;
using System.Threading;

namespace InoSoft.Tools
{
    public abstract class AsyncProcessor<T>
    {
        private Thread _dispatcherThread;
        private EventWaitHandle _queueHasItemsEvent;
        private Queue<T> _queue;

        protected AsyncProcessor()
        {
            _queue = new Queue<T>();
            _queueHasItemsEvent = new EventWaitHandle(false, EventResetMode.ManualReset);
            _dispatcherThread = new Thread(DispatcherThread) { IsBackground = true };
        }

        public void Start()
        {
            _dispatcherThread.Start();
        }

        public void EnqueueItem(T item)
        {
            lock (_queue)
            {
                _queue.Enqueue(item);
                _queueHasItemsEvent.Set();
            }
        }

        protected abstract void ProcessItem(T item);

        protected virtual void OnProcessItemException(T item, Exception ex)
        {
        }

        private void DispatcherThread()
        {
            while (true)
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