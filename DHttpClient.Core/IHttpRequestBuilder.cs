using DHttpClient.Models;

namespace DHttpClient
{
    public interface IHttpRequestBuilder : IDisposable
    {
        /// <summary>
        /// Sets the request URI.
        /// </summary>
        IHttpRequestBuilder WithRequestUri(string requestUri);

        /// <summary>
        /// Appends query parameters to the request URI.
        /// </summary>
        IHttpRequestBuilder WithQueryParameters(object parameters);

        /// <summary>
        /// Sets the request content directly.
        /// </summary>
        IHttpRequestBuilder WithContent(HttpContent content);

        /// <summary>
        /// Serializes the object to JSON and sets it as the request body.
        /// </summary>
        IHttpRequestBuilder WithBodyContent(object parameters);

        /// <summary>
        /// Serializes the dictionary to JSON and sets it as the request body.
        /// </summary>
        IHttpRequestBuilder WithBodyContent(Dictionary<string, string> parameters);

        /// <summary>
        /// Builds multipart/form-data content.
        /// </summary>
        IHttpRequestBuilder WithFormMultiPartContent(Func<MultiPartContentBuilder, HttpContent> builder);

        /// <summary>
        /// Sets form URL-encoded content using an object's key/value pairs.
        /// </summary>
        IHttpRequestBuilder WithFormUrlEncodedContent(object parameters);

        /// <summary>
        /// Sets form URL-encoded content using a dictionary.
        /// </summary>
        IHttpRequestBuilder WithFormUrlEncodedContent(Dictionary<string, string> parameters);

        /// <summary>
        /// Adds a single header to the request.
        /// </summary>
        IHttpRequestBuilder WithHeader(string key, string value);

        /// <summary>
        /// Adds multiple headers to the request.
        /// </summary>
        IHttpRequestBuilder WithHeaders(Dictionary<string, string> headers);

        /// <summary>
        /// Sets the HTTP method (GET, POST, PUT, DELETE, etc.).
        /// </summary>
        IHttpRequestBuilder WithMethod(HttpMethod method);

        /// <summary>
        /// Configures the request timeout.
        /// </summary>
        IHttpRequestBuilder WithTimeout(TimeSpan timeout);

        /// <summary>
        /// Sends the request asynchronously and returns a wrapped HttpResponseMessage.
        /// </summary>
        Task<Result<HttpResponseMessage>> SendAsync(CancellationToken cancellationToken = default);

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
        Task<Result<Stream>> SendStreamAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends the request and returns the response content as a byte array wrapped in a Result.
        /// </summary>
        Task<Result<byte[]>> SendBytesAsync(CancellationToken cancellationToken = default);
    }
}
