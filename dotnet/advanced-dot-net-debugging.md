By Mario Hewardt

# Chapter 5. Managed Heap and Garbage Collection

## Windows Memory Architecture Overview

Windows heap manager
most allocate/free requests in segments(large chunk). maintaining look aside & free lists.

CLR heap manager

managed process

diff: DS & algorithm to maintain the **Integrity** of the heap.

GC code: `mscorwks.dll`

when CLR loaded, setting up 2 heaps:
1.  small object heap: object size < 850000 bytes
        segment exhausts, GC and expands heap if low space
2.  large object heap (LOH): object size >= 850000
        segment exhausts, create new segment.

not all memory in segment is committed. **commite on demand**

## What's in an Address

```
> !address <virtual address>
```

-   type: 
-   protect: page protect level: R/W
-   state: if committed or not
-   usage: 

## Allocating Memory

```
allocate
{
    if (GC needed)
    {
        GC;
    }

    // 1. No GC is needed. enough free blocks
    // 2. GC is finished
    
    object = new allocation

    if (object is finalizable)
    {
        recored the object in GC to manage its lifetime
    }
}
```

```
> !dumpheap [-type <namespace.class>]
```
lists all objects stored in heap: address, method table, size, count by class

use this to check which class is exhausting the memory

## Garbage Collector Internals

reference tracking for all live objects

short-lived vs long-lived objects. short-lived be collected more offen. track age by **Generations**.

CLR GC: reference trackign + generational GC

## Generations

gen 0(young), 1, 2(old). object move to next by surviving a GC cycle.

survive: still being referenced (in use, not free)

gen i and gen j have different GC freq.

gc is triggered by new allocation request:

```
gc()
{
    if (gen 0 is full)
    {
        // perform gc on gen 0
        free the not being referenced objects
        move the referenced objects to gen 1

        if (gen 1 is full)
        {
            // perform gc on gen 1
            free the not being referenced objects
            move the referenced objects to gen 2

            if (gen 2 is full)
            {
                // perform gc on gen 2
                free the not being referenced objects

                success = request new segment to hold gen 2 objects
                if (success == false)
                {
                    throw OutOfMemoryException;
                }
            }
            else
            {
                return;
            }
        }
        else
        {
            return;
        }
    }
    else
    {
        return;
    }
}
```

force GC by calling API: `GC.Collect`

Segment & Generation

Seg 0 (Ephemeral Seg, targeting the short-lived objects):

{Gen 0, Gen 1, Gen 2(maybe)}

Seg 2 (Extended Seg)

{Gen 2 only}

```
> !eeheap -gc
```
shows gen 0 and gen 1 starting address. or the simple command:

```
> !dumpgen 0
> !dumpgen 1
> !dumpgen 2
```

## Roots

detection of objects being referenced:

-   JIT (Just In Time) Compiler. IL to machine code and know the context during intepretation.
-   Stack walker: unmanaged calls
-   Handle table: pointers to pinned reference types on heap
-   Finalize queue: like zombie - application thinks it's dead, but alive to do clean up

probing/detecting, GC marks all as ROOTED.

```
> !gcroot <object heap address>
```

root ---> ref 1 ---> ref 2 ---> ref 3 ---> object

may have false positive on stack.

## Finalization

E.g. managed object wraps a native file handle. When deallocated, the handle is leaked. So need finalization, like destructor in C++.

CLR use finalization queue to record objects with finializer (placed during allocation).

When GC, put finalizable object to **F-Reachable Queue**. So the referenced object (e.g. file handle) is still alive. Processed by **Finalization Thread** during GC.

Problem: Random wake up of finalization thread. Holding huge resource and should be freed immediately. Use `IDisposable` and/or `Close` patterns to do *deterministic cleap up*.

```
> !FinalizeQueue
```

## Reclaiming GC Memory

When object is freed, 3 problems:

1.  DS to manage the free block
2.  Is block free
3.  Heap fragmentation

Compacting & Coalescing

when gen 2 segs are no longer needed, totally free. Grow and shrink with virtual memory APIs with OS:

```
VirtualAlloc
VirtualFree
```

## Large Object Heap

LOH: compacting large objects is expensive. like extension of gen 2: gen 0 ---> gen 1 ---> gen 2 ---> LOH

do not compacting LOH and use free list to manage: **Sweeping**.

Compacting, No. Coalescing, Yes. Just like malloc lab.

LOH has some small objects used by CLR to do management.

## Pinning

when compacting or promoting to next generation, update referencing address. Bad when C# and C++ work together.

pin the object to its address, do not move until released. For GC, fragmentation.

## Debugging Managed Heap Corruptions

corruption: violation of the integrity of the heap.

Bad news: corruption rarely breaks at the point of corruption. usually later.

Best situation: crash happens close th the source of corruption, then no need to do historic back tracking.

What causes a heap corruption to occur? The common cause: not properly managing the memory the app owns:

-   reuse after free (CLR handled: no root)
-   dangling pointers (not easily achieved)
-   buffer overruns (trapped as exception)

but when collaborate with native C++ ...

Corruption without native behavior usually indicates bug in CLR itself.

Access Violation

```
> !VerifyHeap
```

ATTENTION: `verifyheap` may fail when doing GC.

## Debugging Managed Heap Fragmentation

Problem: `OutOfMemory` Exception due to fragmentation.

Common causes: excessive or prolonged pinning. Dev: Pinning must be short-lived when necessary.

## Debugging Out of Memory Exceptions

Task manager: per-process memory information

-   working set: amount of memory used by the process
-   commit size: amount of virtual memory committed by the process
-   paged/nonpaged pool: 

**Windows Reliability and Performance Monitor**

use **Performance counters**, trace log, configuration information as data sources.

```
.Net CLR Memory perf counter
    # total bytes counter
    # total committed bytes counter
```

loader usage increasing: a possible assembly leak.

For non-live postmoterm debugging, no cookbook. Use `eeheap`, `dumpheap`, `dumpdomain` to collect clues.


# Chapter 6. Synchronization

## Synchronization Basics

Windows: **Preemptive** and **Multithreaded** OS.

Multithreaded: run any number of threads concurrently. Single processor: use time quantum and do context switch. Multiple processors: per processor.

Preemptive: any thread can yield control of processor to another thread at any time.

Problem: Dependent multithreading: 2+ threads work together.

## Thread Synchronization Primitives

Windows: Thread Execution Block (TEB): id, last error, local storage, etc.

CLR: Thread

```
> !threads
```

The data structure

```
class Thread
{
    /// alive, aborted, etc - bit mask
    volatile ThreadState m_State;

    /// # locks currently held by the thread
    DWORD m_dwLockCount;

    /// managed id
    DWORD m_ThreadId;

    /// reader/writer lock state
    LockEntry *m_pHead;

    /// reader/writer lock state
    LockEntry m_embeddedEntry;
}
```

No tool to inspect the fields. Need to calculate the bytes offset by hand.

## Events

Event: kernel mode primitive, synchronization object: signaled or nonsignaled. Sync code flow execution between mutiple threads. E.g. `ReadFile` event. T0 call event, T1 (maybe hardware driver) do the thread, T0 do other work, T1 notifies the finish, T0 wait for the signal, T0 resumes continuation.

Nonsignaled --> Signaled: a event occurred. *A Thread* waiting the event is awaken and allowed to continue execution.

Event objects: mannual reset & auto reset. Suppose n threads are waiting for one event.

-   mannual reset: event object is still in signaled state. When explicitly reset, turn to nonsignaled. m(m <= n) threads can be released.
-   auto reset: one thread is released (no longer waiting) before automatically reset to the nonsignaled state. If no threads waiting, remains signaled until first thread tries to wait for the event.

`System.Threading.Event`: wrapper over underlying Windows kernel object. Use `handle` command.

## Mutex

Mutex: kernel mode synchronization constrcut to sync threads: threads within one process & threads across processes.

`System.Threading.Mutex`. still a wrapper of kernel.

## Semaphore

Synchronization object accessible from user mode: exclusive access. Semaphores use **Resource counting**, so X number of threads can access to the resource. E.g. 4 USB ports access to one resource. 4 threads.

## Monitor

`System.Threading.Monitor`: Construct monitoring access to an object and creating a lock on it. **Not** a wrapper for kernel primitives.

```
Monitor.Enter(db1);
// exclusive logic
Monitor.Exit(db1);

lock(object)
{
    // exclusive logic
}
```

`lock` grammar sugar for monitor. Locked object keeps the information in the memory layout to maintain the integrity of the lock.

Stateless.

## ReaderWriterLock (Slim)

Readers > Writers: poor perf because lock is heavily contended. `System.Threading.ReaderWriterLock`: multiple read, one write.

Event handle `_hWriterEvent`, `_hReaderEvent` to give access control to reader and writer queue. State `_dwState`: the internal states of the lock: reader, writer, waiting reader, waiting writer, etc.

`System.Threading.ReaderWriterLock` perf is poor, but correct. `System.Threading.ReaderWriterLockSlim` improve perf.

## Thread Pool

Do not construct and destruct `System.Threading.Thread` for each HTTP request. Use `System.Threading.ThreadPool` managed by the runtime. Default number 250. Min number: number of processors.

```
> !threadpool
```

## Synchronization Internals

### Object Header

Sync block is stored in non GC memory and accessed by index into sync block table. Object header stores the pointer (table index).

The header can be pointer or data. If data is too large to fit in the header, turn it to pointer to sync block table. Difference is the high bits. E.g. `0x0f78734a` is information, `0x0800001` is index `[1]`. If `0x08000000` is `1`, a sync block index. If `0x04000000`, a hash code.

### Sync Blocks

`dd` inspecting the bytes of object (object address `-4`) to get the sync information.

```
> !syncblk <sync block index>
```

### Thin Locks

In thin lock, the header stores only thread id of acquiring thread, no sync block. CLR **infers** that there is a simple spinning lock. If spinning is not finished in short time, create a sync block do wait in ready queue.

## Synchronization Scenarios

### Basic Deadlock

Mutally locked, CPU no execution but only thread switches. 

Task: find the address that different threads are waiting on.

Method 1

T0 `clrstack` to check. If `System.Threading.Monitor.Enter` is in call stack, T0 is trying to acquire a lock. Use `%rip` to find the code and check whick lock is getting acquired.

`syncblk` to check the sync block in object header of the lock address. Find T1 is holding the lock. Check the call stack of T1. Do the same to further investigation.

Method 2

deadlock detection tool:

```
> !dlk
```

`dlk` does not work with thin locks. Need to debug mannually.

## Orphaned Lock Exceptions

## Thread Abortion

`Thread.Abort` making lock not properly released.