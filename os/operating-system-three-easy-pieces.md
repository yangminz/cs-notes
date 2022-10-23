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

2 key operating system abstractions: **process, address space**, now add one new: **persistent storage**, can be classic hard disk drive, or modern solid-state storage device.

## 39.1 Files And Directories

File: a linear array of bytes. Historically, internal file name is **inode number**. Each file has one inode number.

File System (FS) responsibility: store data persistently on disk. 

Directory: also have inode number. A list of entries (either file, directory). *Directory Tree/Directory Hierarchy*.

Root directory (in Unix: `/`). 

File name: arbitrary name + file type. Just a convention, no enforcement.

FS: a way to name all files: directory + file name.

## 39.3 Creating Files

`open` system call, passing filename, flag, mode.

Returns **file descriptor**: an integer private per process, used in system to access files. It's (1)an opaque handle to perform file operations; (2)a pointer to an object of type file.

Process maintains the array of file descriptors. 

## 39.4 Reading And Writing Files

`strace` to see how does `cat` open files. `strace` tool can see what programs are up to. Trace system calls the program makes, see arguments & return codes. `strace -f`, see fored children; `strace -t` reports time of call; `strace -e trace=open,close,read,write` to focus.

```shell
strace cat x.txt
```

Each process already has three files open: `stdin`, `stdout`, `stderr`. So fd >= 3.

## 39.5 Reading And Writing, But Not Sequentially

If have index on file content, may read from offset. To do so, use `lseek()` system call:

```c
#include <sys/types.h>
#include <unistd.h>

off_t lseek(int fd, off_t offset, int whence);
```

`lseek()` repositions the file offset of the open file description associated with the file descriptor `fd` to the argument `offset` according to the directive `whence`.

| `whence`   | Description                                                       |
|------------|-------------------------------------------------------------------|
| `SEEK_SET` | The file offset is set to offset bytes.                           |
| `SEEK_CUR` | The file offset is set to its current location plus offset bytes. |
| `SEEK_END` | The file offset is set to the size of the file plus offset bytes. |

```c
#include <sys/types.h>
#include <sys/stat.h>
#include <fcntl.h>
#include <unistd.h>
int main()
{
    // file: x.txt: 1111111122222222
    char buf[8];
    int fd = open("./x.txt", O_RDONLY, S_IRUSR);
    lseek(fd, 8, SEEK_SET);
    read(fd, &buf, 8);
    close(fd);
    // strace -e trace=open,read,write,close ./a
}
```

`buf` content: `22222222`.

`read()/write()` will implicitly update current file offset, `lseek()` will explicitly, in memory (`struct file {...}`), NOT IN DISK!

All opened files are managed by system as **Open File Table**, each entry will have one lock.

Example: open same file twice and read.

```c
#include <sys/types.h>
#include <sys/stat.h>
#include <fcntl.h>
#include <unistd.h>
int main()
{
    // file: x.txt: 1111111122222222
    char buf1[8];
    char buf2[8];
    int fd1 = open("./x.txt", O_RDONLY, S_IRUSR);
    int fd2 = open("./x.txt", O_RDONLY, S_IRUSR);
    read(fd1, &buf1, 8);
    read(fd2, &buf2, 8);
    close(fd1);
    close(fd2);
    // strace -e trace=open,read,write,close ./a
}
```

This will create 2 independent open file table entries, each has one lock and one offset. So `buf1` and `buf2` are all `11111111`.

## 39.6 Shared File Table Entries: `fork()` And `dup()`

Table entry in open file table can be shared: `fork()` and `dup()`.

Child process shares the same open file table entry with parent:

```c
#include <sys/types.h>
#include <sys/stat.h>
#include <fcntl.h>
#include <unistd.h>
#include <sys/wait.h>
int main()
{
    char buf[8];
    int fd = open("./x.txt", O_RDONLY, S_IRUSR);
    pid_t pid = fork();
    if (pid == 0)
    {
        // child
        lseek(fd, 8, SEEK_SET);
    }
    else
    {
        // parent
        int status;
        waitpid(pid, &status, 0);
        read(fd, &buf, 8);
        close(fd);
    }
    // strace -e trace=open,read,write,close ./a
}
```

So `buf` will receive `22222222` from the offset set by child.

**Reference count**: when a open file table entry is shared, `refcnt+=1`. If `refcnt==0`, remove entry.

`dup()` call create new fd refers to the same open file entry as an existing fd. Useful for shell to do I/O redirection.

## 39.7 Writing Immediately With `fsync()`

`write()`: FS will write data to persistent storage at some point in future: not immediately.

FS will buffer write in memory for delay for perf reasons. Write may crash before write to disk and lost data.

But for DBMS (database management system), recovery protocol needs to force write immediately. Use `fsync` to force dirty data to disk.

```c
#include <unistd.h>
int fsync(int fd);
```

> `fsync()`  transfers ("flushes") all modified **in-core** data of (i.e., modified buffer cache **pages** for) the file referred to by the file descriptor `fd` to the disk device (or other permanent storage device) so that all changed information can be retrieved even if the system crashes or is rebooted. This includes **writing through** or **flushing a disk cache if present**.  The call blocks until the device reports that the transfer has completed.

Usage: not only flush the file, but also flush the directory. This is important if newly created.

## 39.8 Renaming Files

`mv` command and `rename()` system call. Usually atomic so if system crash, the file is either old name or new name, no in-between state. Example, add new content in file:

```
int fd = open("x.txt.tmp", O_WRONLY|O_CREATE|O_TRUNC, S_IRUSR|S_IWUSR);
write(fd, buffer, size);
fsync(fd);
close(fd);
rename("x.txt.tmp", "x.txt");
```

`rename` will atomically swaps new file into place & concurrently deleting old file, so we have atomic file update.

## 39.9 Getting Information About Files

`stat()/fstat()` to get file metadata. FS usually keeps metadata in `inode`, persistent data structure kept by FS.

## 39.10 Removing Files

`rm`, actually calls system call `unlink()`. It's related with files & directories.

## 39.11 Making Directories

Directory is considered as FS metadata, so can create, read, delete but cannot write.

`mkdir()` to create directory. On just created, directory is empty with only 2 links: `.` & `..`.

## 39.12 Reading Directories

`ls` calls: `opendir(), readdir(), closedir()` system calls.

## 39.13 Deleting Directories

`rmdir()`, dangerous so directory must be empty.

## 39.14 Hard Links

`link()` system call to create an entry in file system tree. `ln`:

```shell
echo "hello world!" > foo
ln foo bar
cat bar
# hello world!
ls -i foo bar
#23643898043799116 bar
#23643898043799116 foo
```

`link()` creates another name in directory and refers it to the same inode number of the old file. 

When create file, actually do 2 things:

1.  Create a inode to track metadata of the file: file size, location on disk, etc.
2.  Link a human-readable file name to the file, put the link into a directory.

inode also has **reference count** or **link count**. It's only deleted when counted as zero.

-   No hard link to directory in case there were loop in directory tree.
-   No hard link to other disk partitions: inode numbers are unique within one FS.

## 39.15 Symbolic Links

**Symbolic link** or **soft link**. `ln -s`.

Soft link is actually itself a file, of different types. 

```
$ echo "hello world!" > foo
$ ln -s foo bar
$ stat foo bar
  File: foo
  Size: 13              Blocks: 0          IO Block: 4096   regular file
Device: 2h/2d   Inode: 24769797950587651  Links: 1
Access: (0644/-rw-r--r--)  Uid: ( 1000/yangminz)   Gid: ( 1000/yangminz)
Access: 2022-10-18 11:52:48.264809700 +0800
Modify: 2022-10-18 11:52:48.264809700 +0800
Change: 2022-10-18 11:52:48.264809700 +0800
 Birth: -

  File: bar -> foo
  Size: 3               Blocks: 0          IO Block: 4096   symbolic link
Device: 2h/2d   Inode: 6755399441171766  Links: 1
Access: (0777/lrwxrwxrwx)  Uid: ( 1000/yangminz)   Gid: ( 1000/yangminz)
Access: 2022-10-18 11:52:53.154569900 +0800
Modify: 2022-10-18 11:52:53.154569900 +0800
Change: 2022-10-18 11:52:53.154569900 +0800
 Birth: -
```

Unlike hard link reference count, when foo is removed, bar just loses the link.

## 39.17 Making And Mounting A File System

To build up full directory tree from FS:

1.  make FS: `mkfs` to make FS. It will write an empty file system, e.g., `EXT3`, onto the disk partition.
2.  mount FS to make contents accessible: mount the created FS to uniform FS tree viwa `mount` program and `mount()` call. Take the existing directory as **mount point** and paste the new FS onto the directory tree.

E.g., an unmounted `EXT3` FS in device partition `/dev/sda1`: a root directory contains `a, b` sub-directories. Now mount this FS to `/home/users`:

```
mount -t ext3 /dev/sda1 /home/users
```

Then there will be `/home/users/a` and `home/users/b`.

# Chapter 40. File System Implementation

How to build a simple FS; The data structures needed on the disk; What to track; How accessed.

## 40.1 The Way To Think

-   **Data structure of FS**: e.g., arrays of blocks, tree-based structures.
-   **Access methods**: i.e., `open(), read(), write()`, map the function calls onto data structures.

## 40.2 Overall Organization

First, divide disk into blocks, e.g., 4KB. E.g., have a very small disk: 64 blocks.

| Indices | Description                                                                  |
|---------|------------------------------------------------------------------------------|
| 0       | super block, info about FS, e.g., number of inodes & data blocks, FS type    |
| 1-2     | allocation structures to track if inodes/data blocks are freed or allocated. |
| 3-7     | inode table to index the files in data region.                               |
| 8-63    | Data region.                                                                 |

Can use most allocation-tracking methods, e.g., free list, bitmap array for data region and inode table.

## 40.3 File Organization: The Inode

inode: **Index node**. Given index number, should directly calculate the location on disk:

```
                                    | iblock 0  | iblock 1  | iblock 2  | iblock 3  | iblock 4  |
+-----------+-----------+-----------+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
|           |           |           | 0| 1| 2| 3|16|17|18|19|32|33|34|35|48|49|50|51|64|65|66|67|
|           |           |           |--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--|
|           |           |           | 4| 5| 6| 7|20|21|22|23|36|37|38|39|52|53|54|55|68|69|70|71|
| super     | i-bitmap  | d-bitmap  |--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--|
|           |           |           | 8| 9|10|11|24|25|26|27|40|41|42|43|56|57|58|59|72|73|74|75|
|           |           |           |--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--|
|           |           |           |12|13|14|15|28|29|30|31|44|45|46|47|60|61|62|63|76|77|78|79|
+-----------+-----------+-----------+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
0KB         4KB         8KB         12KB        16KB        20KB        24KB        28KB        32KB
```

Convert the inode offset bytes into disk sector and find the inode data content.

Most important thing of inode design: metadata, e.g., type, time, etc., and **how indexing into data region**. Simple way: record disk address in data region, one pointer to one block contained in file. But have file size limit.

### The Multi-Level Index

Use multi-level pointers, inode points to set of more pointers. Inode can have 12 direct pointers + 1 indirect pointer. If file size > 12 * 4KB, use indirect pointer.

Or use extents: a disk pointer + length (in blocks). Need to consider contiguous space. 

| Pointer-Based           | Extent-Based  |
|-------------------------|---------------|
| Flexible                | Less flexible |
| Large metadata per file | More compact  |

Besides, there is linked-list based approach. Poor perf for random access. Old FS **FAT** used in old Windows.

FS measurement summary to guide design:

| Most files are small                 | ˜2K is the most common size                       |
|--------------------------------------|---------------------------------------------------|
| Average file size is growing         | Almost 200K is the average                        |
| Most bytes are stored in large files | A few big files use most of space                 |
| File systems contains lots of files  | Almost 100K on average                            |
| File systems are roughly half full   | Even as disks grow, file systems remain ˜50% full |
| Directories are typically small      | Many have few entries; most have 20 or fewer      |

## 40.4 Directory Organization

Directory is a simple list. It's a simple name -> inode number mapping. Each entry: inode number, record length, string length, name of the entry:

| inum | reclen | strlen | name                          |
|------|--------|--------|-------------------------------|
| 5    | 12     | 2      | `.`                           |
| 2    | 12     | 3      | `..`                          |
| 12   | 12     | 4      | `foo`                         |
| 13   | 12     | 4      | `bar`                         |
| 24   | 36     | 28     | `foobar_is_a_pretty_longname` |

Delete file can leave hole in the directory. New entry may reuse the hole.

Directory is a special file: it has inode, index into data region.

## 40.5 Free Space Management

FS needs to track which inodes & data blocks are free by **Free Space Management**: bitmap, free lists, B-tree, etc.

When create a file, need to allocate an inode. FS search through the bitmap for free inode, allocate it to the file, mark the inode as used, eventually update the on-disk bitmap.

## 40.6 Access Paths: Reading and Writing

Assume that: FS mounted, superblock in memory, everything else (inodes, directories) is still on the disk.

### Reading A File From Disk

Open 12KB file (3 blocks), read, close. 

1.  Open. FS needs to find the inode by full pathname. 
    1.  The **root directory `/`** inode number is *well known*: `2`. When get the root inode, FS uses the on-disk pointers to read directory and looking for file (inode number).
    2.  DFS go down and match to target file.
    3.  Load on-disk target file inode into memory, do permission check, allocate file descriptor.
2.  Read. Read in the first block of file, check with inode and find its disk address. Update the in-memory open file table.
3.  Close. Free the file descriptor.

The timeline to read file `foo/bar`:

`open(foo/bar)`:

1.  read root inode;
2.  read root directory data block
3.  read `foo` directory inode
4.  read `foo` directory data block
5.  read `bar` file inode
6.  read `bar` file data block

I/O time to open file is proportional to pathname length.

`read()`:

7.  read `bar` file inode
8.  read `bar` data block `[0]`
9.  write `bar` file inode to update last access time
10. update in-memory current file offset
11. read `bar` file inode
12. read `bar` data block `[1]`
13. write `bar` file inode to update
14. update in-memory current file offset

### Writing A File To Disk

Open the file is the same as above. But write to file may need allocate free data block. So also need to update bitmap to track allocation:

`write()` call:

1.  read `bar` file inode
2.  read `bar` allocation bit in data bitmap (free or overwrite)
3.  write allocated to `bar` allocation bit in data bitmap
4.  write data to `bar` data block `[i]`
5.  write `bar` file inode for last update time
6.  update in-memory open file table entry

## 40.7 Caching and Buffering

Need to aggressively use DRAM to cache important blocks to reduce I/O time.

LRU fixed-size cache (**Static partitioning**) to hold popular blocks. It's around 10% of total memory and allocated at boot time.

**Dynamic partitioning**: integrate virtual memory pages & file system pages into same cache: *unified page cache*. 

OS will buffer writes in memory for 5-30 seconds. But need to consider system crash. This is the **Durability/Performance Trade-Off**. Choose the strategy based on application requirements. If cannot tolerate data lost, e.g., DBMS, use `fsync()`.

# Chapter 41. Locality and The Fast File System

Old UNIX file system, really simple, a first step:

```
+---+--------+--------------+
| S | Inodes | Data         |
+---+--------+--------------+
```

-   Superblock: metadata about the entire FS: volume size, number of inodes, pointer to free block list head, etc.
-   inodes: all inodes
-   data: files.

## 41.1 The Problem: Poor Performance

Problem: OUFS treated disk like RAM; data spread over the place without the fact of disk. Need to expensive seek.

Worse: **Fragmented**: free space was not carefully managed: Logically contiguous file is distributed across the disk. Example, file `A,B,C,D` over 8 blocks (each file 2 blocks): `A1,A2,B1,B2,C1,C2,D1,D2`. Delete `B,D`: `A1,A2,_,_,C1,C2,_,_`. Allocate file `E` with 4 blocks: `A1,A2,E1,E2,C1,C2,E3,E4`.

**Internal Fragmentation**: Small block can minimize internal fragmentation, but bad for transfer.

## 41.2 FFS: Disk Awareness Is The Solution

Berkeley built a better faster FS, *Fast File System (FFS)*. FFS is **Disk aware**.

## 41.3 Organizing Structure: The Cylinder Group

FFS divide disk into **cylinder groups**. One cylinder is a set of tracks on different surfaces of a hard drive that are the same distance from the center:

```
height  [m]:  circle[m][0], circle[m][1], ... circle[m][n]
...
height  [1]:  circle[1][0], circle[1][1], ... circle[1][n]
height  [0]:  circle[0][0], circle[0][1], ... circle[0][n]
```

Then a cylinder group is:

```
cylinder group{i} = { circle[0][i], circle[1][i], ... circle[m][i] }
```

The group is like a geometric cylinder. Then the whole disk is a set of cylinder groups:

```
disk = { CG[0], CG[1], ..., CG[n] }
```

Modern drives do not provide hardware details but a logical address space of blocks. Thus modern FS (ext2,3,4) organize the drive into **block groups**, e.g., every 8 blocks a group.

Cylinder groups or block groups, inner-group seeking is faster than cross-group seeking. FFS keeps each group like a small FS: superblock, inodes, data.

## 41.4 Policies: How To Allocate Files and Directories

Basic mantra: keep related stuff together & keep unrelated stuff far apart. Related stuff are placed in the same block group, unrelated stuff placed in different groups.

For directory, it can be placed evenly across the groups. For a new dir to be placed, find a group that: 1) has low number of dirs; 2) has large number of free inodes. Insert the dir into this group. Can use other strategies.

For files, 1) allocate the file in the group with its inode; 2) files in same dir are together in the group of dir.

Example, each group has 10 inodes, 10 blocks, one directory uses 1 data block, one file uses 2 data blocks. 3 roots: `/,/a,/b`, 4 files: `/a/x, /a/y, /a/z, /b/u`. Then:

```
grp | inodes     | data blocks
0   | /--------- | /---------
1   | axyz------ | axxyyzz---
2   | bu-------- | buu-------
```

Bad case to allocate sequentially: (if we have 8 groups)

```
grp | inodes     | data blocks
0   | /--------- | /---------
1   | a--------- | a---------
2   | b--------- | b---------
3   | x--------- | xx--------
4   | y--------- | yy--------
5   | z--------- | zz--------
6   | u--------- | uu--------
7   | ---------- | ----------
```

## 41.5 Measuring File Locality

Statistics show that locality of file/dir access. Another pattern is: 

```
/proj
    /src
        foo.c
    /obj
        foo.o
```

## 41.6 The Large-File Exception

For large file, one entire group may not be enough. It's not good and may hurt file-access locality. 

Divide the large file into chunks to fit into multiple groups. This will add cross-group seeking and hurt perf. But it can be mitigated by choosing chunk size carefully. If chunk size is large, the **amortized** seeking time is smaller.

The number of chunks: $\frac{LargeFileSize}{ChunkSize}$

The average positioning time (seek & rotate) for one group: $T_{pos}$

The disk transfer speed: $v_{trans}$

Then the total time is:

$$T_{total} = \frac{LargeFileSize}{ChunkSize} \cdot ( \frac{ChunkSize}{v_{trans}} + T_{pos} )$$

So the ratio of positioning is:

$$r = \frac{\frac{LargeFileSize}{ChunkSize} \cdot T_{pos}}{T_{total}} = \frac{T_{pos}}{\frac{ChunkSize}{v_{trans}} + T_{pos}} = \frac{T_{pos} \cdot v_{trans}}{ChunkSize + T_{pos} \cdot v_{trans}}$$

One fact is: it's easy to improve $v_{trans}$ but difficult to improve $T_{pos}$.

## 41.7 A Few Other Things About FFS

Small file concern: most files are only 2KB in size, but use 4KB block. So the internal fragmentation is high.

FFS uses sub-blocks (512B). Allocate 512B first until 4KB and copy all 512B sub-blocks into one whole block. Not efficient enough. So should avoid this behavior: `libc` should buffer write in 4KB chunks to FS and avoid sub-block in most cases. 

Disk layout: old: `0,1,2,3,4,5,6,7,8,9,10,11` in circle, read `0,1`. Read `0` first and then `1`, but it's too late after reading `0` and transfer, the head will invoke a full rotation. So use better layout by skipping: `0,6,1,7,2,8,3,9,4,10,5,11` in circle. Read `0`, when read is complete, head goes over `6`, now request next block read for `1`. This is **parameterization**.







