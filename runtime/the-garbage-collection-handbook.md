By Richard Jones, Antony Hosking, Eliot Moss

# Chapter 1. Introduction

**Reference**: a pointer to the object (the address in memory of the object). Directly or indirectly.

## 1.1 Explicit deallocation

Mannual managing risks:

1.  Memory be freed prematurely: dangling pointer
2.  Memory leaked: fail to free an object no longer required by the program

## 1.2 Automatic dynamic memory management

**Garbage Collection (GC)**: prevents dangling pointers being created. 

_Memory management is a software engineering issue._ GC uncouples the problem from interface. 

GC is not silver bullet. It tends to reduce the chance of memory leak, but not guarantee to eliminate them.

## 1.3 Comparing garbage collection algorithms

### Safty

Must not reclaim the storage of live objects. Comes with cost, conservative collection.

### Throughput

E.g. mark-sweep collection occasionally performs poor.

### Completeness and promptness

Eventually, all garbage should be reclaimed. Garbage collected in next cycle: floating garbage.

### Pause time

Pause the execution to collect the garbages.

### Space overhead

Per-object space cost; per-heap cost. E.g. only one semispace is available at any time.

### Optimisations for specific languages

Functional languages is good for optimisations.

### Scalability and portability

For multicore hardware.

## 1.6 Terminology and notation

Dijkstra: a GC system is 2 semi-independent parts:

-   Mutator: executes the application code (application thread), allocates objects and mutates the object graph by changing reference fields.
-   Collector: executes the GC code, discovering unreachable objects and reclaims the storage.

Mutator roots: the pointers directly accessible to mutator without going through other objects. Roots are usuall static/global storage, thread-local storage (stack). 

Liveness: An object is live if it will be accessed in future execution of the mutator. GC should never collect live objects. _But liveness is undecidable problem (we cannot predict future)_. Approximate liveness by a decidable property: _Pointer reachability_.  This is a conservative approximation. 

# Chapter 2. Mark-sweep garbage collection

4 fundamental approaches: 

1.  mark-sweep collection
2.  copying collection
3.  mark-compact collection
4.  reference counting

Collectors can combine them in different ways.

Single collector thread, one or more mutator threads. When collector is running, all mutator threads are stopped: **Stop the world**. 

Any automatic memory management system has 3 tasks:

1.  Allocate space for new objects
2.  Identify live objects
3.  Reclaim the space occupied by dead objects

Mark-Sweep allocation:

```cs
void *New()
{
    void *ref = Allocate();
    if (ref == null)
    {
        // heap is full
        Collect();

        ref = Allocate();
        if (ref == null)
        {
            // still full 
            Exception("Out of Memory");
        }
    }

    return ref;
}

exclusive void Collect()
{
    MarkFromRoots();
    Sweep(Heap.Start, Heap.End);
}
```

Use _pointer reachability_ to estimate _true liveness_. It's safe: the dead are true dead, the survived may be dead.

**Mark-Sweep Collection**: indirect collection algorithm. Identifies all live objects and then anything else are garbage. 2 phases:

1.  **Marking Phase**: Collector traverses the graph of objects, starting from roots(_registers, thread stacks, global variables_), marking the objects it finds. This is tracing.
2.  **Sweeping Phase**: Examines every object in heap. Reclaim the unmarked storage.

## 2.1 The mark-sweep algorithm

For collector, mutator do 3 operations: `New, ReadRef, WriteRef`. An object can be marked by setting a bit, in object header or a side table.

Use a work list as queue to manage the nodes being marked. BFS to mark:

```csharp
void MarkFromRoots()
{
    // initialize work list
    workList.Empty();

    foreach (var fld in Roots)
    {
        obj = *fld;

        if (obj != null && obj.IsMarked() == false)
        {
            obj.SetMarked();
            workList.Add(obj);

            // BFS to mark the nodes in work list
            while (workList.IsEmpty() == false)
            {
                void *current = workList.Dequeue();

                foreach (var child in current.GetReferences())
                {
                    if (child != null && child.IsMarked() == false)
                    {
                        child.SetMarked();
                        workList.Add(child);
                    }
                }
            }
        }
    }
}

void Sweep(Object start, Object end)
{
    Object scan = start;
    while (scan < end)
    {
        if (scan.IsMarked() == 1)
        {
            // it's reachable
            scan.UnsetMarked();
        }
        else
        {
            Free(scan);
        }

        scan = NextObject(scan);
    }
}
```

Any unmarked object is unreachable, therefore garbage.

1.  Mark-Sweep does not move objects
2.  Sweeper must be able to find all blocks in the heap

## 2.2 The tricolour abstraction

To describte the state of objects: marked? in the work list? etc. Dijkstra uses tricolor abstraction to trace. 

Objects are:

-   **Black**: presumed live. Collector has finished processing it. Object has been marked, e.g. Root, black.
-   **White**: possibly dead. All nodes has not been detected, white. After sweeping, the white are the garbages.
-   **Grey**: block/object/node first hit, color grey. When it's scanned and children identified, turn black. All nodes in the working queue would be revisited, so grey.

Algorithm invariant: _At the end of each iteration of loop, no reference from black to white. Thus any white object that is reachable must be reached from a grey object._

## 2.3 Improving mark-sweep

Temporal locality of mark-sweep collection is poor. MSGC reads and writes the object's header just once. 

## 2.4 Bitmap marking

Store mark bits in a separate bitmap table to improve locality. Consider alignment, divide heap into blocks, store the per-block bitmaps at the fixed place inside the block. But multiple mutators contending ... So store the bitmap to the side, use a table indexed by block, e.g. hashing. This avoids both paging and cache conflicts.

Use byte maps instead of bit maps to avoid racing. Otherwise bitmap need synchronization to protect each bit.

The potential advantages of marking bitmap: Update marking status will have no affect to the object. To modify fewer bytes, dirty fewer cache lines, so less data would be WB to memory.

Bitmap marking was desigend for C & C++. 

## 2.5 Lazy sweeping

Marking phase complexity: O(L), L is the size of live data in heap. O(H), H is the size of the heap. Actually, graph traversing O(L) leads to unpredictable memory access pattern. 

One way to improve cache behavior in sweeping phase: prefetch objects. 

# Chapter 3. Mark-compact garbage collection

Fragmentation can be a problem for non-moving collectors. Allocators may alleviate this problem by storing small objects of the same size together in blocks. 

Compacting live objects in the heap in order to eliminate external fragmentation. 2 strategies: in-place compacting into one end & copying from one region to another.

Mark-compact phases:

1.  Marking phase
2.  Compacting phase: compact live data by relocating objects and updating the pointer values of all live references to the objects that have moved.

Compaction order: rearrange objects in heap in 3 ways:

1.  Arbitrary: relocated without regard for the original order. Fast and simple, but poor spatial locality.
2.  Linearising: relocated adjacent to related objects. E.g. ones to which they refer, which refer to them.
3.  Sliding: slid to one end of the heap, squeezing out garbage.

Usually arbitrary + sliding. 

## 3.1 Two-finger compaction

Best for compacting regions containing objects of a **fixed object size**. A 2-Pass, arbitrary order algorithm. `object < low`: live object; `high < object`: garbage. In the middle: unknown.

```cs
void Compact()
{
    // Relocate
    object low = Heap.Start;
    object high = Heap.End;

    while (low < high)
    {
        while (low.IsMarked() == true)
        {
            low.UnsetMarked();
            low = low.IncreaseObject();
        }
        // now low is pointing to an unmarked object

        while (high.IsMarked() == false && low < high)
        {
            high = low.DecreaseObject();
        }
        // now high is pointing to a marked object

        if (low < high)
        {
            high.UnsetMarked();
            // because this is fixed size object
            CopyFromTo(from: high, to: low);

            // record the moved new low address in high free block
            high.WriteAddress(low);

            low = low.IncreaseObject();
            high = high.DecreaseObject();
        }
    }

    object waterMark = high;

    // update reference

    // updating references in roots
    foreach (object ptr in Roots)
    {
        if (ptr.PointerValue > waterMark)
        {
            // this pointer is moved to low space
            object moved = (object)ptr.PointerValue;
            ptr.PointerValue = moved.PointerValue;
        }
    }

    // updating references in low space
    low = Heap.Start;
    while (low < Heap.End)
    {
        foreach (object ptr in low.Referencing())
        {
            // ptr is referencing another address
            // e.g. class A { private class B b; } a;
            // a - low; b - ptr
            if (ptr.PointerValue > waterMark)
            {
                // this pointer is moved to low space
                object moved = (object)ptr.PointerValue;
                ptr.PointerValue = moved.PointerValue;
            }
        }
        low = low.IncreaseObject();
    }
}
```

## 3.2 The Lisp 2 algorithm

Can have parallel form. Compaction are usually slow, but Lisp 2 is fast. Drawback: every object needs an additional full-slot field in header to store the address to which the object is to be moved. Also used for mark-bit.


```cs
void Compact()
{
    // 3 passes

    // compute locations
    address slow = Heap.Start;
    object fast = Heap.Start;

    while (fast < Heap.End)
    {
        if (fast.IsMarked() == true)
        {
            // live object
            // fast is the lived object
            // slow is the location fast would be moved
            // fast recoreds this place in its header
            // And it's true that moved <= address(object)
            fast.HeaderBytes.WriteBytes(slow);
            slow += sizeof(fast);
        }
        fast = fast.IncreaseObject();
    }

    // update references first
    // update roots
    foreach (object ptr in Roots)
    {
        if (ptr.PointerValue != null)
        {
            ptr.PointerValue = ptr.PointerValue.HeaderBytes;
        }
    }
    // update fields
    object low = Heap.Start;
    while (low < Heap.End)
    {
        if (low.IsMarked() == true)
        {
            foreach (object ptr in low.Referencing())
            {
                if (ptr.PointerValue != null)
                {
                    ptr.PointerValue = ptr.PointerValue.HeaderBytes;
                }
            }
        }
        low = low.IncreaseObject();
    }
    
    // relocate
    low = Heap.Start;
    while (low < Heap.End)
    {
        if (low.IsMarked() == true)
        {
            address moved = low.HeaderBytes;
            CopyFromTo(from: low, to: moved);
            moved.UnsetMarked();
        }
        low = low.IncreaseObject();
    }
}
```

Parallel compaction: divide the heap into blocks.

## 3.5 Issues to consider

-   Is compaction necessary?

Mark-sweep: non-moving collector, vulnerable to fragmentation. Most run time environment use compaction.

-   Throughput costs of compaction

For throughput, compacting < non-compacting: more passes over the heap. Trade-off: Run mark-sweep as long as possible, switching to mark-compact only when metrics detects that it's worth compacting.

-   Long-lived data

Long-lived or immortal data will accumulate near the heap start. Not necessary to collect them. So generational collectors move the long lived to another space.

-   Locality

Mark-compact sequentially may preserve the allocation order of objects, so maybe good for cache locality.

-   Limitations of mark-compact algorithms

To what extent is compaction necessary? Most compaction algorithms preclude the use of interior pointers except Two-Finger.
