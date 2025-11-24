# CleanArchitecture.ApiTemplate.Infrastructure.Azure Project

> *"Cloud platforms abstract away infrastructure concerns, allowing developers to focus on what truly matters: delivering value to users."*  
> � **Scott Guthrie**, Executive Vice President, Microsoft Cloud + AI

---

**📚 New to Clean Architecture or DDD?**  
Read **[Architecture Patterns Explained](../ARCHITECTURE_PATTERNS_EXPLAINED.md)** first to understand how Clean Architecture and Domain-Driven Design work together in this project.

---

## 📖 Overview
The **Azure Infrastructure Layer** is an optional specialized infrastructure project focused on Azure-specific implementations and integrations. This separation allows you to keep Azure-specific code isolated and makes it easier to swap cloud providers if needed.

---

## 🎯 Purpose
- Implement Azure-specific services (Key Vault, Blob Storage, Service Bus)
- Manage Azure resource integrations
- Configure Azure App Configuration
- Implement Azure-specific authentication
- Handle Azure monitoring and diagnostics
- Provide Azure-optimized implementations

---

## 📁 Project Structure

```
CleanArchitecture.ApiTemplate.Infrastructure.Azure/
📖? KeyVault/                         # Azure Key Vault integration
?   📖? KeyVaultSecretProvider.cs    # Secret management
?   📖? KeyVaultConfigurationExtensions.cs
?
📖? Storage/                          # Azure Blob Storage
?   📖? BlobStorageService.cs        # File storage implementation
?   📖? BlobContainerFactory.cs
?
📖? ServiceBus/                       # Azure Service Bus
?   📖? ServiceBusPublisher.cs       # Message publishing
?   📖? ServiceBusConsumer.cs        # Message consumption
?
📖? ApplicationInsights/              # Telemetry and monitoring
?   📖? TelemetryService.cs
?   📖? CustomTelemetryInitializer.cs
?
📖? AppConfiguration/                 # Azure App Configuration
?   📖? AppConfigurationProvider.cs
?   📖? FeatureFlagManager.cs
?
📖? Identity/                         # Azure AD / Entra ID
?   📖? AzureAdAuthenticationService.cs
?   📖? ManagedIdentityTokenProvider.cs
?
📖? DependencyInjection.cs            # Extension method: AddAzureInfrastructure()

```

---

## ☁️ Key Azure Implementations

### 1. **Azure Key Vault Integration**

```csharp
/// <summary>
/// Extension methods for Azure Key Vault configuration
/// </summary>
public static class KeyVaultConfigurationExtensions
{
    public static IConfigurationBuilder AddAzureKeyVault(
        this IConfigurationBuilder builder,
        IConfiguration configuration)
    {
        var keyVaultUrl = configuration["KeyVault:Url"];
        
        if (string.IsNullOrEmpty(keyVaultUrl))
        {
            // Key Vault not configured, skip
            return builder;
        }
        
        // Use Managed Identity in Azure (DefaultAzureCredential)
        // Falls back to Visual Studio, Azure CLI, or Environment credentials locally
        var credential = new DefaultAzureCredential();
        
        builder.AddAzureKeyVault(
            new Uri(keyVaultUrl),
            credential,
            new AzureKeyVaultConfigurationOptions
            {
                ReloadInterval = TimeSpan.FromMinutes(5)
            });
        
        return builder;
    }
}

/// <summary>
/// Service for managing secrets in Azure Key Vault
/// </summary>
public class KeyVaultSecretProvider : ISecretProvider
{
    private readonly SecretClient _secretClient;
    private readonly ILogger<KeyVaultSecretProvider> _logger;
    
    public KeyVaultSecretProvider(
        IConfiguration configuration,
        ILogger<KeyVaultSecretProvider> logger)
    {
        var keyVaultUrl = configuration["KeyVault:Url"];
        
        if (string.IsNullOrEmpty(keyVaultUrl))
        {
            throw new InvalidOperationException("Key Vault URL is not configured");
        }
        
        var credential = new DefaultAzureCredential();
        _secretClient = new SecretClient(new Uri(keyVaultUrl), credential);
        _logger = logger;
    }
    
    public async Task<string> GetSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving secret: {SecretName}", secretName);
            
            var secret = await _secretClient.GetSecretAsync(secretName, cancellationToken: cancellationToken);
            
            return secret.Value.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("Secret not found: {SecretName}", secretName);
            throw new SecretNotFoundException($"Secret '{secretName}' not found in Key Vault", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving secret: {SecretName}", secretName);
            throw;
        }
    }
    
    public async Task SetSecretAsync(
        string secretName, 
        string secretValue, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Setting secret: {SecretName}", secretName);
            
            await _secretClient.SetSecretAsync(secretName, secretValue, cancellationToken);
            
            _logger.LogInformation("Secret set successfully: {SecretName}", secretName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting secret: {SecretName}", secretName);
            throw;
        }
    }
}
```

---

### 2. **Azure Blob Storage Service**

```csharp
/// <summary>
/// Implementation of IFileStorageService using Azure Blob Storage
/// </summary>
public class BlobStorageService : IFileStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<BlobStorageService> _logger;
    private readonly string _containerName;
    
    public BlobStorageService(
        IConfiguration configuration,
        ILogger<BlobStorageService> logger)
    {
        var connectionString = configuration["AzureStorage:ConnectionString"];
        _containerName = configuration["AzureStorage:ContainerName"] 📖 "files";
        
        // Use Managed Identity in Azure (preferred)
        // var credential = new DefaultAzureCredential();
        // _blobServiceClient = new BlobServiceClient(
        //     new Uri(configuration["AzureStorage:BlobEndpoint"]), 
        //     credential);
        
        // Or use connection string (development)
        _blobServiceClient = new BlobServiceClient(connectionString);
        _logger = logger;
    }
    
    public async Task<Result<string>> UploadFileAsync(
        string fileName, 
        Stream fileStream, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
            
            var blobClient = containerClient.GetBlobClient(fileName);
            
            _logger.LogInformation("Uploading file to blob storage: {FileName}", fileName);
            
            await blobClient.UploadAsync(
                fileStream, 
                overwrite: true, 
                cancellationToken: cancellationToken);
            
            var blobUrl = blobClient.Uri.ToString();
            
            _logger.LogInformation("File uploaded successfully: {BlobUrl}", blobUrl);
            
            return Result<string>.Ok(blobUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file: {FileName}", fileName);
            return Result<string>.Fail($"File upload failed: {ex.Message}");
        }
    }
    
    public async Task<Result<Stream>> DownloadFileAsync(
        string fileName, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(fileName);
            
            _logger.LogInformation("Downloading file from blob storage: {FileName}", fileName);
            
            var downloadResponse = await blobClient.DownloadAsync(cancellationToken);
            
            return Result<Stream>.Ok(downloadResponse.Value.Content);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("File not found: {FileName}", fileName);
            return Result<Stream>.Fail($"File '{fileName}' not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file: {FileName}", fileName);
            return Result<Stream>.Fail($"File download failed: {ex.Message}");
        }
    }
    
    public async Task<Result<bool>> DeleteFileAsync(
        string fileName, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(fileName);
            
            _logger.LogInformation("Deleting file from blob storage: {FileName}", fileName);
            
            await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
            
            return Result<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {FileName}", fileName);
            return Result<bool>.Fail($"File deletion failed: {ex.Message}");
        }
    }
}
```

---

### 3. **Azure Service Bus Integration**

```csharp
/// <summary>
/// Service for publishing messages to Azure Service Bus
/// </summary>
public class ServiceBusPublisher : IMessagePublisher
{
    private readonly ServiceBusClient _client;
    private readonly ILogger<ServiceBusPublisher> _logger;
    
    public ServiceBusPublisher(
        IConfiguration configuration,
        ILogger<ServiceBusPublisher> logger)
    {
        var connectionString = configuration["ServiceBus:ConnectionString"];
        
        // Use Managed Identity (preferred)
        // var credential = new DefaultAzureCredential();
        // _client = new ServiceBusClient(
        //     configuration["ServiceBus:Namespace"], 
        //     credential);
        
        // Or use connection string
        _client = new ServiceBusClient(connectionString);
        _logger = logger;
    }
    
    public async Task<Result<bool>> PublishAsync<T>(
        string queueOrTopicName, 
        T message, 
        CancellationToken cancellationToken = default)
    {
        ServiceBusSender sender = null;
        
        try
        {
            sender = _client.CreateSender(queueOrTopicName);
            
            var messageBody = JsonSerializer.Serialize(message);
            var serviceBusMessage = new ServiceBusMessage(messageBody)
            {
                ContentType = "application/json",
                MessageId = Guid.NewGuid().ToString()
            };
            
            _logger.LogInformation(
                "Publishing message to {QueueOrTopic}: {MessageId}", 
                queueOrTopicName, 
                serviceBusMessage.MessageId);
            
            await sender.SendMessageAsync(serviceBusMessage, cancellationToken);
            
            _logger.LogInformation("Message published successfully");
            
            return Result<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing message to {QueueOrTopic}", queueOrTopicName);
            return Result<bool>.Fail($"Message publishing failed: {ex.Message}");
        }
        finally
        {
            if (sender != null)
            {
                await sender.DisposeAsync();
            }
        }
    }
}

/// <summary>
/// Service for consuming messages from Azure Service Bus
/// </summary>
public class ServiceBusConsumer : BackgroundService
{
    private readonly ServiceBusClient _client;
    private readonly ILogger<ServiceBusConsumer> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _queueName;
    
    public ServiceBusConsumer(
        IConfiguration configuration,
        ILogger<ServiceBusConsumer> logger,
        IServiceProvider serviceProvider)
    {
        var connectionString = configuration["ServiceBus:ConnectionString"];
        _queueName = configuration["ServiceBus:QueueName"];
        
        _client = new ServiceBusClient(connectionString);
        _logger = logger;
        _serviceProvider = serviceProvider;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var processor = _client.CreateProcessor(_queueName, new ServiceBusProcessorOptions
        {
            MaxConcurrentCalls = 5,
            AutoCompleteMessages = false
        });
        
        processor.ProcessMessageAsync += ProcessMessageAsync;
        processor.ProcessErrorAsync += ProcessErrorAsync;
        
        await processor.StartProcessingAsync(stoppingToken);
        
        _logger.LogInformation("Service Bus consumer started for queue: {QueueName}", _queueName);
        
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
    
    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        try
        {
            var messageBody = args.Message.Body.ToString();
            
            _logger.LogInformation(
                "Processing message: {MessageId}, Body: {Body}", 
                args.Message.MessageId, 
                messageBody);
            
            // Process message using scoped service
            using var scope = _serviceProvider.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<IMessageHandler>();
            
            await handler.HandleAsync(messageBody, args.CancellationToken);
            
            // Complete the message
            await args.CompleteMessageAsync(args.Message);
            
            _logger.LogInformation("Message processed successfully: {MessageId}", args.Message.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message: {MessageId}", args.Message.MessageId);
            
            // Abandon the message (will be retried)
            await args.AbandonMessageAsync(args.Message);
        }
    }
    
    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Error in Service Bus processor: {ErrorSource}", args.ErrorSource);
        return Task.CompletedTask;
    }
}
```

---

### 4. **Application Insights Telemetry**

```csharp
/// <summary>
/// Custom telemetry service for Application Insights
/// </summary>
public class TelemetryService : ITelemetryService
{
    private readonly TelemetryClient _telemetryClient;
    private readonly ILogger<TelemetryService> _logger;
    
    public TelemetryService(
        TelemetryClient telemetryClient,
        ILogger<TelemetryService> logger)
    {
        _telemetryClient = telemetryClient;
        _logger = logger;
    }
    
    public void TrackEvent(string eventName, Dictionary<string, string> properties = null)
    {
        _telemetryClient.TrackEvent(eventName, properties);
        _logger.LogInformation("Tracked event: {EventName}", eventName);
    }
    
    public void TrackMetric(string metricName, double value, Dictionary<string, string> properties = null)
    {
        _telemetryClient.TrackMetric(metricName, value, properties);
        _logger.LogDebug("Tracked metric: {MetricName} = {Value}", metricName, value);
    }
    
    public void TrackException(Exception exception, Dictionary<string, string> properties = null)
    {
        _telemetryClient.TrackException(exception, properties);
        _logger.LogError(exception, "Tracked exception");
    }
    
    public void TrackDependency(
        string dependencyName, 
        string commandName, 
        DateTimeOffset startTime, 
        TimeSpan duration, 
        bool success)
    {
        _telemetryClient.TrackDependency(
            dependencyTypeName: dependencyName,
            target: commandName,
            dependencyName: commandName,
            data: commandName,
            startTime: startTime,
            duration: duration,
            resultCode: success ? "200" : "500",
            success: success);
    }
}

/// <summary>
/// Custom telemetry initializer for adding common properties
/// </summary>
public class CustomTelemetryInitializer : ITelemetryInitializer
{
    private readonly IConfiguration _configuration;
    
    public CustomTelemetryInitializer(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public void Initialize(ITelemetry telemetry)
    {
        if (telemetry == null) return;
        
        // Add custom properties to all telemetry
        telemetry.Context.GlobalProperties["Environment"] = 
            _configuration["Environment"] 📖 "Unknown";
        
        telemetry.Context.GlobalProperties["ApplicationName"] = 
            _configuration["ApplicationName"] 📖 "CleanArchitecture.ApiTemplate";
    }
}
```

---

### 5. **Azure App Configuration**

```csharp
/// <summary>
/// Extension methods for Azure App Configuration
/// </summary>
public static class AppConfigurationExtensions
{
    public static IConfigurationBuilder AddAzureAppConfiguration(
        this IConfigurationBuilder builder,
        IConfiguration configuration)
    {
        var appConfigEndpoint = configuration["AppConfiguration:Endpoint"];
        
        if (string.IsNullOrEmpty(appConfigEndpoint))
        {
            return builder;
        }
        
        builder.AddAzureAppConfiguration(options =>
        {
            options
                .Connect(new Uri(appConfigEndpoint), new DefaultAzureCredential())
                .ConfigureRefresh(refresh =>
                {
                    // Refresh configuration every 30 seconds if a sentinel key changes
                    refresh.Register("Sentinel", refreshAll: true)
                           .SetCacheExpiration(TimeSpan.FromSeconds(30));
                })
                .UseFeatureFlags(featureFlags =>
                {
                    // Refresh feature flags every 10 seconds
                    featureFlags.CacheExpirationInterval = TimeSpan.FromSeconds(10);
                });
        });
        
        return builder;
    }
}

/// <summary>
/// Service for managing feature flags
/// </summary>
public class FeatureFlagManager : IFeatureFlagManager
{
    private readonly IFeatureManager _featureManager;
    private readonly ILogger<FeatureFlagManager> _logger;
    
    public FeatureFlagManager(
        IFeatureManager featureManager,
        ILogger<FeatureFlagManager> logger)
    {
        _featureManager = featureManager;
        _logger = logger;
    }
    
    public async Task<bool> IsEnabledAsync(String featureName)
    {
        var isEnabled = await _featureManager.IsEnabledAsync(featureName);
        
        _logger.LogDebug("Feature flag {FeatureName} is {Status}", 
            featureName, 
            isEnabled ? "enabled" : "disabled");
        
        return isEnabled;
    }
}
```

---

## 🔧 Dependency Injection Setup

```csharp
/// <summary>
/// Extension method to register Azure-specific infrastructure services
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddAzureInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Azure Key Vault
        services.AddSingleton<ISecretProvider, KeyVaultSecretProvider>();
        
        // Azure Blob Storage
        services.AddSingleton<IFileStorageService, BlobStorageService>();
        
        // Azure Service Bus
        services.AddSingleton<IMessagePublisher, ServiceBusPublisher>();
        services.AddHostedService<ServiceBusConsumer>();
        
        // Application Insights
        services.AddApplicationInsightsTelemetry(options =>
        {
            options.ConnectionString = configuration["ApplicationInsights:ConnectionString"];
            options.EnableAdaptiveSampling = true;
            options.EnableDebugLogger = false;
        });
        
        services.AddSingleton<ITelemetryInitializer, CustomTelemetryInitializer>();
        services.AddSingleton<ITelemetryService, TelemetryService>();
        
        // Azure App Configuration (feature flags)
        services.AddAzureAppConfiguration();
        services.AddFeatureManagement();
        services.AddSingleton<IFeatureFlagManager, FeatureFlagManager>();
        
        return services;
    }
}
```

---

## 📦 Dependencies

### **NuGet Packages**
```xml
<ItemGroup>
  <!-- Azure Key Vault -->
  <PackageReference Include="Azure.Extensions.AspNetCore.Configuration.Secrets" Version="1.3.0" />
  <PackageReference Include="Azure.Identity" Version="1.10.0" />
  <PackageReference Include="Azure.Security.KeyVault.Secrets" Version="4.5.0" />
  
  <!-- Azure Blob Storage -->
  <PackageReference Include="Azure.Storage.Blobs" Version="12.19.0" />
  
  <!-- Azure Service Bus -->
  <PackageReference Include="Azure.Messaging.ServiceBus" Version="7.17.0" />
  
  <!-- Application Insights -->
  <PackageReference Include="Microsoft.ApplicationInsights.AspNetCore" Version="2.21.0" />
  
  <!-- Azure App Configuration -->
  <PackageReference Include="Microsoft.Azure.AppConfiguration.AspNetCore" Version="7.0.0" />
  <PackageReference Include="Microsoft.FeatureManagement.AspNetCore" Version="3.1.0" />
  
  <!-- Common Azure -->
  <PackageReference Include="Azure.Core" Version="1.35.0" />
</ItemGroup>

<!-- Project References -->
<ItemGroup>
  <ProjectReference Include="..\CleanArchitecture.ApiTemplate.Application\CleanArchitecture.ApiTemplate.Application.csproj" />
</ItemGroup>
```

---

## ⚙️ Configuration (appsettings.json)

```json
{
  "KeyVault": {
    "Url": "https://your-keyvault.vault.azure.net/"
  },
  "AzureStorage": {
    "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=...",
    "BlobEndpoint": "https://yourstorageaccount.blob.core.windows.net/",
    "ContainerName": "files"
  },
  "ServiceBus": {
    "ConnectionString": "Endpoint=sb://...",
    "Namespace": "your-servicebus-namespace.servicebus.windows.net",
    "QueueName": "sample-queue"
  },
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=...;IngestionEndpoint=..."
  },
  "AppConfiguration": {
    "Endpoint": "https://your-appconfig.azconfig.io"
  }
}
```

---

## 🔐 Managed Identity Configuration

### **Enable Managed Identity in Azure App Service**
1. Navigate to your App Service in Azure Portal
2. Go to **Identity** > **System assigned**
3. Turn status **ON**
4. Copy the **Object (principal) ID**

### **Grant Permissions**

#### **Key Vault Access**
```bash
# Grant Key Vault access to Managed Identity
az keyvault set-policy --name your-keyvault \
  --object-id <managed-identity-object-id> \
  --secret-permissions get list
```

#### **Storage Account Access**
```bash
# Assign Storage Blob Data Contributor role
az role assignment create \
  --role "Storage Blob Data Contributor" \
  --assignee <managed-identity-object-id> \
  --scope /subscriptions/<subscription-id>/resourceGroups/<resource-group>/providers/Microsoft.Storage/storageAccounts/<storage-account>
```

#### **Service Bus Access**
```bash
# Assign Service Bus Data Sender role
az role assignment create \
  --role "Azure Service Bus Data Sender" \
  --assignee <managed-identity-object-id> \
  --scope /subscriptions/<subscription-id>/resourceGroups/<resource-group>/providers/Microsoft.ServiceBus/namespaces/<namespace>
```

---

## 🧪 Testing Strategy

### **Integration Tests with Azure**
```csharp
public class BlobStorageServiceIntegrationTests : IClassFixture<AzureTestFixture>
{
    private readonly BlobStorageService _service;
    
    public BlobStorageServiceIntegrationTests(AzureTestFixture fixture)
    {
        _service = fixture.BlobStorageService;
    }
    
    [Fact]
    public async Task UploadFile_WithValidFile_ShouldSucceed()
    {
        // Arrange
        var fileName = $"test-{Guid.NewGuid()}.txt";
        var content = "Test content";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        
        // Act
        var result = await _service.UploadFileAsync(fileName, stream);
        
        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        
        // Cleanup
        await _service.DeleteFileAsync(fileName);
    }
}
```

---

## ☁️ Azure Infrastructure Checklist

- [ ] Key Vault configured with Managed Identity
- [ ] Blob Storage configured with appropriate access
- [ ] Service Bus configured for messaging
- [ ] Application Insights configured for monitoring
- [ ] App Configuration configured for feature flags
- [ ] Managed Identity enabled and permissions granted
- [ ] Connection strings secured in Key Vault
- [ ] Retry policies configured for Azure services
- [ ] Logging configured for Azure diagnostics

---

## ✅ Best Practices

### ? DO
- Use Managed Identity for authentication (no secrets!)
- Store connection strings in Key Vault
- Configure retry policies for transient failures
- Use Application Insights for monitoring
- Implement circuit breakers for Azure services
- Use Azure App Configuration for dynamic config
- Enable diagnostic logging
- Test with Azurite (local Azure emulator)

### ? DON'T
- Hard-code connection strings
- Use service principals when Managed Identity is available
- Skip retry logic
- Ignore telemetry and monitoring
- Expose Azure-specific code outside this project
- Mix Azure logic with generic infrastructure

---

## 📖 Migration from Current Structure

```
Current Structure ? Azure Infrastructure Layer

(Azure Key Vault code from Program.cs)     ? Infrastructure.Azure/KeyVault/
(Create new)                                ? Infrastructure.Azure/Storage/BlobStorageService.cs
(Create new)                                ? Infrastructure.Azure/ServiceBus/
(Application Insights from Program.cs)     ? Infrastructure.Azure/ApplicationInsights/
```

---

## 📝 Summary

The Azure Infrastructure Layer:
- **Isolates** Azure-specific implementations
- **Uses** Managed Identity for secure authentication
- **Integrates** with Azure services (Key Vault, Storage, Service Bus)
- **Provides** Azure-optimized implementations
- **Enables** easy cloud provider switching
- **Maintains** separation from generic infrastructure

This layer makes your Azure integrations **modular, testable, and swappable**.
