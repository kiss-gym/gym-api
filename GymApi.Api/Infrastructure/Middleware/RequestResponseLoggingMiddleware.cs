namespace GymApi.Api.Infrastructure.Middleware
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;

#pragma warning disable CA1873
    public class RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            // Log the request
            var request = await FormatRequest(context.Request);
            var requestHeaders = string.Join(", ", context.Request.Headers.Select(h => $"{h.Key}: {h.Value}"));
            logger.LogInformation("Request: {Method} {Path} | Headers: {Headers} | Body: {Body}", context.Request.Method, context.Request.Path, requestHeaders, request);

            // Copy the original response body stream
            var originalBodyStream = context.Response.Body;

            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            // Call the next middleware in the pipeline
            await next(context);

            // Log the response
            var response = await FormatResponse(context.Response);
            logger.LogInformation("Response: {StatusCode} {Response}", context.Response.StatusCode, response);

            // Copy the contents of the new memory stream (which contains the response) to the original stream
            await responseBody.CopyToAsync(originalBodyStream);
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
            if (response.Body.CanSeek)
            {
                response.Body.Seek(0, SeekOrigin.Begin);
                var text = await new StreamReader(response.Body).ReadToEndAsync();
                response.Body.Seek(0, SeekOrigin.Begin);
                return text;
            }
            else
            {
                return "[Response body is not seekable]";
            }
        }
    }
}
