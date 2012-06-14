using System;

namespace InoSoft.Tools.Net
{
    public class RequestException : Exception
    {
        public RequestException(int errorCode)
        {
            ErrorCode = errorCode;
        }

        public RequestException(Enum errorCode)
        {
            ErrorCode = Convert.ToInt32(errorCode);
        }

        public int ErrorCode { get; private set; }
    }
}