using System;

namespace Application.Common.Exceptions
{
    /// <summary>
    /// This exception is used when the user sends data that the server cannot process.
    /// Think of it like a "User Error" or "Invalid Input" error.
    /// </summary>
    public class BadRequestException : Exception
    {
        public BadRequestException() : base() { }
        public BadRequestException(string message) : base(message) { }
        public BadRequestException(string message, Exception innerException) : base(message, innerException) { }
    }
}
