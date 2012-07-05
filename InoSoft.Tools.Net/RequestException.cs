using System;

namespace InoSoft.Tools.Net
{
    /// <summary>
    /// Defines exception, which can be thrown by contract method implementation and caught on the other side.
    /// Encapsulates error code, which can be custom-defined. It's useful to let remote caller know that his request is bad.
    /// </summary>
    public class RequestException : Exception
    {
        /// <summary>
        /// Creates RequestException.
        /// </summary>
        /// <param name="errorCode">Custom error code.</param>
        public RequestException(int errorCode)
        {
            ErrorCode = errorCode;
        }

        /// <summary>
        /// Creates RequestException.
        /// </summary>
        /// <param name="errorCode">Custom error code.</param>
        public RequestException(Enum errorCode)
        {
            ErrorCode = Convert.ToInt32(errorCode);
        }

        /// <summary>
        /// Gets error code.
        /// </summary>
        public int ErrorCode { get; private set; }
    }
}