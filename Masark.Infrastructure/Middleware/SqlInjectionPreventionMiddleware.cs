using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;

namespace Masark.Infrastructure.Middleware
{
    public class SqlInjectionPreventionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SqlInjectionPreventionMiddleware> _logger;
        private readonly HashSet<string> _sqlPatterns;
        private readonly Regex _sqlInjectionRegex;

        public SqlInjectionPreventionMiddleware(RequestDelegate next, ILogger<SqlInjectionPreventionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
            
            _sqlPatterns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "' OR '1'='1",
                "' OR 1=1",
                "\" OR \"1\"=\"1",
                "\" OR 1=1",
                "'; DROP TABLE",
                "'; DELETE FROM",
                "'; INSERT INTO",
                "'; UPDATE ",
                "'; CREATE ",
                "'; ALTER ",
                "UNION SELECT",
                "UNION ALL SELECT",
                "SELECT * FROM",
                "INSERT INTO",
                "DELETE FROM",
                "UPDATE SET",
                "DROP TABLE",
                "DROP DATABASE",
                "CREATE TABLE",
                "ALTER TABLE",
                "EXEC(",
                "EXECUTE(",
                "sp_executesql",
                "xp_cmdshell",
                "sp_configure",
                "sp_addlogin",
                "sp_password",
                "OPENROWSET",
                "OPENDATASOURCE",
                "BULK INSERT",
                "LOAD_FILE",
                "INTO OUTFILE",
                "INTO DUMPFILE",
                "SCRIPT>",
                "WAITFOR DELAY",
                "BENCHMARK(",
                "SLEEP(",
                "pg_sleep(",
                "DBMS_PIPE.RECEIVE_MESSAGE",
                "UTL_INADDR.GET_HOST_ADDRESS",
                "SYS.DBMS_EXPORT_EXTENSION",
                "EXTRACTVALUE",
                "UPDATEXML",
                "AND 1=1",
                "OR 1=1",
                "AND 1=2",
                "OR 1=2",
                "HAVING 1=1",
                "GROUP BY",
                "ORDER BY",
                "LIMIT 1,1",
                "OFFSET ",
                "INFORMATION_SCHEMA",
                "SYSOBJECTS",
                "SYSCOLUMNS",
                "MSysAccessObjects",
                "MSysQueries",
                "DUAL",
                "ALL_TABLES",
                "USER_TABLES",
                "DBA_TABLES",
                "V$VERSION",
                "@@VERSION",
                "VERSION()",
                "CURRENT_USER",
                "USER()",
                "SYSTEM_USER",
                "SESSION_USER",
                "DATABASE()",
                "SCHEMA()",
                "CONCAT(",
                "CHAR(",
                "ASCII(",
                "SUBSTRING(",
                "MID(",
                "LENGTH(",
                "COUNT(*)",
                "HEX(",
                "UNHEX(",
                "LOAD_FILE(",
                "FILE_PRIV"
            };

            var patterns = string.Join("|", _sqlPatterns.Select(p => Regex.Escape(p)));
            _sqlInjectionRegex = new Regex(patterns, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var requestId = context.Items["RequestId"]?.ToString() ?? Guid.NewGuid().ToString();

            try
            {
                if (IsHealthCheckEndpoint(context.Request.Path))
                {
                    await _next(context);
                    return;
                }

                if (await ContainsSqlInjectionAsync(context))
                {
                    await HandleSqlInjectionDetectedAsync(context, requestId);
                    return;
                }

                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SQL Injection Prevention: Error processing request {RequestId}", requestId);
                await HandleSqlInjectionDetectedAsync(context, requestId);
            }
        }

        private async Task<bool> ContainsSqlInjectionAsync(HttpContext context)
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
                if (ContainsSqlInjectionPattern(param.Key) || param.Value.Any(ContainsSqlInjectionPattern))
                {
                    _logger.LogWarning("SQL injection detected in query parameter: {Key}", param.Key);
                    return true;
                }
            }
            return false;
        }

        private async Task<bool> CheckHeadersAsync(IHeaderDictionary headers)
        {
            var headersToCheck = new[] { "User-Agent", "Referer", "X-Forwarded-For", "X-Real-IP", "Authorization" };
            
            foreach (var headerName in headersToCheck)
            {
                if (headers.ContainsKey(headerName))
                {
                    var headerValue = headers[headerName].ToString();
                    if (ContainsSqlInjectionPattern(headerValue))
                    {
                        _logger.LogWarning("SQL injection detected in header: {Header}", headerName);
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
                if (ContainsSqlInjectionPattern(field.Key) || field.Value.Any(ContainsSqlInjectionPattern))
                {
                    _logger.LogWarning("SQL injection detected in form field: {Field}", field.Key);
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

                if (ContainsSqlInjectionPattern(body))
                {
                    _logger.LogWarning("SQL injection detected in JSON body");
                    return true;
                }

                try
                {
                    var jsonDoc = JsonDocument.Parse(body);
                    if (await CheckJsonElementAsync(jsonDoc.RootElement))
                    {
                        _logger.LogWarning("SQL injection detected in JSON content");
                        return true;
                    }
                }
                catch (JsonException)
                {
                    return false;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking JSON body for SQL injection");
                return true;
            }
        }

        private async Task<bool> CheckJsonElementAsync(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    return ContainsSqlInjectionPattern(element.GetString() ?? "");
                
                case JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject())
                    {
                        if (ContainsSqlInjectionPattern(property.Name) || await CheckJsonElementAsync(property.Value))
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

        private bool ContainsSqlInjectionPattern(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            var decodedInput = HttpUtility.HtmlDecode(input);
            var urlDecodedInput = HttpUtility.UrlDecode(input);
            
            var normalizedInput = input.Replace(" ", "").Replace("\t", "").Replace("\n", "").Replace("\r", "");
            var normalizedDecoded = decodedInput.Replace(" ", "").Replace("\t", "").Replace("\n", "").Replace("\r", "");
            var normalizedUrlDecoded = urlDecodedInput.Replace(" ", "").Replace("\t", "").Replace("\n", "").Replace("\r", "");

            return _sqlInjectionRegex.IsMatch(input) || 
                   _sqlInjectionRegex.IsMatch(decodedInput) || 
                   _sqlInjectionRegex.IsMatch(urlDecodedInput) ||
                   _sqlInjectionRegex.IsMatch(normalizedInput) ||
                   _sqlInjectionRegex.IsMatch(normalizedDecoded) ||
                   _sqlInjectionRegex.IsMatch(normalizedUrlDecoded) ||
                   ContainsAdvancedSqlInjectionPattern(input);
        }

        private bool ContainsAdvancedSqlInjectionPattern(string input)
        {
            var suspiciousPatterns = new[]
            {
                @"(\w+)\s*=\s*\1",
                @"'\s*OR\s*'\w*'\s*=\s*'\w*'",
                @"""\s*OR\s*""\w*""\s*=\s*""\w*""",
                @";\s*(DROP|DELETE|INSERT|UPDATE|CREATE|ALTER)\s+",
                @"UNION\s+(ALL\s+)?SELECT",
                @"(AND|OR)\s+\d+\s*=\s*\d+",
                @"(HAVING|WHERE)\s+\d+\s*=\s*\d+",
                @"'\s*;\s*--",
                @"--\s*$",
                @"/\*.*\*/",
                @"0x[0-9a-fA-F]+",
                @"CHAR\s*\(\s*\d+\s*\)",
                @"WAITFOR\s+DELAY\s+",
                @"BENCHMARK\s*\(",
                @"SLEEP\s*\(",
                @"pg_sleep\s*\("
            };

            return suspiciousPatterns.Any(pattern => Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase));
        }

        private bool IsHealthCheckEndpoint(PathString path)
        {
            var healthCheckPaths = new[]
            {
                "/health",
                "/health/ready",
                "/health/live",
                "/health/db",
                "/health/cache",
                "/health/system",
                "/api/system/health"
            };

            return healthCheckPaths.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));
        }

        private async Task HandleSqlInjectionDetectedAsync(HttpContext context, string requestId)
        {
            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = context.Request.Headers["User-Agent"].ToString();
            
            _logger.LogWarning("SQL injection attack detected - Request: {RequestId}, IP: {ClientIp}, UserAgent: {UserAgent}, Path: {Path}",
                requestId, clientIp, userAgent, context.Request.Path);

            context.Response.StatusCode = 400;
            context.Response.ContentType = "application/json";

            var response = new
            {
                error = "Bad Request",
                message = "Request contains potentially malicious SQL patterns",
                requestId = requestId,
                timestamp = DateTime.UtcNow.ToString("O")
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }

    public static class SqlInjectionPreventionMiddlewareExtensions
    {
        public static IApplicationBuilder UseSqlInjectionPrevention(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SqlInjectionPreventionMiddleware>();
        }
    }
}
