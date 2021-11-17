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

# Chapter 4. Copying garbage collection

Coping compacts the heap, fast allcation. Requires only one single pass over the live objects. Chief drawback: reduces the size of the available heap by half.

## 4.1 Semispace copying collection

Heap = _fromspace_ + _tospace_, 2 semispaces. At the end of collection, all live objects are placed in the dense prefix of tospace. Flipping the 2 semispaces.

Copied but not yet scanned objects are grey. Each referencing field in grey objects is (1) null, or (2) pointing to fromspace object, not yet copied. After initialization, copy the roots to _tospace_, they are grey. 

```cs
public class CopyCollector
{
    private Address ToSpaceStart;
    private Address ToSpaceEnd;

    private Address FromSpaceStart;
    private Address FromSpaceEnd;

    private int SemiSize;
    
    private Address FreePointer;
    private Address UnscannedStart;

    public void Initialize(Heap heap)
    {
        this.SemiSize = (heap.End - heap.Start) / 2;

        this.FromSpaceStart = heap.Start;
        this.FromSpaceEnd = heap.Start + this.SemiSize;

        this.ToSpaceStart = heap.Start + this.SemiSize;
        this.ToSpaceEnd = heap.End;

        this.FreePointer = this.ToSpaceStart;
    }

    public Address Allocate(int size)
    {
        Address newFree = this.FreePointer + size;
        if (newFree > this.ToSpaceEnd)
        {
            // out of memory
            return null;
        }
        this.FreePointer = newFree;
        return this.FreePointer - size;
    }

    [ThreasSafe]
    public void Collect()
    {
        // used by collector thread
        this.Flip();

        // initialize the pointer
        this.UnscannedStart = this.ToSpaceStart;

        // copy the roots
        foreach (object obj in Roots)
        {
            this.Process(obj);
        }

        // copy fields
        while (this.UnscannedStart < this.FreePointer)
        {
            this.Scan(this.UnscannedStart);
            this.UnscannedStart += sizeof(this.UnscannedStart);
        }
    }

    public void Flip()
    {
        SwapValue(this.ToSpaceStart, this.FromSpaceStart);
        SwapValue(this.ToSpaceEnd, this.FromSpaceEnd);
        this.FreePointer = this.ToSpaceStart;
    }

    public void Scan(object obj)
    {
        // scan one object
        foreach (object ptr in obj.ReferencingPointers())
        {
            this.Process(ptr);
        }
    }

    public void Process(Pointer ptr)
    {
        // obj is not scanned, ptr is null or in from space
        if (ptr.PointerValue != null)
        {
            // ptr.PointerValue is in from space
            object fromObj = (object)ptr.PointerValue;
            object toObj = null;

            if (fromObj.ToSpaceAddress == null)
            {
                // not copied (not marked)
                // copy this object to from space
                toObj = this.FreePointer;
                this.FreePointer += sizeof(fromObj);
                CopyFromTo(from: fromObj, to: toObj);

                // record the tospace address in from space slot
                fromObj.ToSpaceAddress = toObj;

                // add toObj to working list
            }
            else
            {
                // copied & marked
                // use the redirected (forwarding) address in to space
                toObj = fromObj.ToSpaceAddress;
            }

            ptr.PointerValue = toObj;
        }
    }
}
```

It's essential that the collectors preserve the topology of live objects in the tospace. Achieved by storing the address of each tospace object as a _forwarding address_ in its old-fromspace replica when it's copied.

Unlike most mark-compact collectors, semspace copying does not require extra headers. Use slots in old-fromspace.

-   Work list implementations

Work list can be implemented in different ways. Cheney uses the grey objects in tospace as FIFO queue. Cheney adds only one pointer `UnscannedStart`. `UnscannedStart : FreePointer` are precisely the copied but unscanned objects. 

## 4.3 Issues to consider

Pro: fast allocation and zero fragmentation. Easy to implement. Trade-off: copying collection uses 2x virtual memory.

-   Allocation

Compacted heap: allocation is fast. Sequential allocation good with multithreading.

-   Space and locality

Disadvantage: need to maintain copy reserve. Only half is used. consequence: more garbage collection cycles than other collectors. Performance depends on trade-off between mutator and collector, user process, heap space available.

Calculation, H be the total size of heap, L be the number of lived data. 

Mark-Copy time complexity $c L$

Mark-Sweep time complexity $a L + b H$

Mark-Copy memory reclaimed $\frac{H}{2} - L$

Mark-Sweep memory reclaimed $H - L$

Suppose that $r = \frac{L}{H}$, the ratio of live memory. Then the memory reclaiming speed = memory reclaimed / algorithm time:

Mark-Copy speed: $\frac{2cr}{1-2r}$

Mark-Sweep speed: $\frac{ar + b}{1-r}$

-   Moving objects

Sometimes objects cannot be moved. E.g. type accuracy, it's unsafe to modify the reference. Another situation, object passed to unmanaged code, reference should not change. 

Copying is more expensive than marking. Copying large objects is more costy, leading poor perf. 2 strategies: 1. Do not copy them; 2. Copy them virtually instead of physically:

1.  Holding large objects on a linked list, updating pointers instead of physical memory copying
2.  Allocating large objects on their own virtual memory pages which can be remapped.

# Chapter 5. Reference counting

Invariant: an object is live iff the number of references to that object > 0. This count can be stored in object's header. Reference counts are incremented or decremented when reference is created or destroyed. 

```cs
public class RCObject
{
    private Address SelfAddress;
    public int RefCount;

    public void RCObject()
    {
        this.SelfAddress = Allocate();
        if (this.SelfAddress == null)
        {
            throw new OutOfMemoryException();
        }

        this.RefCount = 0;
    }

    [ThreadSafe]
    public void ReferencedBy(Address stackAddr)
    {
        // src = this object
        // increase counter of the new target
        // decrease counter of the old object
        // A x = new A();       // x.cnt = 0
        // A y = new A();       // y.cnt = 0
        // A z = x;             // x.cnt = 1; y.cnt = 0; z does not have cnt because it's on stack
        // A z = y;             // x.cnt -= 1; y.cnt += 1;
        this.IncreaseCounter();

        RCObject oldObject = (RCObject)stackAddr.PointerValue;
        if (oldObject != null)
        {
            oldObject.DecreaseCounter();
        }

        stackAddr.PointerValue = this.SelfAddress;
    }

    public void IncreaseCounter()
    {
        this.RefCount += 1;
    }

    public void DecreaseCounter()
    {
        this.RefCount -= 1;

        if (this.RefCount == 0)
        {
            foreach (RCObject child in this.Referencing())
            {
                child.DecreaseCounter();
            }

            Free(this.SelfAddress);
        }
    }
}
```

## 5.1 Advantages and disadvantages of reference counting

Pros:

1.  Amortized cost is low. Cost of management is distributed throughout the computation.
2.  Recycle memory as soon as it's garbage (actually not always desirable)
3.  Good when heap is nearlly full, without need of headroom.
4.  Locality may be no worse than client program.
5.  Can be implemented without knowledge of run-time system.
6.  Can run in distributed system

Cons:

1.  Time overhead when read and write reference. So the naive algorithm above is not practical.
2.  Counter manipulations are atomic actions. Need to prevent races to updating pointer.
3.  Read-only operations is no more read-only since the counter needed to be updated.
4.  Cannot reclaim cyclic data structures. They are unreachable. E.g. self-referential structures.
5.  Internal fragmentation can be high for heap full of small objects.
6.  Recursive decreasing reference can be long time.

The 2 major problems:

1.  Counter updating cost
2.  Cyclic garbage

A common solution: _stop-the-world pause_.

## 5.2 Improving efficiency

2 ways to improve efficiency:

1.  Reduce the number of barrier operations
2.  Replace expensive synchronization operations with cheaper unsynchronized ones

Several kinds of solutions:

1.  **Deferral** Use fine-grain increment. Defer the identification of garbage to the end of some period to eliminate barrier operations.
2.  **Coalescing** Remove some unnecessary, temporary counter updating. Compiler & runtime environment can both help.
3.  **Buffering** Buffers _all_ counter updating for later processing.

Problem: counter is a global state, but optimize on local state. So the common approach: divide execution into periods or epochs, seperated by _stop-the-world_ pauses.

## 5.3 Deferred reference counting

Most high-performance Reference Counting systems use deferred RC. Add 0-count object to **Zero Counting Table (ZCT)** instead of reclaim them immediately. Others are the same as before. ZCT can be implemented by bitmap or hash table.

ZCT is the table for zero counting, but maybe-live objects. Because there may be uncounted reference from stack to the 0-counting object. 

When the process is going to OOM, must do collection. All threads are stopped, mark & sweep for ZCT. Each ZCT object is checked to see if its true RC to see if it's garbage. ZCT object is alive iff there is reference from Roots to it. So scan the roots and mark by incrementing the RC.

```cs
public class Collector
{
    [ThreadSafe]
    public static void Collect()
    {
        foreach (object obj in Roots)
        {
            // this is not increasing counter
            // it is using counter as mark bit
            obj.IncreaseCounter();
        }

        Collector.SweepZCT();

        foreach (object obj in Roots)
        {
            // unmark
            obj.DecreaseCounterToZCT();
        }
    }

    public static void SweepZCT()
    {
        while (ZCT.IsEmpty() == false)
        {
            object obj = ZCT.Pop();

            if (obj.RefCount == 0)
            {
                // it's true 0-counting object now
                foreach (RCObject child in this.Referencing())
                {
                    // now recursively decrease the counters of child
                    child.DecreaseCounter();
                }
            }

            Free(obj);
        }
    }
}

public class DeferredRCObject : RCObject
{
    public void DeferredRCObject()
    {
        this.SelfAddress = Allocate();
        if (this.SelfAddress == null)
        {
            Collector.Collect();

            this.SelfAddress = Allocate();
            if (this.SelfAddress == null)
            {
                throw new OutOfMemoryException();
            }
        }

        this.RefCount = 0;
        ZCT.Add(this.SelfAddress);
    }

    [ThreadSafe]
    public void ReferencedBy(Address stackAddr)
    {
        // src = this object
        if (Roots.Contains(stackAddr))
        {
            stackAddr.PointerValue = this.SelfAddress;
        }
        else
        {
            Lock.Acquire();
            
            this.IncreaseCounter();
            ZCT.Remove(this.SelfAddress);
            
            RCObject oldObject = (RCObject)stackAddr.PointerValue;
            oldObject.DecreaseCounterToZCT();
            stackAddr.PointerValue = this.SelfAddress;

            Lock.Release();
        }
    }

    public void DecreaseCounterToZCT()
    {
        this.RefCount -= 1;

        if (this.RefCount == 0)
        {
            // So the childs' RC are not updated
            // the cost is reduced
            // but there ZCT object may not be true garbage
            // the counter of objects in ZCT can still get updated
            ZCT.Add(this.SelfAddress);
        }
    }
}
```

## 5.4 Coalesced reference counting

Still the question of how to reduce the counting overhead of writing objects' pointer fields. Suppose object X has field F, F is O0, O1, O2, ..., On. Then the counter is:

```
X.F = O[1];   //  O[0].counter --; O[1].counter ++;
X.F = O[2];   //  O[1].counter --; O[2].counter ++;
X.F = O[3];   //  O[2].counter --; O[3].counter ++;
...
X.F = O[n];   //  O[n-1].counter --; O[n].counter ++;
```

Finally, `O[0]--, O[n]++`.

So log the updated counters as dirty, the unchanged counters as clean. 

## 5.5 Cyclic reference counting

Simple solution: combine reference counting + tracing collection. 

Most common solution: trial deletion. Key point: tracing is only needed for possible cycles.

1.  In any garbage pointer structure, all reference counts must be due to internal pointers (pointers between objects within the structure)
2.  Garbage cycles can arisze only from a pointer deletion that leaves a reference count greater than zero

3 phases:

1.  The collector traces partial graphs, starting from possible cycle members, decrementing counts due to internal pointers. Visited objects are colored grey.
2.  Check the count of each node in these subgraphs: if > 0, then ex-subgraph reference exists, so will not be cycle members, color black. Else, color white.
3.  Reclaim the white, they are all garbages.



