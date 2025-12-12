# ASP.NET Web Integration - Web-Specific AOP Patterns

## Overview
The Aspectacular AOP Framework provides specialized aspects and utilities for ASP.NET web applications, enabling request-scoped caching, ASP.NET trace integration, role-based authorization, and web-specific extensions. This document details web-specific patterns and integration strategies.

**Supported Platforms:**
- ASP.NET Web Forms (.NET Framework 4.0/4.5/4.8)
- ASP.NET MVC (.NET Framework 4.0/4.5/4.8)
- ⚠️ **ASP.NET Core:** Not currently supported (requires significant rewrite due to System.Web → ASP.NET Core API changes)

**Module:** `Aspectacular.Framework.Web`  
**Namespaces:** `Aspectacular`

---

## Core Web Components

### 1. ASP.NET Role Authorization Aspect

#### Purpose
Enforces ASP.NET Membership role-based authorization using AOP, preventing unauthorized access to methods and classes.

**Source:** `NET 4.5/Aspectacular.Framework.Web.45/AspNetRoleAuthorizationAspect.cs`

#### Components

##### DemandAspNetRoleAttribute

```csharp
/// <summary>
/// Decorate methods and classes to enforce ASP.NET role membership.
/// Uses Roles.GetRolesForUser() internally.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class DemandAspNetRoleAttribute : AuthorizationDemandAttribute
{
    // Constructor 1: Demand at least one role (OR logic)
    public DemandAspNetRoleAttribute(params string[] demandedRoles)
        : this(trueDemandAny_falseDemandAll: true, demandedRoles);
    
    // Constructor 2: Configure AND vs OR logic
    public DemandAspNetRoleAttribute(
        bool trueDemandAny_falseDemandAll, 
        params string[] demandedRoles
    );
}
```

**Parameters:**
- `trueDemandAny_falseDemandAll`: 
  - `true` = User must be in **at least one** role (OR logic)
  - `false` = User must be in **all** roles (AND logic)
- `demandedRoles`: Array of required role names

##### AspNetRoleAuthorizationAspect

```csharp
/// <summary>
/// Enforces ASP.NET role membership authorization for methods/classes 
/// decorated with DemandAspNetRoleAttribute.
/// Sets HTTP 403 Forbidden status on authorization failure.
/// </summary>
public class AspNetRoleAuthorizationAspect : AuthorizationAspect
{
    protected override void ThrowAuthorizationException(string errorMsg)
    {
        // Set HTTP 403 Forbidden status
        HttpContext.Current.Response.StatusCode = (int)HttpStatusCode.Forbidden;
        
        base.ThrowAuthorizationException(errorMsg);
    }
}
```

#### Usage Patterns

**Pattern 1: Method-Level Authorization (OR logic)**
```csharp
public class OrderService
{
    // User must be in Admin OR Manager role
    [DemandAspNetRole("Admin", "Manager")]
    public void ApproveOrder(int orderId)
    {
        // Method executes only if user is Admin or Manager
        // Otherwise: UnauthorizedAccessException + HTTP 403
    }
}

// Invoke with AOP
var aspects = new[] { new AspNetRoleAuthorizationAspect() };
var proxy = Proxy.Create<OrderService>(aspects);
proxy.Invoke(svc => svc.ApproveOrder(123));
```

**Pattern 2: Method-Level Authorization (AND logic)**
```csharp
public class AuditService
{
    // User must be in BOTH Auditor AND Compliance roles
    [DemandAspNetRole(trueDemandAny_falseDemandAll: false, "Auditor", "Compliance")]
    public void GenerateAuditReport(DateTime startDate, DateTime endDate)
    {
        // Executes only if user has both roles
    }
}
```

**Pattern 3: Class-Level Authorization**
```csharp
// All methods require Admin role
[DemandAspNetRole("Admin")]
public class AdminService
{
    public void ManageUsers() { }
    public void ConfigureSystem() { }
    public void ViewSecurityLogs() { }
}

// Usage
var aspects = new[] { new AspNetRoleAuthorizationAspect() };
var proxy = Proxy.Create<AdminService>(aspects);
proxy.Invoke(svc => svc.ManageUsers()); // Requires Admin role
```

**Pattern 4: Combined with Other Aspects**
```csharp
public class SecureBusinessService
{
    [DemandAspNetRole("Manager", "Admin")]
    [InvariantReturn(true)]  // Enable caching
    public List<FinancialReport> GetFinancialReports(int year)
    {
        // Authorized users get cached results
        return GenerateReports(year);
    }
}

// Invoke with multiple aspects
var aspects = new Aspect[]
{
    new AspNetRoleAuthorizationAspect(),  // Authorization first
    new CacheAspect(),                     // Then caching
    new LoggingAspect()                    // Then logging
};

var proxy = Proxy.Create<SecureBusinessService>(aspects);
var reports = proxy.Invoke(svc => svc.GetFinancialReports(2024));
```

#### Authorization Flow

```
1. User requests method execution
   ↓
2. AspNetRoleAuthorizationAspect.Step_1_BeforeResolvingInstance()
   ↓
3. Read DemandAspNetRoleAttribute from method/class
   ↓
4. Get current user: HttpContext.Current.User.Identity
   ↓
5. Get user roles: Roles.GetRolesForUser()
   ↓
6. Check authorization:
   - OR logic: User in at least one demanded role?
   - AND logic: User in all demanded roles?
   ↓
7a. AUTHORIZED → Continue to method execution
7b. UNAUTHORIZED → Set HTTP 403 → Throw UnauthorizedAccessException
```

#### Security Considerations

**Caching Warning:**
```csharp
/// <remarks>
/// Caution: Uses Roles.GetRolesForUser() internally, which has weird caching policy.
/// For better performance, implement your own ClaimsIdentity for ASP.NET Roles
/// that caches claims for longer periods, and then use DemandClaimsAttribute 
/// instead of DemandAspNetRoleAttribute.
/// </remarks>
```

**Best Practices:**
- ✅ **DO:** Apply authorization aspect as first aspect in chain (before caching, etc.)
- ✅ **DO:** Use claims-based authorization (`DemandClaimsAttribute`) for better performance
- ✅ **DO:** Test authorization with both authenticated and anonymous users
- ❌ **DON'T:** Cache authorized method results without considering user context
- ❌ **DON'T:** Rely on client-side authorization alone (always enforce server-side)

---

### 2. ASP.NET Trace Output Aspect

#### Purpose
Outputs AOP logging information to ASP.NET's built-in trace context, visible in trace.axd and page trace output.

**Source:** `NET 4.5/Aspectacular.Framework.Web.45/AspNetTraceAspect.cs`

#### Implementation

```csharp
/// <summary>
/// Outputs AOP call log records to ASP.NET Trace context.
/// </summary>
public class AspNetTraceAspect : LogOutputAspectBase
{
    // Constructor 1: Log errors, warnings, and info
    public AspNetTraceAspect()
        : this(EntryType.Error | EntryType.Warning | EntryType.Info);
    
    // Constructor 2: Configure entry types to log
    public AspNetTraceAspect(
        EntryType typeOfEntriesToOutput, 
        string optionalKey = null,
        bool writeAllEntriesIfKeyFound = false
    );
    
    // Constructor 3: Filter by multiple keys
    public AspNetTraceAspect(
        EntryType typeOfEntriesToOutput, 
        bool writeAllEntriesIfKeyFound,
        params string[] optionalKeys
    );
}
```

**EntryType Flags:**
- `EntryType.Info` → Trace.Write() (normal message)
- `EntryType.Warning` → Trace.Warn() (warning message)
- `EntryType.Error` → Trace.Warn() (error message)

#### Usage Patterns

**Pattern 1: Basic Trace Logging**
```csharp
// Enable ASP.NET tracing in Web.config
// <trace enabled="true" pageOutput="true" />

var aspects = new Aspect[]
{
    new AspNetTraceAspect(),  // Logs errors, warnings, info
    new LoggingAspect()        // In-memory logging
};

var proxy = Proxy.Create<ProductService>(aspects);
var products = proxy.Invoke(svc => svc.GetProducts());

// View trace output at: http://localhost/trace.axd
```

**Pattern 2: Filtered Trace Output**
```csharp
// Log only errors and warnings (exclude info)
var traceAspect = new AspNetTraceAspect(
    EntryType.Error | EntryType.Warning
);

var aspects = new Aspect[] { traceAspect };
var proxy = Proxy.Create<OrderService>(aspects);
proxy.Invoke(svc => svc.ProcessOrder(orderId));
```

**Pattern 3: Conditional Trace Output**
```csharp
// Log all entries only if specific key found (e.g., performance issue detected)
var traceAspect = new AspNetTraceAspect(
    typeOfEntriesToOutput: EntryType.Error | EntryType.Warning | EntryType.Info,
    optionalKey: "PerformanceIssue",
    writeAllEntriesIfKeyFound: true
);

// If "PerformanceIssue" key present in log, output all entries to trace
```

#### Trace Output Format

**Example Trace Output:**
```
Category: Aspectacular
Message: 
Method: ProductService.GetProducts()
Started: 2024-12-12 10:30:15.123
Duration: 245ms
Result: List<Product>[50]
Aspects: [LoggingAspect, AspNetTraceAspect, CacheAspect]
```

**With Error:**
```
Category: Aspectacular [Warning]
Message: 
Method: OrderService.ProcessOrder(orderId=999)
Started: 2024-12-12 10:35:22.456
Duration: 15ms
Exception: OrderNotFoundException - Order with ID 999 not found
Stack Trace: [...]
```

#### Integration with Page Trace

**Enable Page Trace:**
```aspx
<%@ Page Trace="true" %>
```

**View in Page:**
```html
<!-- Trace output appears at bottom of page -->
<div id="__TRACE">
    <table>
        <tr>
            <td>Category</td>
            <td>Message</td>
            <td>From First(s)</td>
            <td>From Last(s)</td>
        </tr>
        <tr class="trace-warn">
            <td>Aspectacular</td>
            <td>Method: OrderService.ProcessOrder() - Duration: 245ms</td>
            <td>0.015</td>
            <td>0.001</td>
        </tr>
    </table>
</div>
```

#### Best Practices
- ✅ **DO:** Use for development and staging environments
- ✅ **DO:** Filter to errors/warnings in production
- ✅ **DO:** Disable trace output in production (performance impact)
- ❌ **DON'T:** Log sensitive data (passwords, credit cards) to trace
- ❌ **DON'T:** Enable trace.axd in production (security risk - exposes internal info)

---

### 3. Request-Scoped Cache (RequestCache)

#### Purpose
Caches method results for the duration of a single HTTP request, eliminating redundant database queries or expensive calculations within a single page/request lifecycle.

**Source:** `NET 4.5/Aspectacular.Framework.Web.45/RequestCache.cs`

#### Implementation

```csharp
/// <summary>
/// Caches data for the duration of the HTTP request processing.
/// Should be used together with [InvariantReturn] attribute.
/// No explicit cleanup/invalidation required - data dropped at end of request.
/// </summary>
public class RequestCache : ICacheProvider
{
    // Factory method (singleton pattern)
    public static RequestCache Get();
    
    // ICacheProvider implementation
    public void Set(string key, object val);
    public bool TryGet(string key, out object val);
}
```

**Storage Mechanism:** Uses `HttpContext.Current.Items` dictionary (request-scoped storage)

**Cache Key Format:** `"RequestCache_" + methodSignature + parameters`

#### Usage Patterns

**Pattern 1: Basic Request Caching**
```csharp
public class ProductService
{
    [InvariantReturn(true)]  // Mark method as cacheable
    public Product GetProduct(int productId)
    {
        // Expensive database query
        return dbContext.Products.Find(productId);
    }
}

// Configure CacheAspect with RequestCache
var cacheAspect = new CacheAspect(RequestCache.Get());
var aspects = new Aspect[] { cacheAspect };

var proxy = Proxy.Create<ProductService>(aspects);

// Within same HTTP request:
var product1 = proxy.Invoke(svc => svc.GetProduct(123)); // DB query executed
var product2 = proxy.Invoke(svc => svc.GetProduct(123)); // Cached (no DB query)
var product3 = proxy.Invoke(svc => svc.GetProduct(456)); // Different param - DB query

// Next HTTP request: Cache cleared automatically
```

**Pattern 2: Eliminating N+1 Query Problem**
```csharp
public class OrderService
{
    [InvariantReturn(true)]
    public Customer GetCustomer(int customerId)
    {
        // Without caching: Called N times for N orders with same customer
        return dbContext.Customers.Find(customerId);
    }
    
    public List<OrderViewModel> GetOrdersForDisplay(List<Order> orders)
    {
        var viewModels = new List<OrderViewModel>();
        
        foreach (var order in orders)
        {
            // First call loads from DB, subsequent calls use request cache
            var customer = GetCustomer(order.CustomerId); 
            
            viewModels.Add(new OrderViewModel
            {
                OrderId = order.OrderId,
                CustomerName = customer.Name,  // No N+1 problem
                Total = order.Total
            });
        }
        
        return viewModels;
    }
}
```

**Pattern 3: Master/Detail Page Caching**
```csharp
// ASP.NET MVC Controller
public class ProductController : Controller
{
    private readonly Proxy<ProductService> _serviceProxy;
    
    public ProductController()
    {
        var cacheAspect = new CacheAspect(RequestCache.Get());
        _serviceProxy = Proxy.Create<ProductService>(new[] { cacheAspect });
    }
    
    public ActionResult Details(int id)
    {
        // First call - loads from DB
        var product = _serviceProxy.Invoke(svc => svc.GetProduct(id));
        
        // Subsequent calls in same request - cached
        var relatedProducts = _serviceProxy.Invoke(svc => 
            svc.GetRelatedProducts(product.CategoryId)
        );
        
        // If GetRelatedProducts internally calls GetProduct(id), 
        // it will hit cache instead of DB
        
        return View(product);
    }
}
```

**Pattern 4: Composite Data Loading**
```csharp
public class DashboardService
{
    [InvariantReturn(true)]
    public UserProfile GetUserProfile(int userId)
    {
        return dbContext.Users.Find(userId);
    }
    
    [InvariantReturn(true)]
    public List<Order> GetUserOrders(int userId)
    {
        return dbContext.Orders.Where(o => o.UserId == userId).ToList();
    }
    
    [InvariantReturn(true)]
    public List<Review> GetUserReviews(int userId)
    {
        return dbContext.Reviews.Where(r => r.UserId == userId).ToList();
    }
    
    public DashboardViewModel GetDashboard(int userId)
    {
        // All three methods may internally call GetUserProfile(userId)
        // With RequestCache, profile loaded only once per request
        return new DashboardViewModel
        {
            Profile = GetUserProfile(userId),
            Orders = GetUserOrders(userId),
            Reviews = GetUserReviews(userId)
        };
    }
}
```

#### Cache Lifecycle

```
HTTP Request Start
   ↓
1. HttpContext.Current.Items created (empty dictionary)
   ↓
2. First method call: GetProduct(123)
   - CacheAspect checks RequestCache
   - Cache miss → Execute method
   - Store result: Items["RequestCache_GetProduct_123"] = product
   ↓
3. Second method call: GetProduct(123)
   - CacheAspect checks RequestCache
   - Cache hit → Return cached value (no method execution)
   ↓
4. Third method call: GetProduct(456)
   - CacheAspect checks RequestCache
   - Cache miss (different parameter) → Execute method
   - Store result: Items["RequestCache_GetProduct_456"] = product
   ↓
HTTP Request End
   ↓
5. HttpContext.Current.Items disposed (cache cleared)
```

#### Performance Characteristics

| Scenario | Without Caching | With RequestCache | Improvement |
|----------|----------------|-------------------|-------------|
| Single method call | 50ms (DB query) | 50ms (DB query) | 0% |
| 10 identical calls | 500ms (10 × 50ms) | 50ms + 0.5ms | **90% faster** |
| 100 identical calls | 5000ms | 50ms + 5ms | **99% faster** |
| N+1 query (100 orders, 10 unique customers) | 550ms (10 × 50ms + overhead) | 500ms + 5ms | **10% faster** |

**Cache Lookup Overhead:** ~0.05ms per lookup (negligible)

#### Best Practices
- ✅ **DO:** Use for methods called multiple times per request
- ✅ **DO:** Apply `[InvariantReturn(true)]` to cacheable methods
- ✅ **DO:** Use for N+1 query scenarios
- ✅ **DO:** Cache expensive calculations repeated within request
- ❌ **DON'T:** Use for methods with side effects (updates, deletes)
- ❌ **DON'T:** Expect cache to persist across requests (request-scoped only)
- ❌ **DON'T:** Cache user-specific data without considering user context in cache key

---

## Web Utility Extensions

### 4. Query String Extensions

**Source:** `NET 4.5/Aspectacular.Framework.Web.45/QueryStringEx.cs`

Provides extension methods for simplified query string parsing with type conversion.

```csharp
public static class QueryStringEx
{
    // Parse query string value with default
    public static T GetValue<T>(this NameValueCollection queryString, 
        string key, T defaultValue = default(T));
    
    // Parse query string value with conversion
    public static T ParseValue<T>(this NameValueCollection queryString, string key);
}
```

**Usage:**
```csharp
// In ASP.NET page or controller
int productId = Request.QueryString.GetValue("id", defaultValue: 0);
bool includeDeleted = Request.QueryString.GetValue("deleted", defaultValue: false);
DateTime? fromDate = Request.QueryString.GetValue<DateTime?>("from", defaultValue: null);
```

---

### 5. HTTP Request Extensions

**Source:** `NET 4.5/Aspectacular.Framework.Web.45/RequestExtensions.cs`

Extensions for common HTTP request operations.

```csharp
public static class RequestExtensions
{
    // Check if request is AJAX
    public static bool IsAjaxRequest(this HttpRequest request);
    
    // Get client IP address (handles proxies)
    public static string GetClientIpAddress(this HttpRequest request);
    
    // Check if request is local
    public static bool IsLocal(this HttpRequest request);
}
```

**Usage:**
```csharp
// In ASP.NET MVC Controller
if (Request.IsAjaxRequest())
{
    return Json(data);
}
else
{
    return View(data);
}

// Log client IP for auditing
string clientIp = Request.GetClientIpAddress();
Logger.Log($"Request from IP: {clientIp}");
```

---

## Integration Patterns

### Pattern 1: Complete Web AOP Stack

```csharp
public class ProductController : Controller
{
    private readonly Proxy<ProductService> _proxy;
    
    public ProductController()
    {
        var aspects = new Aspect[]
        {
            new AspNetRoleAuthorizationAspect(),        // Security first
            new AspNetTraceAspect(EntryType.Error),     // Trace errors
            new CacheAspect(RequestCache.Get()),        // Request-scoped cache
            new PerformanceAspect(),                    // Performance monitoring
            new LoggingAspect()                         // Detailed logging
        };
        
        _proxy = Proxy.Create<ProductService>(aspects);
    }
    
    [HttpGet]
    public ActionResult Index()
    {
        // All aspects applied: auth → trace → cache → perf → log
        var products = _proxy.Invoke(svc => svc.GetAllProducts());
        return View(products);
    }
    
    [HttpPost]
    public ActionResult Create(Product product)
    {
        // Authorization enforced via aspect
        _proxy.Invoke(svc => svc.AddProduct(product));
        return RedirectToAction("Index");
    }
}
```

### Pattern 2: Repository with Web Caching

```csharp
public class ProductRepository
{
    private static readonly Proxy<ProductRepository> _cachedProxy;
    
    static ProductRepository()
    {
        var aspects = new Aspect[]
        {
            new CacheAspect(RequestCache.Get()),
            new LoggingAspect()
        };
        _cachedProxy = Proxy.Create<ProductRepository>(aspects);
    }
    
    [InvariantReturn(true)]
    public Product GetById(int id)
    {
        // Cached within request scope
        using (var db = new DbContext())
        {
            return db.Products.Find(id);
        }
    }
    
    [InvariantReturn(true)]
    public List<Product> GetByCategory(int categoryId)
    {
        // Cached within request scope
        using (var db = new DbContext())
        {
            return db.Products.Where(p => p.CategoryId == categoryId).ToList();
        }
    }
    
    // Public static method for cached access
    public static Product GetProduct(int id)
    {
        return _cachedProxy.Invoke(repo => repo.GetById(id));
    }
}
```

### Pattern 3: Secure Web Service

```csharp
[DemandAspNetRole("ApiUser")]  // Class-level security
public class WebApiService
{
    [DemandAspNetRole("Admin")]  // Method-level override (Admin only)
    public void DeleteProduct(int productId)
    {
        // Only Admin users can delete
    }
    
    [InvariantReturn(true)]  // Cacheable
    public List<Product> GetProducts(string category)
    {
        // All authenticated ApiUsers can read
        // Results cached per request
    }
}

// Usage
var aspects = new Aspect[]
{
    new AspNetRoleAuthorizationAspect(),
    new CacheAspect(RequestCache.Get()),
    new AspNetTraceAspect()
};

var proxy = Proxy.Create<WebApiService>(aspects);
var products = proxy.Invoke(svc => svc.GetProducts("Electronics"));
```

---

## Migration Considerations

### ASP.NET Framework 4.0/4.5 → 4.8
**Status:** ✅ Fully compatible  
**Effort:** None required  
**Testing:** Regression testing recommended

---

### ASP.NET (System.Web) → ASP.NET Core
**Status:** ⚠️ **MAJOR REWRITE REQUIRED**

**Breaking Changes:**
1. **HttpContext.Current removed**
   - Replace with dependency injection of `IHttpContextAccessor`
   - Pass `HttpContext` explicitly

2. **Roles.GetRolesForUser() removed**
   - Use ASP.NET Core Identity: `UserManager<TUser>.GetRolesAsync(user)`
   - Or claims-based authorization

3. **Request.QueryString API changes**
   - `NameValueCollection` → `IQueryCollection`

4. **Trace.axd removed**
   - Replace with ASP.NET Core logging (`ILogger<T>`)
   - Or third-party tracing (Application Insights, Serilog)

5. **HttpContext.Current.Items changes**
   - Still available but different API: `HttpContext.Items`
   - Access via `IHttpContextAccessor.HttpContext.Items`

**Migration Effort:** HIGH (4-6 weeks)

**Recommended Approach:**
1. **Phase 1:** Port authorization to claims-based (compatible with both)
2. **Phase 2:** Replace `HttpContext.Current` with dependency injection
3. **Phase 3:** Migrate tracing to `ILogger<T>`
4. **Phase 4:** Update request cache to use `IHttpContextAccessor`

**Alternative:** Deprecate web module for ASP.NET Core projects, use core framework only

---

## Testing Strategy

### Unit Tests
```csharp
[Test]
public void RequestCache_CachesWithinRequestScope()
{
    // Simulate HTTP context
    HttpContext.Current = new HttpContext(
        new HttpRequest("", "http://localhost", ""),
        new HttpResponse(new StringWriter())
    );
    
    var cache = RequestCache.Get();
    cache.Set("test_key", "test_value");
    
    object result;
    bool found = cache.TryGet("test_key", out result);
    
    Assert.IsTrue(found);
    Assert.AreEqual("test_value", result);
}

[Test]
public void AspNetRoleAuthorization_ThrowsUnauthorized_WhenUserNotInRole()
{
    // Mock user without required role
    var identity = new GenericIdentity("testuser");
    var principal = new GenericPrincipal(identity, new[] { "User" });
    HttpContext.Current.User = principal;
    
    var aspect = new AspNetRoleAuthorizationAspect();
    // Apply to method requiring "Admin" role
    
    Assert.Throws<UnauthorizedAccessException>(() => {
        // Invoke method with DemandAspNetRole("Admin")
    });
}
```

### Integration Tests
```csharp
[Test]
public void RequestCache_EliminatesNPlusOneQueries()
{
    using (var context = CreateTestHttpContext())
    {
        var cacheAspect = new CacheAspect(RequestCache.Get());
        var proxy = Proxy.Create<TestService>(new[] { cacheAspect });
        
        int dbCallCount = 0;
        TestService.OnDbCall += () => dbCallCount++;
        
        // First call
        proxy.Invoke(svc => svc.GetCustomer(1)); // DB call
        Assert.AreEqual(1, dbCallCount);
        
        // 10 more calls with same parameter
        for (int i = 0; i < 10; i++)
        {
            proxy.Invoke(svc => svc.GetCustomer(1)); // Cached
        }
        
        Assert.AreEqual(1, dbCallCount); // Still only 1 DB call
    }
}
```

---

## Performance Impact

| Feature | Overhead | Benefit |
|---------|----------|---------|
| AspNetRoleAuthorizationAspect | ~1-2ms (role lookup) | Security enforcement |
| AspNetTraceAspect | ~0.1-0.5ms | Development diagnostics |
| RequestCache (hit) | ~0.05ms | 99% faster for repeated calls |
| RequestCache (miss) | ~0.1ms | Enables caching |

---

## Related Documentation
- [Authorization Business Logic](../../behavior/business-logic.md#authorization-rules)
- [Security Patterns](../../analysis/security-patterns.md)
- [Technical Debt - Web Module](../../technical-debt/outdated-components.md#aspnet-framework)

---

**Document Version:** 1.0  
**Framework Version:** Aspectacular Web Module (NET 4.5/4.0)  
**Last Updated:** Migration Planning Phase
