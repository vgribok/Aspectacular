# Exception Propagation - Error Handling Flow

## Overview
This diagram visualizes how exceptions propagate through the Aspectacular AOP Framework, including retry logic, exception transformation, logging, and final handling.

---

## Complete Exception Flow

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ METHOD EXECUTION (Normal Flow)                                              │
│   var result = proxy.Invoke(svc => svc.ProcessOrder(orderId));            │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    │ try {
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ INTERCEPTED METHOD                                                          │
│   public Order ProcessOrder(int orderId)                                    │
│   {                                                                         │
│       var order = dbContext.Orders.Find(orderId);                          │
│       // ... business logic ...                                            │
│       return order;                                                         │
│   }                                                                         │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    │ Exception thrown
                                    ▼
                              ┌─────────────┐
                              │ EXCEPTION   │
                              │ Occurred    │
                              └──────┬──────┘
                                     │
            ╔════════════════════════╧════════════════════════╗
            ║  Exception Captured by Interception Pipeline   ║
            ╚════════════════════════╤════════════════════════╝
                                     │
                                     ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ STAGE 6: Step_6_Optional_AfterCatchingAllExceptions()                      │
│ ─────────────────────────────────────────────────────────────────────────── │
│                                                                             │
│ Aspect Chain: [Logging] → [Retry] → [Transaction] → [ErrorHandler]        │
│                                                                             │
│ Each aspect processes exception in sequence:                               │
└─────────────────────────────────────────────────────────────────────────────┘
                                     │
                                     ▼
        ╔════════════════════════════════════════════════╗
        ║  Aspect 1: LOGGING ASPECT                      ║
        ╚════════════════════════════════════════════════╝
                                     │
┌─────────────────────────────────────────────────────────────────────────────┐
│ LoggingAspect.Step_6_Optional_AfterCatchingAllExceptions()                 │
│ ─────────────────────────────────────────────────────────────────────────── │
│                                                                             │
│ Actions:                                                                    │
│  1. Capture exception details                                              │
│  2. Log error entry                                                        │
│  3. Record stack trace                                                     │
│  4. Pass exception to next aspect                                          │
│                                                                             │
│ Log Entry:                                                                  │
│  ┌─────────────────────────────────────────────────────────────────────┐  │
│  │ [ERROR] 2024-12-12 10:30:15.789                                      │  │
│  │ Method: ProcessOrder(orderId=123)                                    │  │
│  │ Exception: System.Data.SqlClient.SqlException                        │  │
│  │ Message: "Timeout expired. The timeout period elapsed..."           │  │
│  │ Duration: 30012ms                                                    │  │
│  │ Stack Trace: [Full stack trace captured]                            │  │
│  └─────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│ Decision: CONTINUE to next aspect                                          │
└─────────────────────────────────────────────────────────────────────────────┘
                                     │
                                     ▼
        ╔════════════════════════════════════════════════╗
        ║  Aspect 2: RETRY ASPECT                        ║
        ╚════════════════════════════════════════════════╝
                                     │
┌─────────────────────────────────────────────────────────────────────────────┐
│ RetryAspect.Step_6_Optional_AfterCatchingAllExceptions()                   │
│ ─────────────────────────────────────────────────────────────────────────── │
│                                                                             │
│ Configuration:                                                              │
│   maxAttempts: 3                                                           │
│   delayMillisec: 2000                                                      │
│   exponentialBackoff: true                                                 │
│                                                                             │
│ Analysis:                                                                   │
│   Exception Type: SqlException                                             │
│   Error Code: -2 (timeout)                                                 │
│   Classification: TRANSIENT ✓                                              │
│   Current Attempt: 1                                                       │
│   Max Attempts: 3                                                          │
│                                                                             │
│ Decision Tree:                                                              │
│   ┌─────────────────────────────────────────────────────────────────────┐  │
│   │ Is exception transient?                                             │  │
│   │   ├─ YES → Is attempt < maxAttempts?                               │  │
│   │   │         ├─ YES → RETRY (wait 2000ms)                           │  │
│   │   │         └─ NO  → FAIL (propagate exception)                    │  │
│   │   └─ NO  → FAIL (propagate immediately)                            │  │
│   └─────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│ Decision: RETRY ✓                                                          │
└─────────────────────────────────────────────────────────────────────────────┘
                                     │
                                     │ Wait 2000ms (exponential backoff)
                                     ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ RETRY ATTEMPT #2                                                            │
│ ─────────────────────────────────────────────────────────────────────────── │
│                                                                             │
│ Loop back to Stage 2 (Step_2_BeforeTryingMethodExec)                      │
│   ↓                                                                         │
│ Re-execute method                                                           │
│   ↓                                                                         │
│ Exception thrown again: SqlException (timeout)                             │
│   ↓                                                                         │
│ Back to Stage 6 (exception handling)                                       │
│                                                                             │
│ RetryAspect Analysis:                                                       │
│   Current Attempt: 2                                                       │
│   Max Attempts: 3                                                          │
│   Decision: RETRY again (wait 4000ms - exponential backoff)               │
└─────────────────────────────────────────────────────────────────────────────┘
                                     │
                                     │ Wait 4000ms (2000ms × 2)
                                     ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ RETRY ATTEMPT #3                                                            │
│ ─────────────────────────────────────────────────────────────────────────── │
│                                                                             │
│ Loop back to Stage 2                                                        │
│   ↓                                                                         │
│ Re-execute method                                                           │
│   ↓                                                                         │
│ Exception thrown again: SqlException (timeout)                             │
│   ↓                                                                         │
│ Back to Stage 6 (exception handling)                                       │
│                                                                             │
│ RetryAspect Analysis:                                                       │
│   Current Attempt: 3                                                       │
│   Max Attempts: 3                                                          │
│   Decision: MAX RETRIES EXCEEDED → FAIL ✗                                 │
└─────────────────────────────────────────────────────────────────────────────┘
                                     │
                                     │ Exception continues to next aspect
                                     ▼
        ╔════════════════════════════════════════════════╗
        ║  Aspect 3: TRANSACTION ASPECT                  ║
        ╚════════════════════════════════════════════════╝
                                     │
┌─────────────────────────────────────────────────────────────────────────────┐
│ TransactionAspect.Step_6_Optional_AfterCatchingAllExceptions()             │
│ ─────────────────────────────────────────────────────────────────────────── │
│                                                                             │
│ Actions:                                                                    │
│  1. Detect active transaction                                              │
│  2. Rollback transaction                                                   │
│  3. Log rollback event                                                     │
│  4. Pass exception to next aspect                                          │
│                                                                             │
│ Transaction State:                                                          │
│   ┌─────────────────────────────────────────────────────────────────────┐  │
│   │ Transaction ID: tx-9876543                                          │  │
│   │ Status: Active → Rolling Back...                                    │  │
│   │ Uncommitted Changes:                                                │  │
│   │   - Order record (INSERT pending)                                   │  │
│   │   - Inventory update (UPDATE pending)                               │  │
│   │   - Audit log (INSERT pending)                                      │  │
│   │ → ROLLBACK COMPLETE ✓                                               │  │
│   │ All changes reverted                                                │  │
│   └─────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│ Decision: CONTINUE to next aspect                                          │
└─────────────────────────────────────────────────────────────────────────────┘
                                     │
                                     ▼
        ╔════════════════════════════════════════════════╗
        ║  Aspect 4: ERROR HANDLER ASPECT                ║
        ╚════════════════════════════════════════════════╝
                                     │
┌─────────────────────────────────────────────────────────────────────────────┐
│ ErrorHandlerAspect.Step_6_Optional_AfterCatchingAllExceptions()            │
│ ─────────────────────────────────────────────────────────────────────────── │
│                                                                             │
│ Actions:                                                                    │
│  1. Classify exception severity                                            │
│  2. Transform exception if needed                                          │
│  3. Add context information                                                │
│  4. Determine if exception should propagate                                │
│                                                                             │
│ Exception Transformation:                                                   │
│   Original: SqlException (infrastructure detail)                           │
│   Transformed: OrderProcessingException (business-level)                   │
│                                                                             │
│ Enhanced Exception:                                                         │
│   ┌─────────────────────────────────────────────────────────────────────┐  │
│   │ Type: OrderProcessingException                                      │  │
│   │ Message: "Failed to process order after 3 retry attempts"          │  │
│   │ InnerException: SqlException                                        │  │
│   │ Additional Context:                                                 │  │
│   │   - OrderId: 123                                                    │  │
│   │   - RetryCount: 3                                                   │  │
│   │   - TotalDuration: 36,012ms                                         │  │
│   │   - UserContext: john.doe@company.com                              │  │
│   └─────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│ Decision: PROPAGATE enhanced exception                                     │
└─────────────────────────────────────────────────────────────────────────────┘
                                     │
                                     │ All aspects processed
                                     ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ STAGE 5: Step_5_FinallyAfterMethodExecution() (Finally Block)              │
│ ─────────────────────────────────────────────────────────────────────────── │
│                                                                             │
│ Cleanup Actions (ALWAYS execute, even on exception):                       │
│  ✓ DbContext.Dispose()                                                     │
│  ✓ Connection closed                                                       │
│  ✓ Performance timer stopped                                               │
│  ✓ Temporary resources freed                                               │
│  ✓ Lock released                                                           │
│                                                                             │
│ Note: Cleanup guaranteed even if exception will propagate                  │
└─────────────────────────────────────────────────────────────────────────────┘
                                     │
                                     ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ STAGE 7: Step_7_AfterEverythingSaidAndDone()                               │
│ ─────────────────────────────────────────────────────────────────────────── │
│                                                                             │
│ Final Logging (even on exception):                                         │
│   ┌─────────────────────────────────────────────────────────────────────┐  │
│   │ [FINAL] 2024-12-12 10:30:51.801                                     │  │
│   │ Method: ProcessOrder(orderId=123)                                   │  │
│   │ Result: FAILURE                                                     │  │
│   │ Duration: 36,012ms (including retries and delays)                   │  │
│   │ Retry Count: 3                                                      │  │
│   │ Final Exception: OrderProcessingException                           │  │
│   │ Transaction: ROLLED BACK                                            │  │
│   │ Resources: CLEANED UP                                               │  │
│   └─────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────┘
                                     │
                                     │ } catch propagates exception
                                     ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ EXCEPTION PROPAGATED TO CALLER                                              │
│ ─────────────────────────────────────────────────────────────────────────── │
│                                                                             │
│ try {                                                                       │
│     var result = proxy.Invoke(svc => svc.ProcessOrder(orderId));          │
│ }                                                                           │
│ catch (OrderProcessingException ex)                                        │
│ {                                                                           │
│     // Caller handles transformed, enhanced exception                      │
│     LogError(ex);                                                          │
│     ShowUserMessage("Order processing failed. Please try again.");         │
│     // InnerException contains original SqlException for debugging         │
│ }                                                                           │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Exception Classification

### Transient Exceptions (Retry Eligible)

```
┌──────────────────────────────────────────────────────────┐
│ TRANSIENT EXCEPTIONS                                     │
│ (Automatically retried by RetryAspect)                   │
├──────────────────────────────────────────────────────────┤
│ SqlException (error codes -2, -1, 1205)                  │
│   → Timeout, connection failure, deadlock                │
│                                                           │
│ TimeoutException                                          │
│   → Network timeout, service unavailable                 │
│                                                           │
│ HttpRequestException (5xx status codes)                  │
│   → 500, 502, 503, 504 errors                           │
│                                                           │
│ Azure Storage transient errors                           │
│   → Throttling, temporary unavailability                 │
│                                                           │
│ Custom transient exceptions                              │
│   → Marked with [TransientException] attribute          │
└──────────────────────────────────────────────────────────┘
```

### Non-Transient Exceptions (Immediate Failure)

```
┌──────────────────────────────────────────────────────────┐
│ NON-TRANSIENT EXCEPTIONS                                 │
│ (Fail immediately, no retry)                             │
├──────────────────────────────────────────────────────────┤
│ ArgumentException / ArgumentNullException                │
│   → Bad input, programming error                         │
│                                                           │
│ InvalidOperationException                                │
│   → Business rule violation, state error                │
│                                                           │
│ UnauthorizedAccessException                              │
│   → Authorization failure (won't succeed on retry)      │
│                                                           │
│ NotFoundException                                         │
│   → Resource doesn't exist                               │
│                                                           │
│ ValidationException                                       │
│   → Data validation failure                              │
└──────────────────────────────────────────────────────────┘
```

---

## Exception Handling Patterns

### Pattern 1: Retry with Exponential Backoff

```
Attempt 1: Exception → Wait 2s
Attempt 2: Exception → Wait 4s (2s × 2)
Attempt 3: Exception → Wait 8s (2s × 4)
Attempt 4: Exception → FAIL (max attempts exceeded)

Total time: 14 seconds + 4 method executions
```

### Pattern 2: Circuit Breaker (Custom Aspect)

```
State: CLOSED (normal operation)
  ↓
Failure Count reaches threshold (e.g., 5 failures in 1 minute)
  ↓
State: OPEN (reject all calls immediately)
  ↓
Wait for timeout period (e.g., 30 seconds)
  ↓
State: HALF-OPEN (allow test call)
  ↓
  ├─ Test call SUCCESS → State: CLOSED (resume normal)
  └─ Test call FAILURE → State: OPEN (wait again)
```

### Pattern 3: Fallback Strategy

```
Primary Method: Call external API
  ↓
Exception: ApiUnavailableException
  ↓
Fallback Method: Return cached data or default value
  ↓
Return fallback result to caller (degraded functionality but no failure)
```

---

## Exception Data Enrichment

### Before Enrichment (Raw Exception)
```
SqlException
  Message: "Timeout expired"
  ErrorCode: -2
  Stack Trace: [...]
```

### After Enrichment (By Error Handler Aspect)
```
OrderProcessingException
  Message: "Failed to process order after 3 retry attempts"
  InnerException: SqlException (original)
  Data:
    OrderId: 123
    CustomerId: 456
    RetryCount: 3
    TotalDuration: 36012ms
    UserContext: john.doe@company.com
    CorrelationId: 9c8e7f16-1234-5678-90ab-cdef12345678
    Timestamp: 2024-12-12T10:30:51.801Z
  Stack Trace: [full trace with context]
```

---

## Compensation Actions (Saga Pattern)

```
Step 1: Reserve Inventory → SUCCESS
  ↓
Step 2: Process Payment → SUCCESS
  ↓
Step 3: Create Shipment → FAILURE (exception)
  ↓
Compensation Flow:
  ↓
Compensate Step 2: Refund Payment
  ↓
Compensate Step 1: Release Inventory
  ↓
Log compensation complete
  ↓
Propagate exception with compensation status
```

---

## Exception Metrics

| Metric | Value (Example) |
|--------|-----------------|
| Exception Type | SqlException → OrderProcessingException |
| Transient | Yes |
| Retry Attempts | 3 |
| Retry Delays | 2s, 4s, 8s (exponential) |
| Total Duration | 36,012ms |
| Method Execution Time | 3 × 30s = 90s |
| Overhead Time | ~12ms (aspects) |
| Transaction Status | Rolled Back |
| Resources Cleaned | Yes |

---

## Retry Decision Matrix

| Exception Type | Transient? | Retry? | Max Attempts |
|---------------|-----------|--------|--------------|
| SqlException (timeout) | ✓ | ✓ | 3 |
| SqlException (deadlock) | ✓ | ✓ | 5 |
| SqlException (constraint violation) | ✗ | ✗ | 0 |
| TimeoutException | ✓ | ✓ | 3 |
| ArgumentException | ✗ | ✗ | 0 |
| UnauthorizedAccessException | ✗ | ✗ | 0 |
| HttpException (503) | ✓ | ✓ | 5 |
| HttpException (404) | ✗ | ✗ | 0 |

---

## Related Diagrams
- [Interception Pipeline](interception-pipeline.md) - Complete data flow including success path
- [Aspect Lifecycle Sequence](../behavioral/aspect-lifecycle-sequence.md) - Stage-by-stage lifecycle
- [System Context](../architecture/system-context.md) - External system error handling

---

**Document Version:** 1.0  
**Last Updated:** Migration Planning Phase
