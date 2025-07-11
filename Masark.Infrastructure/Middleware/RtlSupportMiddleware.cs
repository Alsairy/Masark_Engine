using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.Json;

namespace Masark.Infrastructure.Middleware
{
    public class RtlSupportMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RtlSupportMiddleware> _logger;
        private readonly HashSet<string> _rtlLanguages = new() { "ar", "ar-SA", "he", "he-IL", "fa", "fa-IR", "ur", "ur-PK" };

        public RtlSupportMiddleware(RequestDelegate next, ILogger<RtlSupportMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var currentCulture = CultureInfo.CurrentCulture;
            var currentUICulture = CultureInfo.CurrentUICulture;
            
            var isRtl = IsRightToLeft(currentUICulture.Name) || IsRightToLeft(currentCulture.Name);
            
            if (isRtl)
            {
                context.Response.Headers.Append("Content-Language", currentUICulture.Name);
                context.Response.Headers.Append("X-Text-Direction", "rtl");
                context.Response.Headers.Append("X-Language-Direction", "rtl");
                
                context.Items["IsRtl"] = true;
                context.Items["TextDirection"] = "rtl";
                context.Items["LanguageDirection"] = "rtl";
            }
            else
            {
                context.Response.Headers.Append("Content-Language", currentUICulture.Name);
                context.Response.Headers.Append("X-Text-Direction", "ltr");
                context.Response.Headers.Append("X-Language-Direction", "ltr");
                
                context.Items["IsRtl"] = false;
                context.Items["TextDirection"] = "ltr";
                context.Items["LanguageDirection"] = "ltr";
            }

            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await _next(context);

            if (context.Response.ContentType?.Contains("application/json") == true && responseBody.Length > 0)
            {
                responseBody.Seek(0, SeekOrigin.Begin);
                var responseText = await new StreamReader(responseBody).ReadToEndAsync();
                
                try
                {
                    var jsonDocument = JsonDocument.Parse(responseText);
                    var modifiedJson = AddDirectionMetadata(jsonDocument, isRtl, currentUICulture.Name);
                    
                    var modifiedResponseText = JsonSerializer.Serialize(modifiedJson, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        WriteIndented = false
                    });
                    
                    var modifiedResponseBytes = System.Text.Encoding.UTF8.GetBytes(modifiedResponseText);
                    context.Response.ContentLength = modifiedResponseBytes.Length;
                    
                    await originalBodyStream.WriteAsync(modifiedResponseBytes);
                }
                catch (JsonException)
                {
                    responseBody.Seek(0, SeekOrigin.Begin);
                    await responseBody.CopyToAsync(originalBodyStream);
                }
            }
            else
            {
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
            }
        }

        private bool IsRightToLeft(string cultureName)
        {
            if (string.IsNullOrEmpty(cultureName))
                return false;

            var languageCode = cultureName.Split('-')[0].ToLower();
            return _rtlLanguages.Contains(cultureName.ToLower()) || _rtlLanguages.Contains(languageCode);
        }

        private object AddDirectionMetadata(JsonDocument jsonDocument, bool isRtl, string cultureName)
        {
            var rootElement = jsonDocument.RootElement;
            var result = new Dictionary<string, object>();

            foreach (var property in rootElement.EnumerateObject())
            {
                result[property.Name] = GetJsonValue(property.Value);
            }

            result["_localization"] = new
            {
                language = cultureName,
                direction = isRtl ? "rtl" : "ltr",
                isRtl = isRtl,
                textAlign = isRtl ? "right" : "left",
                fontFamily = GetFontFamily(cultureName),
                numeralSystem = GetNumeralSystem(cultureName)
            };

            return result;
        }

        private object GetJsonValue(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.TryGetInt32(out var intValue) ? intValue : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                JsonValueKind.Object => element.EnumerateObject().ToDictionary(p => p.Name, p => GetJsonValue(p.Value)),
                JsonValueKind.Array => element.EnumerateArray().Select(GetJsonValue).ToArray(),
                _ => element.ToString()
            };
        }

        private string GetFontFamily(string cultureName)
        {
            var languageCode = cultureName.Split('-')[0].ToLower();
            return languageCode switch
            {
                "ar" => "Tahoma, 'Noto Sans Arabic', Arial, sans-serif",
                "he" => "'Noto Sans Hebrew', Arial, sans-serif",
                "fa" => "'Noto Sans Persian', Arial, sans-serif",
                "ur" => "'Noto Sans Urdu', Arial, sans-serif",
                _ => "Arial, sans-serif"
            };
        }

        private string GetNumeralSystem(string cultureName)
        {
            var languageCode = cultureName.Split('-')[0].ToLower();
            return languageCode switch
            {
                "ar" => "arab",
                "fa" => "persian",
                _ => "latin"
            };
        }
    }

    public static class RtlSupportMiddlewareExtensions
    {
        public static IApplicationBuilder UseRtlSupport(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RtlSupportMiddleware>();
        }
    }
}
