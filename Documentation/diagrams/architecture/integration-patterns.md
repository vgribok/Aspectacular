# Integration Patterns - External System Integration Visualization

## Overview
This diagram illustrates common integration patterns when using the Aspectacular AOP Framework with external systems, including databases, web services, cloud services, and messaging systems.

---

## Pattern 1: Database Integration with Entity Framework

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ APPLICATION LAYER                                                           │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Controller / Service Class                                                │
│  ┌───────────────────────────────────────────────────────────────────────┐ │
│  │  public class OrderService                                            │ │
│  │  {                                                                     │ │
│  │      public Order GetOrder(int orderId)                               │ │
│  │      {                                                                 │ │
│  │          // Business logic here                                       │ │
│  │      }                                                                 │ │
│  │  }                                                                     │ │
│  └───────────────────────────────────────────────────────────────────────┘ │
│                                │                                            │
└────────────────────────────────┼────────────────────────────────────────────┘
                                 │
                                 │ Wrapped by AOP Proxy
                                 ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ ASPECTACULAR FRAMEWORK (AOP Layer)                                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  var aspects = new Aspect[] {                                              │
│      new LoggingAspect(),              // Log all DB operations            │
│      new CacheAspect(),                 // Cache query results             │
│      new RetryAspect(maxAttempts: 3),  // Retry on transient failures      │
│      new PerformanceAspect()            // Track query performance         │
│  };                                                                         │
│                                                                             │
│  var proxy = EfAOP.GetDbProxy<MyDbContext>(aspects);                      │
│                                                                             │
│  var result = proxy.Invoke(db =>                                           │
│      db.Orders                                                              │
│        .Include(o => o.Customer)                                            │
│        .FirstOrDefault(o => o.OrderId == orderId)                          │
│  );                                                                         │
│                                │                                            │
└────────────────────────────────┼────────────────────────────────────────────┘
                                 │
                                 │ Generated SQL + Interception
                                 ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ ENTITY FRAMEWORK LAYER                                                      │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  DbContext / ObjectContext                                                 │
│  ┌───────────────────────────────────────────────────────────────────────┐ │
│  │  • Query translation (LINQ → SQL)                                     │ │
│  │  • Change tracking                                                    │ │
│  │  • Connection management                                              │ │
│  │  • Transaction coordination                                           │ │
│  └───────────────────────────────────────────────────────────────────────┘ │
│                                │                                            │
└────────────────────────────────┼────────────────────────────────────────────┘
                                 │
                                 │ ADO.NET Commands
                                 ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ DATABASE (SQL Server)                                                       │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  SELECT o.*, c.*                                                            │
│  FROM Orders o                                                              │
│  INNER JOIN Customers c ON o.CustomerId = c.CustomerId                     │
│  WHERE o.OrderId = @p0                                                      │
│                                                                             │
│  ┌───────────────────────────────────────────────────────────────────────┐ │
│  │ Result Set:                                                            │ │
│  │  OrderId | CustomerName | OrderTotal | ...                            │ │
│  │  123     | Acme Corp    | 1,250.00   | ...                            │ │
│  └───────────────────────────────────────────────────────────────────────┘ │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘

Flow with Aspects:
  1. LoggingAspect:      Logs "Query: GetOrder(123)" + Start Time
  2. CacheAspect:        Checks cache → MISS → Continue
  3. RetryAspect:        Prepares retry logic for transient failures
  4. PerformanceAspect:  Starts performance timer
  
  → Execute Query → SQL Server
  
  5. PerformanceAspect:  Stops timer (45ms) + Records metric
  6. CacheAspect:        Stores result in cache (TTL: 5 minutes)
  7. LoggingAspect:      Logs "Query completed in 45ms"
  
  → Return Order object to caller
```

---

## Pattern 2: Web API Integration with Retry and Circuit Breaker

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ CLIENT APPLICATION                                                          │
└─────────────────────────────────────────────────────────────────────────────┘
                                 │
                                 │ HTTP Request
                                 ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ ASPECTACULAR PROXY WRAPPER                                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  public class PaymentGatewayClient                                          │
│  {                                                                          │
│      public PaymentResult ProcessPayment(Payment payment)                  │
│      {                                                                      │
│          // Calls external payment API                                     │
│          var result = httpClient.PostAsync(...);                           │
│          return result;                                                     │
│      }                                                                      │
│  }                                                                          │
│                                                                             │
│  // AOP Configuration                                                       │
│  var aspects = new Aspect[] {                                              │
│      new CircuitBreakerAspect(                                             │
│          failureThreshold: 5,      // Open after 5 failures               │
│          timeout: TimeSpan.FromSeconds(30)  // Test after 30s              │
│      ),                                                                     │
│      new RetryAspect(                                                       │
│          maxAttempts: 3,                                                    │
│          exponentialBackoff: true                                           │
│      ),                                                                     │
│      new TimeoutAspect(timeout: TimeSpan.FromSeconds(10)),                 │
│      new LoggingAspect()                                                    │
│  };                                                                         │
│                                                                             │
│  var proxy = Proxy.Create<PaymentGatewayClient>(aspects);                 │
│                                │                                            │
└────────────────────────────────┼────────────────────────────────────────────┘
                                 │
                                 │ HTTP POST with Circuit Breaker
                                 ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ EXTERNAL PAYMENT GATEWAY (REST API)                                        │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  POST /api/payments                                                         │
│  {                                                                          │
│    "amount": 99.99,                                                         │
│    "currency": "USD",                                                       │
│    "cardToken": "tok_12345"                                                 │
│  }                                                                          │
│                                │                                            │
│                                ▼                                            │
│  Response 200 OK:                                                           │
│  {                                                                          │
│    "transactionId": "txn_67890",                                            │
│    "status": "approved"                                                     │
│  }                                                                          │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘

Circuit Breaker State Machine:

  CLOSED (Normal Operation)
    │
    │ 5 consecutive failures
    ▼
  OPEN (Reject All Calls)
    │ Return cached/default response immediately
    │ No external calls made
    │
    │ After 30 seconds
    ▼
  HALF-OPEN (Test Call)
    │
    ├─ Test call SUCCESS → CLOSED (Resume normal)
    └─ Test call FAILURE → OPEN (Wait another 30s)

Retry with Exponential Backoff:
  Attempt 1: Fail (timeout) → Wait 2s
  Attempt 2: Fail (503) → Wait 4s
  Attempt 3: Success → Return result
```

---

## Pattern 3: Azure Queue-Based Message Processing

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ MESSAGE PUBLISHER                                                           │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  public class OrderEventPublisher                                           │
│  {                                                                          │
│      public void PublishOrderCreated(Order order)                          │
│      {                                                                      │
│          var message = new { EventType = "OrderCreated", Data = order };   │
│          queueClient.AddMessage(JsonConvert.SerializeObject(message));     │
│      }                                                                      │
│  }                                                                          │
│                                │                                            │
└────────────────────────────────┼────────────────────────────────────────────┘
                                 │
                                 │ Publish message
                                 ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ AZURE QUEUE STORAGE - Source Queue: "order-events"                         │
│ ─────────────────────────────────────────────────────────────────────────── │
│  Messages:                                                                  │
│  [1] { "EventType": "OrderCreated", "OrderId": 123, ... }                  │
│  [2] { "EventType": "OrderCreated", "OrderId": 124, ... }                  │
│  [3] { "EventType": "OrderCreated", "OrderId": 125, ... }                  │
└─────────────────────────────────────────────────────────────────────────────┘
                                 │
                                 │ Poll (every 1 second)
                                 ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ ASPECTACULAR QUEUE MULTICASTER                                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  var route = new AzureQueueMulticastRoute                                   │
│  {                                                                          │
│      SourceQueue = new AzureSourceQueueConnection                          │
│      {                                                                      │
│          QueueName = "order-events",                                        │
│          PollingIntervalMs = 1000,                                          │
│          MaxMessagesPerPoll = 32                                            │
│      },                                                                     │
│      DestinationQueues = new List<AzureDestinationQueueConnection>         │
│      {                                                                      │
│          new() { QueueName = "inventory-updates" },                        │
│          new() { QueueName = "shipping-requests" },                        │
│          new() { QueueName = "customer-notifications" },                   │
│          new() { QueueName = "analytics-events" }                          │
│      },                                                                     │
│      OptionalThreadSafeMessageTransformer = TransformMessage               │
│  };                                                                         │
│                                                                             │
│  route.Start();  // Begin multicasting                                     │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
                                 │
                                 │ Multicast to destinations
                                 ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ DESTINATION QUEUES                                                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌──────────────────────┐   ┌──────────────────────┐                       │
│  │ inventory-updates    │   │ shipping-requests    │                       │
│  │ ──────────────────── │   │ ──────────────────── │                       │
│  │ [1] Order 123        │   │ [1] Order 123        │                       │
│  │ [2] Order 124        │   │ [2] Order 124        │                       │
│  └──────────────────────┘   └──────────────────────┘                       │
│                                                                             │
│  ┌──────────────────────┐   ┌──────────────────────┐                       │
│  │ customer-            │   │ analytics-events     │                       │
│  │ notifications        │   │ ──────────────────── │                       │
│  │ ──────────────────── │   │ [1] Order 123 (anon.)│                       │
│  │ [1] Order 123        │   │ [2] Order 124 (anon.)│                       │
│  │ [2] Order 124        │   └──────────────────────┘                       │
│  └──────────────────────┘                                                   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
                                 │
                                 │ Processed by microservices
                                 ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ MICROSERVICES (Consumers)                                                   │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │ Inventory Service                                                    │  │
│  │ • Monitors "inventory-updates" queue                                 │  │
│  │ • Reserves stock for order                                           │  │
│  │ • Publishes "InventoryReserved" event                                │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │ Shipping Service                                                     │  │
│  │ • Monitors "shipping-requests" queue                                 │  │
│  │ • Creates shipment record                                            │  │
│  │ • Generates shipping label                                           │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │ Notification Service                                                 │  │
│  │ • Monitors "customer-notifications" queue                            │  │
│  │ • Sends order confirmation email                                     │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │ Analytics Service                                                    │  │
│  │ • Monitors "analytics-events" queue                                  │  │
│  │ • Aggregates order statistics                                        │  │
│  │ • Updates real-time dashboards                                       │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘

Message Transformation Example:
  Original Message (from source queue):
    { "EventType": "OrderCreated", "OrderId": 123, "CustomerEmail": "john@example.com", "CreditCard": "1234-5678-9012-3456" }
  
  Transformed for analytics queue (PII removed):
    { "EventType": "OrderCreated", "OrderId": 123, "CustomerEmail": "j***@example.com", "CreditCard": "****-****-****-3456", "CorrelationId": "9c8e7f16-..." }
```

---

## Pattern 4: Saga Pattern for Distributed Transactions

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ SAGA ORCHESTRATOR                                                           │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Order Processing Saga (Choreography Pattern)                              │
│                                                                             │
│  Step 1: Create Order                                                       │
│    ↓                                                                        │
│  Publish Event: "OrderCreated"                                              │
│    ↓                                                                        │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐                        │
│  │ Inventory   │  │ Payment     │  │ Shipping    │                        │
│  │ Service     │  │ Service     │  │ Service     │                        │
│  └─────────────┘  └─────────────┘  └─────────────┘                        │
│        ↓                ↓                ↓                                  │
│  Reserve Stock    Process       Prepare                                    │
│                   Payment        Shipment                                   │
│        ↓                ↓                ↓                                  │
│  Publish:         Publish:       Publish:                                  │
│  "InventoryOK"    "PaymentOK"    "ShippingOK"                              │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ ALL SUCCESS → Saga Complete                                         │   │
│  │   • Update order status: "Confirmed"                                │   │
│  │   • Send confirmation to customer                                   │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ ANY FAILURE → Compensating Transactions                             │   │
│  │   IF "PaymentFailed":                                               │   │
│  │     → Publish "ReleaseInventory"                                    │   │
│  │     → Update order status: "Cancelled"                              │   │
│  │     → Send cancellation to customer                                 │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘

Timeline Example (Success Path):

  T=0s:    OrderCreated event published
  T=0.1s:  Inventory service reserves stock → InventoryReserved
  T=0.2s:  Payment service charges card → PaymentProcessed
  T=0.3s:  Shipping service creates label → ShippingPrepared
  T=0.4s:  Saga orchestrator confirms all steps → Order Confirmed

Timeline Example (Failure Path):

  T=0s:    OrderCreated event published
  T=0.1s:  Inventory service reserves stock → InventoryReserved
  T=0.2s:  Payment service fails (insufficient funds) → PaymentFailed
  T=0.3s:  Saga orchestrator triggers compensation
  T=0.4s:  Inventory service releases stock → InventoryReleased
  T=0.5s:  Order status updated → Cancelled
```

---

## Pattern 5: Request-Response with Caching and Fallback

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ CLIENT REQUEST                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
                                 │
                                 │ GET /api/products/123
                                 ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ API CONTROLLER (ASP.NET MVC/Web API)                                        │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  public class ProductController : Controller                                │
│  {                                                                          │
│      private readonly Proxy<ProductService> _serviceProxy;                 │
│                                                                             │
│      public ProductController()                                             │
│      {                                                                      │
│          var aspects = new Aspect[]                                         │
│          {                                                                  │
│              new CacheAspect(RequestCache.Get()),  // Request-scoped       │
│              new FallbackAspect(GetFallbackProduct), // If failure         │
│              new RetryAspect(maxAttempts: 2),      // Brief retry          │
│              new LoggingAspect()                    // Log all operations  │
│          };                                                                 │
│          _serviceProxy = Proxy.Create<ProductService>(aspects);            │
│      }                                                                      │
│                                                                             │
│      public ActionResult GetProduct(int id)                                │
│      {                                                                      │
│          var product = _serviceProxy.Invoke(svc => svc.GetProduct(id));   │
│          return Json(product);                                              │
│      }                                                                      │
│  }                                                                          │
│                                │                                            │
└────────────────────────────────┼────────────────────────────────────────────┘
                                 │
                                 ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ ASPECT CHAIN EXECUTION                                                      │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  1. CacheAspect: Check RequestCache                                         │
│     ├─ Cache HIT  → Return cached product (2ms)                            │
│     └─ Cache MISS → Continue to next aspect                                │
│                                                                             │
│  2. FallbackAspect: Prepare fallback logic                                  │
│                                                                             │
│  3. RetryAspect: Prepare retry (max 2 attempts)                            │
│                                                                             │
│  4. Execute Method: ProductService.GetProduct(123)                         │
│     ├─ SUCCESS → Store in cache → Return product                           │
│     └─ FAILURE →                                                           │
│        ├─ Retry once                                                       │
│        └─ Still fails → Invoke FallbackAspect                              │
│           → Return default/cached product                                   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘

Fallback Strategy:
  Primary:   Query live database
  Retry:     Retry once after 500ms
  Fallback:  Return last known good value from distributed cache
  Ultimate:  Return minimal product info (ID + "Temporarily Unavailable")
```

---

## Pattern 6: Event Sourcing with Audit Trail

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ COMMAND: UpdateProductPrice                                                 │
├─────────────────────────────────────────────────────────────────────────────┤
│  Input: { ProductId: 123, OldPrice: 99.99, NewPrice: 89.99 }              │
└─────────────────────────────────────────────────────────────────────────────┘
                                 │
                                 ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ ASPECTACULAR PROXY with AUDIT ASPECT                                        │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  var aspects = new Aspect[]                                                 │
│  {                                                                          │
│      new AuditTrailAspect(auditStore),                                     │
│      new AuthorizationAspect(),                                             │
│      new ValidationAspect(),                                                │
│      new LoggingAspect()                                                    │
│  };                                                                         │
│                                                                             │
│  AuditTrailAspect captures:                                                 │
│    • Who: CurrentUser.Identity                                              │
│    • What: Method name + parameters                                         │
│    • When: Timestamp                                                        │
│    • Where: IP address, machine name                                        │
│    • Why: Reason (from parameter)                                           │
│    • Result: Success/Failure + new state                                    │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
                                 │
                                 ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ EVENT STORE (Audit Database)                                                │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  AuditLog Table:                                                            │
│  ┌───────────────────────────────────────────────────────────────────────┐ │
│  │ Id | Timestamp  | User      | Action           | EntityId | Before   │ │
│  │────|────────────|-----------|------------------|----------|──────────│ │
│  │ 1  | 2024-12-12 | john.doe  | UpdateProduct    | 123      | {        │ │
│  │    | 10:30:15   |           | Price            |          │  "Price":│ │
│  │    |            |           |                  |          │  99.99   │ │
│  │    |            |           |                  |          | }        │ │
│  │────|────────────|-----------|------------------|----------|──────────│ │
│  │                                                           | After    │ │
│  │                                                           |──────────│ │
│  │                                                           | {        │ │
│  │                                                           │  "Price":│ │
│  │                                                           │  89.99   │ │
│  │                                                           | }        │ │
│  └───────────────────────────────────────────────────────────────────────┘ │
│                                                                             │
│  Event Replay Capability:                                                   │
│    • Query all events for Product 123                                       │
│    • Rebuild state at any point in time                                     │
│    • Audit trail for compliance (SOX, GDPR)                                 │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Integration Anti-Patterns (To Avoid)

### ❌ Anti-Pattern 1: Chatty Integration

```
// BAD: N+1 query problem
for (int i = 1; i <= 100; i++)
{
    var product = proxy.Invoke(svc => svc.GetProduct(i));  // 100 DB calls
    DisplayProduct(product);
}

// GOOD: Batch query
var products = proxy.Invoke(svc => svc.GetProducts(1, 100));  // 1 DB call
foreach (var product in products)
{
    DisplayProduct(product);
}
```

### ❌ Anti-Pattern 2: No Timeout on External Calls

```
// BAD: No timeout (can hang indefinitely)
var result = externalApiClient.GetData();

// GOOD: With timeout aspect
var aspects = new[] { new TimeoutAspect(TimeSpan.FromSeconds(5)) };
var proxy = Proxy.Create<ExternalApiClient>(aspects);
var result = proxy.Invoke(client => client.GetData());
```

### ❌ Anti-Pattern 3: Missing Error Handling

```
// BAD: No fallback or retry
var product = productService.GetProduct(id);  // Throws on failure

// GOOD: With retry and fallback
var aspects = new Aspect[]
{
    new RetryAspect(maxAttempts: 3),
    new FallbackAspect(GetCachedProduct)
};
var proxy = Proxy.Create<ProductService>(aspects);
var product = proxy.Invoke(svc => svc.GetProduct(id));  // Resilient
```

---

## Performance Optimization Patterns

| Pattern | Technique | Improvement |
|---------|-----------|-------------|
| Caching | Request-scoped cache (RequestCache) | 99% faster for repeated calls |
| Batching | Batch DB operations (AddEntities) | 90% faster for bulk inserts |
| Eager Loading | EF Include() + AsNoTracking() | 50% faster, 70% less memory |
| Connection Pooling | DbContext auto-dispose | Reuse connections efficiently |
| Async Processing | Azure queue multicasting | Non-blocking, scalable |

---

## Related Documentation
- [System Context](system-context.md) - Overall system boundaries
- [Interception Pipeline](../data-flow/interception-pipeline.md) - Data flow details
- [EF Integration](../../specialized/entity-framework/ef-integration.md) - Database patterns
- [Azure Integration](../../specialized/azure/cloud-integration.md) - Cloud patterns
- [Web Integration](../../specialized/aspnet-web/web-aspects.md) - Web patterns

---

**Document Version:** 1.0  
**Last Updated:** Migration Planning Phase
