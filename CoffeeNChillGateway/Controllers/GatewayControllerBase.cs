using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChillGateway.Controllers;

/// <summary>
/// Shared helpers for controllers that forward requests to the Azure Functions app.
/// The [ApiController] attribute is explained in Microsoft (n.d.b); errors use ProblemDetails (Microsoft, n.d.d).
/// </summary>
[ApiController]
public abstract class GatewayControllerBase : ControllerBase
{
    protected static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Builds a JSON request body with a known length. The Functions host has chunked request binding turned off,
    /// so a body sent with chunked transfer encoding (the default for PostAsJsonAsync) arrives empty.
    /// </summary>
    protected static StringContent JsonBody(object value) =>
        new(JsonSerializer.Serialize(value, JsonOptions), Encoding.UTF8, "application/json");

    /// <summary>Buffers a request body so it is sent with a Content-Length instead of chunked.</summary>
    protected static async Task<HttpContent> Buffered(HttpContent content)
    {
        await content.LoadIntoBufferAsync();
        return content;
    }

    /// <summary>
    /// Reads a Functions response, maps it to a gateway DTO and returns it with the same status code.
    /// A failed Functions response is turned into a ProblemDetails error.
    /// </summary>
    protected async Task<IActionResult> Relay<TFrom, TTo>(HttpResponseMessage response, Func<TFrom, TTo> map)
    {
        string text = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return FunctionsError(response, text);
        }

        TFrom? data = JsonSerializer.Deserialize<TFrom>(text, JsonOptions);
        if (data is null)
        {
            return StatusCode(StatusCodes.Status502BadGateway,
                new ProblemDetails { Status = 502, Title = "The Functions backend returned an empty response." });
        }

        return StatusCode((int)response.StatusCode, map(data));
    }

    /// <summary>Builds a ProblemDetails error from a failed Functions response.</summary>
    protected IActionResult FunctionsError(HttpResponseMessage response, string text)
    {
        string detail = string.IsNullOrWhiteSpace(text) ? response.ReasonPhrase ?? "Request failed." : text;
        return StatusCode((int)response.StatusCode, new ProblemDetails
        {
            Status = (int)response.StatusCode,
            Title = "The request could not be completed.",
            Detail = detail
        });
    }
}
