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

        storageAccount.AllowBlobPublicAccess = true;
        
        storageAccount.MinimumTlsVersion = StorageMinimumTlsVersion.Tls1_3;
        
        storageAccount.EnableHttpsTrafficOnly = false;
    });

// Add blob service to the storage account
var blobs = storage.AddBlobs("blobs");

builder.Build().Run();
