# System Context - High-Level System Boundaries

## Overview
This diagram shows the Aspectacular AOP Framework in its system context, including external dependencies, integration points, and boundaries with external systems.

---

## System Context Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                             │
│                      EXTERNAL SYSTEMS & DEPENDENCIES                        │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘

    ┌────────────────┐      ┌────────────────┐      ┌────────────────┐
    │  .NET Runtime  │      │   Entity       │      │   ASP.NET      │
    │                │      │   Framework    │      │   (System.Web) │
    │  • .NET 4.0    │      │                │      │                │
    │  • .NET 4.5    │      │  • EF 5.0      │      │  • Web Forms   │
    │  • .NET 4.8    │      │  • EF 6.x      │      │  • MVC         │
    └────────┬───────┘      └────────┬───────┘      └────────┬───────┘
             │                       │                       │
             │ Depends on            │ Integrates            │ Integrates
             │                       │                       │
             ▼                       ▼                       ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                             │
│                     ASPECTACULAR AOP FRAMEWORK                              │
│                        (Main System Boundary)                               │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  CORE LAYER                                                         │   │
│  │  ───────────────────────────────────────────────────────────────────│   │
│  │  • Aspect Interface (IAspect)                                       │   │
│  │  • Proxy Generation                                                 │   │
│  │  • Interception Context                                             │   │
│  │  • Method Signature Analysis                                        │   │
│  │  • Parameter Capture                                                │   │
│  │  • Result Handling                                                  │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  MAIN LAYER                                                         │   │
│  │  ───────────────────────────────────────────────────────────────────│   │
│  │  • Built-in Aspects:                                                │   │
│  │    - LoggingAspect                                                  │   │
│  │    - CacheAspect                                                    │   │
│  │    - RetryAspect                                                    │   │
│  │    - PerformanceAspect                                              │   │
│  │    - TransactionAspect                                              │   │
│  │    - AuthorizationAspect                                            │   │
│  │  • Aspect Composition                                               │   │
│  │  • Lifecycle Management                                             │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  SPECIALIZED MODULES                                                │   │
│  │  ───────────────────────────────────────────────────────────────────│   │
│  │  • EntityFramework Module                                           │   │
│  │  • Web Module (ASP.NET)                                             │   │
│  │  • Azure Module                                                     │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
             │                       │                       │
             │ Uses                  │ Uses                  │ Uses
             │                       │                       │
             ▼                       ▼                       ▼
    ┌────────────────┐      ┌────────────────┐      ┌────────────────┐
    │   SQL Server   │      │   Azure Queue  │      │  HTTP Clients  │
    │   Database     │      │   Storage      │      │  (External     │
    │                │      │                │      │   APIs)        │
    │  • ADO.NET     │      │  • Azure SDK   │      │                │
    │  • EF Context  │      │  • Queue Ops   │      │  • REST APIs   │
    └────────────────┘      └────────────────┘      └────────────────┘

    ┌────────────────┐      ┌────────────────┐      ┌────────────────┐
    │  Configuration │      │   Logging      │      │   Monitoring   │
    │  Providers     │      │   Providers    │      │   Systems      │
    │                │      │                │      │                │
    │  • App.config  │      │  • Log4Net     │      │  • App Insights│
    │  • Web.config  │      │  • NLog        │      │  • Custom      │
    │  • Custom      │      │  • Trace.axd   │      │  • Metrics     │
    └────────────────┘      └────────────────┘      └────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                             │
│                         CONSUMER APPLICATIONS                               │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘

    ┌────────────────┐      ┌────────────────┐      ┌────────────────┐
    │  Web           │      │  Desktop       │      │  Service       │
    │  Applications  │      │  Applications  │      │  Applications  │
    │                │      │                │      │                │
    │  • ASP.NET MVC │      │  • WinForms    │      │  • WCF         │
    │  • Web API     │      │  • WPF         │      │  • WebAPI      │
    │  • Web Forms   │      │  • Console     │      │  • Workers     │
    └────────────────┘      └────────────────┘      └────────────────┘
```

---

## System Boundaries

### Internal Boundaries

```
┌─────────────────────────────────────────────────────────────┐
│ ASPECTACULAR FRAMEWORK                                      │
│                                                             │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ CORE MODULE                                          │  │
│  │ • Foundation components                              │  │
│  │ • No external dependencies (except .NET BCL)         │  │
│  └──────────────────────────────────────────────────────┘  │
│                           │                                 │
│                           │ Depends on                      │
│                           ▼                                 │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ MAIN MODULE                                          │  │
│  │ • Depends on: Core                                   │  │
│  │ • No other external dependencies                     │  │
│  └──────────────────────────────────────────────────────┘  │
│                           │                                 │
│                           │ Extended by                     │
│                           ▼                                 │
│  ┌─────────────────────┬─────────────────┬──────────────┐  │
│  │ ENTITYFRAMEWORK     │ WEB MODULE      │ AZURE        │  │
│  │ MODULE              │                 │ MODULE       │  │
│  │ • Depends: Core,    │ • Depends: Core,│ • Depends:   │  │
│  │   Main, EF 5.0      │   Main,         │   Core, Main,│  │
│  │                     │   System.Web    │   Azure SDK  │  │
│  └─────────────────────┴─────────────────┴──────────────┘  │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### External Boundaries

```
┌───────────────────────────────────────────────────────────────┐
│ INTEGRATION POINTS                                            │
├───────────────────────────────────────────────────────────────┤
│                                                               │
│  1. DATABASE BOUNDARY                                         │
│     ┌──────────────────────────────────────────────┐        │
│     │ Framework → ADO.NET/EF → SQL Server          │        │
│     │ • Connection management                      │        │
│     │ • Query interception                         │        │
│     │ • Transaction coordination                   │        │
│     └──────────────────────────────────────────────┘        │
│                                                               │
│  2. WEB BOUNDARY                                              │
│     ┌──────────────────────────────────────────────┐        │
│     │ Framework → System.Web → ASP.NET Runtime     │        │
│     │ • HttpContext access                         │        │
│     │ • Request/Response interception              │        │
│     │ • Session/Cache management                   │        │
│     └──────────────────────────────────────────────┘        │
│                                                               │
│  3. CLOUD BOUNDARY                                            │
│     ┌──────────────────────────────────────────────┐        │
│     │ Framework → Azure SDK → Azure Services       │        │
│     │ • Queue operations                           │        │
│     │ • Storage operations                         │        │
│     │ • Authentication                             │        │
│     └──────────────────────────────────────────────┘        │
│                                                               │
│  4. LOGGING BOUNDARY                                          │
│     ┌──────────────────────────────────────────────┐        │
│     │ Framework → Logging Providers → Sinks        │        │
│     │ • Structured logging                         │        │
│     │ • Log aggregation                            │        │
│     │ • Trace correlation                          │        │
│     └──────────────────────────────────────────────┘        │
│                                                               │
└───────────────────────────────────────────────────────────────┘
```

---

## Data Flow Across Boundaries

### Incoming Data Flow

```
┌─────────────────────────────────────────────────────────────┐
│ APPLICATION CODE (Consumer)                                 │
└─────────────────────────────────────────────────────────────┘
                         │
                         │ 1. Method invocation via Proxy
                         │    proxy.Invoke(svc => svc.Method(args))
                         ▼
┌─────────────────────────────────────────────────────────────┐
│ ASPECTACULAR FRAMEWORK                                      │
│ • Captures method metadata                                  │
│ • Applies aspect chain                                      │
│ • Executes 7-stage lifecycle                                │
└─────────────────────────────────────────────────────────────┘
                         │
          ┌──────────────┼──────────────┐
          │              │              │
          ▼              ▼              ▼
    ┌──────────┐   ┌──────────┐   ┌──────────┐
    │ Database │   │ Azure    │   │ External │
    │          │   │ Queue    │   │ APIs     │
    └──────────┘   └──────────┘   └──────────┘
```

### Outgoing Data Flow

```
    ┌──────────┐   ┌──────────┐   ┌──────────┐
    │ Database │   │ Azure    │   │ External │
    │ Results  │   │ Messages │   │ Responses│
    └──────────┘   └──────────┘   └──────────┘
          │              │              │
          └──────────────┼──────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│ ASPECTACULAR FRAMEWORK                                      │
│ • Processes results through aspects                         │
│ • Caching, logging, transformation                          │
│ • Completes lifecycle stages                                │
└─────────────────────────────────────────────────────────────┘
                         │
                         │ 2. Return result to caller
                         │    return result;
                         ▼
┌─────────────────────────────────────────────────────────────┐
│ APPLICATION CODE (Consumer)                                 │
│ • Receives result                                           │
│ • Or handles exception                                      │
└─────────────────────────────────────────────────────────────┘
```

---

## External System Integration Details

### 1. Database Integration (Entity Framework)

**Integration Point:** `Aspectacular.Framework.EntityFramework`

```
┌─────────────────────────────────────────────────────────────┐
│ DATABASE INTEGRATION                                        │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Framework Components:                                      │
│  • DbContextSingleCallProxy<TDbContext>                    │
│  • ObjectContextSingleCallProxy<TObjectContext>            │
│  • LazyLoadSwitch (TempLazyLoadOn)                         │
│  • IDbEntityKey interface                                  │
│  • EF extension methods                                     │
│                                                             │
│  External Dependencies:                                     │
│  • Entity Framework 5.0 (current, deprecated)              │
│  • System.Data.Entity                                       │
│  • System.Data.SqlClient                                    │
│                                                             │
│  Operations:                                                │
│  • Query interception                                       │
│  • Connection management                                    │
│  • Transaction coordination                                 │
│  • Lazy loading control                                     │
│  • Entity attachment/detachment                             │
│                                                             │
│  Data Flow:                                                 │
│  Framework → EF Context → ADO.NET → SQL Server             │
│  Framework ← Result Sets ← ADO.NET ← SQL Server            │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

### 2. Web Integration (ASP.NET)

**Integration Point:** `Aspectacular.Framework.Web`

```
┌─────────────────────────────────────────────────────────────┐
│ WEB INTEGRATION                                             │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Framework Components:                                      │
│  • AspNetRoleAuthorizationAspect                           │
│  • AspNetTraceAspect                                        │
│  • RequestCache (HttpContext.Items)                        │
│  • Request/Response extensions                              │
│                                                             │
│  External Dependencies:                                     │
│  • System.Web (ASP.NET Runtime)                            │
│  • System.Web.Security (Roles)                             │
│  • HttpContext (Current)                                    │
│                                                             │
│  Operations:                                                │
│  • Request-scoped caching                                   │
│  • Role-based authorization                                 │
│  • Trace output to trace.axd                               │
│  • HTTP context access                                      │
│  • Session state management                                 │
│                                                             │
│  Data Flow:                                                 │
│  HTTP Request → ASP.NET → Framework → Business Logic       │
│  HTTP Response ← ASP.NET ← Framework ← Result              │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

### 3. Azure Cloud Integration

**Integration Point:** `Aspectacular.Framework.Azure`

```
┌─────────────────────────────────────────────────────────────┐
│ AZURE CLOUD INTEGRATION                                     │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Framework Components:                                      │
│  • AzureQueueMulticastRoute                                │
│  • AzureQueueMonitor                                        │
│  • AzureQueueConnection (Source/Destination)               │
│  • Message transformation pipeline                          │
│                                                             │
│  External Dependencies:                                     │
│  • Microsoft.WindowsAzure.Storage (v4 - deprecated)        │
│  • Azure Storage account                                    │
│  • Queue storage service                                    │
│                                                             │
│  Operations:                                                │
│  • Queue message multicasting                               │
│  • Queue monitoring (polling)                               │
│  • Message transformation                                   │
│  • Retry on Azure transient failures                        │
│                                                             │
│  Data Flow:                                                 │
│  Framework → Azure SDK → Queue Storage (source)            │
│  Framework → Azure SDK → Queue Storage (destinations)      │
│                                                             │
│  Security:                                                  │
│  • Connection string-based authentication                   │
│  • HTTPS for all operations                                 │
│  • Storage account key management                           │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## Security Boundaries

```
┌─────────────────────────────────────────────────────────────┐
│ SECURITY BOUNDARIES                                         │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  1. AUTHENTICATION BOUNDARY                                 │
│     ┌──────────────────────────────────────────────────┐  │
│     │ • ASP.NET Forms/Windows Authentication          │  │
│     │ • Azure storage account keys                     │  │
│     │ • Database connection strings                    │  │
│     │ Framework validates identity before execution    │  │
│     └──────────────────────────────────────────────────┘  │
│                                                             │
│  2. AUTHORIZATION BOUNDARY                                  │
│     ┌──────────────────────────────────────────────────┐  │
│     │ • Role-based access control (RBAC)              │  │
│     │ • Claims-based authorization                     │  │
│     │ • Enforced in Stage 1 of aspect lifecycle       │  │
│     │ • HTTP 403 on authorization failure              │  │
│     └──────────────────────────────────────────────────┘  │
│                                                             │
│  3. DATA BOUNDARY                                           │
│     ┌──────────────────────────────────────────────────┐  │
│     │ • Connection string encryption                   │  │
│     │ • No PII in logs (must be implemented)           │  │
│     │ • Secure credential storage                      │  │
│     │ • HTTPS for external communication               │  │
│     └──────────────────────────────────────────────────┘  │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## Deployment Context

```
┌─────────────────────────────────────────────────────────────────────┐
│ DEPLOYMENT SCENARIOS                                                │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  SCENARIO 1: Web Application Deployment                            │
│  ┌───────────────────────────────────────────────────────────────┐ │
│  │ IIS Server                                                    │ │
│  │  ├─ ASP.NET Application Pool                                  │ │
│  │  │   ├─ Web Application (.dll)                                │ │
│  │  │   ├─ Aspectacular.Framework.Core.dll                       │ │
│  │  │   ├─ Aspectacular.Framework.Main.dll                       │ │
│  │  │   ├─ Aspectacular.Framework.Web.dll                        │ │
│  │  │   └─ Aspectacular.Framework.EntityFramework.dll            │ │
│  │  └─ Web.config (connection strings, settings)                 │ │
│  │                                                                │ │
│  │ External Dependencies:                                         │ │
│  │  ├─ SQL Server (on-premises or Azure SQL)                     │ │
│  │  ├─ Azure Queue Storage (optional)                            │ │
│  │  └─ Logging service (Log4Net, Application Insights)           │ │
│  └───────────────────────────────────────────────────────────────┘ │
│                                                                     │
│  SCENARIO 2: Windows Service Deployment                            │
│  ┌───────────────────────────────────────────────────────────────┐ │
│  │ Windows Service Host                                          │ │
│  │  ├─ Service Executable (.exe)                                 │ │
│  │  ├─ Aspectacular.Framework.Core.dll                           │ │
│  │  ├─ Aspectacular.Framework.Main.dll                           │ │
│  │  ├─ Aspectacular.Framework.Azure.dll (for queue processing)   │ │
│  │  └─ App.config                                                │ │
│  │                                                                │ │
│  │ External Dependencies:                                         │ │
│  │  ├─ Azure Queue Storage (primary)                             │ │
│  │  ├─ SQL Server (for data persistence)                         │ │
│  │  └─ Monitoring service                                        │ │
│  └───────────────────────────────────────────────────────────────┘ │
│                                                                     │
│  SCENARIO 3: Azure Cloud Service Deployment                        │
│  ┌───────────────────────────────────────────────────────────────┐ │
│  │ Azure Web Role / Worker Role                                  │ │
│  │  ├─ Cloud Service Package (.cspkg)                            │ │
│  │  ├─ Aspectacular.Framework.*.dll                              │ │
│  │  └─ ServiceConfiguration.cscfg                                │ │
│  │                                                                │ │
│  │ External Dependencies:                                         │ │
│  │  ├─ Azure SQL Database                                        │ │
│  │  ├─ Azure Queue Storage                                       │ │
│  │  ├─ Azure Application Insights                                │ │
│  │  └─ Azure Key Vault (for secrets)                            │ │
│  └───────────────────────────────────────────────────────────────┘ │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Technology Stack Summary

| Layer | Technology | Version | Status |
|-------|-----------|---------|---------|
| Runtime | .NET Framework | 4.0, 4.5, 4.8 | ⚠️ Upgrade to 4.8 |
| Database | Entity Framework | 5.0 | ⚠️ Upgrade to EF 6/Core |
| Database | SQL Server | 2008+ | ✓ Supported |
| Web | ASP.NET (System.Web) | 4.0+ | ⚠️ Consider Core |
| Cloud | Azure SDK | v4 | ⚠️ Upgrade to v12 |
| Cloud | Azure Queue Storage | Any | ✓ Supported |
| Logging | Log4Net, NLog, Trace | Any | ✓ Supported |
| Serialization | Newtonsoft.Json | 5.0.8 | ⚠️ Upgrade to 13.0.3 |

---

## Related Diagrams
- [Integration Patterns](integration-patterns.md) - Detailed integration scenarios
- [Dependency Graph](../structural/dependency-graph.md) - Internal module dependencies
- [Interception Pipeline](../data-flow/interception-pipeline.md) - Data flow through framework

---

**Document Version:** 1.0  
**Last Updated:** Migration Planning Phase
