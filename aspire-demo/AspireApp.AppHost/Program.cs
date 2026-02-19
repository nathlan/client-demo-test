using Aspire.Hosting;
using Azure.Provisioning.Storage;
using Azure.Provisioning.Monitoring;
using Azure.Provisioning.OperationalInsights;
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

        storageAccount.AllowBlobPublicAccess = false;

        storageAccount.MinimumTlsVersion = StorageMinimumTlsVersion.Tls1_0;
        
        storageAccount.EnableHttpsTrafficOnly = true;
        
        // Configure diagnostic settings for compliance (nathlan/shared-standards Section 3)
        var logAnalyticsWorkspace = new OperationalInsightsWorkspace("monitoring-workspace");
        
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
