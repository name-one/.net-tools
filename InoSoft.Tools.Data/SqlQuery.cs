using System;
using System.Threading;

namespace InoSoft.Tools.Data
{
    /// <summary>
    /// Encapsulates SQL query, which will be executed asynchronously
    /// </summary>
    public class SqlQuery
    {
        private EventWaitHandle _eventWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

        /// <summary>
        /// Type of query result elements or null no need to return anything.
        /// </summary>
        public Type ElementType { get; set; }

        /// <summary>
        /// Query string representation.
        /// </summary>
        public string Sql { get; set; }

        /// <summary>
        /// Input parameters of query.
        /// </summary>
        public object[] Parameters { get; set; }

        /// <summary>
        /// Result of execution will be set there.
        /// </summary>
        public Array Result { get; set; }

        /// <summary>
        /// If exception will occur during execution, it will be set here.
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// Signals waiting thread, which called WaitSignal(), to continue.
        /// </summary>
        public void Signal()
        {
            _eventWaitHandle.Set();
        }

        /// <summary>
        /// Causes calling thread to block and wait other thread to call Signal().
        /// </summary>
        public void WaitSignal()
        {
            _eventWaitHandle.WaitOne();
        }
    }
}