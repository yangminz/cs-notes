2017 Edition By Russ Cox, Frans Kaashoek, Robert Morris

# Chapter 0. Operating System Interfaces

When a process needs to inovke a kernel service, it invokes a procedure call in OS interface. -- syscall.process alternates between user space & kernel space.

CPU's hardware protection mechanism: user mode process will access its own address space only.

## Process and Memory

Xv6: time sharing. Transparently switch the CPU among the processes. 

`fork`: child and parent have the same memory content, they are executing with different address space & different registers. 

`exec`: allocate enough memory to hold the executable. build up memory image.

`sbrk`: request more during run-time

## Real World

Xv6 is not POSIX compliant: implement syscalls partially. 

# Chapter 1. Operating System Organization

key requirement: 

1.  **Multiplexing**: support several activities at once. -- time-share of the resources: CPU, registers, etc. 
2.  **Isiolation**: isolation between processes. process will not crash other processes
3.  **Interaction**

Xv6 design: monolithic kernel. 80386 Platform

## Abstracting Physical Resources

Implement the syscalls as a library, with which the process link. For embedded devices or real-time systems, applications interact with hardware resources directly. MacOS: cooperative time-sharing system: periodically gives up CPU to other processes. Applications trust each other, optimistic.

Syscalls are carefully designed to provide both useability & isolation. _It's the way to abstract resources._

## User Mode, Kernel Mode and System Calls

Strong isolation: hard boundary between application and kernel. Application error should not fail the OS.

Hardware support for isolation: X86 processor has user mode and kernel mode. In kernel mode, process is allowed to execute privileged instructions, e.g. R/W disks. In user mode, process can only execute user-mode instructions.

User-mode process wants to execute privileged operations, must trap to kernel mode. Hardware ISA provides such instruction to switch from user mode to kernel mode, goto the entry point defined by kernel at boot time, x86: `int`. 

Once trapped in kernel mode, kernel verifies the arguments of syscall, do the job.

## Kernel Organization

Problem: what part of OS should be in kernel mode? Unix, monolithic kernel: the entire of operating system is in kernel. In this organization, OS runs with full hardware privilege. Downside: interface between different parts of OS is complex.

Micro kernel: minimize the OS code running in kernel mode, execute the majority of OS in user mode. Define the kernel service.

## Process Overview

The unit of isolation is **Process**. Process abstraction prevents one process corrupt the resource owned by another process. To enforce isolation, process abstraction provides the illusion to program that it has its own private machine. The private memory system, _address space_. 

Page tables give each process its own address space: mapping virtual address (program & CPU generated) to physical address (hardware). Each process has its own page table & that defines the address space. _Page table maps kernel's text & data also._ When syscall, the syscall executes in the kernel mappings of the process's address space. This helps kernel to directly use user memory. Xv6 maps the kernel at high address.

Xv6 kernel: process struct `struct proc`. Each process = (thread of execution + address space + other resources). Thread can be suspended or later resumed. To switch, kernel suspends the current thread and resumes another process's thread. 

Each process has 2 stacks: user stack & **kernel stack**. When process traps into kernel mode, kernel code runs on kernel stack, the user stack remains unchanged but is not used. They are seperated so kernel stack can work even when user stack is corrupted.

When makes syscall and trap:

1.  `rsp` switch from user stack to kernel stack
2.  CPU raise hardware privilege level to kernel mode
3.  `rip` jump to kernel code entry point through interrupt table
4.  execute the syscall in kernel code

`proc->state` indicates the states of process: allocated, ready to run, running, waiting for I/O, exiting

`proc->pgdir` is the process's page tabe.

## Code: The First Address Space

-   How the kernel creates the first address (for itself)
-   How the kernel creates & starts the first process
-   How the first process performs the first syscall

When PC boots on, load **boot loader** from disk into memory and executes it. Boot loader will load xv6 kernel from disk and executes it at entry point, e.g. physical address `0x100000`. Hardware paging is not yet enabled, virtual addresses map directly to physical addresses. Why `0x100000`, not too high and not too low: low address are holding I/O devices; high address may not available for small RAM.

`main.c` will set up a page table that maps `va: 0x80000000` (`KERNBASE` kernel base) to `pa: 0x0`. 2 ranges of virtual addresses maps to the same physical memory range.

```C
<main.c>

// The boot page table used in entry.S and entryother.S.
// Page directories (and page tables) must start on page boundaries,
// hence the __aligned__ attribute.
// PTE_PS in a page directory entry enables 4Mbyte pages.

__attribute__((__aligned__(PGSIZE)))
pde_t entrypgdir[NPDENTRIES] = {
  // Map VA's [0, 4MB) to PA's [0, 4MB) - user space
  [0] = (0) | PTE_P | PTE_W | PTE_PS,
  // Map VA's [KERNBASE, KERNBASE+4MB) to PA's [0, 4MB) - kernel space
  [KERNBASE>>PDXSHIFT] = (0) | PTE_P | PTE_W | PTE_PS,
};
```

Above code: entry 0 maps VA's [0, 4MB) to PA's [0, 4MB), this is needed as long as `entry` is executing at low addresses, but will eventually be removed. Entry 512 (`KERNBASE>>PDXSHIFT = 0x80000000>>22 = 0x1000 = 512`) will be used by kernel after `entry` has finished. It maps high virtual address of kernel text and data to low RAM addresses so the boot loader can load them. Kernel text & data are restricted to 4MB.

Then `entry` loads `entrypgdir` above into `%cr3`, this must be a physical RAM address. Then set `CR0_PG` bit in `%cr0` register to enable paging. Now CPU is still executing codes in low addresses. If no entry0, computer will crash after enabling paging. `%rip = low` (physical) ---paging--> `%rip = low` (virtual), `%rip` value is unchanged, but with entry0, it's virtual now.

Then set up `%rsp` to point to stack. All kernel symbols (.text, .data, stack) have high addresses (entry 512), so they are safe when entry0 is removed. 

Finally, `entry` jumps to `main` of kernel (entry512), this jump is implemented by a PC-relative direct jump. Now the kernel is runnign in high addresses in function `main()`.

## Code: Creating the First Process

Now kernel creates user-level processes and ensures isolation.

`main()` will initialzie several devices & subsystems, then create the first process by calling `proc.c:userinit()`. `userinit()` will first allocate process struct through `proc.c:allocproc()`. `allocproc()` will set log, check the list to see if there is free (`UNUSED`) proc (like thread pool, this is the process pool), if found, release lock. 

With this free proc slot in _process table_, `allocproc` will set up the new process's (state `EMBRYO`) kernel stack. Kernel prepares the kernel stack (`kalloc()`) and a set of kernel registers so the process will return to user space when it runs for the first time. The kernel stack:

```
+-----------+ <=== top of new stack // 1
|   esp     |
+-----------+
|   ...     |
+-----------+
|   eip     |
+-----------+
|   ...     |
+-----------+
|   edi     |
+-----------+ <=== p->tf // 2
|   trapret |
+-----------+ <=== address forkret will return to // 3
|   eip     |
+-----------+
|   ...     |
+-----------+
|   edi     |
+-----------+ <=== p->context   // 5: context are all zeros
|   EMPTY   |
+-----------+ <=== p->kstack
```

set the rturn PC values: new process's kernel thread will first execute in `forkret` and then in `trapret`:

```C
  p->state = EMBRYO;
  p->pid = nextpid++;

  release(&ptable.lock);

  // Allocate kernel stack.
  if((p->kstack = kalloc()) == 0){
    p->state = UNUSED;
    return 0;
  }
  sp = p->kstack + KSTACKSIZE;  // 1

  // Leave room for trap frame.
  sp -= sizeof *p->tf;              // 2
  p->tf = (struct trapframe*)sp;    // 2

  // Set up new context to start executing at forkret,
  // which returns to trapret.
  sp -= 4;                      // 3
  *(uint*)sp = (uint)trapret;   // 3

  sp -= sizeof *p->context;             // 5
  p->context = (struct context*)sp;     // 5
  memset(p->context, 0, sizeof *p->context);    // 5
  p->context->eip = (uint)forkret;

  return p;
```

`userinit()` calls `setupkvm` to create a page table and resulting the kernel address space: {kernel .text, kernel .data, ...}.

```C
  p = allocproc();
  
  initproc = p;
  if((p->pgdir = setupkvm()) == 0)
    panic("userinit: out of memory?");
```

The first user process `initcode.S` compile its user-space memory in assembly. The linker will embeds the kernel variable with user process. `userinit` copies binary into new process's memory by `vm.c:inituvm`: allocate one page frame, maps va `0x0` to this frame, copy binaries into the page. Mapping size must be smaller than 1 Page.

```
  inituvm(p->pgdir, _binary_initcode_start, (int)_binary_initcode_size);
```

Then `userinit` sets up the trap frame with initial user mode state: `%cs, %ds, %es, %ss, %eflags` registers. `%esp` is the max virtual address (no random); `%eip` is `0` lowest virtual address (like `0x00400000` in x86-64, .text entry point).

```C
  memset(p->tf, 0, sizeof(*p->tf));
  p->tf->cs = (SEG_UCODE << 3) | DPL_USER;
  p->tf->ds = (SEG_UDATA << 3) | DPL_USER;
  p->tf->es = p->tf->ds;
  p->tf->ss = p->tf->ds;
  p->tf->eflags = FL_IF;    // interrupt flag
  p->tf->esp = PGSIZE;
  p->tf->eip = 0;  // beginning of initcode.S
```

## Code: Running the First Process

`scheduler` looks for a process `RUNNABLE`, there is only one: `initproc`. `proc` is the per-CPU variable (one CPU can run only one process), set it as `initproc`. Call `switchuvm` to tell the hardware to start using the target process's page table. Also set up the task state segment `SEG_TSS` that instructs the hardware to execute syscalls and interrupts on the process's kernel stack.

`scheduler` then call `swtch` to do context switch to the target process's kernel thread:

1.  Save the current registers. But current context is not a process, it's scheduler. So the hardware registers are stored in `cpu->scheduler`, instead of the kernel thread context. 
2.  `swtch` then loads the saved registers of the target kernel thread (`p->context`) into X86 hardware registers, including `%esp, %eip`
3.  Finally, `ret` pops the target process's `%eip` from stack, finish context switching. Now CPU is running on the kernel stack of process `p`.

`ret` starts executing `forkret`.

`trapret` starts executing, `%esp` set to `p->tf`.

Finally, `%eip = 0 && %esp = 4096`, the virtual adresses in the process's address space. Set up the page tables so the process is constrained to use its own memory.

## The First System Call: `exec`

User level process re-enters the kernel -- trap.

```
  movl $SYS_exec, %eax
  int $T_SYSCALL
```

`exec` replaces memory, registers, but file descriptors, process id, parent process unchanged.


## Real World

A real OS will find `proc` with an explicit free list in constant time instead of linear-time search. Another problem: address space layout cannot make use of RAM > 2GB.

# Chapter 2. Page Tables

OS uses page tables to control memory address. Multiplex the address spaces of different processes onto a single physical memory, and to protect the memories of different processes. Page table tricks:

1.  Mapping the same memory (the kernel) in several address spaces
2.  Mapping the same memory more than once in one address space. Each user page is also mapped into kernel's physical view of memory.
3.  Guarding a user stack with an unmapped page.

## Paging Hardware

X86 page table: PTE: 20-bit PPN/PFN and some flags. Use high 20 bits of PTE to index into page table to find next level PTE. Virtual-to-Physical address translation in page unit.

PTE low 12 control bits:

1.  P - Present
2.  W - Writable
3.  U - User
4.  WT - Write through or write back
5.  CD - Cache Disabled
6.  A - Accessed
7.  D - Dirty
8.  AVL - Available for system use

## Process Address Space

OS tells page table hardware to switch page tables when OS switches between processes. 

Mappings for kernel: Fixed mapping: `KERNBASE:KERNBASE+PHYSTOP` to `0:PHYSTOP`. So kernel can use its own instructions and data. Sometimes kernel needs to write a given physical page, e.g. when creating pages for page tables. So it's better physical page appear at a predictable virtual address. 

Every process's page table contains mapping for both user space & kernel space: easy switch when trap into kernel during syscalls or interrupts: _No page table switch is needed. In most cases, kernel does not have its own page table, it borrows the page table of last process._

## Code: Creating an Address Space

`kvmalloc` to create & switch to page table in kernel space. `setupkvm`:

1.  Allocate a page to hold page directory
2.  `mappages` to install the translations that the kernel needs, described in `kmap` array. Including the kernel .text, data, physical memory up to `PHYSTOP`. No user space mapping is installed
3.  `mappages` installs mappings into a page table for a range of virtual addresses to physical addresses. Call `walkpgdir` to find the PTE for that address, initialize the PTE with PFN, flags.
4.   

## Physical Memory Allocation

Kernel allocates and frees PFN at run-time for page tables, user space, kernel stacks, pipe buffers. Xv6: `END OF KERNEL:PHYSTOP` for run-time allocation. Tracking the free pages.

-   Allocation: remove a page from the linked list
-   Free: adding a freed page to the list

**A Bootstrap Problem**: All physical frames must be allocated for the allocator to initialize the free list, but creating a page table with thoese mappings involves allocating **page-table pages**. Xv6 solution: use a separate page allocator during entry, 4MB mapping.

## Code: Physical Memory Allocator

Free list of physical frames free for allocation, each element `struct run`. The allocator stores each free frame's `run` within the frame itself (malloc lab). The free list is protected by a spin lock. 

# Chapter 3. Traps, Interrupts, and Drivers

When control of CPU must be transfered from user process to kernel:

1.  Device signaling, e.g. I/O event happens
2.  User space illegal, e.g. invalid access
3.  User process syscall

Challenges:

1.  Processor switches from user mode to kernel mode (and back)
2.  Kernel and device coordinate their parallel activities
3.  Kernel needs to understand device's interface

## System Calls, Exceptions, and Interrupts

3 cases to trap to kernel:

1.  Syscall, e.g. `exec`
2.  Exception, e.g. divide by zero
3.  Interrupt, e.g. time interrupt or disk I/O interrupt

All these cases, OS do the following:

1.  Save the user process's registers for future transparent resume
2.  OS be set up for kernel space execution thread
3.  OS defines the entry point for kernel to start executing
4.  Kernel should know the details of the event
5.  User and kernel must be isolated

So OS needs to be aware how hardware handles syscall, exception, and interrupts. In most CPUs, these 3 are handled bby a single hardware mechanism - interrupt handling. Xv6 term: **trap**, a conventional Unix term. X86 term: interrupt. Actually, trap: caused by running process (fault), interrupt: caused by device (disk I/O event).

An interrupt stops CPU's instruction loop, switch to execute a **interrupt handler**. Before handling, CPU saves its registers so that OS can restore them when returns from the interrupt. 

## X86 Protection

X86 4 protection levels: ring 0 (most privileged) and ring 3 (least privileged). OS use 2: ring 0 for kernel mode & ring 3 for user mode. `%cs` register **CPL** field stores the level, Current Privilege Level. DPL - Descriptor Privilege Level for each segment descriptor. RPL - Request Privilege Level, for segment selector. 

X86: interrupt handlers defined by **Interrupt Descriptor Table (IDT)**: 256 entries, each provides `%cs` and `%eip` for handling. `int n` instruction check `IDT[n]`. `int` do the following:

-   Fetch the n’th descriptor from the IDT, where n is the argument of int.
-   Check that CPL in %cs is <= DPL, where DPL is the privilege level in the descriptor.   
    -   CPL <= DPL allows forbid `int` calls to inappropriate IDT entries such as device interrupt routines
    -   User process execute `int`, `IDT[n].cs.DPL == 3`, current descriptor is in user mode
    -   If user program does not have correct privilege, then result in `int 13`, general protection fault.
-   Save %esp and %ss in CPU-internal registers, but only if the target segment selector’s PL < CPL.
-   Load %ss and %esp from a task segment descriptor.
-   Push %ss.
-   Push %esp.
-   Push %eflags.
-   Push %cs.
-   Push %eip.
-   Clear the IF bit in %eflags, but only on an interrupt.
-   Set %cs and %eip to the values in the descriptor.

Hardware uses the stack specified in TSS, set by kernel. Kernel stack after `int`:

```
+-----------+   \
|   ss      |   |   only present on privilege change (privilege level in descriptor < CPL)
+-----------+   +   if int does not need privilege-level change (Kernel mode interrupt to Kernel mode),
|   esp     |   |   no these registers
+-----------+   /
|   eflags  |
+-----------+
|   cs      |
+-----------+
|   eip     |
+-----------+
|   error c |
+-----------+ <--- esp: grow down: kernel stack used by kernel
|   EMPTY   |
+-----------+
```

`iret` return from `int` instruction: pops the saved values from kernel stack, resumes execution at saved `%eip`.

## Code: Assembly trap handlers

X86 allows for 256 different interrupts. 0-31 defined for software exceptions/faults. 64 is for system call interrupt.

When system call, the gate is _trap_. Do not clear `IF` flag, allowing other interrupts during syscall handling. Set privilege: user program to trap into kernel. Kernel should not use user stack because it may be invalid. 

X86 hardware is programed to perform stack switch on trap by setting up task segment descriptor through which the hardware loads a stack segment selector and a new value for `%esp`. 

When traps (`int 64`), save stack and PC, error no, flags. Each entry pushes an error code if CPU didn't, then push interrupt number, then jumps to `alltraps`.

**This `alltraps` is part of OS's interrupt handler now. It's not provided by hardware.**

`alltraps` continues to save processor registers: from `%ds` to `%rax`, this is the trap frame. **CPU pushes `SS, ESP, EFLAGS, CS, EIP`, CPU or trap vector pushes error number, `alltraps` pushes the rest.** These are the necessary info to resume the user process execution. Including `%rax`, the system call number.

After `alltraps`, user mode status are all saved. Now prepare to run kernel C code. CPU sets `CS, SS`, `alltraps` sets `DS, ES`. `alltraps` call C trap handler `trap`, push `%esp` (the trap frame just created) to stack as **argument to `trap`**. 

Then call `trap` with argument, the old kernel stack's top, trap frame.

When `trap` returns from kernel to user, pop arguments off the stack, then execute code at label `trapret`. Finally `iret` to jump to user space.

Trap in kernel: no stack switch.

```
 cpu->ts.esp0
 ------------->  +------------+ --+
             /   | ss         |   | Only present on
            /    +------------+   | privilege change
            |    | esp        |   |
            |    +------------+ --+
Processor   |    | eflags     |
pushed      |    +------------+
int n inst  |    | cs         |
            |    +------------+
            |    | eip        |
            |    +------------+
            +--  | error no   | --+
                 +------------+   |
                 | General    |   | alltrap pushed
                 | purpose    |   | trapframe
  esp            | registers  | --+
  ------------>  +------------+
                 |            |
                 | Empty      | used by kernel functions, e.g. kmalloc
  pcb->kstack    |            |
  ------------>  +------------+
```

Linux:

Register IDT:

```c
arch/x86/include/asm/irq_vectors.h
#define IA32_SYSCALL_VECTOR		0x80

arch/x86/kernel/idt.c
#if defined(CONFIG_IA32_EMULATION)
	SYSG(IA32_SYSCALL_VECTOR,	entry_INT80_compat),
#elif defined(CONFIG_X86_32)
	SYSG(IA32_SYSCALL_VECTOR,	entry_INT80_32),
```

Entering system call

```c
arch/x86/entry/entry_32.S

/*
 * 32-bit legacy system call entry.
 *
 * 32-bit x86 Linux system calls traditionally used the INT $0x80
 * instruction.  INT $0x80 lands here.
 *
 * This entry point can be used by any 32-bit perform system calls.
 * Instances of INT $0x80 can be found inline in various programs and
 * libraries.  It is also used by the vDSO's __kernel_vsyscall
 * fallback for hardware that doesn't support a faster entry method.
 * Restarted 32-bit system calls also fall back to INT $0x80
 * regardless of what instruction was originally used to do the system
 * call.  (64-bit programs can use INT $0x80 as well, but they can
 * only run on 64-bit kernels and therefore land in
 * entry_INT80_compat.)
 *
 * This is considered a slow path.  It is not used by most libc
 * implementations on modern hardware except during process startup.
 *
 * Arguments:
 * eax  system call number
 * ebx  arg1
 * ecx  arg2
 * edx  arg3
 * esi  arg4
 * edi  arg5
 * ebp  arg6
 */
SYM_FUNC_START(entry_INT80_32)
	ASM_CLAC
	pushl	%eax			/* pt_regs->orig_ax */

	SAVE_ALL pt_regs_ax=$-ENOSYS switch_stacks=1	/* save rest */

	movl	%esp, %eax
	call	do_int80_syscall_32
```

**Note that: when trapped into kernel, all registers are user process register values, only rip & rsp are kernel values, saved in TSS before. So how do we find the task PCB? Use `thread_info` to get the current process.**

## Code: Interrupts

Devices on motherboard can generate interrupts. OS must set up the hardware to handle these itnerrutps. Devices interrupt to tell the kernel that some hardware event has occured, e.g. I/O completion. Interrupts are usually optional in the sense that kernel could instead periodically check (or **poll**) the device hardware to check for new events. But polling would waste CPU time. 

Hardware generated interrupts can happen at any time. E.g. timer: 100 interrupts per second, not swamping CPU.

Early boards have Programmable Interrupt Controller, PIC. CPU needs: 

1.  An interrupt controller to handle the interrupt sent to CPU
2.  Routing interrupts to processors

2 parts to do this:

1.  I/O system: IO APIC
2.  CPU local: Local APIC

Xv6 ignores interrupts from PIC and configures IOAPIC and Local APIC.

IO APIC has a table. CPU can edit entries in the table through **memory-mapped I/O**. In initialization, OS maps interrupt i to IRQ i, but disables them all. They are enabled by the device.

Timer chip is inside LAPIC, so each processor can receive timer interrupt independently. LAPIC to periodically generate an interrupt at IRQ_TIMER, which is IRQ 0. The interrupt would be routed to local processor.

Timer interrupts through vector 32 (xv6 IRQ 0), an interrupt gate. Vector 64 is used for syscalls, a trap gate. Interrupt gates clear `IF` (interrupt flag) so the interrupted processor will be blocked - not receive any other interrupts while handling the current. Then interrupt follows the same code path as syscall & exceptions. Build trap frame.

Timer interrupt do 2 things:

1.  Increase tick
2.  call `wakeup`

## Real World

Typically devices are slower than CPU, so the hardware uses interrupts to notify the operating system of status changes.

# Chapter 5. Scheduling

Time-sharing transparent to user processes. Each process has the illusion that it has its own virtual processor by multiplexing the hardware resources.

## Multiplexing

Xv6: `sleep` & `wakeup`: process

1.  waits for device to complete
2.  waits for a child to exit
3.  waits in `sleep` syscall

Xv6 periodically forces a switch through timer.

Challenges:

1.  How to switch? The standard mechanism
2.  How to do it transparently? timer interrupt handler to drive context switches
3.  Many CPUs may switching concurrently, locking is needed to avoid races.
4.  When process exits, all other resources should be freed, but kernel stack is still in use
5.  Multiprocessor, the core needs to know which process it is running

Processes need to coordinates among themselves, e.g. parent process wait for one child to exit. Repeatedly checking the desired event: CPU-wasting. Insteand, OS allows process to give up CPU and sleep, waiting for an event. Avoid race condition.

## Code: Context Switching

```
shell
    trap/interrupt
kstack - shell
    switch
kstack - scheduler
    switch
kstack - cat
    iret
cat
```

2 context switches!! For scheduler to run on its own kstack. 

Each CPU has a separate scheduler thread for use, instead of any process's kernel thread. _`%esp, %eip` are saved and restored means that the CPU will switch stacks and code._

`swtch` does not know thread, it just store and restore registers (`context`). 

scheduler's context: `cpu->scheduler`, this is the per-CPU scheduler context.

`swtch` saves the old context:

1.  Copy the arguments `%eax, %edx` from stack before change `%esp`. Because arguemnts are no longer accessible via `%esp`.
2.  Only the callee-saved registers need to be saved, X86 convention: `%ebp, %ebx, %esi, %edi, %esp`. Push `%ebp, %ebx, %esi, %edi` to stack.
3.  Save `%esp` to `old->context`. 
4.  `%eip` is already on stack by `call` instruction: `call swtch`. **The old context is saved now.**

Restores the new context (scheduler):

1.  `%esp` to new stack pointer. New kernel stack is the same format.
2.  Pop `%edi, %esi, %ebx, %ebp`
3.  `swtch` return will restore `%eip`

## Code: Scheduling

Lock & release.

**Coroutines**: The procedures in which _this_ stylized switching between 2 threads: A kernel thread always gives up CPU in `sched` and always switches back to this same location in scheduler, which almost always switches to some kernel thread that previously called `sched`.

Scheduler's call to `swtch` does not end up in `sched`. `forkret`.

**Round Robin**

## Code: `mycpu` and `myproc`

CPU identifies which process it's running, process identifies the CPU.

Local APIC will help find the CPU - `mycpu` scan the CPU array.

`myproc` to find the running process. Disable interrupt and call `mycpu` to find.

## Code: `sleep` and `wakeup`

Processes' interaction. `sleep` & `wakeup`: **sequence coordination or conditional synchronization**. 

Sleep channel. -- wait queue in Linux. 

## Code: `wait`, `exit` and `kill`

`wait` system call that a parent process uses to wait for a child to exit. When a child exits, it does not die immediately. Instead, it switches to the `ZOMBIE` process state until the parent calls wait to learn of the exit. The parent is then responsible for freeing the memory associated with the process and preparing the `struct proc` for reuse. If the parent exits before the child, the `init` process adopts the child and waits for it, so that every child has a parent to clean up after it.

An implementation challenge is the possibility of races between parent and child `wait` and `exit`.

`wait`:

1.  Acquire process table lock
2.  Scan the process table to look for children
3.  If find current process has child but non exited, call `sleep` to wait one of the child to exit
4.  Scan again

`exit`:

1.  Acquire process table lock
2.  Wake up any process sleeping on the wait channelequal to the current process's parent `proc`
3.  If there is such process, it will be the parent in `wait`. 
4.  Before exit reschedules, reparents all of the children, passing them to `initproc`
5.  Finally, call `sched` to relinquish CPU

Parent free childs kstack and pgdir.

`kill`:

1.  Set the victim: `victim->killed`. If victim is sleeping, wake it up.
2.  Victim will enter or leave kernel, code in `trap` will call `exit` if it's killed.
3.  If victim is in user space, it will trap into kernel by syscall or timer.
4.  If victim is sleeping, `wakeup` from sleep, can be dangerous because the waiting condition is not true.

## Real World

`sleep` & `wakeup` synchronization challenge: the _lost wakeups_ problem. Linux's `sleep` uses an explicit process queue instead of a wait channel, the queue has its own internal lock.

Scanning the entire process list in `wakeup` is inefficient. Use data structures that holds the list of processes sleeping on that structure. In this, `sleep --> wait`, `wakeup --> signal`. The sleep condition is protected by some lock.

Semaphores: avoid the _lost wakeup_ problem.

Terminating & cleaning up processes is very complex. 

# uCore OS Gitbook

[git-book](https://objectkuan.gitbooks.io/ucore-docs/content/)

## Protected Mode and Segmentation

In protected mode, all 32 address lines of 80386 are effective.

2 segment tables: GDT (Global Descriptor Table) and LDT (Local Descriptor Table), each can have 2^13 descriptors, at most 2^13 + 2^13 segments.

Only use segment in protected mode. Seach segment is defiend by starting address and max bytes. 

Logical address (Process & CPU) --> physical address (RAM)

Logical address = {segment selector, segment offset}. Translation:

```C
uint64_t get_physical_addr(logical_address_t logical_addr)
{
    // logical address is virtual address
    if (CPU.protected == true)
    {
        linear_addr = 
            CPU.GDT[logical_addr.segment_selector].base_address +
            logical_addr.segment_offset;

        if (CPU.paging == false)
        {
            // linear address is physical address
            return linear_addr;
        }
        else
        {
            // linear address paging
            return pagewalk(linear_addr);
        }
    }
}
```

-   Segment descriptor entry: to describe a segment with: base addrss, size, attribute(e.g. present, writable, etc.)
-   Global descriptor table: GDT, 2^13 descriptor entries. Saved in `GDTR` register.
-   Segment selector: index bits (13 bits to select in GDT or LDT), indicator bit (use GDT or LDT), **Requested Privilege Level, RPL**.

data segment register (ds) selector: 16-bit DT index + 1-bit table index + PRL

code segment register (cs) selector: 16-bit DT index + 1-bit table index + CRL

// `rip` stores the code address, `cs` describes the current code segment, CRL is the current privilege level of this instruction, indicating the current ring, user mode, kernel mode.

CPU protects memory in 2 checkpoints: when a segment selector is loaded, when access a frame through linear address.

When a data segment selector is loaded, check CPL, DPL, RPL to see if triggers protection error.

When access physical frame, check page table to see if triggers page fault.

[Combining Segment and Page Translation](https://pdos.csail.mit.edu/6.828/2018/readings/i386/s05_03.htm)

5.3.1 "Flat" Architecture

When the 80386 is used to execute software designed for architectures that don't have segments, it may be expedient to effectively "turn off" the segmentation features of the 80386. The 80386 does not have a mode that disables segmentation, but the same effect can be achieved by initially loading the segment registers with selectors for descriptors that encompass the entire 32-bit linear address space. Once loaded, the segment registers don't need to be changed. The 32-bit offsets used by 80386 instructions are adequate to address the entire linear-address space.

5.3.6 Page-Table per Segment

An approach to space management that provides even further simplification of space-management software is to maintain a one-to-one correspondence between segment descriptors and page-directory entries, as Figure 5-13 illustrates. Each descriptor has a base address in which the low-order 22 bits are zero; in other words, the base address is mapped by the first entry of a page table. A segment may have any limit from 1 to 4 megabytes. Depending on the limit, the segment is contained in from 1 to 1K page frames. A task is thus limited to 1K segments (a sufficient number for many applications), each containing up to 4 Mbytes. The descriptor, the corresponding page-directory entry, and the corresponding page table can be allocated and deallocated simultaneously.

>   So actually logical address (virtual address) and linear address are almost the same concept. No need to distinguish them. But we need to know about the segment registers, etc.
>   Now the segments are all flat in this way: cs start 0, length = whole virtual address, ds start 0, length = whole virtual address, ... So all segments are overlapped, each overing the all virtual adddress. In this way, the logical (virtual) address is just linear address.

## Interrupt and Exception

RTOS polling to ask devices. Unix, interrupt.

-   Asynchronous interrupt (interrupt)
    -   timer
    -   I/O
-   Synchronous interrupt (exception)
    -   system call
    -   illegal access

CPU gets interrupt through 8259A, halt, jump to interrupt handler through Interrupt Descriptor Table, IDT. IDT is anywhere in DRAM. IDTR, IDT register to search the starting address of IDT. ISA support:

-   lidt, load IDTR
-   sidt, store IDTR

48-bit IDTR: base address + limit

Protected Mode: <= 256 Interrupt/Exception vectors. Some are reserved by hardware. Some are registered by OS.

`IDT[index] = IDT gate descriptor entry`. 2 kinds of gate:

1.  Interrupt Gate: CPU clear IF bit (interrupt flag) in case interrupted when handling interrupt. Used by interrupt
2.  Trap Gate: interrupt when handling trap. Used by syscall.

How to interrupt

1.  CPU checks 8259A after each instruction, read interrupt number
2.  Goto IDT through IDTR, get gate through interrupt number
3.  Get segment selector and segment descriptor from gate. PC jump to this interrupt handler's entry point
4.  Check if privilege changes through segment descriptor.
    - If user mode traps to kernel mode, get TSS from TR register (task register)
    - Get kstack address from TSS, including `ss` and `esp`
    - Switch to use kstack, push user-level `ss` and `esp` to kstack
5.  Push the registers to trap frame in kstack
6.  Load `cs` and `eip`, start handling.

`iret`, the reverted procedure to go back from interrupt.

_From Intel book. This function is provided by ISA hardware. So the hardware said that there will be a stack switch when interrupts_

If the handler procedure is going to be executed at a numerically lower privilege level, a stack switch occurs. When the stack switch occurs:

a.  The segment selector and stack pointer for the stack to be used by the handler are obtained from the TSS for the currently executing task. On this new stack, the processor pushes the stack segment selector and stack pointer of the interrupted procedure.
b.  The processor then saves the current state of the EFLAGS, CS, and EIP registers on the new stack (see Figure 6-4).
c.  If an exception causes an error code to be saved, it is pushed on the new stack after the EIP value.

If the handler procedure is going to be executed at the same privilege level as the interrupted procedure:

a.  The processor saves the current state of the EFLAGS, CS, and EIP registers on the current stack (see Figure 6-4).
b.  If an exception causes an error code to be saved, it is pushed on the current stack after the EIP value.

# Debugging Xv6

https://pdos.csail.mit.edu/6.828/2017/tools.html

```
sudo apt-get install gcc
sudo apt-get install gdb
```

for build qemu patch

```
sudo apt-get install libsdl1.2-dev 
sudo apt-get install libtool-bin 
sudo apt-get install libglib2.0-dev 
sudo apt-get install libz-dev 
sudo apt-get install libpixman-1-dev 
sudo apt-get install libfdt-dev
```

build qemu patch i386 only

```
git clone http://web.mit.edu/ccutler/www/qemu.git -b 6.828-2.3.0
cd qemu
./configure --disable-kvm --target-list="i386-softmmu x86_64-softmmu"
make
sudo make install
```

build xv6
the official x86 source (but github is way too slow for domestic users)
https://github.com/mit-pdos/xv6-public.git

```
git clone https://gitee.com/yangminz/xv6-public.git
cd xv6-public
make qemu-gdb
```

GDB session. Before start, add path to it.


## First User Process

_To this stage, the kernel page table is set up. CPU is running kernel thread_

```
b userinit
```

This function setup the first user process. The pointer is a global variable:

```
static struct proc *initproc;
```

### Allocate `proc` PDB for the First User Process

`proc` is from the pre-allocated process pool: `ptable.proc`. This is global variable on `.kdata`.

**The main work here is allocating the one-page kernel stack. Set the context & trapframe on kstack for switch to scheduler.**

Step into `kalloc`. Allocate one physical frame for kstack of `userinit`:

```
#0  kalloc () at kalloc.c:87
#1  0x8010351b in allocproc () at proc.c:95
#2  0x801036dc in userinit () at proc.c:126
#3  0x80102f0c in main () at main.c:36
```

Allocate one free frame from `struct run *:kmem.freelist`. Return the head node as pointer to the free list. 

```
(gdb) p kmem
$3 = {lock = {locked = 0, name = 0x80106e4c "kmem", cpu = 0x0, pcs = {0, 0, 0, 0, 0, 0, 0, 0, 0, 0}}, use_lock = 1,
  freelist = 0x8dfff000}
(gdb) p kmem.freelist
$4 = (struct run *) 0x8dfff000
(gdb) p kmem.freelist->next
$5 = (struct run *) 0x8dffe000
(gdb) p kmem.freelist->next->next
$6 = (struct run *) 0x8dffd000
```

`0x8dfff000 -> 0x8dffe000 -> 0x8dffd000`, delta = `0x1000`, 4096 Bytes, one page.

Stack pointer `sp = p->kstack + KSTACKSIZE = 0x8dfff000 + 0x1000 = 0x8e000000`, one page offset.

```
0x8dffff90:     0x01010101      0x01010101      0x01010101

0x8dffff9c:                                                     0x00000000  \ context sizeof(struct context) = 20
0x8dffffa0:     0x00000000      0x00000000      0x00000000     <0x80103590> / forkret 

0x8dffffb0:    <0x801053d5> trapret: {.text address}  size = 4: 1 .text address

0x8dffffb4:                     0x01010101      0x01010101      0x01010101  \
0x8dffffc0:     0x01010101      0x01010101      0x01010101      0x01010101  |
0x8dffffd0:     0x01010101      0x01010101      0x01010101      0x01010101  + trap frame
0x8dffffe0:     0x01010101      0x01010101      0x01010101      0x01010101  | sizeof(struct trapframe) = 76
0x8dfffff0:     0x01010101      0x01010101      0x01010101      0x01010101  /

0x8e000000:     Cannot access memory at address 0x8e000000
kstacp bottom
```

To now, the process `userinit` is initialized. The main job of `allocproc()` is creating kernel stack, set the context, trapframe. 

### Allocate Page Table for the First User Process

Now allocate page table for the process `setupkvm()`. This create one new frame for level-1 page directory, but the following level-2 page table is the same as kernel page tables (both `kvm`).

Go through all pre-assigned kernel mappings: `kmap[0 : -1]`

```
static struct kmap {
  void *virt;
  uint phys_start;
  uint phys_end;
  int perm;
} kmap[] =
{
  {virt = 0x80000000,                     phys_start = 0,           phys_end = 0x100000,  perm = PTE_W},  // I/O space
  {virt = 0x80100000 <multiboot_header>,  phys_start = 0x100000,    phys_end = 0x108000,  perm = 0},      // kern text+rodata
  {virt = 0x80108000 <ctlmap>,            phys_start = 0x108000,    phys_end = 0xe000000, perm = PTE_W},  // kern data+memory
  {virt = 0xfe000000,                     phys_start = 0xfe000000,  phys_end = 0,         perm = PTE_W}   // more devices
}
```

Call `mappages` to map the physical frames to virtual address. Create the page table mappings for the mappings above.

_To this stage, the page table of `initproc` is the same as kernel._

`vm.c: inituvm` allocate one new free frame for user space to hold the code & data of `initproc`. The program data should be in `initcode.S`.

Remap the user page tabe (original same as kernel) from `0:4096` --> free frame. Load user .text & .data (< one page size) into this free frame.

### Set up the trapframe

Then set trapframe of `userinit`:

```
p = 
{sz = 4096, pgdir = 0x8dffe000, kstack = 0x8dfff000 "", state = RUNNABLE, pid = 1, parent = 0x0,
  tf = 0x8dffffb4, context = 0x8dffff9c, chan = 0x0, killed = 0, ofile = {0x0 <repeats 16 times>},
  cwd = 0x80110a14 <icache+52>, name = "initcode\000\000\000\000\000\000\000"}
```

This trapframe becomes:

```
0x8dffffb4:     0x00000000
                0x00000000
                0x00000000
                0x00000000
0x8dffffc4:     0x00000000      
                0x00000000      
                0x00000000      
                0x00000000
0x8dffffd4:     0x00000000
                0x00000000
                0x00000023  es      
                0x00000023  ds
0x8dffffe4:     0x00000000
                0x00000000
                0x00000000  eip // beginning of initcode.S
                0x0000001b  cs
0x8dfffff4:     0x00000200  eflags
                0x00001000  esp // page size
                0x00000023  ss
```

### `swtchuvm`

From `main()` get into `scheduler()`. Process name: `initcode`, process info:

```
{
    sz = 4096, 
    pgdir = 0x8dffe000, 
    kstack = 0x8dfff000, 
    state = RUNNABLE, 
    pid = 1, 
    parent = 0x0,
    tf = 0x8dffffb4, 
    context = 0x8dffff9c, 
    chan = 0x0, 
    killed = 0, 
    ofile = {0x0 <repeats 16 times>},
    cwd = 0x80110a14 <icache+52>, 
    name = "initcode"
}
```

Now we check the register info of hardware. Go to qemu console in the box, `CTRL + A C` to switch to qemu console. `info registers` to get all register values:

```
EAX=0000000a EBX=80112d54 ECX=80112d2c EDX=00007bf8
ESI=80112780 EDI=80112784 EBP=8010b578 ESP=8010b550
EIP=801039af EFL=00000046 [---Z-P-] CPL=0 II=0 A20=1 SMM=0 HLT=0
ES =0010 00000000 ffffffff 00cf9300 DPL=0 DS   [-WA]
CS =0008 00000000 ffffffff 00cf9a00 DPL=0 CS32 [-R-]
SS =0010 00000000 ffffffff 00cf9300 DPL=0 DS   [-WA]
DS =0010 00000000 ffffffff 00cf9300 DPL=0 DS   [-WA]
FS =0000 00000000 00000000 00000000
GS =0000 00000000 00000000 00000000
LDT=0000 00000000 0000ffff 00008200 DPL=0 LDT
TR =0000 00000000 0000ffff 00008b00 DPL=0 TSS32-busy
GDT=     801127f0 0000002f
IDT=     80114ca0 000007ff
CR0=80010011 CR2=00000000 CR3=003ff000 CR4=00000010
DR0=00000000 DR1=00000000 DR2=00000000 DR3=00000000
DR6=ffff0ff0 DR7=00000400
EFER=0000000000000000
```

Now page table is `kpgdir`, the kernel page table of `scheduler`. And we check the kstack context:

```
0x8dffff9c:     0x00000000      0x00000000      0x00000000      0x00000000
```

Now we call `switchuvm`. After this call, let's check `cr3` register from qemu:

```
EAX=80112780 EBX=80112dd0 ECX=00000002 EDX=00000001
ESI=80112780 EDI=80112784 EBP=8010b578 ESP=8010b550
EIP=801039ba EFL=00000002 [-------] CPL=0 II=0 A20=1 SMM=0 HLT=0
EAX=80112780 EBX=80112dd0 ECX=00000002 EDX=00000001
ESI=80112780 EDI=80112784 EBP=8010b578 ESP=8010b550
EIP=801039ba EFL=00000002 [-------] CPL=0 II=0 A20=1 SMM=0 HLT=0
ES =0010 00000000 ffffffff 00cf9300 DPL=0 DS   [-WA]
CS =0008 00000000 ffffffff 00cf9a00 DPL=0 CS32 [-R-]
SS =0010 00000000 ffffffff 00cf9300 DPL=0 DS   [-WA]
DS =0010 00000000 ffffffff 00cf9300 DPL=0 DS   [-WA]
FS =0000 00000000 00000000 00000000
GS =0000 00000000 00000000 00000000
LDT=0000 00000000 0000ffff 00008200 DPL=0 LDT
TR =0028 80112788 00000067 00408900 DPL=0 TSS32-avl
GDT=     801127f0 0000002f
IDT=     80114ca0 000007ff
CR0=80010011 CR2=00000000 CR3=0dffe000 CR4=00000010
DR0=00000000 DR1=00000000 DR2=00000000 DR3=00000000
DR6=ffff0ff0 DR7=00000400
```

Now we see the `cr3` has been switched to `initcode->pgdir`. The stack pointer is still the user stack pointer. How do we know that? Note that `esp` is not changed. And we check `cs` stack segment, the privilege level is `DPL=0`, so ring 3 and the user mode. 

(One thing to note is that `initcode->pgdir` is virtual address!!! But it use kernel's mapping)

Then kernel switches to scheduler thread. Note that we MUST use assembly to exactly control this switch job to store & restore the registers. Before the switching, we check registers and kstack for each:

```
initcode->kstack
(gdb) p/x *p->context
$19 = {edi = 0x0, esi = 0x0, ebx = 0x0, ebp = 0x0, eip = 0x80103590}

scheduler->kstack
NULL

esp            0x8010b550
```

Now do context switch:

```
initcode->kstack
(gdb) p/x *p->context
$23 = {edi = 0x8010b5f4, esi = 0x200, ebx = 0x80112d70, ebp = 0x8dfffebc, eip = 0x80103a5c}

scheduler
{edi = 0x80112784, esi = 0x80112780, ebx = 0x80112dd0, ebp = 0x8010b578, eip = 0x801039d0}

esp            0x8010b550
```

`esp` is still the kernel stack? 

`switchkvm` switches `cr3` to scheduler's page table `kpgdir = (pde_t *) 0x803ff000`.

_To this point, we can check the kernel page table and user page table relationship_





