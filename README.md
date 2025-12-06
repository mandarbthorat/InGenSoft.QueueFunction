# Azure Function with Queue Trigger (InGenSoft.QueueFunction)

A serverless Azure Function application that demonstrates queue-triggered processing using Azure Storage Queues.

## Overview

This project implements a queue-triggered Azure Function that automatically processes messages as they arrive in an Azure Storage Queue. The function demonstrates serverless architecture patterns and event-driven processing.

## Architecture
```
Azure Storage Queue → Queue Trigger → Azure Function → Process Message
```

- **Trigger**: Azure Storage Queue
- **Runtime**: .NET 8.0 (Isolated Worker)
- **Hosting**: Azure Functions Consumption Plan

## Prerequisites

- Visual Studio 2022
- .NET 8.0 SDK
- Azure Subscription
- Azure Functions Core Tools

## Azure Resources Required

1. **Resource Group** - Container for all resources
2. **Storage Account** - Hosts the queue and function metadata
3. **Storage Queue** - Input queue for messages
4. **Function App** - Hosts the serverless function

## Project Structure
```
/
├── QueueProcessor.cs          # Main function code
├── Program.cs                 # Host configuration
├── host.json                  # Function host settings
├── local.settings.json        # Local configuration (not in source control)
├── .gitignore                 # Git ignore rules
└── README.md                  # This file
```

## Setup Instructions

### 1. Create Azure Resources

#### Create Resource Group
```bash
az group create --name rg-queue-function --location eastus
```

#### Create Storage Account
```bash
az storage account create \
  --name stqueuefunction \
  --resource-group rg-queue-function \
  --location eastus \
  --sku Standard_LRS
```

#### Create Queue
```bash
az storage queue create \
  --name input-queue \
  --account-name stqueuefunction
```

#### Get Connection String
```bash
az storage account show-connection-string \
  --name stqueuefunction \
  --resource-group rg-queue-function
```

### 2. Configure Local Development

Create `local.settings.json`:
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "YOUR_CONNECTION_STRING_HERE",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
  }
}
```

### 3. Build and Run Locally
```bash
# Restore packages
dotnet restore

# Build project
dotnet build

# Run function locally
func start
```

Or press **F5** in Visual Studio.

## Function Code

### Basic Queue Trigger
```csharp
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

public class QueueProcessor
{
    private readonly ILogger<QueueProcessor> _logger;

    public QueueProcessor(ILogger<QueueProcessor> logger)
    {
        _logger = logger;
    }

    [Function(nameof(QueueProcessor))]
    public void Run(
        [QueueTrigger("input-queue", Connection = "AzureWebJobsStorage")] 
        string myQueueItem)
    {
        _logger.LogInformation($"Processing message: {myQueueItem}");
        
        // Add your business logic here
    }
}
```

### Advanced Queue Trigger with Metadata
```csharp
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

public class QueueProcessorAdvanced
{
    private readonly ILogger<QueueProcessorAdvanced> _logger;

    public QueueProcessorAdvanced(ILogger<QueueProcessorAdvanced> logger)
    {
        _logger = logger;
    }

    [Function(nameof(QueueProcessorAdvanced))]
    public void Run(
        [QueueTrigger("input-queue", Connection = "AzureWebJobsStorage")] 
        QueueMessage message)
    {
        _logger.LogInformation($"Message ID: {message.MessageId}");
        _logger.LogInformation($"Dequeue Count: {message.DequeueCount}");
        _logger.LogInformation($"Content: {message.MessageText}");
        
        // Process message
        ProcessMessage(message.MessageText);
    }

    private void ProcessMessage(string content)
    {
        // Your business logic here
    }
}
```

## Testing

### Send Test Message via Azure Portal

1. Navigate to Storage Account > Queues > input-queue
2. Click **+ Add message**
3. Enter message text
4. Click **OK**
5. Observe function execution in logs

### Send Test Message via Azure CLI
```bash
az storage message put \
  --queue-name input-queue \
  --content "Test message" \
  --account-name stqueuefunction
```

### Send Test Message via Code
```csharp
using Azure.Storage.Queues;

string connectionString = "YOUR_CONNECTION_STRING";
QueueClient queueClient = new QueueClient(connectionString, "input-queue");

await queueClient.SendMessageAsync("Test message from code");
```

## Configuration

### host.json

Configure queue behavior:
```json
{
  "version": "2.0",
  "logging": {
    "applicationInsights": {
      "samplingSettings": {
        "isEnabled": true,
        "maxTelemetryItemsPerSecond": 20
      }
    }
  },
  "extensions": {
    "queues": {
      "maxPollingInterval": "00:00:02",
      "visibilityTimeout": "00:00:30",
      "batchSize": 16,
      "maxDequeueCount": 5,
      "newBatchThreshold": 8
    }
  }
}
```

### Configuration Options

| Setting | Description | Default |
|---------|-------------|---------|
| maxPollingInterval | Maximum time between queue polls | 00:00:02 |
| visibilityTimeout | Time message is invisible during processing | 00:00:30 |
| batchSize | Number of messages to retrieve in parallel | 16 |
| maxDequeueCount | Max retry attempts before poison queue | 5 |
| newBatchThreshold | Threshold to fetch new batch | 8 |

## Deployment

### Deploy via Visual Studio

1. Right-click project > **Publish**
2. Select **Azure** > **Azure Function App (Windows)**
3. Create new or select existing Function App
4. Click **Publish**

### Deploy via Azure CLI
```bash
# Create Function App
az functionapp create \
  --resource-group rg-queue-function \
  --consumption-plan-location eastus \
  --runtime dotnet-isolated \
  --runtime-version 8 \
  --functions-version 4 \
  --name func-queue-processor \
  --storage-account stqueuefunction

# Deploy code
func azure functionapp publish func-queue-processor
```

## Message Processing Flow
```
1. Message arrives in queue
   ↓
2. Function triggered automatically
   ↓
3. Message becomes invisible (visibility timeout)
   ↓
4. Function processes message
   ↓
5a. Success → Message deleted (acknowledged)
5b. Failure → Message becomes visible → Retry
   ↓
6. After max retries → Message moved to poison queue
```

## Error Handling & Retries

### Automatic Retry

Messages are automatically retried on failure:
- **Default retries**: 5 attempts
- **Visibility timeout**: 30 seconds between retries
- **Exponential backoff**: Automatic

### Poison Queue

After maximum retries, messages move to: `{queue-name}-poison`

Example: `input-queue` → `input-queue-poison`

### Custom Error Handling
```csharp
[Function(nameof(QueueProcessor))]
public void Run(
    [QueueTrigger("input-queue", Connection = "AzureWebJobsStorage")] 
    QueueMessage message)
{
    try
    {
        _logger.LogInformation($"Processing message {message.MessageId}");
        
        // Your logic here
        ProcessMessage(message.MessageText);
        
        _logger.LogInformation("Message processed successfully");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"Error processing message {message.MessageId}");
        
        // Re-throw to trigger retry
        throw;
    }
}
```

## Monitoring

### View Logs in Azure Portal

1. Navigate to Function App
2. Click **Functions** > Your function name
3. Click **Monitor** tab
4. View execution history and logs

### Application Insights

Enable Application Insights for advanced monitoring:
```bash
az monitor app-insights component create \
  --app func-queue-insights \
  --location eastus \
  --resource-group rg-queue-function
```

Connect to Function App:
```bash
az functionapp config appsettings set \
  --name func-queue-processor \
  --resource-group rg-queue-function \
  --settings APPINSIGHTS_INSTRUMENTATIONKEY=<your-key>
```

## Scaling

### Automatic Scaling

Azure Functions automatically scales based on queue depth:
- More messages → More instances
- Fewer messages → Fewer instances
- No messages → Scale to zero

### Scaling Behavior

- **Target**: 1 instance per 1000 queue messages
- **Maximum instances**: Configurable (default: 200)
- **Scale-out**: Within seconds
- **Scale-in**: Gradual (5-10 minutes)

## Best Practices

1. **Idempotency** - Ensure message processing is idempotent (safe to retry)
2. **Small Messages** - Keep messages under 64KB
3. **Poison Queue Monitoring** - Monitor and handle poison queue messages
4. **Logging** - Use structured logging for troubleshooting
5. **Error Handling** - Implement proper exception handling
6. **Visibility Timeout** - Set appropriate timeout based on processing time
7. **Connection Strings** - Store in Azure Key Vault for production

## Troubleshooting

### Function Not Triggering

- Verify connection string in configuration
- Check queue name matches exactly (case-sensitive)
- Ensure messages exist in queue
- Check Application Insights for errors

### Messages Not Processing

- Verify `AzureWebJobsStorage` setting
- Check function is running (not stopped)
- Review logs for exceptions
- Check visibility timeout isn't too short

### High Latency

- Reduce `maxPollingInterval` in host.json
- Increase `batchSize` for high throughput
- Check for processing bottlenecks

## Security

### Connection String Management

**Development:**
```json
// local.settings.json (not in source control)
{
  "Values": {
    "AzureWebJobsStorage": "DefaultEndpointsProtocol=https;..."
  }
}
```

**Production:**
```bash
# Store in Key Vault
az keyvault secret set \
  --vault-name your-keyvault \
  --name StorageConnectionString \
  --value "DefaultEndpointsProtocol=https;..."

# Reference in Function App
az functionapp config appsettings set \
  --name func-queue-processor \
  --resource-group rg-queue-function \
  --settings AzureWebJobsStorage=@Microsoft.KeyVault(SecretUri=https://your-keyvault.vault.azure.net/secrets/StorageConnectionString/)
```

### Managed Identity

Use Managed Identity instead of connection strings:
```csharp
services.AddAzureClients(builder =>
{
    builder.AddQueueServiceClient(new Uri("https://youraccount.queue.core.windows.net"))
           .WithCredential(new DefaultAzureCredential());
});
```

## Performance Optimization

### Batch Processing
```csharp
[Function(nameof(QueueProcessor))]
public async Task Run(
    [QueueTrigger("input-queue", Connection = "AzureWebJobsStorage")] 
    string[] messages)
{
    // Process multiple messages
    foreach (var message in messages)
    {
        await ProcessMessageAsync(message);
    }
}
```

### Parallel Processing

Configure in host.json:
```json
{
  "extensions": {
    "queues": {
      "batchSize": 32,
      "newBatchThreshold": 16
    }
  }
}
```

## Resources

- [Azure Functions Documentation](https://docs.microsoft.com/azure/azure-functions/)
- [Azure Queue Storage Documentation](https://docs.microsoft.com/azure/storage/queues/)
- [Best Practices for Azure Functions](https://docs.microsoft.com/azure/azure-functions/functions-best-practices)

## License

This project is licensed under the MIT License.

## Contributing

Contributions are welcome! Please open an issue or submit a pull request.

---

**Note**: This is a sample implementation. Customize according to your specific requirements and organizational standards.
