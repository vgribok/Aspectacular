# Dependency Graph - Visual Dependency Mapping

## Overview

This document provides text-based visual representations of dependency relationships in the Aspectacular AOP Framework. All diagrams use ASCII art for universal readability.

**Legend:**
```
→   Depends on / Uses
├── Branch / Contains
│   Vertical connection
└── Terminal branch
⚠️   Technical debt / Outdated
```

---

## Internal Module Dependency Graph

### Complete Dependency Hierarchy

```
┌─────────────────────────────────────────────────────────────────────┐
│                         Application Layer                           │
│                    (Consumer Applications)                          │
└──────────────────┬──────────────────────────────────────────────────┘
                   │ Consumes Framework API
┌──────────────────▼──────────────────────────────────────────────────┐
│                    Extension Modules Layer                          │
│ ┌──────────────┐  ┌──────────────┐  ┌──────────────┐              │
│ │   Entity     │  │   ASP.NET    │  │    Azure     │              │
│ │  Framework   │  │     Web      │  │    Cloud     │              │
│ │  Module      │  │   Module     │  │   Module     │              │
│ │              │  │              │  │              │              │
│ │ - EfInt      │  │ - AspNet     │  │ - Azure      │              │
│ │   erceptor   │  │   Auth       │  │   Aspects    │              │
│ │ - LazyLoad   │  │ - Trace      │  │ - Storage    │              │
│ │   Switch     │  │ - Request    │  │   Helpers    │              │
│ │ - IDbEntity  │  │   Cache      │  │              │              │
│ │   Key        │  │ - Session    │  │              │              │
│ └──────┬───────┘  └──────┬───────┘  └──────┬───────┘              │
│        │                 │                 │                       │
│        │  ⚠️ EF 5.0      │  ⚠️ System.Web  │  ⚠️ Azure SDK 4.x     │
│        │  (2012)         │  (.NET Fwk)     │  (Outdated)           │
│        └─────────────────┼─────────────────┘                       │
└──────────────────────────┼─────────────────────────────────────────┘
                           │ Depends on
┌──────────────────────────▼─────────────────────────────────────────┐
│                     Core AOP Engine Layer                           │
│           Aspectacular.Framework.Main                               │
│                                                                     │
│  ┌────────────────────────────────────────────────────────────┐   │
│  │  Proxy                 (Dynamic interception)              │   │
│  │  Aspect                (Base class + lifecycle)            │   │
│  │  IAspect               (7-stage interface)                 │   │
│  │  AspectInjector        (Orchestration)                     │   │
│  │  InterceptedMethodMD   (Metadata)                          │   │
│  │  CallLog               (Logging infrastructure)            │   │
│  │  DalInterceptor        (DAL helper)                        │   │
│  │                                                             │   │
│  │  Built-in Aspects:                                         │   │
│  │  - StopwatchAspect      - RetryAspect                      │   │
│  │  - CacheAspect          - DtcTransactionAspect             │   │
│  │  - ClaimsAuthAspect     - SqlExecutionPlanAspect           │   │
│  └────────────────────────┬───────────────────────────────────┘   │
└───────────────────────────┼───────────────────────────────────────┘
                            │ Depends on
┌───────────────────────────▼───────────────────────────────────────┐
│                     Foundation Utilities Layer                     │
│              Aspectacular.Framework.Core                           │
│                                                                    │
│  ┌──────────────────────────────────────────────────────────┐    │
│  │  LINQ & Query:                                           │    │
│  │  - QueryModifiers        - PredicateBuilder              │    │
│  │  - FullOuterJoin         - QueryableExtensions           │    │
│  │                                                           │    │
│  │  Collections:                                            │    │
│  │  - CollectionExtensions  - Range<T>                      │    │
│  │                                                           │    │
│  │  Type System:                                            │    │
│  │  - TypeAndReflectionExtensions                           │    │
│  │  - StringExtensions                                      │    │
│  │                                                           │    │
│  │  Data Access:                                            │    │
│  │  - AdoNetExtensions      - SqlConnAttribs                │    │
│  │                                                           │    │
│  │  DateTime:                                               │    │
│  │  - DateTimeExtensions    - RelativeTimeSpan              │    │
│  │                                                           │    │
│  │  Threading:                                              │    │
│  │  - Threading             - BlockingPoll                  │    │
│  │                                                           │    │
│  │  Other:                                                  │    │
│  │  - Caching              - Disposer                       │    │
│  │  - ExceptionExtensions  - SvcLocator                     │    │
│  └──────────────────────────────────────────────────────────┘    │
└────────────────────────────────────────────────────────────────────┘
```

---

## External Dependency Graph

### Package Dependencies by Module

```
Aspectacular Framework Modules
│
├─── Core Module (No external packages)
│     └─── Only .NET BCL dependencies
│
├─── Main Module (No external packages)
│     └─── Only .NET BCL + Core dependencies
│
├─── EntityFramework Module
│     ├─── Main Module (internal)
│     ├─── Core Module (transitive, internal)
│     └─── Entity Framework 5.0.0 ⚠️ CRITICAL
│           └─── Released: August 2012 (12+ years old)
│
├─── Web Module  
│     ├─── Main Module (internal)
│     ├─── Core Module (transitive, internal)
│     └─── System.Web ⚠️ .NET FRAMEWORK ONLY
│           └─── Not available in .NET Core+
│
├─── Azure Module
│     ├─── Main Module (internal)
│     ├─── Core Module (transitive, internal)
│     └─── Azure SDK v1/v2 (Multiple packages) ⚠️
│           ├─── WindowsAzure.Storage 4.3.0 (Old, current: v12)
│           ├─── Microsoft.Data.OData 5.6.3 (Deprecated)
│           ├─── Microsoft.Data.Edm 5.6.3 (Deprecated)
│           ├─── Newtonsoft.Json 5.0.8 ⚠️ SECURITY
│           ├─── Microsoft.WindowsAzure.ConfigurationManager 2.0.3
│           ├─── System.Spatial 5.6.3
│           └─── Unofficial.Microsoft.WindowsAzure.ServiceRuntime ⚠️
│                 └─── Third-party unofficial package (RISKY)
│
├─── Example Projects
│     ├─── EntityFramework Module (internal)
│     └─── Entity Framework 5.0.0 ⚠️ (same as EF module)
│
├─── UnitTest Projects
│     ├─── Various modules (internal)
│     └─── Entity Framework 5.0.0 ⚠️ (for EF tests)
│
└─── .NET Core Projects ⚠️ BETA/UNSUPPORTED
      ├─── Core.50Core Module
      │     └─── System.* 4.0.*-beta-22816 (15+ beta packages)
      └─── Main.50Core Module
            └─── System.* 4.0.*-beta-22816 (9+ beta packages)
```

---

## BCL (Base Class Library) Dependency Tree

### .NET Framework BCL Dependencies

```
Aspectacular Framework
│
├─── System (Universal)
│     └─── Used by: All modules
│
├─── System.Core (LINQ, Expression Trees)
│     └─── Used by: Core (QueryModifiers, PredicateBuilder)
│           Used by: Main (InterceptedMethodMetadata, Proxy)
│
├─── System.Data (ADO.NET)
│     └─── Used by: Core (AdoNetExtensions)
│           Used by: Main (SqlConnectionAttributesAspect)
│
├─── System.Data.Entity ⚠️ .NET FRAMEWORK ONLY
│     └─── Used by: EntityFramework module
│           Status: NOT in .NET Core (use EF Core instead)
│
├─── System.Transactions (DTC Support)
│     └─── Used by: Main (DtcTransactionAspect)
│           Status: LIMITED support in .NET Core 2.0+
│
├─── System.Runtime.Caching (Memory Cache)
│     └─── Used by: Core (Caching utilities)
│           Used by: Main (CacheAspect base)
│           Migration: Use Microsoft.Extensions.Caching in .NET Core+
│
├─── System.Configuration (app.config/web.config)
│     └─── Used by: Main (DefaultAspectsConfigSection)
│           Migration: Use Microsoft.Extensions.Configuration
│
├─── System.Web ⚠️ .NET FRAMEWORK ONLY
│     ├─── Used by: Web module (ALL classes)
│     ├─── HttpContext.Current
│     ├─── HttpSessionState
│     ├─── HttpRequest/HttpResponse
│     └─── Status: NOT in .NET Core (ASP.NET Core rewrite required)
│
├─── System.Net (Network Operations)
│     └─── Used by: Azure module
│
├─── System.Xml / System.Xml.Linq
│     └─── Used by: Core (XmlSerializationHelpers)
│
├─── System.Collections.Generic
│     └─── Used by: All modules (universal collections)
│
├─── System.Linq
│     └─── Used by: All modules (LINQ queries)
│
├─── System.Reflection
│     └─── Used by: Core (reflection utilities)
│           Used by: Main (metadata extraction)
│
├─── System.Threading
│     └─── Used by: Core (Threading utilities)
│           Used by: Main (ThreadLocal<Stack<Proxy>>)
│
└─── System.Diagnostics
      └─── Used by: Main (Debug.Assert, tracing)
```

---

## Data Flow Dependency Graph

### Method Interception Data Flow

```
                    Application Method Call
                            │
                            ▼
              ┌─────────────────────────┐
              │   Proxy.Invoke()        │
              │   (AspectInjector.cs)   │
              └──────┬──────────────────┘
                     │ Depends on ↓
        ┌────────────┼────────────┐
        │            │            │
        ▼            ▼            ▼
   ┌────────┐  ┌─────────┐  ┌─────────┐
   │ Core   │  │  Main   │  │Extension│
   │Utils   │  │Aspects  │  │ Modules │
   └────────┘  └─────────┘  └─────────┘
        │            │            │
        └────────────┼────────────┘
                     ▼
         ┌────────────────────────┐
         │  Actual Method         │
         │  Execution             │
         └────────┬───────────────┘
                  │
    ┌─────────────┼─────────────┐
    │             │             │
    ▼             ▼             ▼
┌────────┐  ┌──────────┐  ┌─────────┐
│ BCL    │  │External  │  │Database/│
│Classes │  │Packages  │  │Web/Cloud│
└────────┘  └──────────┘  └─────────┘
```

### Aspect Execution Dependency Chain

```
Step 1: BeforeResolvingInstance
    ↓ (uses Core reflection utilities)
    │
Step 2: BeforeTryingMethodExec
    ↓ (uses Main metadata classes)
    │
Step 3: BeforeMassagingReturnedResult
    ↓ (uses Core LINQ modifiers)
    │
Step 4: AfterSuccess/Exception
    ↓ (uses Core exception extensions)
    │
Step 5: FinallyAfterMethodExecution
    ↓ (uses Main logging infrastructure)
    │
Step 6: AfterInstanceCleanup
    ↓ (uses Core Disposer patterns)
    │
Step 7: AfterEverythingSaidAndDone
    ↓ (uses Main CallLog finalization)
```

---

## Compilation Dependency Order

### Required Build Order

```
Level 1: Foundation
┌────────────────────────────────┐
│ Aspectacular.Framework.Core    │ ← Build FIRST (no dependencies)
└──────────────┬─────────────────┘
               │ Reference ↓
               │
Level 2: Core Engine
┌──────────────▼─────────────────┐
│ Aspectacular.Framework.Main    │ ← Build SECOND (depends on Core)
└──────────────┬─────────────────┘
               │ Reference ↓
               │
Level 3: Extensions (parallel)
┌──────────────┼─────────────────────────────────┐
│              │                                  │
▼              ▼                                  ▼
┌─────────┐  ┌──────────┐  ┌───────────────────┐
│   EF    │  │   Web    │  │      Azure        │ ← Build THIRD (parallel)
│ Module  │  │  Module  │  │     Module        │
└─────────┘  └──────────┘  └───────────────────┘
│              │                  │
│              │                  │
Level 4: Examples & Tests
└──────────────┼──────────────────┘
               │ Reference ↓
               │
┌──────────────▼────────────────────┐
│  Example Projects                 │ ← Build FOURTH
│  UnitTest Projects                │
└───────────────────────────────────┘
```

---

## Threading Dependency Pattern

### ThreadLocal Call Stack

```
Thread 1                    Thread 2                    Thread 3
│                           │                           │
├─ ThreadLocal<Stack>       ├─ ThreadLocal<Stack>       ├─ ThreadLocal<Stack>
│  └─ Proxy Stack           │  └─ Proxy Stack           │  └─ Proxy Stack
│     ├─ Proxy 1            │     ├─ Proxy A            │     ├─ Proxy X
│     │  └─ Call ID: ...    │     │  └─ Call ID: ...    │     │  └─ Call ID: ...
│     ├─ Proxy 2 (nested)   │     └─ Proxy B (nested)   │     └─ Proxy Y (nested)
│     │  └─ Call ID: ...    │        └─ Call ID: ...    │        └─ Call ID: ...
│     └─ Proxy 3 (nested)   │                           │
│        └─ Call ID: ...    │                           │
│                           │                           │
└─ Independent stack        └─ Independent stack        └─ Independent stack
   (no cross-thread         (no cross-thread           (no cross-thread
    interference)            interference)              interference)
```

**Key Dependencies:**
- `System.Threading.ThreadLocal<T>` (BCL)
- `System.Collections.Generic.Stack<T>` (BCL)
- No cross-thread synchronization needed (by design)

---

## Technical Debt Dependency Impact

### Severity by Dependency Age

```
                        2024 (Current Year)
                              │
    ┌─────────────────────────┼──────────────────────────┐
    │                         │                          │
    │          12 YEARS OLD   │   8+ YEARS OLD          │   BETA (Unstable)
    ▼                         ▼                          ▼
┌─────────┐              ┌─────────┐              ┌──────────┐
│Entity   │ ⚠️ CRITICAL  │Azure    │ ⚠️ HIGH      │.NET Core │ ⚠️ CRITICAL
│Framework│              │SDK 4.x  │              │Beta 5.0  │
│5.0      │              │packages │              │packages  │
│(2012)   │              │(2014-16)│              │(2015)    │
└─────────┘              └─────────┘              └──────────┘
    │                         │                          │
    │ Impacts:                │ Impacts:                 │ Impacts:
    ├─ EF Module              ├─ Azure Module            ├─ Core.50Core
    ├─ Examples               ├─ Azure Tests             └─ Main.50Core
    └─ Unit Tests             └─ Security Risk              │
                                  (Newtonsoft 5.0.8)          │
                                                              └─ UNSUPPORTED
                                                                 (should delete)
```

### Migration Blocker Dependencies

```
Cannot Migrate to .NET Core WITHOUT Addressing:

┌──────────────────────────────────────────────────────────┐
│  BLOCKER 1: System.Web                                   │
│  └─ Web Module uses extensively                          │
│     └─ Solution: Complete rewrite to ASP.NET Core        │
│        └─ Effort: VERY HIGH (2-3 months)                 │
│           └─ Alternative: Deprecate Web module           │
└──────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────┐
│  BLOCKER 2: System.Data.Entity                           │
│  └─ EntityFramework module depends on it                 │
│     └─ Solution: Upgrade to EF Core 8.x                  │
│        └─ Effort: MEDIUM (2-4 weeks)                     │
│           └─ Breaking changes in EF Core API             │
└──────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────┐
│  BLOCKER 3: System.Transactions (Distributed)            │
│  └─ DtcTransactionAspect uses DTC                        │
│     └─ Solution: Limited support in .NET Core 2.0+       │
│        └─ Effort: LOW-MEDIUM (1 week)                    │
│           └─ May need to redesign for distributed trans  │
└──────────────────────────────────────────────────────────┘
```

---

## Summary Statistics

### Dependency Counts

| Category | Count | Outdated | Critical |
|----------|-------|----------|----------|
| Internal Modules | 8 | 2 (.NET Core beta) | 0 |
| External Packages | 26 | 23 | 3 |
| BCL Namespaces | 15+ | 3 (.NET Fwk only) | 2 |
| Framework-Locked | 1 | 1 (System.Web) | 1 |

### Technical Debt Summary

| Severity | Count | Requires Immediate Action |
|----------|-------|--------------------------|
| CRITICAL | 4 | Yes (Security + EOL) |
| HIGH | 3 | Yes (Outdated 8+ years) |
| MEDIUM | 5 | Planning needed |
| LOW | 2 | Nice to have |

---

**Document Version:** 1.0  
**Last Updated:** 2024  
**Diagrams Generated:** 10 comprehensive dependency visualizations  
**Cross-References:** See [Dependency Analysis](../../analysis/dependency-analysis.md) and [Technical Debt Report](../../technical-debt-report.md)
