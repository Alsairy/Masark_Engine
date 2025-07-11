using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Security.Cryptography;

namespace Masark.Infrastructure.Services
{
    public interface IEncryptionService
    {
        Task<string> EncryptAsync(string plainText, string purpose = "default");
        Task<string> DecryptAsync(string encryptedText, string purpose = "default");
        Task<string> EncryptSensitiveDataAsync(string data);
        Task<string> DecryptSensitiveDataAsync(string encryptedData);
        Task<string> HashPasswordAsync(string password, string salt = null);
        Task<bool> VerifyPasswordAsync(string password, string hashedPassword, string salt = null);
        Task<string> GenerateSecureTokenAsync(int length = 32);
        Task<string> EncryptPersonalDataAsync(string personalData, string userId);
        Task<string> DecryptPersonalDataAsync(string encryptedData, string userId);
    }

    public class EncryptionService : IEncryptionService
    {
        private readonly IDataProtectionProvider _dataProtectionProvider;
        private readonly ILogger<EncryptionService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _encryptionKey;

        public EncryptionService(
            IDataProtectionProvider dataProtectionProvider,
            ILogger<EncryptionService> logger,
            IConfiguration configuration)
        {
            _dataProtectionProvider = dataProtectionProvider;
            _logger = logger;
            _configuration = configuration;
            _encryptionKey = configuration["Encryption:MasterKey"] ?? GenerateDefaultKey();
        }

        public async Task<string> EncryptAsync(string plainText, string purpose = "default")
        {
            try
            {
                if (string.IsNullOrEmpty(plainText))
                    return string.Empty;

                var protector = _dataProtectionProvider.CreateProtector($"MasarkEngine.{purpose}");
                var encryptedBytes = protector.Protect(Encoding.UTF8.GetBytes(plainText));
                var result = Convert.ToBase64String(encryptedBytes);

                _logger.LogDebug("Successfully encrypted data for purpose: {Purpose}", purpose);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to encrypt data for purpose: {Purpose}", purpose);
                throw new InvalidOperationException("Encryption failed", ex);
            }
        }

        public async Task<string> DecryptAsync(string encryptedText, string purpose = "default")
        {
            try
            {
                if (string.IsNullOrEmpty(encryptedText))
                    return string.Empty;

                var protector = _dataProtectionProvider.CreateProtector($"MasarkEngine.{purpose}");
                var encryptedBytes = Convert.FromBase64String(encryptedText);
                var decryptedBytes = protector.Unprotect(encryptedBytes);
                var result = Encoding.UTF8.GetString(decryptedBytes);

                _logger.LogDebug("Successfully decrypted data for purpose: {Purpose}", purpose);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decrypt data for purpose: {Purpose}", purpose);
                throw new InvalidOperationException("Decryption failed", ex);
            }
        }

        public async Task<string> EncryptSensitiveDataAsync(string data)
        {
            return await EncryptAsync(data, "SensitiveData");
        }

        public async Task<string> DecryptSensitiveDataAsync(string encryptedData)
        {
            return await DecryptAsync(encryptedData, "SensitiveData");
        }

        public async Task<string> HashPasswordAsync(string password, string salt = null)
        {
            try
            {
                if (string.IsNullOrEmpty(password))
                    throw new ArgumentException("Password cannot be null or empty", nameof(password));

                salt ??= GenerateSalt();
                
                using var pbkdf2 = new Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes(salt), 100000, HashAlgorithmName.SHA256);
                var hash = pbkdf2.GetBytes(32);
                
                var result = $"{salt}:{Convert.ToBase64String(hash)}";
                _logger.LogDebug("Successfully hashed password");
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to hash password");
                throw new InvalidOperationException("Password hashing failed", ex);
            }
        }

        public async Task<bool> VerifyPasswordAsync(string password, string hashedPassword, string salt = null)
        {
            try
            {
                if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hashedPassword))
                    return false;

                var parts = hashedPassword.Split(':');
                if (parts.Length != 2)
                    return false;

                var storedSalt = parts[0];
                var storedHash = parts[1];

                using var pbkdf2 = new Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes(storedSalt), 100000, HashAlgorithmName.SHA256);
                var computedHash = pbkdf2.GetBytes(32);
                var computedHashString = Convert.ToBase64String(computedHash);

                var result = CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(storedHash),
                    Encoding.UTF8.GetBytes(computedHashString));

                _logger.LogDebug("Password verification completed: {Result}", result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to verify password");
                return false;
            }
        }

        public async Task<string> GenerateSecureTokenAsync(int length = 32)
        {
            try
            {
                using var rng = RandomNumberGenerator.Create();
                var bytes = new byte[length];
                rng.GetBytes(bytes);
                
                var result = Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
                _logger.LogDebug("Generated secure token of length: {Length}", length);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate secure token");
                throw new InvalidOperationException("Token generation failed", ex);
            }
        }

        public async Task<string> EncryptPersonalDataAsync(string personalData, string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(personalData) || string.IsNullOrEmpty(userId))
                    return string.Empty;

                var purpose = $"PersonalData.{userId}";
                var result = await EncryptAsync(personalData, purpose);
                
                _logger.LogDebug("Successfully encrypted personal data for user: {UserId}", userId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to encrypt personal data for user: {UserId}", userId);
                throw new InvalidOperationException("Personal data encryption failed", ex);
            }
        }

        public async Task<string> DecryptPersonalDataAsync(string encryptedData, string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(encryptedData) || string.IsNullOrEmpty(userId))
                    return string.Empty;

                var purpose = $"PersonalData.{userId}";
                var result = await DecryptAsync(encryptedData, purpose);
                
                _logger.LogDebug("Successfully decrypted personal data for user: {UserId}", userId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decrypt personal data for user: {UserId}", userId);
                throw new InvalidOperationException("Personal data decryption failed", ex);
            }
        }

        private string GenerateSalt()
        {
            using var rng = RandomNumberGenerator.Create();
            var saltBytes = new byte[16];
            rng.GetBytes(saltBytes);
            return Convert.ToBase64String(saltBytes);
        }

        private string GenerateDefaultKey()
        {
            using var rng = RandomNumberGenerator.Create();
            var keyBytes = new byte[32];
            rng.GetBytes(keyBytes);
            return Convert.ToBase64String(keyBytes);
        }
    }

    public static class EncryptionServiceExtensions
    {
        public static IServiceCollection AddEncryptionServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDataProtection()
                .SetApplicationName("MasarkEngine")
                .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "keys")))
                .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

            services.AddScoped<IEncryptionService, EncryptionService>();

            return services;
        }
    }
}
