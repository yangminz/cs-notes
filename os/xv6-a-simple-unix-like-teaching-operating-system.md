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
  p->tf->eflags = FL_IF;
  p->tf->esp = PGSIZE;
  p->tf->eip = 0;  // beginning of initcode.S
```

## Code: Running the First Process

`scheduler` looks for a process `RUNNABLE`, there is only one: `initproc`. `proc` is the per-CPU variable (one CPU can run only one process), set it as `initproc`. Call `switchuvm` to tell the hardware to start using the target process's page table. Also set up the task state segment `SEG_TSS` that instructs the hardware to execute syscalls and interrupts on the process's kernel stack.

`scheduler` then call `swtch` to do context switch to the target process's kernel thread:

1.  Save the current registers. But current context is not a process, it's scheduler. So the hardware registers are stored in `cpu->scheduler`, instead of the kernel thread context. 
2.  `swtch` then loads the saved registers of the target kernel thread (`p->context`) into X86 hardware registers, including `%esp, %eip`
3.  Finally, `ret` pops the target process's `%eip` from stack, finish context switching. Now CPU is running on the kernel stack of process `p`.