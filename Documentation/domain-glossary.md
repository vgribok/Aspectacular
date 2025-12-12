# Domain Glossary - AOP Terminology

## Core Concepts

**Aspect:** Cross-cutting concern implementation (logging, caching, security, etc.)  
**Aspect-Oriented Programming (AOP):** Programming paradigm for modularizing cross-cutting concerns  
**Augmented Class:** Class whose methods are intercepted by the framework  
**Proxy:** Dynamic interception object that wraps method execution  
**Interception:** Capturing method calls to apply aspects before/after execution  
**Call ID:** Unique GUID identifying each intercepted method invocation  
**Call Context:** Thread-local state maintaining interception information  

## Lifecycle Stages

**Step_1_BeforeResolvingInstance:** Pre-instantiation stage (instance methods only)  
**Step_2_BeforeTryingMethodExec:** Pre-execution stage (parameter validation)  
**Step_3_BeforeMassagingReturnedResult:** Post-execution stage (result transformation)  
**Step_4a_AfterSuccessfulCallCompletion:** Success handler  
**Step_4b_AfterCatchingMethodExecException:** Exception handler  
**Step_5_FinallyAfterMethodExecution:** Cleanup stage (always executed)  
**Step_6_Optional_AfterInstanceCleanup:** Instance disposal stage  
**Step_7_AfterEverythingSaidAndDone:** Final completion stage  

## Aspect Types

**RetryAspect:** Automatic retry for transient failures  
**CacheAspect:** Result caching for expensive operations  
**StopwatchAspect:** Performance timing and monitoring  
**ClaimsAuthorizationAspect:** Claims-based security  
**DtcTransactionAspect:** Distributed transaction management  
**LoggingAspect:** Call logging and tracing  

## Technical Terms

**Call-Invariant Method:** Method that returns same data for different instances called simultaneously  
**Required Aspect:** Aspect that must be present for method to execute  
**Aspect Ordering:** Sequence in which aspects execute (configurable via dependencies)  
**DAL (Data Access Layer):** Database interaction layer  
**EF (Entity Framework):** Microsoft's ORM framework  
**DTC (Distributed Transaction Coordinator):** Microsoft transaction management system  

**Document Version:** 1.0
