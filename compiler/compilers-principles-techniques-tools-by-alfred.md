# Chapter 7. Run-Time Environments

## 7.5 Introduction to Garbage Collection

garbage: data cannot be referenced

### 7.5.1 Design Goals for Garbage Collectors

GC knows the type and address of object.

#### A Basic Requirement: Type Safety

automatic GC: type-safe languages. type should be determined at compile or run time (JIT). C & C++ are not type safe - we cannot tell if the object is referenced: when do `(type_t *)int_value`. thus no storage can be recycled safely.

#### Performance Metrics

GC is expensive. 

### 7.5.2 Reachability

**Root set**. static field, stack, etc. In runtime, referene may be in registers. compiled code: offset can be illegal.

once one object becomes unreachable, it cannot become reachable again.

reachable set may change by:

1.  object allocation - new reachability
2.  parameter passing and returning values - propagate reachability
3.  reference assignments - terminate reachability: `ref_u = ref_v`
4.  producer returns - terminate reachability

2 methods to find unreachable objects:

1.  Tracking when reachable objects turn unreachable. E.g. **reference counting**
2.  Periodically locate all reachable objects. E.g. mark-sweep. Trace-based algorithms

### 7.5.3 Reference Counting Garbage Collectors

object can be freed when reference counting decrease to 0. object has a field for reference count.

maintain reference count:

1.  object allocation. init to `ref_count = 1`
2.  parameter passing. increment: `ref_count += 1`
3.  reference assignment. For `ref_u = ref_v`, `ref_count -= 1` by `ref_u`, `ref_count += 1` by `ref_v`
4.  procedure returns. `ref_count -= number of referencing local variables in stack`
5.  transitive loss of reachability. `A {B, C}`. When `A_ref_count = 0`, `B_ref_count -= 1` and `C_ref_count -= 1`

cons: cannot collect **cyclic referencing**. and it's expensive.

## 7.6 Introduction to Traced-Based Collection

trace-based collectors run periodically to find unreachable objects.

### 7.6.1 A Basic Mark-and-Sweep Collector

stop-the-world find all unreachable objects and do free.

reached-bit. 

mark phase: for each root, do DFS

### 7.6.2 Basic Abstraction

memory chunck types:

-   Free: ready to be allocated. No reachable object here
-   Unreached: the reachability has not been detected. default garbage. starting status for each GC round
-   Unscanned: reachable objects can be Unscanned or Scanned. Unscanned: we know it's reachable, but its pointers have not yet been scanned.

-   Scanned: reachable objects, and its pointers are all reachable.

When Unscanned set is empty, reachability is calculated. Unreached set is truly unreachable.

### 7.6.3 Optimizing Mark-and-Sweep

basic sweeping is expensive. 

Baker's improvement: keep a list of all allocated objects. no reached-bit, but state bit indicating: free, unreached, unscanned, scanned.

### 7.6.4 Mark-and-Compact Garbage Collectors

relocating: eliminate memory fragmentation. move all reachable to one end. updating every reference within reachable objects. 

-   compacting in plance: mark-and-compact collector
-   copying collector, move from one region to another (SICP Lisp stop-and-copy)

core: updating references

### 7.6.5 Copying collectors

Cheney's stop-and-copy

memory partitioned into 2 semi-spaces, A and B. 

### 7.6.6 Comparing Costs

Cheney's stop-and-copy: expensive for large objects and long-lived objects. 

## 7.7 Short-Pause Garbage Collection

-   divide the work in time, mutation and evolution. *incremental GC*
-   divide the work in space, by collecting subset of objects. *partial GC*

**generational GC**: partitioned by how long they have lived.  **train GC**: applied to more mature objects.

### 7.7.1 Incremental Garbage Collection

incremental gc: conservative. do not collect all garbage in one GC round. garbage left after collection: floating garbage. 

it's expensive when GC loses a reference to the object. so do incremental collection.

#### Simple Incremental Tracing

overwritten reference

### 7.7.3 Partial-Collection Basics

most objects die young: within a few million instructions. so cost effective to GC new objects frequently. objects survive one GC are likely to survive more. 

-   generational GC: work frequently on young objects.
-   train algorithm: do not focus on young objects, but limit the pauses due to GC.

combining these 2, generational GC and promotion to heap managed by train algorithm.

### 7.7.4 Generational Garbage Collection

collects younger generations more offten


# Chapter 10. Instruction-Level Parallelism

## 10.1 Processor Architectures

### 10.1.1 Instruction Pipelines and Branch Delays

1.  IF: instruction fetch
2.  ID: instruction decode
3.  EX: execution
4.  MEM: memory access
5.  WB: write back

### 10.1.2 Pipelined Execution

memory access (with cache or not) takes several clocks to return data to regsiter. 5 stages, 5 instructions in flight at the same time.

CPU dynamically detect instruction dependency && automatically stall the execution if operands are not available. compiler: insert `NOP` instructions to assure the results are available when needed.

### 10.1.3 Multiple Instruction Issue

multiple-issue machines' parallelism:

-   parallelism managed by software: VLIW (very long instruction word machine)
-   managed by hardware: Superscalar machines.

CPU executes instructions out-of-order. operations are blocked until all depending values have been produced.

## 10.2 Code-Scheduling Constraints





