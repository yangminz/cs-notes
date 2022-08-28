3rd Edition By Randal E. Bryant and David R. O'Hallaron

# Chapter 3. Machine-Level Representation of Program

## 3.8 Array Allocation and Access

```c
int A1[2][5];

int *A2[5][5];
int *(A3[2][5]);
/*
+---+---+---+---+---+
| * | * | * | * | * |
+---+---+---+---+---+
| * | * | * | * | * |
+---+---+---+---+---+
*/

int (*A4)[3][5]
/*
A4 ->
+---+---+---+---+---+
|   |   |   |   |   |
+---+---+---+---+---+
|   |   |   |   |   |
+---+---+---+---+---+
*/

int (*A5[2])[5];
/*
+---+---+  +---+---+---+---+---+  +---+---+---+---+---+
| * | * |  |   |   |   |   |   |  |   |   |   |   |   |
+---+---+  +---+---+---+---+---+  +---+---+---+---+---+
*/
```

# Chapter 7. Linking

## 7.9 Loading Executable Object Files

**Loader** can be invoked by calling `execve`. Loader copies code & data in EOF from disk into memory. Then set PC to first instruction (entry point). This process of copying the program into memory and then running it is known as loading.

```
         +-------------------------+
         |                         |
         |      Kernel Memory      | Memory invisible to user code
         |                         |
     +---------------------------------+
(2^48)-1 |       User stack        |
         |  (created at run time)  |
         +-------------------------+ <---+ %esp (stack pointer)
         |                         |
         |                         |
         +-------------------------+
         |   Memory-mapped region  |
         |   for shared libraries  |
         +-------------------------+
         |                         |
         |                         |
         +-------------------------+ <---+ brk
         |      Run-time heap      |
         |   (created by malloc)   |
         +-------------------------+
         |   Read/Write segment    | ---+
         |      (.data,.bss)       |    |
         +-------------------------+    | Loaded from the
         |  Read-only code segment |    | executable file
         |  (.init,.text,.rodata)  | ---+
0x400000 +-------------------------+
         |                         |
         |                         |
         |                         |
       0 +-------------------------+
```

# Chapter 8. Exception Control Flow

## 8.1 Exceptions

### 8.1.2 Classes of Exceptions

| Class        | Cause                         | (A)Sync | Return behavior              |
|--------------|-------------------------------|---------|------------------------------|
| Interrupt    | Signal from I/O device        | Async   | Always return to next inst   |
| Trap/Syscall | Intentional exception         | Sync    | Always return to next inst   |
| Fault        | Potentially recoverable error | Sync    | Might return to current inst |
| Abort        | Nonrecoverable error          | Sync    | Never returns                |             

#### Interrupts

Async as a result of signals from I/O devices external to CPU: Network adapters, disk controllers, timer chips. Especially timer interrupt to force context switch.

After/Before the current instruction execution, CPU check _interrupt pin_ and see if it's high (means there is a interrupt signal). If high, CPU reads interrupt number from system bus and call the handler. When returns, resume the execution of next instruction. But CPU's control may give to other processes due to context switch.

#### Traps and System Calls

As the result of executing system call instructions, `int` for interrupt or `syscall` for system call. These are _intentional exceptions_. Identical to a regular function call, but in kernel mode.

#### Faults

As a result of unexpected error when executing instructions. E.g. Access Violation, Page Fault, Divide by Zero, etc. Process may register handlers to catch these faults. If such error can be handled, then re-execute the instruction. Else, OS goes to abort routine to terminate the process.

-   Divide error: /0. Linux can report the error
-   General protection fault: E.g. reference an undfined address or write to an RO memory. _Segmentation Faults_. Use this with Page Fault to implement `fork`.
-   Page fault: Page table entry indicates that the virtual address is not in DRAM.

#### Abort

Uncatched or unhandled or unrecoverable fatal errors. Process would crash, but OS is still safe. 

E.g. Machine check: fatal hardware error detected when executing a instruction. Process is terminated immediately.

## 8.2 Processes

One of the most profound and successful ideas in CS. Illusion: the program is the only one currently running in the system, having exclusive use of processor and memory. 

Classic definition: *An instance of program in execution*. Program runs in the context of some process, including code, data, etc.

Key abstractions:

1.  An independent logical control flow that provides the illusion that program has exclusive use of processor.
2.  A private address space that provides the illusion that program has exclusive use of memory system.

### 8.2.1 Logical Control Flow

The sequence of Program Counter -- logical control flow. Multi-task system: processes share PC by time: `A -> B -> C -> B -> C -> A -> ...`. Take turns using the processor. 

### 8.2.2 Concurrent Flows

Logical flow X & Y are **concurrent** i.f.f. `Y.starts < X.starts < Y.ends` or `X.starts < Y.starts < X.ends`. **Time slice**, each exclusive use.  *Parallel flows*: 2 flows running concurrently on different processor cores.

### 8.2.3 Private Address Space

Byte in private address space cannot be R/W by any other process in general.

### 8.2.4 User and Kernel Modes

Airtight process abstraction, provide a mechanisim restricting instruction execution.

A **Mode Bit** in some control register describing the current privilege. When `ModeBit == 1`, run in kernel/supervisor mode. Else, run in user mode. Some instructions are privileged, user mode process cannot execute privileged instructions, e.g., halt processor, change mode bit, initiate I/O operation. *User programs must access kernel code & data indirectly via system call interface.*

Linux use `/proc` filesystem allows user mode processess to access contents of kernel data structures. `/proc` exports the contents of many kernel data structures. 

### 8.2.5 Context Switches

Higher-level form of exceptional control flow: **Context Switch** to implement multitasking. Contenxt is the states that the kernel needs to restart a preempted process, e.g., general-purposed registers, floating-point registers, program coutners, user stack, status registers, kernel stack, page table, process table, file table, etc.

Kernel can preempt the current process, and restart a previously preempted process -- **Scheduling**. When kernel scheduled a new process to run, then do context switch:

1.  Saves the context of current process
2.  Restores the saved context of previous process
3.  Pass control to the newly restored process.

Context switch can happen 

-   System call, e.g., `read`, `sleep`.
-   Interrupt, e.g., periodic timer interrupts (typically 1ms or 10ms), page fault.

| Mode           | Process A (User) | Kernel                                | Process B (User)        |
|----------------|------------------|---------------------------------------|-------------------------|
| User           | Invoke `read`    |                                       |                         |
| Kernel         |                  | System call: store A user states      |                         |
|                |                  | Kernel `read` invoke disk transaction |                         |
| Context switch |                  | Store A kernel states                 |                         |
|                |                  | Schedule to B                         |                         |
| Context switch |                  | Restore B kernel states               |                         |
|                |                  | Restore B user states                 |                         |
| User           |                  |                                       | Continue B instructions |

## 8.3 System Call Error Handling

When system-level functions fail, usually return `-1` and set the global integer variable `errno`. 

## 8.4 Process Control

### 8.4.1 Obtaining Process IDs

```c
#include <sys/types.h>
#include <unistd.h>
pid_t getpid(void);
pid_t getppid(void);
```

### 8.4.2 Creating and Terminating Processes

Think of a process in one of three states:

-   **Running**: executing on CPU or waiting to be executed and will eventually be scheduled by kernel.
-   **Stopped**: execution is suspended and will not be scheduled as a result of receiving a `SIGSTOP`, `SIGTSTP`, `SIGTTOU` signal. It remains stopped until receives a ` SIGCONT` signal.
-   **Terminated**: stopped permanently due to:
    1.  Receving a signal whose default action is to terminate the process
    2.  Returning from main routine
    3.  Calling `exit` function.

```c
#include <stdlib.h>
void exit(int status);
```

`fork` to create child: identical & separate copy of parent's user-level virtual address space. 

```c
#include <sys/types.h>
#include <unistd.h>
pid_t fork(void);
```

-   Call once, return twice: `0` in child; `> 0` in parent.
-   Concurrent execution
-   Duplicate but separate address space
-   Shared files

So the execution of parents & childs are **partially ordered** and a tree. The execution would be any **topological sort**.

```c
int main()
{
                            // 0
    fork();                 // 1
    fork();                 // 2
    printf("hello\n");      // 3
    eixt(0);                // 4
}
```

The process graph:

```
                +-------+-------+
                |       3       4
                |
        +-------+-------+-------+
        |       2       3       4
        |
        |       +-------+-------+
        |       |       3       4
        |       |
+-------+-------+-------+-------+
0       1       2       3       4
```

### 8.4.3 Reaping Child Processes

When process terminates, kernel does not immediately remove it. Instead, PCB is kept in a terminated state until it's **Reaped** by its parent. Terminated process not yet been reaped is **Zombie**. Kernel maintains a minimal set of information: Pid, termination status, resource usage information. Why do this? Parent would like to know why the child terminated later. 

When parent terminates, `init` (PID = `1`, created by kernel during start-up, ancestor of every process) become the adopted parent of orphaned children. Long-running process should always reap zombie childs. 

`waitpid` to wait for children to terminate or stop. Very complicated. By default, `waitpid` suspends execution of calling process until a child process in parent's `waitset` terminates. If any process in wait set already terminated, `waitpid` returns immediately. 

Actually, `waitpid` waits for children's status change. A state change is:

-   Child terminated. In this case, `waitpid` will release the resources associated with the child.
-   Child stopped by a signal;
-   CHild resumed by a signal;

```c
#include <sys/types.h>
#include <sys/wait.h>
pid_t waitpid(pid_t pid, int *statusp, int options);
```
**Determining the Members of the Wait Set**

-   `pid > 0`, then wait set is the singleton child process whose process ID is `pid`.
-   `pid = -1`, then wait set is all of parent's child processes

Also supports Unix process groups as wait set.

**Modifying the Default Behavior**

Set the `options` flag:

-   `WNOHANG`: No hang. Return immediately if none has terminated yet. Useful when you want to continue doing work while waiting for child to terminate.
-   `WUNTRACED`: Untraced. Suspend until a process in waitset either terminated or stopped. Useful when want to check both terminated AND stopped children.
-   `WCONTINUED`: Continued. Suspend until (1) a *running* process in waitset terminated; (2) a *stopped* process in waitset resumed by the receipt of SIGCONT.

`WNOHANG | WUNTRACED`: Return immediately, with a return value of 0, if none of the children in the wait set has stopped or terminated, or with a return value equal to the PID of one of the stopped or terminated children.

**Checking the Exit Status of a Reaped Child**

If `statusp != NULL` (status of child pointer), then encode the info about the child that caused the return in `statusp -> status`. Then use marco to check `status`:

Exit status:

-   `WIFEXITED(status)`: true if the child terminated normally, via a call to `exit` or return.
-   `WEXITSTATUS(status)`: Get the exit status of a normally terminated child. Effective only when `WIFEXITED(status) == 1`.

Signal related:

-   `WIFSIGNALED(status)`: If child terminated due to a signal that was not caught.
-   `WTERMSIG(status)`: The number of signal caused the child to terminate. Effective only when `WIFSIGNALED(status) == 1`.

Stopped child:

-   `WIFSTOPPED(status)`: true if the child is currently stopped.
-   `WSTOPSIG(status)`: The number of signal caused the child to stop. Effective only when `WIFSTOPPED(status) == 1`.

Continue:

-   `WIFCONTINUED(status)`: true if the child was restarted by receipt of a `SIGCONT` signal.

**Error Conditions**

If caller has no children, then returns `-1`, `errno = ECHILD`.

If `waitpid` was interrupted by a signal, then returns `-1`, `errno = EINTR`.

Example

```c
#define N (10)

int main()
{
    // parent creates N children
    for (int i = 0; i < N; i ++)
    {
        pid_t child_pid = fork();
        if (child_pid == 0)
        {
            // terminate the childs to zombies
            exit(100 + i);
        }

        // parent reaps children in any order
        while (1)
        {
            int status;
            pid_t child_pid = waitpid(-1, &status, 0);
            if (child_pid > 0)
            {
                // child_pid reaped by parent
                if (WIFEXITED(status))
                {
                    printf("child %d terminated normally with exit status=%d\n",
                        child_pid, WEXITSTATUS(status));
                }
                else
                {
                    printf("child %d terminated abnormally\n", child_pid);
                }
            }
            else
            {
                break;
            }
        }

        if (errno != ECHILD)
        {
            unix_error("waitpid error");
        }

        exit(0);
    }
}
```

### 8.4.4 Putting Processes to Sleep

```c
#include <unistd.h>
unsigned int sleep(unsigned int secs);
```

Returns `0` if time up. Else returns the seconds still left to sleep (may interrupted by a signal).

```c
#include <unistd.h>
int pause(void);
```

`pause` puts the caller to sleep until a signal is received by the process.

### Loading and Running Programs

```c
#include <unistd.h>
int execve(const char *filename, const char *argv[], const char *envp[]);
```

`execve` loads and runs a new program in the context of current process. Returns to the caller only if error, e.g., `filename` not found. So it's *called once and never returns*.

When parsed `filename`, calls the start-up code to set up the stack and passes control to main routine of the new program. 

## 8.5 Signals

Higher-level software form of ECF: Linux signal, allowing processes and the kernel to interrupt other processes.

| Number | Name        | Default action          | Corresponding event                       |
|--------|-------------|-------------------------|-------------------------------------------|
| 2      | `SIGINT`    | Terminate               | Interrupt from keyboard                   |
| 3      | `SIGQUIT`   | Terminate               | Quit from keyboard                        |
| 5      | `SIGTRAP`   | Terminate and dump core | Trace trap                                |
| 6      | `SIGABRT`   | Terminate and dump core | Abort signal from abort function          |
| 8      | `SIGFPE`    | Terminate and dump core | Floating-point exception                  |
| 9      | `SIGKILL`   | Terminate               | Kill program                              |
| 11     | `SIGSEGV`   | Terminate and dump core | Invalid memory reference (seg fault)      |
| 17     | `SIGCHLD`   | Ignore                  | A child process has stopped or terminated |
| 18     | `SIGCONT`   | Ignore                  | Continue process if stopped               |
| 19     | `SIGSTOP`   | Stop until next SIGCONT | Stop signal not from terminal             |
| 20     | `SIGTSTP`   | Stop until next SIGCONT | Stop signal from terminal                 |
| 21     | `SIGTTIN`   | Stop until next SIGCONT | Background process read from terminal     |
| 22     | `SIGTTOU`   | Stop until next SIGCONT | Background process wrote to terminal      |

Each signal is some kind of system event. Expose the low level exceptions to user processes.

### 8.5.1 Signal Terminology

**Sending a signal**: kernel sends a signal to destination process by updating the context of destination. 2 reasons for delivery: (1) detect a system event; (2) another process invokes `kill` to ask the kernel to send signal. can be send to itself.

**Receiving a signal**: destination process forced by kernel to react to the signal delivery: ignore, terminate, catch.

A signal updated in destination but not reacted is called **pending**. There is *at most one* pending signal *of a particular type*. i.e., `SIGCHLD` can be only one. Subsequent of pending signals are discarded.

Process can block the receipt of certain signals. The signal will be delivered but no action until destination unblock it.

A pending signal is received at most once.

### 8.5.2 Sending Signals

One can send signals to process groups via:

1.  use `/bin/kill` program
2.  from keyboard
3.  use `kill` system function (to themselves)
4.  use `alarm` system function (`SIGALRM`)

```c
#include <sys/types.h>
#include <signal.h>
int kill(pid_t pid, int sig);
```

### 8.5.3 Receiving Signals

When process `iret` from kernel to user, it check the unblocked pending signals. If there is unblocked pending signals, call default actions/user-defined handler.

Signal handlers can be interrupted by other handlers.

### 8.5.4 Blocking and Unblocking Signals

-   *Implicit blocking mechanism*: By default, blocks any pending signals of the type currently being processed by a handler. E.g., when process `SIGCHLD`, another `SIGCHLD` delivered, it will be pending but not received until the handler returns.
-   *Explicit blocking mechanism*: use `sigprocmask` and its helpers. It will change the set of currently blocked bit vector.

```c
#include <signal.h>
int sigprocmask(int how, const sigset_t *set, sigset_t *oldset);
int sigemptyset(sigset_t *set);
int sigfillset(sigset_t *set);
int sigaddset(sigset_t *set, int signum);
int sigdelset(sigset_t *set, int signum);
int sigismember(const sigset_t *set, int signum);
```

Temporarily blocking a signal from being received:

```c
sigset_t mask, prev_mask;
sigemptyset(&mask);
sigaddset(&mask, SIGINT);

/* Block SIGINT and save previous blocked set */
sigprocmask(SIG_BLOCK, &mask, &prev_mask);

// Code region that will not be interrupted by SIGINT

/* Restore previous blocked set, unblocking SIGINT */
sigprocmask(SIG_SETMASK, &prev_mask, NULL);
```

### 8.5.6 Synchronizing Flows to Avoid Nasty Concurrency Bugs

There might be race condition due to interleaving of parent's main routine and signal-handling when `fork` and signal handler for `SIGCHLD`. Can block `SIGCHLD` before `fork` and unblock it afterwards to eliminate race.

### 8.5.7 Explicitly Waiting for Signals

Spin loop:

```c
while (!pid)
    /*  pause() may be interrupted by receiving one or more SIGINT. 
        if SIGCHLD is received after `while` and before `pause`,
        `pause` will sleep forever -- RACE CONDITION!!!
     */
    pause();

while (!pid)
    /*  No race but too slow
        If signal received after `while` and before `sleep`,
        the program must wait a long time before it can check the loop termination condition 
     */
    sleep(1);
```

Use `sigsuspend` instead:

```c
#include <signal.h>
int sigsuspend(const sigset_t *mask);
```

It's a atomic/uninterruptible block and pause and unblock to eliminate race:

```c
sigprocmask(SIG_BLOCK, &mask, &prev);
pause();
sigprocmask(SIG_SETMASK, &prev, NULL);
```

`pause` will cause the caller to sleep until a signal is delivered that either terminates the process or causes the invocation of a signal-catching function.

`sleep` will delay for a specific amount of time.

`sigsuspend` will temporarily replaces the signal mask of the caller by argument. And then suspends the process until delivery of a signal whose action is to invoke a signal handler or termination. If terminates, no return. If signal is caught, `sigsuspend` returns after the handler returns and then restore the mask. CANNOT BLOCK `SIGKILL` or `SIGSTOP`.

Normally, it is used together with `sigprocmask` to prevent delivery of a signal during execution of a critical code section. The caller first blocks the signal with `sigprocmask`. When critical code completed, caller then waits for the signal by `sigsuspend` with the signal mask returned by `sigprocmask`.

# Chapter 9. Virtual Memory

## 9.7 Case Study: The Intel Core i7/Linux Memory System

### 9.7.2 Linux Virtual Memory System

#### Linux Virtual Memory Areas

virtual memory: collection of areas or segments. each existing virtual page is contained in some area. cannot reference pages outside the all areas. Allow virutal address space to have gaps. kernel do not track non-existing pages.

`task_struct->mm_struct->pgd`: when process scheduled to run, stores `pgd` to CR3 control register.

`task_struct->mm_struct->mmap`: area structs `vm_area_structs` list: describe the virtual adderss space area.

#### Linux Page Fault Exception Handling

MMU triggers a page fault when translate virtual address `vaddr`. control transfered to kernel's page fault handler:

1.  Is `vaddr` legal? Check `vm_area_structs` list. If illegal, issue **Segmentation Fault**, because process is accessing a nonexistent page. It's fatal, program exits. use red-black to do fast search, because `mmap` may create many many areas. -- VMA is only used in this case.
2.  Is `mem[vaddr]` readable or writeable? Check if the process is privileged to perform the operation. E.g. write to `.text` segment ==> **Protection Fault**. program exits. *remember `fork`, `vm_area_struct` r/w but `page_table` ro, then demand paging.* -- Check page table entry/page descriptor.
3.  Finally, without segmentation fault and protection fault, a normal page fault. If clean, discardable. If dirty, backed by file or swap space? Be careful here.

## 9.8 Memory Mapping

map virtual memory with object on disk:

1.  Regular file in File System: file divided into pages. Demand paging: virtual pages on disk is swapped into physical memory until they are referenced by page fault. -- No segmentation fault because memory mapped in page table. Zero padding
2.  Anonymous file: not in FS. e.g. stack & heap. virtual area mapped to zeros. page fault (1st reference) of anonymous file area (e.g. stack when process just created), find a victim (this victim can be file backed or anonymous) from physical pages, swap out if it's dirty (file backed: to FS; anonymous: to swap space), overwrites the victim page (as the new stack page) with zeros, update page table to mark the page as resident (in physical memory, even if there is no FS file backed). **demand-zero page** -- No transaction between disk & physical memory: copy stack bytes from disk to memory, no, just memory writing zeros.

### 9.8.1 Shared Objects Revisited

multiple processes share the same read-only page (shared lib's `.text` segment).

For r/w pages shared, e.g. `.data`, COW.

### 9.8.2 The `fork` Function Revisited

create virtual memory for new process: create exact copy of parent's `mm_struct`, `vm_area_structs`, page tables. flags each **Page** as read-only in page table (these are kernel data structures, so mark the kernel page table).

when child process runs, `vm_area_struct` says read/write, kernel page table says read-only, do copy-on-write.

### 9.8.3 The `execve` Function Revisited

loading:

1.  Delete parent's `vm_area_struct` in **user-space**.
2.  Map private areas. map `.data` with assembly as R/W, map `.text` with assembly as RO, map `bss` and stack as anonymous page (to the **static zero page** (that's read-only in physical memory, do COW when tried to write))
3.  Map shared areas. e.g. GLIBC
4.  Set program counter.

