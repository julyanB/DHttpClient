using DHttpClient;

namespace TestProject1
{
    public class HttpMethodsTests
    {
        // ========= GET Tests =========
        [Fact]
        public async Task GetRequest_ShouldReturnMethodGet()
        {
            using var builder = new HttpRequestBuilder();
            builder.WithRequestUri("https://httpbin.org/anything")
                   .WithMethod(HttpMethod.Get);

            var result = await builder.SendAsync();
            Assert.True(result.IsSuccess, $"Request failed: {result.ErrorMessage}");
            var content = await result.Data.Content.ReadAsStringAsync();
            Assert.Contains("\"method\": \"GET\"", content, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task GetRequest_WithQueryParameters_ShouldEchoQueryParameters()
        {
            using var builder = new HttpRequestBuilder();
            builder.WithRequestUri("https://httpbin.org/anything")
                   .WithQueryParameters(new { test = "value", number = 123 })
                   .WithMethod(HttpMethod.Get);

            var result = await builder.SendAsync();
            Assert.True(result.IsSuccess, $"Request failed: {result.ErrorMessage}");
            var content = await result.Data.Content.ReadAsStringAsync();
            Assert.Contains("test", content, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("value", content, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("123", content, StringComparison.OrdinalIgnoreCase);
        }

        // ========= POST Tests =========
        [Fact]
        public async Task PostRequest_ShouldReturnMethodPostAndJsonPayload()
        {
            using var builder = new HttpRequestBuilder();
            var payload = new { name = "test", value = 456 };
            builder.WithRequestUri("https://httpbin.org/anything")
                   .WithMethod(HttpMethod.Post)
                   .WithBodyContent(payload);

            var result = await builder.SendAsync();
            Assert.True(result.IsSuccess, $"Request failed: {result.ErrorMessage}");
            var content = await result.Data.Content.ReadAsStringAsync();
            Assert.Contains("\"method\": \"POST\"", content, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("\"name\": \"test\"", content, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("\"value\": 456", content, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task PostRequest_WithFormUrlEncodedContent_ShouldReturnFormData()
        {
            using var builder = new HttpRequestBuilder();
            var formData = new Dictionary<string, string>
            {
                {"field1", "value1"},
                {"field2", "value2"}
            };
            builder.WithRequestUri("https://httpbin.org/anything")
                   .WithMethod(HttpMethod.Post)
                   .WithFormUrlEncodedContent(formData);

            var result = await builder.SendAsync();
            Assert.True(result.IsSuccess, $"Request failed: {result.ErrorMessage}");
            var content = await result.Data.Content.ReadAsStringAsync();
            Assert.Contains("\"method\": \"POST\"", content, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("value1", content, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("value2", content, StringComparison.OrdinalIgnoreCase);
        }

        // ========= PUT Tests =========
        [Fact]
        public async Task PutRequest_ShouldReturnMethodPutAndJsonPayload()
        {
            using var builder = new HttpRequestBuilder();
            var payload = new { id = 1, status = "updated" };
            builder.WithRequestUri("https://httpbin.org/anything")
                   .WithMethod(HttpMethod.Put)
                   .WithBodyContent(payload);

            var result = await builder.SendAsync();
            Assert.True(result.IsSuccess, $"Request failed: {result.ErrorMessage}");
            var content = await result.Data.Content.ReadAsStringAsync();
            Assert.Contains("\"method\": \"PUT\"", content, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("\"id\": 1", content, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("\"status\": \"updated\"", content, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task PutRequest_WithQueryParameters_ShouldEchoQueryParameters()
        {
            using var builder = new HttpRequestBuilder();
            builder.WithRequestUri("https://httpbin.org/anything")
                   .WithQueryParameters(new { update = "true", user = "john" })
                   .WithMethod(HttpMethod.Put);

            var result = await builder.SendAsync();
            Assert.True(result.IsSuccess, $"Request failed: {result.ErrorMessage}");
            var content = await result.Data.Content.ReadAsStringAsync();
            Assert.Contains("update", content, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("true", content, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("john", content, StringComparison.OrdinalIgnoreCase);
        }

        // ========= DELETE Tests =========
        [Fact]
        public async Task DeleteRequest_ShouldReturnMethodDelete()
        {
            using var builder = new HttpRequestBuilder();
            builder.WithRequestUri("https://httpbin.org/anything")
                   .WithMethod(HttpMethod.Delete);

            var result = await builder.SendAsync();
            Assert.True(result.IsSuccess, $"Request failed: {result.ErrorMessage}");
            var content = await result.Data.Content.ReadAsStringAsync();
            Assert.Contains("\"method\": \"DELETE\"", content, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task DeleteRequest_WithJsonPayload_ShouldReturnMethodDeleteAndPayload()
        {
            using var builder = new HttpRequestBuilder();
            var payload = new { reason = "cleanup", force = true };
            builder.WithRequestUri("https://httpbin.org/anything")
                   .WithMethod(HttpMethod.Delete)
                   .WithBodyContent(payload);

            var result = await builder.SendAsync();
            Assert.True(result.IsSuccess, $"Request failed: {result.ErrorMessage}");
            var content = await result.Data.Content.ReadAsStringAsync();
            Assert.Contains("\"method\": \"DELETE\"", content, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("\"reason\": \"cleanup\"", content, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("\"force\": true", content, StringComparison.OrdinalIgnoreCase);
        }

        // ========= PATCH Tests =========
        [Fact]
        public async Task PatchRequest_ShouldReturnMethodPatchAndJsonPayload()
        {
            using var builder = new HttpRequestBuilder();
            var payload = new { patchField = "value" };
            builder.WithRequestUri("https://httpbin.org/anything")
                   .WithMethod(new HttpMethod("PATCH"))
                   .WithBodyContent(payload);

            var result = await builder.SendAsync();
            Assert.True(result.IsSuccess, $"Request failed: {result.ErrorMessage}");
            var content = await result.Data.Content.ReadAsStringAsync();
            Assert.Contains("\"method\": \"PATCH\"", content, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("\"patchField\": \"value\"", content, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task PatchRequest_WithFormUrlEncodedContent_ShouldReturnFormData()
        {
            using var builder = new HttpRequestBuilder();
            var formData = new Dictionary<string, string>
            {
                {"patchField", "newValue"}
            };
            builder.WithRequestUri("https://httpbin.org/anything")
                   .WithMethod(new HttpMethod("PATCH"))
                   .WithFormUrlEncodedContent(formData);

            var result = await builder.SendAsync();
            Assert.True(result.IsSuccess, $"Request failed: {result.ErrorMessage}");
            var content = await result.Data.Content.ReadAsStringAsync();
            Assert.Contains("\"method\": \"PATCH\"", content, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("newValue", content, StringComparison.OrdinalIgnoreCase);
        }

        // ========= HEAD Tests =========
        [Fact]
        public async Task HeadRequest_ShouldReturnStatus200AndNoBody()
        {
            using var builder = new HttpRequestBuilder();
            builder.WithRequestUri("https://httpbin.org/anything")
                   .WithMethod(HttpMethod.Head);

            var result = await builder.SendAsync();
            Assert.True(result.IsSuccess, $"Request failed: {result.ErrorMessage}");
            var content = await result.Data.Content.ReadAsStringAsync();
            Assert.True(string.IsNullOrEmpty(content));
        }

        [Fact]
        public async Task HeadRequest_ShouldReturnCorrectHeaders()
        {
            using var builder = new HttpRequestBuilder();
            builder.WithRequestUri("https://httpbin.org/anything")
                   .WithMethod(HttpMethod.Head);

            var result = await builder.SendAsync();
            Assert.True(result.IsSuccess, $"Request failed: {result.ErrorMessage}");
            Assert.True(result.Data.Headers.Contains("Date"));
        }

        // ========= OPTIONS Tests =========
        [Fact]
        public async Task OptionsRequest_ShouldReturnAllowedMethods()
        {
            using var builder = new HttpRequestBuilder();
            builder.WithRequestUri("https://httpbin.org/anything")
                   .WithMethod(HttpMethod.Options);

            var result = await builder.SendAsync();
            Assert.True(result.IsSuccess, $"Request failed: {result.ErrorMessage}");
            bool hasAllow = (result.Data.Content.Headers.Allow != null && result.Data.Content.Headers.Allow.Count > 0)
                || (result.Data.Headers.TryGetValues("Allow", out var values) && values != null);
            Assert.True(hasAllow);
        }

        [Fact]
        public async Task OptionsRequest_WithQueryParameters_ShouldReturnAllowedMethods()
        {
            using var builder = new HttpRequestBuilder();
            builder.WithRequestUri("https://httpbin.org/anything")
                   .WithQueryParameters(new { extra = "true" })
                   .WithMethod(HttpMethod.Options);

            var result = await builder.SendAsync();
            Assert.True(result.IsSuccess, $"Request failed: {result.ErrorMessage}");
            bool hasAllow = (result.Data.Content.Headers.Allow != null && result.Data.Content.Headers.Allow.Count > 0)
                || (result.Data.Headers.TryGetValues("Allow", out var values) && values != null);
            Assert.True(hasAllow);
        }
    }
}
