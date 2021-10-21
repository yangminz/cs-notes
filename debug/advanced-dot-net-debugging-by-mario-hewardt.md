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

