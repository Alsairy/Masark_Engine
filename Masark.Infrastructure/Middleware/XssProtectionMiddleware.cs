using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;

namespace Masark.Infrastructure.Middleware
{
    public class XssProtectionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<XssProtectionMiddleware> _logger;
        private readonly HashSet<string> _xssPatterns;
        private readonly Regex _xssRegex;

        public XssProtectionMiddleware(RequestDelegate next, ILogger<XssProtectionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
            
            _xssPatterns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "<script",
                "</script>",
                "javascript:",
                "vbscript:",
                "data:",
                "onload=",
                "onerror=",
                "onclick=",
                "onmouseover=",
                "onfocus=",
                "onblur=",
                "onchange=",
                "onsubmit=",
                "eval(",
                "expression(",
                "url(",
                "import(",
                "document.cookie",
                "document.write",
                "window.location",
                "alert(",
                "confirm(",
                "prompt(",
                "setTimeout(",
                "setInterval(",
                "Function(",
                "constructor",
                "prototype",
                "__proto__",
                "innerHTML",
                "outerHTML",
                "insertAdjacentHTML",
                "document.createElement",
                "appendChild",
                "removeChild",
                "replaceChild"
            };

            var patterns = string.Join("|", _xssPatterns.Select(Regex.Escape));
            _xssRegex = new Regex(patterns, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var requestId = context.Items["RequestId"]?.ToString() ?? Guid.NewGuid().ToString();

            try
            {
                if (await ContainsXssAsync(context))
                {
                    await HandleXssDetectedAsync(context, requestId);
                    return;
                }

                context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
                context.Response.Headers.Add("X-Frame-Options", "DENY");
                context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
                context.Response.Headers.Add("Content-Security-Policy", 
                    "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; font-src 'self' https:; connect-src 'self' https:; frame-ancestors 'none';");

                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "XSS Protection: Error processing request");
                await HandleXssDetectedAsync(context, requestId);
            }
        }

        private async Task<bool> ContainsXssAsync(HttpContext context)
        {
            if (await CheckQueryStringAsync(context.Request.Query))
                return true;

            if (await CheckHeadersAsync(context.Request.Headers))
                return true;

            if (context.Request.HasFormContentType)
            {
                if (await CheckFormAsync(context.Request.Form))
                    return true;
            }

            if (context.Request.ContentType?.Contains("application/json") == true)
            {
                if (await CheckJsonBodyAsync(context))
                    return true;
            }

            return false;
        }

        private async Task<bool> CheckQueryStringAsync(IQueryCollection query)
        {
            foreach (var param in query)
            {
                if (ContainsXssPattern(param.Key) || param.Value.Any(ContainsXssPattern))
                {
                    _logger.LogWarning("XSS detected in query parameter");
                    return true;
                }
            }
            return false;
        }

        private async Task<bool> CheckHeadersAsync(IHeaderDictionary headers)
        {
            var headersToCheck = new[] { "User-Agent", "Referer", "X-Forwarded-For", "X-Real-IP" };
            
            foreach (var headerName in headersToCheck)
            {
                if (headers.ContainsKey(headerName))
                {
                    var headerValue = headers[headerName].ToString();
                    if (ContainsXssPattern(headerValue))
                    {
                        _logger.LogWarning("XSS detected in header");
                        return true;
                    }
                }
            }
            return false;
        }

        private async Task<bool> CheckFormAsync(IFormCollection form)
        {
            foreach (var field in form)
            {
                if (ContainsXssPattern(field.Key) || field.Value.Any(ContainsXssPattern))
                {
                    _logger.LogWarning("XSS detected in form field");
                    return true;
                }
            }
            return false;
        }

        private async Task<bool> CheckJsonBodyAsync(HttpContext context)
        {
            try
            {
                context.Request.EnableBuffering();
                var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
                context.Request.Body.Position = 0;

                if (string.IsNullOrEmpty(body))
                    return false;

                if (ContainsXssPattern(body))
                {
                    _logger.LogWarning("XSS detected in JSON body");
                    return true;
                }

                try
                {
                    var jsonDoc = JsonDocument.Parse(body);
                    if (await CheckJsonElementAsync(jsonDoc.RootElement))
                    {
                        _logger.LogWarning("XSS detected in JSON content");
                        return true;
                    }
                }
                catch (JsonException)
                {
                    _logger.LogWarning("Invalid JSON format detected");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking JSON body for XSS");
                return true;
            }
        }

        private async Task<bool> CheckJsonElementAsync(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    return ContainsXssPattern(element.GetString() ?? "");
                
                case JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject())
                    {
                        if (ContainsXssPattern(property.Name) || await CheckJsonElementAsync(property.Value))
                            return true;
                    }
                    break;
                
                case JsonValueKind.Array:
                    foreach (var item in element.EnumerateArray())
                    {
                        if (await CheckJsonElementAsync(item))
                            return true;
                    }
                    break;
            }
            
            return false;
        }

        private bool ContainsXssPattern(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            var decodedInput = HttpUtility.HtmlDecode(input);
            var urlDecodedInput = HttpUtility.UrlDecode(input);

            return _xssRegex.IsMatch(input) || 
                   _xssRegex.IsMatch(decodedInput) || 
                   _xssRegex.IsMatch(urlDecodedInput);
        }

        private async Task HandleXssDetectedAsync(HttpContext context, string requestId)
        {
            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = context.Request.Headers["User-Agent"].ToString();
            
            _logger.LogWarning("XSS attack detected");

            context.Response.StatusCode = 400;
            context.Response.ContentType = "application/json";

            var response = new
            {
                error = "Bad Request",
                message = "Request contains potentially malicious content",
                requestId = requestId,
                timestamp = DateTime.UtcNow.ToString("O")
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }

    public static class XssProtectionMiddlewareExtensions
    {
        public static IApplicationBuilder UseXssProtection(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<XssProtectionMiddleware>();
        }
    }
}
