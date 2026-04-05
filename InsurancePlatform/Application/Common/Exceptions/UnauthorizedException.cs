using System;

namespace Application.Common.Exceptions
{
    /// <summary>
    /// This exception is used when a user tries to access something they don't have permission for.
    /// It's like trying to enter a locked room without a key.
    /// </summary>
    public class UnauthorizedException : Exception
    {
        public UnauthorizedException() : base() { }
        public UnauthorizedException(string message) : base(message) { }
        public UnauthorizedException(string message, Exception innerException) : base(message, innerException) { }
    }
}
