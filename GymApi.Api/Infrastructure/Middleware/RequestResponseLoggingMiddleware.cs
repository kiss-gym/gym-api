namespace GymApi.Api.Infrastructure.Middleware;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable CA1873
public class RequestResponseLoggingMiddleware(ILogger<RequestResponseLoggingMiddleware> logger) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // Log the request
        var request = await FormatRequest(context.Request);
        var requestHeaders = string.Join(", ", context.Request.Headers.Select(h => $"{h.Key}: {h.Value}"));
        logger.LogInformation("Request: {Method} {Path} | Headers: {Headers} | Body: {Body}", context.Request.Method, context.Request.Path, requestHeaders, request);

        // Copy the original response body stream
        var originalBodyStream = context.Response.Body;

        // Create a new memory stream to temporarily buffer the response
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            // Call the next middleware in the pipeline
            await next(context);
        }
        catch (KeyNotFoundException ex)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsJsonAsync(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            await context.Response.WriteAsJsonAsync(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted)
            {
                logger.LogWarning("The response has already started, unable to write error response.");
                // Re-throw the exception to let the server handle it (e.g., Kestrel will close the connection)
                throw;
            }
            logger.LogError(ex, "Unhandled exception");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new { error = "An unexpected error occurred." });
        }
        finally
        {
            // Log the response
            var response = await FormatResponse(context.Response);
            logger.LogInformation("Response: {StatusCode} {Response}", context.Response.StatusCode, response);

            // Restore the original response body stream
            context.Response.Body = originalBodyStream;

            // Copy the contents of the new memory stream (which contains the response) to the original stream
            responseBody.Seek(0, SeekOrigin.Begin); // Ensure the memory stream is at the beginning
            await responseBody.CopyToAsync(originalBodyStream);
            
            // It's important to dispose the MemoryStream here, after its content has been copied
            // and the original stream has been restored.
            await responseBody.DisposeAsync();
        }
    }

    private static async Task<string> FormatRequest(HttpRequest request)
    {
        request.EnableBuffering();

        var bodyAsText = string.Empty;
        if (request.ContentLength > 0)
        {
            var bufferSize = Math.Min(4096, Convert.ToInt32(request.ContentLength)); // Limit to 4KB
            var buffer = new byte[bufferSize];
            await request.Body.ReadExactlyAsync(buffer, 0, buffer.Length);
            bodyAsText = Encoding.UTF8.GetString(buffer);
            request.Body.Position = 0;
        }

        return $"Query: {request.QueryString} | Body: {bodyAsText}";
    }
#pragma warning restore CA1873

    private static async Task<string> FormatResponse(HttpResponse response)
    {
        if (!response.Body.CanSeek)
        {
            return "[Response body is not seekable]";
        }

        response.Body.Seek(0, SeekOrigin.Begin);
        var text = await new StreamReader(response.Body).ReadToEndAsync();
        response.Body.Seek(0, SeekOrigin.Begin);
        return text;

    }
}
