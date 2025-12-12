# Error Handling - Exception Propagation Patterns

## Exception Flow Through Aspect Pipeline

```
Method Throws Exception
         ↓
Captured in Proxy.ExecuteMainSequence()
         ↓
Stored in MethodExecutionException property
         ↓
Stage 4b: AfterCatchingMethodExecException() called for each aspect
         ↓
Aspects can:
  • Set ShouldRetryCall = true (trigger retry)
  • Transform exception (set new MethodExecutionException)
  • Log exception
  • Take corrective action
         ↓
IF ShouldRetryCall = true:
    Clear exception, retry method
ELSE:
    Stage 5: Finally block executes
         ↓
    Exception rethrown to caller
```

## Exception Transformation Pattern

**RetryAspect Example:**
```csharp
Step_4_Optional_AfterCatchingMethodExecException():
    IF exception is transient:
        Log("Retry attempt {AttemptsMade}")
        ShouldRetryCall = true  // Prevents exception from propagating
    ELSE:
        // Let exception propagate
```

## Error Recovery Strategies

1. **Retry** - Catch transient exceptions, retry execution
2. **Fallback** - Return default/cached value on error
3. **Transform** - Convert framework exceptions to domain exceptions
4. **Log and Rethrow** - Record error details, preserve original exception
5. **Circuit Breaker** - Stop attempts after repeated failures (custom aspect)

**Document Version:** 1.0  
**Source:** AspectInjector.cs ExecuteMainSequence method
