using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;

namespace DHttpClient.Models
{
    /// <summary>
    /// Helps build a MultipartFormDataContent by adding different content types.
    /// </summary>
    public class MultiPartContentBuilder
    {
        private readonly List<HttpContent> _contents;

        public MultiPartContentBuilder()
        {
            _contents = new List<HttpContent>();
        }

        /// <summary>
        /// Adds text content with the specified name and content.
        /// </summary>
        /// <param name="name">The name of the content part.</param>
        /// <param name="content">The text content.</param>
        /// <returns>The current instance for fluent chaining.</returns>
        public MultiPartContentBuilder AddTextContent(string name, string content)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Content name cannot be null or whitespace.", nameof(name));

            // Use an empty string if content is null.
            var textContent = new StringContent(content ?? string.Empty);
            textContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
            {
                // Enclose in quotes to ensure proper formatting.
                Name = $"\"{name}\""
            };

            _contents.Add(textContent);
            return this;
        }

        /// <summary>
        /// Adds file content using a byte array.
        /// </summary>
        /// <param name="name">The name of the content part.</param>
        /// <param name="content">The file content as a byte array.</param>
        /// <param name="fileName">The file name.</param>
        /// <returns>The current instance for fluent chaining.</returns>
        public MultiPartContentBuilder AddFileContent(string name, byte[] content, string fileName)
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

            _contents.Add(byteContent);
            return this;
        }

        /// <summary>
        /// Adds file content using a stream.
        /// </summary>
        /// <param name="name">The name of the content part.</param>
        /// <param name="content">The stream containing the file content.</param>
        /// <param name="fileName">The file name.</param>
        /// <returns>The current instance for fluent chaining.</returns>
        public MultiPartContentBuilder AddStreamContent(string name, Stream content, string fileName)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Content name cannot be null or whitespace.", nameof(name));
            if (content == null)
                throw new ArgumentNullException(nameof(content));
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be null or whitespace.", nameof(fileName));

            var streamContent = new StreamContent(content);
            streamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
            {
                Name = $"\"{name}\"",
                FileName = $"\"{fileName}\""
            };

            _contents.Add(streamContent);
            return this;
        }

        /// <summary>
        /// Builds and returns a MultipartFormDataContent instance containing all added parts.
        /// </summary>
        /// <returns>A MultipartFormDataContent instance.</returns>
        public MultipartFormDataContent Build()
        {
            var multipartContent = new MultipartFormDataContent();
            foreach (var content in _contents)
            {
                multipartContent.Add(content);
            }
            return multipartContent;
        }
    }
}
