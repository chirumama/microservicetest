using Serilog;
using System.Diagnostics;
using System.Text;

namespace MicroserviceHub.API.Middleware
{
    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;

        public RequestResponseLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();

            // ── REQUEST LOG ──────────────────────────────────────
            var requestBody = await ReadRequestBody(context.Request);

            Log.ForContext("LogType", "Request")
               .Information(
                   "[REQUEST] {Method} {Path}{Query} | IP: {IP} | Body: {Body}",
                   context.Request.Method,
                   context.Request.Path,
                   context.Request.QueryString,
                   context.Connection.RemoteIpAddress,
                   string.IsNullOrWhiteSpace(requestBody) ? "(empty)" : requestBody
               );

            // ── CAPTURE RESPONSE ─────────────────────────────────
            var originalBodyStream = context.Response.Body;
            using var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;

            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                // ── ERROR LOG ────────────────────────────────────
                Log.ForContext("LogType", "Error")
                   .Error(ex,
                       "[ERROR] {Method} {Path} | {ExceptionType}: {Message}",
                       context.Request.Method,
                       context.Request.Path,
                       ex.GetType().Name,
                       ex.Message
                   );
                throw;
            }
            finally
            {
                stopwatch.Stop();

                // ── RESPONSE LOG ─────────────────────────────────
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                var responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                await responseBodyStream.CopyToAsync(originalBodyStream);

                Log.ForContext("LogType", "Response")
                   .Information(
                       "[RESPONSE] {Method} {Path} | Status: {StatusCode} | Duration: {Duration}ms | Body: {Body}",
                       context.Request.Method,
                       context.Request.Path,
                       context.Response.StatusCode,
                       stopwatch.ElapsedMilliseconds,
                       string.IsNullOrWhiteSpace(responseBody) ? "(empty)" : responseBody
                   );
            }
        }

        private static async Task<string> ReadRequestBody(HttpRequest request)
        {
            if (request.ContentLength == null || request.ContentLength == 0)
                return string.Empty;

            request.EnableBuffering();
            request.Body.Seek(0, SeekOrigin.Begin);

            using var reader = new StreamReader(
                request.Body,
                encoding: Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                leaveOpen: true
            );

            var body = await reader.ReadToEndAsync();
            request.Body.Seek(0, SeekOrigin.Begin);
            return body;
        }
    }
}