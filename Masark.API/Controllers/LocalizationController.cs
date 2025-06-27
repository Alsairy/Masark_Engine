using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Masark.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LocalizationController : ControllerBase
    {
        private readonly ILogger<LocalizationController> _logger;

        public LocalizationController(ILogger<LocalizationController> logger)
        {
            _logger = logger;
        }

        [HttpGet("languages")]
        public async Task<IActionResult> GetSupportedLanguages()
        {
            try
            {
                var languages = new[]
                {
                    new
                    {
                        code = "en",
                        name = "English",
                        native_name = "English",
                        direction = "ltr",
                        locale = "en-US",
                        font_family = "Arial, sans-serif"
                    },
                    new
                    {
                        code = "ar",
                        name = "Arabic",
                        native_name = "العربية",
                        direction = "rtl",
                        locale = "ar-SA",
                        font_family = "Tahoma, Arial, sans-serif"
                    }
                };

                return Ok(new
                {
                    success = true,
                    languages = languages,
                    default_language = "en"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get supported languages error");
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to get supported languages",
                    message = ex.Message
                });
            }
        }

        [HttpGet("config")]
        public async Task<IActionResult> GetLanguageConfig([FromQuery] string? lang = null)
        {
            try
            {
                var language = DetermineLanguage(lang);

                var config = language == "ar" ? new
                {
                    code = "ar",
                    name = "Arabic",
                    native_name = "العربية",
                    direction = "rtl",
                    locale = "ar-SA",
                    font_family = "Tahoma, Arial, sans-serif",
                    date_format = "dd/MM/yyyy",
                    time_format = "HH:mm",
                    decimal_separator = ".",
                    thousands_separator = ","
                } : new
                {
                    code = "en",
                    name = "English",
                    native_name = "English",
                    direction = "ltr",
                    locale = "en-US",
                    font_family = "Arial, sans-serif",
                    date_format = "MM/dd/yyyy",
                    time_format = "h:mm tt",
                    decimal_separator = ".",
                    thousands_separator = ","
                };

                return Ok(new
                {
                    success = true,
                    language_config = config
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get language config error");
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to get language configuration",
                    message = ex.Message
                });
            }
        }

        [HttpGet("translations")]
        public async Task<IActionResult> GetTranslations([FromQuery] string? lang = null, 
                                                        [FromQuery] string? category = null, 
                                                        [FromQuery] string? categories = null)
        {
            try
            {
                var language = DetermineLanguage(lang);

                var requestedCategories = new List<string>();
                if (!string.IsNullOrWhiteSpace(categories))
                {
                    requestedCategories = categories.Split(',').Select(c => c.Trim()).ToList();
                }
                else if (!string.IsNullOrWhiteSpace(category))
                {
                    requestedCategories.Add(category);
                }
                else
                {
                    requestedCategories = new List<string> { "system", "auth", "assessment", "careers", "reports", "admin", "personality_types" };
                }

                var translations = new Dictionary<string, object>();
                foreach (var cat in requestedCategories)
                {
                    translations[cat] = new { };
                }

                var config = language == "ar" ? new
                {
                    code = "ar",
                    name = "Arabic",
                    native_name = "العربية",
                    direction = "rtl"
                } : new
                {
                    code = "en",
                    name = "English",
                    native_name = "English",
                    direction = "ltr"
                };

                return Ok(new
                {
                    success = true,
                    language = language,
                    translations = translations,
                    language_config = config
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get translations error");
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to get translations",
                    message = ex.Message
                });
            }
        }

        [HttpPost("translate")]
        public async Task<IActionResult> TranslateText([FromBody] TranslateTextRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "Invalid request data",
                        details = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                    });
                }

                if (request.Keys == null || !request.Keys.Any())
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "Keys array is required"
                    });
                }

                var language = DetermineLanguage(request.Language);
                var category = request.Category ?? "system";

                var translations = new Dictionary<string, string>();
                foreach (var key in request.Keys)
                {
                    translations[key] = $"[{language.ToUpper()}] {key}"; // Placeholder
                }

                return Ok(new
                {
                    success = true,
                    language = language,
                    category = category,
                    translations = translations
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Translate text error");
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to translate text",
                    message = ex.Message
                });
            }
        }

        [HttpPost("format")]
        public async Task<IActionResult> FormatLocalizedContent([FromBody] FormatContentRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "Invalid request data",
                        details = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                    });
                }

                if (!double.TryParse(request.Value?.ToString(), out var numericValue))
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "Value must be a number"
                    });
                }

                var language = DetermineLanguage(request.Language);
                var formatType = request.Type?.ToLower() ?? "number";

                string formattedValue;
                if (formatType == "percentage")
                {
                    formattedValue = language == "ar" ? $"%{numericValue:F1}" : $"{numericValue:F1}%";
                }
                else
                {
                    formattedValue = numericValue.ToString("N2");
                }

                return Ok(new
                {
                    success = true,
                    type = formatType,
                    original_value = numericValue,
                    formatted_value = formattedValue,
                    language = language
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Format localized content error");
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to format content",
                    message = ex.Message
                });
            }
        }

        [HttpPost("localize")]
        public async Task<IActionResult> LocalizeContent([FromBody] LocalizeContentRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "Invalid request data",
                        details = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                    });
                }

                if (request.Content == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "Content object is required"
                    });
                }

                var language = DetermineLanguage(request.Language);

                var localizedContent = new Dictionary<string, object>();
                foreach (var kvp in request.Content)
                {
                    if (kvp.Key.EndsWith("_en") && language == "en")
                    {
                        var baseKey = kvp.Key.Substring(0, kvp.Key.Length - 3);
                        localizedContent[baseKey] = kvp.Value;
                    }
                    else if (kvp.Key.EndsWith("_ar") && language == "ar")
                    {
                        var baseKey = kvp.Key.Substring(0, kvp.Key.Length - 3);
                        localizedContent[baseKey] = kvp.Value;
                    }
                    else if (!kvp.Key.EndsWith("_en") && !kvp.Key.EndsWith("_ar"))
                    {
                        localizedContent[kvp.Key] = kvp.Value;
                    }
                }

                var config = language == "ar" ? new
                {
                    code = "ar",
                    direction = "rtl"
                } : new
                {
                    code = "en",
                    direction = "ltr"
                };

                return Ok(new
                {
                    success = true,
                    language = language,
                    original_content = request.Content,
                    localized_content = localizedContent,
                    language_config = config
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Localize content error");
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to localize content",
                    message = ex.Message
                });
            }
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetLocalizationStats()
        {
            try
            {
                var translationStats = new
                {
                    en = new
                    {
                        categories = new
                        {
                            system = 50,
                            auth = 25,
                            assessment = 100,
                            careers = 75,
                            reports = 30,
                            admin = 40,
                            personality_types = 16
                        },
                        total = 336
                    },
                    ar = new
                    {
                        categories = new
                        {
                            system = 50,
                            auth = 25,
                            assessment = 100,
                            careers = 75,
                            reports = 30,
                            admin = 40,
                            personality_types = 16
                        },
                        total = 336
                    }
                };

                var languageConfigs = new
                {
                    en = new
                    {
                        name = "English",
                        native_name = "English",
                        direction = "ltr",
                        locale = "en-US",
                        font_family = "Arial, sans-serif"
                    },
                    ar = new
                    {
                        name = "Arabic",
                        native_name = "العربية",
                        direction = "rtl",
                        locale = "ar-SA",
                        font_family = "Tahoma, Arial, sans-serif"
                    }
                };

                return Ok(new
                {
                    success = true,
                    statistics = new
                    {
                        supported_languages = 2,
                        total_translations = 672,
                        default_language = "en",
                        translation_stats = translationStats,
                        language_configs = languageConfigs
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get localization stats error");
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to get localization statistics",
                    message = ex.Message
                });
            }
        }

        private string DetermineLanguage(string? langParam)
        {
            var lang = langParam?.ToLower();
            return lang switch
            {
                "ar" or "arabic" => "ar",
                "en" or "english" => "en",
                _ => "en"
            };
        }
    }

    public class TranslateTextRequest
    {
        [Required]
        public List<string> Keys { get; set; } = new();

        public string? Category { get; set; } = "system";

        public string? Language { get; set; }
    }

    public class FormatContentRequest
    {
        public string? Type { get; set; } = "number";

        [Required]
        public object? Value { get; set; }

        public string? Language { get; set; }
    }

    public class LocalizeContentRequest
    {
        [Required]
        public Dictionary<string, object> Content { get; set; } = new();

        public string? Language { get; set; }
    }
}
