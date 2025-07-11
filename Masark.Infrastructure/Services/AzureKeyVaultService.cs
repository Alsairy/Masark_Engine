using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Masark.Infrastructure.Services
{
    public interface IKeyVaultService
    {
        Task<string> GetSecretAsync(string secretName);
        Task SetSecretAsync(string secretName, string secretValue);
        Task<bool> SecretExistsAsync(string secretName);
        Task DeleteSecretAsync(string secretName);
        Task<Dictionary<string, string>> GetAllSecretsAsync();
    }

    public class AzureKeyVaultService : IKeyVaultService
    {
        private readonly SecretClient _secretClient;
        private readonly ILogger<AzureKeyVaultService> _logger;
        private readonly bool _isEnabled;

        public AzureKeyVaultService(IConfiguration configuration, ILogger<AzureKeyVaultService> logger)
        {
            _logger = logger;
            
            var keyVaultUrl = configuration["AzureKeyVault:VaultUrl"];
            _isEnabled = !string.IsNullOrEmpty(keyVaultUrl);

            if (_isEnabled)
            {
                try
                {
                    var credential = new DefaultAzureCredential();
                    _secretClient = new SecretClient(new Uri(keyVaultUrl), credential);
                    _logger.LogInformation("Azure Key Vault client initialized successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to initialize Azure Key Vault client. Falling back to configuration.");
                    _isEnabled = false;
                }
            }
            else
            {
                _logger.LogInformation("Azure Key Vault not configured. Using configuration-based secrets.");
            }
        }

        public async Task<string> GetSecretAsync(string secretName)
        {
            try
            {
                if (!_isEnabled)
                {
                    _logger.LogDebug("Key Vault not enabled, returning null for secret: {SecretName}", secretName);
                    return null;
                }

                var response = await _secretClient.GetSecretAsync(secretName);
                _logger.LogDebug("Successfully retrieved secret: {SecretName}", secretName);
                return response.Value.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve secret: {SecretName}", secretName);
                return null;
            }
        }

        public async Task SetSecretAsync(string secretName, string secretValue)
        {
            try
            {
                if (!_isEnabled)
                {
                    _logger.LogWarning("Key Vault not enabled, cannot set secret: {SecretName}", secretName);
                    return;
                }

                await _secretClient.SetSecretAsync(secretName, secretValue);
                _logger.LogInformation("Successfully set secret: {SecretName}", secretName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set secret: {SecretName}", secretName);
                throw;
            }
        }

        public async Task<bool> SecretExistsAsync(string secretName)
        {
            try
            {
                if (!_isEnabled)
                    return false;

                var response = await _secretClient.GetSecretAsync(secretName);
                return response.Value != null;
            }
            catch
            {
                return false;
            }
        }

        public async Task DeleteSecretAsync(string secretName)
        {
            try
            {
                if (!_isEnabled)
                {
                    _logger.LogWarning("Key Vault not enabled, cannot delete secret: {SecretName}", secretName);
                    return;
                }

                await _secretClient.StartDeleteSecretAsync(secretName);
                _logger.LogInformation("Successfully deleted secret: {SecretName}", secretName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete secret: {SecretName}", secretName);
                throw;
            }
        }

        public async Task<Dictionary<string, string>> GetAllSecretsAsync()
        {
            var secrets = new Dictionary<string, string>();

            try
            {
                if (!_isEnabled)
                    return secrets;

                await foreach (var secretProperties in _secretClient.GetPropertiesOfSecretsAsync())
                {
                    try
                    {
                        var secret = await _secretClient.GetSecretAsync(secretProperties.Name);
                        secrets[secretProperties.Name] = secret.Value.Value;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to retrieve secret: {SecretName}", secretProperties.Name);
                    }
                }

                _logger.LogInformation("Retrieved {Count} secrets from Key Vault", secrets.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve secrets from Key Vault");
            }

            return secrets;
        }
    }

    public class LocalKeyVaultService : IKeyVaultService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<LocalKeyVaultService> _logger;

        public LocalKeyVaultService(IConfiguration configuration, ILogger<LocalKeyVaultService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> GetSecretAsync(string secretName)
        {
            var value = _configuration[secretName] ?? _configuration[$"Secrets:{secretName}"];
            _logger.LogDebug("Retrieved local secret: {SecretName}", secretName);
            return value;
        }

        public async Task SetSecretAsync(string secretName, string secretValue)
        {
            _logger.LogWarning("Cannot set secrets in local configuration mode");
            throw new NotSupportedException("Setting secrets is not supported in local configuration mode");
        }

        public async Task<bool> SecretExistsAsync(string secretName)
        {
            var value = _configuration[secretName] ?? _configuration[$"Secrets:{secretName}"];
            return !string.IsNullOrEmpty(value);
        }

        public async Task DeleteSecretAsync(string secretName)
        {
            _logger.LogWarning("Cannot delete secrets in local configuration mode");
            throw new NotSupportedException("Deleting secrets is not supported in local configuration mode");
        }

        public async Task<Dictionary<string, string>> GetAllSecretsAsync()
        {
            var secrets = new Dictionary<string, string>();
            var secretsSection = _configuration.GetSection("Secrets");
            
            foreach (var item in secretsSection.GetChildren())
            {
                secrets[item.Key] = item.Value;
            }

            return secrets;
        }
    }
}
