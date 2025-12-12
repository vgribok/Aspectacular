# Use Cases - Common Framework Usage Patterns

## Use Case 1: Automatic Method Performance Monitoring
**Goal:** Track execution time of all service methods without code changes  
**Solution:** Apply StopwatchAspect globally  
**Benefits:** Non-intrusive performance profiling

## Use Case 2: Automatic Retry for Transient Database Failures
**Goal:** Retry database operations on timeout/deadlock  
**Solution:** Apply RetryAspect to data access methods  
**Benefits:** Improved resilience without custom retry logic

## Use Case 3: Result Caching for Expensive Queries
**Goal:** Cache database query results to reduce database load  
**Solution:** Mark methods with [InvariantReturn], apply CacheAspect  
**Benefits:** Reduced database queries, faster response times

## Use Case 4: Claims-Based Authorization
**Goal:** Enforce access control based on user claims  
**Solution:** Apply ClaimsAuthorizationAspect with required claims  
**Benefits:** Centralized security enforcement

## Use Case 5: Distributed Transaction Management
**Goal:** Ensure atomic operations across multiple databases  
**Solution:** Apply DtcTransactionAspect to coordinator methods  
**Benefits:** Automatic transaction boundaries

## Use Case 6: Cross-Cutting Logging
**Goal:** Log all method calls with parameters and results  
**Solution:** Apply LoggingAspect or custom aspect with logging  
**Benefits:** Comprehensive audit trail without scattered logging code

## Use Case 7: Entity Framework Lazy Loading Control
**Goal:** Prevent N+1 query problem in list operations  
**Solution:** Use EfInterceptor with LazyLoadSwitch  
**Benefits:** Optimized database queries

## Use Case 8: DAL Standardization
**Goal:** Consistent patterns for data access  
**Solution:** Use DalInterceptor helper for all database calls  
**Benefits:** Standardized error handling, logging, caching

**Document Version:** 1.0
