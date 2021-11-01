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
EXIT_DEAD               process is being removed by kernel because parent wait
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

#### The process lists

`task_struct` are linked together. The head: Pid 0 **process 0 or swapper**

#### The lists of `TASK_RUNNING` processes

scheduler check this running queue. 

### How Processes Are Organized

group the processes by their states: running --> runqueue. `TASK_STOPPED, EXIT_ZOMBIE, EXIT_DEAD`, no list for them. just the list of all processes.

`TASK_INTERRUPTABLE, TASK_UNINTERRUPTIBLE` are divided into different classes, each for a specific event. 

#### Wait queues

(Blocked)

wait queues several uses. particularly for interrupt handling, process ysnc, timing. in general, a process must wait for some event to occur, e.g. disk read to terminate, 500ms to elapse, etc. Implement conditional waits on event. 

Multiple wait queues. Each wait queue should be protected from concurrent access. They are modified by interrupt handlers & kernel functions. 

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

## 9.4 Page Fault Exception Handler

### Handling Noncontiguous Memory Area Accesses

Kernel is lazy in updating page table entries for noncontiguous memory areas. `vmalloc()` and `vfree()` limit themselves in updating master kernel page tables (`init_mm.pgd` and its child tables).

Master kernel page tables are not directly used by any user process or kernel thread. When process in kernel mode first access a noncontiguous memory area address, MMU encounters a null page table entry, raises a page fault. _The handler finds this address is kernel address, then check master kernel page table entry._

If master kernel page table entry is null, goto `no_context` and hanldling this address as outside the address space (note that kernel thread is not having address space, this address space is not the regular process's address space). Else, 

# Chapter 10. System Calls

