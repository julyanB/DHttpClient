using System.Runtime.CompilerServices;
using DHttpClient.Models;

namespace DHttpClient
{
    public interface IDHttpClient : IDisposable
    {
        /// <summary>
        /// Sets the request URI.
        /// </summary>
        IDHttpClient WithRequestUri(string requestUri);

        /// <summary>
        /// Appends query parameters to the request URI.
        /// </summary>
        IDHttpClient WithQueryParameters(object parameters);

        /// <summary>
        /// Sets the request content directly.
        /// </summary>
        IDHttpClient WithContent(HttpContent content);

        /// <summary>
        /// Serializes the object to JSON and sets it as the request body.
        /// </summary>
        IDHttpClient WithBodyContent(object parameters);

        /// <summary>
        /// Serializes the dictionary to JSON and sets it as the request body.
        /// </summary>
        IDHttpClient WithBodyContent(Dictionary<string, string> parameters);

        /// <summary>
        /// Builds multipart/form-data content.
        /// </summary>
        IDHttpClient WithFormMultiPartContent(Func<MultiPartContentBuilder, HttpContent> builder);

        /// <summary>
        /// Sets form URL-encoded content using an object's key/value pairs.
        /// </summary>
        IDHttpClient WithFormUrlEncodedContent(object parameters);

        /// <summary>
        /// Sets form URL-encoded content using a dictionary.
        /// </summary>
        IDHttpClient WithFormUrlEncodedContent(Dictionary<string, string> parameters);

        /// <summary>
        /// Adds a single header to the request.
        /// </summary>
        IDHttpClient WithHeader(string key, string value);

        /// <summary>
        /// Adds multiple headers to the request.
        /// </summary>
        IDHttpClient WithHeaders(Dictionary<string, string> headers);

        /// <summary>
        /// Sets the HTTP method (GET, POST, PUT, DELETE, etc.).
        /// </summary>
        IDHttpClient WithMethod(HttpMethod method);

        /// <summary>
        /// Configures the request timeout.
        /// </summary>
        IDHttpClient WithTimeout(TimeSpan timeout);

        /// <summary>
        /// Sends the request asynchronously and returns a wrapped HttpResponseMessage.
        /// </summary>
        Task<Result<HttpResponseMessage>> SendAsync(HttpCompletionOption? completionOption = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends the request and returns the response content as a string wrapped in a Result.
        /// </summary>
        Task<Result<string>> SendStringAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends the request and deserializes the JSON response to an object of type T wrapped in a Result.
        /// </summary>
        Task<Result<T>> SendObjectAsync<T>(CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends the request and returns the response content as a stream wrapped in a Result.
        /// </summary>
        Task<Result<Stream>> SendStreamAsync(HttpCompletionOption? completionOption = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends the request and returns the response content as a stream of type T wrapped in a Result.
        /// </summary>
        Task<Result<IAsyncEnumerable<string>>> SendLiveStreamAsync([EnumeratorCancellation] CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Sends the request and returns the response content as a byte array wrapped in a Result.
        /// </summary>
        Task<Result<byte[]>> SendBytesAsync(CancellationToken cancellationToken = default);
    }
}
