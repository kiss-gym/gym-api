using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using GymApi.Api.Infrastructure.Middleware;

namespace GymApi.Api.Infrastructure.Azure;

internal class KeyVaultSecretRetriever(ILogger<KeyVaultSecretRetriever> logger)
{
    public string GetStorageConnectionString()
    {
        const string keyVaultName = "storage-connection-key01";
        const string secretName = "storage-connection-string";

        // Construct the Key Vault URI
        const string kvUri = $"https://{keyVaultName}.vault.azure.net";

        var client = new SecretClient(new Uri(kvUri), new DefaultAzureCredential());

        try
        {
            KeyVaultSecret secret = client.GetSecret(secretName);
            return secret.Value;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error retrieving secret '{secretName}' from Key Vault '{keyVaultName}'");
            throw;
        }
    }
}
