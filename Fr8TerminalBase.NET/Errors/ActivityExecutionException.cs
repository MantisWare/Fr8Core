using System;
using Fr8.Infrastructure.Data.Constants;

namespace Fr8.TerminalBase.Errors
{
    public class ActivityExecutionException : Exception
    {
        public ActivityErrorCode? ErrorCode
        {
            get;
        }

        public ActivityExecutionException(string message, ActivityErrorCode? errorCode = null)
            : base(message)
        {
            ErrorCode = errorCode;
        }

        public ActivityExecutionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public ActivityExecutionException()
        {
        }
    }
}