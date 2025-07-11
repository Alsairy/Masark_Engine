using System.ComponentModel.DataAnnotations;

namespace Masark.Infrastructure.Options
{
    public class CachingOptions
    {
        public const string SectionName = "Caching";

        [Required(ErrorMessage = "Redis connection string is required")]
        public string RedisConnectionString { get; set; } = string.Empty;

        [Required(ErrorMessage = "Instance name is required")]
        public string InstanceName { get; set; } = "MasarkEngine";

        [Range(1, 3600, ErrorMessage = "DefaultExpirationMinutes must be between 1 and 3600")]
        public int DefaultExpirationMinutes { get; set; } = 30;

        [Range(1, 10000, ErrorMessage = "MaxCacheSize must be between 1 and 10000")]
        public int MaxCacheSize { get; set; } = 1000;

        public bool EnableDistributedCache { get; set; } = true;
        public bool EnableMemoryCache { get; set; } = true;
        public bool EnableCompression { get; set; } = true;
    }
}
