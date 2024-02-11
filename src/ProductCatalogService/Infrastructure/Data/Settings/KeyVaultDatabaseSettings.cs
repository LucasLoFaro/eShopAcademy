using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Application.Interfaces.Data;
using Data.Interfaces;

public class KeyVaultDatabaseSettings : IDatabaseSettingsProvider
{
    private readonly SecretClient _secretClient;

    public KeyVaultDatabaseSettings(string keyVaultUri)
    {
        var credential = new DefaultAzureCredential();
        _secretClient = new SecretClient(new Uri(keyVaultUri), credential);
    }

    public string GetClient()
    {
        throw new NotImplementedException();
    }

    public string GetConnectionBundlePath()
    {
        throw new NotImplementedException();
    }

    public string GetConnectionString()
    {
        throw new NotImplementedException();
        /*var secret = await _secretClient.GetSecretAsync(secretName);
        var connectionString = secret.Value;

        return new DatabaseSettings
        {
            ConnectionString = connectionString
        };*/
    }

    public string GetSecret()
    {
        throw new NotImplementedException();
    }
}
