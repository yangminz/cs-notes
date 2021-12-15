4th edition By Andrew S. Tanenbaum and Herbert Bos

# Chapter 2. Processes and Threads

Process: abstraction provided by OS. pseudo concurrent operation even only 1 CPU core (`%rip`).

## 2.1 Processes

multiprogramming system: CPU switches from process to process, running for *10 or *100 ms.

sequential process: conceptual model to think parallelism

### 2.1.1 The Process Model

Process: instance of executing programe: program counter, registers, variables. Each process has its own virtual CPU.

Critical real-time requirements.

### 2.1.2 Process Creation

events cause processes to be created:

1.  System initialization: when booted, several processes: foreground, background (**daemons**).
2.  Running processes created (`fork`)

A new process is created by having an existing process execute a creation syscall. `fork`

`fork`: exact clone of the calling process. parent & child have the same memory image, same environment strings, same open files.

`execve` change memory image and run a new program. allow child to manipulate its file descriptors after `fork` but before `execve`: redirection of standard I/O/E.

Windows `CreateProcess`: creation process and loading program file at the same time.

copy-on-write: create 2 distinct address spaces. some may share the `.text` section. Windows: different from the start.

### 2.1.3 Process Termination

1.  Normal exit (voluntary)
2.  Error exit (voluntary)
3.  Fatal error (involuntary) - self handle: signaled (interrupt) instead of terminated
4.  Killed by another process (involuntary) `kill` and `TerminateProcess`: how to handle the orphans

### 2.1.4 Process Hierarchies

parent - child

a process & all descendants ==> process group. each process can catch signal, ignore signal, take default action (be killed by the signal)

`init` - in boot image. on start: detect num of terminals from a file. Then fork `shell` for each terminal, waiting login. `init` the root (process id 1)

Windows: **no process hierarcy**. parent gets a handle to control the child. the handle can point to any other process.

### 2.1.5 Process States

process blocked, cannot continue, typically waiting for something like input.

1.  Running (actually using the CPU at that instant).
2.  Ready (runnable; temporarily stopped to let another process run). like running, but no CPU resource for the process.
3.  Blocked (unable to run until some external event happens).

-   Running -> Blocked: When process cannot continue. e.g. `pause()` syscall; read from file and input not ready, automatically blocked.
-   Running -> Ready: schedule to give up due to time slice
-   Ready -> Running: schedule to run due to time slice
-   Blocked -> Ready: external event waited happens.

Each process is a state machine. Consider C# `async` and `await` creates a state machine.


### 2.1.6 Implementation of Process

OS: process table, entry: process control block, PCB.

PCB: program counter, stack pointer, memory alloation, files, necessary information to switch between Runnning, Ready, Blocked.

Associated with each I/O class is a location (typically at a fixed location near the bottom of memory) called the **interrupt vector**. It contains the address of the interrupt service procedure.

context pushed onto the current stack by the interrupt hardware. program counter then jumps to the address in interrupt vector. -- hardware's work. Then software

Interrupt:
1.  saving the registers to PCB
2.  Remove info on stack pushed by interrupt
3.  set stack pointer to a temporary stack used by process handler - assembly not C
4.  C procedure: for the interrupt. do job.
5.  scheduler called to run next.
6.  control passed back to assembly code to load up registers and memory map for the scheduled process.

after each interrupt the interrupted process returns to precisely the same state it was in before the interrupt occurred.

## 2.2 Threads

multiple threads in same address space running in quasi-parallel.

### 2.2.1 Thread Usage

why: multiple activities are going on at once.

share address space and data among themselves

thread: light and faster

### 2.2.2 The Classical Thread Model

process model: resource grouping & execution

resource grouping: the address space, etc.

execution: thread: registers, program counter, stack, etc. 

# Chapter 3. Memory Management

## 3.5 Design Issues for Paging Systems

### 3.5.7 Mapped Files

shared lib: a special case of memory-mapped files. **syscall to map a file onto a portion of its virtual address space**. In implementations, when mapping, no pages are in memory (so the page table only records the mapping to disk offset? how do I distinguish swap space & normaly file since they are not encoded in the same way???). when the page is referenced, allocate as demand paging.

provide an alternative model for I/O: **files can be accessed as a big byte array in memory**.

# Chapter 5. Input/Output

OS controls all I/O devices: issue commands to the devices, catch interrupts, handle errors. So OS must provide an interface between the device & rest of OS. 

## 5.1 Principles of I/O Hardware

### 5.1.1 I/O Devices

3 categories of devices:

1.  Block device: Transfer data in blocks structure (512 Bytes to 65536 Bytes). The structure is addressable. Hard disks, USB sticks, etc. File system deals with abstract block devices.
2.  Character device: Transfer character stream without any structure. Not addressable. Pritners, network interfaces, mice, and other not disk-like devices.
3.  Timer/Clock.

### 5.1.2 Device Controllers

I/O units: mechanical component (the device itself) & electronic component (device controller or adapter). Controller can handle 2, 4, 8, etc identical devices.

### 5.1.3 Memory-Mapped I/O

Each controller has control registers to communicate with CPU. And data buffer to transfer data. Problem: How CPU do the communication?

1.  Each control register assigned an **I/O port number**. CPU uses special I/O instructions to r/w the port. Early computers work in this way. This makes the address spaces different for VM & I/O.
2.  Map all control registers into (kernel) memory space, each control register assigned a unique memory address. I.e. **Memory-Mapped I/O**. 

In hardware implementation, when CPU wants to read from device, (from memory or I/O port), CPU write address to bus's _address line_ and signal READ to bus' _control line_. Then DRAM or I/O device gives response.

Memory-Mapped I/O:

Pros:

1.  No special I/O instructions for r/w device control registers, so C/C++ code is still good.
2.  No special protection mechanism is needed for user process.
3.  Old instructions can reference control registers. 

Cons:

1.  Caching a device control register is very bad. May not sync with physical device. Page table should disable caching for this part.
2.  Multiple data bus: CPU - DRAM, CPU - Devices, making the memory address complex in the only one address space. Put a snooping device on memory bus to redirect the I/O related reference.

### 5.1.4 Direct Memory Access

? For block device ?

CPU must address the device controllers to transfer data with them (may not implement memory-mapped I/O). Use **DMA, Directed Memory Access** to effectively use CPU time (time multiplexing between CPU & device) by reducing CPU copying time.

Hardware should have a DMA controller. E.g. integrated into disk controllers, etc. A separate DMA controller is needed for each device. The physical location is not important, DMA controller must have access to the _system bus_ indepedent of CPU.

CPU programs the DMA controller by setting its registers so it knows which disk area to transfer from disk into DMA controller buffer. ...

Bus can run in 2 modes: (1) word-at-a-time mode, (2) block mode. DMA controllers can also run in 2 modes. (1) **Cycle Stealing**: DMA may steal cycle of bus from CPU, CPU needs to wait for DMA controller. (2) **Burst Mode**: DMA tells the device to acqurie the bus and issue multiple transfers, then release the bus. 

Why disk first reads data into disk controller buffer before DMA starts?

1.  Verify the checksum before transfer
2.  Disk data to disk controller buffer speed is constant, but bus transfer speed is unstable.

But if DMA is slower than CPU, it's no good to use DMA.

### 5.1.5 Interrupts Revisited

1.  Device finished the work, asserts a sginal on bus line
2.  Interrupt controller chip detects this signal. If no other pending interrupt, then handle this interrupt immediately. Else, this signal is ignored. The device continues signals to CPU until it's ACKed.
3.  Interrupt controller puts a number on address line to specify the device, and signals interrupt to CPU.

The above is hardware's work. Now OS's work:

When CPU is interrupted (check before/after each instruction execution), CPU use the number on address link as index into **interrupt vector table** to go into the handler routine. 

CPU use one control register to point to interrupt vector table. The interrupt service ACKs the interrupt by writing to one of the interrupt controller's I/O ports. Consider avoiding **race condition** here.

Before start interrupt handling, hardware should save the hardware context (trap). At extreme, all visible registers may be saved.

To where save this hardware context? One option, save them to CPU's internal registers (TSS?). But the registers can be overwritten by another interrupt. May lost interrupts & data.

So most CPU save the information on stack. Not user stack, but kernel stack. But switch into kernel mode **may** need to change MMU contexts, and invalidates the cache & TLB. 

#### Precise and Imprecise Interrupts

Old architecture, CPU checks pending interrupts after each instruction execution. But how about pipelined & superscalar parallel CPUs? 






# Chapter 10. Case Study 1: Unix, Linux, and Android

## 10.3 Process in Linux

### 10.3.1 Fundamental Concepts

Each process runs a single program and initially has a single thread of control - only one program counter = the next instruction.

`fork` syscall creates exact copy of the parent process. They have their own private memory images. COW

open files are shared between parent and child. change to file is visible between the 2 processes.

`fork` return: 0 for child and child's pid for parent. implemented by setting `%rax` register.

Process communication: messge passing. e.g. create channels namedpipes.

```
sort < f | head
```

process send **signal** to another process. user-handled signals or default behavior (mostly killed by signal)

### 10.3.2 Process-Management System Calls in Linux

parent `waitpid` for the child to finish - just waits until the child terminiates (any child if more than one exists).

`execve` - most complex system call

`exit` - error conditions. parent waiting will be awaken.

Porcess exits and parent has not yet waited for it, the process become **zombie**, the living dead. adopted by `init`.

several syscalls are related to signals. 

### 10.3.3 Implementation of Processes and Threads in Linux

each process: user part that runs the user program. when do a syscall, traps to kernel mode and run kernel context, with a different memory map and full access to all machine resources. **Still the same thread, but more power, its own kernel mode stack and kernel mode program counter**.

`task_struct`: any execution context. `task_struct` is resident in memory at all times, pinned, not swappable. *It's possible for a process to be sent a signal when it's swapped out, it's not possible for it to read a file.* So information about signals must be in memory all the time.

1.  **Scheduling parameters**. Process priority, amount of CPU time consumed recently, amount of time spent sleeping recently. Together, these are used to determine which process to run next.
2.  **Memory image**. Pointers to the text, data, and stack segments, or page tables. If the text segment is shared, the text pointer points to the shared text table. When the process is not in memory, information about how to find its parts on disk is here too.
3.  **Signals**. Masks showing which signals are being ignored, which are being caught, which are being temporarily blocked, and which are in the process of being delivered.
4.  **Machine registers**. When a trap to the kernel occurs, the machine registers (including the floating-point ones, if used) are saved here.
5.  **System call state**. Information about the current system call, including the parameters, and results.
6.  **File descriptor table**. When a system call involving a file descriptor is invoked, the file descriptor is used as an index into this table to locate the in-core data structure (i-node) corresponding to this file.
7.  **Accounting**. Pointer to a table that keeps track of the user and system CPU time used by the process. Some systems also maintain limits here on the amount of CPU time a process may use, the maximum size of its stack, the number of page frames it may consume, and other items.
8.  **Kernel stack**. A fixed stack for use by the kernel part of the process.
9.  **Miscellaneous**. Current process state, event being waited for, if any, time until alarm clock goes off, PID, PID of the parent process, and user and group identification.

When `fork`:

1.  malloc new `task_struct` and `mm_struct` for child, new PID, `thread_info` (fixed offset from the process's end-of-stack)
2.  child set up memory map, shared access to parent's files, registers
3.  store `task_struct` at a fixed location

copying memory is expensive: COW. **Protection Fault** instead of page fault.

child running, do `exec` syscall, locate, verify, load assembly, *release the old address space and page table*.

Then create new address space. Set up new page tables: only one stack page. **But address space is backed by assembly on disk** when the new process run, immediately get a **page fault**, caused by *the first page of text code from assembly (rip)*. Nothing needed to be loaded in advance. (? so assembly memory mapping is?: assembly loaded from disk to main memory pages, but no page table mapping?)

Finally, arguments & environment strings copied to the new stack, signals reset (cleared), registers initialized to zeros (unrelated, of course we need rip & rsp). then execute the first assembly instruction.

Fork:

```
Allocate child’s task structure
Fill child’s task structure from parent
Allocate child’s stack and user area
Fill child’s user area from parent
Allocate PID for child
Set up child to share parent’s text
Copy page tables for data and stack
Set up sharing of open files
Copy parent’s registers to child
```

Exec

```
Find the executable program
Verify the execute permission
Read and verify the header
Copy arguments, environ to kernel
Free the old address space
Allocate new address space
Copy arguments, environ to stack
Reset signals
Initialize registers
```

#### Threads in Linux

Historically, processes were resource containers and threads were the units of execution.

Classically, when a new thread was created, the original thread(s) and the new one shared everything but their registers. Linux `clone` fine-grained resource sharing by bit map flags.



## 10.4 Memory Management in Linux

barely changed in history.

### 10.4.1 Fundamental Concepts

Text, data, bss segment. 1950s, self-update text. too complex, then read-only.

bss, uninitialized data, actually just an optimization.

Linux: static **zero page**, write-protected page full of zeros. When write, copy-on-write.

shared text segments. data and stack are not shared except after a fork. 

Access file data through **memory-mapped files**. map a file onto a portion of process's address space so the file can be r/w as byte array in memory. makes the random access to file easier than I/O syscalls `read` & `write`. Shared libraries are accessed in this way.

2 processes map the same file, write to file is visible to the others: multiple processes sharing memory. 

### 10.4.2 Memory Management System Calls in Linux

```
brk()
mmap()
unmap()
```

### 10.4.3 Implementation of Memory Management in Linux

64-bit X86 machine: 48 bits for addressing: kernel space and user space. *address space is created when the process is created and is overwritten on an `exec` syscall.*

#### Physical Memory Management

not all physical memory can be treated identically, especially with respect to I/O and virtual memory. (UMA vs NUMA? different memory controllers)

1.  `ZONE_DMA` and `ZONE_DMA32`: physical pages used for DMA
2.  `ZONE_NORMAL`: normal and regularly mapped pages
3.  `ZONE_HIGHMEM`: pages with high-memory address, not permantly mapped

above zones are architecture dependent.

Kernel and memory map are pinned in memory, never paged out. the rest are divided into page frames, each containing text, data, stack, page table, or free list. 

Linux maintains array of page descriptors: each descriptor `page` for each physical page frame. The array: `mem_map`.

```
// physical page descriptor
typedef struct page
{
    // the address space this page belongs to
    struct address_space *mapping;

    // doubly-linked list pointers
    // if this page is free
    // linked with other descriptors as free list
    struct page *prev;
    struct page *next;
} mem_map_t;

// 1/128 of physical memory
mem_map_t mem_map[NUM_PHYSICAL_PAGES];

// Zone descriptor
typedef struct zone_struct
{
    // Free area bitmaps used by the buddy allocator
    free_area_t free_area[MAX_ORDER];
} zone_t;

// Node descriptor
typedef struct pglist_data
{
    // zones for this node, ZONE_HIGHMEM, etc
    zone_t node_zones[MAX_NR_ZONES];

    // first page of struct page array representing each physical frame in the node.
    // it's placed somewhere within global mem_map array.
    struct page *node_mem_map;
} pg_data_t;
```

`sizeof(page) == 32 Bytes`. So 32Bytes / 4KB = 1/128 of whole physical memory is used for management.

physical memory = zones, so a zone descriptor: memory utilization within each zone, e.g. active or inactive, etc.

?

zone descriptor: array of free areas. `free_area[i]` identifies the first page descriptor of the first block of 2^i free pages. E.g. `free_area[0]`, for all free areas of memory, area size is 1 (2^0) page, points to `free_area[i]`.

NUMA: different address have different access time. node descriptor. **UMA: describe all memory via one node descriptor.**

4-level paging

kernel itself is fully haredwired: no part of it is ever paged out. The rest, user pages, paging cache, and other.

page cache: pages containing file blocks recently read, pages of file blocks swapped out, etc. not a real cache, but a set of user pages no longer needed and waiting to be paged out.

#### Memory-Allocation Mechanisms

Page allocator: buddy algorithm

Initially, e.g. 64 pages as a whole. request, split into 2^i: request 4 pages, do splitting: 64 = 32 + 16 + 8 + 4 + 4, one of 4 pages are allocated.

Internal fragmentation: request 65 pages, get 128 pages. To alleviate, **slab allocator**: request through buddy, but split into slabs (smaller units) and manage the smaller units separately.

object cache based on allocated type. type-based cache.

```
typedef struct
{
    slab_t *slab;
} object_cache_t;
```

E.g. allocate a new `task_struct`:

1.  Find a partially full slab and allocate, return
2.  Looks through the list of empty slabs, return
3.  Allocate a new slab, link this slab with `task_struct` object cache, return

#### Virtual Address-Space Representation

virtual address space is divided into areas. each area: consecutive virtual pages with same page properties, e.g. r/w level. can have holes between the areas. 

fatal page fault: reference to hole results

```
typedef struct
{
    /* 
        protection mode, RO or RW
        if pinned in memory: pageable or not
        direction growing: stack to low address, heap to high address
        private to process or shared
        if there is backing storage on disk: if so (.text), there is. If not (stack), back when swapped out.
     */
} vm_area_struct
```

list + balaced tree to support area searching.

Fork and COW: after `fork`

-   vm areas list: child copied from parent. the areas are marked as R/W
-   page tables: child share the same page table with parent. The pages are RO.

Parent/child tries to write, **Protection Fault** (Not page fault) occurs. Kernel found: area is writable, but page is not. So copy the page and mark it as R/W.

Top level memory descriptor `mm_struct`:

-   information of all virtual memory areas belonging to address space
-   information about segments: text, data, stack
-   information about user sharing

### 10.4.4 Paging in Linux

swapper process: move pages betweej memory adn disk. **Page Frame Reclaiming Algorithm**.

-   Process 0 - idle, or named as swapper
-   Process 1 - init
-   Process 2 - page daemon

Pid2 page daemon runs periodically. one awake, check if needs to do swapping.

Linux: demand-paged. No prepaging, no working-set. 

text, mapped files are paged to files on disk. Other anonymous pages are swapped to swap area. Paging files can be added or removed dynamically and each one as a **priority**. raw device is more efficient.

#### The Page Replacement Algorithm

PFRA - Page Frame Reclaiming Algorithm

page types:

1.  unreclaimable - reserved or locked pages, kernel mode stacks, not paged out
2.  swappable - dirty: must be written back to swap area or paging disk partition before reclaimed
3.  syncable - if dirty, must be written back to disk
4.  discardable - can be reclaimed immediately

If memory is low, page daemon `kswapd` run PFRA. for each run, a certain target number of pages is reclaimed, typically <= 32. 

reclaim priority:

1.  discardable - clean or readonly, etc
2.  backed but not referenced recently - LRU

clock-like algorithm: LRU




