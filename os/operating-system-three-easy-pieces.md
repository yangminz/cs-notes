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

# Chapter 5. Interlude: Process API




# Chapter 26. Concurrency and Threads

# Chapter 28. Locks

# Chapter 29. Locked Data Structures

# Chapter 30. Condition Variables

# Chapter 31. Semaphores

# Chapter 32. Concurrency Bugs

# Chapter 33. Event-Based Concurrency