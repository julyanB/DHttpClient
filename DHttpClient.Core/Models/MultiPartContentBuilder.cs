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
        /// Adds file content from a file path.
        /// </summary>
        public MultiPartContentBuilder AddFileContentFromPath(string name, string filePath)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Content name cannot be null or whitespace.", nameof(name));
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                throw new ArgumentException("Invalid file path.", nameof(filePath));

            byte[] fileBytes = File.ReadAllBytes(filePath);
            string fileName = Path.GetFileName(filePath);
            return AddFileContent(name, fileBytes, fileName);
        }

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
