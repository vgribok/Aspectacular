# Aspect Lifecycle Sequence Diagram

## Complete 7-Stage Interception Sequence

```
App Code    Proxy      Aspects    Target Method    Return
   |          |           |              |            |
   |--Call--->|           |              |            |
   |          |           |              |            |
   |      [Create Proxy]  |              |            |
   |          |           |              |            |
   |          |--Step_1-->|              |            |
   |          |<---------[Auth Check]    |            |
   |          |           |              |            |
   |      [Resolve Instance]             |            |
   |          |           |              |            |
   |          |--Step_2-->|              |            |
   |          |<---------[Validate]      |            |
   |          |<---------[Cache Check]   |            |
   |          |           |              |            |
   |          |--------Invoke----------->|            |
   |          |<---------Result----------|            |
   |          |           |              |            |
   |          |--Step_3-->|              |            |
   |          |<---------[Transform]     |            |
   |          |<---------[Cache Store]   |            |
   |          |           |              |            |
   |          |--Step_4a->|              |            |
   |          |<---------[Log Success]   |            |
   |          |           |              |            |
   |          |--Step_5-->|              |            |
   |          |<---------[Commit Tx]     |            |
   |          |           |              |            |
   |          |--Step_7-->|              |            |
   |          |<---------[Final Log]     |            |
   |          |           |              |            |
   |<-Result--|           |              |            |
   |          |           |              |            |
```

## Exception Path Sequence

```
App Code    Proxy      Aspects    Target Method
   |          |           |              |
   |          |--------Invoke----------->|
   |          |<------Exception----------X
   |          |           |
   |          |--Step_4b->|
   |          |<--------[Decide Retry]
   |          |<--------[Log Error]
   |          |           |
   |      [IF Retry]      |
   |          |--------Invoke Again----->|
   |          |<---------Result----------|
   |          |           |
   |      [ELSE Throw]    |
   |<-Exception-|         |
```

**Source:** AspectInjector.cs (Proxy class)  
**Document Version:** 1.0
