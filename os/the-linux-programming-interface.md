1st by Michael Kerrisk

# Chapter 20: Signals: Fundamental Concepts

### 20.14 Waiting for a Signal: `pause()`

`pause` will suspends the execution until interrupted by:

-   a signal handler. `pause()` will be interrutped and return `-1`, set `errno` to `EINTR`.
-   an unhandled signal terminates the process;

# Chapter 21: Signals: Signal Handlers

## 21.1 Designing Signal Handlers

-   How to design a signal handler;
-   Normal return from a signal handler;
-   Handling of signals on an alternate stack;
-   `sigaction()` to get signal invocation details;
-   Interrupt a blocking system call and restarting.

## 21.1 Designing Signal Handlers

Write simple signal handlers to reduce the risk of *race conditions*. Two common designs:

-   Sets a global flag and exits;
-   Do cleanup or terminates or nonlocal goto.

### 21.1.1 Signals Are Not Queued (Revisited)

When execute the handler, the signal type is blocked delivering. If generated again during execution, marked as pending and handle when handler returns.

Signals can *disappear* in this way. So cannot rely on the count. Handlers should deal with multiple occurance. This is especially the case in `SIGCHLD`.

### 21.1.1.2 Reentrant and Async-Signal-Safe Functions

**Reentrant and nonreentrant functions**: First need to distinguish single/multi-threaded programs.

A signal handler may asyncly interrupted at any point in time, so there will be two *independent** execution threads (not concurrent): (1) the main program; (2) signal handler.

**Reentrant**: a function can safely be simultaneously executed by multiple threads in the same process. *Safe* means achieving the expectations regardless of any other threads.

**Nonreentrant**: a function updates global or static data structures. E.g., 2 threads simultaneously update the same global variable. `printf(), scanf()` are nonreentrant because they use static data structures. Program may crash if `printf()` during signal handling. `write()` is safe.

```c
#include <unistd.h>
#include <signal.h>
#include <stdio.h>
static char *s = "Interrupted by yangmin\n";
static void handler(int sig)
{
    // a system call inside a handler
    write(1, s, 23);
}
int main()
{
    struct sigaction sa;
    sigemptyset(&sa.sa_mask);
    sa.sa_flags = 0;
    sa.sa_handler = handler;
    // register the signal action
    if (sigaction(SIGINT, &sa, NULL) == -1)
    {
        printf("Failed to register signal handler for SIGINT\n");
        return 1;
    }
    for (;;);    
}
```

Two choices in signal handling:

1.  Signal handler calls only async-signal-safe functions;
2.  Block delivery of signals while calling unsafe functions or r/w global data structures.

# Chapter 26 Monitoring Child Processes

A parent process needs to know when one of its child changes state:

-   Child terminated. Do `wait()` to release the resources;
-   Child stopped by a signal;
-   Child resumed by a signal.

`wait` and `SIGCHLD` to do the monitoring.

## 26.2 Orphans and Zombies

Different lifetimes of parent & childs. So two questions:

-   Who is the parent of an orphaned child? `init`, the ancestor of all processes, pid 1. So for child process to check its parent's liveness: `if (getppid() == 1) {// parent dead}`
-   Child terminates before parent `wait`. Parent must use `wait` to learn why the child terminated. So turn child into **Zombie**. Zombie has no info unless pid, termination status, resource usage statistics.

Zombie cannot be killed by a signal, even if `SIGKILL` (silver bullet :D). So parent can eventually perform `wait()`.

When parent `wait()` zombie, kernel removes all its resources. If parent terminates without `wait`, zombie become orphan and adopted by `init` and automatically `wait` later.

Important for long-lived parent processes, e.g., network servers & shells. Parent should `wait` in order to remove the terminated childs to prevent zombies. `wait` can be called syncly or asyncly in response to `SIGCHLD`.

## 26.3 The `SIGCHLD` Signal

Parent cannot predict the termination of childs, even if parent sends a `SIGKILL`. That depends on the schedule of CPU. Parent should `wait`:

-   Call `wait` without `WNOHANG`, then the call will block if a child has not already terminated;
-   Periodically perform a nonblocking check (**a poll**) for dead children by calling `waitpid` with `WNOHANG`.

Both are not good. Blocking need to wait for time, nonblocking will wastes CPU time to switch back to user mode. So make a handler for `SIGCHLD`.

### 26.3.1 Establishing a Handler for `SIGCHLD`

When a child process terminates, `SIGCHLD` is sent to parent process. By default, it's ignored. Within the handler, use `wait` to reap the zombie.

Now the problem is how to block the signal for handler.

When calling a signal handler, the catched signal is temporarily blocked. And `SIGCHLD` is not queued. So a second and third child terminate when handling the first child, only one signal would be delivered. So parent needs to `wait()` multiple times until there is no zombie childs during handler, since `wait()` will return if one child is reaped.

Thus loop in handler and call `waitpid` with `WNOHANG` until no more dead child can be reaped:

```c
while (waitpid(-1, NULL, WNOHANG) > 0)
    continue;
```

#### Design issues for `SIGCHLD` handlers

The handler should be established before creating any children. Otherwise need to consider: when the handler is ready, there are already zombies. 

Reentrancy. `waitpid()` will change the value of global variable `errno`, so save it locally before system call and restore it before returning.

Be careful to use `sigprocmask()` to block `SIGCHLD` before any child creation. If do not block `SIGCHLD` in first place, child may terminated before a `sigsuspend()`. Then parent would wait forever for a signal already caught.

### 26.3.2 Delivery of SIGCHLD for Stopped Children

Parent may receive `SIGCHLD` when child is stopped by `SIGSTOP`. Set `SA_NOCLDSTOP` to avoid `SIGCHLD` when child stops.

### 26.3.3 Ignoring Dead Child Processes

In some case, child may be terminated immediately removed from the system instead of being a zombie. Then `wait()` cannot return any info.





