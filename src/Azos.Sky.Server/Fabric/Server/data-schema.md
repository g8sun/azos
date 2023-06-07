﻿# Data Schema

Logical Fiber store design using **pseudo SQL DDL**.
Runspace is the database instance

## Table fiber

```sql
 table fiber
 (
   Gdid     GDID  not null PRIMARY KEY , -- imut
   ShardId  ATOM  not null, -- imut
   Guid     GUID  not null, -- imut
   Origin   ATOM  not null, -- imut
   ImgId    GUID not null,  -- imut
   Group    VARCHAR,        -- imut
   Impersonate ENTITYID,    -- imut
   CreateUtc  DATETIME not null, -- imut
   Initiator ENTITYID,  -- imut
   Owner     ENTITYID,  -- imut
   Description VARCHAR, -- imut
   Parameters  BIN,     -- imut
   Tags      [{p,v},..],    -- imut
 )
```

## Table state
```sql
 table state -- contains mutable status data
 (
   G_FIBER     GDID  not null PRIMARY KEY, -- imut

   Status      ENUM  not null, -- Created|Started|Paused|Suspended|Finished|Crashed|Aborted
   StatusDescr varchar,
   Priority     FLOAT not null,

   LockProc   ATOM,
   LockUtc    INT,
   NextSliceUtc  DATETIME not NULL,
   CurrentStep ATOM not null,

   LastDurationMs int not null,
   AvgSliceDurationMs int not null,

   LastLatencyMs long not null,
   AvgLatencyMs long not null,

   ExitCode   int not null,
   Result     BIN,
   Exception  JSON TEXT,
 )
```

The scheduler statement for `IFiberStoreShard.Task<FiberMemory> CheckOutNextPendingAsync(Atom runspace, Atom procId);`
```sql
  select t1.* from state t1
  where
    (t1.Status = `Active`) and
    (t1.NextSliceUtc <= NOW) and
    (LockProc is null || LockUtc < ?HUNG_15MIN_AGO) and
  --  (t1.NextSliceUtc > ?TIME_SCROLL_CURSOR)
  order by
    t1.NextSliceUtc ASC
  LIMIT 250
```
Priority sorting is done in memory



## Table slot
```sql
 table status -- contains mutable status data
 (
   G_FIBER     GDID  not null PRIMARY KEY(col 1), -- imut
   SLOT_ID     ATOM  not null PRIMARY KEY(col 2), -- imut
   NO_PRELOAD  BOOL  not null,
   CONTENT     BLOB
 )
```


