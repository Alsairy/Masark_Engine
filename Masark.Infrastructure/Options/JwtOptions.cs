using System.ComponentModel.DataAnnotations;

namespace Masark.Infrastructure.Options
{
    public class JwtOptions
    {
        public const string SectionName = "Jwt";

        [Required(ErrorMessage = "JWT SecretKey is required")]
        [MinLength(32, ErrorMessage = "JWT SecretKey must be at least 32 characters long")]
        public string SecretKey { get; set; } = string.Empty;

        [Required(ErrorMessage = "JWT Issuer is required")]
        public string Issuer { get; set; } = string.Empty;

        [Required(ErrorMessage = "JWT Audience is required")]
        public string Audience { get; set; } = string.Empty;

        [Range(1, 1440, ErrorMessage = "ExpirationMinutes must be between 1 and 1440 (24 hours)")]
        public int ExpirationMinutes { get; set; } = 60;

        [Range(0, 60, ErrorMessage = "ClockSkewMinutes must be between 0 and 60")]
        public int ClockSkewMinutes { get; set; } = 0;
    }
}
