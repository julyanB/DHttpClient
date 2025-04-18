using DHttpClient.Extensions;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using DHttpClient.Models;

namespace DHttpClient
{
    /// <summary>
    /// A fluent HTTP request builder that supports various content types and HTTP methods.
    /// </summary>
    public class DHttpClient : IDHttpClient, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly bool _disposeHttpClient;
        private HttpMethod _httpMethod;
        private readonly Dictionary<string, string> _httpHeaders;
        private UriBuilder _uriBuilder;
        private HttpContent _httpContent;

        public DHttpClient() : this(new HttpClient(), disposeHttpClient: true)
        {
        }

        public DHttpClient(HttpClient httpClient, bool disposeHttpClient = false)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _disposeHttpClient = disposeHttpClient;
            _httpHeaders = new Dictionary<string, string>();
        }

        public IDHttpClient WithRequestUri(string requestUri)
        {
            if (string.IsNullOrWhiteSpace(requestUri))
                throw new ArgumentException("Request URI cannot be null or whitespace.", nameof(requestUri));

            _uriBuilder = new UriBuilder(requestUri);
            return this;
        }

        public IDHttpClient WithQueryParameters(object parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            var queryToAppend = parameters.ToQueryString();
            if (!string.IsNullOrWhiteSpace(queryToAppend))
            {
                var currentQuery = _uriBuilder.Query.TrimStart('?');
                _uriBuilder.Query = string.IsNullOrEmpty(currentQuery)
                    ? queryToAppend.TrimStart('?')
                    : $"{currentQuery}&{queryToAppend.TrimStart('?')}";
            }

            return this;
        }

        public IDHttpClient WithContent(HttpContent content)
        {
            _httpContent = content;
            return this;
        }

        public IDHttpClient WithBodyContent(object parameters)
        {
            var json = parameters.ToJson();
            _httpContent = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);
            return this;
        }

        public IDHttpClient WithBodyContent(Dictionary<string, string> parameters)
        {
            var json = parameters.ToJson();
            _httpContent = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);
            return this;
        }

        public IDHttpClient WithFormMultiPartContent(Func<MultiPartContentBuilder, HttpContent> builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            var multipartBuilder = new MultiPartContentBuilder();
            _httpContent = builder(multipartBuilder);
            return this;
        }

        public IDHttpClient WithFormUrlEncodedContent(object parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            _httpContent = new FormUrlEncodedContent(parameters.ToKeyValue());
            return this;
        }

        public IDHttpClient WithFormUrlEncodedContent(Dictionary<string, string> parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            _httpContent = new FormUrlEncodedContent(parameters);
            return this;
        }

        public IDHttpClient WithHeader(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Header key cannot be null or whitespace.", nameof(key));

            _httpHeaders[key] = value;
            return this;
        }

        public IDHttpClient WithHeaders(Dictionary<string, string> headers)
        {
            if (headers == null)
                throw new ArgumentNullException(nameof(headers));

            foreach (var header in headers)
            {
                _httpHeaders[header.Key] = header.Value;
            }

            return this;
        }

        public IDHttpClient WithMethod(HttpMethod method)
        {
            _httpMethod = method ?? throw new ArgumentNullException(nameof(method));
            return this;
        }

        public IDHttpClient WithTimeout(TimeSpan timeout)
        {
            _httpClient.Timeout = timeout;
            return this;
        }

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

        public async Task<Result<HttpResponseMessage>> SendAsync(
            HttpCompletionOption? completionOption = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var requestMessage = BuildRequestMessage();
                var options = completionOption ?? HttpCompletionOption.ResponseContentRead;

                var response = await _httpClient.SendAsync(requestMessage, options, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
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

        public async Task<Result<string>> SendStringAsync(CancellationToken cancellationToken = default)
        {
            var responseResult = await SendAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
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

        public async Task<Result<Stream>> SendStreamAsync(
            HttpCompletionOption? completionOption = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var responseResult = await SendAsync(
                    completionOption: completionOption,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

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

        public async Task<Result<IAsyncEnumerable<string>>> SendLiveStreamAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var request = BuildRequestMessage();
                // Use SendAsync here, but with ResponseHeadersRead
                var responseResult = await SendAsync(HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

                if (!responseResult.IsSuccess)
                {
                    // Return failure result from the initial connection attempt
                    return new Result<IAsyncEnumerable<string>>
                    {
                        IsSuccess = false,
                        ErrorMessage = responseResult.ErrorMessage, // Includes status code and potentially body
                        Data = null,
                        StatusCode = responseResult.StatusCode
                    };
                }

                // If successful so far, return a Result containing the async enumerable
                var asyncEnumerable = ReadLiveStreamContentAsync(responseResult.Data, cancellationToken); // Helper method
                return new Result<IAsyncEnumerable<string>>
                {
                    IsSuccess = true,
                    Data = asyncEnumerable,
                    StatusCode = responseResult.StatusCode
                };
            }
            catch (Exception ex)
            {
                return new Result<IAsyncEnumerable<string>>
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    Data = null,
                    StatusCode = null
                };
            }
        }

        public async Task<Result<byte[]>> SendBytesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var responseResult = await SendAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
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

        private async IAsyncEnumerable<string> ReadLiveStreamContentAsync(
            HttpResponseMessage response,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            using var disposableResponse = response;

            await using var stream = await disposableResponse.Content
                .ReadAsStreamAsync() 
                .ConfigureAwait(false);

            using var reader = new StreamReader(stream, Encoding.UTF8);

            while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync().ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(line))
                {
                    yield return line;
                }
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