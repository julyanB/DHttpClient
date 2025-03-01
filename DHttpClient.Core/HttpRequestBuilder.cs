using DHttpClient.Extensions;
using System.Net.Mime;
using System.Text;
using DHttpClient.Models;

namespace DHttpClient
{
    /// <summary>
    /// A fluent HTTP request builder that supports various content types and HTTP methods.
    /// </summary>
    public class HttpRequestBuilder : IHttpRequestBuilder, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly bool _disposeHttpClient;
        private HttpMethod _httpMethod;
        private readonly Dictionary<string, string> _httpHeaders;
        private UriBuilder _uriBuilder;
        private HttpContent _httpContent;

        /// <summary>
        /// Initializes a new instance of HttpRequestBuilder using a new HttpClient instance.
        /// </summary>
        public HttpRequestBuilder() : this(new HttpClient(), disposeHttpClient: true)
        {
        }

        /// <summary>
        /// Initializes a new instance of HttpRequestBuilder using the provided HttpClient instance.
        /// </summary>
        public HttpRequestBuilder(HttpClient httpClient, bool disposeHttpClient = false)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _disposeHttpClient = disposeHttpClient;
            _httpHeaders = new Dictionary<string, string>();
        }

        /// <summary>
        /// Sets the request URI.
        /// </summary>
        public IHttpRequestBuilder WithRequestUri(string requestUri)
        {
            if (string.IsNullOrWhiteSpace(requestUri))
                throw new ArgumentException("Request URI cannot be null or whitespace.", nameof(requestUri));

            _uriBuilder = new UriBuilder(requestUri);
            return this;
        }

        /// <summary>
        /// Appends query parameters to the request URI.
        /// </summary>
        public IHttpRequestBuilder WithQueryParameters(object parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            var queryToAppend = parameters.ToQueryString();
            if (!string.IsNullOrWhiteSpace(queryToAppend))
            {
                // Append to any existing query string.
                var currentQuery = _uriBuilder.Query.TrimStart('?');
                _uriBuilder.Query = string.IsNullOrEmpty(currentQuery)
                    ? queryToAppend.TrimStart('?')
                    : $"{currentQuery}&{queryToAppend.TrimStart('?')}";
            }
            return this;
        }

        /// <summary>
        /// Sets the request content directly.
        /// </summary>
        public IHttpRequestBuilder WithContent(HttpContent content)
        {
            _httpContent = content;
            return this;
        }

        /// <summary>
        /// Serializes the object to JSON and sets it as the request body.
        /// </summary>
        public IHttpRequestBuilder WithBodyContent(object parameters)
        {
            var json = parameters.ToJson();
            _httpContent = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);
            return this;
        }

        /// <summary>
        /// Serializes the dictionary to JSON and sets it as the request body.
        /// </summary>
        public IHttpRequestBuilder WithBodyContent(Dictionary<string, string> parameters)
        {
            var json = parameters.ToJson();
            _httpContent = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);
            return this;
        }

        /// <summary>
        /// Builds multipart/form-data content.
        /// </summary>
        public IHttpRequestBuilder WithFormMultiPartContent(Func<MultiPartContentBuilder, HttpContent> builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            var multipartBuilder = new MultiPartContentBuilder();
            _httpContent = builder(multipartBuilder);
            return this;
        }

        /// <summary>
        /// Sets form URL-encoded content using an object's key/value pairs.
        /// </summary>
        public IHttpRequestBuilder WithFormUrlEncodedContent(object parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            _httpContent = new FormUrlEncodedContent(parameters.ToKeyValue());
            return this;
        }

        /// <summary>
        /// Sets form URL-encoded content using a dictionary.
        /// </summary>
        public IHttpRequestBuilder WithFormUrlEncodedContent(Dictionary<string, string> parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            _httpContent = new FormUrlEncodedContent(parameters);
            return this;
        }

        /// <summary>
        /// Adds a single header to the request.
        /// </summary>
        public IHttpRequestBuilder WithHeader(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Header key cannot be null or whitespace.", nameof(key));

            _httpHeaders[key] = value;
            return this;
        }

        /// <summary>
        /// Adds multiple headers to the request.
        /// </summary>
        public IHttpRequestBuilder WithHeaders(Dictionary<string, string> headers)
        {
            if (headers == null)
                throw new ArgumentNullException(nameof(headers));

            foreach (var header in headers)
            {
                _httpHeaders[header.Key] = header.Value;
            }
            return this;
        }

        /// <summary>
        /// Sets the HTTP method (GET, POST, PUT, DELETE, etc.).
        /// </summary>
        public IHttpRequestBuilder WithMethod(HttpMethod method)
        {
            _httpMethod = method ?? throw new ArgumentNullException(nameof(method));
            return this;
        }

        /// <summary>
        /// Configures the request timeout.
        /// </summary>
        public IHttpRequestBuilder WithTimeout(TimeSpan timeout)
        {
            _httpClient.Timeout = timeout;
            return this;
        }

        /// <summary>
        /// Constructs the HttpRequestMessage with all configured settings.
        /// </summary>
        private HttpRequestMessage BuildRequestMessage()
        {
            if (_uriBuilder == null)
                throw new InvalidOperationException("Request URI must be set. Call WithRequestUri first.");

            if (_httpMethod == null)
                throw new InvalidOperationException("HTTP method must be set. Call WithMethod before sending.");

            var request = new HttpRequestMessage
            {
                Method = _httpMethod,
                RequestUri = _uriBuilder.Uri,
                Content = _httpContent
            };

            foreach (var header in _httpHeaders)
            {
                if (!header.Key.Contains("Content", StringComparison.OrdinalIgnoreCase))
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
                else if (request.Content != null)
                {
                    request.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            return request;
        }

        /// <summary>
        /// Sends the request asynchronously and returns a wrapped HttpResponseMessage.
        /// </summary>
        public async Task<Result<HttpResponseMessage>> SendAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var requestMessage = BuildRequestMessage();
                var response = await _httpClient.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    return new Result<HttpResponseMessage>
                    {
                        IsSuccess = false,
                        Data = response,
                        ErrorMessage = $"Request failed with status code {response.StatusCode}. Response: {content}",
                        StatusCode = response.StatusCode
                    };
                }

                return new Result<HttpResponseMessage>
                {
                    IsSuccess = true,
                    Data = response,
                    StatusCode = response.StatusCode
                };
            }
            catch (Exception ex)
            {
                return new Result<HttpResponseMessage>
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    Data = null,
                    StatusCode = null
                };
            }
        }

        /// <summary>
        /// Sends the request and returns the response content as a string wrapped in a Result.
        /// </summary>
        public async Task<Result<string>> SendStringAsync(CancellationToken cancellationToken = default)
        {
            var responseResult = await SendAsync(cancellationToken).ConfigureAwait(false);
            if (!responseResult.IsSuccess)
            {
                return new Result<string>
                {
                    IsSuccess = false,
                    ErrorMessage = responseResult.ErrorMessage,
                    Data = null,
                    StatusCode = responseResult.StatusCode
                };
            }
            try
            {
                string content = await responseResult.Data.Content.ReadAsStringAsync().ConfigureAwait(false);
                return new Result<string>
                {
                    IsSuccess = true,
                    Data = content,
                    StatusCode = responseResult.StatusCode
                };
            }
            catch (Exception ex)
            {
                return new Result<string>
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    Data = null,
                    StatusCode = responseResult.StatusCode
                };
            }
        }

        /// <summary>
        /// Sends the request and deserializes the JSON response to an object of type T wrapped in a Result.
        /// </summary>
        public async Task<Result<T>> SendObjectAsync<T>(CancellationToken cancellationToken = default)
        {
            var stringResult = await SendStringAsync(cancellationToken).ConfigureAwait(false);
            if (!stringResult.IsSuccess)
            {
                return new Result<T>
                {
                    IsSuccess = false,
                    ErrorMessage = stringResult.ErrorMessage,
                    Data = default,
                    StatusCode = stringResult.StatusCode
                };
            }
            try
            {
                T data = stringResult.Data.ToObject<T>();
                return new Result<T>
                {
                    IsSuccess = true,
                    Data = data,
                    StatusCode = stringResult.StatusCode
                };
            }
            catch (Exception ex)
            {
                return new Result<T>
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    Data = default,
                    StatusCode = stringResult.StatusCode
                };
            }
        }

        /// <summary>
        /// Sends the request and returns the response content as a stream wrapped in a Result.
        /// </summary>
        public async Task<Result<Stream>> SendStreamAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var responseResult = await SendAsync(cancellationToken).ConfigureAwait(false);
                if (!responseResult.IsSuccess)
                {
                    return new Result<Stream>
                    {
                        IsSuccess = false,
                        ErrorMessage = responseResult.ErrorMessage,
                        Data = null,
                        StatusCode = responseResult.StatusCode
                    };
                }
                var stream = await responseResult.Data.Content.ReadAsStreamAsync().ConfigureAwait(false);
                return new Result<Stream>
                {
                    IsSuccess = true,
                    Data = stream,
                    StatusCode = responseResult.StatusCode
                };
            }
            catch (Exception ex)
            {
                return new Result<Stream>
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    Data = null,
                    StatusCode = null
                };
            }
        }

        /// <summary>
        /// Sends the request and returns the response content as a byte array wrapped in a Result.
        /// </summary>
        public async Task<Result<byte[]>> SendBytesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var responseResult = await SendAsync(cancellationToken).ConfigureAwait(false);
                if (!responseResult.IsSuccess)
                {
                    return new Result<byte[]>
                    {
                        IsSuccess = false,
                        ErrorMessage = responseResult.ErrorMessage,
                        Data = null,
                        StatusCode = responseResult.StatusCode
                    };
                }
                var bytes = await responseResult.Data.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                return new Result<byte[]>
                {
                    IsSuccess = true,
                    Data = bytes,
                    StatusCode = responseResult.StatusCode
                };
            }
            catch (Exception ex)
            {
                return new Result<byte[]>
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    Data = null,
                    StatusCode = null
                };
            }
        }

        public void Dispose()
        {
            if (_disposeHttpClient)
            {
                _httpClient?.Dispose();
            }
        }
    }
}
