using System;
using System.Collections.Generic;

namespace Application.Common.Exceptions
{
    /// <summary>
    /// This exception is used when data fails our business rules or format checks.
    /// It can hold a list of multiple errors (e.g., "Email is wrong" AND "Age must be over 18").
    /// </summary>
    public class ValidationException : Exception
    {
        // A list to store multiple validation error messages
        public List<string> Errors { get; }

        public ValidationException() : base("One or more validation failures have occurred.")
        {
            Errors = new List<string>();
        }

        public ValidationException(List<string> errors) : base("One or more validation failures have occurred.")
        {
            Errors = errors;
        }

        public ValidationException(string message) : base(message)
        {
            Errors = new List<string>();
        }
    }
}
