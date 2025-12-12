# API Reference - Quick Reference Guide

## Overview

This document provides a quick reference for the Aspectacular AOP Framework's public API. For detailed information, see [Program Structure](program-structure.md), [Interfaces](interfaces.md), and [Data Models](data-models.md).

---

## Core API Entry Points

### Method Interception API

#### Static Method Interception

```csharp
// Invoke static method with aspects
using Aspectacular;

var result = AOP.Invoke(
    () => SomeClass.StaticMethod(param1, param2),
    new StopwatchAspect(),
    new CacheAspect()
);

// Invoke void static method
AOP.Invoke(
    () => SomeClass.VoidStaticMethod(),
    new LoggingAspect()
);
```

**Source:** `NET 4.5/Aspectacular.Framework.Main.45/AspectInjector.cs` (extension methods)

#### Instance Method Interception

```csharp
// Get proxy for instance
var instance = new MyService();
var result = instance.GetProxy(new StopwatchAspect(), new CacheAspect())
    .Invoke(x => x.GetData(param));

// Alternative syntax
var proxy = instance.GetProxy(aspects);
var result = proxy.Invoke(x => x.GetData(param));

// With return value post-processing
var result = instance.GetProxy(aspects)
    .Invoke(
        x => x.GetQueryable(param),
        queryResult => queryResult.ToList()  // Execute query
    );
```

**Source:** `NET 4.5/Aspectacular.Framework.Main.45/InstanceInterceptor.cs`

---

## Aspect Base Class API

### Aspect Class

**Namespace:** `Aspectacular`  
**Assembly:** `Aspectacular.Framework.Main`  
**Base Class:** `System.Object`  
**Implements:** `IAspect`

#### Lifecycle Methods (Override as Needed)

```csharp
public abstract class Aspect : IAspect
{
    // Access to interception context
    public virtual Proxy Proxy { get; set; }
    
    // 7-stage lifecycle hooks
    public virtual void Step_1_BeforeResolvingInstance() { }
    public virtual void Step_2_BeforeTryingMethodExec() { }
    public virtual void Step_3_BeforeMassagingReturnedResult() { }
    public virtual void Step_4_Optional_AfterSuccessfulCallCompletion() { }
    public virtual void Step_4_Optional_AfterCatchingMethodExecException() { }
    public virtual void Step_5_FinallyAfterMethodExecution(bool success) { }
    public virtual void Step_6_Optional_AfterInstanceCleanup() { }
    public virtual void Step_7_AfterEverythingSaidAndDone() { }
}
```

#### Protected Utility Methods

```csharp
// Cancel method execution and supply cached result
protected void CancelInterceptedMethodCallAndSetReturnValue(object newReturnValue)

// Logging methods
protected void Log(EntryType type, string key, string format, params object[] args)
protected void LogInformation(string format, params object[] args)
protected void LogInformationWithKey(string key, string format, params object[] args)
protected void LogInformationData(string key, object data)
protected void LogError(string format, params object[] args)
protected void LogErrorWithKey(string key, string format, params object[] args)
protected void LogWarning(string format, params object[] args)
protected void LogWarningWithKey(string key, string format, params object[] args)

// Log retrieval
public string GetLogText(Func<List<CallLogEntry>, IEnumerable<string>> selector = null)
public EntryType? WorstEntryType { get; }
public EntryType? PresentEntryTypes { get; }
```

#### Static Members

```csharp
// Global default aspects
public static IEnumerable<Aspect> DefaultAspects { get; }
public static Func<IEnumerable<Aspect>> DefaultAspectFactory { get; set; }

// Deprecated - use Threading.ApplicationExiting instead
[Obsolete]
public static ManualResetEvent ApplicationExiting;
```

**Example Custom Aspect:**

```csharp
public class MyAspect : Aspect
{
    public override void Step_2_BeforeTryingMethodExec()
    {
        this.LogInformation("Method starting: {0}", 
            this.Proxy.InterceptedCallMetaData.GetMethodSignature());
    }
    
    public override void Step_5_FinallyAfterMethodExecution(bool success)
    {
        this.LogInformation("Method completed: {0}", success ? "Success" : "Failed");
    }
}
```

---

## Built-In Aspects API

### StopwatchAspect

**Purpose:** Automatic performance timing  
**Source:** `NET 4.5/Aspectacular.Framework.Main.45/Aspects/StopwatchAspect.cs`

```csharp
var result = obj.GetProxy(new StopwatchAspect())
    .Invoke(x => x.ExpensiveOperation());

// Logs timing automatically:
// "Method execution: 125ms"
// "Total call duration: 127ms"
```

### RetryAspect / DefaultRetryAspect

**Purpose:** Automatic retry on transient failures  
**Source:** `NET 4.5/Aspectacular.Framework.Main.45/Aspects/RetryAspects.cs`

```csharp
// Retry up to 3 times with 100ms delay
var retry = new DefaultRetryAspect(maxAttempts: 3, delayMillisec: 100);
var result = obj.GetProxy(retry).Invoke(x => x.UnreliableMethod());

// Custom retry logic
public class CustomRetryAspect : RetryAspect
{
    protected override bool ShouldRetry(Exception ex)
    {
        return ex is SqlException || ex is TimeoutException;
    }
}
```

### CacheAspect / CacheAspectBase

**Purpose:** Result caching  
**Source:** `NET 4.5/Aspectacular.Framework.Main.45/Aspects/CacheAspect.cs`

```csharp
// Method must be marked [InvariantReturn]
[InvariantReturn(true)]
public DataSet GetReport(int year)
{
    // Expensive operation
}

// Use caching aspect
var cache = new MyCacheAspect();
var result = obj.GetProxy(cache).Invoke(x => x.GetReport(2023));
// Second call with same parameters returns cached result
```

**Note:** Requires custom implementation of cache storage (memory, Redis, etc.)

### ClaimsAuthorizationAspect

**Purpose:** Claims-based security  
**Source:** `NET 4.5/Aspectacular.Framework.Main.45/Aspects/ClaimsAuthorizationAspect.cs`

```csharp
public class MyAuthAspect : ClaimsAuthorizationAspect
{
    protected override ClaimsPrincipal CurrentPrincipal => Thread.CurrentPrincipal;
    
    protected override bool IsAuthorized(string requiredClaim)
    {
        return CurrentPrincipal.HasClaim(requiredClaim);
    }
}

// Apply to methods requiring authorization
[RequiredAspect(typeof(MyAuthAspect))]
public void DeleteUser(int userId) { }
```

### DtcTransactionAspect

**Purpose:** Distributed Transaction Coordinator integration  
**Source:** `NET 4.5/Aspectacular.Framework.Main.45/Aspects/DtcTransactionAspect.cs`

```csharp
var transaction = new DtcTransactionAspect();
obj.GetProxy(transaction).Invoke(x => x.TransactionalOperation());
// Automatically commits on success, rolls back on exception
```

### LinqToSqlAspect

**Purpose:** LINQ to SQL interception  
**Source:** `NET 4.5/Aspectacular.Framework.Main.45/Aspects/LinqToSqlAspect.cs`

### SqlConnectionAttributesAspect

**Purpose:** SQL connection string manipulation  
**Source:** `NET 4.5/Aspectacular.Framework.Main.45/Aspects/SqlConnectionAttributesAspect.cs`

### SqlExecutionPlanAspect

**Purpose:** Capture SQL query execution plans  
**Source:** `NET 4.5/Aspectacular.Framework.Main.45/Aspects/SqlExecutionPlanAspect.cs`

---

## DAL Interception API

### DalInterceptor Static Helper

**Source:** `NET 4.5/Aspectacular.Framework.Main.45/DalInterceptor.cs`

```csharp
public static class DalInterceptor
{
    // Invoke function with aspects
    public static TOut Invoke<TOut>(
        Func<TOut> func, 
        params Aspect[] aspects)
    
    // Invoke action with aspects
    public static void Invoke(
        Action action, 
        params Aspect[] aspects)
}
```

**Example:**

```csharp
public class CustomerRepository
{
    public Customer GetById(int id)
    {
        return DalInterceptor.Invoke(
            () => _dbContext.Customers.Find(id),
            new StopwatchAspect(),
            new CacheAspect()
        );
    }
}
```

---

## Entity Framework API

### EfInterceptor

**Source:** `NET 4.5/Aspectacular.Framework.EntityFramework.45/EfInterceptor.cs`

```csharp
using(var ctx = new MyDbContext())
{
    var customers = ctx.GetDbProxy(new LazyLoadSwitch(), new StopwatchAspect())
        .Invoke(x => x.Customers.Where(c => c.Active).ToList());
}
```

### LazyLoadSwitch Aspect

**Purpose:** Control EF lazy loading during method execution

```csharp
var lazyLoad = new LazyLoadSwitch(enableLazyLoading: false);
var result = dbContext.GetDbProxy(lazyLoad)
    .Invoke(x => x.GetCustomers());
// Lazy loading disabled during this call
```

---

## ASP.NET Web API

### AspNetRoleAuthorizationAspect

**Source:** `NET 4.5/Aspectacular.Framework.Web.45/AspNetRoleAuthorizationAspect.cs`

```csharp
public class AdminAuthAspect : AspNetRoleAuthorizationAspect
{
    public AdminAuthAspect() : base("Admin") { }
}

// Usage
[RequiredAspect(typeof(AdminAuthAspect))]
public void DeleteAllUsers() { }
```

### AspNetTraceAspect

**Purpose:** Write logs to ASP.NET trace output

```csharp
var result = service.GetProxy(new AspNetTraceAspect())
    .Invoke(x => x.ProcessRequest());
// Logs appear in ASP.NET trace viewer
```

### RequestCache

**Purpose:** Per-request caching using HttpContext.Items

**Source:** `NET 4.5/Aspectacular.Framework.Web.45/RequestCache.cs`

---

## Proxy Class API

### Key Properties

```csharp
public class Proxy
{
    // Identity
    public readonly Guid CallID;
    
    // Execution state
    public object AugmentedClassInstance { get; }
    public object ReturnedValue { get; }
    public Exception MethodExecutionException { get; }
    public bool MethodWasCalled { get; }
    public bool InterceptedMedthodCallFailed { get; }
    public byte AttemptsMade { get; }
    
    // Metadata
    public InterceptedMethodMetadata InterceptedCallMetaData { get; }
    public PostProcessingMethodMetadata PostProcessingCallMetadata { get; }
    
    // Control flags (set by aspects)
    public bool ShouldRetryCall { get; set; }
    public bool CancelInterceptedMethodCall { get; set; }
    public bool StopAspectCallChain { get; set; }
    public bool ForceCallInvariance { get; set; }
    
    // Computed properties
    public bool CanCacheReturnedResult { get; }
    
    // Static access (for static methods only)
    public static IMethodLogProvider CurrentLog { get; }
}
```

### Key Methods

```csharp
// Execute intercepted method
public TOut Invoke<TOut>(
    Expression<Func<TOut>> interceptedCallExpression,
    Expression<Func<TOut, object>> retValPostProcessorExpression = null)

public void Invoke(Expression<Action> interceptedCallExpression)

// Caching support
public object SlowGetReturnValueForCaching(out string cacheKey)

// Return value formatting
public string FormateReturnValue(bool trueUi_FalseInternal)
```

---

## Metadata API

### InterceptedMethodMetadata

```csharp
public class InterceptedMethodMetadata
{
    // Core properties
    public MethodInfo MethodReflectionInfo { get; }
    public Type ClassType { get; }
    public Type MethodReturnType { get; }
    public bool IsStaticMethod { get; }
    public List<InterceptedMethodParamMetadata> Params { get; }
    
    // Caching metadata
    public bool IsReturnResultInvariant { get; }
    public bool IsReturnValueSecret { get; }
    public bool HasOutOrRefParams { get; }
    
    // Key methods
    public string GetMethodSignature(ParamValueOutputOptions options = NoValue)
    
    // Attribute access
    public TAttribute GetMethodAttribute<TAttribute>(bool inherit = false)
    public TAttribute GetClassAttribute<TAttribute>(bool inherit = true)
    public TAttribute GetMethodOrClassAttribute<TAttribute>()
    
    // Has attribute checks
    public bool HasMethodAttribute<TAttribute>(bool inherit = false)
    public bool HasClassAttribute<TAttribute>(bool inherit = true)
    public bool HasMethodOrClassAttribute<TAttribute>()
}
```

### InterceptedMethodParamMetadata

```csharp
public class InterceptedMethodParamMetadata
{
    // Core properties
    public readonly ParameterInfo ParamReflection;
    public string Name { get; }
    public Type Type { get; }
    public ParamDirectionEnum Direction { get; }
    public readonly bool CannotBeEvaluated;
    
    // Value evaluation (SLOW - use sparingly)
    public object SlowEvaluatingValueLoader { get; }
    public bool ValueIsSecret { get; }
    
    // Methods
    public T GetCustomAttribute<T>(bool inherit = false)
    public bool HasAttribute<T>()
    public string FormatSlowEvaluatingValue(bool trueUi_FalseInternal)
}
```

---

## Attribute API

### InvariantReturnAttribute

```csharp
// Mark method as cacheable
[InvariantReturn(true)]
public DataSet GetReport(int year) { }

// Mark entire class
[InvariantReturn(true)]
public class CacheableService
{
    public Data GetData() { }  // Cacheable
    
    [InvariantReturn(false)]   // Opt-out
    public DateTime GetTime() { }  // Not cacheable
}
```

### SecretParamValueAttribute

```csharp
// Mark sensitive parameter
public bool Login(
    string username, 
    [SecretParamValue] string password)
{ }

// Mark sensitive return value
[return: SecretParamValue]
public string GetApiKey() { }
```

### RequiredAspectAttribute

```csharp
// Require aspect - throw if missing
[RequiredAspect(typeof(TransactionAspect), 
    WhenRequiredAspectIsMissing.DontInstantiate)]
public void CriticalOperation() { }

// Auto-instantiate aspect
[RequiredAspect(typeof(StopwatchAspect), 
    WhenRequiredAspectIsMissing.InstantiateAndAppend)]
public void MonitoredOperation() { }
```

---

## Extension Methods API

### Core Utilities (Aspectacular.Framework.Core)

#### Collection Extensions

```csharp
// ForEach
collection.ForEach(item => Console.WriteLine(item));

// Smart union
var combined = list1.SmartUnion(list2);  // Handles nulls

// Null/empty check
if (collection.IsNullOrEmpty()) { }

// Reverse
var reversed = collection.ReverseOrder();
```

#### String Extensions

```csharp
// Safe format
string.SmartFormat("Hello {0}", name);  // No exception on format errors

// Safe ToString
object.ToStringEx();  // Handles nulls

// Safe comparison
str1.CompareToOrdinalIgnoreCase(str2);
```

#### Type Extensions

```csharp
// C# type formatting
typeof(List<int>).FormatCSharp();  // "List<int>"

// Simple type check
typeof(int).IsSimpleCSharpType();  // true
typeof(Customer).IsSimpleCSharpType();  // false

// Generic attribute access
type.GetCustomAttribute<MyAttribute>();
```

#### DateTime Extensions

```csharp
// Relative time spans
var timeAgo = new RelativeTimeSpan(DateTime.Now.AddDays(-2));
// "2 days ago"

// Date range check
if (date.IsInRange(startDate, endDate)) { }
```

---

## Configuration API

### Default Aspects

```csharp
// Set global default aspects (applied to all interceptions)
Aspect.DefaultAspectFactory = () => new[]
{
    new StopwatchAspect(),
    new LoggingAspect()
};

// Get current defaults
var defaults = Aspect.DefaultAspects;
```

### Config File Integration

```xml
<!-- app.config / web.config -->
<configuration>
  <configSections>
    <section name="defaultAspects" 
             type="Aspectacular.DefaultAspectsConfigSection, Aspectacular.Framework.Main"/>
  </configSections>
  
  <defaultAspects>
    <aspect type="MyNamespace.StopwatchAspect, MyAssembly"/>
  </defaultAspects>
</configuration>
```

---

## Common Usage Patterns

### 1. Simple Logging

```csharp
var result = obj.GetProxy(new LoggingAspect())
    .Invoke(x => x.DoWork());
```

### 2. Caching with Performance Monitoring

```csharp
[InvariantReturn(true)]
public DataSet GetReport(int year) { }

var result = repo.GetProxy(
    new CacheAspect(),
    new StopwatchAspect()
).Invoke(x => x.GetReport(2023));
```

### 3. Retry with Logging

```csharp
var result = service.GetProxy(
    new DefaultRetryAspect(maxAttempts: 3, delayMillisec: 100),
    new LoggingAspect()
).Invoke(x => x.UnreliableCall());
```

### 4. Transaction with Authorization

```csharp
[RequiredAspect(typeof(ClaimsAuthAspect))]
public void UpdateRecords()
{
    // Automatically wrapped in transaction + auth check
}

service.GetProxy(
    new ClaimsAuthAspect(),
    new DtcTransactionAspect()
).Invoke(x => x.UpdateRecords());
```

### 5. Database Access Pattern

```csharp
public Customer GetCustomer(int id)
{
    return DalInterceptor.Invoke(
        () => _context.Customers.Find(id),
        new StopwatchAspect(),
        new CacheAspect()
    );
}
```

---

## API Availability by Framework Version

| API Component | .NET 4.0 | .NET 4.5 | .NET Core 5.0 |
|--------------|----------|----------|---------------|
| Core AOP Engine | ✅ | ✅ | ✅ |
| All Aspects | ✅ | ✅ | ✅ |
| Entity Framework | ✅ (EF 5.0) | ✅ (EF 5.0) | ❌ |
| ASP.NET Web | ✅ | ✅ | ❌ |
| Azure Integration | ❌ | ✅ | ❌ |

---

## Performance Considerations

### Fast Operations
- Aspect instantiation: ~1-10 μs
- Proxy creation: ~10-100 μs
- Method signature formatting (no values): ~1-10 μs

### Slow Operations
- Parameter value evaluation (first time): 10-100 ms (Expression.Compile)
- Metadata reflection (first time): 1-10 ms
- Method signature with values: 10-100 ms

### Optimization Tips
1. Reuse aspect instances when possible
2. Avoid `ParamValueOutputOptions.Slow*` unless necessary
3. Use `[SecretParamValue]` sparingly (hashing overhead)
4. Consider caching for expensive operations
5. Default aspects applied to every call - keep lightweight

---

## Error Handling

### Common Exceptions

```csharp
// Proxy reuse error
throw new Exception("Same instance of the call interceptor cannot be used more than once.");

// Missing required aspect
throw new Exception("Aspect {0} is required by \"{1}\", but is not in the caller's aspect collection.");

// Invalid instance for static method
throw new Exception("Instance for AOP augmentation needs to be specified...");

// Cache call after method execution
throw new Exception("Invalid attempt to cancel intercepted method call after it was called.");
```

### Exception Handling in Aspects

```csharp
public class SafeAspect : Aspect
{
    public override void Step_4_Optional_AfterCatchingMethodExecException()
    {
        var ex = this.Proxy.MethodExecutionException;
        
        if (ex is SqlException sqlEx && IsTransient(sqlEx))
        {
            // Trigger retry
            this.Proxy.ShouldRetryCall = true;
        }
        else
        {
            // Log and rethrow
            this.LogError("Fatal error: {0}", ex.Message);
        }
    }
}
```

---

## Thread Safety

### ThreadLocal Call Stack
- Each thread maintains its own proxy stack
- Nested calls supported per thread
- No cross-thread context sharing

### Aspect Thread Safety
- Aspect instances can be shared across threads IF stateless
- Stateful aspects should be instantiated per call
- Use Proxy properties (thread-safe) instead of aspect instance fields

---

**Document Version:** 1.0  
**Last Updated:** 2024  
**API Coverage:** Complete public API for Main, Core, EntityFramework, and Web modules  
**Quick Reference:** See detailed docs for comprehensive information
