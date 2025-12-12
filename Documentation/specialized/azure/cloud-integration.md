# Azure Integration - Cloud Service Patterns with AOP

## Overview
The Aspectacular AOP Framework provides Azure-specific integration for Azure Queue Storage, enabling message multicasting, queue monitoring, and cloud-based distributed system patterns with full AOP capabilities (logging, retry, error handling).

**Supported Azure Services:**
- Azure Queue Storage (Cloud Queues)
- Message multicasting (one source → multiple destinations)
- Queue monitoring and processing

**Current SDK:** Azure SDK v4 (unofficial package)  
**Migration Target:** Azure SDK v12+ (official Microsoft.Azure.Storage.Queue)  
**Status:** ⚠️ **CRITICAL** - Using unsupported unofficial package with supply chain risk

**Module:** `Aspectacular.Framework.Azure`  
**Namespaces:** `Aspectacular`

---

## Core Azure Components

### 1. Azure Queue Multicaster (AzureQueueMulticaster)

#### Purpose
Implements publish-subscribe pattern for Azure Queue Storage by replicating messages from a single source queue to multiple destination queues with optional message transformation.

**Source:** `NET 4.5/Aspectacular.Framework.Azure.45/AzureQueueMulticaster.cs`

#### Architecture

```
Source Queue                Destination Queues
(Single)                    (Multiple)
    │                              
    │  ┌─────────────────┐         
    └──┤  Multicaster    ├──────→  Queue 1 (Processing)
       │  with AOP       ├──────→  Queue 2 (Audit)
       │  Aspects         ├──────→  Queue 3 (Analytics)
       └─────────────────┘         
       
Message Flow:
1. Poll source queue (configurable interval)
2. Remove message from source
3. Optional: Transform message
4. Post to all destination queues
5. Retry on transient failures
```

#### Components

##### AzureQueueMulticastRoute

```csharp
/// <summary>
/// Represents a route between a single source queue and multiple destination queues.
/// </summary>
public class AzureQueueMulticastRoute : IDisposable, ICallLogger
{
    // Source queue configuration
    public AzureSourceQueueConnection SourceQueue { get; set; }
    
    // Destination queues collection
    public List<AzureDestinationQueueConnection> DestinationQueues { get; set; }
    
    // Optional message transformation hook
    [XmlIgnore]
    public Func<CloudQueueMessage, CloudQueueMessage> OptionalThreadSafeMessageTransformer { get; set; }
    
    // Starts monitoring and multicasting
    public void Start();
    
    // Stops monitoring
    public void Stop();
    
    // Disposes resources
    public void Dispose();
}
```

##### AzureSourceQueueConnection

```csharp
/// <summary>
/// Configuration for source queue (where messages are consumed from).
/// </summary>
public class AzureSourceQueueConnection
{
    // Azure storage connection string
    public string ConnectionString { get; set; }
    
    // Queue name
    public string QueueName { get; set; }
    
    // Polling interval (milliseconds)
    public int PollingIntervalMs { get; set; } = 1000;
    
    // Maximum messages per poll
    public int MaxMessagesPerPoll { get; set; } = 32;
    
    // Message visibility timeout (seconds)
    public int VisibilityTimeoutSeconds { get; set; } = 30;
}
```

##### AzureDestinationQueueConnection

```csharp
/// <summary>
/// Configuration for destination queue (where messages are published to).
/// </summary>
public class AzureDestinationQueueConnection
{
    // Azure storage connection string
    public string ConnectionString { get; set; }
    
    // Queue name
    public string QueueName { get; set; }
    
    // Time-to-live for messages (optional)
    public TimeSpan? MessageTtl { get; set; }
    
    // Retry policy for failures
    public IRetryPolicy RetryPolicy { get; set; }
}
```

#### Usage Patterns

**Pattern 1: Basic Message Multicasting**

```csharp
// Configure source queue
var sourceQueue = new AzureSourceQueueConnection
{
    ConnectionString = "DefaultEndpointsProtocol=https;AccountName=myaccount;AccountKey=...",
    QueueName = "orders-incoming",
    PollingIntervalMs = 1000,  // Poll every second
    MaxMessagesPerPoll = 32     // Process up to 32 messages per poll
};

// Configure destination queues
var destinationQueues = new List<AzureDestinationQueueConnection>
{
    new AzureDestinationQueueConnection
    {
        ConnectionString = sourceQueue.ConnectionString,
        QueueName = "orders-processing",
        MessageTtl = TimeSpan.FromDays(7)
    },
    new AzureDestinationQueueConnection
    {
        ConnectionString = sourceQueue.ConnectionString,
        QueueName = "orders-audit",
        MessageTtl = TimeSpan.FromDays(90)  // Longer retention for audit
    },
    new AzureDestinationQueueConnection
    {
        ConnectionString = sourceQueue.ConnectionString,
        QueueName = "orders-analytics",
        MessageTtl = TimeSpan.FromDays(30)
    }
};

// Create and configure route
var route = new AzureQueueMulticastRoute
{
    SourceQueue = sourceQueue,
    DestinationQueues = destinationQueues
};

// Start multicasting
route.Start();

// Run indefinitely (or until Stop() called)
Console.WriteLine("Multicasting started. Press Enter to stop...");
Console.ReadLine();

// Stop and cleanup
route.Stop();
route.Dispose();
```

**Pattern 2: Message Transformation**

```csharp
var route = new AzureQueueMulticastRoute
{
    SourceQueue = sourceQueue,
    DestinationQueues = destinationQueues,
    
    // Transform messages before posting to destinations
    OptionalThreadSafeMessageTransformer = (message) =>
    {
        // Deserialize original message
        var orderData = JsonConvert.DeserializeObject<Order>(message.AsString);
        
        // Add metadata
        orderData.ProcessedTimestamp = DateTime.UtcNow;
        orderData.RouteId = "MulticastRoute-1";
        
        // Mask sensitive data for analytics queue
        if (currentDestinationQueue.QueueName == "orders-analytics")
        {
            orderData.CustomerEmail = MaskEmail(orderData.CustomerEmail);
            orderData.CreditCardNumber = "XXXX-XXXX-XXXX-" + orderData.CreditCardNumber.Substring(15);
        }
        
        // Create transformed message
        var transformedJson = JsonConvert.SerializeObject(orderData);
        return new CloudQueueMessage(transformedJson);
    }
};

route.Start();
```

**Pattern 3: Multicasting with AOP Aspects**

```csharp
// Define aspects for monitoring and resilience
var aspects = new Aspect[]
{
    new LoggingAspect(),                           // Log all multicast operations
    new PerformanceAspect(),                       // Track multicast performance
    new RetryAspect(maxAttempts: 5,                // Retry on Azure transient failures
                    delayMillisec: 2000, 
                    exponentialBackoff: true),
    new ErrorHandlingAspect(),                     // Graceful error handling
    new MetricsAspect()                            // Track success/failure metrics
};

// Apply aspects to multicaster
// (Requires wrapping multicast operations in AOP proxy)
var proxy = Proxy.Create<AzureQueueMulticaster>(aspects);

proxy.Invoke(multicaster => multicaster.ProcessMessages());
```

**Pattern 4: Fan-Out for Microservices**

```csharp
/// <summary>
/// Fan-out pattern: Single event → Multiple microservices
/// </summary>
public class OrderEventMulticaster
{
    private readonly AzureQueueMulticastRoute _route;
    
    public OrderEventMulticaster()
    {
        _route = new AzureQueueMulticastRoute
        {
            SourceQueue = new AzureSourceQueueConnection
            {
                ConnectionString = ConfigurationManager.ConnectionStrings["AzureStorage"].ConnectionString,
                QueueName = "order-events",
                PollingIntervalMs = 500  // Fast polling for real-time processing
            },
            DestinationQueues = new List<AzureDestinationQueueConnection>
            {
                // Inventory Service
                new AzureDestinationQueueConnection
                {
                    ConnectionString = ConfigurationManager.ConnectionStrings["AzureStorage"].ConnectionString,
                    QueueName = "inventory-updates"
                },
                // Shipping Service
                new AzureDestinationQueueConnection
                {
                    ConnectionString = ConfigurationManager.ConnectionStrings["AzureStorage"].ConnectionString,
                    QueueName = "shipping-requests"
                },
                // Notification Service
                new AzureDestinationQueueConnection
                {
                    ConnectionString = ConfigurationManager.ConnectionStrings["AzureStorage"].ConnectionString,
                    QueueName = "customer-notifications"
                },
                // Analytics Service
                new AzureDestinationQueueConnection
                {
                    ConnectionString = ConfigurationManager.ConnectionStrings["AzureStorage"].ConnectionString,
                    QueueName = "analytics-events"
                }
            },
            
            // Add correlation ID for distributed tracing
            OptionalThreadSafeMessageTransformer = AddCorrelationId
        };
    }
    
    private CloudQueueMessage AddCorrelationId(CloudQueueMessage message)
    {
        var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(message.AsString);
        
        if (!data.ContainsKey("CorrelationId"))
        {
            data["CorrelationId"] = Guid.NewGuid().ToString();
        }
        
        data["MulticastTimestamp"] = DateTime.UtcNow;
        
        return new CloudQueueMessage(JsonConvert.SerializeObject(data));
    }
    
    public void Start()
    {
        _route.Start();
    }
    
    public void Stop()
    {
        _route.Stop();
        _route.Dispose();
    }
}
```

#### Multicast Flow with Aspects

```
┌──────────────────────────────────────────────────────────────┐
│ AOP Aspect Pipeline                                          │
└──────────────────────────────────────────────────────────────┘
   ↓
1. LoggingAspect.Step_2_BeforeTryingMethodExec()
   → Log: "Starting multicast from queue: orders-incoming"
   ↓
2. RetryAspect.Step_2_BeforeTryingMethodExec()
   → Prepare retry logic for transient failures
   ↓
3. Poll source queue (GetMessages)
   → Retrieve 1-32 messages
   ↓
4. For each message:
   a. OptionalThreadSafeMessageTransformer(message)
      → Transform/enrich message
   b. Post to Destination 1 (orders-processing)
      → Retry on failure (RetryAspect)
   c. Post to Destination 2 (orders-audit)
      → Retry on failure (RetryAspect)
   d. Post to Destination 3 (orders-analytics)
      → Retry on failure (RetryAspect)
   e. Delete from source queue
      → Only after all destinations successful
   ↓
5. PerformanceAspect.Step_3_BeforeMassagingReturnedResult()
   → Log: "Multicast completed in 125ms, 10 messages processed"
   ↓
6. LoggingAspect.Step_4_Optional_AfterSuccessfulCallCompletion()
   → Log success metrics
```

#### Error Handling

**Transient Failures:**
- Network timeouts
- Azure throttling (HTTP 503)
- Temporary service unavailability

**Handling Strategy:**
```csharp
var retryAspect = new RetryAspect(
    maxAttempts: 5,
    delayMillisec: 2000,
    exponentialBackoff: true  // 2s, 4s, 8s, 16s, 32s
);

// Transient failures automatically retried
// Non-transient failures logged and message moved to poison queue
```

**Poison Message Handling:**
```csharp
// Configure destination with dead-letter queue
var destination = new AzureDestinationQueueConnection
{
    ConnectionString = connectionString,
    QueueName = "orders-processing",
    PoisonMessageQueue = "orders-processing-poison",  // Failed messages go here
    MaxDequeueCount = 5  // Move to poison queue after 5 failed attempts
};
```

---

### 2. Azure Queue Monitor (AzureQueueMonitor)

#### Purpose
Monitors Azure Queue Storage queues and processes messages with configurable polling intervals and concurrent processing.

**Source:** `NET 4.5/Aspectacular.Framework.Azure.45/AzureQueueMonitor.cs`

#### Implementation

```csharp
/// <summary>
/// Monitors Azure Queue and processes messages with callback.
/// </summary>
public class AzureQueueMonitor : IDisposable
{
    public AzureQueueMonitor(
        string connectionString,
        string queueName,
        Action<CloudQueueMessage> messageProcessor,
        int pollingIntervalMs = 1000,
        int maxConcurrentMessages = 1
    );
    
    // Start monitoring
    public void Start();
    
    // Stop monitoring
    public void Stop();
    
    // Dispose resources
    public void Dispose();
}
```

#### Usage Patterns

**Pattern 1: Simple Queue Processing**

```csharp
var monitor = new AzureQueueMonitor(
    connectionString: "DefaultEndpointsProtocol=https;AccountName=...",
    queueName: "orders-processing",
    messageProcessor: (message) =>
    {
        try
        {
            // Process message
            var order = JsonConvert.DeserializeObject<Order>(message.AsString);
            ProcessOrder(order);
            
            // Delete message on success
            // (handled automatically by monitor)
        }
        catch (Exception ex)
        {
            // Log error
            Logger.Error($"Failed to process message: {ex.Message}");
            
            // Message will be retried based on visibility timeout
            throw;
        }
    },
    pollingIntervalMs: 1000,
    maxConcurrentMessages: 10  // Process up to 10 messages concurrently
);

monitor.Start();
```

**Pattern 2: Queue Processing with AOP**

```csharp
public class OrderProcessor
{
    private readonly Proxy<OrderProcessingService> _serviceProxy;
    
    public OrderProcessor()
    {
        var aspects = new Aspect[]
        {
            new LoggingAspect(),
            new RetryAspect(maxAttempts: 3),
            new PerformanceAspect(),
            new TransactionAspect()  // Database transaction
        };
        
        _serviceProxy = Proxy.Create<OrderProcessingService>(aspects);
    }
    
    public void ProcessMessage(CloudQueueMessage message)
    {
        var order = JsonConvert.DeserializeObject<Order>(message.AsString);
        
        // Process with full AOP pipeline
        _serviceProxy.Invoke(svc => svc.ProcessOrder(order));
    }
}

// Configure monitor
var processor = new OrderProcessor();
var monitor = new AzureQueueMonitor(
    connectionString,
    "orders-processing",
    processor.ProcessMessage,
    pollingIntervalMs: 500,
    maxConcurrentMessages: 20
);

monitor.Start();
```

---

## Cloud Patterns with AOP

### Pattern 1: Distributed Transaction Saga

```csharp
/// <summary>
/// Implements Saga pattern for distributed transactions across microservices.
/// Uses Azure Queues for choreography.
/// </summary>
public class OrderSagaOrchestrator
{
    private readonly AzureQueueMulticastRoute _route;
    
    public void InitiateSaga(Order order)
    {
        // Step 1: Publish order created event
        var message = new
        {
            SagaId = Guid.NewGuid(),
            OrderId = order.OrderId,
            EventType = "OrderCreated",
            Timestamp = DateTime.UtcNow,
            Data = order
        };
        
        PublishToQueue("saga-events", message);
        
        // Multicaster distributes to:
        // - inventory-service (reserve inventory)
        // - payment-service (process payment)
        // - shipping-service (prepare shipment)
        
        // Each service publishes success/failure events back to saga-events queue
        // Orchestrator monitors and coordinates compensating transactions if needed
    }
    
    public void HandleSagaEvent(CloudQueueMessage queueMessage)
    {
        var @event = JsonConvert.DeserializeObject<SagaEvent>(queueMessage.AsString);
        
        switch (@event.EventType)
        {
            case "InventoryReserved":
                // Continue saga
                break;
            case "PaymentProcessed":
                // Continue saga
                break;
            case "InventoryReservationFailed":
                // Trigger compensating transactions
                CompensateOrder(@event.SagaId);
                break;
        }
    }
}
```

### Pattern 2: Event Sourcing with Azure Queues

```csharp
/// <summary>
/// Event sourcing pattern: Store all state changes as events in queue.
/// </summary>
public class EventStore
{
    private readonly CloudQueueClient _queueClient;
    
    public void PublishEvent(DomainEvent @event)
    {
        var aspects = new Aspect[]
        {
            new RetryAspect(maxAttempts: 5, exponentialBackoff: true),
            new LoggingAspect()
        };
        
        var proxy = Proxy.Create<EventStore>(aspects);
        
        proxy.Invoke(store =>
        {
            var eventMessage = JsonConvert.SerializeObject(@event);
            var queue = _queueClient.GetQueueReference("domain-events");
            queue.AddMessage(new CloudQueueMessage(eventMessage));
        });
    }
    
    public List<DomainEvent> ReplayEvents(Guid aggregateId)
    {
        // Read all events for aggregate from persistent storage
        // Rebuild aggregate state
        return GetEventsForAggregate(aggregateId);
    }
}
```

### Pattern 3: Priority Queue Processing

```csharp
/// <summary>
/// Process messages from multiple queues with priority ordering.
/// </summary>
public class PriorityQueueProcessor
{
    private readonly List<AzureQueueMonitor> _monitors;
    
    public PriorityQueueProcessor()
    {
        _monitors = new List<AzureQueueMonitor>
        {
            // High priority: Poll frequently, process immediately
            new AzureQueueMonitor(
                connectionString, 
                "orders-high-priority",
                ProcessHighPriorityOrder,
                pollingIntervalMs: 100,
                maxConcurrentMessages: 50
            ),
            
            // Normal priority: Standard polling
            new AzureQueueMonitor(
                connectionString,
                "orders-normal",
                ProcessNormalOrder,
                pollingIntervalMs: 1000,
                maxConcurrentMessages: 20
            ),
            
            // Low priority: Infrequent polling
            new AzureQueueMonitor(
                connectionString,
                "orders-low-priority",
                ProcessLowPriorityOrder,
                pollingIntervalMs: 5000,
                maxConcurrentMessages: 5
            )
        };
    }
    
    public void Start()
    {
        _monitors.ForEach(m => m.Start());
    }
}
```

---

## Migration Guide

### Current: Azure SDK v4 (Unofficial) → Azure SDK v12 (Official)

**Status:** ⚠️ **CRITICAL PRIORITY** - Supply chain security risk

#### Breaking Changes

**1. Namespace Changes**
```csharp
// Old (Azure SDK v4)
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;

// New (Azure SDK v12)
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
```

**2. Client Initialization**
```csharp
// Old
var storageAccount = CloudStorageAccount.Parse(connectionString);
var queueClient = storageAccount.CreateCloudQueueClient();
var queue = queueClient.GetQueueReference("myqueue");

// New
var queueClient = new QueueClient(connectionString, "myqueue");
```

**3. Message Operations**
```csharp
// Old
CloudQueueMessage message = new CloudQueueMessage("content");
queue.AddMessage(message);
var messages = queue.GetMessages(32);

// New
await queueClient.SendMessageAsync("content");
var messages = await queueClient.ReceiveMessagesAsync(maxMessages: 32);
```

**4. Async-First API**
```csharp
// Old (sync methods available)
queue.AddMessage(message);
var messages = queue.GetMessages(32);

// New (async-first)
await queueClient.SendMessageAsync(message);
var messages = await queueClient.ReceiveMessagesAsync(32);
```

#### Migration Effort

**Affected Files:**
- `AzureQueueMulticaster.cs` - Update queue operations
- `AzureQueueMonitor.cs` - Update polling logic
- `AzureQueueConnection.cs` - Update connection handling

**Estimated Effort:** MEDIUM (2-3 weeks)

**Migration Steps:**
1. **Week 1:** Update NuGet packages, fix compilation errors
2. **Week 2:** Convert synchronous operations to async/await
3. **Week 3:** Testing and validation

**Testing Requirements:**
- Unit tests for all queue operations
- Integration tests with Azure Storage Emulator
- Load testing (1000+ messages/second)
- Failure scenario testing (network failures, throttling)

---

## Best Practices

### Queue Design
- ✅ **DO:** Use descriptive queue names (`orders-processing`, not `queue1`)
- ✅ **DO:** Implement poison message handling (dead-letter queues)
- ✅ **DO:** Set appropriate message TTL (avoid infinite retention)
- ✅ **DO:** Use visibility timeout to prevent duplicate processing
- ❌ **DON'T:** Store large payloads in messages (use blob reference instead)
- ❌ **DON'T:** Exceed 64KB message size limit

### Multicasting
- ✅ **DO:** Use idempotent message processing (handle duplicates)
- ✅ **DO:** Add correlation IDs for distributed tracing
- ✅ **DO:** Implement retry with exponential backoff
- ✅ **DO:** Monitor message lag (queue depth)
- ❌ **DON'T:** Assume message order (queues don't guarantee FIFO)
- ❌ **DON'T:** Process messages synchronously (use concurrent processing)

### Security
- ✅ **DO:** Use managed identities instead of connection strings (when possible)
- ✅ **DO:** Rotate storage account keys regularly
- ✅ **DO:** Use HTTPS for all queue operations
- ✅ **DO:** Mask sensitive data in analytics/audit queues
- ❌ **DON'T:** Log connection strings or account keys
- ❌ **DON'T:** Share connection strings across environments

### Performance
- ✅ **DO:** Batch message operations (send/receive multiple)
- ✅ **DO:** Use concurrent processing (adjust maxConcurrentMessages)
- ✅ **DO:** Tune polling intervals based on load
- ✅ **DO:** Monitor Azure throttling (HTTP 503 responses)
- ❌ **DON'T:** Poll too frequently (increases costs and throttling risk)
- ❌ **DON'T:** Create new queue clients per operation (reuse clients)

---

## Performance Characteristics

| Operation | Latency | Throughput |
|-----------|---------|------------|
| SendMessage | 20-50ms | 2000 msg/sec (per queue) |
| ReceiveMessages (32) | 50-100ms | 10,000 msg/sec |
| Multicast (1 → 3) | 100-200ms | 1000 msg/sec |
| Delete Message | 20-30ms | N/A |

**Note:** Performance varies based on Azure region, queue depth, and network latency.

---

## Related Documentation
- [Retry Logic](../../behavior/business-logic.md#retry-logic-business-rules)
- [Technical Debt - Azure SDK](../../technical-debt/outdated-components.md#azure-sdk)
- [Security Vulnerabilities](../../technical-debt/security-vulnerabilities.md#azure-supply-chain-risk)

---

**Document Version:** 1.0  
**Framework Version:** Aspectacular Azure Module (NET 4.5)  
**Last Updated:** Migration Planning Phase  
**Migration Priority:** CRITICAL (Security Risk)
