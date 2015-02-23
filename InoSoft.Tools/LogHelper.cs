using System;
using System.IO;

namespace InoSoft.Tools
{
    public static class LogHelper
    {
        /// <summary>
        ///   Logs an exception to a text writer.
        /// </summary>
        /// <param name="textWriter">Text writer to log the error to.</param>
        /// <param name="ex">The exception to log.</param>
        /// <param name="isVerbose">If set to <c>true</c>, logs stack trace and other details of the exception.</param>
        /// <example>
        ///   <code>
        ///   try
        ///   {
        ///       // Code that may throw an exception, e.g.:
        ///       throw new Exception();
        ///   }
        ///   catch (Exception ex)
        ///   {
        ///       Console.Error.LogError(exception, false);
        ///   }
        ///   </code>
        /// </example>
        public static void LogError(this TextWriter textWriter, Exception ex, bool isVerbose)
        {
            if (isVerbose)
            {
                textWriter.WriteLine(ex);
            }
            else
            {
                textWriter.WriteLine(ex.Message);
                var aggregate = ex as AggregateException;
                if (aggregate != null)
                {
                    for (int i = 0; i < aggregate.InnerExceptions.Count; i++)
                    {
                        textWriter.Write("{0}. ", i + 1);
                        textWriter.LogError(aggregate.InnerExceptions[i], false);
                    }
                }
                else if (ex.InnerException != null)
                {
                    textWriter.LogError(ex.InnerException, false);
                }
            }
        }
    }
}