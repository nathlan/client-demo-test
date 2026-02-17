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
        
        // SECURITY ISSUE #2: Set minimum TLS version to 1.0 (should be 1.2)
        storageAccount.MinimumTlsVersion = StorageMinimumTlsVersion.Tls1_0;
        
        // Disable secure transfer (allows HTTP)
        storageAccount.EnableHttpsTrafficOnly = false;
    });

// Add blob service to the storage account
var blobs = storage.AddBlobs("blobs");

builder.Build().Run();
