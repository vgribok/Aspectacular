# Data Models - Core Data Structures

## Overview

This document details the key data structures and models in the Aspectacular AOP Framework. These classes represent the core data types used for interception metadata, call logging, and aspect execution context.

---

## Core Data Models

### Proxy Class (Primary Interception Context)

**Purpose:** Central data model representing an intercepted method call  
**Location:** `NET 4.5/Aspectacular.Framework.Main.45/AspectInjector.cs` (Proxy class)  
**Base Class:** `CallLifetimeLog`  
**Implements:** `IMethodLogProvider`

#### Key Properties

```csharp
public class Proxy : CallLifetimeLog, IMethodLogProvider
{
    // === Identity and State ===
    
    /// <summary>Unique identifier for this method call</summary>
    public readonly Guid CallID;
    
    /// <summary>Instance being intercepted (null for static methods)</summary>
    public object AugmentedClassInstance { get; protected set; }
    
    /// <summary>Number of execution attempts (starts at 1)</summary>
    public byte AttemptsMade { get; private set; }
    
    /// <summary>Indicates if method was actually called (vs cached)</summary>
    public bool MethodWasCalled { get; protected set; }
    
    // === Method Execution Results ===
    
    /// <summary>Value returned by intercepted method (if successful)</summary>
    public object ReturnedValue { get; internal set; }
    
    /// <summary>Exception thrown by method or aspects (if failed)</summary>
    public Exception MethodExecutionException { get; set; }
    
    /// <summary>True if method threw exception or aspects failed</summary>
    public bool InterceptedMedthodCallFailed { get; }
    
    // === Metadata ===
    
    /// <summary>Complete intercepted method metadata</summary>
    public InterceptedMethodMetadata InterceptedCallMetaData { get; protected set; }
    
    /// <summary>Post-processing method metadata (optional)</summary>
    public InterceptedMethodMetadata PostProcessingCallMetadata { get; set; }
    
    // === Aspect Control Flags ===
    
    /// <summary>Set by aspects to trigger retry of failed call</summary>
    public bool ShouldRetryCall { get; set; }
    
    /// <summary>Set by caching aspects to bypass method execution</summary>
    public bool CancelInterceptedMethodCall { get; internal set; }
    
    /// <summary>Set to stop calling remaining aspects in chain</summary>
    public bool StopAspectCallChain { get; set; }
    
    /// <summary>Force method to be treated as cacheable</summary>
    public bool ForceCallInvariance { get; set; }
    
    // === Computed Properties ===
    
    /// <summary>True if method can be cached (instance-invariant)</summary>
    public bool CanCacheReturnedResult { get; }
    
    /// <summary>Static property to access current proxy on call stack</summary>
    public static IMethodLogProvider CurrentLog { get; }
}
```

#### Internal Data Structures

```csharp
// Thread-safe call stack (ThreadLocal)
private static readonly ThreadLocal<Stack<Proxy>> proxyStack;

// Aspect collection
protected readonly List<IAspect> aspects = new List<IAspect>();

// Instance resolution
private readonly Func<object> instanceResolverFunc;
private readonly Action<object> instanceCleanerFunc;

// Single-use enforcement
private volatile bool isUsed;

// Intercepted method delegate
protected Delegate interceptedMethod;
```

**Memory Footprint:** ~200-500 bytes base + aspects + metadata  
**Lifetime:** Created per method call, disposed after Step_7  
**Thread Safety:** ThreadLocal stack ensures thread isolation

---

### InterceptedMethodMetadata Class

**Purpose:** Comprehensive reflection metadata for intercepted methods  
**Location:** `NET 4.5/Aspectacular.Framework.Main.45/InterceptedMethodMetadata.cs`

#### Structure

```csharp
public class InterceptedMethodMetadata
{
    // === Core Reflection Data ===
    
    /// <summary>Raw MethodInfo from reflection</summary>
    public MethodInfo MethodReflectionInfo { get; private set; }
    
    /// <summary>Method's declaring class type</summary>
    public Type ClassType { get; }
    
    /// <summary>Method return type</summary>
    public Type MethodReturnType { get; }
    
    /// <summary>True for static methods</summary>
    public bool IsStaticMethod { get; }
    
    // === Parameter Metadata ===
    
    /// <summary>Collection of all parameter metadata</summary>
    public readonly List<InterceptedMethodParamMetadata> Params;
    
    // === Caching Metadata ===
    
    /// <summary>True if method returns same data for different instances</summary>
    public bool IsReturnResultInvariant { get; }
    
    /// <summary>True if return value contains sensitive data</summary>
    public bool IsReturnValueSecret { get; }
    
    /// <summary>True if method has out or ref parameters</summary>
    public bool HasOutOrRefParams { get; }
    
    // === Attributes ===
    
    /// <summary>All method attributes</summary>
    public IEnumerable<Attribute> MethodAttributes { get; }
}
```

#### Key Methods

```csharp
// Signature Formatting
string GetMethodSignature(ParamValueOutputOptions valueOutputOptions = NoValue)

// Attribute Access (with inheritance)
TAttribute GetMethodAttribute<TAttribute>(bool inherit = false)
IEnumerable<TAttribute> GetMethodAttributes<TAttribute>(bool inherit = false)
bool HasMethodAttribute<TAttribute>(bool inherit = false)

TAttribute GetClassAttribute<TAttribute>(bool inherit = true)
IEnumerable<TAttribute> GetClassAttributes<TAttribute>(bool inherit = true)
bool HasClassAttribute<TAttribute>(bool inherit = true)

// Combined attribute lookup (method first, then class)
TAttribute GetMethodOrClassAttribute<TAttribute>()
IEnumerable<TAttribute> GetMethodOrClassAttributes<TAttribute>()
bool HasMethodOrClassAttribute<TAttribute>()

// Value Formatting
string FormatReturnResult(object returnedResult, bool trueUi_FalseInternal)

// Slow Evaluation
object SlowEvaluateThisObject()  // Compiles and invokes 'this' expression
```

**Example Metadata:**
```
Method Signature: Customer Example.Dal.CustomerService.GetCustomerById(int customerId)
Class Type: Example.Dal.CustomerService
Return Type: Customer
Is Static: false
Parameters: 1 (int customerId)
Is Call-Invariant: true (marked with [InvariantReturn])
```

---

### InterceptedMethodParamMetadata Class

**Purpose:** Individual parameter metadata with lazy value evaluation  
**Location:** `NET 4.5/Aspectacular.Framework.Main.45/InterceptedMethodMetadata.cs`

#### Structure

```csharp
public class InterceptedMethodParamMetadata
{
    // === Core Parameter Data ===
    
    /// <summary>Raw parameter reflection data</summary>
    public readonly ParameterInfo ParamReflection;
    
    /// <summary>Parameter name</summary>
    public string Name { get; private set; }
    
    /// <summary>Parameter type</summary>
    public Type Type { get; private set; }
    
    /// <summary>Parameter direction (In/Out/RefInOut)</summary>
    public ParamDirectionEnum Direction { get; }
    
    // === Value Evaluation ===
    
    /// <summary>
    /// Lazy parameter value loader (SLOW - uses Expression.Compile() and reflection)
    /// Warning: Evaluates expression, may have side effects
    /// </summary>
    public object SlowEvaluatingValueLoader { get; }
    
    /// <summary>True if parameter cannot be evaluated</summary>
    public readonly bool CannotBeEvaluated;
    
    /// <summary>True if parameter has [SecretParamValue] attribute</summary>
    public bool ValueIsSecret { get; }
    
    // Internal expression tree storage
    private readonly Expression expression;
    private readonly Lazy<object> slowEvaluatingValueLoader;
    private readonly Lazy<ParamDirectionEnum> paramDirection;
}
```

#### Key Methods

```csharp
// Attribute Access
T GetCustomAttribute<T>(bool inherit = false) where T : Attribute
IEnumerable<T> GetCustomAttributes<T>(bool inherit = false) where T : Attribute
bool HasAttribute<T>() where T : Attribute

// Value Formatting
string FormatSlowEvaluatingValue(bool trueUi_FalseInternal)
string ToString(ParamValueOutputOptions options)

// Static utility for parameter evaluation
static object VerySlowlyCompileAndInvoke(object augmentedObject, Expression expression)
static string FormatParamValue(Type type, object val, bool trueUi_FalseInternal)
```

**Performance Warning:**  
Accessing `SlowEvaluatingValueLoader` first time per parameter:
- Compiles expression tree (~10-100ms per unique expression)
- Invokes via reflection (~0.1-1ms per call)
- Result is cached in Lazy<> after first evaluation

**Example Parameter Metadata:**
```
Parameter: int customerId = 42
Type: System.Int32
Direction: In
Value Is Secret: false
Can Evaluate: true
```

---

### CallLifetimeLog Class (Base Logging Infrastructure)

**Purpose:** Base class providing call logging functionality  
**Location:** `NET 4.5/Aspectacular.Framework.Main.45/CallLog.cs`  
**Inherited By:** `Proxy`

#### Structure

```csharp
public class CallLifetimeLog
{
    // === Log Storage ===
    
    /// <summary>Collection of all log entries for this call</summary>
    protected readonly List<CallLogEntry> CallLog;
    
    // === Computed Properties ===
    
    /// <summary>Worst severity level in log (Error > Warning > Info)</summary>
    public EntryType? WorstEntryType { get; }
    
    /// <summary>Bitwise combination of all entry types present</summary>
    public EntryType? PresentEntryTypes { get; }
}
```

#### Key Methods

```csharp
// Log Entry Addition
void AddLogEntry(object logger, EntryType entryType, string optionalKey, 
                 string format, params object[] args)

// Log Text Generation
string GetLogText(Func<List<CallLogEntry>, IEnumerable<string>> entrySelector = null)
string GetLogText(string lineSeparator, 
                  Func<List<CallLogEntry>, IEnumerable<string>> entrySelector = null)

// Convenience Logging (protected, for aspects)
void LogInformation(string format, params object[] args)
void LogInformationWithKey(string optionalKey, string format, params object[] args)
void LogInformationData(string optionalKey, object data)
void LogError(string format, params object[] args)
void LogErrorWithKey(string optionalKey, string format, params object[] args)
void LogWarning(string format, params object[] args)
void LogWarningWithKey(string optionalKey, string format, params object[] args)
```

---

### CallLogEntry Class

**Purpose:** Individual log entry record  
**Location:** `NET 4.5/Aspectacular.Framework.Main.45/CallLog.cs`

#### Structure

```csharp
public class CallLogEntry
{
    /// <summary>When entry was created</summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>Entry severity (Info/Warning/Error)</summary>
    public EntryType Type { get; set; }
    
    /// <summary>Optional categorization key</summary>
    public string Key { get; set; }
    
    /// <summary>Log message text</summary>
    public string Message { get; set; }
    
    /// <summary>Logger object (typically an aspect instance)</summary>
    public object Logger { get; set; }
}
```

#### ToString() Format

```
[2024-01-15 14:32:45.123] [Info] [OptionalKey] Message text
```

**Example Log Entries:**
```
[2024-01-15 14:32:45.100] [Info] [Call ID] 3a4b5c6d-7e8f-9g0h-1i2j-3k4l5m6n7o8p
[2024-01-15 14:32:45.101] [Info] [Method signature] Customer GetCustomerById(int customerId = 42)
[2024-01-15 14:32:45.150] [Info] [Stopwatch] Method execution: 48ms
[2024-01-15 14:32:45.151] [Info] [Call outcome] success
```

---

## Attribute Data Models

### InvariantReturnAttribute

**Purpose:** Marks methods/classes as having cacheable return values  
**Location:** `NET 4.5/Aspectacular.Framework.Main.45/InterceptedMethodMetadata.cs` (lines 46-60)

```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method, 
    AllowMultiple = false)]
public class InvariantReturnAttribute : Attribute
{
    /// <summary>
    /// True if method returns same data when called on different instances
    /// at the same time (instance-invariant)
    /// </summary>
    public bool IsInstanceInvariant { get; private set; }
    
    public InvariantReturnAttribute(bool isInstanceInvariant = true)
    {
        this.IsInstanceInvariant = isInstanceInvariant;
    }
}
```

**Usage Examples:**
```csharp
// Entire class has cacheable methods
[InvariantReturn(true)]
public class ProductCatalog
{
    // All methods cacheable by default
    public Product GetProduct(int id) { ... }
    
    // Opt-out specific method
    [InvariantReturn(false)]
    public DateTime GetServerTime() { ... }
}

// Individual method caching
public class MixedService
{
    [InvariantReturn(true)]
    public static List<Category> GetAllCategories() { ... }  // Cacheable
    
    public UserProfile GetCurrentUser() { ... }  // Not cacheable (no attribute)
}
```

---

### SecretParamValueAttribute

**Purpose:** Marks parameters/return values as containing sensitive data  
**Location:** `NET 4.5/Aspectacular.Framework.Main.45/InterceptedMethodMetadata.cs` (lines 18-24)

```csharp
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue, 
    AllowMultiple = false)]
public class SecretParamValueAttribute : Attribute
{
    // No properties - presence of attribute is the flag
}
```

**Usage Examples:**
```csharp
public class AuthService
{
    // Password parameter won't be logged in plain text
    public bool Login(string username, 
        [SecretParamValue] string password)
    {
        // Aspects will log password as hash instead of actual value
    }
    
    // Secret return value
    [return: SecretParamValue]
    public string GetApiKey(int userId)
    {
        // Return value won't be logged in plain text
    }
}
```

**Effect:** Values replaced with `SecretValueHash` in logs (shows hash instead of value)

---

### RequiredAspectAttribute

**Purpose:** Declares required aspects for methods/classes  
**Location:** `NET 4.5/Aspectacular.Framework.Main.45/Aspects/RequiredAspectAttribute.cs`

```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequiredAspectAttribute : Attribute
{
    /// <summary>Type of aspect that must be present</summary>
    public Type AspectClassType { get; private set; }
    
    /// <summary>Behavior when aspect is missing</summary>
    public WhenRequiredAspectIsMissing InstantiateIfMissing { get; private set; }
    
    public RequiredAspectAttribute(
        Type aspectType, 
        WhenRequiredAspectIsMissing whenMissing = WhenRequiredAspectIsMissing.DontInstantiate)
    {
        this.AspectClassType = aspectType;
        this.InstantiateIfMissing = whenMissing;
    }
    
    /// <summary>Creates aspect instance if needed</summary>
    public Aspect InstantiateAspect()
    {
        return (Aspect)Activator.CreateInstance(this.AspectClassType);
    }
}
```

**Usage Examples:**
```csharp
// Require transaction aspect - throw if missing
[RequiredAspect(typeof(DtcTransactionAspect), 
    WhenRequiredAspectIsMissing.DontInstantiate)]
public void TransferFunds(int from, int to, decimal amount)
{
    // Aspect must be explicitly added by caller
}

// Auto-instantiate stopwatch if missing (add to end)
[RequiredAspect(typeof(StopwatchAspect), 
    WhenRequiredAspectIsMissing.InstantiateAndAppend)]
public DataSet GetLargeReport()
{
    // Stopwatch automatically added if not present
}

// Auto-instantiate auth aspect first
[RequiredAspect(typeof(ClaimsAuthorizationAspect), 
    WhenRequiredAspectIsMissing.InstantiateAndPrepend)]
public void DeleteUser(int userId)
{
    // Authorization checked first, even if caller forgot to add aspect
}
```

---

## Helper Data Models

### SecretValueHash (Internal)

**Purpose:** Hash representation of secret values for logging  
**Location:** `NET 4.5/Aspectacular.Framework.Main.45/InterceptedMethodMetadata.cs` (lines 90-121)

```csharp
internal class SecretValueHash
{
    internal readonly string valHash;
    
    public SecretValueHash(object val)
    {
        if (val == null)
            this.valHash = null;
        else
        {
            // Combines GetHashCode() and CRC of Unicode bytes
            byte[] stringBytes = Encoding.Unicode.GetBytes(val.ToString());
            long crc = ComputeCRC(stringBytes);
            this.valHash = string.Format("{0:X}-{1:X}", val.GetHashCode(), crc);
        }
    }
    
    public override string ToString() => this.valHash;
    
    // CRC-16 computation for additional hash uniqueness
    public static long ComputeCRC(byte[] val) { ... }
}
```

**Example Hashes:**
```
Input: "myPassword123"
Hash: "7A3B9C2E-4F8D1A6C"  (not actual password)

Input: null
Hash: null

Input: "admin"
Hash: "2C5E8F1A-9D3B7E4C"
```

---

### PostProcessingMethodMetadata

**Purpose:** Metadata for optional return value post-processing methods  
**Location:** `NET 4.5/Aspectacular.Framework.Main.45/InterceptedMethodMetadata.cs` (lines 690-703)

```csharp
public class PostProcessingMethodMetadata : InterceptedMethodMetadata
{
    public PostProcessingMethodMetadata(
        object methodInstance, 
        LambdaExpression callLambdaExp, 
        bool forceClassInstanceInvariant)
        : base(methodInstance, callLambdaExp, forceClassInstanceInvariant, 
               checkInstanceVsStatic: false)
    {
    }
    
    // Overrides parameter evaluation to skip first parameter
    // (which is the not-yet-available IQueryable/IEnumerable)
    protected override bool CanEvaluateParamValue(int index, 
        ParameterInfo paramInfo, Expression paramExpression)
    {
        return index != 0;  // First param is return value from main method
    }
}
```

**Usage Context:**
```csharp
// Main method returns IQueryable
proxy.Invoke(
    () => dbContext.Customers.Where(c => c.Active),
    // Post-processor called after main method
    result => result.ToList()  // Executes query
);
// PostProcessingMethodMetadata captures ToList() metadata
```

---

## Enumeration Data Models

### EntryType (Log Severity)

```csharp
[Flags]
public enum EntryType
{
    Info = 1,      // 0001
    Warning = 2,   // 0010
    Error = 4      // 0100
}
```

**Bitwise Operations:**
```csharp
// Check if errors present
if ((entryTypes & EntryType.Error) == EntryType.Error) { ... }

// Combine types
EntryType combined = EntryType.Info | EntryType.Warning;  // 0011 = 3

// Check worst type
EntryType worst = log.WorstEntryType;  // Returns highest severity
```

---

### ParamValueOutputOptions

```csharp
public enum ParamValueOutputOptions
{
    NoValue,            // Don't evaluate/show parameter values
    SlowUIValue,        // Evaluate for user display (very slow)
    SlowInternalValue   // Evaluate for caching (very slow)
}
```

**Performance Impact:**
- `NoValue`: Instant (no evaluation)
- `SlowUIValue`: 10-100ms per unique parameter (first time)
- `SlowInternalValue`: 10-100ms per unique parameter (first time)

---

### ParamDirectionEnum

```csharp
public enum ParamDirectionEnum
{
    In,        // Input parameter (default)
    Out,       // Output parameter (out keyword)
    RefInOut   // Reference parameter (ref keyword)
}
```

**Detection Logic:**
```csharp
if (paramInfo.IsOut) return ParamDirectionEnum.Out;
if (paramInfo.ParameterType.IsByRef) return ParamDirectionEnum.RefInOut;
return ParamDirectionEnum.In;
```

---

### WhenRequiredAspectIsMissing

```csharp
public enum WhenRequiredAspectIsMissing
{
    DontInstantiate,        // Throw exception if missing
    InstantiateAndPrepend,  // Auto-create and add first
    InstantiateAndAppend    // Auto-create and add last
}
```

---

## Data Model Relationships

```
Proxy
  ├── Contains: InterceptedMethodMetadata
  │     └── Contains: List<InterceptedMethodParamMetadata>
  │           └── Contains: Expression (for lazy evaluation)
  │
  ├── Contains: PostProcessingMethodMetadata (optional)
  │
  ├── Contains: List<IAspect> aspects
  │
  ├── Inherits: CallLifetimeLog
  │     └── Contains: List<CallLogEntry>
  │
  └── Uses: SecretValueHash (for sensitive data)
```

---

## Memory and Performance Considerations

### Proxy Instance
- **Creation:** One per intercepted method call
- **Lifetime:** Entire call duration (Step 1 through Step 7)
- **Memory:** ~200-500 bytes + aspects + metadata + log entries
- **Thread Safety:** ThreadLocal<Stack<Proxy>> ensures isolation

### InterceptedMethodMetadata
- **Creation:** Once per Proxy
- **Lazy Evaluation:** Parameter values evaluated on-demand
- **Caching:** Expression compilation results cached in Lazy<>
- **Memory:** ~100-300 bytes + parameter metadata

### CallLog
- **Growth:** Grows with each log entry
- **Typical Size:** 10-50 entries per call
- **Entry Size:** ~100-200 bytes per entry
- **Total:** ~1-10 KB per call for logging

---

## Summary

**Core Data Models:** 6 primary classes (Proxy, InterceptedMethodMetadata, InterceptedMethodParamMetadata, CallLifetimeLog, CallLogEntry, PostProcessingMethodMetadata)

**Attributes:** 3 (InvariantReturn, SecretParamValue, RequiredAspect)

**Enumerations:** 4 (EntryType, ParamValueOutputOptions, ParamDirectionEnum, WhenRequiredAspectIsMissing)

**Helper Classes:** 1 (SecretValueHash)

All data models are designed for:
- **Rich Metadata:** Comprehensive reflection information
- **Lazy Evaluation:** Expensive operations deferred
- **Type Safety:** Strong typing throughout
- **Thread Safety:** ThreadLocal isolation where needed
- **Performance:** Balanced between features and speed

---

**Document Version:** 1.0  
**Last Updated:** 2024  
**Data Models Documented:** 10+ core structures  
**Source Analysis:** Complete data model extraction from Main module
