# Interception Pipeline - Method Call Data Flow

## Overview
This diagram visualizes the complete data flow through the Aspectacular AOP interception pipeline, from initial method invocation through all seven lifecycle stages to final result return.

---

## Complete Interception Data Flow

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ CLIENT CODE                                                                 │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    │ Invoke Method
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ PROXY INTERCEPTOR                                                           │
│ ┌─────────────────────────────────────────────────────────────────────────┐ │
│ │  Proxy.Invoke(instance => instance.MyMethod(param1, param2))           │ │
│ └─────────────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    │ Capture: Method Info, Parameters, Timestamp
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ ASPECT CHAIN                                                                │
│ [Aspect 1] → [Aspect 2] → [Aspect 3] → ... → [Aspect N]                   │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
            ╔═══════════════════════╧═══════════════════════╗
            ║     7-STAGE LIFECYCLE BEGINS                  ║
            ╚═══════════════════════╤═══════════════════════╝
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ STAGE 1: Step_1_BeforeResolvingInstance()                                  │
│ ─────────────────────────────────────────────────────────────────────────── │
│ For Each Aspect in Chain:                                                  │
│   • Authorization checks (early validation)                                │
│   • Pre-instantiation logging                                              │
│   • Resource pre-allocation                                                │
│                                                                             │
│ Data Flow:                                                                  │
│   IN:  Method metadata, parameters, caller context                         │
│   OUT: Authorization result, early validation status                       │
│                                                                             │
│ Example (AuthorizationAspect):                                             │
│   User: CurrentUser.Identity                                               │
│   Roles: ["Admin", "Manager"]                                              │
│   Required: ["Admin"]                                                      │
│   → AUTHORIZED ✓                                                           │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    │ If NOT static method
                                    ▼
                         ┌──────────────────────┐
                         │ INSTANCE RESOLUTION  │
                         │ new MyClass()        │
                         └──────────────────────┘
                                    │
                                    │ Instance created
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ STAGE 2: Step_2_BeforeTryingMethodExec()                                   │
│ ─────────────────────────────────────────────────────────────────────────── │
│ For Each Aspect in Chain:                                                  │
│   • Parameter validation                                                   │
│   • Cache lookup (CacheAspect)                                             │
│   • Transaction begin (TransactionAspect)                                  │
│   • Performance timer start (PerformanceAspect)                            │
│   • Request context setup (Web)                                            │
│                                                                             │
│ Data Flow:                                                                  │
│   IN:  Method parameters, instance (if non-static)                         │
│   OUT: Cache hit/miss, validation results, transaction handle              │
│                                                                             │
│ Example (CacheAspect):                                                     │
│   Cache Key: "MyMethod_param1=123_param2=abc"                             │
│   Cache Lookup: MISS                                                       │
│   → CONTINUE to method execution                                           │
│                                                                             │
│ Example If Cache HIT:                                                      │
│   Cache Key: "MyMethod_param1=123_param2=abc"                             │
│   Cache Lookup: HIT → Value: [Cached Result Object]                       │
│   → SKIP method execution, jump to Stage 3 with cached result             │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    │ Proceed to execution (if no cache hit)
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ METHOD EXECUTION (Try Block)                                               │
│ ─────────────────────────────────────────────────────────────────────────── │
│   try {                                                                     │
│       var result = instance.MyMethod(param1, param2);                      │
│       // ↓ Method executes actual business logic ↓                        │
│       //   Database queries, calculations, external API calls, etc.       │
│       return result;                                                       │
│   }                                                                         │
│                                                                             │
│ Data Flow:                                                                  │
│   IN:  Validated parameters, initialized instance                          │
│   OUT: Method return value OR exception                                    │
│                                                                             │
│ Possible Outcomes:                                                          │
│   SUCCESS PATH → Return value available                                    │
│   EXCEPTION PATH → Exception thrown                                        │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                     ╔══════════════╧══════════════╗
                     ║   Branch on Success/Failure  ║
                     ╚══════════════╤══════════════╝
                                    │
                ┌───────────────────┴───────────────────┐
                │                                       │
      ┌─────────▼─────────┐                  ┌─────────▼─────────┐
      │ SUCCESS PATH      │                  │ EXCEPTION PATH    │
      └─────────┬─────────┘                  └─────────┬─────────┘
                │                                       │
                ▼                                       ▼
┌───────────────────────────────┐     ┌───────────────────────────────────────┐
│ STAGE 3: Step_3_Before       │     │ STAGE 6: Step_6_Optional_After        │
│ MassagingReturnedResult()    │     │ CatchingAllExceptions()               │
│ ───────────────────────────── │     │ ───────────────────────────────────── │
│ For Each Aspect in Chain:    │     │ For Each Aspect in Chain:             │
│  • Result transformation     │     │  • Exception analysis                 │
│  • Cache population          │     │  • Retry logic evaluation             │
│  • Result validation         │     │  • Error logging                      │
│  • Logging result metadata   │     │  • Exception translation              │
│                              │     │  • Compensation logic                 │
│ Data Flow:                   │     │                                       │
│  IN:  Raw method return value│     │ Data Flow:                            │
│  OUT: Transformed result     │     │  IN:  Exception object, method info   │
│                              │     │  OUT: Retry decision, transformed exc │
│ Example (CacheAspect):       │     │                                       │
│  Store in cache:             │     │ Example (RetryAspect):                │
│   Key: "MyMethod_..."        │     │  Exception: SqlTransientException     │
│   Value: [Result Object]     │     │  Attempt: 1 of 3                      │
│   Expiry: 300 seconds        │     │  Decision: RETRY after 2 sec delay    │
│  → CACHED ✓                  │     │  → LOOP BACK to method execution      │
└───────────────┬───────────────┘     └───────────────┬───────────────────────┘
                │                                     │
                │ Continue success path               │ If max retries exceeded
                ▼                                     ▼
┌───────────────────────────────┐     ┌───────────────────────────────────────┐
│ STAGE 4: Step_4_Optional_    │     │ Exception propagates                  │
│ AfterSuccessfulCall           │     │ (After logging and handling)          │
│ Completion()                  │     └───────────────┬───────────────────────┘
│ ───────────────────────────── │                     │
│ For Each Aspect in Chain:    │                     │
│  • Success logging           │                     │
│  • Performance metrics       │                     │
│  • Transaction commit        │                     │
│  • Audit trail recording     │                     │
│  • Cache hit notification    │                     │
│                              │                     │
│ Data Flow:                   │                     │
│  IN:  Final result, metrics  │                     │
│  OUT: Committed changes      │                     │
└───────────────┬───────────────┘                     │
                │                                     │
                │ Both paths converge                 │
                └─────────────────┬───────────────────┘
                                  │
                                  ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ STAGE 5: Step_5_FinallyAfterMethodExecution() (Finally Block)              │
│ ─────────────────────────────────────────────────────────────────────────── │
│ For Each Aspect in Chain:                                                  │
│  • Resource cleanup (connections, files, handles)                          │
│  • Performance timer stop                                                  │
│  • Logging final state                                                     │
│  • Context disposal (DbContext, HttpContext items)                         │
│  • Lock release                                                            │
│                                                                             │
│ Data Flow:                                                                  │
│  IN:  Execution state (success/failure), resource handles                  │
│  OUT: Released resources, final logs                                       │
│                                                                             │
│ Guaranteed Execution: ALWAYS runs (even if exception thrown)               │
│                                                                             │
│ Example (DbContextAspect):                                                 │
│   DbContext.Dispose()  → Connection closed ✓                               │
│   Transaction disposed → Resources freed ✓                                 │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ STAGE 7: Step_7_AfterEverythingSaidAndDone()                               │
│ ─────────────────────────────────────────────────────────────────────────── │
│ For Each Aspect in Chain:                                                  │
│  • Final metrics aggregation                                               │
│  • Comprehensive logging output                                            │
│  • Performance summary                                                     │
│  • Audit trail finalization                                                │
│  • Final aspect cleanup                                                    │
│                                                                             │
│ Data Flow:                                                                  │
│  IN:  Complete execution log, all metrics, final state                     │
│  OUT: Consolidated report, published metrics                               │
│                                                                             │
│ Example (LoggingAspect):                                                   │
│   Log Entry: {                                                             │
│     Method: "MyMethod",                                                    │
│     Duration: 245ms,                                                       │
│     Result: Success,                                                       │
│     CacheHit: false,                                                       │
│     RetryCount: 0                                                          │
│   }                                                                         │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
            ╔═══════════════════════╧═══════════════════════╗
            ║     7-STAGE LIFECYCLE COMPLETE                ║
            ╚═══════════════════════╤═══════════════════════╝
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ RETURN TO CALLER                                                            │
│ ─────────────────────────────────────────────────────────────────────────── │
│                                                                             │
│ SUCCESS PATH:                                                               │
│   return transformedResult;  // From Stage 3                               │
│                                                                             │
│ EXCEPTION PATH:                                                             │
│   throw handledException;    // From Stage 6 (after retry exhausted)       │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ CLIENT CODE RECEIVES RESULT                                                 │
│   var result = proxy.Invoke(svc => svc.MyMethod(param1, param2));         │
│   // Process result or handle exception                                    │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Data Transformation Example

### Input Data
```csharp
Method: GetProduct(int productId)
Parameters: { productId = 123 }
Caller: OrderController.ProcessOrder()
Timestamp: 2024-12-12 10:30:15.123
```

### Stage-by-Stage Data Flow

**Stage 1 (Authorization):**
```
IN:  User = { Identity = "john.doe@company.com", Roles = ["User", "Manager"] }
OUT: Authorized = true (requires "User" role)
```

**Stage 2 (Cache Check):**
```
IN:  CacheKey = "GetProduct_productId=123"
OUT: CacheMiss = true → Proceed to execution
```

**Method Execution:**
```
IN:  productId = 123
QUERY: SELECT * FROM Products WHERE ProductId = 123
OUT: Product = { ProductId: 123, Name: "Widget", Price: 99.99, Stock: 50 }
DURATION: 45ms
```

**Stage 3 (Result Processing):**
```
IN:  Product = { ProductId: 123, Name: "Widget", Price: 99.99, Stock: 50 }
ACTION: Store in cache with 300s TTL
OUT: Product (unchanged)
```

**Stage 4 (Success Logging):**
```
IN:  Result = Product, ExecutionTime = 45ms
ACTION: Log success, record metrics
OUT: Metrics = { QueryTime: 45ms, CachePopulated: true }
```

**Stage 5 (Cleanup):**
```
ACTION: DbContext.Dispose(), close connection
OUT: Resources freed
```

**Stage 7 (Final Logging):**
```
LOG: {
  Method: "GetProduct",
  Parameters: { productId: 123 },
  Duration: 52ms (total with aspects),
  Result: "Success",
  CachedForFutureUse: true
}
```

**Return to Caller:**
```
RETURN: Product = { ProductId: 123, Name: "Widget", Price: 99.99, Stock: 50 }
```

---

## Retry Flow (Exception Path)

```
Attempt 1:
  Method Execution → SqlTransientException (timeout)
  Stage 6 → Retry decision: YES (1 of 3)
  Wait 2000ms
  
Attempt 2:
  Method Execution → SqlTransientException (timeout)
  Stage 6 → Retry decision: YES (2 of 3)
  Wait 4000ms (exponential backoff)
  
Attempt 3:
  Method Execution → SUCCESS
  Stage 3 → Process result
  Stage 4 → Log success with retry count = 2
  Stage 5 → Cleanup
  Stage 7 → Final logging
  Return → Result to caller
```

---

## Cache Hit Short-Circuit

```
Stage 1: Authorization → PASS
Stage 2: Cache Check → HIT
  ↓
  Cache returns stored result
  ↓
  SKIP method execution
  ↓
Stage 3: Result already available (from cache)
Stage 4: Log cache hit
Stage 5: No cleanup needed (no resources allocated)
Stage 7: Final logging with cache hit flag
Return → Cached result to caller

Total time: ~1-2ms (vs 45-50ms for full execution)
```

---

## Performance Data Flow

| Stage | Typical Duration | Purpose |
|-------|-----------------|---------|
| Stage 1 (Authorization) | 0.5-1ms | Security checks |
| Stage 2 (Pre-execution) | 0.5-2ms | Cache check, setup |
| Method Execution | 10-500ms | Actual business logic |
| Stage 3 (Result Processing) | 0.5-1ms | Transform, cache store |
| Stage 4 (Success Logging) | 0.2-0.5ms | Metrics recording |
| Stage 5 (Cleanup) | 0.5-2ms | Resource disposal |
| Stage 7 (Final Logging) | 0.2-0.5ms | Consolidated logging |
| **TOTAL OVERHEAD** | **2.5-7ms** | AOP infrastructure |

**Cache Hit Path:** 1-2ms total (skips execution)  
**Normal Execution:** Method time + 2.5-7ms overhead  
**With Retry (3 attempts):** 3× method time + overhead + retry delays

---

## Related Diagrams
- [Aspect Lifecycle Sequence](../behavioral/aspect-lifecycle-sequence.md) - Detailed lifecycle stages
- [Exception Propagation](exception-propagation.md) - Error handling flow
- [Dependency Graph](../structural/dependency-graph.md) - Component relationships

---

**Document Version:** 1.0  
**Last Updated:** Migration Planning Phase
