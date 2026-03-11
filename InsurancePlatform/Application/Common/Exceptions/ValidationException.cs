using System;
using System.Collections.Generic;

namespace Application.Common.Exceptions
{
    public class ValidationException : Exception
    {
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
