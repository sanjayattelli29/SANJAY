using System;

namespace Application.Common.Exceptions
{
    /// <summary>
    /// This exception is used when the system cannot find a specific item.
    /// For example, if a user looks for a policy that doesn't exist.
    /// </summary>
    public class NotFoundException : Exception
    {
        public NotFoundException() : base() { }
        public NotFoundException(string message) : base(message) { }
        public NotFoundException(string message, Exception innerException) : base(message, innerException) { }
    }
}
