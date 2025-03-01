# DHttpClient - A Fluent HTTP Request Builder for .NET

DHttpClient is a flexible and fluent HTTP request builder for .NET applications. It simplifies making HTTP requests by providing an intuitive API for configuring requests, handling responses, and managing various content types.

## Features

- **Fluent API**: Easily build HTTP requests with a chainable, readable syntax.
- **Supports All HTTP Methods**: GET, POST, PUT, DELETE, PATCH, HEAD, OPTIONS.
- **Flexible Content Handling**:
  - JSON serialization and deserialization
  - Form URL-encoded content
  - Multipart form-data for file uploads
- **Automatic Query Parameter Handling**
- **Customizable Headers and Timeout Settings**
- **Error Handling with Unified Response Wrapper**
- **Supports Custom HttpClient for DI compatibility**
- **Result<T> Wrapper** for uniform success/error handling
- **Stream and Byte Array Support** for file downloads
- **Custom HTTP Headers for Requests and Content**
- **Supports Multipart Requests** for handling multiple parts of data
- **Custom HTTP Timeout Configuration**

---

## Installation

Add the package reference to your .NET project:
```sh
Install-Package DHttpClient
```

Or, using .NET CLI:
```sh
dotnet add package DHttpClient
```

---

## Usage

### Basic GET Request
```csharp
using DHttpClient;

var request = new HttpRequestBuilder()
    .WithRequestUri("https://httpbin.org/get")
    .WithMethod(HttpMethod.Get);

var response = await request.SendAsync();

if (response.IsSuccess)
{
    var content = await response.Data.Content.ReadAsStringAsync();
    Console.WriteLine(content);
}
else
{
    Console.WriteLine($"Error: {response.ErrorMessage}");
}
```

### GET Request with Query Parameters
```csharp
var request = new HttpRequestBuilder()
    .WithRequestUri("https://httpbin.org/get")
    .WithQueryParameters(new { user = "JohnDoe", age = 30 })
    .WithMethod(HttpMethod.Get);
```

### POST Request with JSON Payload
```csharp
var payload = new { name = "John", email = "john@example.com" };

var request = new HttpRequestBuilder()
    .WithRequestUri("https://httpbin.org/post")
    .WithMethod(HttpMethod.Post)
    .WithBodyContent(payload);
```

### POST Request with Form URL-Encoded Content
```csharp
var formData = new Dictionary<string, string>
{
    { "username", "testuser" },
    { "password", "securepassword" }
};

var request = new HttpRequestBuilder()
    .WithRequestUri("https://httpbin.org/post")
    .WithMethod(HttpMethod.Post)
    .WithFormUrlEncodedContent(formData);
```

### Multipart Form-Data (File Upload)
```csharp
var multipartRequest = new HttpRequestBuilder()
    .WithRequestUri("https://httpbin.org/post")
    .WithMethod(HttpMethod.Post)
    .WithFormMultiPartContent(builder => builder
        .AddTextContent("description", "Test file upload")
        .AddFileContent("file", File.ReadAllBytes("test.txt"), "test.txt"));
```

### Handling JSON Responses
```csharp
var request = new HttpRequestBuilder()
    .WithRequestUri("https://httpbin.org/json")
    .WithMethod(HttpMethod.Get);

var response = await request.SendObjectAsync<MyResponseModel>();

if (response.IsSuccess)
{
    Console.WriteLine(response.Data.SomeProperty);
}
```

### Configuring Headers
```csharp
var request = new HttpRequestBuilder()
    .WithRequestUri("https://httpbin.org/get")
    .WithMethod(HttpMethod.Get)
    .WithHeader("Authorization", "Bearer YOUR_TOKEN")
    .WithHeader("Custom-Header", "HeaderValue");
```

### Setting Custom Timeout
```csharp
var request = new HttpRequestBuilder()
    .WithRequestUri("https://httpbin.org/get")
    .WithMethod(HttpMethod.Get)
    .WithTimeout(TimeSpan.FromSeconds(10));
```

### Using a Custom HttpClient
```csharp
var httpClient = new HttpClient();
var request = new HttpRequestBuilder(httpClient)
    .WithRequestUri("https://httpbin.org/get")
    .WithMethod(HttpMethod.Get);
```


### Handling Multipart Requests
```csharp
var request = new HttpRequestBuilder()
    .WithRequestUri("https://httpbin.org/post")
    .WithMethod(HttpMethod.Post)
    .WithFormMultiPartContent(builder => builder
        .AddTextContent("key", "value")
        .AddFileContent("file", File.ReadAllBytes("image.png"), "image.png"));
```

# DHttpClient - Send Functionalities Overview

## Sending Requests in DHttpClient

DHttpClient provides multiple methods for sending HTTP requests and handling responses in a structured way using the `Result<T>` wrapper.

---

## `SendAsync()`
### Description:
Sends the HTTP request asynchronously and returns the raw `HttpResponseMessage` wrapped in a `Result<HttpResponseMessage>`.

### Usage:
```csharp
var request = new HttpRequestBuilder()
    .WithRequestUri("https://httpbin.org/get")
    .WithMethod(HttpMethod.Get);

var result = await request.SendAsync();

if (result.IsSuccess)
{
    Console.WriteLine($"Response Status: {result.Data.StatusCode}");
}
else
{
    Console.WriteLine($"Error: {result.ErrorMessage}");
}
```

---

## `SendStringAsync()`
### Description:
Sends the HTTP request and returns the response body as a `string`, wrapped in a `Result<string>`.

### Usage:
```csharp
var result = await request.SendStringAsync();

if (result.IsSuccess)
{
    Console.WriteLine(result.Data); // The raw response content
}
else
{
    Console.WriteLine($"Error: {result.ErrorMessage}");
}
```

---

## `SendObjectAsync<T>()`
### Description:
Sends the HTTP request and deserializes the JSON response to an object of type `T`, wrapped in a `Result<T>`.

### Usage:
```csharp
public class ApiResponse
{
    public string Message { get; set; }
}

var result = await request.SendObjectAsync<ApiResponse>();

if (result.IsSuccess)
{
    Console.WriteLine(result.Data.Message); // Access the deserialized object
}
else
{
    Console.WriteLine($"Error: {result.ErrorMessage}");
}
```

---

## `SendStreamAsync()`
### Description:
Sends the request and returns the response content as a `Stream`, wrapped in a `Result<Stream>`. Useful for downloading large files.

### Usage:
```csharp
var result = await request.SendStreamAsync();

if (result.IsSuccess)
{
    using var fileStream = File.Create("downloadedFile.txt");
    await result.Data.CopyToAsync(fileStream);
    Console.WriteLine("File downloaded successfully.");
}
else
{
    Console.WriteLine($"Error: {result.ErrorMessage}");
}
```

---

## `SendBytesAsync()`
### Description:
Sends the request and returns the response content as a `byte[]`, wrapped in a `Result<byte[]>`. Useful for handling binary data.

### Usage:
```csharp
var result = await request.SendBytesAsync();

if (result.IsSuccess)
{
    File.WriteAllBytes("image.png", result.Data);
    Console.WriteLine("Image downloaded successfully.");
}
else
{
    Console.WriteLine($"Error: {result.ErrorMessage}");
}
```

---

## Error Handling
Each method returns a `Result<T>` that includes:
- `IsSuccess`: Indicates if the request was successful.
- `Data`: The response content in the respective format (`HttpResponseMessage`, `string`, `T`, `Stream`, `byte[]`).
- `ErrorMessage`: Contains error details if the request fails.
- `StatusCode`: The HTTP status code of the response (if available).

Example:
```csharp
var response = await request.SendStringAsync();

if (!response.IsSuccess)
{
    Console.WriteLine($"Error: {response.ErrorMessage}");
}
```

---

These send functionalities provide **structured error handling** and **typed responses**, making HTTP operations in .NET applications more robust and reliable. 🚀



---

## License
This project is licensed under the MIT License.

---

## Contributing
Contributions are welcome! Please open an issue or submit a pull request if you have improvements.

---

## Contact
For questions or issues, open an issue on GitHub.

