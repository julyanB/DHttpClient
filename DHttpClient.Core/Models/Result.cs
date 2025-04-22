using System.Net; // For HttpStatusCode

namespace DHttpClient.Models
{
    /// <summary>
    /// Represents the result of an HTTP operation, encapsulating success status, data, error message, and status code.
    /// </summary>
    /// <typeparam name="T">The type of the data payload (e.g., HttpResponseMessage, string, custom object, Stream, byte[]).</typeparam>
    public class Result<T> : IDisposable 
    {
        private bool _disposedValue;

        /// <summary>
        /// Indicates whether the HTTP request was considered successful (typically based on status code).
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Contains the result data (e.g., HttpResponseMessage, deserialized object, string, stream) if the request was successful and data is available.
        /// Check IsSuccess before accessing. Can be null or default even on success (e.g., empty response body).
        /// If T is IDisposable (like HttpResponseMessage or Stream), the consumer of this Result might be responsible for its disposal.
        /// </summary>
        public T? Data { get; set; } // Use nullable T?

        /// <summary>
        /// Contains an error message if the request failed or an exception occurred during processing.
        /// </summary>
        public string? ErrorMessage { get; set; } // Nullable string

        /// <summary>
        /// The HTTP status code returned by the server, if a response was received.
        /// Can be null if the request failed before receiving a response (e.g., network error, cancellation).
        /// </summary>
        public HttpStatusCode? StatusCode { get; set; }

        public static implicit operator bool(Result<T> result) => result.IsSuccess;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    if (Data is IDisposable disposableData)
                    {
                        try
                        {
                           disposableData.Dispose();
                        }
                        catch (ObjectDisposedException) { /* Ignore if already disposed */ }
                    }
                }
                Data = default; 
                _disposedValue = true;
            }
        }


        /// <summary>
        /// Disposes the Result and its disposable Data, if any.
        /// Important for Results containing HttpResponseMessage or Stream.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}