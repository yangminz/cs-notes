3rd Edition By Randal E. Bryant and David R. O'Hallaron

# Chapter 8. Exception Control Flow

## 8.1 Exceptions

### 8.1.2 Classes of Exceptions

```
+---------------+-------------------------------+-----------+-------------------------------+
| Class         | Cause                         | (A)Sync   | Return behavior               |
+---------------+-------------------------------+-----------+-------------------------------+
| Interrupt     | Signal from I/O device        | Async     | Always return to next inst    |
| Trap/Syscall  | Intentional exception         | Sync      | Always return to next inst    |
| Fault         | Potentially recoverable error | Sync      | Might return to current inst  |
| Abort         | Nonrecoverable error          | Sync      | Never returns                 |
+---------------+-------------------------------+-----------+-------------------------------+
```

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


# Chapter 9. Virtual Memory

## 9.7 Case Study: The Intel Core i7/Linux Memory System

### 9.7.2 Linux Virtual Memory System

#### Linux Virtual Memory Areas

virtual memory: collection of areas or segments. each existing virtual page is contained in some area. cannot reference pages outside the all areas. Allow virutal address space to have gaps. kernel do not track non-existing pages.

`task_struct->mm_struct->pgd`: when process scheduled to run, stores `pgd` to CR3 control register.

`task_struct->mm_struct->mmap`: area structs `vm_area_structs` list: describe the virtual adderss space area.

#### Linux Page Fault Exception Handling

MMU triggers a page fault when translate virtual address `vaddr`. control transfered to kernel's page fault handler:

1.  Is `vaddr` legal? Check `vm_area_structs` list. If illegal, issue **Segmentation Fault**, because process is accessing a nonexistent page. It's fatal, program exits. use red-black to do fast search, because `mmap` may create many many areas.
2.  Is `mem[vaddr]` readable or writeable? Check if the process is privileged to perform the operation. E.g. write to `.text` segment ==> **Protection Fault**. program exits. *remember `fork`, `vm_area_struct` r/w but `page_table` ro, then demand paging.*
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

