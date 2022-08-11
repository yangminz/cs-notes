By Remzi H. Arpaci-Dusseau and Andrea C. Arpaci-Dusseau

# Chapter 4. The Abstraction: The Process

Process (memory): the running program (disk). Illusion of virtual CPUs: time sharing of the CPU -- concurrency, multi-tasking.

context switch + scheduling policy

## 4.1 The Abstraction: A Process

machine state: memory that the process can address - address space; registers; program counter, stack pointer, frame pointer, I/O information.

## 4.3 Process Creation: A Little More Detail

Load executable format assembly from disk into meory. Lazy loading: load part of code / data when they are needed -- demand paging & swapping.

create run-time stack & heap. file descriptors.

## 4.4 Process States

-   Running: the process is running: CPU program counter is executing the .text of this process
-   Ready: CPU is executing the .text of other process. But this process can be scheduled to be executed.
-   Blocked: not ready to run until some other event takes place. Common example: wait for I/O response from disk.

## 4.5 Data Structures

process list for all process that are ready & running. also track the blocked process. when I/O event completes, wake the correct process to ready.

```c
struct register_context
{
    uint64_t rip;
    uint64_t rsp;
    // ...
} register_context_t;

enum proc_state {UNUSED, EMBRYO, SLEEPING, RUNNING, RUNNABLE, ZOMBIE};

struct proc
{
    char *mem;  // start of process memory
    uint size;  // size of process memory
    char *kstack;   // bottom of kernel stack for this process
    enum proc_state state;  // process state
    int pid;    // process id hashcode
    struct proc *parent;    // process parent
    void *chan; // if !zero, sleeping on chan
    int killed; // if it's killed
    struct register_context context;    // switch here to run process
    struct trapframe *tf;   // trap frame for the current interrupt
};
```

process control block (PCB)

# Chapter 6. Mechanism: Limited Direct Execution

virutalization challenges:

1.  performance
2.  control

## 6.1 Basic Technique: Limited Direct Execution

direct execution: run the program directly on CPU. pro: fast; problem: cannot control the process; cannot time sharing.

limitations on the process, OS be in control of everything, **just a library**!

## 6.2 Problem 1: Restricted Operations

system calls are like typical procedure call in C. It is a procedure call, but hide the famous trap instruction. hardware provide the mode bit. ISA also provide trap instruction and return-from-trap to user mode process. OS tells the hardware where is trap table in memory.

**user mode**: a processor mode. **kernel mode**: the os kernel runs code. 

user process perform system call to perform privileged operations. To execute syscall, process execute a special **trap** instruction. (so trap is the tool for process to get into kernel voluntarily, but context switch is different and another mechanism that the process would not feel)

Trap instruction jumps into kernel and raise the privilege level to kernel mode. then do the work, and call a special return-from-trap instruction to return to user mode.

when trap, need to save caller's registers to return back. X86: processor push PC, flags, a few other registers onto a **per-process kernel stack**. when return, pop the values off the stack and resume the execution of user mode. (Intel manuals *Intel 64 and IA-32 Architectures Software Developer’s Manual* Volume 3A and 3B)

trap jump destination (e.g. when I call `read`, to which address should `rip` jump to): decided by the trap table (set up at boot time): syscall no --> trap handler. the hardware records the location of these handlers until next rebooting. 

```
OS @ boot                       Hardware
(kernel mode)
-------------------------------------------------------------------------
initialize trap table
                                remember address of...
                                syscall handler
-------------------------------------------------------------------------
OS @ run                        Hardware                    Program
(kernel mode)                                               (user mode)
-------------------------------------------------------------------------
Create entry for process list
Allocate memory for program
Load program into memory
Setup user stack with argv
Fill kernel stack with reg/PC
return-from-trap
                                restore regs
                                (from kernel stack)
                                move to user mode
                                jump to main
                                                            Run main()
                                                            ...
                                                            Call system call
                                                            trap into OS
                                save regs
                                (to kernel stack)
                                move to kernel mode
                                jump to trap handler
Handle trap
Do work of syscall
return-from-trap
                                restore regs
                                (from kernel stack)
                                move to user mode
                                jump to PC after trap
                                                            ...
                                                            return from main
                                                            trap (via exit())
Free memory of process
Remove from process list
```

## 6.3 Problem 2: Switching Between Processes

crux: how to regain control of CPU when OS is not running to do context switch?

#### A Cooperative Approach: Wait For System Calls

old MacOS: cooperative approach: trust the process will periodically give up CPU so the OS can reschedule.

explicit `yield` syscall: do nothing except to transfer control to OS so it can run other processes.

For other exceptions, e.g. divides by zero, illegal memory access, also trap kernel and OS will take control.

Only in this case will the CPU be occupied forever: spinning (no syscall in `while`)

```
while (1);
```

#### A Non-Cooperative Approach: The OS Takes Control

Need help from hardware. time interrupter since only spining will block OS to get control. trigger the interrupt handler (**Not trap handler**) in OS.

when interrupt, hardware save enough states of process to kernel stack for future resuming. *These states (saved by **interrpt**) can be restored by return-from-**trap** instruction.*

#### Saving and Restoring Context

context switch: save register values for the current process and restore them when back to execution.

1.  execute low-level assembly code to save the general purpose registers, PC and kernel stack pointer of current process [A]. This must be implemented by the assembly since we want to save the particular registers. No C here.
2.  restore the context of the scheduled-to-run process [B].
3.  switch to the kernel stack of [B]. context switching code is like a connecting door between [A] and [B]: go in from [A], go out from [B]
4.  execute the return-from-trap instruction.

2 types of register store/restore:

1.  time itnerrupt: **user register**s are implicitly saved by hardware to kernel stack of the process
2.  OS switch from A to B: **kernel registers** are explicitly saved by OS to `task_struct->context`. making OS from running as if it just trapped into the kernel from A to as if it just trapped into the kernel from B.

```
OS @ boot                           Hardware
(kernel mode)
-----------------------------------------------------------------
initialize trap table
                                    remember addresses of...
                                    syscall handler
                                    timer handler
start interrupt timer
                                    start timer
                                    interrupt CPU in X ms
-----------------------------------------------------------------
OS @ run                            Hardware                Program
(kernel mode)                                               (user mode)
-----------------------------------------------------------------
                                                            Process A
                                                            ...
                                    timer interrupt
                                    save regs(A) -> k-stack(A)
                                    move to kernel mode
                                    jump to trap handler
Handle the trap
Call switch() routine
save regs(A) -> proc t(A)
restore regs(B) <- proc t(B)
switch to k-stack(B)
return-from-trap (into B)
                                    restore regs(B) <- k-stack(B)
                                    move to user mode
                                    jump to B’s PC
                                                            Process B
                                                            ...
```

```
# void swtch(struct context **old, struct context *new);
#
# Save current register context in old
# and then load register context from new.
.globl swtch
swtch:
    # Save old registers
    movl 4(%esp), %eax          # put old ptr into eax
    popl 0(%eax)                # save the old IP
    movl %esp, 4(%eax)          # and stack
    movl %ebx, 8(%eax)          # and other registers
    movl %ecx, 12(%eax)
    movl %edx, 16(%eax)
    movl %esi, 20(%eax)
    movl %edi, 24(%eax)
    movl %ebp, 28(%eax)

    # Load new registers
    movl 4(%esp), %eax          # put new ptr into eax
    movl 28(%eax), %ebp         # restore other registers
    movl 24(%eax), %edi
    movl 20(%eax), %esi
    movl 16(%eax), %edx
    movl 12(%eax), %ecx
    movl 8(%eax), %ebx
    movl 4(%eax), %esp          # stack is switched here
    pushl 0(%eax)               # return addr put in place
    ret                         # finally return into new ctxt
```

## 6.4 Worried About Concurrency?

what if time interrupts during syscall? This is the concurrency problem.

simple solution: disable interrupts during processing interrupts. but blocking too long will lost interrupts.

another way: locking

# Chapter 13. The Abstraction: Address Spaces

# Chapter 23. Complete Virtual Memory Systems

Key elements: page-table designs, interactions with TLB, page replacement strategies, etc.

Complex query when put user page tables in kernel virtual memory. 

Page 0 is marked inaccessible to support **null-pointer** access detection. When process access virtual address `0`, page table founds it's invalid access, transfers control back to OS.

The kernel is mapped into each address space. ==> Kernel appears almost as a library to applications, a protected one.

clean-page free list, dirty-page free list.

Clustering: group large batches of pages together.

Lazy optimizations: 

1. **Demand zeroing**: E.g., when heap requests one new page, put an entry in page table and *marks it inaccessible*. Trap to OS, OS finds it's a demand-zero page, then find a physical frame and zero it. 
2. **Copy on write**: When OS needs to copy one page from one address space to another, instead of copying, map it into target address space and mark it **read-only** in **both address spaces**. When write, OS finds it's a COW page, and then allocate new page and make a private copy.

Linux kernel virtual addresses: 2 types

1.  **Kernel Logical Addresses** - the normal virtual address space of the kernel. `kmalloc`. Most of kernel data structures: page table, per-process kernel stacks, etc. are here. CANNOT be swapped to disk. 

Direct mapping between kernel *logical* address and *first portion* of physical memory. (Logical `0xC0000000`, Physical `0x00000000`), (Logical `0xC0000FFF`, Physical `0x00000FFF`). Continous. I/O transfer to and from devices via **Directory Memory Access**.

2.  **Kernel Virtual Addresses** - `vmalloc`. Usually not contiguous, kernel virtual page may map to non-contiguous physical frames. Easier to allocate.

# Chapter 26. Concurrency and Threads

# Chapter 28. Locks

# Chapter 29. Locked Data Structures

# Chapter 30. Condition Variables

# Chapter 31. Semaphores

# Chapter 32. Concurrency Bugs

# Chapter 33. Event-Based Concurrency

# Chapter 36. I/O Devices

## 36.1 System Architecture

CPU & DRAM: memory bus

CPU & GPU, display: general I/O bus, PCIe

CPU & disk, mice, keyboards: peripheral bus, e.g. USB

speed vs cost ==> hierarchy

```
            PCIe Graphics           Memory Interconnect
Graphics <-----------------> CPU <-----------------------> Memory
                              ^
                              |
                              | DMI
                              |
            PCIe              v         eSATA
Network <----------------> I/O Chip <--------------------> Disk
                              ^
                              |
                              | USB
                              |
                              v
                       Keyboard, Mouse
```

## 36.2 A Canonical Device

2 components of a device:

1.  Hardware interface: exposed to OS
2.  Internal structure: implementation

E.g.

```
+---------------------------------------+
|   Registers: Status, Command, Data    |   interface
+---------------------------------------+
|   CPU, DRAM, other chips              |   implementation
+---------------------------------------+
```

## 36.3 A Canonical Protocol

A typical interaction:

```
While (Status == Busy)              // polling
    ;                               // polling

Write data to Register.Data;        // data movement:
Write command to Register.Command;  // programmed I/O, PIO

While (Status == Busy)              // polling
    ;                               // polling
```

**Polling** the device: OS waits until device is ready to receive a command by repeatedly reading the status.

Polling is wasting CPU time. 

## 36.4 Lowering CPU Overhead With Interrupts

OS issue a request, put the calling process to sleep, then context switch to another process. When device is finished, raise a hardware interrupt, causing CPU to jump into the OS at predetermined **interrupt service routine (ISR)**, or **interrupt handler**. 

Interrupt allows overlapping of CPU and I/O, improving CPU utilization time:

```
process 1                       polling         process 1
+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+
| 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | p | p | p | p | 1 | 1 | 1 | 1 |   CPU
+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+
                                | 1 | 1 | 1 | 1 |                   Device
                                +---+---+---+---+
                                process 1
```

With interrupt:

```
process 1                       process 2       process 1
+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+
| 1 | 1 | 1 | 1 | 1 | 1 | 1 | 1 | 2 | 2 | 2 | 2 | 1 | 1 | 1 | 1 |   CPU (context switch)
+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+
                                | 1 | 1 | 1 | 1 |                   Device
                                +---+---+---+---+
                                process 1
```

Spin lock is good only when the device task is quick, in this case, interrupt is not good.

Or use hybrid: two-phased approach, poll a little while, then interrupt.

**Network: not use interrupt.** A huge stream, each packet an interrupt, **live lock**: OS is only processing interrupts and never allowing user process to run and serve the request!!! It's better to ocasionally use polling to allow web server to serve some requests before going back to the device to check for more packet arrivals.

> Eliminating Receive Livelock in an Interrupt-driven Kernel
> not flow-controlled application protocol: multi-media app. Constant-rate, low-latency service; UDP instead of TCP. 
> **Receive Livelock**
> Solution: improve the purely interrupt-driven model and guarantee throughput and latency under overload

Coalescing the interrupts: device waits for next interrupt before sending signal to CPU. Merge the data from 2 interrupts, and send one interrupt only. 

## 36.5 More Efficient Data Movement With DMA

When PIO is transfering many data to device, CPU overburdened with trivial task, and wasting time.

```
process 1           p1 copying  process 2       process 1
+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+
| 1 | 1 | 1 | 1 | 1 | c | c | c | 2 | 2 | 2 | 2 | 1 | 1 | 1 | 1 |   CPU (copying)
+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+
                                | 1 | 1 | 1 | 1 |                   Device
                                +---+---+---+---+
                                process 1
```

How to lower PIO overheads? CPU is spending too much time moving data to and from device.

**Direct Memory Access (DMA)**. DMA engine is a special device can orchestrate transfers between device & main memory _without much CPU help_.

```
process 1           process 2                   process 1
+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+
| 1 | 1 | 1 | 1 | 1 | 2 | 2 | 2 | 2 | 2 | 2 | 2 | 1 | 1 | 1 | 1 |   CPU
+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+
                    | c | c | c |                                   DMA copying
                    +---+---+---+---+---+---+---+
                                | 1 | 1 | 1 | 1 |                   device
                                +---+---+---+---+
                                process 1
```

## 36.6 Methods of Device Interaction

2 primary methods of device communication:

1.  Explicit I/O instructions in ISA (old method)
2.  Memory mapped I/O: take device registers as memory locations.

Both in use today.

## 36.7 Fitting Into The OS: The Device Driver

Build a file system working on top of SCSI disks, IDE disks, USB keychian drivers, etc. Use the FS to issue read or write requests to different types of devices. -- Device driver to do the abstraction.

```
Application
------------------------- POSIX API [open, read, write, close, etc]
File System | Raw
------------------------- Generic Block Interface [block read/write]
Generic Block Layer
------------------------- Specific Block Interface [protocol-specified read/write]
Device Driver [SCSI, ATA, etc]
```

# Chapter 39. Interlude: Files and Directories










