# Aspire Demo - Insecure Storage Account Configuration

⚠️ **WARNING: This is an intentionally insecure configuration for demonstration and testing purposes only!**

## Overview

This Aspire application demonstrates an insecure Azure Storage Account configuration with multiple security vulnerabilities.

## Security Issues Demonstrated

### 1. **Public Blob Access Enabled**
```csharp
storageAccount.AllowBlobPublicAccess = true;
```
- **Risk**: Allows anonymous public access to blob containers
- **Impact**: Data can be accessed by anyone without authentication
- **Recommendation**: Set to `false` to disable public blob access

### 2. **TLS 1.0 Enabled**
```csharp
storageAccount.MinimumTlsVersion = StorageMinimumTlsVersion.Tls1_0;
```
- **Risk**: TLS 1.0 has known security vulnerabilities
- **Impact**: Susceptible to man-in-the-middle attacks and data interception
- **Recommendation**: Use `StorageMinimumTlsVersion.Tls1_2` or higher

### 3. **Additional Security Issues**
```csharp
storageAccount.AllowSharedKeyAccess = true; // Should use Azure AD only
storageAccount.IsHttpsOnly = false; // Should require HTTPS
storageAccount.EnableHttpsTrafficOnly = false; // Allows HTTP traffic
```

## Project Structure

```
aspire-demo/
└── AspireApp.AppHost/
    ├── AspireApp.AppHost.csproj    # Aspire host project file
    ├── Program.cs                   # Main application with insecure configuration
    └── README.md                    # This file
```

## Running the Application

```bash
cd aspire-demo/AspireApp.AppHost
dotnet run
```

## How to Fix These Issues

To make this configuration secure, update `Program.cs`:

```csharp
var storage = builder.AddAzureStorage("storage")
    .ConfigureInfrastructure(infrastructure =>
    {
        var storageAccount = infrastructure.GetProvisionableResources()
            .OfType<Azure.Provisioning.Storage.StorageAccount>()
            .Single();

        // SECURE CONFIGURATION:
        storageAccount.AllowBlobPublicAccess = false;           // Disable public access
        storageAccount.MinimumTlsVersion = StorageMinimumTlsVersion.Tls1_2;  // Use TLS 1.2
        storageAccount.AllowSharedKeyAccess = false;            // Require Azure AD
        storageAccount.IsHttpsOnly = true;                      // Require HTTPS
        storageAccount.EnableHttpsTrafficOnly = true;           // Enforce HTTPS
    });
```

## Security Scanning

This code will be flagged by:
- Azure Security Center
- Checkov (Infrastructure as Code scanning)
- CodeQL (if configured for C#)
- Azure Policy enforcement

## References

- [Azure Storage security recommendations](https://docs.microsoft.com/azure/storage/common/security-recommendations)
- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)
- [Azure Storage Account security best practices](https://learn.microsoft.com/azure/storage/common/storage-security-guide)
