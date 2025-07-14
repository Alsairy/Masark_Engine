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
        Arabic,
        Spanish,
        Chinese
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
        public List<string> SupportedLanguages { get; set; } = new() { "en", "ar", "es", "zh" };
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

            _languageConfigs["es"] = new LanguageConfig
            {
                Language = Language.Spanish,
                Code = "es",
                Name = "Spanish",
                NativeName = "Español",
                Direction = TextDirection.LeftToRight,
                CultureCode = "es-ES",
                IsDefault = false
            };

            _languageConfigs["zh"] = new LanguageConfig
            {
                Language = Language.Chinese,
                Code = "zh",
                Name = "Chinese",
                NativeName = "中文",
                Direction = TextDirection.LeftToRight,
                CultureCode = "zh-CN",
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
                },
                ["es"] = new Dictionary<string, string>
                {
                    ["welcome"] = "Bienvenido a Masark",
                    ["loading"] = "Cargando...",
                    ["error"] = "Ocurrió un error",
                    ["success"] = "Operación completada exitosamente",
                    ["validation_error"] = "Por favor verifica tu entrada",
                    ["server_error"] = "Ocurrió un error del servidor",
                    ["not_found"] = "Recurso no encontrado",
                    ["unauthorized"] = "Acceso no autorizado",
                    ["forbidden"] = "Acceso prohibido"
                },
                ["zh"] = new Dictionary<string, string>
                {
                    ["welcome"] = "欢迎来到Masark",
                    ["loading"] = "加载中...",
                    ["error"] = "发生错误",
                    ["success"] = "操作成功完成",
                    ["validation_error"] = "请检查您的输入",
                    ["server_error"] = "服务器错误",
                    ["not_found"] = "资源未找到",
                    ["unauthorized"] = "未授权访问",
                    ["forbidden"] = "访问被禁止"
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
                },
                ["es"] = new Dictionary<string, string>
                {
                    ["login"] = "Iniciar sesión",
                    ["logout"] = "Cerrar sesión",
                    ["register"] = "Registrarse",
                    ["email"] = "Correo electrónico",
                    ["password"] = "Contraseña",
                    ["confirm_password"] = "Confirmar contraseña",
                    ["forgot_password"] = "¿Olvidaste tu contraseña?",
                    ["reset_password"] = "Restablecer contraseña",
                    ["login_success"] = "Inicio de sesión exitoso",
                    ["login_failed"] = "Error al iniciar sesión",
                    ["invalid_credentials"] = "Correo electrónico o contraseña inválidos",
                    ["account_locked"] = "La cuenta está bloqueada",
                    ["password_reset_sent"] = "Correo de restablecimiento de contraseña enviado"
                },
                ["zh"] = new Dictionary<string, string>
                {
                    ["login"] = "登录",
                    ["logout"] = "登出",
                    ["register"] = "注册",
                    ["email"] = "电子邮件",
                    ["password"] = "密码",
                    ["confirm_password"] = "确认密码",
                    ["forgot_password"] = "忘记密码？",
                    ["reset_password"] = "重置密码",
                    ["login_success"] = "登录成功",
                    ["login_failed"] = "登录失败",
                    ["invalid_credentials"] = "无效的电子邮件或密码",
                    ["account_locked"] = "账户已锁定",
                    ["password_reset_sent"] = "密码重置邮件已发送"
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
                },
                ["es"] = new Dictionary<string, string>
                {
                    ["start_assessment"] = "Iniciar evaluación",
                    ["continue_assessment"] = "Continuar evaluación",
                    ["complete_assessment"] = "Completar evaluación",
                    ["question"] = "Pregunta",
                    ["of"] = "de",
                    ["next"] = "Siguiente",
                    ["previous"] = "Anterior",
                    ["submit"] = "Enviar",
                    ["assessment_completed"] = "Evaluación completada exitosamente",
                    ["assessment_progress"] = "Progreso de la evaluación",
                    ["time_remaining"] = "Tiempo restante",
                    ["save_progress"] = "Guardar progreso",
                    ["resume_later"] = "Continuar más tarde"
                },
                ["zh"] = new Dictionary<string, string>
                {
                    ["start_assessment"] = "开始评估",
                    ["continue_assessment"] = "继续评估",
                    ["complete_assessment"] = "完成评估",
                    ["question"] = "问题",
                    ["of"] = "的",
                    ["next"] = "下一个",
                    ["previous"] = "上一个",
                    ["submit"] = "提交",
                    ["assessment_completed"] = "评估成功完成",
                    ["assessment_progress"] = "评估进度",
                    ["time_remaining"] = "剩余时间",
                    ["save_progress"] = "保存进度",
                    ["resume_later"] = "稍后继续"
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
                },
                ["es"] = new Dictionary<string, string>
                {
                    ["career_matches"] = "Coincidencias de carrera",
                    ["top_matches"] = "Mejores coincidencias",
                    ["match_score"] = "Puntuación de coincidencia",
                    ["career_details"] = "Detalles de carrera",
                    ["job_description"] = "Descripción del trabajo",
                    ["required_skills"] = "Habilidades requeridas",
                    ["education_requirements"] = "Requisitos educativos",
                    ["salary_range"] = "Rango salarial",
                    ["career_outlook"] = "Perspectivas de carrera",
                    ["related_careers"] = "Carreras relacionadas",
                    ["programs"] = "Programas",
                    ["pathways"] = "Rutas"
                },
                ["zh"] = new Dictionary<string, string>
                {
                    ["career_matches"] = "职业匹配",
                    ["top_matches"] = "最佳匹配",
                    ["match_score"] = "匹配分数",
                    ["career_details"] = "职业详情",
                    ["job_description"] = "工作描述",
                    ["required_skills"] = "所需技能",
                    ["education_requirements"] = "教育要求",
                    ["salary_range"] = "薪资范围",
                    ["career_outlook"] = "职业前景",
                    ["related_careers"] = "相关职业",
                    ["programs"] = "项目",
                    ["pathways"] = "路径"
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
                },
                ["es"] = new Dictionary<string, string>
                {
                    ["personality_report"] = "Informe de personalidad",
                    ["career_report"] = "Informe de carrera",
                    ["detailed_report"] = "Informe detallado",
                    ["summary_report"] = "Informe resumen",
                    ["download_pdf"] = "Descargar PDF",
                    ["share_report"] = "Compartir informe",
                    ["print_report"] = "Imprimir informe",
                    ["report_generated"] = "Informe generado exitosamente",
                    ["personality_type"] = "Tipo de personalidad",
                    ["strengths"] = "Fortalezas",
                    ["challenges"] = "Desafíos",
                    ["recommendations"] = "Recomendaciones"
                },
                ["zh"] = new Dictionary<string, string>
                {
                    ["personality_report"] = "性格报告",
                    ["career_report"] = "职业报告",
                    ["detailed_report"] = "详细报告",
                    ["summary_report"] = "摘要报告",
                    ["download_pdf"] = "下载PDF",
                    ["share_report"] = "分享报告",
                    ["print_report"] = "打印报告",
                    ["report_generated"] = "报告生成成功",
                    ["personality_type"] = "性格类型",
                    ["strengths"] = "优势",
                    ["challenges"] = "挑战",
                    ["recommendations"] = "建议"
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
                },
                ["es"] = new Dictionary<string, string>
                {
                    ["dashboard"] = "Panel de control",
                    ["users"] = "Usuarios",
                    ["assessments"] = "Evaluaciones",
                    ["reports"] = "Informes",
                    ["settings"] = "Configuración",
                    ["analytics"] = "Analíticas",
                    ["user_management"] = "Gestión de usuarios",
                    ["system_health"] = "Salud del sistema",
                    ["performance_metrics"] = "Métricas de rendimiento",
                    ["audit_logs"] = "Registros de auditoría"
                },
                ["zh"] = new Dictionary<string, string>
                {
                    ["dashboard"] = "仪表板",
                    ["users"] = "用户",
                    ["assessments"] = "评估",
                    ["reports"] = "报告",
                    ["settings"] = "设置",
                    ["analytics"] = "分析",
                    ["user_management"] = "用户管理",
                    ["system_health"] = "系统健康",
                    ["performance_metrics"] = "性能指标",
                    ["audit_logs"] = "审计日志"
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
                    _logger.LogWarning("Invalid translation key format");
                    return key;
                }

                var category = parts[0];
                var textKey = parts[1];

                if (!_translations.ContainsKey(category) ||
                    !_translations[category].ContainsKey(language) ||
                    !_translations[category][language].ContainsKey(textKey))
                {
                    _logger.LogWarning("Translation not found for requested key and language");
                    
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
                _logger.LogError(ex, "Error getting translation for key");
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

                _logger.LogWarning("Translations not found for requested category and language");
                return new Dictionary<string, string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting translations for category");
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

            _logger.LogWarning("Language config not found for code");
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
                _logger.LogError(ex, "Error formatting number for language");
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
                _logger.LogError(ex, "Error formatting currency for language");
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
                _logger.LogError(ex, "Error formatting date for language");
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
                _logger.LogError(ex, "Error getting language metadata");
                return new Dictionary<string, object>();
            }
        }
    }
}
