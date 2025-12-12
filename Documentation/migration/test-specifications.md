# Test Specifications - Complete Testing Strategy for Migration

## Overview
This document provides comprehensive test specifications for validating the Aspectacular AOP Framework migration. All specifications are designed to ensure functionality, performance, and compatibility are maintained throughout the migration process.

---

## Core Framework Tests

### 1. Aspect Lifecycle Tests

#### 1.1 Seven-Stage Lifecycle Execution
**Test ID:** CORE-001  
**Component:** IAspect Interface  
**Priority:** CRITICAL  

**Test Specification:**
```csharp
// Verify all 7 lifecycle stages execute in correct order
Test: AspectLifecycleOrderTest
Input: Mock aspect implementing all 7 stages
Expected: Stages execute in order:
  1. Step_1_BeforeResolvingInstance()
  2. Step_2_BeforeTryingMethodExec()
  3. Step_3_BeforeMassagingReturnedResult()
  4. Step_4_Optional_AfterSuccessfulCallCompletion()
  5. Step_5_FinallyAfterMethodExecution()
  6. Step_6_Optional_AfterCatchingAllExceptions()
  7. Step_7_AfterEverythingSaidAndDone()
```

**Validation Criteria:**
- Each stage called exactly once
- Stages never execute out of order
- Thread-safe execution verified with parallel calls

**Source Reference:** `NET 4.5/Aspectacular.Framework.Main.45/Aspects/Aspect.cs` (lines 28-44)

#### 1.2 Exception Handling in Lifecycle
**Test ID:** CORE-002  
**Component:** AspectProxy  
**Priority:** CRITICAL  

**Test Specification:**
```csharp
Test: AspectExceptionPropagationTest
Scenario 1: Exception in intercepted method
  - Verify Step_6_Optional_AfterCatchingAllExceptions() called
  - Verify Step_7_AfterEverythingSaidAndDone() still executes
  - Verify exception propagates to caller

Scenario 2: Exception in aspect itself
  - Verify Step_5_FinallyAfterMethodExecution() executes
  - Verify graceful degradation (method still executes)
```

**Validation Criteria:**
- No swallowed exceptions
- Aspect failures don't prevent method execution
- All cleanup stages execute even on failure

---

### 2. Proxy Generation Tests

#### 2.1 Dynamic Proxy Creation
**Test ID:** PROXY-001  
**Component:** Proxy.Create() / GetProxy()  
**Priority:** CRITICAL  

**Test Specification:**
```csharp
Test: DynamicProxyGenerationTest
Input: Various class types (interface-based, class-based, sealed, generic)
Expected: Valid proxy generated for all supported types

Test Cases:
1. Interface proxy: ITestService → Proxy implements ITestService
2. Class proxy: TestService → Proxy inherits TestService
3. Generic class: TestService<T> → Proxy<T> maintains generics
4. Sealed class: Should throw InvalidOperationException with clear message
```

**Validation Criteria:**
- Proxy type matches original signatures
- All public methods interceptable
- Proxy performance overhead < 5% vs direct call

#### 2.2 Proxy Caching and Reuse
**Test ID:** PROXY-002  
**Component:** Proxy cache mechanism  
**Priority:** HIGH  

**Test Specification:**
```csharp
Test: ProxyCachingTest
Scenario: Create proxy for same type multiple times
Expected: Second call returns cached proxy type (not new generation)

Validation:
- Memory footprint stable across repeated calls
- No memory leaks after 1000+ proxy generations
- Cache invalidation works on assembly reload
```

---

### 3. Aspect Ordering and Dependencies

#### 3.1 Aspect Execution Order
**Test ID:** ORDER-001  
**Component:** Aspect chain execution  
**Priority:** CRITICAL  

**Test Specification:**
```csharp
Test: AspectOrderingTest
Input: Method with multiple aspects [Logging, Cache, Retry, Authorization]
Expected: Aspects execute in declaration order

Example:
[LoggingAspect]
[CacheAspect]
[RetryAspect]
void MyMethod();

Execution Order:
1. Logging.Step_2 → Cache.Step_2 → Retry.Step_2 → Method → Retry.Step_3 → Cache.Step_3 → Logging.Step_3
```

**Validation Criteria:**
- Order maintained across 100+ invocations
- Thread-safe ordering in concurrent scenarios

#### 3.2 Required Aspect Dependencies
**Test ID:** ORDER-002  
**Component:** RequiredAspectAttribute  
**Priority:** HIGH  

**Test Specification:**
```csharp
Test: RequiredAspectAutoInstantiationTest
Scenario: Aspect A requires Aspect B via [RequiredAspect(typeof(B))]
Expected: If B not declared, framework auto-instantiates B before A

Validation:
- B automatically added to aspect chain
- B executes before A in lifecycle
- Configuration option to disable auto-instantiation works
```

---

## Retry Logic Tests

### 4. RetryAspect Functionality

#### 4.1 Transient Exception Retry
**Test ID:** RETRY-001  
**Component:** RetryAspect  
**Priority:** CRITICAL  

**Test Specification:**
```csharp
Test: TransientExceptionRetryTest
Configuration:
  - maxAttempts = 3
  - delayMillisec = 100
  - exponentialBackoff = false

Scenario: Method throws transient exception twice, succeeds third time
Expected:
  - Attempt 1: Throws SqlTransientException → Wait 100ms
  - Attempt 2: Throws SqlTransientException → Wait 100ms
  - Attempt 3: Returns successfully
  - Total retries: 2
  - Result: Success

Validation:
- Only transient exceptions trigger retry (SqlTransientException, TimeoutException, etc.)
- Non-transient exceptions fail immediately (ArgumentException, etc.)
- Total elapsed time: ~200ms (2 × 100ms delays)
```

**Source Reference:** `NET 4.5/Aspectacular.Framework.Main.45/Aspects/RetryAspects.cs`

#### 4.2 Exponential Backoff
**Test ID:** RETRY-002  
**Component:** RetryAspect with exponentialBackoff  
**Priority:** HIGH  

**Test Specification:**
```csharp
Test: ExponentialBackoffTest
Configuration:
  - maxAttempts = 4
  - delayMillisec = 100
  - exponentialBackoff = true

Expected Delays:
  - Attempt 1 → 2: Wait 100ms
  - Attempt 2 → 3: Wait 200ms (100 × 2)
  - Attempt 3 → 4: Wait 400ms (100 × 4)
  
Total elapsed time: ~700ms
```

#### 4.3 Max Attempts Enforcement
**Test ID:** RETRY-003  
**Component:** RetryAspect max attempts limit  
**Priority:** CRITICAL  

**Test Specification:**
```csharp
Test: MaxAttemptsExceededTest
Configuration: maxAttempts = 3
Scenario: Method throws transient exception 5 times
Expected:
  - Retries exactly 2 times (3 total attempts)
  - Final exception propagates to caller
  - Exception message indicates "Max retry attempts (3) exceeded"
```

---

## Caching Tests

### 5. CacheAspect Functionality

#### 5.1 Cache Hit/Miss Logic
**Test ID:** CACHE-001  
**Component:** CacheAspect  
**Priority:** CRITICAL  

**Test Specification:**
```csharp
Test: CacheHitMissTest
Method Signature: [InvariantReturn(true)] int ExpensiveCalculation(int input)

Scenario 1: Cache Miss
  - First call with input=10: Method executes, result cached, returns 42
  - Validation: Cache key "ExpensiveCalculation_10" stored with value 42

Scenario 2: Cache Hit
  - Second call with input=10: Method NOT executed, cached value returned
  - Validation: Method execution count = 0 for second call

Scenario 3: Different Parameter
  - First call with input=20: Cache miss, method executes
  - Validation: Separate cache key "ExpensiveCalculation_20" created
```

**Validation Criteria:**
- Cache key includes method name + parameter values
- Cache respects parameter equality semantics
- Thread-safe cache access

**Source Reference:** Business logic documented in `Documentation/behavior/business-logic.md`

#### 5.2 Cache Expiration
**Test ID:** CACHE-002  
**Component:** CacheAspect expiration policy  
**Priority:** HIGH  

**Test Specification:**
```csharp
Test: CacheExpirationTest
Configuration: cacheDurationSeconds = 5

Timeline:
  T=0s: Call method, result cached
  T=3s: Call method, cache hit (result from cache)
  T=6s: Call method, cache expired, method re-executed
  
Validation:
  - Cache lifetime respected
  - Expired entries removed from cache
  - No memory leak from old cache entries
```

#### 5.3 Cache Invalidation
**Test ID:** CACHE-003  
**Component:** Manual cache invalidation  
**Priority:** MEDIUM  

**Test Specification:**
```csharp
Test: CacheInvalidationTest
Scenario:
  1. Call cached method, result stored
  2. Invalidate cache for specific method/parameters
  3. Call again with same parameters
  
Expected:
  - Step 3 executes method (cache miss after invalidation)
  - Other cache entries unaffected
```

---

## Authorization Tests

### 6. Authorization Aspects

#### 6.1 Role-Based Authorization
**Test ID:** AUTH-001  
**Component:** AspNetRoleAuthorizationAspect  
**Priority:** CRITICAL  

**Test Specification:**
```csharp
Test: RoleBasedAuthorizationTest
Method: [DemandAspNetRole("Admin", "Manager")] void SecureMethod()

Scenario 1: User in required role
  - User roles: ["Admin", "User"]
  - Expected: Method executes successfully

Scenario 2: User not in required role
  - User roles: ["User", "Guest"]
  - Expected: Throws UnauthorizedAccessException
  - HTTP Response: 403 Forbidden

Scenario 3: Anonymous user
  - User roles: null
  - Expected: Throws UnauthorizedAccessException
```

**Validation Criteria:**
- 403 status code set in HTTP context
- Exception message includes required roles
- Authorization check happens in Step_1 (before method execution)

**Source Reference:** `NET 4.5/Aspectacular.Framework.Web.45/AspNetRoleAuthorizationAspect.cs`

#### 6.2 Claims-Based Authorization
**Test ID:** AUTH-002  
**Component:** ClaimsAuthorizationAspect  
**Priority:** HIGH  

**Test Specification:**
```csharp
Test: ClaimsBasedAuthorizationTest
Method: [DemandClaims(demandAny: true, "CanRead", "CanWrite")] void SecureMethod()

Test Cases:
1. demandAny=true, user has "CanRead": PASS
2. demandAny=true, user has "CanWrite": PASS
3. demandAny=true, user has neither: FAIL
4. demandAny=false, user has both: PASS
5. demandAny=false, user has only one: FAIL
```

---

## Entity Framework Integration Tests

### 7. EF-Specific Features

#### 7.1 Lazy Loading Control
**Test ID:** EF-001  
**Component:** LazyLoadSwitch (TempLazyLoadOn)  
**Priority:** HIGH  

**Test Specification:**
```csharp
Test: LazyLoadSwitchTest
Setup: DbContext with LazyLoadingEnabled = false

Test Code:
using(var lazyOn = dbContext.TurnLazyLoadOn())
{
    // Lazy loading enabled here
    var entity = dbContext.Entities.First();
    var relatedData = entity.RelatedCollection; // Should load lazily
    Assert.NotNull(relatedData);
}
// Lazy loading disabled again

Validation:
- LazyLoadingEnabled = true inside using block
- LazyLoadingEnabled = false after Dispose()
- Nested using blocks work correctly
```

**Source Reference:** `NET 4.5/Aspectacular.Framework.EntityFramework.45/LazyLoadSwitch.cs`

#### 7.2 Entity Key Interface
**Test ID:** EF-002  
**Component:** IDbEntityKey  
**Priority:** MEDIUM  

**Test Specification:**
```csharp
Test: EntityKeyImplementationTest
Entity: Product implements IDbEntityKey

Test Cases:
1. Attach entity by key: dbContext.Attach(product, product.DbContextEntityKey)
2. Delete entity by key: dbContext.Delete(product, product.DbContextEntityKey)
3. Composite key support: Order (OrderId, LineNumber) → DbContextEntityKey returns tuple

Validation:
- Attach/Delete operations work without explicit key parameter
- Composite keys handled correctly
- Key extraction doesn't trigger lazy loading
```

**Source Reference:** `NET 4.5/Aspectacular.Framework.EntityFramework.45/IDbEntityKey.cs`

#### 7.3 EF Query Interception
**Test ID:** EF-003  
**Component:** EfInterceptor  
**Priority:** MEDIUM  

**Test Specification:**
```csharp
Test: EfQueryInterceptionTest
Setup: Register EfInterceptor with logging aspect

Scenario: Execute LINQ query against DbContext
Expected:
- Query SQL captured by interceptor
- Execution time logged
- Query parameters logged (if enabled)
- Result count logged

Validation:
- No performance degradation > 5%
- No impact on query results
- Thread-safe interception in parallel queries
```

---

## Azure Integration Tests

### 8. Azure Queue Operations

#### 8.1 Queue Message Multicasting
**Test ID:** AZURE-001  
**Component:** AzureQueueMulticaster  
**Priority:** HIGH  

**Test Specification:**
```csharp
Test: QueueMulticastTest
Setup:
  - Source queue: "orders"
  - Destination queues: ["orders-processing", "orders-audit", "orders-analytics"]

Scenario: Post message to source queue
Expected:
  - Message removed from source queue
  - Identical message posted to all 3 destination queues
  - Message transformer hook called (if configured)
  - Retry on transient failures

Validation:
- Message content identical across destinations
- Delivery guaranteed (at-least-once semantics)
- No message loss on intermittent Azure failures
```

**Source Reference:** `NET 4.5/Aspectacular.Framework.Azure.45/AzureQueueMulticaster.cs`

#### 8.2 Queue Monitoring
**Test ID:** AZURE-002  
**Component:** AzureQueueMonitor  
**Priority:** MEDIUM  

**Test Specification:**
```csharp
Test: QueueMonitoringTest
Setup: Monitor queue with polling interval = 1 second

Scenario:
  - T=0s: Queue empty, no messages processed
  - T=1s: Add message to queue
  - T=2s: Verify message processed by monitor

Validation:
- Messages processed within polling interval
- Monitor stops cleanly on cancellation
- No orphaned background threads after disposal
```

---

## LINQ Extensions Tests

### 9. Query Optimization

#### 9.1 Exists() vs Any() Performance
**Test ID:** LINQ-001  
**Component:** QueryableExtensions.Exists()  
**Priority:** MEDIUM  

**Test Specification:**
```csharp
Test: ExistsPerformanceTest
Setup: EF DbContext with 100,000 records

Scenario 1: Use LINQ Any()
  var exists = dbContext.Products.Any(p => p.Name == "Test");
  // Measures SQL: SELECT TOP 1 * FROM Products WHERE Name = 'Test'

Scenario 2: Use Exists() extension
  var exists = dbContext.Products.Exists(p => p.Name == "Test");
  // Measures SQL: SELECT TOP 1 (1) FROM Products WHERE Name = 'Test'

Validation:
- Exists() generates more efficient SQL
- Performance improvement measurable for large tables
- Result identical for both methods
```

**Source Reference:** `NET 4.5/Aspectacular.Framework.Core.45/QueryableExtensions.cs`

#### 9.2 DistinctBy() Extension
**Test ID:** LINQ-002  
**Component:** QueryableExtensions.DistinctBy()  
**Priority:** LOW  

**Test Specification:**
```csharp
Test: DistinctByTest
Input: Collection of Orders with duplicate CustomerIds
Query: orders.DistinctBy(o => o.CustomerId)

Expected:
- Returns one order per unique CustomerId
- Preserves order of first occurrence
- Works with complex key types (composite keys)
```

---

## Performance Tests

### 10. Benchmarking

#### 10.1 Aspect Overhead Measurement
**Test ID:** PERF-001  
**Component:** Overall proxy performance  
**Priority:** HIGH  

**Test Specification:**
```csharp
Test: AspectOverheadBenchmark
Baseline: Direct method call (no proxy)
  - 10,000 iterations
  - Average time: X ms

With Proxy (no aspects):
  - 10,000 iterations
  - Average time: Y ms
  - Overhead: (Y-X)/X × 100%
  
With Proxy (3 aspects):
  - 10,000 iterations
  - Average time: Z ms
  - Overhead: (Z-X)/X × 100%

Acceptance Criteria:
- No-aspect proxy overhead < 5%
- 3-aspect proxy overhead < 15%
```

#### 10.2 Cache Performance
**Test ID:** PERF-002  
**Component:** CacheAspect performance  
**Priority:** MEDIUM  

**Test Specification:**
```csharp
Test: CacheLookupPerformance
Scenario: Cached method with 10,000 cache hits

Metrics:
- Cache hit average time < 0.1ms
- Cache miss time = original method time + overhead
- Memory usage scales linearly with cache size
- No performance degradation after 1,000,000 cached entries
```

---

## Integration Test Scenarios

### 11. End-to-End Workflows

#### 11.1 Complete AOP Pipeline
**Test ID:** E2E-001  
**Component:** Full aspect pipeline  
**Priority:** CRITICAL  

**Test Specification:**
```csharp
Test: CompleteAopPipelineTest
Method Configuration:
[LoggingAspect]
[CacheAspect(cacheDurationSeconds: 60)]
[RetryAspect(maxAttempts: 3)]
[AuthorizationAspect(requiredRole: "User")]
public Order GetOrder(int orderId)

Test Workflow:
1. First call (authenticated user, orderId=123):
   - Authorization check passes
   - Cache miss → method executes
   - Result cached
   - Execution logged

2. Second call (same parameters):
   - Authorization check passes
   - Cache hit → method NOT executed
   - Cached result returned
   - Cache hit logged

3. Third call (unauthenticated user):
   - Authorization check fails
   - Method NOT executed
   - Exception thrown and logged

Validation:
- All aspects cooperate correctly
- No aspect interference
- Expected behavior for all scenarios
```

---

## Regression Test Suite

### 12. Backward Compatibility

#### 12.1 API Compatibility
**Test ID:** COMPAT-001  
**Component:** Public API surface  
**Priority:** CRITICAL  

**Test Specification:**
```csharp
Test: PublicApiCompatibilityTest
Validation:
- All public classes from original framework present
- All public methods maintain same signatures
- All public properties accessible
- All attributes maintain same usage patterns
- No breaking changes in default behaviors

Tools: Use API comparison tool (e.g., ApiCompat, DiffPlex)
```

#### 12.2 Serialization Compatibility
**Test ID:** COMPAT-002  
**Component:** Data serialization  
**Priority:** HIGH  

**Test Specification:**
```csharp
Test: SerializationCompatibilityTest
Scenario: Deserialize objects serialized by old framework version

Test Cases:
1. Cached data from old version still loadable
2. Queue messages from old version still processable
3. Configuration files from old version still parseable

Validation:
- No data loss during migration
- Graceful handling of version mismatches
```

---

## Security Tests

### 13. Security Validation

#### 13.1 Authorization Bypass Prevention
**Test ID:** SEC-001  
**Component:** Authorization aspects  
**Priority:** CRITICAL  

**Test Specification:**
```csharp
Test: AuthorizationBypassTest
Attack Scenarios:
1. Direct instantiation (bypassing proxy): Should be prevented by design
2. Reflection calls: Authorization should still apply
3. Null/empty role claims: Should fail authorization
4. Malformed user identity: Should fail gracefully

Validation:
- No code path bypasses authorization
- All attack vectors blocked
- Security exceptions logged
```

#### 13.2 Injection Attack Prevention
**Test ID:** SEC-002  
**Component:** Logging and caching  
**Priority:** HIGH  

**Test Specification:**
```csharp
Test: InjectionPreventionTest
Scenario: Method parameters contain malicious input
  - SQL injection strings
  - XSS payloads
  - Script injection attempts

Validation:
- Log output sanitizes dangerous characters
- Cache keys don't execute injected code
- No security vulnerabilities in parameter serialization
```

---

## Migration-Specific Tests

### 14. Framework Upgrade Validation

#### 14.1 .NET Framework 4.8 Compatibility
**Test ID:** MIG-001  
**Component:** All modules  
**Priority:** CRITICAL  

**Test Specification:**
```csharp
Test: DotNet48CompatibilityTest
Target: .NET Framework 4.8

Validation:
- All modules compile without errors
- All unit tests pass
- No deprecated API warnings
- Runtime performance equivalent to .NET 4.5 version
```

#### 14.2 Entity Framework 6 Compatibility
**Test ID:** MIG-002  
**Component:** EF integration  
**Priority:** CRITICAL  

**Test Specification:**
```csharp
Test: EF6CompatibilityTest
Setup: Migrate from EF 5.0 to EF 6.4.4

Validation:
- All EF interception patterns work
- LazyLoadSwitch compatible with EF6 API
- IDbEntityKey implementation compatible
- Query optimizations still effective
- No breaking changes in EF context usage
```

#### 14.3 Azure SDK v12 Compatibility
**Test ID:** MIG-003  
**Component:** Azure integration  
**Priority:** HIGH  

**Test Specification:**
```csharp
Test: AzureSdkV12CompatibilityTest
Setup: Migrate from Azure SDK v4 to v12

API Changes to Validate:
- CloudQueue → QueueClient API mapping
- Storage account connection string handling
- Authentication methods (connection string vs Azure.Identity)
- Async/await patterns (v12 is async-first)

Validation:
- Queue multicasting works with new SDK
- Message format remains compatible
- Performance equivalent or better
```

---

## Test Execution Strategy

### Test Prioritization
1. **CRITICAL Tests (Run first):** CORE-001, CORE-002, PROXY-001, RETRY-001, CACHE-001, AUTH-001, E2E-001, MIG-001, MIG-002, COMPAT-001, SEC-001
2. **HIGH Priority Tests:** All remaining HIGH priority tests
3. **MEDIUM/LOW Tests:** Run after critical path validated

### Automation Requirements
- All tests must be automatable (no manual steps)
- Tests must be deterministic (no flaky tests)
- Test execution time < 30 minutes for full suite
- Parallel execution support for test performance

### Coverage Goals
- **Code Coverage:** 85%+ of public APIs
- **Branch Coverage:** 75%+ of conditional logic
- **Integration Coverage:** 100% of component interactions

### Test Environment
- Clean database for each EF test
- Isolated Azure storage emulator for Azure tests
- Mock HTTP context for web tests
- Thread pool with 10+ threads for concurrency tests

---

## Validation Checklist

Before declaring migration complete, verify:

- ✅ All CRITICAL tests pass (100%)
- ✅ All HIGH priority tests pass (100%)
- ✅ MEDIUM/LOW tests pass (>95%)
- ✅ No performance regressions (within 5% of baseline)
- ✅ No memory leaks detected (stress test with 1M iterations)
- ✅ All security tests pass (100%)
- ✅ Backward compatibility verified (no breaking changes)
- ✅ Documentation updated with new test results

---

**Document Version:** 1.0  
**Last Updated:** Migration Planning Phase  
**Total Test Cases:** 35+ comprehensive specifications  
**Estimated Test Execution Time:** 25-30 minutes (full suite)
