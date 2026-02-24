namespace InsurancePlatform.Helpers
{
    public class ResponseWrapper<T>
    {
        public bool Success { get; set; }

        public string Message { get; set; }

        public T Data { get; set; }

        public static ResponseWrapper<T> SuccessResponse(T data, string message = null)
        {
            return new ResponseWrapper<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        public static ResponseWrapper<T> FailureResponse(string message)
        {
            return new ResponseWrapper<T>
            {
                Success = false,
                Message = message,
                Data = default
            };
        }

        public static ResponseWrapper<T> ErrorResponse(string message)
        {
            return FailureResponse(message);
        }
    }
}