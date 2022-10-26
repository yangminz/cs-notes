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

## 3.4 Page Replacement Algorithms

### 3.4.8 The Working Set Page Replacement Algorithm

Ideally, processes started with no page in memory. When CPU tries to fetch the first instruction, page fault, bring in the page containing the first instruction. *This is **Demand Paging** because pages are loaded only on demand, not in advance*. Based on **locality of reference**.

The set of pages that a process is currently using is **working set**. Good case: entire working set is in memory. Otherwise, **thrashing**.

## 3.5 Design Issues for Paging Systems

### 3.5.5 Shared Page

Example: Run the same progam at the same time. RO pages (.text) are sharable, but RW data pages should be private.

A and B share the same code pages. A evicted code page, B will get page faults. So it's necessary to detect the pages. *Searching all page tables of all processes to see if a page is shared is too expensive, so a data structure `page_descriptor._count` is necessary to keep track of shared pages. Especially if the shared is one individual page instead of an entire page table.*

`fork` in UNIX: parent and child share both text and data, each has their own page table but points to the same set of frames. Thus no copying of pages is done at fork time. But all the frames are marked as READ ONLY. When one process updates a memory workd, violation of READ ONLY protection causes a trap to operating system.

(READ ONLY is one flag in page table entries. And we may need to flush TLB so the table entry would be read.)

Then OS detects it's COW and copy a private page. Both copies are now set to R/W in page table entries. COW is marked in `vm_area_structs`.

### 3.5.7 Mapped Files

shared lib: a special case of memory-mapped files. **syscall to map a file onto a portion of its virtual address space**. In implementations, when mapping, no pages are in memory (so the page table only records the mapping to disk offset? how do I distinguish swap space & normaly file since they are not encoded in the same way???). when the page is referenced, allocate as demand paging.

provide an alternative model for I/O: **files can be accessed as a big byte array in memory**.

## 3.6 Implementation Issues

### 3.6.1 Operating System Involvement with Paging

4 cases of page related work:

1.  process creation
2.  process execution
3.  process fault
4.  process termination

Page table need not to be resident when the process is swapped out but has to be in memory when the process is running.

Creation: prepare swap area on disk. Info about page table and swap area must be recorded in the process table.

Execution: MMU reset for the new process and TLB flushed. Copy the address of the page table of the new process to hardware registers. 

Page fault: OS read out hardware registers to find which virtual address caused the fault. Compute which page is needed and locate that on disk. Find page frame to put the new page, evicting old if needed. Back up PC so the faulting instruction would be executed again.

Termination: Release the page table, pages, and swap addresses. 

### 3.6.2 Page Fault Handling

1.  MMU traps to kernel. Push PC to kstack, save other info to special registers.
2.  Save general registers & volatile info for later resume. Go into OS.
3.  OS get page fault info (e.g. vaddr) from: (1) Hardware register; Or (2) parse current instruction.
4.  Check page table: if vaddr is valid: page fault & protection fault. If not valid or bad protection, kill process. Else, run page replacement.
5.  No free physical frame, select victim, swap to disk. When doing disk transaction, context switch to other processes.
6.  On free frame available, swap in file-backed or create annoymous page, switch to other processes
7.  I/O interrupt, switch back. Mark the frame in page table as normal.
8.  Restore to the state before page fault instruction.
9.  OS scheduled the fault process.
10. Restore user space & re-execute the instruction.

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

# Chapter 4. File Systems

Magetic disks/Solid-state drives: a linear sequence of fixed-size blocks supporting locating, reading, writing.

## 4.3 FILE-SYSTEM IMPLEMENTATION

-   How files & dirs are stored
-   How disk space managed
-   How to make everything work efficiently & reliably

### 4.3.1 File-System Layout

FS stored on disks: Disk can be divided into **partitions**: each partition has own FS. **Sector 0**: **MBR (Master Boot Record)** to boot the computer. The end of MBR: *partition table*.

Partition table records begin & end of each partition. When PC booted, BIOS reads in & exec MBR. MBR will locate the *Active Partition* (one partition marked as active), read in first block (**Boot Block**) & execute. OS is loaded from this partition.

```
          MBR
+--------------------+-----------+-----------+-----------+
|          |Partition| Partitions| Active    | Partitions|
|          |Table    |           | Partition |           |
+--------------------+-----------+-----------+-----------+
```

Actually each partition has the following structure for uniformity, whether it has OS or not:

```
+-----------+-----------+-----------+-----------+-----------+--------------+
| Boot      | Super     |Free Space | I-nodes   | Root Dir  | Data Blocks: |
| Block     | Block     |Mgmt       |           |           | Files & Dirs |
+-----------+-----------+-----------+-----------+-----------+--------------+
```

-   Boot Block: If partition does not have OS, it's empty. If has OS, execute to load OS.
-   Super Block: all key parameters about FS, e.g., FS type, number of blocks, etc.
-   Free Space Mgmt: A bitmap or list of pointers of free blocks
-   I-nodes: one per file tell about the file metadata.
-   Root dir: top of the FS. `/`
-   Data blocks: the files & dirs.

### 4.3.2 Implementing Files

Most important issue: keep track of which disk blocks go with which file.

#### Contiguous Allocation

Each file is a set of contiguous disk blocks. E.g., Block size 1KB, 50KB file will use 50 consecutve blocks. May have internal fragmentations if 512B file, will not use the left 512B space.

Pros:

1.  Simple to impl: starting disk address & number of blocks.
2.  Read is good perf: in sequence. No more disk seek & rotation.

Cons: Fragmentation is severe. Will eventually to compact the disk, stop the world and expensive.

It's good for CD-ROMs, DVDs, Blu-rays, and other write-once media.

#### Linked-List Allocation

```
+-----+    +-----+    +-----+    +-----+    +-----+
|  ------->|  ------->|  ------->|  ------->|     |
+-----+    +-----+    +-----+    +-----+    +-----+
|File |    |File |    |File |    |File |    |File |
|Block|    |Block|    |Block|    |Block|    |Block|
|[0]  |    |[1]  |    |[2]  |    |[3]  |    |[4]  |
+-----+    +-----+    +-----+    +-----+    +-----+

Physical   Physical   Physical   Physical   Physical
Block 4    Block 7    Block 2    Block 10   Block 12
```

First word of the block is a pointer to the next block. If shuffle the blocks, access is very slow due to re-seek.

#### Linked-List Allocation Using a Table in Memory

Use a **FAT (File-Allocation Table)** in memory to record the pointers. So the search is done in memory instead of on disk. Sort and access in sequence.

So not scale well to large disks. MS-DOS & all windows use this FS.

But really consumes memory.

#### I-nodes

Use **i-node (index-node)** to keep track of each file meta data and find all blocks of the file.

```
+------------------------------+
| File Metadata                |
+------------------------------+
| Address of disk block 0      |
+------------------------------+
| Address of disk block 1      |
+------------------------------+
| Address of disk block 2      |
+------------------------------+
| Address of disk block 3      |
+------------------------------+
| Address of disk block 4      |
+------------------------------+
| Address of disk block 5      |
+------------------------------+
| Address of disk block 6      |
+------------------------------+
| Address of disk block 7      |
+------------------------------+
| Address of block of pointers |
+------------------------------+
```

It can save space. Use multi-level pointers to save more pointers.

### 4.3.3 Implementing Directories

Dir sys: map ASCII name of file onto info to locate file.

How to store: 

Solution 1: simple way: dir have array of fixed size entries to map. So the file name must be limited, less than entry size.

Solution 2: fixed-length header (owner, time, protection, etc.) + file name. E.g.,

```c
typedef struct
{
    uint32_t        filesize;
    file_metadata_t metadata;
    char            filename_firstchar;
    // the following chars all belong to filename
    // read: (char*)(&entry_p->filename_firstchar)
} dir_entry_t
```

But when file is removed, there is a variable-sized gap. Only way is compacting the directory (feasible since it's in-memory). Another problem: directory entry can be huge (more than one page), so may have page fault.

Solution 3: ELF way: fixed-size entry + pointer to variable names, like string table in ELF file. Still need to manage heap of strings since may delete (which is different from ELF string table, that's read only).

The above solutions use linear search. Improve: hash table/trie. If use hash table, cache the search result for faster searching.

### 4.3.4 Shared Files

Files can be in different dirs for different users to share. Thus FS hierarchy is DAG (directed acyclic graph) instead of a tree.

Symbolic link: Only true owner has the pointer to i-node. Other users just have path names (**Link-Type File**), OS sees the requested file type is LINK and thus search the FS to get the i-node. When true owner deletes the file, symbolic link will fail. It brings multiple disk accesses and add access to i-node of the Link file itself.

Hard link: Add the i-node of the shared file to directories. And do reference count in i-node. But owner of i-node and actual referencer may mismatch: `foo=(Owner=/A,Count=1</A>)`, add hard link of `/B`: `foo=(Owner=/A,Count=2</A,/B>)`, `/A` deletes the file `/A/foo`: `foo=(Owner=/A,Count=1</B>)`, mismatch in this case. `/A` may be counted as owning the file.

### 4.3.5 Log-Structured File Systems

Disk seek time is very hard to reduce even CPU much more faster, the bottleneck. Berkeley desigend **LFS (Log-structured File System)** to try to alleviate.

Consistency vs Performance: cache writes and do batch writing will help perf, but if system crash then no consistency. So i-node writes are generally done immediately.

Idea: do FS cache without disk access. Observation: most disk activities will be **small writes**, so prefetch-block for read will not help perf.

LFS: structure the entire disk as big log to achieve full bandwidth of disk, even a workload of so many small random writes. OS collects all pending writes in memory into a single segment, append to the end of the disk log. A segment contains: i-nodes, dir blocks, data blocks, all in one. If average size of segment is about 1MB, then full bandwidth of disk can be utilized.

Now much harder to find an i-node (location cannot be calculated by i-node number). So maintain an i-node to disk address map, the map is on disk, also cached in memory.

> all writes are initially buffered in memory, and periodically all the buffered writes are written to the disk in a single segment, at the end of the log.

Real world: disk size is limited, so need to overwrite the old log entries, e.g., the old data blocks. LFS need one cleaner thread to do scanning & compacting.

### 4.3.6 Journaling File Systems

Use the basic idea of LFS: keep track of what FS is going to do before do it, so operations can be recovered from crash. **Journaling File System**.

Consider removing file problem: In UNIX, 3 steps:

1.  Remove file from directory
2.  Release i-node to the pool of free i-nodes
3.  Return data blocks to the pool of free data blocks

System can crash anytime. If step 1 happened, system crashed, then we lose the reference to i-node, it's unreachable. And it happens on disk, so rebooting will not help at all. If do step 2 or step 3 first, and then system crash, still have severe problem.

JFS firstly writes a log entry listing the 3 actions to be done. Only when the log is written on disk, then do the operations. When done, remove the log.

The logged operations must be **idempotent**: can be repeated as often as necessary without harm (Like RESTful API). "Add block n to free list" is not idempotent, "Search free block list and add block n to it if it's not there" is.

JFS can also use **atomic transaction**, a group of actions between `BEGIN TRANSACTION` & `END TRANSACTION`: FS must complete the transaction or not at all (like DBMS). 

### 4.3.7 Virtual File Systems

Windows: `C:`, `D:`, can be different FS. UNIX: all in one: `/` can be ext2, `/usr` can be ext3 partition. This is done by **VFS (Virtual FS)**. Idea: abstraction of FS interface. The user program will only use POSIX standard calls: `open`, `read`, `write`, `lseek`, etc.

Original motivation: support network file: user program do not know the data is local or remote.

Timeline:

1.  System boot, root file system is registered with VFS.
2.  Boot time or lazily, other FSs register with VFS: FS provides the function addresses of `open`, `read`, `write`, `lseek`, etc. So VFS knows how to r/w block through underlying FS.
3.  User program call VFS functions through POSIX: `open("/usr/include/unistd.h", O_RDONLY);`
    1.  VFS locates FS superblock of `/usr` of all registered FSs
    2.  VFS creates a **v-node** (in RAM) and call the underlying FS to get i-node.
4. VFS makes an entry in file-descriptor table for calling process and points to new v-node.
5. VFS return the file-descriptor back to user process.

When user process tries to read:

1.  VFS locate the v-node through file descriptor table
2.  VFS call the underlying FS's `read` and get the data block.

## 4.4 FILE-SYSTEM MANAGEMENT AND OPTIMIZATION

### 4.4.1 Disk-Space Management

2 general strategies for storing n-byte file: 1) allocate consecutive space on disk; 2) split the file into blocks. Same trade-off in segmentation vs paging in memory management. Nearly all FS split the file into blocks so file can grows larger.

#### Block Size

Disk view: sector, track, cylinder, unit of allocation, all device dependent. In paging system, page size.

If block is too large, waste space for small files; if too small, waste time in seeking next blocks. So we need to study the file-size distribution. FS often use 1KB to 4KB range, but disk is cheap, we can use 64KB and accept the wasted disk space.

#### Keeping Track of Free Blocks

2 widely used methods: 1) Linked list of free blocks. Free blocks will hold the list, so the storage is free. Bad when disk is severely fragmented; 2) bitmap: n blocks will make a n-bit bitmap.

Linked list problem: when block of pointers is almost empty, short-lived temporary files can cause a lot of disk I/O. So split the full block of pointers. Keep most of the pointer blocks on disk full to minimize disk usage, but keep the one in memory about half full to handle file creation and removal on the free list.

> This issue illustrates a problem operating system designers often have. There are multiple data structures and algorithms that can be used to solve a problem, but choosing the best one requires data that the designers do not have and will not have until the system is deployed and heavily used. And even then, the data may not be available.

#### Disk Quotas

Multiuser OS: enforcing disk quotas. Open file table in main memory has a *owner* attribute, file size will be counted to the owner.

A quota table to record per user.

### 4.4.2 File-System Backups

FS cannot protect against physical destruction. It can only do backups.

**Incremental dumps**: A complete dump weekly/monthly, daily dump for files only changed since last dump.

Dump is taking time, make rapid snapshots of FS state by copying critical data structures. Future changes to files & dirs will copy the blocks instead of updating them in place.

2 strategies:

-   **Physical dump**: copy from block 0 to the last. Useless to backup unused blocks. But accessing free block data structure need writing. Bad blocks (physically) usually invisualable to OS. Pros: simple, fast; Cons: cannot skip dirs, cannot make incremental dumps, cannot restore individual files. So the most make logical dumps.
-   **Logical dump**: start from one specified dir and recursively dump down. 

### 4.4.3 File-System Consistency

System crashes before writing to disk, FS is in inconsistent state. Unix `fsck`, Windows `sfc` to check FS consistency. It will run whenever the system is booted. The consistency checks: Blocks & Files.

Block consistency checker: Use 2 counters, one counter per block, initialized as `0`. Counter 1 checks the block belong to file: for each i-node, for each block, add 1; Counter 2 checks free blocks, for each free block, add 1. If consistent, then each block should not be shared, and it's allocated or free. So `cnt1 + cnt2 == 1`. If inconsistent:

1.  `cnt1 + cnt2 == 0`: **missing block** -- an allocated block but not referenced by any file, no harm but waste space. Just add it back to free block pool.
2.  `cnt1 == 1 && cnt2 > 1`: counted twice in free block pool. May happen in free block list, bitmap will not have this problem. Rebuild the free list.
3.  `cnt1 > 1`: block is shared by files. Checker will allocate a new block and copy the payload to new-freed block.

For dir, check file consistency in the same fashion, check in the unit of file (i-node).

### 4.4.4 File-System Performance

3 techs to improve perf:

#### Caching

Block cache or buffer cache. Check all read requests and see if the requested block is in cache in memory: Hash table + doubly linked list = LRU cache. But LRU may undesirable due to crash & consistency: i-node read into cache and updated but not written back to disk, then inconsistent. And i-node is not likely to be referenced twice shortly.

So divide the blocks: i-node blocks, indirect blocks, data blocks, etc. Arrange their LRU order by access frequency. If block is critical to consistency, write back immediately.

UNIX `sync` to force dirty blocks to be written to disk immediately. Windows: write-through cache, more disk I/O, every char written.

Integrate buffer cache with page cache, good for memory-mapped files.

#### Block Read Ahead

Pre-fetch into cache to improve cache hit. Good for files being read sequentially, and not help for randomly accessed file. So each file can set a flag: *sequential-access mode* or *random-access mode*.

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




