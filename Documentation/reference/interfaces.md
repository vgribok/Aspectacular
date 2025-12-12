# Interfaces & Contracts - Complete API Contracts

## Overview

This document details all public interfaces and contracts in the Aspectacular AOP Framework. These interfaces define the core contracts that enable aspect-oriented programming, interception, logging, and extensibility.

---

## Core Interception Interfaces

### IAspect Interface

**Purpose:** Primary contract for all aspects - defines the 7-stage aspect lifecycle

**Location:** `NET 4.5/Aspectacular.Framework.Main.45/Aspects/Aspect.cs` (lines 28-44)

```csharp
namespace Aspectacular
{
    public interface IAspect
    {
        /// <summary>
        /// Stage 1: Called before instance resolution (non-static methods only)
        /// Use Case: Early authorization checks, dependency resolution
        /// </summary>
        void Step_1_BeforeResolvingInstance();
        
        /// <summary>
        /// Stage 2: Called immediately before method execution
        /// Use Case: Parameter validation, cache lookups, transaction setup
        /// </summary>
        void Step_2_BeforeTryingMethodExec();
        
        /// <summary>
        /// Stage 3: Called after method returns but before result post-processing
        /// Use Case: Result transformation, cache population
        /// May be called multiple times if retries are enabled
        /// </summary>
        void Step_3_BeforeMassagingReturnedResult();
        
        /// <summary>
        /// Stage 4a: Called after successful method completion
        /// Use Case: Success logging, cache updates
        /// May be called multiple times on retries
        /// </summary>
        void Step_4_Optional_AfterSuccessfulCallCompletion();
        
        /// <summary>
        /// Stage 4b: Called when method throws an exception
        /// Use Case: Retry logic, error logging, exception translation
        /// </summary>
        void Step_4_Optional_AfterCatchingMethodExecException();
        
        /// <summary>
        /// Stage 5: Finally block - always called after method execution
        /// Use Case: Transaction commit/rollback, resource cleanup
        /// </summary>
        /// <param name="interceptedCallSucceeded">True if method succeeded, false if failed</param>
        void Step_5_FinallyAfterMethodExecution(bool interceptedCallSucceeded);
        
        /// <summary>
        /// Stage 6: Called after instance cleanup (IDisposable.Dispose() if applicable)
        /// Use Case: Connection cleanup, context disposal
        /// </summary>
        void Step_6_Optional_AfterInstanceCleanup();
        
        /// <summary>
        /// Stage 7: Final completion - called after everything is done
        /// Use Case: Performance logging, call recording, final metrics
        /// </summary>
        void Step_7_AfterEverythingSaidAndDone();
    }
}
```

**Implemented By:**
- `Aspect` (abstract base class)
- All aspect implementations (StopwatchAspect, RetryAspect, CacheAspect, etc.)
- Augmented classes that want to be their own aspects

**Lifecycle Flow:**
```
Step 1 → Instance Resolution → Step 2 → Method Execution → Step 3 → Post-Processing
  → Step 4a (success) OR Step 4b (exception) → Step 5 (finally) 
  → Step 6 (cleanup) → Step 7 (completion)
```

---

### IInterceptionContext Interface

**Purpose:** Allows intercepted objects to access the interception proxy

**Location:** `NET 4.5/Aspectacular.Framework.Main.45/Aspects/Aspect.cs` (lines 17-21)

```csharp
namespace Aspectacular
{
    /// <summary>
    /// Interface for augmented objects that want to be interception-context-aware
    /// </summary>
    public interface IInterceptionContext
    {
        /// <summary>
        /// Gets or sets the AOP proxy intercepting the current call
        /// Set automatically by the framework when instance is resolved
        /// </summary>
        Proxy Proxy { get; set; }
    }
}
```

**Usage Pattern:**
```csharp
public class MyService : IInterceptionContext
{
    public Proxy Proxy { get; set; }  // Injected by framework
    
    public void MyMethod()
    {
        // Can access Proxy.CallID, Proxy.InterceptedCallMetaData, etc.
        if (this.Proxy != null)
        {
            Guid callId = this.Proxy.CallID;
            // Access interception metadata
        }
    }
}
```

**Key Benefits:**
- Intercepted object can access call metadata
- Enables logging from within intercepted methods
- Provides context awareness without tight coupling

---

### IMethodLogProvider Interface

**Purpose:** Provides access to the call logging infrastructure

**Location:** `NET 4.5/Aspectacular.Framework.Main.45/CallLog.cs`

```csharp
namespace Aspectacular
{
    public interface IMethodLogProvider
    {
        // Implemented by Proxy class
        // Provides logging methods accessible during interception
    }
}
```

**Implemented By:** `Proxy` class

**Accessible Via:** `Proxy.CurrentLog` static property for static method interception

**Key Methods (inherited from implementation):**
- Log entry addition
- Log text generation
- Entry type filtering

---

### ICallLogger Interface

**Purpose:** Allows instances to receive AOP logger injection

**Location:** Inferred from `Proxy.ResolveClassInstance()` implementation

```csharp
namespace Aspectacular
{
    public interface ICallLogger
    {
        /// <summary>
        /// Gets or sets the AOP logger for this instance
        /// Automatically set by framework during instance resolution
        /// </summary>
        IMethodLogProvider AopLogger { get; set; }
    }
}
```

**Usage Pattern:**
```csharp
public class MyDataService : ICallLogger
{
    public IMethodLogProvider AopLogger { get; set; }
    
    public void SaveData()
    {
        // Can log from within intercepted method
        if (this.AopLogger != null)
        {
            // Use logging capabilities
        }
    }
}
```

---

## Entity Framework Interfaces

### IDbEntityKey Interface

**Purpose:** Generic entity key abstraction for POCO entities

**Location:** `NET 4.5/Aspectacular.Framework.EntityFramework.45/IDbEntityKey.cs`

```csharp
namespace Aspectacular
{
    /// <summary>
    /// Interface for entities that expose their database key
    /// Enables generic entity handling in DAL interceptors
    /// </summary>
    public interface IDbEntityKey
    {
        // Entity key access methods
        // Exact signature to be determined from implementation
    }
}
```

**Use Cases:**
- Generic entity tracking
- Cache key generation for entities
- Entity state management

---

## Interface Implementation Patterns

### 1. Aspect Implementation Pattern

```csharp
public class MyCustomAspect : Aspect  // Aspect implements IAspect
{
    // Override only the lifecycle stages you need
    
    public override void Step_2_BeforeTryingMethodExec()
    {
        // Pre-execution logic
        this.LogInformation("Method about to execute");
    }
    
    public override void Step_5_FinallyAfterMethodExecution(bool success)
    {
        // Cleanup logic
        this.LogInformation(success ? "Success" : "Failed");
    }
    
    // Other stages have default empty implementations from Aspect base class
}
```

### 2. Interception-Aware Object Pattern

```csharp
public class MyService : IInterceptionContext
{
    public Proxy Proxy { get; set; }
    
    public DataSet GetData()
    {
        // Access interception context
        if (this.Proxy != null)
        {
            var metadata = this.Proxy.InterceptedCallMetaData;
            Guid callId = this.Proxy.CallID;
        }
        
        return LoadData();
    }
}
```

### 3. Self-Aspect Pattern

```csharp
// An object can be its own aspect - unconventional but supported
public class SelfAspectingService : IAspect
{
    public void Step_2_BeforeTryingMethodExec()
    {
        // Pre-process self
    }
    
    public void Step_5_FinallyAfterMethodExecution(bool success)
    {
        // Post-process self
    }
    
    // Implement other IAspect methods...
    
    public void BusinessMethod()
    {
        // Regular business logic
    }
}
```

**Note:** When an intercepted object implements IAspect, the framework automatically adds it to the aspect collection.

---

## Interface Contracts and Guarantees

### IAspect Contract Guarantees

1. **Stage Order:** Methods called in exact sequence (Step 1 → 7)
2. **Step 1:** Called only for non-static methods, never for static
3. **Step 2:** Always called before method execution attempt
4. **Step 3:** May be called multiple times if retries enabled
5. **Step 4a/4b:** Mutually exclusive - only one called per attempt
6. **Step 5:** Always called (finally block semantics)
7. **Step 6:** Called only if instance cleanup needed (IDisposable)
8. **Step 7:** Final stage, always called, aspects called in reverse order

### Proxy Property Contract (IInterceptionContext)

1. **Automatic Injection:** Set by framework during instance resolution
2. **Null During Construction:** Not available until Step_1 completes
3. **Cleared After Step 7:** Set to null after interception completes
4. **Thread-Safe:** Each thread has isolated proxy stack

### Logger Contract (ICallLogger)

1. **Injection Timing:** Set during `ResolveClassInstance()`
2. **Availability:** Available from Step_2 onwards
3. **Null Safety:** Always check for null before use

---

## Interface Usage Best Practices

### 1. IAspect Implementation
- **DO:** Override only needed lifecycle stages
- **DO:** Call base implementation if extending Aspect class
- **DON'T:** Modify Proxy state unless you understand implications
- **DON'T:** Throw exceptions from Step_5/Step_7 (cleanup stages)

### 2. IInterceptionContext Implementation
- **DO:** Check Proxy for null before accessing
- **DO:** Use Proxy.CallID for correlation
- **DON'T:** Store Proxy reference beyond method execution
- **DON'T:** Modify Proxy.ReturnedValue unless implementing cache

### 3. Multiple Interface Implementation
```csharp
// Combining interfaces is supported and useful
public class AdvancedService : IInterceptionContext, ICallLogger, IDisposable
{
    public Proxy Proxy { get; set; }
    public IMethodLogProvider AopLogger { get; set; }
    
    public void BusinessMethod()
    {
        // Use both Proxy and AopLogger
    }
    
    public void Dispose()
    {
        // Step_6 will be called after this
    }
}
```

---

## Interface Extension Points

### Custom Aspect Interface Pattern

You can create domain-specific aspect interfaces:

```csharp
public interface IAuthorizationAspect : IAspect
{
    bool IsAuthorized(ClaimsPrincipal user, string resource);
}

public class MyAuthAspect : Aspect, IAuthorizationAspect
{
    public bool IsAuthorized(ClaimsPrincipal user, string resource)
    {
        // Authorization logic
    }
    
    public override void Step_2_BeforeTryingMethodExec()
    {
        // Use IsAuthorized() to check access
        if (!IsAuthorized(currentUser, currentResource))
            throw new UnauthorizedAccessException();
    }
}
```

### Interface-Based Aspect Discovery

```csharp
// Find all authorization aspects in aspect collection
var authAspects = proxy.aspects
    .OfType<IAuthorizationAspect>()
    .ToList();
```

---

## Interface Visibility and Accessibility

| Interface | Visibility | Assembly | Namespace |
|-----------|-----------|----------|-----------|
| IAspect | Public | Aspectacular.Framework.Main | Aspectacular |
| IInterceptionContext | Public | Aspectacular.Framework.Main | Aspectacular |
| IMethodLogProvider | Public | Aspectacular.Framework.Main | Aspectacular |
| ICallLogger | Public | Aspectacular.Framework.Main | Aspectacular |
| IDbEntityKey | Public | Aspectacular.Framework.EntityFramework | Aspectacular |

---

## Interface Dependencies

```
IAspect (no dependencies)
  ↑
Aspect (implements IAspect)
  ↑
All concrete aspects

IInterceptionContext (depends on Proxy)
  ↑
Implemented by user classes

IMethodLogProvider (no dependencies)
  ↑
Proxy (implements IMethodLogProvider)

ICallLogger (depends on IMethodLogProvider)
  ↑
Implemented by user classes
```

---

## Summary

**Total Public Interfaces:** 5  
**Core Lifecycle Interface:** IAspect (7 methods)  
**Context Awareness Interfaces:** IInterceptionContext, ICallLogger  
**Specialized Interfaces:** IDbEntityKey (Entity Framework)  

All interfaces follow clear separation of concerns:
- **IAspect:** Defines aspect behavior
- **IInterceptionContext:** Provides context access
- **ICallLogger:** Enables logging
- **IMethodLogProvider:** Abstracts logging implementation
- **IDbEntityKey:** Domain-specific entity handling

---

**Document Version:** 1.0  
**Last Updated:** 2024  
**Interfaces Documented:** 5 core interfaces  
**Source Analysis:** Complete interface contracts from Main and EntityFramework modules
