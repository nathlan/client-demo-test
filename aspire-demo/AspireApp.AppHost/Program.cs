using Aspire.Hosting;
using Azure.Provisioning.Storage;
using Azure.Provisioning.Monitoring;
using Azure.Provisioning.OperationalInsights;
using System.Linq;

var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddAzureStorage("storage")
    .ConfigureInfrastructure(infrastructure =>
    {
        // Get the storage account from the infrastructure
        var storageAccount = infrastructure.GetProvisionableResources()
            .OfType<StorageAccount>()
            .Single();

        storageAccount.AllowBlobPublicAccess = false;

        storageAccount.MinimumTlsVersion = StorageMinimumTlsVersion.Tls1_3;
        
        storageAccount.EnableHttpsTrafficOnly = true;

        var demoSubnet = Azure.Provisioning.Network.Subnet.FromExisting("demo-subnet");

        var storagePrivateEndpoint = new Azure.Provisioning.Network.PrivateEndpoint("storage-private-endpoint")
        {
            Subnet = new Azure.Provisioning.Network.SubnetData { Id = demoSubnet.Id },
            PrivateLinkServiceConnections =
            {
                new Azure.Provisioning.Network.PrivateLinkServiceConnection
                {
                    Name = "storage-blob-connection",
                    PrivateLinkServiceId = storageAccount.Id,
                    GroupIds = { "blob" }
                }
            }
        };

        infrastructure.Add(demoSubnet);
        infrastructure.Add(storagePrivateEndpoint);
        
        // Configure diagnostic settings for compliance
        var logAnalyticsWorkspace = OperationalInsightsWorkspace.FromExisting("monitoring-workspace");
        
        var diagnosticSettings = new DiagnosticSetting("storageAccountDiagnostics")
        {
            WorkspaceId = logAnalyticsWorkspace.Id,
            // Enable all storage account metrics
            Metrics = 
            {
                new MetricSettings
                {
                    Category = "AllMetrics",
                    Enabled = true,
                    RetentionPolicy = new RetentionPolicy
                    {
                        Enabled = true,
                        Days = 90 // 90 days hot storage per organizational policy
                    }
                }
            },
            // Enable storage account logs
            Logs = 
            {
                new LogSettings
                {
                    Category = "StorageRead",
                    Enabled = true,
                    RetentionPolicy = new RetentionPolicy
                    {
                        Enabled = true,
                        Days = 90 // 90 days hot storage, with 2 years cold storage configured in Log Analytics
                    }
                },
                new LogSettings
                {
                    Category = "StorageWrite",
                    Enabled = true,
                    RetentionPolicy = new RetentionPolicy
                    {
                        Enabled = true,
                        Days = 90
                    }
                },
                new LogSettings
                {
                    Category = "StorageDelete",
                    Enabled = true,
                    RetentionPolicy = new RetentionPolicy
                    {
                        Enabled = true,
                        Days = 90
                    }
                }
            }
        };
        
        infrastructure.Add(logAnalyticsWorkspace);
        infrastructure.Add(diagnosticSettings);
    });

// Add blob service to the storage account
var blobs = storage.AddBlobs("blobs");

builder.Build().Run();
