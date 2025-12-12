# Business Logic - AOP Framework Business Rules

## Overview
This document extracts business logic and domain rules embedded in the Aspectacular AOP Framework's implementation.

## Core Business Concepts

### 1. Aspect-Oriented Programming as Business Capability
**Business Value:** Enable cross-cutting concerns (logging, caching, security, transactions) without code modification  
**Implementation:** Proxy-based dynamic interception with 7-stage lifecycle

### 2. Method Call Lifecycle Management
**Business Process:** Structured interception pipeline with well-defined stages  
**Stages:** Step_1 (pre-instantiation) → Step_7 (final completion)  
**Business Rule:** Each stage is optional; aspects override only needed stages

### 3. Aspect Ordering and Dependencies
**Business Rule:** Aspects execute in declared order; dependencies enforced via RequiredAspectAttribute  
**Priority:** Required aspects auto-instantiated if missing (configurable)  
**Pattern:** Chain of Responsibility with dependency resolution

## Retry Logic Business Rules (RetryAspect)

### Decision: When to Retry
- **IF** exception is transient (network timeout, DB deadlock, etc.)
- **AND** max attempts not exceeded
- **THEN** retry method execution after delay

### Parameters
- `maxAttempts`: Maximum retry count (default: 3)
- `delayMillisec`: Delay between retries (default: 100ms)
- `exponentialBackoff`: Increase delay exponentially (optional)

### Business Logic
```
Attempt 1: Execute method
  → If fails with transient error: Wait {delay}ms
Attempt 2: Execute method
  → If fails with transient error: Wait {delay * 2}ms (if exponential)
Attempt 3: Execute method
  → If still fails: Throw exception to caller
```

**Source:** `NET 4.5/Aspectacular.Framework.Main.45/Aspects/RetryAspects.cs`

## Caching Business Rules (CacheAspect)

### Decision: When to Cache
- **IF** method marked with [InvariantReturn(true)]
- **AND** method has no out/ref parameters
- **AND** result is instance-invariant
- **THEN** cache result keyed by method signature + parameters

### Cache Key Generation
```
Key = {ClassName}.{MethodName}({Param1Value}, {Param2Value}, ...)
Example: "CustomerService.GetCustomer(customerId = 42)"
```

### Cache Invalidation Rules
- Time-based expiration (configurable per cache)
- Manual invalidation via cache key
- Dependency-based invalidation (custom logic)

**Source:** `NET 4.5/Aspectacular.Framework.Main.45/Aspects/CacheAspect.cs`

## Authorization Business Rules

### Claims-Based Authorization
**Business Rule:** Method executes only if current principal has required claim  
**Decision Logic:**
```
IF user.HasClaim(requiredClaim) THEN
    Allow method execution
ELSE
    Throw UnauthorizedAccessException
```

### Role-Based Authorization (ASP.NET)
**Business Rule:** User must be in required role  
**Decision Logic:**
```
IF HttpContext.User.IsInRole(requiredRole) THEN
    Allow method execution
ELSE
    Throw UnauthorizedAccessException("User not in role: {role}")
```

**Source:** `ClaimsAuthorizationAspect.cs`, `AspNetRoleAuthorizationAspect.cs`

## Transaction Management Business Rules (DtcTransactionAspect)

### Decision: When to Use Transactions
- **IF** method modifies multiple resources (databases, message queues)
- **AND** atomic commit required across all resources
- **THEN** wrap execution in DTC transaction

### Transaction Lifecycle
```
Begin DTC Transaction
  → Execute method
  → IF method succeeds:
      Commit transaction (all resources)
  → ELSE IF method fails:
      Rollback transaction (all resources)
```

**Isolation Level:** Read Committed (default), configurable  
**Timeout:** 60 seconds (default), configurable

**Source:** `NET 4.5/Aspectacular.Framework.Main.45/Aspects/DtcTransactionAspect.cs`

## Performance Monitoring Business Rules (StopwatchAspect)

### Automatic Timing
**Business Value:** Measure method execution time without code changes  
**Metrics Captured:**
- Gross method time (including aspects)
- Net method time (excluding aspect overhead)
- Per-aspect execution time
- Total call duration

### Logging Rules
```
Log Format: "Method {MethodSignature} executed in {ElapsedMs}ms"
Threshold: Log warning if execution > configured threshold
```

**Source:** `NET 4.5/Aspectacular.Framework.Main.45/Aspects/StopwatchAspect.cs`

## DAL Interception Business Logic

### Pattern
```
Query Initiation
  → Apply pre-execution aspects (auth, validation)
  → Execute database query
  → Apply post-execution aspects (caching, logging)
  → Return results
```

### Business Rules
1. **Authorization first:** Security checks before query execution
2. **Cache after query:** Populate cache with query results
3. **Log always:** Record query execution (success/failure)
4. **Transaction scope:** Ensure queries within transaction boundary

**Source:** `NET 4.5/Aspectacular.Framework.Main.45/DalInterceptor.cs`

## Entity Framework Integration Business Logic

### Lazy Loading Control
**Business Rule:** Toggle lazy loading during method execution  
**Use Case:** Prevent N+1 query problem by disabling lazy load during list operations

### Pattern
```
Save original lazy loading setting
  → Disable lazy loading
  → Execute method (loads all required data eagerly)
  → Restore lazy loading setting
```

**Source:** `NET 4.5/Aspectacular.Framework.EntityFramework.45/LazyLoadSwitch.cs`

## Summary
- **7-stage lifecycle** provides fine-grained control
- **Retry logic** handles transient failures automatically
- **Caching** based on method invariance properties
- **Authorization** enforced before method execution
- **Transactions** ensure atomicity across resources
- **Performance monitoring** automatic and non-intrusive
- **DAL patterns** standardize data access
- **EF integration** optimizes ORM behavior

**Document Version:** 1.0  
**Source Analysis:** Static code analysis of Core and Main modules
