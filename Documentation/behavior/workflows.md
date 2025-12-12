# Workflows - Aspect Execution Sequences

## Complete 7-Stage Aspect Lifecycle Workflow

```
┌─────────────────────────────────────────────────────────┐
│ Application Code Calls Intercepted Method              │
└───────────────────┬─────────────────────────────────────┘
                    ▼
        ┌──────────────────────────┐
        │ Proxy.Invoke() Called    │
        └────────┬─────────────────┘
                 ▼
    ┌────────────────────────────────┐
    │ STAGE 1: BeforeResolveInstance │  [Instance methods only]
    │ • Check authorization early    │
    │ • Resolve dependencies         │
    └────────┬───────────────────────┘
             ▼
    ┌────────────────────────────┐
    │ Resolve Instance           │
    │ • Call instanceFactory()   │
    │ • Inject proxy reference   │
    └────────┬───────────────────┘
             ▼
    ┌─────────────────────────────────┐
    │ STAGE 2: BeforeTryingMethodExec │
    │ • Validate parameters            │
    │ • Check cache for results        │
    │ • Start transactions             │
    │ • Check required aspects         │
    └────────┬────────────────────────┘
             ▼
    ┌────────────────────────┐
    │ Execute Method (try)   │ ← Actual intercepted method
    └────────┬───────────────┘
             ▼
    ┌──────────────────────────────────────┐
    │ STAGE 3: BeforeMassagingReturnedResult│
    │ • Transform return value              │
    │ • Populate cache                      │
    │ • Apply LINQ modifiers (ToList, etc.) │
    └────────┬────────────────────────────┘
             ▼
      ┌─────────────┐
      │ Success?    │
      └──┬──────┬───┘
         │ Yes  │ No
         ▼      ▼
    ┌─────────────────────┐  ┌────────────────────────────────┐
    │ STAGE 4a: Success   │  │ STAGE 4b: AfterCatchingException│
    │ • Log success       │  │ • Decide if retry               │
    │ • Commit changes    │  │ • Log exception                 │
    │ • Update metrics    │  │ • Transform exception           │
    └──────────┬──────────┘  └────────┬───────────────────────┘
               │                      │
               └──────────┬───────────┘
                          ▼
            ┌──────────────────────────────┐
            │ STAGE 5: FinallyAfterMethodExec│  [Always executed]
            │ • Commit/rollback transaction  │
            │ • Release resources            │
            │ • Final logging                │
            └────────┬─────────────────────┘
                     ▼
       ┌─────────────────────────────┐
       │ Instance Cleanup Needed?    │
       └──────┬──────────────┬───────┘
              │ Yes          │ No
              ▼              │
    ┌───────────────────────────┐   │
    │ STAGE 6: AfterInstanceCleanup│
    │ • Call IDisposable.Dispose()│ │
    │ • Close connections         │ │
    │ • Clean up contexts         │ │
    └───────────┬───────────────┘   │
                └──────┬──────────────┘
                       ▼
          ┌──────────────────────────────┐
          │ STAGE 7: AfterEverythingSaid │
          │ • Performance logging         │
          │ • Call recording              │
          │ • Final metrics               │
          │ • Clear proxy reference       │
          └────────┬─────────────────────┘
                   ▼
        ┌──────────────────────┐
        │ Return to Application│
        └──────────────────────┘
```

## Retry Workflow with Backoff

```
Attempt = 1
┌─────────────────────┐
│ Execute Method      │
└─────┬───────────────┘
      ▼
  ┌───────────┐
  │ Success?  │
  └─┬─────┬───┘
    │Yes  │No
    ▼     ▼
 Return  ┌──────────────────┐
         │ Transient Error? │
         └─┬────────────┬───┘
           │Yes         │No
           ▼            ▼
    ┌────────────┐   Throw
    │ Retry?     │  Exception
    │ Attempt<Max│
    └─┬──────┬───┘
      │Yes   │No
      ▼      ▼
  ┌────────┐ Throw
  │ Wait   │Exception
  │ Delay  │
  └───┬────┘
      ▼
  Attempt++
  Delay *= 2  (if exponential)
      │
      └─────→ [Execute Method]
```

## Cache Lookup Workflow

```
Method Called
      ▼
┌─────────────────┐
│ Is Cacheable?   │
│ [InvariantReturn]│
└──┬───────────┬──┘
   │Yes        │No
   ▼           ▼
┌──────────────┐ Execute
│ Generate Key │ Method
│ Check Cache  │ Normally
└──┬──────┬────┘
   │Hit   │Miss
   ▼      ▼
Return  Execute
Cached  Method
Result     │
           ▼
       ┌──────────┐
       │ Cache    │
       │ Result   │
       └──────────┘
```

## Complete Documentation Generated
See [Business Logic](business-logic.md) for detailed rules  
See [Decision Logic](decision-logic.md) for decision trees  
See [Error Handling](error-handling.md) for exception patterns

**Document Version:** 1.0
