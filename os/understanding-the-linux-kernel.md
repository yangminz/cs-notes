3rd By Daniel P. Bovet, Marco Cesati

# Chapter 2. Memory Addressing

## 2.5 Paging in Linux

32-bit address: 2-level paging is enough; 64-bit: 4-level paging

1.  Page Global Directory
2.  Page Upper Directory
3.  Page Middle Directory
4.  Page Table

Each process has its own PGD and own set of page tables. When context switch, saves `cr3` register into `task_struct`.

### Page Table Handling

`pte_t, pmd_t, pud_t, pgd_t`

### Physical Memory Layout

during initialization, kernel build a **physical addresses map** that specifies the range of useable memory for kernel. others are unavailable, maybe used by hardware device's I/O shared buffer, or BIOS data, etc.

These physical pages are reseved in memory, will not **be swapped to disk** or dynamically assigned:

-   unavailable physical address, e.g. hardware's shared buffer, BIOS data (frame 0)
-   kernel's `.text` & `.data` segments

### Process Page Tables

32-bit:

-   `0x00000000, 0xbfffffff` can be addressed when CPU in user or kernel mode
-   `0xc0000000, 0xffffffff` can only be addressed when CPU in kernel mode

kernel size = 1/4 virtual space, user size = 3/4 virtual size. So PGD: 1/4 high entries are for kernel page table, 3/4 entries are for user page table.

these entries should be the same for all processes and equal to the corresponding entries of the **master kernel Page Global Directory**

### Kernel Page Tables

kernel maintaines a set of page tables for its own use, rooted at **master kernel Page Global Directory**. It's nevevr directly used by any user process. The high entries are the reference model for the high entries of PGD in every regular process.

The changes to master kernel PGD are propagated to PGD of processes. {Chapter 8}

Initialize master kernel PGD in 2 phases, when CPU is still in real-mode, no paging here:

1.  kernel creates a minimal address space, including kernel `.text` & `.dta`, the initial page tables, 128KB for other data structures. This minimal address space is just large enough to install kernel in RAM and initialize its core data structures
2.  Use all existing RAM and set up the page table in following steps:
    1.  in compilation, initialize the **provisional kernel page tables** in `swapper_pg_dir` (pid 0)
    2.  provisional page table make the first 8MB of RAM addressable in protected mode.
    3.  if RAM < 896 MB, RAM can be mapped entirely into kernel linear address space, store `swapper_pg_dir` to `cr3`.
    4.  896MB < RAM  < 4096MB, RAM cannot be mapped entirely into kernel linear address space, ...
    5. 4096BM < RAM, ...

# Chapter 3. Processes

## 3.2 Process Descriptor

```c
task_struct
    thread_info     // kernel stack related for trap & context switch
    void *stack
    mm_struct       // page table, virtual address space, check page access
    signal_struct   // signals received

    tty_struct      // tty associated with the process
    fs_struct       // current directory
    files_struct    // pointers to file descriptors
```

### Process State

```
TASK_RUNNING
TASK_INTERRUPTABLE      sleeping & waiting for conditon (blocked)
TASK_UNINTERRUPTIBLE    seldom used
TASK_STOPPED            execution has been stopped: SIGSTOP, SIGTSTP, SIGTIN, SIGTTOU
TASK_TRACED             stopped by debugger

EXIT_ZOMBIE             terminated but parent has not yet issued a wait
EXIT_DEAD               process is being removed by kernel because parent wait. To avoid race condition
```

### Identifying a Process

#### Process descriptors handling

`task_struct` can be swapped out as anonymous pages. (but list head should be a `.data` member hold in RAM forever) 

`thread_info` + kernel stack (2 pages, small because kernel call stack are shallow). 

kernel stack: in kernel mode, process use stack with kernel data segment, different from user stack. 

`task_struct`, `task_struct->thread_info`, `task_struct->stack`, any one can get other two.

right after switching from user mode to kernel, kernel stack is always empty (no call stack), `rsp` points to `task_struct->stack`. When call kernel functions, `rsp` moves down to `thread_info`. 

#### Identifying the current process

So in kernel mode, with `rsp` value in CPU, we can get `thread_info`, and then `task_struct`.

> Why we need this? Because when trap (from user to kernel, `int`), the kernel process only knows: the `esp` value from saved TSS, the `rip` from the IDT, trap handler entry point. Although the kernel process does not need to switch page table `%cr3`, but it need other information from `task_struct`. What we have now is: `esp, eip`, all other registers are zero or user data. One method is place the pointer of current `task_struct` as kdata, and at the entry point, assign this pointer to one register. But this is not good because it involes kernel page mapping. So the method is get `task_struct` directly from `kstack, %esp`. `%esp - 8KB + sizeof(thread_info)` is a constant operation, and will give the `task_struct` directly.

#### The process lists

`task_struct` are linked together. The head: Pid 0 **process 0 or swapper**

#### The lists of `TASK_RUNNING` processes

scheduler check this running queue. 

### How Processes Are Organized

group the processes by their states: running --> runqueue. `TASK_STOPPED, EXIT_ZOMBIE, EXIT_DEAD`, no list for them. just the list of all processes.

`TASK_INTERRUPTABLE, TASK_UNINTERRUPTIBLE` are divided into different classes, each for a specific event. 

#### Wait queues

(Blocked)

wait queues several uses. particularly for interrupt handling, process sync, timing. in general, a process must wait for some event to occur, e.g. disk read to terminate, 500ms to elapse, etc. Implement conditional waits on event. 

Multiple wait queues - doubly linked list. Each wait queue should be protected from concurrent access. They are modified by interrupt handlers & kernel functions. 

Processes in wait queue are sleeping, waiting for some event to occur. When wake up, 2 situations: wake up one particular process, wake up all processes (**Thundering herd**). Choose this by flags. Each sleeping process is *exclusive* or *nonexclusive*.

#### Handling wait queues

When a process is awakened, set process state to `TASK_RUNNING`, move from wait queue to runqueue.

```C
void wake_up(wait_queue_head_t *q)
{
    // q is the head for all wait queues
    struct list_head *tmp;
    wait_queue_t *curr; // current wait queue

    list_for_each(tmp, &q->task_list)
    {
        // get the current wait queue
        curr = list_entry(tmp, wait_queue_t, task_list);

        if (curr->func(curr, TASK_INTERRUPTIBLE | TASK_UNINTERRUPTIBLE, 0, NULL) && // check the condition
            curr->flags // check if thundering herd
            )
        {
            break;
        }
    }
}
```

## 3.3 Process Switch

### Hardware Context

process: private address space, shared CPU registers.

Linux: part of hardware context stored in process descriptor, the remaining part is saved in kernel stack.

Suppose switch from Prev to Next process. They are local variables in `schedule()`.

context switching is only in kernel mode. When trap from user mode to kernel mode, all registers used in User Mode have been saved on kernel stack.

### Task State Segment

TSS is used by 80x86 to store hardware contexts. Linux does not use TSS for hardware context switching.

### Performing the Process Switch

context switch will only happen in `schedule()`. 2 steps:

1.  Switching the PGD to install a new address space {Chapter 9}
2.  Swithcing the kernel stack and hardware context

#### The `switch_to` marco

Step 2 is performed by the `switch_to` marco. Need Prev, Next, Last: Prev -> Next (store), Last -> Prev (restore).

store the registers flags, registers, PC to kernel stack. and restore the context of the new process.

# Chapter 4. Interrupts And Exceptions

## 4.1 The Role of Interrupt Signals

Diff {interrupt, context switch}: they are all kernel substitution, but code executed by interrupt is not a process. Interrupt is lighter than process. 

Interrupt is asynchronous, and can be interrupted by another interrupt. Be careful about the race conditions.

## 4.2 Interrupts and Exceptions

### IRQs and Interrupts

Interrut ReQuest (IRQ) Line, hardware _Programmable Interrupt Controller_(PIC): Monitor IRQ lines, select the low pin one, wait until CPU ack interrupt. By `IF` flag in `eflag`, CPU can ignore the interrupts sent from PIC.

### Interrupt Descriptor Table

`IDTR` register stores the physical address.

IDT associates each interrupt/exception with the address of its handler. IDT must be initialized before kernel enable interrupts. 3 types of descriptors in IDT, task gate descriptor, interrupt gate descriptor, trap gate descriptor:

```
Interrupt Gate
0   :   15  Offset
16  :   31  Segment Selector
32  :   36  Reserved
37  :   37  0
38  :   38  0
39  :   39  0
40  :   40  0
41  :   41  1
42  :   42  1
43  :   43  1
44  :   44  0
45  :   46  DPI
47  :   47  P
48  :   63  Offset

Trap Gate
0   :   15  Offset
16  :   31  Segment Selector
32  :   36  Reserved
37  :   37  0
38  :   38  0
39  :   39  0
40  :   40  1
41  :   41  1
42  :   42  1
43  :   43  1
44  :   44  0
45  :   46  DPI
47  :   47  P
48  :   63  Offset
```

### Hardware Handling of Interrupts and Exceptions

Precondition: CPU running in Protected Mode. 

After executing an instruction, before executing the next instruction, CPU checks if interrupt or exception. If there is:

1.  Determin the vector / interrupt number `i`
2.  Get `IDT` by `idtr` register, read `IDT[i]`
3.  Get `GDT` by `gdtr` register, look for the segment by the `IDT[i].segment_selector`. This is the segment includes the base address of handler
4.  Check privilege level is correct.
5.  Check if privilege level change. 
    -   If user to kernel
        -   Read `tr` register to get the task. The TSS segment of the running process. This TSS segment stores the kernel register information
        -   Load kernel `ss` and `esp` from TSS as kstack.
        -   Push user `ss` and `esp` to kstack.
6.  If fault, load `cs` and `eip`
7.  Push `eflags`, `cs`, `eip` to kstack
8.  If exception with hardware error code, save error code on stack
9.  Load `cs` and `eip` from segment selector and offset from `IDT[i]` gate. It's a jump to interrupt/exception handler. The control goes back to OS till this point.

`iret` the opposite operations.

# Chapter 8. Memory Management

## 8.3 Noncontiguous Memory Area Management

### Allocating a Noncontiguous Memory Area

Input: a fresh interval of contiguous linear address, a group of noncontiguous page frames has been allocated

Map the contiguous linear address to the maybe-discrete physical page frames. Update the page tables to do the mapping. `map_vm_area`

1.  require a spin lock for `init_mm->pgd`
2.  Then create new PUD, PMD, PTE covering the addresses

# Chapter 9. Process Address Space

## 9.1 The Process's Address Space

Address space: all linear addresses that the process is allowed to use. The intervals of linear addresses: *memory regions*. 

## 9.2 The Memory Descriptor

All information about address space is in `task_struct->mm_struct`. All `mm_struct` are stored in doubly linked list `mmlist`. protected by a spin lock against concurrent access. Reference counting: when `mm_count` decreased, kernel check if it's zero, then deallocate when not in use. 

### Memory Descriptor of Kernel Threads

Kernel thread does not access linear address below `0xc0000000`. So kernel thread do not use memory regions, and will not use most fields of `mm_struct`.

**The page table entries above `0xc0000000` should be the same for all processes**, so kernel thread can use the page tables of the last previously running regular process: `mm` - owned by the process; `active_mm` used by the process when in execution. For regular process, `mm == active_mm`; For kernel threads, `mm == NULL, active_mm = last active_mm`. 

(So when process traps to kernel, rip moves to kernel .text by trap handler, `mm` becomes NULL, `active_mm` does not change, `active_mm->pgd` are in 2 halves: half high is kernel pgd entries, points to kernel pud, pmd, pte, half low is user process pgd entries, private to the process. And all processes share the same high half of pgd. kernel PGD have multiple copies, but kernel PUD, PMD, PTE are identical in the whole memory.)

When kernel process updates page table entry in high space, it should update the correspoinding entry in all processes' page tables. It's costy operation, therefore use deferred approach: when high address remapped, update a canonical set of page tables rooted at `swapper_pg_dir` master kernel PGD: `init_mm->pgd`.

## 9.3 Memory Regions

`vm_area_struct` -- memory region descriptor, identifies a linear address interval.

`vm_start, vm_end`, first linear address inside/outside the interval: `end - start` is the length of interval. `vm_mm` pionts back to `mm_struct`.

Regions of one process does not overlap, kernel tries to merge regions when possible.

................

`mm_struct.vm_area_struct` is processor-independent, abstract memory regions, aka mappings. `mm_struct.pgd` is processor-specific page table. 

When loads a new program, kernel setup VMAs according to the segments in executable file (ELF). VMA can generate page table. Kernel will update page table by looking at VMAs. 

On page fault, kernel check VMAs.

```assembly
section .data
    buffer db "...", 0xA
section .text
    global _start
    _start:
        mov rbx, 0
    loop:
        cmp rbx, 0xFFFFFFFFFFFFFFFF
        je end
        mov rax, 1
        mov rdi, 1
        mov rsi, buffer
        mov rdx, 4
        syscall
        add rbx, 1
        jmp loop
    end:
```

Compile: `nasm x.asm -f elf64 -o x.o`

```
x.o:     file format elf64-x86-64


Disassembly of section .data:

0000000000000000 <buffer>:
   0:   2e                      cs
   1:   2e                      cs
   2:   2e                      cs
   3:   0a                      .byte 0xa

Disassembly of section .text:

0000000000000000 <_start>:
   0:   bb 00 00 00 00          mov    $0x0,%ebx

0000000000000005 <loop>:
   5:   48 83 fb ff             cmp    $0xffffffffffffffff,%rbx
   9:   74 21                   je     2c <end>
   b:   b8 01 00 00 00          mov    $0x1,%eax
  10:   bf 01 00 00 00          mov    $0x1,%edi
  15:   48 be 00 00 00 00 00    movabs $0x0,%rsi
  1c:   00 00 00
  1f:   ba 04 00 00 00          mov    $0x4,%edx
  24:   0f 05                   syscall
  26:   48 83 c3 01             add    $0x1,%rbx
  2a:   eb d9                   jmp    5 <loop>
```

Link: `ld x.o`. This will generate executable `a.out` without glibc.

```
a.out:     file format elf64-x86-64


Disassembly of section .text:

0000000000401000 <_start>:
  401000:       bb 00 00 00 00          mov    $0x0,%ebx

0000000000401005 <loop>:
  401005:       48 83 fb ff             cmp    $0xffffffffffffffff,%rbx
  401009:       74 21                   je     40102c <end>
  40100b:       b8 01 00 00 00          mov    $0x1,%eax
  401010:       bf 01 00 00 00          mov    $0x1,%edi
  401015:       48 be 00 20 40 00 00    movabs $0x402000,%rsi
  40101c:       00 00 00
  40101f:       ba 04 00 00 00          mov    $0x4,%edx
  401024:       0f 05                   syscall
  401026:       48 83 c3 01             add    $0x1,%rbx
  40102a:       eb d9                   jmp    401005 <loop>

Disassembly of section .data:

0000000000402000 <buffer>:
  402000:       2e                      cs
  402001:       2e                      cs
  402002:       2e                      cs
  402003:       0a                      .byte 0xa
```

The VMAs

| address start-address end | mode | offset   | major id:minor id | inode id | file path            |
|---------------------------|------|----------|-------------------|----------|----------------------|
| 00400000-00401000         | r--p | 00000000 | 08:20             | 50358    | /home/yangminz/a.out |
| 00401000-00402000         | r-xp | 00001000 | 08:20             | 50358    | /home/yangminz/a.out |
| 00402000-00403000         | rw-p | 00002000 | 08:20             | 50358    | /home/yangminz/a.out |
| 7ffc4380c000-7ffc4382e000 | rw-p | 00000000 | 00:00             | 0        | [stack]              |
| 7ffc439da000-7ffc439de000 | r--p | 00000000 | 00:00             | 0        | [vvar]               |
| 7ffc439de000-7ffc439df000 | r-xp | 00000000 | 00:00             | 0        | [vdso]               |

4 permissions in mode: read, write, execupte, private/shared

................

### Memory Region Data Structure

list + red black tree. Most processess use very few regions. But large applications, e.g. object-oriented databases or debuggers for `malloc` may have even 1k+ regions. 

RBT: locate a region including a specific address;
Linked List: scan the whole set of regions.

### Memory Region Access Rights

The relation between a page and a memory region.

Each memory region consists of a set of pages that have consecutive page numbers.

A page can have:

1.  Flags `R/W, Present, User/Supervisor` stored in page table entry. Used by `80x86` hardware to check **whether the requested kind of addressing can be performed**.
2.  Flags stored in `page` descriptor (reversed mapping). Used by Linux for many different purposes.

Now, *a third kind of flag*: associated with the pages of a region, stored in `vm_area_struct.vm_flags`. Some tell the page information in the region, e.g., what they contain and what rights the process has to access. Other describe the region, e.g., how it can grow. E.g., controls pages in region to be read but not executed.

This is a protection scheme: Read, Write, Execute access rights should be duplicated in corresponding page table entries. ***The page access rights dictate what kinds of access should generate a Page Fault exception.*** Page Fault handler will figure out what caused the Page Fault.

However, translating region's access right into page protection bits is not straightforward: 

1.  Copy On Write: Page fault even when access is allowed by region. 
2.  `80x86` page table have just 2 protection bits: R/W and U/S.

For linux compiled without PAE, to overcome `80x86` limitation:

1.  Read <==> Execute
2.  Write ==> Read

For linux compiled with PAE:

1.  Execute ==> Read
2.  Write ==> Read

For COW, page frame is write-protected whenever the page must not be shared by several processes.

Then, `Read, Write, Execute, Shared`, the 4 access rights (2^4 = 16) combinations can be scaled down.

### Memory Region Handling

`find_vma()` to find the closet region to a given address in memory descriptor. Use RBT to search.

`insert_vm_struct()` to insert a vma to linked list & rbt. 

## 9.4 Page Fault Exception Handler

Handler must distinguish exceptions caused by programming errors from those caused by a reference to a page that legitimately belongs to the process address space but simply hasn’t been allocated yet.

```json
{
    "Access to kernel space?":
    {
        "Yes": ["Access in Kernel Mode?"],
        "No": ["In Interrupt, softirq, critical region, or kernel thread?"]
    },
    "Access in Kernel Mode?":
    {
        "Yes": ["Noncontiguous memory area address?", "vmalloc_fault"],
        "No": ["Access in User Mode?", "bad_area"]
    },
    "Noncontiguous memory area address?":
    {
        "Yes": ["Kernel page table entry fixup"],
        "No": ["Address is a wrong system call parameter?", "no_context"]
    },
    "In Interrupt, softirq, critical region, or kernel thread?":
    {
        "Yes": ["Address is a wrong system call parameter?", "no_context"],
        "No": ["Address in a memory region?"]
    },
    "Address in a memory region?":
    {
        "Yes": ["Write access?", "good_area"],
        "No": ["Address could belong to User Mode stack?"]
    },
    "Address could belong to User Mode stack?":
    {
        "Yes": ["Write access?", "good_area"],
        "No": ["Access in User Mode?", "bad_area"]
    },
    "Write access?":
    {
        "Yes": ["Region is writeable?"],
        "No": ["Page is present?"]
    },
    "Region is writeable?":
    {
        "Yes": ["Demand Paging and/or Copy On Write"],
        "No": ["Access in User Mode?", "bad_area"]
    },
    "Page is present?":
    {
        "Yes": ["Access in User Mode?", "bad_area"],
        "No": ["Region is readable or executable?"]
    },
    "Region is readable or executable?":
    {
        "Yes": ["Demand paging"],
        "No": ["Access in User Mode?", "bad_area"]
    },
    "Access in User Mode?":
    {
        "Yes": ["Send SIGSEGV", "do_sigbus"],
        "No": ["Address is a wrong system call parameter?", "no_context"]
    },
    "Address is a wrong system call parameter?":
    {
        "Yes": ["Fixup code (typically send SIGSEGV)"],
        "No": ["Kill process and kernel Oops"]
    }
}
```

### Demand Paging

Defer page frame allocation until the last possible moment—until the process attempts to address a page that is not present in RAM, thus causing a Page Fault exception

Motivation: process does not need to address all addresses in the address space. Some addrsses may never be used. V.s global allocation: assign all frames to process from start to termination.

Price: system overhead. Each page fault induced by Demand Paging will waste CPU cycles. But locality will help.

When page fault handler assign a new frame to process, how is it initialized:

1.  `PTE.value == 0` Page table entry is all zero: (1)page was never accessed and does not map a disk file; (2) page linearly maps a disk file. -- `pte_none == 1`
2.  `PTE.present == 0 && PTE.dirty == 1`: page belongs to a non-linkear disk file mapping. -- `pte_file == 1`
3.  `PTE.value != 0 && PTE.present == 0 && PTE.dirty == 0`: page was already accessed but content is on disk.

```c
if (pte.present == 0)
{
    if (pte.value == 0)
    {
        // case 1
        // may do anonymous page mapping, handle read and write differently
        return do_no_page(mm, vma, address, write_access, pte, pmd);
    }
    else
    {
        if (pte.dirty == 1)
        {
            // case 2
            return do_file_page(mm, vma, address, write_access, pte, pmd);
        }
        else
        {
            // case 3
            return do_swap_page(mm, vma, address, write_access, pte, pmd);
        }
    }
}
```

### Copy On Write

Old `fork` takes time to copy the whole address space:

1.  Allocate frames for page tables of child
2.  Allocate frames for pages of child
3.  Initialize page tables of child
4.  Copy pages of parent to child

COW: instead of duplicating frames, they are shared between parent and child. As long as they are shared, they cannot be modified. The original page frame **remains write-protected**: when the other process tries to write into it, the kernel checks whether the writing process is the only owner of the page frame; in such a case, it makes the page frame writable for the process.

**Reversed mapping: `page._count` to keep track of sharing processes of one frame.**

1.  Derive the page descriptor of frame referenced by PTE
2.  Determine if the page must be duplicated: if only one process owns the page, no COW. Check by `_count` field in page descriptor. (`_count == 0` indicates single proecss).
3.  Copy the content of old page to the new. New page is allocated by `alloc_page`. To avoid race, use `get_page` to increase `_count` of old.
4.  Write zero if old page is zero page for better performance.
5.  Write frame address to PTE, update TLB.


### Handling Noncontiguous Memory Area Accesses

Kernel is lazy in updating page table entries for noncontiguous memory areas. `vmalloc()` and `vfree()` limit themselves in updating master kernel page tables (`init_mm.pgd` and its child tables).

Master kernel page tables are not directly used by any user process or kernel thread. When process in kernel mode first access a noncontiguous memory area address, MMU encounters a null page table entry, raises a page fault. _The handler finds this address is kernel address, then check master kernel page table entry._

If master kernel page table entry is null, goto `no_context` and hanldling this address as outside the address space (note that kernel thread is not having address space, this address space is not the regular process's address space). Else, 

# Chapter 10. System Calls

# Chapter 11. Signals

## 11.1 The Role of Signals

Signal: a very short message may be sent to a process or a group of processes. Usually a number identifying the signal. Purposes:

1.  Notify a process of a specific event happened
2.  Cause a process to execute a signal handler in user code

| System Call     | Description                         |
|-----------------|-------------------------------------|
| `kill`          | Send a signal to a thread group     |
| `sigprocmask`   | Modify the set of blocked signals   |
| `sigsuspend`    | Wait for a signal                   |

Signal can be sent at any time to a process in any state. If not currently executing, kernel saves the signal until execution resumed. Blocking a signal -- hold off the delivery until it's unblocked. 2 phases:

1.  Signal generation: kernel updates the data structure of the destination process;
2.  Signal delivery: kernel force the destination process to react to the signal by changing its execution state, a specified signal handler.

Each signal generated can be delivered at most once. Generated but not delivered are called pending signals. At any time, at most one pending signal of a type exists for a process. 

### 11.1.1 Actions Performed upon Delivering a Signal

1.  Explicitly ignore the signal;
2.  Execute the default actions including: Terminate, Dump, Ignore, Stop, Continue;
3.  Catch the signal by signal-handler function.

Blocking $\neq$ Ignoring: Signal blocked ==> not delivered. It's delivered only after unblocked. Ignore ==> delivered but no action.

`SIGKILL, SIGSTOP` can not be ignored, caught, blocked and must execute the default actions.

### 11.1.3 Data Structures Associated with Signals

Each process needs to keep track of the pending or masked signals.

```c
struct task_struct
{
    // storing the private pending signals
    struct sigpending pending;    
    /*  the signal descriptor of the process
        signal counts, shared_pending list
    */
    struct signal_struct *signal;
    // the signal handler of the process
    struct sighand_struct *sighand;
    /* mask of blocked signals
        typedef struct {
            unsigned long sig[2]; // 64 bits - at most 64 signals
        } sigset_t;
        1-31 signals
        32-64 real-time signals
    */
    sigset_t blocked;
}
```

#### 11.1.3.3 The pending signal queues

Signal can be send to a whole thread group. So keep track of shared pending signals:

1.  The shared pending signal queue: the pending signals for the whole thread group;
2.  The private pending signal queue: for the process itself only.

```c
struct sigpending
{
    // doubly linked list head containing `sigqueue`
    struct      list_head list;
    // bit mask specifying the pending signals
    sigset_t    signal;
}
```

## 11.3. Delivering a Signal

Kernel notices the arrival of a signal and then prepare the PCB to receive the signal. But when the process is not running on CPU, kernel defer the task of delivering the signal.

Kernel firstly handles exception/interrupt, and then checks the value of `TIF_SIGPENDING` flag of process before resume user mode execution. If there is pending signals, handle it by invoking `do_signal`. One parameter is the user stack address saving the register contents.

`do_signal` has a loop to repeatedly invoke `dequeue_signal` until no nonblocked pending signals (handle then one by one). `dequeue_signal` handles private signals first, from low signal number to high.

`do_signal` will check if current receiver process is being monitored by others. If so, notify the parent of child stop and schedule to parent to aware of the signal handling.

Three kinds of actions:

1.  Ignoring the signal. `do_signal` simply continues.
2.  Executing a default action;
3.  Executing a signal handler;

### 11.3.1. Executing the Default Action for the Signal

`init` process (`pid==1`) will discard the signals received. For other ordinary processes, `do_signal` do the default action when `SIG_DFL`. 

If default action is *ignore*, just continue to next signal handling.

If default action is *stop*, set all processes in the thread group states to `TASK_STOPPED` and then `schedule()` to active processes. Also send `SIGCHLD` to parent process of the group leader.

If default action is *dump*, create a `core` file in process working directory. 

### 11.3.2. Catching the Signal

If there is a handler, then `do_signal` must execute the handler. After handling one signal, other pending signals *won't be considered* until next invocation of `do_signal`.

When handling, kernel need to take care of stacks carefully between user mode & kernel mode.

Signal handlers are defined by user mode and in user mode code segment. `handle_signal()` runs in kernel mode, handlers run in user mode. But after handing signals and return to kernel, the interrupt context are emptied (interrupt).

And handlers may invoke system call. 

Linux will copy the hardware context from kernel stack to user stack and then execute signal handler. When handler returns, copy the hardware context back from user stack to kernel stack. And restore the user context in user stack.







## 11.4 System Calls Related to Signal Handling

### Suspending the Process

`sigsuspend()` put process in `TASK_INTERRUPTIBLE` state. The process will wake up only when a nonignored, nonblocked signal is sent to it.

```c
mask &= ~(sigmask(SIGKILL) | sigmask(SIGSTOP));
saveset = current->blocked;
siginitset(&current->blocked, mask);
recalc_sigpending(current);
regs->eax = -EINTR;
while (1) {
    current->state = TASK_INTERRUPTIBLE;
    schedule();
    // process 2 run

    // When process 1 (caller) executed again
    // Start from here and deliver the signal that has awakened the process
    // If return 1, signal not ignored (awaken incorrectly)
    if (do_signal(regs, &saveset))
        return -EINTR;
}
```

`sigsuspend` does not equal to `sigprocmask() + sleep()`. Need to consider interrupt between the 2 syscalls.