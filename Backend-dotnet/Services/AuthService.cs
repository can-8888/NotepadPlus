using Azure.Identity;
using Microsoft.Extensions.Configuration;

public class AuthService
{
    private readonly DefaultAzureCredential _credentials;
    
    public AuthService(IConfiguration configuration)
    {
        // Mitigare pentru vulnerabilitatea din Azure.Identity
        _credentials = new DefaultAzureCredential(
            new DefaultAzureCredentialOptions 
            { 
                ExcludeVisualStudioCredential = true,
                ExcludeVisualStudioCodeCredential = true,
                ExcludeManagedIdentityCredential = false,
                ExcludeAzureCliCredential = false,
                ExcludeEnvironmentCredential = false
            });
    }
    
    // ... restul implementării
} 