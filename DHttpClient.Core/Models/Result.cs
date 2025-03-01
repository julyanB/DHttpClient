using System.Net;

namespace DHttpClient.Models;

public class Result<T>
{
    /// <summary>
    /// Indicates if the request was successful.
    /// </summary>
    public bool IsSuccess { get; set; }
    
    /// <summary>
    /// Contains the deserialized data if the request succeeded.
    /// </summary>
    public T Data { get; set; }
    
    /// <summary>
    /// Contains the error message if something went wrong.
    /// </summary>
    public string ErrorMessage { get; set; }
    
    /// <summary>
    /// The HTTP status code from the response.
    /// </summary>
    public HttpStatusCode? StatusCode { get; set; }
}
