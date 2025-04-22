using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DHttpClient.Models; 

namespace DHttpClient
{
    /// <summary>
    /// Interface for a fluent HTTP request builder.
    /// </summary>
    public interface IDHttpClient : IDisposable
    {
        /// <summary>
        /// Sets the base request URI.
        /// </summary>
        /// <param name="requestUri">The absolute or relative request URI.</param>
        /// <returns>The current IDHttpClient instance for fluent chaining.</returns>
        IDHttpClient WithRequestUri(string requestUri);

        /// <summary>
        /// Appends query parameters to the request URI from an object's properties.
        /// Keys and values will be URL-encoded.
        /// </summary>
        /// <param name="parameters">An object whose public properties represent the query parameters.</param>
        /// <returns>The current IDHttpClient instance for fluent chaining.</returns>
        IDHttpClient WithQueryParameters(object parameters);

        /// <summary>
        /// Sets the request body content directly using an HttpContent instance.
        /// </summary>
        /// <param name="content">The HttpContent for the request body.</param>
        /// <returns>The current IDHttpClient instance for fluent chaining.</returns>
        IDHttpClient WithContent(HttpContent content);

        /// <summary>
        /// Serializes the object to JSON (using System.Text.Json with camelCase) and sets it as the request body with "application/json" content type.
        /// </summary>
        /// <param name="parameters">The object to serialize.</param>
        /// <returns>The current IDHttpClient instance for fluent chaining.</returns>
        IDHttpClient WithBodyContent(object parameters);

        /// <summary>
        /// Serializes the dictionary to JSON (using System.Text.Json with camelCase) and sets it as the request body with "application/json" content type.
        /// </summary>
        /// <param name="parameters">The dictionary to serialize.</param>
        /// <returns>The current IDHttpClient instance for fluent chaining.</returns>
        IDHttpClient WithBodyContent(Dictionary<string, string> parameters);

        /// <summary>
        /// Builds multipart/form-data content using a helper builder.
        /// </summary>
        /// <param name="builder">A function that configures the multipart content using <see cref="MultiPartContentBuilder"/> and returns the resulting <see cref="HttpContent"/>.</param>
        /// <returns>The current IDHttpClient instance for fluent chaining.</returns>
        IDHttpClient WithFormMultiPartContent(Func<MultiPartContentBuilder, HttpContent> builder);

        /// <summary>
        /// Sets form URL-encoded content (application/x-www-form-urlencoded) using an object's public property key/value pairs.
        /// </summary>
        /// <param name="parameters">The object whose properties provide the form data.</param>
        /// <returns>The current IDHttpClient instance for fluent chaining.</returns>
        IDHttpClient WithFormUrlEncodedContent(object parameters);

        /// <summary>
        /// Sets form URL-encoded content (application/x-www-form-urlencoded) using a dictionary.
        /// </summary>
        /// <param name="parameters">The dictionary providing the form data.</param>
        /// <returns>The current IDHttpClient instance for fluent chaining.</returns>
        IDHttpClient WithFormUrlEncodedContent(Dictionary<string, string> parameters);

        /// <summary>
        /// Adds or updates a single header for the request.
        /// </summary>
        /// <param name="key">The header key.</param>
        /// <param name="value">The header value.</param>
        /// <returns>The current IDHttpClient instance for fluent chaining.</returns>
        IDHttpClient WithHeader(string key, string value);

        /// <summary>
        /// Adds or updates multiple headers for the request.
        /// </summary>
        /// <param name="headers">A dictionary containing the headers.</param>
        /// <returns>The current IDHttpClient instance for fluent chaining.</returns>
        IDHttpClient WithHeaders(Dictionary<string, string> headers);

        /// <summary>
        /// Sets the HTTP method (e.g., GET, POST, PUT, DELETE). Defaults to GET if not called.
        /// </summary>
        /// <param name="method">The HttpMethod to use.</param>
        /// <returns>The current IDHttpClient instance for fluent chaining.</returns>
        IDHttpClient WithMethod(HttpMethod method);

        // WithTimeout method removed. Use CancellationToken in Send* methods for timeouts.
        // /// <summary>
        // /// Configures the request timeout. Warning: Affects the underlying HttpClient instance if shared.
        // /// Prefer using CancellationToken for per-request timeouts.
        // /// </summary>
        // IDHttpClient WithTimeout(TimeSpan timeout);

        /// <summary>
        /// Sends the configured request asynchronously.
        /// Use the CancellationToken to implement timeouts (e.g., `new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token`).
        /// </summary>
        /// <param name="completionOption">Defines when the operation should complete (e.g., after reading headers or content).</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation, yielding a Result containing the HttpResponseMessage.</returns>
        Task<Result<HttpResponseMessage>> SendAsync(
            HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends the request, reads the response body as a string, and returns it wrapped in a Result.
        /// Use the CancellationToken to implement timeouts.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation, yielding a Result containing the response body as a string.</returns>
        Task<Result<string>> SendStringAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends the request, reads the response body as JSON, deserializes it to type T (using System.Text.Json), and returns it wrapped in a Result.
        /// Use the CancellationToken to implement timeouts.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the JSON response into.</typeparam>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation, yielding a Result containing the deserialized object.</returns>
        Task<Result<T>> SendObjectAsync<T>(CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends the request and returns the response content as a Stream wrapped in a Result.
        /// Sends request with HttpCompletionOption.ResponseHeadersRead by default.
        /// Dispose the returned Stream to close the underlying connection.
        /// Use the CancellationToken to implement timeouts.
        /// </summary>
        /// <param name="completionOption">Defines when the operation should complete. Defaults to ResponseHeadersRead.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation, yielding a Result containing the response stream.</returns>
        Task<Result<Stream>> SendStreamAsync(
             HttpCompletionOption completionOption = HttpCompletionOption.ResponseHeadersRead,
             CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends the request and returns the response content as an asynchronous stream of strings (lines).
        /// Useful for server-sent events or chunked responses. The underlying HttpResponseMessage is disposed when the enumeration completes or is cancelled.
        /// Use the CancellationToken to implement timeouts or stop enumeration early.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation, yielding a Result containing an IAsyncEnumerable of strings.</returns>
        Task<Result<IAsyncEnumerable<string>>> SendLiveStreamAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default); // [EnumeratorCancellation] helps IDEs

        /// <summary>
        /// Sends the request, reads the response body as a byte array, and returns it wrapped in a Result.
        /// Use the CancellationToken to implement timeouts.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation, yielding a Result containing the response body as a byte array.</returns>
        Task<Result<byte[]>> SendBytesAsync(CancellationToken cancellationToken = default);
    }
}