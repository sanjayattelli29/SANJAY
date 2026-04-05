namespace Application.Common.Models
{
    /// <summary>
    /// This is the standard format for all responses sent back from our API.
    /// It helps the frontend know if a request worked and gives them the data or error messages.
    /// </summary>
    /// <typeparam name="T">The type of data being returned (like a Policy or a User)</typeparam>
    public class ApiResponse<T>
    {
        // True if everything went well, False if there was an error
        public bool Success { get; set; }
        
        // A short message explaining what happened (e.g., "Login successful")
        public string Message { get; set; } = string.Empty;
        
        // If something went wrong, this list contains the specific error details
        public List<string> Errors { get; set; } = new List<string>();
        
        // The actual data requested by the user
        public T? Data { get; set; }

        /// <summary>
        /// A quick way to create a 'Success' response.
        /// </summary>
        public static ApiResponse<T> SuccessResponse(T data, string message = "Success")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        /// <summary>
        /// A quick way to create a 'Failure' response with an error message.
        /// </summary>
        public static ApiResponse<T> FailureResponse(string message, List<string>? errors = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Errors = errors ?? new List<string>()
            };
        }
    }
}
