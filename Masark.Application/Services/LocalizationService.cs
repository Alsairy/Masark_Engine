using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Masark.Application.Services
{
    public enum Language
    {
        English,
        Arabic
    }

    public enum TextDirection
    {
        LeftToRight,
        RightToLeft
    }

    public class LanguageConfig
    {
        public Language Language { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string NativeName { get; set; } = string.Empty;
        public TextDirection Direction { get; set; }
        public string CultureCode { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
    }

    public class LocalizationOptions
    {
        public string DefaultLanguage { get; set; } = "en";
        public List<string> SupportedLanguages { get; set; } = new() { "en", "ar" };
        public bool EnableRtlSupport { get; set; } = true;
        public string TranslationsPath { get; set; } = "Translations";
    }

    public interface ILocalizationService
    {
        Task<string> GetTextAsync(string key, string language = "en", Dictionary<string, object>? parameters = null);
        Task<Dictionary<string, string>> GetTranslationsAsync(string category, string language = "en");
        Task<List<LanguageConfig>> GetSupportedLanguagesAsync();
        Task<LanguageConfig> GetLanguageConfigAsync(string languageCode);
        string GetLanguageFromHeaders(Dictionary<string, string> headers, Dictionary<string, string> queryParams);
        TextDirection GetTextDirection(string languageCode);
        string FormatNumber(double number, string languageCode);
        string FormatCurrency(decimal amount, string languageCode, string currencyCode = "SAR");
        string FormatDate(DateTime date, string languageCode, string format = "short");
        Task<bool> IsLanguageSupportedAsync(string languageCode);
        Task LoadTranslationsAsync();
        Task<Dictionary<string, object>> GetLanguageMetadataAsync(string languageCode);
    }

    public class LocalizationService : ILocalizationService
    {
        private readonly ILogger<LocalizationService> _logger;
        private readonly LocalizationOptions _options;
        private readonly Dictionary<string, LanguageConfig> _languageConfigs;
        private readonly Dictionary<string, Dictionary<string, Dictionary<string, string>>> _translations;

        public LocalizationService(ILogger<LocalizationService> logger, IOptions<LocalizationOptions> options)
        {
            _logger = logger;
            _options = options.Value;
            _languageConfigs = new Dictionary<string, LanguageConfig>();
            _translations = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
            
            InitializeLanguageConfigs();
            InitializeTranslations();
        }

        private void InitializeLanguageConfigs()
        {
            _languageConfigs["en"] = new LanguageConfig
            {
                Language = Language.English,
                Code = "en",
                Name = "English",
                NativeName = "English",
                Direction = TextDirection.LeftToRight,
                CultureCode = "en-US",
                IsDefault = true
            };

            _languageConfigs["ar"] = new LanguageConfig
            {
                Language = Language.Arabic,
                Code = "ar",
                Name = "Arabic",
                NativeName = "العربية",
                Direction = TextDirection.RightToLeft,
                CultureCode = "ar-SA",
                IsDefault = false
            };
        }

        private void InitializeTranslations()
        {
            _translations["system"] = new Dictionary<string, Dictionary<string, string>>
            {
                ["en"] = new Dictionary<string, string>
                {
                    ["welcome"] = "Welcome to Masark",
                    ["loading"] = "Loading...",
                    ["error"] = "An error occurred",
                    ["success"] = "Operation completed successfully",
                    ["validation_error"] = "Please check your input",
                    ["server_error"] = "Server error occurred",
                    ["not_found"] = "Resource not found",
                    ["unauthorized"] = "Unauthorized access",
                    ["forbidden"] = "Access forbidden"
                },
                ["ar"] = new Dictionary<string, string>
                {
                    ["welcome"] = "مرحباً بك في مسارك",
                    ["loading"] = "جاري التحميل...",
                    ["error"] = "حدث خطأ",
                    ["success"] = "تمت العملية بنجاح",
                    ["validation_error"] = "يرجى التحقق من المدخلات",
                    ["server_error"] = "حدث خطأ في الخادم",
                    ["not_found"] = "المورد غير موجود",
                    ["unauthorized"] = "وصول غير مصرح",
                    ["forbidden"] = "الوصول محظور"
                }
            };

            _translations["auth"] = new Dictionary<string, Dictionary<string, string>>
            {
                ["en"] = new Dictionary<string, string>
                {
                    ["login"] = "Login",
                    ["logout"] = "Logout",
                    ["register"] = "Register",
                    ["email"] = "Email",
                    ["password"] = "Password",
                    ["confirm_password"] = "Confirm Password",
                    ["forgot_password"] = "Forgot Password?",
                    ["reset_password"] = "Reset Password",
                    ["login_success"] = "Login successful",
                    ["login_failed"] = "Login failed",
                    ["invalid_credentials"] = "Invalid email or password",
                    ["account_locked"] = "Account is locked",
                    ["password_reset_sent"] = "Password reset email sent"
                },
                ["ar"] = new Dictionary<string, string>
                {
                    ["login"] = "تسجيل الدخول",
                    ["logout"] = "تسجيل الخروج",
                    ["register"] = "إنشاء حساب",
                    ["email"] = "البريد الإلكتروني",
                    ["password"] = "كلمة المرور",
                    ["confirm_password"] = "تأكيد كلمة المرور",
                    ["forgot_password"] = "نسيت كلمة المرور؟",
                    ["reset_password"] = "إعادة تعيين كلمة المرور",
                    ["login_success"] = "تم تسجيل الدخول بنجاح",
                    ["login_failed"] = "فشل تسجيل الدخول",
                    ["invalid_credentials"] = "البريد الإلكتروني أو كلمة المرور غير صحيحة",
                    ["account_locked"] = "الحساب مقفل",
                    ["password_reset_sent"] = "تم إرسال رابط إعادة تعيين كلمة المرور"
                }
            };

            _translations["assessment"] = new Dictionary<string, Dictionary<string, string>>
            {
                ["en"] = new Dictionary<string, string>
                {
                    ["start_assessment"] = "Start Assessment",
                    ["continue_assessment"] = "Continue Assessment",
                    ["complete_assessment"] = "Complete Assessment",
                    ["question"] = "Question",
                    ["of"] = "of",
                    ["next"] = "Next",
                    ["previous"] = "Previous",
                    ["submit"] = "Submit",
                    ["assessment_completed"] = "Assessment completed successfully",
                    ["assessment_progress"] = "Assessment Progress",
                    ["time_remaining"] = "Time Remaining",
                    ["save_progress"] = "Save Progress",
                    ["resume_later"] = "Resume Later"
                },
                ["ar"] = new Dictionary<string, string>
                {
                    ["start_assessment"] = "بدء التقييم",
                    ["continue_assessment"] = "متابعة التقييم",
                    ["complete_assessment"] = "إكمال التقييم",
                    ["question"] = "سؤال",
                    ["of"] = "من",
                    ["next"] = "التالي",
                    ["previous"] = "السابق",
                    ["submit"] = "إرسال",
                    ["assessment_completed"] = "تم إكمال التقييم بنجاح",
                    ["assessment_progress"] = "تقدم التقييم",
                    ["time_remaining"] = "الوقت المتبقي",
                    ["save_progress"] = "حفظ التقدم",
                    ["resume_later"] = "المتابعة لاحقاً"
                }
            };

            _translations["careers"] = new Dictionary<string, Dictionary<string, string>>
            {
                ["en"] = new Dictionary<string, string>
                {
                    ["career_matches"] = "Career Matches",
                    ["top_matches"] = "Top Matches",
                    ["match_score"] = "Match Score",
                    ["career_details"] = "Career Details",
                    ["job_description"] = "Job Description",
                    ["required_skills"] = "Required Skills",
                    ["education_requirements"] = "Education Requirements",
                    ["salary_range"] = "Salary Range",
                    ["career_outlook"] = "Career Outlook",
                    ["related_careers"] = "Related Careers",
                    ["programs"] = "Programs",
                    ["pathways"] = "Pathways"
                },
                ["ar"] = new Dictionary<string, string>
                {
                    ["career_matches"] = "المهن المناسبة",
                    ["top_matches"] = "أفضل المطابقات",
                    ["match_score"] = "درجة التطابق",
                    ["career_details"] = "تفاصيل المهنة",
                    ["job_description"] = "وصف الوظيفة",
                    ["required_skills"] = "المهارات المطلوبة",
                    ["education_requirements"] = "المتطلبات التعليمية",
                    ["salary_range"] = "نطاق الراتب",
                    ["career_outlook"] = "توقعات المهنة",
                    ["related_careers"] = "المهن ذات الصلة",
                    ["programs"] = "البرامج",
                    ["pathways"] = "المسارات"
                }
            };

            _translations["reports"] = new Dictionary<string, Dictionary<string, string>>
            {
                ["en"] = new Dictionary<string, string>
                {
                    ["personality_report"] = "Personality Report",
                    ["career_report"] = "Career Report",
                    ["detailed_report"] = "Detailed Report",
                    ["summary_report"] = "Summary Report",
                    ["download_pdf"] = "Download PDF",
                    ["share_report"] = "Share Report",
                    ["print_report"] = "Print Report",
                    ["report_generated"] = "Report generated successfully",
                    ["personality_type"] = "Personality Type",
                    ["strengths"] = "Strengths",
                    ["challenges"] = "Challenges",
                    ["recommendations"] = "Recommendations"
                },
                ["ar"] = new Dictionary<string, string>
                {
                    ["personality_report"] = "تقرير الشخصية",
                    ["career_report"] = "تقرير المهن",
                    ["detailed_report"] = "تقرير مفصل",
                    ["summary_report"] = "تقرير موجز",
                    ["download_pdf"] = "تحميل PDF",
                    ["share_report"] = "مشاركة التقرير",
                    ["print_report"] = "طباعة التقرير",
                    ["report_generated"] = "تم إنشاء التقرير بنجاح",
                    ["personality_type"] = "نوع الشخصية",
                    ["strengths"] = "نقاط القوة",
                    ["challenges"] = "التحديات",
                    ["recommendations"] = "التوصيات"
                }
            };

            _translations["admin"] = new Dictionary<string, Dictionary<string, string>>
            {
                ["en"] = new Dictionary<string, string>
                {
                    ["dashboard"] = "Dashboard",
                    ["users"] = "Users",
                    ["assessments"] = "Assessments",
                    ["reports"] = "Reports",
                    ["settings"] = "Settings",
                    ["analytics"] = "Analytics",
                    ["user_management"] = "User Management",
                    ["system_health"] = "System Health",
                    ["performance_metrics"] = "Performance Metrics",
                    ["audit_logs"] = "Audit Logs"
                },
                ["ar"] = new Dictionary<string, string>
                {
                    ["dashboard"] = "لوحة التحكم",
                    ["users"] = "المستخدمون",
                    ["assessments"] = "التقييمات",
                    ["reports"] = "التقارير",
                    ["settings"] = "الإعدادات",
                    ["analytics"] = "التحليلات",
                    ["user_management"] = "إدارة المستخدمين",
                    ["system_health"] = "صحة النظام",
                    ["performance_metrics"] = "مقاييس الأداء",
                    ["audit_logs"] = "سجلات المراجعة"
                }
            };
        }

        public async Task<string> GetTextAsync(string key, string language = "en", Dictionary<string, object>? parameters = null)
        {
            try
            {
                var parts = key.Split('.');
                if (parts.Length != 2)
                {
                    _logger.LogWarning("Invalid translation key format: {Key}", key);
                    return key;
                }

                var category = parts[0];
                var textKey = parts[1];

                if (!_translations.ContainsKey(category) ||
                    !_translations[category].ContainsKey(language) ||
                    !_translations[category][language].ContainsKey(textKey))
                {
                    _logger.LogWarning("Translation not found: {Key} for language {Language}", key, language);
                    
                    if (language != "en" && _translations.ContainsKey(category) &&
                        _translations[category].ContainsKey("en") &&
                        _translations[category]["en"].ContainsKey(textKey))
                    {
                        return _translations[category]["en"][textKey];
                    }
                    
                    return key;
                }

                var text = _translations[category][language][textKey];

                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        text = text.Replace($"{{{param.Key}}}", param.Value?.ToString() ?? "");
                    }
                }

                return text;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting translation for key {Key}", key);
                return key;
            }
        }

        public async Task<Dictionary<string, string>> GetTranslationsAsync(string category, string language = "en")
        {
            try
            {
                if (_translations.ContainsKey(category) && _translations[category].ContainsKey(language))
                {
                    return await Task.FromResult(_translations[category][language]);
                }

                _logger.LogWarning("Translations not found for category {Category} and language {Language}", category, language);
                return new Dictionary<string, string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting translations for category {Category}", category);
                return new Dictionary<string, string>();
            }
        }

        public async Task<List<LanguageConfig>> GetSupportedLanguagesAsync()
        {
            return await Task.FromResult(_languageConfigs.Values.ToList());
        }

        public async Task<LanguageConfig> GetLanguageConfigAsync(string languageCode)
        {
            if (_languageConfigs.ContainsKey(languageCode))
            {
                return await Task.FromResult(_languageConfigs[languageCode]);
            }

            _logger.LogWarning("Language config not found for code {LanguageCode}", languageCode);
            return await Task.FromResult(_languageConfigs["en"]); // Default to English
        }

        public string GetLanguageFromHeaders(Dictionary<string, string> headers, Dictionary<string, string> queryParams)
        {
            try
            {
                if (queryParams.ContainsKey("lang"))
                {
                    var langParam = queryParams["lang"];
                    if (_options.SupportedLanguages.Contains(langParam))
                    {
                        return langParam;
                    }
                }

                if (headers.ContainsKey("X-Language"))
                {
                    var langHeader = headers["X-Language"];
                    if (_options.SupportedLanguages.Contains(langHeader))
                    {
                        return langHeader;
                    }
                }

                if (headers.ContainsKey("Accept-Language"))
                {
                    var acceptLanguage = headers["Accept-Language"];
                    var preferredLanguages = acceptLanguage.Split(',')
                        .Select(lang => lang.Split(';')[0].Trim().ToLower())
                        .ToList();

                    foreach (var lang in preferredLanguages)
                    {
                        if (_options.SupportedLanguages.Contains(lang))
                        {
                            return lang;
                        }
                        
                        var langPrefix = lang.Split('-')[0];
                        if (_options.SupportedLanguages.Contains(langPrefix))
                        {
                            return langPrefix;
                        }
                    }
                }

                return _options.DefaultLanguage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error determining language from headers");
                return _options.DefaultLanguage;
            }
        }

        public TextDirection GetTextDirection(string languageCode)
        {
            if (_languageConfigs.ContainsKey(languageCode))
            {
                return _languageConfigs[languageCode].Direction;
            }

            return TextDirection.LeftToRight;
        }

        public string FormatNumber(double number, string languageCode)
        {
            try
            {
                var cultureCode = _languageConfigs.ContainsKey(languageCode) 
                    ? _languageConfigs[languageCode].CultureCode 
                    : "en-US";
                
                var culture = new CultureInfo(cultureCode);
                return number.ToString("N2", culture);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error formatting number {Number} for language {Language}", number, languageCode);
                return number.ToString("N2");
            }
        }

        public string FormatCurrency(decimal amount, string languageCode, string currencyCode = "SAR")
        {
            try
            {
                var cultureCode = _languageConfigs.ContainsKey(languageCode) 
                    ? _languageConfigs[languageCode].CultureCode 
                    : "en-US";
                
                var culture = new CultureInfo(cultureCode);
                
                if (currencyCode == "SAR")
                {
                    return languageCode == "ar" 
                        ? $"{amount:N2} ريال" 
                        : $"SAR {amount:N2}";
                }
                
                return amount.ToString("C", culture);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error formatting currency {Amount} for language {Language}", amount, languageCode);
                return amount.ToString("C");
            }
        }

        public string FormatDate(DateTime date, string languageCode, string format = "short")
        {
            try
            {
                var cultureCode = _languageConfigs.ContainsKey(languageCode) 
                    ? _languageConfigs[languageCode].CultureCode 
                    : "en-US";
                
                var culture = new CultureInfo(cultureCode);
                
                return format.ToLower() switch
                {
                    "short" => date.ToString("d", culture),
                    "long" => date.ToString("D", culture),
                    "datetime" => date.ToString("g", culture),
                    "full" => date.ToString("F", culture),
                    _ => date.ToString(format, culture)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error formatting date {Date} for language {Language}", date, languageCode);
                return date.ToString("d");
            }
        }

        public async Task<bool> IsLanguageSupportedAsync(string languageCode)
        {
            return await Task.FromResult(_options.SupportedLanguages.Contains(languageCode));
        }

        public async Task LoadTranslationsAsync()
        {
            try
            {
                _logger.LogInformation("Loading translations from memory");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading translations");
            }
        }

        public async Task<Dictionary<string, object>> GetLanguageMetadataAsync(string languageCode)
        {
            try
            {
                if (!_languageConfigs.ContainsKey(languageCode))
                {
                    return new Dictionary<string, object>();
                }

                var config = _languageConfigs[languageCode];
                var translationCount = _translations.Values
                    .Where(category => category.ContainsKey(languageCode))
                    .Sum(category => category[languageCode].Count);

                return await Task.FromResult(new Dictionary<string, object>
                {
                    ["code"] = config.Code,
                    ["name"] = config.Name,
                    ["native_name"] = config.NativeName,
                    ["direction"] = config.Direction.ToString().ToLower(),
                    ["culture_code"] = config.CultureCode,
                    ["is_default"] = config.IsDefault,
                    ["is_rtl"] = config.Direction == TextDirection.RightToLeft,
                    ["translation_count"] = translationCount,
                    ["categories"] = _translations.Keys.Where(k => _translations[k].ContainsKey(languageCode)).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting language metadata for {LanguageCode}", languageCode);
                return new Dictionary<string, object>();
            }
        }
    }
}
