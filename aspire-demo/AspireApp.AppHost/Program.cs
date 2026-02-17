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
        storageAccount.AllowBlobPublicAccess = false;
        
        // Setting to TLS 1.3 to resolve issue
        storageAccount.MinimumTlsVersion = StorageMinimumTlsVersion.Tls1_3;
        
        // Still keeping this disabled
        storageAccount.EnableHttpsTrafficOnly = false;

        // SECURITY ISSUE #3: No diagnostic settings are configured for this storage account.
        // Logs and metrics are not shipped to Log Analytics, Event Hub, or another storage account.
    });

// Add blob service to the storage account
var blobs = storage.AddBlobs("blobs");

builder.Build().Run();
