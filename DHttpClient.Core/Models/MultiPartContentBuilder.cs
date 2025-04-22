using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks; 

namespace DHttpClient.Models
{
    /// <summary>
    /// Helps build a MultipartFormDataContent by adding different content types fluently.
    /// </summary>
    public class MultiPartContentBuilder
    {
        private readonly List<HttpContent> _contents;

        public MultiPartContentBuilder()
        {
            _contents = new List<HttpContent>();
        }

        /// <summary>
        /// Adds string content (text).
        /// </summary>
        /// <param name="name">The control name.</param>
        /// <param name="content">The string value.</param>
        /// <returns>The builder instance for chaining.</returns>
        public MultiPartContentBuilder AddTextContent(string name, string content)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Content name cannot be null or whitespace.", nameof(name));

            var textContent = new StringContent(content ?? string.Empty);

            textContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
            {
                Name = $"\"{name}\"" 
            };

            _contents.Add(textContent);
            return this;
        }

        /// <summary>
        /// Adds binary content from a byte array (e.g., an image or file read into memory).
        /// </summary>
        /// <param name="name">The control name (for the file).</param>
        /// <param name="content">The file content as a byte array.</param>
        /// <param name="fileName">The original file name to be sent to the server.</param>
        /// <param name="contentType">Optional media type (e.g., "image/jpeg"). If null, HttpClient might guess.</param>
        /// <returns>The builder instance for chaining.</returns>
        public MultiPartContentBuilder AddFileContent(string name, byte[] content, string fileName, string? contentType = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Content name cannot be null or whitespace.", nameof(name));
            if (content == null)
                throw new ArgumentNullException(nameof(content));
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be null or whitespace.", nameof(fileName));

            var byteContent = new ByteArrayContent(content);

            byteContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
            {
                Name = $"\"{name}\"",
                FileName = $"\"{fileName}\"" 
            };

            if (!string.IsNullOrWhiteSpace(contentType))
            {
                byteContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            }

            _contents.Add(byteContent);
            return this;
        }

        /// <summary>
        /// Adds content from a Stream. The caller is responsible for ensuring the stream is positioned correctly and disposed appropriately *after* the request is sent.
        /// </summary>
        /// <param name="name">The control name.</param>
        /// <param name="content">The stream content. Ensure it's readable and positioned at the start.</param>
        /// <param name="fileName">The file name associated with the stream.</param>
        /// <param name="contentType">Optional media type (e.g., "application/octet-stream").</param>
        /// <returns>The builder instance for chaining.</returns>
        public MultiPartContentBuilder AddStreamContent(string name, Stream content, string fileName, string? contentType = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Content name cannot be null or whitespace.", nameof(name));
            if (content == null)
                throw new ArgumentNullException(nameof(content));
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be null or whitespace.", nameof(fileName));
             if (!content.CanRead)
                throw new ArgumentException("Stream must be readable.", nameof(content));

            var streamContent = new StreamContent(content);

            streamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
            {
                Name = $"\"{name}\"",
                FileName = $"\"{fileName}\""
            };

            if (!string.IsNullOrWhiteSpace(contentType))
            {
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            }

            _contents.Add(streamContent);
            return this;
        }

        /// <summary>
        /// Adds file content by reading from a file path asynchronously.
        /// </summary>
        /// <param name="name">The control name.</param>
        /// <param name="filePath">The full path to the file.</param>
        /// <param name="contentType">Optional media type. If null, HttpClient might guess based on extension.</param>
        /// <returns>A Task representing the asynchronous operation, yielding the builder instance for chaining.</returns>
        public async Task<MultiPartContentBuilder> AddFileContentFromPathAsync(string name, string filePath, string? contentType = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Content name cannot be null or whitespace.", nameof(name));
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or whitespace.", nameof(filePath));
            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found.", filePath);

            byte[] fileBytes = await File.ReadAllBytesAsync(filePath).ConfigureAwait(false);
            string fileName = Path.GetFileName(filePath);

            return AddFileContent(name, fileBytes, fileName, contentType);
        }


        /// <summary>
        /// Builds the final MultipartFormDataContent instance.
        /// </summary>
        /// <returns>A MultipartFormDataContent instance containing all added parts.</returns>
        public MultipartFormDataContent Build()
        {
            var multipartContent = new MultipartFormDataContent();

            foreach (var content in _contents)
            {
                multipartContent.Add(content);
            }

            _contents.Clear(); 

            return multipartContent;
        }
    }
}