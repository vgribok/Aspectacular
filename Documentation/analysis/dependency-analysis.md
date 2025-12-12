# Dependency Analysis - Complete Dependency Mapping

## Overview

This document provides comprehensive dependency analysis for the Aspectacular AOP Framework, covering internal module dependencies, external package dependencies, BCL (Base Class Library) dependencies, and cross-component relationships.

**Analysis Method:** Static analysis of project files, package configurations, and using statements  
**Source Files Analyzed:** 21 .csproj files, 11 packages.config files, 153 C# source files

---

## Internal Module Dependencies

### Dependency Hierarchy

```
Aspectacular.Framework.Core (Base Layer - No Dependencies)
          ↑
          |
Aspectacular.Framework.Main (Core AOP Engine - Depends on Core)
          ↑
          |
    ┌─────┼─────┬─────────┬──────────┐
    |     |     |         |          |
   EF    Web  Azure  Examples    Tests
```

### Detailed Internal Dependencies

#### Level 1: Core Module (Foundation)
**Module:** `Aspectacular.Framework.Core`  
**Internal Dependencies:** None  
**Purpose:** Foundation utilities - LINQ, collections, reflection, threading  
**Criticality:** CRITICAL - All other modules depend on Core  
**Files:** 20+ utility classes  
**Migration Priority:** 1 (must migrate first)

#### Level 2: Main Module (AOP Engine)
**Module:** `Aspectacular.Framework.Main`  
**Internal Dependencies:**  
- `Aspectacular.Framework.Core` (Required)

**Purpose:** Core AOP interception engine, Proxy, Aspect lifecycle  
**Criticality:** CRITICAL - Foundation for all specialized modules  
**Files:** 22+ core AOP classes  
**Migration Priority:** 2 (migrate after Core)

#### Level 3: Specialized Modules (Extensions)

##### Entity Framework Module
**Module:** `Aspectacular.Framework.EntityFramework`  
**Internal Dependencies:**  
- `Aspectacular.Framework.Main` (Required)  
- `Aspectacular.Framework.Core` (Transitive through Main)

**Purpose:** Entity Framework-specific interception and ORM integration  
**Criticality:** HIGH - Required for database applications using EF  
**Files:** 3 files (EfInterceptor, LazyLoadSwitch, IDbEntityKey)  
**Migration Priority:** 3a (can migrate in parallel with Web/Azure)

##### Web Module
**Module:** `Aspectacular.Framework.Web`  
**Internal Dependencies:**  
- `Aspectacular.Framework.Main` (Required)  
- `Aspectacular.Framework.Core` (Transitive through Main)

**Purpose:** ASP.NET-specific aspects and web utilities  
**Criticality:** HIGH - Required for web applications  
**Files:** 6 files (AspNetRoleAuthorizationAspect, AspNetTraceAspect, RequestCache, etc.)  
**Migration Priority:** 3b (can migrate in parallel with EF/Azure)

##### Azure Module
**Module:** `Aspectacular.Framework.Azure`  
**Internal Dependencies:**  
- `Aspectacular.Framework.Main` (Required)  
- `Aspectacular.Framework.Core` (Transitive through Main)

**Purpose:** Azure cloud service integration  
**Criticality:** MEDIUM - Optional for non-Azure deployments  
**Files:** Azure-specific aspects  
**Migration Priority:** 3c (can migrate in parallel with EF/Web)

##### Example Projects
**Modules:** `Example.AdventureWorks2008_Dal.*`  
**Internal Dependencies:**  
- `Aspectacular.Framework.EntityFramework` (Required)  
- `Aspectacular.Framework.Main` (Transitive)  
- `Aspectacular.Framework.Core` (Transitive)

**Purpose:** Reference implementations and usage examples  
**Criticality:** LOW - Documentation/example code  
**Files:** 70+ entity classes, DAL implementations  
**Migration Priority:** 4 (migrate last, for testing)

##### Unit Test Projects
**Modules:** `Aspectacular.Framework.UnitTests.*`, `Aspectacular.Framework.AzureUnitTests.*`  
**Internal Dependencies:**  
- Various framework modules (depending on what's being tested)

**Purpose:** Automated testing  
**Criticality:** HIGH for validation  
**Migration Priority:** 5 (migrate after corresponding modules)

---

## External Package Dependencies

### Entity Framework 5.0 ⚠️ CRITICAL TECHNICAL DEBT

**Package:** `EntityFramework 5.0.0`  
**Used In:** 6 projects (.NET 4.0 and .NET 4.5)  
**Current Version:** 5.0.0 (Released: August 2012)  
**Latest Version:** Entity Framework 6.4.4 (.NET Framework) OR EF Core 8.x (.NET Core+)  
**Status:** ⚠️ **SEVERELY OUTDATED** (12+ years old)

**Impact Assessment:**
- **Severity:** CRITICAL
- **Security Risk:** HIGH (no security patches since 2012)
- **Functional Risk:** MEDIUM (works but missing modern features)
- **Performance:** MEDIUM (lacks EF Core performance improvements)

**Migration Path:**
1. **Short-term:** Upgrade to Entity Framework 6.4.4 (last .NET Framework version)
2. **Long-term:** Migrate to EF Core 8.x with .NET 6/7/8

**Affected Files:**
- `NET 4.0/Aspectacular.Framework.EntityFramework.40/packages.config`
- `NET 4.0/Example.AdventureWorks2008_Dal.40/packages.config`
- `NET 4.0/Aspectacular.Framework.UnitTests.40/packages.config`
- `NET 4.5/Aspectacular.Framework.EntityFramework.45/packages.config`
- `NET 4.5/Example.AdventureWorks2008_Dal.45/packages.config`
- `NET 4.5/Aspectacular.Framework.UnitTests.45/packages.config`

### Azure SDK Dependencies ⚠️ OUTDATED

**Packages (Azure Module):**
- `WindowsAzure.Storage 4.3.0` (Current: 9.x, superseded by Azure.Storage.Blobs 12.x)
- `Microsoft.Data.OData 5.6.3` (Deprecated)
- `Microsoft.Data.Edm 5.6.3` (Deprecated)
- `Microsoft.Data.Services.Client 5.6.3` (Deprecated)
- `Microsoft.WindowsAzure.ConfigurationManager 2.0.3` (Old version)
- `Newtonsoft.Json 5.0.8` ⚠️ (Current: 13.x, major security updates)
- `System.Spatial 5.6.3`
- `Unofficial.Microsoft.WindowsAzure.ServiceRuntime 2.5.0.0` (Unofficial package - risky)

**Status:** ⚠️ **OUTDATED** with security concerns  
**Used In:** `.NET 4.5/Aspectacular.Framework.Azure.45/`, `AzureUnitTests.45/`

**Impact Assessment:**
- **Severity:** HIGH
- **Security Risk:** HIGH (especially Newtonsoft.Json 5.0.8 - known vulnerabilities)
- **Compatibility:** MEDIUM (old Azure SDK v1/v2, current is v12+)

**Migration Path:**
1. Update `Newtonsoft.Json` to 13.x immediately (security fix)
2. Migrate from `WindowsAzure.Storage` to `Azure.Storage.Blobs 12.x`
3. Replace deprecated OData packages with modern equivalents
4. Replace unofficial ServiceRuntime package with official Microsoft package

### .NET Core Beta Packages ⚠️ BETA/PRE-RELEASE

**Packages (.NET Core 5.0 projects):**
- `System.* 4.0.*-beta-22816` (15+ packages)

**Status:** ⚠️ **BETA/PRE-RELEASE** - Not production-ready  
**Used In:** `.NET Core/Aspectacular.Framework.Core.50Core/`, `Main.50Core/`

**Impact Assessment:**
- **Severity:** CRITICAL
- **Stability Risk:** HIGH (beta packages, may have breaking changes)
- **Support:** NONE (beta packages are unsupported)

**Migration Path:**
1. Abandon .NET Core 5.0 (never released, was pre-release)
2. Migrate to .NET 6 LTS or .NET 8 LTS with stable packages

---

## BCL (Base Class Library) Dependencies

### .NET Framework 4.0/4.5 Dependencies

#### System.Core
**Usage:** LINQ, LINQ Expression Trees  
**Criticality:** CRITICAL  
**Classes Using:** All Core utilities, Main interception engine  
**Migration:** Standard in all .NET versions

#### System.Data
**Usage:** ADO.NET database access  
**Criticality:** HIGH  
**Classes Using:** AdoNetExtensions, DalInterceptor, SqlConnectionAttributesAspect  
**Migration:** Available in .NET Core+ as System.Data.Common

#### System.Data.Entity
**Usage:** Entity Framework support  
**Criticality:** HIGH (for EF projects)  
**Classes Using:** EfInterceptor, LazyLoadSwitch  
**Migration:** Replace with Microsoft.EntityFrameworkCore in .NET Core+

#### System.Transactions
**Usage:** Distributed Transaction Coordinator (DTC)  
**Criticality:** MEDIUM  
**Classes Using:** DtcTransactionAspect  
**Migration:** Available in .NET Core 2.0+ (limited support for distributed transactions)

#### System.Runtime.Caching
**Usage:** In-memory caching infrastructure  
**Criticality:** MEDIUM  
**Classes Using:** CacheAspect, Caching utilities  
**Migration:** Use Microsoft.Extensions.Caching.Memory in .NET Core+

#### System.Configuration
**Usage:** app.config / web.config reading  
**Criticality:** MEDIUM  
**Classes Using:** DefaultAspectsConfigSection, configuration utilities  
**Migration:** Replace with Microsoft.Extensions.Configuration in .NET Core+

#### System.Net
**Usage:** Network operations (for Azure module)  
**Criticality:** LOW  
**Migration:** Standard in all .NET versions

#### System.Web ⚠️ .NET FRAMEWORK ONLY
**Usage:** ASP.NET Classic support  
**Criticality:** HIGH (for Web module)  
**Classes Using:** All Web module classes (AspNetRoleAuthorizationAspect, AspNetTraceAspect, RequestCache, SmartSession, QueryStringEx)  
**Migration:** **NOT AVAILABLE in .NET Core+** - requires complete rewrite to ASP.NET Core  
**Impact:** Web module CANNOT be migrated without major refactoring

#### System.Xml / System.Xml.Linq
**Usage:** XML processing  
**Criticality:** LOW-MEDIUM  
**Classes Using:** XmlSerializationHelpers  
**Migration:** Standard in all .NET versions

---

## Dependency Graph (Visual)

### Module Dependency Graph

```
┌──────────────────────────────────────────────────────────────┐
│                     Application Code                         │
└────────────────┬─────────────────────────────────────────────┘
                 │ Uses
┌────────────────▼─────────────────────────────────────────────┐
│           Specialized Extension Modules                      │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐          │
│  │    EF       │  │    Web      │  │   Azure     │          │
│  │ Interceptor │  │   Aspects   │  │  Aspects    │          │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘          │
│         │                │                │                  │
│         └────────────────┼────────────────┘                  │
└──────────────────────────┼───────────────────────────────────┘
                           │ Depends on
┌──────────────────────────▼───────────────────────────────────┐
│            Aspectacular.Framework.Main                       │
│  ┌─────────────────────────────────────────────────────┐    │
│  │  Proxy, Aspect Lifecycle, Interception Engine       │    │
│  │  AspectInjector, CallLog, Metadata                  │    │
│  └──────────────────────┬───────────────────────────────┘    │
└─────────────────────────┼────────────────────────────────────┘
                          │ Depends on
┌─────────────────────────▼────────────────────────────────────┐
│            Aspectacular.Framework.Core                       │
│  ┌─────────────────────────────────────────────────────┐    │
│  │  Utilities: LINQ, Collections, Reflection           │    │
│  │  Extensions: String, Type, DateTime, ADO.NET        │    │
│  │  Threading, Caching, Query Modifiers                │    │
│  └─────────────────────────────────────────────────────┘    │
└──────────────────────────────────────────────────────────────┘
```

### External Dependency Graph

```
Aspectacular Framework
    │
    ├─── .NET Framework BCL (System.*, System.Data, System.Core, etc.)
    │
    ├─── Entity Framework 5.0 ⚠️ OUTDATED
    │      └─── Used by: EntityFramework module, Examples, Tests
    │
    ├─── Azure SDK 4.x ⚠️ OUTDATED
    │      ├─── WindowsAzure.Storage 4.3.0
    │      ├─── Microsoft.Data.OData 5.6.3 (deprecated)
    │      ├─── Newtonsoft.Json 5.0.8 ⚠️ SECURITY RISK
    │      └─── Unofficial.Microsoft.WindowsAzure.ServiceRuntime ⚠️ RISKY
    │
    └─── .NET Core Beta Packages ⚠️ UNSUPPORTED
           └─── System.* 4.0.*-beta-22816 (15+ packages)
```

---

## Data Flow Between Components

### Method Call Interception Flow

```
Application Method Call
         ↓
    Proxy Creation
         ↓
  Instance Resolution ────→ Core Utilities (reflection, threading)
         ↓
  Aspect Resolution ──────→ Main (DefaultAspects, RequiredAspects)
         ↓
   Aspect Chain Execution
    (Step 1 → Step 7)
         ↓
  Method Invocation ──────→ Actual Implementation
         ↓
  Return Value Processing → Core (LINQ modifiers, type extensions)
         ↓
   Logging & Cleanup ─────→ Main (CallLog, CallLifetimeLog)
         ↓
   Return to Application
```

### Specialized Module Integration Flow

```
Entity Framework:
  Application → EfInterceptor → Proxy → LazyLoadSwitch Aspect → DbContext Methods
                                   ↓
                          Entity Framework 5.0 ⚠️

ASP.NET Web:
  HTTP Request → AspNetTraceAspect → Proxy → Business Logic
                      ↓                  ↓
               System.Web ⚠️        RequestCache (HttpContext.Items)

Azure:
  Cloud Operation → Azure Aspect → Proxy → Cloud Service Call
                         ↓
              Azure Storage SDK 4.x ⚠️
```

---

## Using Statement Analysis

### Most Common Namespaces

**Core Module:**
- `System` (universal)
- `System.Collections.Generic` (collections)
- `System.Linq` (LINQ queries)
- `System.Reflection` (reflection/metadata)
- `System.Linq.Expressions` (expression trees)
- `System.Threading` (threading primitives)
- `System.Data` (ADO.NET)
- `System.Text` (string operations)

**Main Module:**
- `System`
- `System.Collections.Generic`
- `System.Linq`
- `System.Linq.Expressions`
- `System.Reflection`
- `System.Threading`
- `System.Diagnostics` (Debug.Assert, tracing)
- `System.Configuration` (app.config reading)
- `System.Runtime.Caching` (caching)
- `System.Transactions` (DTC)

**EntityFramework Module:**
- `System`
- `System.Data.Entity` ⚠️
- `Aspectacular` (Main module)

**Web Module:**
- `System`
- `System.Web` ⚠️ .NET Framework only
- `System.Web.SessionState`
- `Aspectacular` (Main module)

**Azure Module:**
- `System`
- `Microsoft.WindowsAzure.*` ⚠️ Old SDK
- `Aspectacular` (Main module)

---

## Threading Patterns and Dependencies

### ThreadLocal Usage

**Pattern:** `ThreadLocal<Stack<Proxy>>`  
**Location:** `Proxy` class (AspectInjector.cs)  
**Purpose:** Thread-safe call context isolation  
**Dependencies:**  
- `System.Threading.ThreadLocal<T>`
- `System.Collections.Generic.Stack<T>`

**Impact:**
- Each thread maintains independent proxy stack
- Nested interceptions supported per thread
- Memory implications: ThreadLocal allocates per thread
- Migration: Available in .NET Core+ (no changes needed)

### ManualResetEvent (Deprecated)

**Pattern:** `static ManualResetEvent ApplicationExiting` ⚠️  
**Location:** `Aspect` base class  
**Status:** Marked [Obsolete]  
**Dependencies:** `System.Threading.ManualResetEvent`

**Technical Debt:**
- Deprecated in framework itself
- Recommendation: Use `Threading.ApplicationExiting` instead
- Migration: Replace with modern cancellation patterns (CancellationToken)

---

## Dependency Criticality Matrix

| Dependency | Type | Version | Criticality | Security Risk | Migration Complexity |
|-----------|------|---------|-------------|---------------|---------------------|
| .NET Framework 4.0/4.5 | Runtime | EOL | CRITICAL | HIGH | HIGH (requires .NET 6/8) |
| Entity Framework 5.0 | Package | 5.0.0 | CRITICAL | HIGH | MEDIUM (upgrade to EF 6 or EF Core) |
| System.Web | BCL | .NET Fwk | HIGH | LOW | VERY HIGH (ASP.NET Core rewrite) |
| Azure Storage 4.3.0 | Package | 4.3.0 | MEDIUM | MEDIUM | MEDIUM (upgrade to v12) |
| Newtonsoft.Json 5.0.8 | Package | 5.0.8 | MEDIUM | HIGH | LOW (easy upgrade to 13.x) |
| Unofficial Azure Runtime | Package | 2.5.0.0 | MEDIUM | HIGH | MEDIUM (replace with official) |
| .NET Core Beta Packages | Package | Beta | CRITICAL | HIGH | HIGH (migrate to stable) |
| System.Transactions | BCL | .NET Fwk | MEDIUM | LOW | MEDIUM (limited Core+ support) |
| System.Configuration | BCL | .NET Fwk | LOW | LOW | LOW (use Extensions.Configuration) |
| System.Runtime.Caching | BCL | .NET Fwk | LOW | LOW | LOW (use Extensions.Caching) |

---

## Cross-Module Coupling Analysis

### Tight Coupling Points

1. **Main → Core**
   - **Coupling Level:** VERY HIGH
   - **Reason:** Main uses 40+ Core utility classes extensively
   - **Examples:** Extension methods (ForEach, SmartFormat, FormatCSharp), QueryModifiers, RelativeTimeSpan
   - **Impact:** Core changes ripple to Main immediately

2. **EntityFramework → Main**
   - **Coupling Level:** HIGH
   - **Reason:** EfInterceptor heavily uses Proxy and Aspect classes
   - **Examples:** Direct Proxy usage, Aspect inheritance
   - **Impact:** Main API changes require EF module updates

3. **Web → System.Web**
   - **Coupling Level:** VERY HIGH (Framework-locked)
   - **Reason:** Deep integration with ASP.NET Classic (HttpContext, SessionState, etc.)
   - **Examples:** HttpContext.Current, HttpSessionState, Trace
   - **Impact:** **CANNOT migrate to .NET Core without complete rewrite**

### Loose Coupling Points

1. **Azure → Main**
   - **Coupling Level:** MEDIUM
   - **Reason:** Uses standard Aspect interface, minimal custom extensions
   - **Impact:** Azure module is relatively independent

2. **Examples → EntityFramework**
   - **Coupling Level:** MEDIUM
   - **Reason:** Uses public EfInterceptor API only
   - **Impact:** Examples are consumption-based, low coupling

---

## Migration Impact Assessment

### Phase 1: Foundation (.NET 6/8 Migration)
**Modules:** Core, Main  
**Dependency Changes:**
- Replace .NET Framework 4.5 BCL with .NET 6/8 BCL
- Update System.Configuration → Microsoft.Extensions.Configuration
- Update System.Runtime.Caching → Microsoft.Extensions.Caching.Memory
- Minimal code changes expected (most BCL is compatible)

**Effort:** MEDIUM  
**Risk:** LOW-MEDIUM  
**Blockers:** None

### Phase 2: Entity Framework Upgrade
**Module:** EntityFramework  
**Dependency Changes:**
- Entity Framework 5.0 → Entity Framework Core 8.x
- System.Data.Entity → Microsoft.EntityFrameworkCore
- DbContext API changes required

**Effort:** MEDIUM-HIGH  
**Risk:** MEDIUM  
**Blockers:** EF Core has breaking changes from EF 5.0

### Phase 3: Web Module Rewrite
**Module:** Web  
**Dependency Changes:**
- System.Web → ASP.NET Core
- HttpContext.Current → HttpContextAccessor
- Session management redesign
- **COMPLETE REWRITE REQUIRED**

**Effort:** VERY HIGH  
**Risk:** HIGH  
**Blockers:** System.Web does not exist in .NET Core - incompatible API

### Phase 4: Azure SDK Update
**Module:** Azure  
**Dependency Changes:**
- WindowsAzure.Storage 4.3.0 → Azure.Storage.Blobs 12.x
- Newtonsoft.Json 5.0.8 → 13.x
- Remove deprecated OData packages

**Effort:** MEDIUM  
**Risk:** MEDIUM  
**Blockers:** API changes in Azure SDK v12

### Phase 5: .NET Core Project Replacement
**Modules:** Core.50Core, Main.50Core  
**Action:** Delete and replace with proper .NET 6/8 projects

**Effort:** LOW  
**Risk:** LOW  
**Blockers:** None (beta code, not production)

---

## Recommendations

### Immediate Actions (Security)
1. **Upgrade Newtonsoft.Json to 13.x** (fixes known vulnerabilities)
2. **Remove unofficial Azure packages**, use official Microsoft packages
3. **Document Entity Framework 5.0 security posture** (12 years without patches)

### Short-Term Actions (Stability)
1. **Upgrade Entity Framework 5.0 → 6.4.4** (last .NET Framework version)
2. **Update Azure SDK to latest compatible with .NET Framework 4.5**
3. **Remove .NET Core beta projects** (unsupported, unstable)

### Long-Term Actions (Modernization)
1. **Migrate Core + Main to .NET 6 LTS or .NET 8 LTS**
2. **Upgrade Entity Framework module to EF Core 8.x**
3. **Rewrite Web module for ASP.NET Core** (or deprecate if not used)
4. **Update Azure module to Azure SDK v12+**
5. **Replace System.Transactions with alternative** (limited Core+ support)

---

**Document Version:** 1.0  
**Last Updated:** 2024  
**Dependencies Analyzed:** 11 packages.config files, 21 projects, 150+ BCL namespaces  
**Technical Debt Items Identified:** 7 critical issues, 5 high priority updates  
**Next Steps:** See [Technical Debt Report](../technical-debt-report.md) and [Outdated Components](../technical-debt/outdated-components.md)
