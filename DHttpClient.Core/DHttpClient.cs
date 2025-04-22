#nullable enable
using DHttpClient.Extensions;
using DHttpClient.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DHttpClient;

/// <summary>
/// Stateless fluent HTTP request builder.  
/// After every send the internal state is reset so the instance can be reused (single‑thread only).
/// </summary>
public sealed class DHttpClient : IDHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly bool _disposeHttpClient;

    private HttpMethod _httpMethod = HttpMethod.Get;
    private readonly Dictionary<string, string> _httpHeaders = new(StringComparer.OrdinalIgnoreCase);
    private UriBuilder? _uriBuilder;
    private HttpContent? _httpContent;

    public DHttpClient(HttpClient httpClient, bool disposeHttpClient = false)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _disposeHttpClient = disposeHttpClient;
    }


    public IDHttpClient WithRequestUri(string requestUri)
    {
        _uriBuilder = string.IsNullOrWhiteSpace(requestUri)
            ? throw new ArgumentException("Request URI cannot be null or whitespace.", nameof(requestUri))
            : new UriBuilder(requestUri);

        return this;
    }

    public IDHttpClient WithQueryParameters(object parameters)
    {
        if (_uriBuilder is null)
            throw new InvalidOperationException("Call WithRequestUri before WithQueryParameters.");

        if (parameters is null) return this;

        var query = parameters.ToQueryString().TrimStart('?');
        if (query.Length == 0) return this;

        _uriBuilder.Query = string.IsNullOrEmpty(_uriBuilder.Query)
            ? query
            : $"{_uriBuilder.Query.TrimStart('?')}&{query}";
        return this;
    }

    public IDHttpClient WithContent(HttpContent content)
    {
        _httpContent = content ?? throw new ArgumentNullException(nameof(content));
        return this;
    }

    public IDHttpClient WithBodyContent(object parameters) =>
        WithContent(new StringContent(parameters?.ToJson() ?? throw new ArgumentNullException(nameof(parameters)),
            Encoding.UTF8, "application/json"));

    public IDHttpClient WithBodyContent(Dictionary<string, string> parameters) =>
        WithContent(new StringContent(parameters?.ToJson() ?? throw new ArgumentNullException(nameof(parameters)),
            Encoding.UTF8, "application/json"));

    public IDHttpClient WithFormMultiPartContent(Func<MultiPartContentBuilder, HttpContent> builder)
    {
        _httpContent = (builder ?? throw new ArgumentNullException(nameof(builder)))(new MultiPartContentBuilder());
        return this;
    }

    public IDHttpClient WithFormUrlEncodedContent(object parameters) =>
        WithContent(new FormUrlEncodedContent((parameters ?? throw new ArgumentNullException(nameof(parameters))).ToKeyValue()));

    public IDHttpClient WithFormUrlEncodedContent(Dictionary<string, string> parameters) =>
        WithContent(new FormUrlEncodedContent(parameters ?? throw new ArgumentNullException(nameof(parameters))));

    public IDHttpClient WithHeader(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Header key cannot be null or whitespace.", nameof(key));

        _httpHeaders[key] = value ?? string.Empty;
        return this;
    }

    public IDHttpClient WithHeaders(Dictionary<string, string> headers)
    {
        foreach (var (k, v) in headers ?? throw new ArgumentNullException(nameof(headers)))
            if (!string.IsNullOrWhiteSpace(k))
                _httpHeaders[k] = v ?? string.Empty;
        return this;
    }

    public IDHttpClient WithMethod(HttpMethod method)
    {
        _httpMethod = method ?? throw new ArgumentNullException(nameof(method));
        return this;
    }


    private HttpRequestMessage BuildRequestMessage()
    {
        if (_uriBuilder is null)
            throw new InvalidOperationException("Call WithRequestUri before sending.");

        var msg = new HttpRequestMessage(_httpMethod, _uriBuilder.Uri) { Content = _httpContent };

        foreach (var (k, v) in _httpHeaders)
            if (!msg.Headers.TryAddWithoutValidation(k, v))
                msg.Content?.Headers.TryAddWithoutValidation(k, v);

        return msg;
    }

    private void Reset()
    {
        _httpMethod = HttpMethod.Get;
        _uriBuilder = null;
        _httpContent = null;
        _httpHeaders.Clear();
    }

    public async Task<Result<HttpResponseMessage>> SendAsync(
        HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead,
        CancellationToken cancellationToken = default)
    {
        using var request = BuildRequestMessage();

        try
        {
            var response = await _httpClient.SendAsync(request, completionOption, cancellationToken)
                .ConfigureAwait(false);

            return new Result<HttpResponseMessage>
            {
                IsSuccess = response.IsSuccessStatusCode,
                Data = response,
                StatusCode = response.StatusCode,
                ErrorMessage = response.IsSuccessStatusCode
                    ? null
                    : $"Request failed with status code {(int)response.StatusCode}."
            };
        }
        catch (OperationCanceledException oce) when (oce.CancellationToken == cancellationToken)
        {
            return new Result<HttpResponseMessage> { IsSuccess = false, ErrorMessage = "The operation was canceled." };
        }
        catch (Exception ex)
        {
            return new Result<HttpResponseMessage> { IsSuccess = false, ErrorMessage = $"Unhandled send error: {ex.Message}" };
        }
        finally
        {
            Reset(); // make instance reusable
        }
    }

    private static async Task<string?> ReadAsStringSafeAsync(HttpResponseMessage? resp) =>
        resp?.Content is null ? null : await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

    public async Task<Result<string>> SendStringAsync(CancellationToken cancellationToken = default)
    {
        var respResult = await SendAsync(HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false);

        try
        {
            if (!respResult)
            {
                var err = await ReadAsStringSafeAsync(respResult.Data).ConfigureAwait(false);
                return new Result<string>
                {
                    IsSuccess = false,
                    ErrorMessage = string.IsNullOrWhiteSpace(err)
                        ? respResult.ErrorMessage
                        : $"{respResult.ErrorMessage} Response: {err}",
                    StatusCode = respResult.StatusCode
                };
            }

            var body = await ReadAsStringSafeAsync(respResult.Data).ConfigureAwait(false) ?? string.Empty;
            return new Result<string> { IsSuccess = true, Data = body, StatusCode = respResult.StatusCode };
        }
        finally
        {
            respResult.Dispose();
        }
    }

    public async Task<Result<T>> SendObjectAsync<T>(CancellationToken cancellationToken = default)
    {
        var strResult = await SendStringAsync(cancellationToken).ConfigureAwait(false);

        if (!strResult)
            return new Result<T> { IsSuccess = false, ErrorMessage = strResult.ErrorMessage, StatusCode = strResult.StatusCode };

        if (string.IsNullOrEmpty(strResult.Data))
            return new Result<T> { IsSuccess = true, ErrorMessage = "Response body was empty.", StatusCode = strResult.StatusCode };

        try
        {
            var obj = strResult.Data!.ToObject<T>();
            return new Result<T> { IsSuccess = true, Data = obj!, StatusCode = strResult.StatusCode };
        }
        catch (JsonException jex)
        {
            return new Result<T> { IsSuccess = false, ErrorMessage = $"JSON deserialize error: {jex.Message}", StatusCode = strResult.StatusCode };
        }
    }

    public async Task<Result<Stream>> SendStreamAsync(
        HttpCompletionOption completionOption = HttpCompletionOption.ResponseHeadersRead,
        CancellationToken cancellationToken = default)
    {
        var respResult = await SendAsync(completionOption, cancellationToken).ConfigureAwait(false);

        if (!respResult)
        {
            respResult.Dispose();
            return new Result<Stream> { IsSuccess = false, ErrorMessage = respResult.ErrorMessage, StatusCode = respResult.StatusCode };
        }

        try
        {
            var response = respResult.Data!;
            var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

            // wrap to ensure socket is closed when caller disposes stream
            var wrapped = new ResponseStream(stream, response);

            return new Result<Stream> { IsSuccess = true, Data = wrapped, StatusCode = respResult.StatusCode };
        }
        catch (Exception ex)
        {
            respResult.Dispose();
            return new Result<Stream> { IsSuccess = false, ErrorMessage = $"Stream read error: {ex.Message}", StatusCode = respResult.StatusCode };
        }
    }

    public async Task<Result<IAsyncEnumerable<string>>> SendLiveStreamAsync(CancellationToken cancellationToken = default)
    {
        var respResult = await SendAsync(HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

        if (!respResult)
        {
            respResult.Dispose();
            return new Result<IAsyncEnumerable<string>> { IsSuccess = false, ErrorMessage = respResult.ErrorMessage, StatusCode = respResult.StatusCode };
        }

        async IAsyncEnumerable<string> ReadAsync([EnumeratorCancellation] CancellationToken ct = default)
        { 
            using var response = respResult.Data!;
            await using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            using var reader = new StreamReader(stream, Encoding.UTF8);

            while (!reader.EndOfStream)
            {
                ct.ThrowIfCancellationRequested();
                var line = await reader.ReadLineAsync().ConfigureAwait(false);
                if (line is null) yield break;
                yield return line;
            }
        }

        return new Result<IAsyncEnumerable<string>> { IsSuccess = true, Data = ReadAsync(cancellationToken), StatusCode = respResult.StatusCode };
    }

    public async Task<Result<byte[]>> SendBytesAsync(CancellationToken cancellationToken = default)
    {
        var respResult = await SendAsync(HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false);

        try
        {
            if (!respResult)
            {
                var err = await ReadAsStringSafeAsync(respResult.Data).ConfigureAwait(false);
                return new Result<byte[]>
                {
                    IsSuccess = false,
                    ErrorMessage = string.IsNullOrWhiteSpace(err) ? respResult.ErrorMessage : $"{respResult.ErrorMessage} Response: {err}",
                    StatusCode = respResult.StatusCode
                };
            }

            var bytes = await respResult.Data!.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            return new Result<byte[]> { IsSuccess = true, Data = bytes, StatusCode = respResult.StatusCode };
        }
        finally
        {
            respResult.Dispose();
        }
    }


    private bool _disposed;

    private void Dispose(bool disposing)
    {
        if (!_disposed && disposing && _disposeHttpClient)
            _httpClient.Dispose();
        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }


    /// <summary>
    /// Stream wrapper that disposes the underlying HttpResponseMessage when the stream is closed.
    /// </summary>
    private sealed class ResponseStream : Stream
    {
        private readonly Stream _inner;
        private readonly HttpResponseMessage _response;

        public ResponseStream(Stream inner, HttpResponseMessage response)
        {
            _inner = inner;
            _response = response;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    _inner.Dispose();
                }
                finally
                {
                    _response.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => _inner.CanSeek;
        public override bool CanWrite => _inner.CanWrite;
        public override long Length => _inner.Length;

        public override long Position
        {
            get => _inner.Position;
            set => _inner.Position = value;
        }

        public override void Flush() => _inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
        public override void SetLength(long value) => _inner.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => _inner.Write(buffer, offset, count);

        public override async ValueTask DisposeAsync()
        {
            await _inner.DisposeAsync();
            _response.Dispose();
            await base.DisposeAsync();
        }
    }
}