using Aspire.Hosting;
using Azure.Provisioning.Storage;
using System.Linq;

var builder = DistributedApplication.CreateBuilder(args);

// WARNING: This is an intentionally INSECURE configuration for demonstration purposes
// DO NOT use these settings in production!

var storage = builder.AddAzureStorage("storage")
    .ConfigureInfrastructure(infrastructure =>
    {
        // Get the storage account from the infrastructure
        var storageAccount = infrastructure.GetProvisionableResources()
            .OfType<StorageAccount>()
            .Single();

        // SECURITY ISSUE #1: Enable public blob access (should be disabled)
        storageAccount.AllowBlobPublicAccess = true;
        
        // SECURITY ISSUE #2: Set minimum TLS version to 1.1 (should be 1.2 at a minimum)
        storageAccount.MinimumTlsVersion = StorageMinimumTlsVersion.Tls1_1;
        
        // Disable secure transfer (allows HTTP)
        storageAccount.EnableHttpsTrafficOnly = false;

        // SECURITY ISSUE #3: No diagnostic settings are configured for this storage account.
        // Logs and metrics are not shipped to Log Analytics, Event Hub, or another storage account.
    });

// Add blob service to the storage account
var blobs = storage.AddBlobs("blobs");

builder.Build().Run();
