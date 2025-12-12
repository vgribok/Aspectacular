# Decision Logic - Framework Decision Trees

## Retry Decision Table

| Condition | Action |
|-----------|--------|
| Exception is SqlException with timeout | Retry |
| Exception is DbUpdateException (optimistic concurrency) | Retry |
| Exception is TimeoutException | Retry |
| Exception is SocketException (network) | Retry |
| Attempts < MaxAttempts | Continue retry loop |
| Attempts >= MaxAttempts | Throw exception |
| Non-transient exception | Throw immediately |

## Caching Decision Matrix

| Method Has InvariantReturn? | Has Out/Ref Params? | Result | Cache? |
|----------------------------|---------------------|---------|--------|
| Yes | No | Success | ✓ Cache |
| Yes | Yes | Success | ✗ Don't cache (multiple outputs) |
| No | No | Success | ✗ Not cacheable |
| Yes | No | Exception | ✗ Don't cache errors |

## Aspect Execution Decision Flow

```
FOR each aspect in aspects:
    IF aspect is required AND missing:
        IF InstantiateIfMissing = DontInstantiate:
            THROW RequiredAspectMissingException
        ELSE IF InstantiateIfMissing = InstantiateAndPrepend:
            CREATE aspect
            INSERT at beginning
        ELSE IF InstantiateIfMissing = InstantiateAndAppend:
            CREATE aspect
            APPEND to end
    
    EXECUTE aspect.Step_N()
    
    IF aspect sets StopAspectCallChain:
        BREAK aspect loop
```

## Method Interception Decision

```
IF method is static:
    Skip Step_1_BeforeResolvingInstance
    Execute Steps 2-7
ELSE IF method is instance:
    Execute all Steps 1-7
    
IF CancelInterceptedMethodCall is set:
    Skip actual method execution
    Use cached/pre-supplied result
```

**Document Version:** 1.0
