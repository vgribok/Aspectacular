# Program Structure - Complete Type Hierarchies

## Overview

This document provides a comprehensive mapping of the Aspectacular AOP Framework's program structure, including all classes, interfaces, enums, delegates, and their relationships. The framework is organized into distinct modules with clear dependency hierarchies.

**Source Code Location:** Multiple framework versions  
- `NET 4.5/Aspectacular.Framework.*` (primary reference)
- `NET 4.0/Aspectacular.Framework.*` (legacy)
- `NET Core/Aspectacular.Framework.*` (cross-platform, limited)

---

## Module Organization

```
Aspectacular Framework
├── Aspectacular.Framework.Core        (Base utilities - no dependencies)
├── Aspectacular.Framework.Main        (Core AOP engine - depends on Core)
├── Aspectacular.Framework.EntityFramework  (EF integration - depends on Main)
├── Aspectacular.Framework.Web         (ASP.NET aspects - depends on Main)
└── Aspectacular.Framework.Azure       (Azure integration - depends on Main)
```

---

## Aspectacular.Framework.Main - Core AOP Engine

### Primary Classes and Hierarchies

#### Aspect Hierarchy (Core Interception Pattern)

```
System.Object
  └── Aspect (abstract base class)
        ├── Implements: IAspect
        │
        ├── StopwatchAspect             [Performance monitoring]
        ├── RetryAspect                 [Retry logic]
        │     └── DefaultRetryAspect
        ├── CacheAspect                 [Result caching]
        │     └── CacheAspectBase
        ├── ClaimsAuthorizationAspect   [Claims-based security]
        ├── DtcTransactionAspect        [Distributed transactions]
        ├── LinqToSqlAspect            [LINQ to SQL integration]
        ├── SqlConnectionAttributesAspect [SQL connection management]
        ├── SqlExecutionPlanAspect      [SQL query plan capture]
        ├── LogOutputAspectBase (abstract) [Logging base]
        │     └── ConsoleAspect
        │     └── TraceAspect
        └── SimpleStockAspects          [Example aspects]
              ├── ConstraintAttribute
              ├── StockQuantityConstraintAspect
              └── StockPriceConstraintAspect
```

**Source Files:**
- `NET 4.5/Aspectacular.Framework.Main.45/Aspects/Aspect.cs` (lines 1-350)
- `NET 4.5/Aspectacular.Framework.Main.45/Aspects/StopwatchAspect.cs`
- `NET 4.5/Aspectacular.Framework.Main.45/Aspects/RetryAspects.cs`
- `NET 4.5/Aspectacular.Framework.Main.45/Aspects/CacheAspect.cs`
- `NET 4.5/Aspectacular.Framework.Main.45/Aspects/ClaimsAuthorizationAspect.cs`
- `NET 4.5/Aspectacular.Framework.Main.45/Aspects/DtcTransactionAspect.cs`

#### Proxy Hierarchy (Interception Engine)

```
System.Object
  └── CallLifetimeLog (base logging infrastructure)
        └── Proxy
              ├── Implements: IMethodLogProvider
              │
              └── DispatcherProxy (specialized for IDisposable handling)
```

**Key Properties:**
- `Guid CallID` - Unique call identifier
- `object AugmentedClassInstance` - Intercepted instance (null for static methods)
- `object ReturnedValue` - Method return value
- `Exception MethodExecutionException` - Captured exception if method failed
- `InterceptedMethodMetadata InterceptedCallMetaData` - Method metadata
- `byte AttemptsMade` - Retry attempt counter
- `bool ShouldRetryCall` - Retry flag set by aspects
- `bool CancelInterceptedMethodCall` - Cache hit flag
- `bool MethodWasCalled` - Execution state tracking

**Source Files:**
- `NET 4.5/Aspectacular.Framework.Main.45/AspectInjector.cs` (Proxy class, lines 1-800)
- `NET 4.5/Aspectacular.Framework.Main.45/DispAspectInjector.cs` (DispatcherProxy)
- `NET 4.5/Aspectacular.Framework.Main.45/CallLog.cs` (CallLifetimeLog base)

#### Method Metadata Hierarchy

```
System.Object
  └── InterceptedMethodMetadata
        ├── PostProcessingMethodMetadata (for return value post-processors)
        └── Contains: List<InterceptedMethodParamMetadata>
```

**InterceptedMethodMetadata Class:**
- **Purpose:** Comprehensive method reflection metadata
- **Key Properties:**
  - `MethodInfo MethodReflectionInfo` - Reflection metadata
  - `Type ClassType` - Declaring class type
  - `Type MethodReturnType` - Return type
  - `bool IsStaticMethod` - Static vs. instance
  - `bool IsReturnResultInvariant` - Cacheability indicator
  - `bool IsReturnValueSecret` - Sensitive data flag
  - `List<InterceptedMethodParamMetadata> Params` - Parameter metadata

- **Key Methods:**
  - `string GetMethodSignature(ParamValueOutputOptions)` - Format method signature
  - `TAttribute GetMethodAttribute<TAttribute>()` - Attribute access
  - `TAttribute GetClassAttribute<TAttribute>()` - Class attribute access
  - `TAttribute GetMethodOrClassAttribute<TAttribute>()` - Combined lookup

**InterceptedMethodParamMetadata Class:**
- **Purpose:** Individual parameter metadata and evaluation
- **Key Properties:**
  - `ParameterInfo ParamReflection` - Raw reflection data
  - `string Name` - Parameter name
  - `Type Type` - Parameter type
  - `object SlowEvaluatingValueLoader` - Lazy parameter value evaluation
  - `ParamDirectionEnum Direction` - In/Out/Ref direction
  - `bool ValueIsSecret` - Sensitive parameter flag

**Source Files:**
- `NET 4.5/Aspectacular.Framework.Main.45/InterceptedMethodMetadata.cs` (lines 1-700)

#### Call Logging Infrastructure

```
System.Object
  ├── CallLogEntry (individual log entry)
  │     ├── DateTime Timestamp
  │     ├── EntryType Type (Info/Warning/Error)
  │     ├── string Key (optional categorization)
  │     └── string Message
  │
  └── CallLifetimeLog (log collection and management)
        ├── List<CallLogEntry> CallLog
        ├── EntryType? WorstEntryType
        ├── EntryType? PresentEntryTypes
        └── Methods:
              ├── AddLogEntry()
              ├── GetLogText()
              └── Log formatting methods
```

**Source Files:**
- `NET 4.5/Aspectacular.Framework.Main.45/CallLog.cs` (lines 1-300)

#### Data Access Layer Helpers

```
System.Object
  ├── DalInterceptor (static helper class)
  │     └── Static methods for DAL interception patterns
  │
  └── DbEngineProxy (database-specific proxy)
        └── Specialized for DbContext/DbConnection interception
```

**DalInterceptor Key Methods:**
- `TOut Invoke<TOut>(Func<TOut> func, params Aspect[] aspects)` - Function interception
- `void Invoke(Action action, params Aspect[] aspects)` - Action interception
- Provides convenient DAL-focused interception patterns

**Source Files:**
- `NET 4.5/Aspectacular.Framework.Main.45/DalInterceptor.cs`
- `NET 4.5/Aspectacular.Framework.Main.45/DbEngineProxy.cs`

---

## Interface Definitions

### IAspect Interface (Core Contract)

```csharp
public interface IAspect
{
    void Step_1_BeforeResolvingInstance();
    void Step_2_BeforeTryingMethodExec();
    void Step_3_BeforeMassagingReturnedResult();
    void Step_4_Optional_AfterSuccessfulCallCompletion();
    void Step_4_Optional_AfterCatchingMethodExecException();
    void Step_5_FinallyAfterMethodExecution(bool interceptedCallSucceeded);
    void Step_6_Optional_AfterInstanceCleanup();
    void Step_7_AfterEverythingSaidAndDone();
}
```

**Purpose:** Defines the 7-stage aspect lifecycle contract  
**Source:** `NET 4.5/Aspectacular.Framework.Main.45/Aspects/Aspect.cs` (lines 36-44)

### IInterceptionContext Interface

```csharp
public interface IInterceptionContext
{
    Proxy Proxy { get; set; }
}
```

**Purpose:** Allows intercepted objects to be context-aware  
**Usage:** Implement on classes that need access to the interception proxy  
**Source:** `NET 4.5/Aspectacular.Framework.Main.45/Aspects/Aspect.cs` (lines 17-21)

### IMethodLogProvider Interface

```csharp
public interface IMethodLogProvider
{
    // Provides access to call logging infrastructure
}
```

**Purpose:** Enables logging access for intercepted methods  
**Implemented By:** Proxy class  
**Source:** `NET 4.5/Aspectacular.Framework.Main.45/CallLog.cs`

### ICallLogger Interface

```csharp
public interface ICallLogger
{
    IMethodLogProvider AopLogger { get; set; }
}
```

**Purpose:** Allows instances to receive AOP logger injection  
**Source:** Inferred from Proxy.ResolveClassInstance() method

---

## Attribute Definitions

### Aspect-Related Attributes

#### RequiredAspectAttribute

```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequiredAspectAttribute : Attribute
{
    public Type AspectClassType { get; }
    public WhenRequiredAspectIsMissing InstantiateIfMissing { get; }
    
    public RequiredAspectAttribute(Type aspectType, 
        WhenRequiredAspectIsMissing whenMissing = WhenRequiredAspectIsMissing.DontInstantiate)
}
```

**Purpose:** Declares aspect dependencies for methods/classes  
**Use Case:** Enforce that specific aspects are present in aspect chain  
**Source:** `NET 4.5/Aspectacular.Framework.Main.45/Aspects/RequiredAspectAttribute.cs`

### Method Metadata Attributes

#### InvariantReturnAttribute

```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method, 
    AllowMultiple = false)]
public class InvariantReturnAttribute : Attribute
{
    public bool IsInstanceInvariant { get; private set; }
    
    public InvariantReturnAttribute(bool isInstanceInvariant = true)
}
```

**Purpose:** Marks methods/classes as cacheable (instance-invariant returns)  
**Semantics:** Method returns same data for different instances called simultaneously  
**Source:** `NET 4.5/Aspectacular.Framework.Main.45/InterceptedMethodMetadata.cs` (lines 46-68)

#### SecretParamValueAttribute

```csharp
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = false)]
public class SecretParamValueAttribute : Attribute
{
}
```

**Purpose:** Marks parameters/return values containing sensitive data  
**Effect:** Aspects avoid logging actual values, use hash instead  
**Source:** `NET 4.5/Aspectacular.Framework.Main.45/InterceptedMethodMetadata.cs` (lines 18-24)

---

## Enumerations

### EntryType (Log Entry Classification)

```csharp
[Flags]
public enum EntryType
{
    Info = 1,
    Warning = 2,
    Error = 4
}
```

**Purpose:** Categorizes log entries by severity  
**Usage:** Bitwise flags for filtering and worst-type detection  
**Source:** `NET 4.5/Aspectacular.Framework.Main.45/CallLog.cs`

### ParamValueOutputOptions (Parameter Value Formatting)

```csharp
public enum ParamValueOutputOptions
{
    NoValue,              // Don't show parameter values
    SlowUIValue,          // User-friendly display (slow - uses Compile + Invoke)
    SlowInternalValue     // Internal caching format (slow - uses reflection)
}
```

**Purpose:** Controls parameter value evaluation strategy  
**Performance Note:** Slow options use Expression.Compile() and reflection  
**Source:** `NET 4.5/Aspectacular.Framework.Main.45/InterceptedMethodMetadata.cs` (lines 71-87)

### ParamDirectionEnum (Parameter Direction)

```csharp
public enum ParamDirectionEnum
{
    In,        // Input parameter
    Out,       // Output parameter
    RefInOut   // Reference (input/output) parameter
}
```

**Purpose:** Identifies parameter direction for interception  
**Source:** Inferred from Core extension methods

### WhenRequiredAspectIsMissing (Aspect Instantiation Strategy)

```csharp
public enum WhenRequiredAspectIsMissing
{
    DontInstantiate,        // Throw exception if missing
    InstantiateAndPrepend,  // Create and add first
    InstantiateAndAppend    // Create and add last
}
```

**Purpose:** Controls automatic aspect instantiation behavior  
**Source:** `NET 4.5/Aspectacular.Framework.Main.45/Aspects/RequiredAspectAttribute.cs`

---

## Aspectacular.Framework.Core - Utility Classes

### Extension Method Classes (Static Utility Classes)

#### AdoNetExtensions

**Purpose:** ADO.NET helper methods for database operations  
**Key Methods:**
- Database connection extensions
- Command execution helpers
- Result set processing utilities

**Source:** `NET 4.5/Aspectacular.Framework.Core.45/AdoNetExtensions.cs`

#### CollectionExtensions

**Purpose:** Collection and LINQ utility methods  
**Key Methods:**
- `ForEach<T>()` - Iterate with action
- `SmartUnion<T>()` - Intelligent collection merging
- `ReverseOrder<T>()` - Reverse enumeration
- `IsNullOrEmpty<T>()` - Null/empty checks

**Source:** `NET 4.5/Aspectacular.Framework.Core.45/CollectionExtensions.cs`

#### DateTimeExtensions

**Purpose:** Date/time manipulation helpers  
**Key Classes:**
- `RelativeTimeSpan` - Human-readable time spans (e.g., "2 days ago")
- Extension methods for DateTime formatting and arithmetic

**Source:** `NET 4.5/Aspectacular.Framework.Core.45/DateTimeExtensions.cs`
**Source:** `NET 4.5/Aspectacular.Framework.Core.45/RelativeTimeSpan.cs`

#### QueryModifiers (Advanced LINQ)

**Purpose:** Complex LINQ query building utilities  
**Key Classes:**
- `PredicateBuilder` - Dynamic predicate composition using Expression trees
- `FullOuterJoin<T>` - Full outer join implementation for LINQ
- Query composition helpers

**Source:** `NET 4.5/Aspectacular.Framework.Core.45/QueryModifiers.cs`
**Source:** `NET 4.5/Aspectacular.Framework.Core.45/PredicateBuilder.cs`
**Source:** `NET 4.5/Aspectacular.Framework.Core.45/FullOuterJoin.cs`

#### StringExtensions

**Purpose:** String manipulation and formatting  
**Key Methods:**
- `SmartFormat()` - String.Format() with error handling
- `ToStringEx()` - Safe ToString() with null handling
- String parsing and validation

**Source:** `NET 4.5/Aspectacular.Framework.Core.45/StringExtensions.cs`

#### TypeAndReflectionExtensions

**Purpose:** Type system and reflection helpers  
**Key Methods:**
- `FormatCSharp()` - C# type name formatting (handles generics, arrays)
- `IsSimpleCSharpType()` - Identifies primitive/simple types
- `GetCustomAttribute<T>()` - Generic attribute access
- Generic type manipulation

**Source:** `NET 4.5/Aspectacular.Framework.Core.45/TypeAndReflectionExtensions.cs`

#### Threading

**Purpose:** Thread synchronization and blocking utilities  
**Key Methods:**
- `Sleep()` - Interruptible sleep with application exit awareness
- `BlockingPoll` - Polling with backoff strategies

**Source:** `NET 4.5/Aspectacular.Framework.Core.45/Threading.cs`
**Source:** `NET 4.5/Aspectacular.Framework.Core.45/BlockingPoll.cs`

#### ExceptionExtensions

**Purpose:** Exception handling and formatting  
**Key Methods:**
- `ToStringEx()` - Comprehensive exception formatting with inner exceptions

**Source:** `NET 4.5/Aspectacular.Framework.Core.45/ExceptionExtensions.cs`

#### QueryableExtensions

**Purpose:** IQueryable<T> and IEnumerable<T> helpers  
**Source:** `NET 4.5/Aspectacular.Framework.Core.45/QueryableExtensions.cs`

### Utility Classes (Non-Extension)

#### Disposer

**Purpose:** IDisposable helper for using-pattern automation  
**Usage:** Simplifies resource cleanup patterns

**Source:** `NET 4.5/Aspectacular.Framework.Core.45/Disposer.cs`

#### SvcLocator (Service Locator)

**Purpose:** Simple dependency injection / service location pattern  
**Warning:** Service Locator is considered an anti-pattern in modern design

**Source:** `NET 4.5/Aspectacular.Framework.Core.45/SvcLocator.cs`

#### ModifiedString

**Purpose:** String tracking with modification detection  
**Source:** `NET 4.5/Aspectacular.Framework.Core.45/ModifiedString.cs`

#### Range<T>

**Purpose:** Generic range representation with min/max bounds  
**Source:** `NET 4.5/Aspectacular.Framework.Core.45/Range.cs`

#### Caching

**Purpose:** Caching infrastructure and utilities  
**Source:** `NET 4.5/Aspectacular.Framework.Core.45/Caching.cs`

#### SqlConnAttribs

**Purpose:** SQL connection string attribute manipulation  
**Source:** `NET 4.5/Aspectacular.Framework.Core.45/SqlConnAttribs.cs`

---

## Aspectacular.Framework.EntityFramework - ORM Integration

### Entity Framework-Specific Classes

#### EfInterceptor

```
System.Object
  └── EfInterceptor (static helper class)
```

**Purpose:** Entity Framework-specific interception patterns  
**Key Features:**
- DbContext method interception
- Lazy loading control integration
- Entity state tracking

**Source:** `NET 4.5/Aspectacular.Framework.EntityFramework.45/EfInterceptor.cs`

#### LazyLoadSwitch

```
System.Object
  └── LazyLoadSwitch : Aspect
```

**Purpose:** Aspect for controlling Entity Framework lazy loading  
**Functionality:** Temporarily enables/disables lazy loading during method execution

**Source:** `NET 4.5/Aspectacular.Framework.EntityFramework.45/LazyLoadSwitch.cs`

### Interfaces

#### IDbEntityKey

```csharp
public interface IDbEntityKey
{
    // Entity key abstraction for generic entity handling
}
```

**Purpose:** Generic entity key access for POCO entities  
**Source:** `NET 4.5/Aspectacular.Framework.EntityFramework.45/IDbEntityKey.cs`

---

## Aspectacular.Framework.Web - ASP.NET Integration

### Web-Specific Aspects

#### AspNetRoleAuthorizationAspect

```
Aspect
  └── AspNetRoleAuthorizationAspect
```

**Purpose:** Role-based authorization for ASP.NET  
**Functionality:** Validates user roles before method execution

**Source:** `NET 4.5/Aspectacular.Framework.Web.45/AspNetRoleAuthorizationAspect.cs`

#### AspNetTraceAspect

```
Aspect
  └── AspNetTraceAspect
```

**Purpose:** ASP.NET request tracing integration  
**Functionality:** Writes interception logs to ASP.NET trace

**Source:** `NET 4.5/Aspectacular.Framework.Web.45/AspNetTraceAspect.cs`

### Web Utility Classes

#### RequestCache

**Purpose:** Per-request caching utilities using HttpContext.Items  
**Source:** `NET 4.5/Aspectacular.Framework.Web.45/RequestCache.cs`

#### SmartSession

**Purpose:** Enhanced ASP.NET session management helpers  
**Source:** `NET 4.5/Aspectacular.Framework.Web.45/SmartSession.cs`

#### QueryStringEx

**Purpose:** Query string parsing and manipulation  
**Source:** `NET 4.5/Aspectacular.Framework.Web.45/QueryStringEx.cs`

#### SimpleJsonSerializer

**Purpose:** Basic JSON serialization utilities  
**Source:** `NET 4.5/Aspectacular.Framework.Web.45/SimpleJsonSerializer.cs`

---

## Design Pattern Implementation Summary

### 1. Proxy Pattern
- **Implementation:** `Proxy` class acts as dynamic proxy
- **Purpose:** Intercept method calls transparently
- **Location:** AspectInjector.cs (Proxy class)

### 2. Decorator Pattern
- **Implementation:** Aspect classes wrap method execution
- **Purpose:** Add behavior without modifying original code
- **Location:** Aspect.cs and all aspect implementations

### 3. Chain of Responsibility
- **Implementation:** `List<IAspect> aspects` processed sequentially
- **Purpose:** Multiple aspects handle different concerns
- **Location:** Proxy.CallAspects() and CallAspectsBackwards()

### 4. Template Method Pattern
- **Implementation:** 7-stage aspect lifecycle in IAspect interface
- **Purpose:** Define algorithm structure, let subclasses fill steps
- **Location:** IAspect interface + Aspect base class

### 5. Strategy Pattern
- **Implementation:** Different aspect implementations (CacheAspect, RetryAspect, etc.)
- **Purpose:** Interchangeable cross-cutting concern strategies
- **Location:** All Aspect subclasses

### 6. Factory Pattern
- **Implementation:** Dynamic proxy creation via Func<object> instanceResolverFunc
- **Purpose:** On-demand proxy instantiation
- **Location:** Proxy constructor and ResolveClassInstance()

### 7. Lazy Initialization
- **Implementation:** `Lazy<T>` for parameter value evaluation
- **Purpose:** Defer expensive reflection operations
- **Location:** InterceptedMethodParamMetadata.slowEvaluatingValueLoader

---

## Type Count Summary

| Category | Count | Notes |
|----------|-------|-------|
| **Core Classes** | 45+ | Utility classes in Core module |
| **Main Classes** | 22+ | AOP engine classes in Main module |
| **Aspect Implementations** | 10+ | Built-in aspect types |
| **Interfaces** | 5+ | Core contracts (IAspect, IInterceptionContext, etc.) |
| **Attributes** | 4+ | Metadata attributes (InvariantReturn, SecretParamValue, RequiredAspect) |
| **Enumerations** | 4+ | EntryType, ParamValueOutputOptions, etc. |
| **Extension Method Classes** | 10+ | Static classes with extension methods |

**Total Types:** ~100+ public types across all modules

---

## Thread Safety Considerations

### ThreadLocal Storage
```csharp
private static readonly ThreadLocal<Stack<Proxy>> proxyStack = 
    new ThreadLocal<Stack<Proxy>>(() => new Stack<Proxy>());
```

**Purpose:** Thread-safe call context tracking  
**Pattern:** Each thread maintains its own proxy stack  
**Implications:** Nested/recursive interceptions supported per thread  
**Location:** AspectInjector.cs (Proxy class, line 24)

### Volatile Fields
```csharp
private volatile bool isUsed;
```

**Purpose:** Ensure single-use proxy instances across threads  
**Pattern:** Prevents proxy reuse with memory barrier  
**Location:** AspectInjector.cs (Proxy class)

---

## Generic Type Usage

### Generic Methods in Proxy

```csharp
public TOut Invoke<TOut>(Expression<Func<TOut>> interceptedCallExpression, 
    Expression<Func<TOut, object>> retValPostProcessorExpression = null)
```

**Purpose:** Type-safe interception with return values  
**Generic Constraint:** None (works with any return type)

### Generic Methods in Extensions

```csharp
public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
public static IEnumerable<T> SmartUnion<T>(this IEnumerable<T> first, IEnumerable<T> second)
```

**Purpose:** Type-safe collection operations  
**Location:** CollectionExtensions.cs

---

## Cross-Module Dependencies

```
Core (no dependencies)
  ↑
Main (depends on Core)
  ↑
  ├─→ EntityFramework (depends on Main, Core, EF 5.0)
  ├─→ Web (depends on Main, Core, System.Web)
  └─→ Azure (depends on Main, Core, Azure SDK)
```

---

## Key Observations

1. **Separation of Concerns:** Clear module boundaries (Core utilities, Main engine, specialized extensions)
2. **Extensibility:** Abstract Aspect base class allows custom aspect creation
3. **Type Safety:** Heavy use of generics for compile-time safety
4. **Metadata-Rich:** Comprehensive reflection and metadata capture
5. **Performance-Aware:** Lazy evaluation patterns for expensive operations
6. **Thread-Safe:** ThreadLocal storage for call context isolation
7. **Attribute-Driven:** Extensive use of attributes for declarative configuration

---

**Document Version:** 1.0  
**Last Updated:** 2024  
**Total Types Documented:** 100+  
**Source Files Analyzed:** 60+ files across Core and Main modules
