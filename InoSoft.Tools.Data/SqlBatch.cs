using System.Threading;

namespace InoSoft.Tools.Data
{
    /// <summary>
    /// Encapsulates a batch of SQL queries that will be executed asynchronously.
    /// </summary>
    public class SqlBatch
    {
        private readonly EventWaitHandle _eventWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

        /// <summary>
        /// SQL queries in the batch.
        /// </summary>
        public SqlQuery[] Queries { get; set; }

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