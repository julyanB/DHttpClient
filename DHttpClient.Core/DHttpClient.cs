using DHttpClient.Extensions;
using DHttpClient.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Mime; 
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json; 
using System.Threading;
using System.Threading.Tasks;

namespace DHttpClient
{
    /// <summary>
    /// A fluent HTTP request builder that supports various content types and HTTP methods.
    /// Note: Per-request timeouts should be handled using the CancellationToken passed to Send* methods.
    /// </summary>
    public class DHttpClient : IDHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly bool _disposeHttpClient;
        private HttpMethod _httpMethod = HttpMethod.Get; 
        private readonly Dictionary<string, string> _httpHeaders;
        private UriBuilder? _uriBuilder;
        private HttpContent? _httpContent; 


        /// <summary>
        /// Initializes a new instance of the <see cref="DHttpClient"/> class with a specific <see cref="HttpClient"/>.
        /// </summary>
        /// <param name="httpClient">The HttpClient instance to use.</param>
        /// <param name="disposeHttpClient">Whether to dispose the HttpClient when this DHttpClient instance is disposed. Set to false when using IHttpClientFactory.</param>
        public DHttpClient(HttpClient httpClient, bool disposeHttpClient = false)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _disposeHttpClient = disposeHttpClient;
            _httpHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); 
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
            if (_uriBuilder == null)
                throw new InvalidOperationException("Request URI must be set before adding query parameters. Call WithRequestUri first.");
            if (parameters == null)
                return this; 

            var queryToAppend = parameters.ToQueryString(); 
            if (!string.IsNullOrWhiteSpace(queryToAppend))
            {
                var currentQuery = _uriBuilder.Query?.TrimStart('?') ?? string.Empty;
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
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));
            
            var json = parameters.ToJson(); 
            _httpContent = new StringContent(json, Encoding.UTF8, "application/json"); 
            return this;
        }

        public IDHttpClient WithBodyContent(Dictionary<string, string> parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));
            
            var json = parameters.ToJson(); 
            _httpContent = new StringContent(json, Encoding.UTF8, "application/json"); 
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
            
            _httpHeaders[key] = value ?? string.Empty;
            return this;
        }

        public IDHttpClient WithHeaders(Dictionary<string, string> headers)
        {
            if (headers == null)
                throw new ArgumentNullException(nameof(headers));
            
            foreach (var header in headers)
            {
                if (!string.IsNullOrWhiteSpace(header.Key))
                {
                     _httpHeaders[header.Key] = header.Value ?? string.Empty;
                }
            }
            return this;
        }

        public IDHttpClient WithMethod(HttpMethod method)
        {
            _httpMethod = method ?? throw new ArgumentNullException(nameof(method));
            return this;
        }


        private HttpRequestMessage BuildRequestMessage()
        {
            if (_uriBuilder == null)
                throw new InvalidOperationException("Request URI must be set. Call WithRequestUri first.");

            var request = new HttpRequestMessage
            {
                Method = _httpMethod,
                RequestUri = _uriBuilder.Uri,
                Content = _httpContent // Can be null
            };

            foreach (var header in _httpHeaders)
            {
                if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value))
                {
                    request.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            return request;
        }

        public async Task<Result<HttpResponseMessage>> SendAsync(
            HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead, 
            CancellationToken cancellationToken = default)
        {
            HttpRequestMessage requestMessage = null!;
            try
            {
                requestMessage = BuildRequestMessage();
                var response = await _httpClient.SendAsync(requestMessage, completionOption, cancellationToken).ConfigureAwait(false);

                return new Result<HttpResponseMessage>
                {
                    IsSuccess = response.IsSuccessStatusCode,
                    Data = response,
                    StatusCode = response.StatusCode,
                    ErrorMessage = response.IsSuccessStatusCode ? null : $"Request failed with status code {response.StatusCode}."
                };
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
            {
                 requestMessage?.Dispose(); 
                 return new Result<HttpResponseMessage>
                 {
                     IsSuccess = false,
                     ErrorMessage = "The operation was canceled.",
                     Data = null, 
                     StatusCode = null
                 };
            }
            catch (Exception ex)
            {
                requestMessage?.Dispose(); 
                return new Result<HttpResponseMessage>
                {
                    IsSuccess = false,
                    ErrorMessage = $"An error occurred: {ex.Message}",
                    Data = null,
                    StatusCode = null
                };
            }
        }

        private async Task<string?> GetContentAsStringAsync(HttpResponseMessage? response)
        {
             if (response?.Content == null) return null;
             return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }


        public async Task<Result<string>> SendStringAsync(CancellationToken cancellationToken = default)
        {
            using var responseResult = await SendAsync(HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false);

            if (!responseResult.IsSuccess)
            {
                var errorContent = await GetContentAsStringAsync(responseResult.Data).ConfigureAwait(false);
                return new Result<string>
                {
                    IsSuccess = false,
                    ErrorMessage = string.IsNullOrWhiteSpace(errorContent) ? responseResult.ErrorMessage : $"{responseResult.ErrorMessage} Response: {errorContent}",
                    Data = null,
                    StatusCode = responseResult.StatusCode
                };
            }

            try
            {
                string content = await GetContentAsStringAsync(responseResult.Data).ConfigureAwait(false) ?? string.Empty;
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
                    ErrorMessage = $"Failed to read response content: {ex.Message}",
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
                    Data = default!, 
                    StatusCode = stringResult.StatusCode
                };
            }

             if (string.IsNullOrEmpty(stringResult.Data))
             {
                 return new Result<T>
                 {
                     IsSuccess = true,
                     Data = default!,
                     StatusCode = stringResult.StatusCode,
                     ErrorMessage = "Response body was empty." 
                 };
             }

            try
            {
                T? data = stringResult.Data.ToObject<T>();
                return new Result<T>
                {
                    IsSuccess = true,
                    Data = data!,
                    StatusCode = stringResult.StatusCode
                };
            }
            catch (JsonException ex) 
            {
                 return new Result<T>
                 {
                     IsSuccess = false,
                     ErrorMessage = $"Failed to deserialize JSON response: {ex.Message}",
                     Data = default!,
                     StatusCode = stringResult.StatusCode
                 };
            }
            catch (Exception ex)
            {
                return new Result<T>
                {
                    IsSuccess = false,
                    ErrorMessage = $"An error occurred during deserialization: {ex.Message}",
                    Data = default!,
                    StatusCode = stringResult.StatusCode
                };
            }
        }

         public async Task<Result<Stream>> SendStreamAsync(
             HttpCompletionOption completionOption = HttpCompletionOption.ResponseHeadersRead, 
             CancellationToken cancellationToken = default)
         {
             var responseResult = await SendAsync(completionOption, cancellationToken).ConfigureAwait(false);

             if (!responseResult.IsSuccess)
             {
                 responseResult.Data?.Dispose();
                 return new Result<Stream>
                 {
                     IsSuccess = false,
                     ErrorMessage = responseResult.ErrorMessage,
                     Data = null,
                     StatusCode = responseResult.StatusCode
                 };
             }

             try
             {
                 var stream = await responseResult.Data!.Content.ReadAsStreamAsync().ConfigureAwait(false); 
                 return new Result<Stream>
                 {
                     IsSuccess = true,
                     Data = stream, 
                     StatusCode = responseResult.StatusCode,
                 };
             }
             catch (Exception ex)
             {
                 responseResult.Data?.Dispose(); 
                 return new Result<Stream>
                 {
                     IsSuccess = false,
                     ErrorMessage = $"Failed to read response stream: {ex.Message}",
                     Data = null,
                     StatusCode = responseResult.StatusCode
                 };
             }
         }

        public async Task<Result<IAsyncEnumerable<string>>> SendLiveStreamAsync(CancellationToken cancellationToken = default)
        {
             var responseResult = await SendAsync(HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

             if (!responseResult.IsSuccess)
             {
                 responseResult.Data?.Dispose();
                 return new Result<IAsyncEnumerable<string>>
                 {
                     IsSuccess = false,
                     ErrorMessage = responseResult.ErrorMessage,
                     Data = null,
                     StatusCode = responseResult.StatusCode
                 };
             }

             try
             {
                 var asyncEnumerable = ReadLiveStreamContentAsync(responseResult.Data, cancellationToken);
                 return new Result<IAsyncEnumerable<string>>
                 {
                     IsSuccess = true,
                     Data = asyncEnumerable,
                     StatusCode = responseResult.StatusCode
                 };
             }
             catch (Exception ex) 
             {
                 responseResult.Data?.Dispose();
                 return new Result<IAsyncEnumerable<string>>
                 {
                     IsSuccess = false,
                     ErrorMessage = $"Error setting up live stream: {ex.Message}",
                     Data = null,
                     StatusCode = responseResult.StatusCode
                 };
             }
        }

        public async Task<Result<byte[]>> SendBytesAsync(CancellationToken cancellationToken = default)
        {
            using var responseResult = await SendAsync(HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false);

            if (!responseResult.IsSuccess)
            {
                var errorContent = await GetContentAsStringAsync(responseResult.Data).ConfigureAwait(false);
                return new Result<byte[]>
                {
                    IsSuccess = false,
                    ErrorMessage = string.IsNullOrWhiteSpace(errorContent) ? responseResult.ErrorMessage : $"{responseResult.ErrorMessage} Response: {errorContent}",
                    Data = null,
                    StatusCode = responseResult.StatusCode
                };
            }

            try
            {
                var bytes = responseResult.Data?.Content != null
                    ? await responseResult.Data.Content.ReadAsByteArrayAsync().ConfigureAwait(false) 
                    : Array.Empty<byte>();

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
                    ErrorMessage = $"Failed to read response content as bytes: {ex.Message}",
                    Data = null,
                    StatusCode = responseResult.StatusCode
                };
            }
        }

        private async IAsyncEnumerable<string> ReadLiveStreamContentAsync(
            HttpResponseMessage response, 
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            using var disposableResponse = response;
            Stream? stream = null;
            StreamReader? reader = null;

            try
            {
                stream = await disposableResponse.Content.ReadAsStreamAsync().ConfigureAwait(false);
                reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: false);

                while (!reader.EndOfStream)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var line = await reader.ReadLineAsync().ConfigureAwait(false); 

                    if (line == null) break;

                    yield return line;
                }
            }
            finally
            {
                 reader?.Dispose();
            }
        }

        private bool _disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    if (_disposeHttpClient)
                    {
                        _httpClient?.Dispose();
                    }
                }
                _disposedValue = true;
            }
        }


        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}