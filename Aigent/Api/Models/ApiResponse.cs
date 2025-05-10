namespace Aigent.Api.Models
{
    /// <summary>
    /// Generic API response model
    /// </summary>
    /// <typeparam name="T">Type of the data</typeparam>
    public class ApiResponse<T>
    {
        /// <summary>
        /// Whether the request was successful
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// Message describing the result
        /// </summary>
        public string Message { get; set; }
        
        /// <summary>
        /// Data returned by the API
        /// </summary>
        public T Data { get; set; }

        /// <summary>
        /// Creates a successful response
        /// </summary>
        /// <param name="data">Data to return</param>
        /// <param name="message">Optional message</param>
        /// <returns>A successful response</returns>
        public static ApiResponse<T> Ok(T data, string message = "Success")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        /// <summary>
        /// Creates an error response
        /// </summary>
        /// <param name="message">Error message</param>
        /// <returns>An error response</returns>
        public static ApiResponse<T> Error(string message)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Data = default
            };
        }
    }
}
