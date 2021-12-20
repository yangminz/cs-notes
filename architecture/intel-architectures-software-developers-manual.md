By Intel

# Volume 1: Basic Architecture

## Chapter 6. Procedure Calls, Interrupts, and Exceptions

### 6.2 Stacks

Only one stack is available at a time: the one contained in the segment referenced by `SS` register.

### 6.4 Calling Procedures Using CALL and RET

#### 6.4.5 Calls to Other Privilege Levels

User mode to kernel mode interface: **Gate**, tightly controlled and protected interface. 

#### 6.4.6 CALL and RET Operation Between Privilege Levels

From user mode to kernel mode (more privileged protection level):

1.  Performs an access rights check (privilege check)
2.  Temporarily saves (internally) the current contents of `SS, ESP, CS, EIP` registers.
3.  Loads the segment selector and stack pointer for the new stack from `TSS` into the `SS, ESP` registers, so the new (kernel) stacks is switched to. 
4.  Push the temporarily saved `SS, ESP` values for the calling procedure's

### 6.5 Interrupts and Exceptions

Intel CPU provides 2 mechanisms for interrupting program execution: 

-   **Interrupt**: Async event typically triggered by an I/O device
-   **Exception**: Sync event generated when CPU detects _predefined conditions_ while executing an instruction. 3 classes of exceptions: faults, traps, aborts.

CPU responds to interrupts & exceptions essentially the same way. When interrupt/exception signaled, CPU halts the execution of current program, switches to a handler procedure to handle the interrupt/exception condition. CPU accesses the handler via **Interrupt Descriptor Table (IDT)**. 

IA32: 18 predefined interrupts & exceptions, 224 user defined, in IDT. Each interrupt/exception is identified with a number, called **vector**.

1.  software interrupts
2.  maskable hardware interrupts

When CPU detects int/ex, do one of the following:

1.  Executes an implicit call to a handler procedure
2.  Executes an implicit call to a handler task

#### 6.5.1 Call and Return Operation for Interrupt or Exception Handling Procedures

A call to int/ex is like procedure call to another protection level. 2 kinds of gates in IDT:

1.  interrupt gate
2.  trap gate

These gates provide the following:

1.  Access rights info
2.  Segment selector for the code segment that contains the handler procedure
3.  Offset into the code segment to the first instruction of the handler procedure

In one word, the address of the first instruction of handler & access info.

If called through interrupt gate: The handler execution would not be interfered. Clear the interrupt enable flag `IF` in `EFLAGS` register.

If called through trap gate: No change to `IF` flag.

^^^

If kernel interrupted to kernel, handler uses the current kernel stack, no stack switch, no privilege-level change. CPU will do the following when calling an int/ex handler:

1.  Pushes the current values of `EFLAGS, CS, EIP` to current stack (kernel);
2.  Pushes error code on stack;
3.  Loads the segment selector from int/trap gate for the new code segment to `CS`, new instruction pointer from int/trap gate to `EIP`;
4.  Clears `IF` flag in `EFLAGS` if interrupt gate;
5.  Begins execution of the handler from `EIP`.

```
+                     +
|                     |
+---------------------+
|                     | <--+ ESP Before
+---------------------+      Transfer to Handler
| Pushed EFLAGS       |
+---------------------+
| Pushed CS           |
+---------------------+
| Pushed EIP          |
+---------------------+
| Pushed Error Code   | <--+ ESP After
+---------------------+      Transfer to Handler
|                     |
+                     +
    Kernel Stack

```

`IRET` instruction to return from kernel to kernel:

1.  Restores `CS, EIP` register values;
2.  Restores `EFLAGS` register
3.  Increments the stack pointer appropriately
4.  Resumes execution of the interrupted procedure.

^^^

If user interrupted to kernel, CPU uses kernel stack. Stack changes and privilege level changes. CPU do the following:

1.  Temporarily saves (internally) the current values of `SS, ESP, EFLAGS, CS, EIP`;
2.  Loads the segment selector and stack pointer for the new stack (kernel stack) from `TSS` into `SS, ESP` registers. **Stack Switching**;
3.  Pushes the temporarily saved `SS, ESP, EFLAGS, CS, EIP` values of the user procedure onto new stack (kernel);
4.  Pushes error code on the new stack (kernel);
5.  Loads the segment and PC info from IDT entry (gate);
6.  If interrupt gate, clears `IF` flag in `EFLAGS`;
7.  Begins the execution of handler procedure at new privilege level (kernel;

```
                +                    +   +                     +
                |                    |   |                     |
ESP Before      +--------------------+   +---------------------+
Transfer   +--> | Local variables    |   | Pushed SS           |
to Handler      +--------------------+   +---------------------+
                |                    |   | Pushed ESP          |
                |                    |   +---------------------+
                |                    |   | Pushed EFLAGS       |
                |                    |   +---------------------+
                |                    |   | Pushed CS           |
                |                    |   +---------------------+
                |                    |   | Pushed EIP          |
                |                    |   +---------------------+
                |                    |   | Pushed Error Code   | <--+ ESP After
                |                    |   +---------------------+      Transfer to Handler
                |                    |   |                     |
                +                    +   +                     +
                     User Stack              Kernel Stack

```

Return from kernel to user:

1.  Performs a privilege check;
2.  Restores `CS, EIP` registers to their values prior to the int/ex;
3.  Restores the `EFLAGS` register;
4.  Restores `SS, ESP` registers, resulting in a stack switch back to the interrupted stack;
5.  Resumes execution of the interrupted procedure;

#### 6.5.2 Calls to Interrupt or Exception Handler Tasks

**Task Gate Descriptor**: task gate provides access to the address space for the handler task.

## Chapter 7. Programming With General-Purpose Instructions

### 7.3 Summary of GP Instructions

#### 7.3.8 Control Transfer Instructions

##### 7.3.8.4 Software Interrupt Instructions

`INT nn` - software interrupt

`INTO` - interrupt on overflow

`BOUND` - detect value out of range

These instructions allow a program to **explicitly raise a specified interrupt or exception**, which in turn causes the handler routine for the interrupt or exception to be called.

`INT nn` can raise _any of the processor's interrupts or exceptions by encoding the vector of the interrupt or exception in the instruction_.

^^^

Software interrupts are handled directly in the CPU core as any other instruction, but Self-IPIs are passing through Local APIC as any other hardware interrupt. While INT nn only mimics hardware interrupt, Self-IPI is indistinguishable from IRQ requests generated by IO devices.

## Chapter 18. Input/Output

CPU: transfers data to & from: (1) external memory; (2) I/O ports. I/O ports are created in system hardware by circuity that decodes the control, data, and address pins on the processor. These I/O ports are configured to communicate with peripheral devices.

### 19.1 I/O Port Addressing

_Problem: how does I/O generate interrupt? I/O APIC redirects to CPU's local APIC_

App are permitted to access I/O ports in either of 2 ways:

1.  Through a separate I/O address space: I/O instruction set & special I/O protection mechanism.
2.  Through memory-mapped I/O: general-purpose move and string instructions, with protection provided through segmentation or paging.

### 19.2 I/O Port Hardware

From HW's point, I/O addressing is handled through CPU's address lines. It's HW's responsibility to decode the memory-I/O bus transaction. 

# Volume 3: System Programming Guide

## Chapter 6. Interrupt and Exception Handling

Int/Ex: events that indicates a condition exists taht requires CPU's attention: in system, processor, or task. Result in a forced transfer of execution from the current task to a _special software routine_, int/ex handler. 

### 6.3 Sources of Interrupts

1.  External (HW generated) interrupts
2.  Software-generated interrupts

#### 6.3.1 External Interrupts

External interrupts are received through:

1.  Pins on the processor
2.  Local APIC(Advanced Programmable Interrupt Controller)

Processor reads from the system bus the interrupt vector number provided by an external interrupt controller, **8529A**. 

The processor's local APIC is connected to a system-based I/O APIC. External interrupts received at the I/O APIC's pins, they are directed to local APIC through system bus to the processor. 

I/O APIC determines the vector number of the interrupt and send the vector to local APIC. Multi-core: processors can send interrupts to each other via system bus.

#### 6.3.2 Maskable Hardware Interrupts

Any external interrupt delivered to processor by `INTR` pin or local APIC is a maskable HW interrupt. Mask via `IF` flag.

#### 6.3.3 Software-Generated Interrupts

`int N` instruction generates interrupts from within software. Cannot be masked.

(So software generated interrupts will not go through APIC hardware)

### 6.4 Sources of Exceptions

3 sources:

1.  Processor-detected program-error exceptions;
2.  Software-generated exceptions;
3.  Machine-check exceptions;

#### 6.4.1 Program-Error Exceptions

When program errors during execution, processor generates one or more exceptions: (1) faults; (2) traps; (3) aborts.

#### 6.4.2 Software-Generated Exceptions

`int` instructions, e.g. `INT3` causes a breakpoint exception.

`INT n` instruction can be used to _emulate_ exceptions in software. But limitation on _error code_ on stack behavior.

#### 6.4.3 Machine-Check Exceptions

Vector 18

### 6.5 Exception Classifications

-   **Fault**: Recoverable. Program restarts the execution with no loss of continuity. 
-   **Traps**: Execute the next instruction on return.
-   **Abort**: Not recoverable.

## Chapter 10. Advanced Programmable Interrupt Controller (APIC)

Local APIC 2 primary functions:

1.  Receive interrupts from processor's interrupt pins, from internal sources, from external I/O APIC or other external interrupt controllers. Local APIC sends these to the processor core for handling.
2.  In multiple processor systems, local APIC sends & receives _Inter-Processor Interrupt (IPI)_ messages to other logical processors on the system bus.

### 10.1 Local and I/O APIC Overview

Each local APIC consists of a set of APIC registers, and associated hardware that control the delivery of interrupts to the processor core and the generation of IPI messages. 

_The APIC registers are memory mapped and can be read & written to using `mov` instruction._

Local APICs can receive interrupts from the following sources:

1.  Locally connected I/O devices. Directly connected to processor's local interrupt pins (`LINT0, LINT1`).
2.  Externally connected I/O devices. From I/O APIC.
3.  Inter-processor interrupts (IPIs). Used for software self-interrupts, interrupt forwarding, **preemptive scheduling**.
4.  **APIC timer generated interrupts.** Send local interrupt to local processor when a programmed count is reached.
5.  Performance monitoring counter interrupts. Interrupt when perf-monitoring counter overflows.
6.  Thermal Sensor interrupts. When internal thermal sensor has been tripped.
7.  APIC internal error interrupts. When error condition is recognized within local APIC (e.g. attempt to access an unimplemented register).

When receive a signal from local source (`LINT0, LINT1` pins, timer, perf counter, thermal sensor, error detector), local APIC delivers the interrupts to processor core by _interrupt delivery protocol_.

The delivery protocol is defined by APIC registers named **local vector table, LVT**.  For example, if the LINT1 pin is going to be used as an NMI pin, the LINT1 entry in the local vector table can be set up to deliver an interrupt with vector number 2 (NMI interrupt) to the processor core.

(IPI: `int` instructions?)

Use **interrupt command register, ICR** in local APIC to generate IPIs. Write to ICR, then IPI message generated and issued on system bus or on AIPC bus.

```
    +-------------+      +-------------+
    |             |      |             |
    |     CPU     |      |     CPU     |
    |             |      |             |
    +-------------+      +-------------+
    | Local APIC  |      | Local APIC  |
    +-----+-+-----+      +-----+-+-----+
          | |                  | |
Interrupt | | IPIs   Interrupt | | IPIs
Messages  | |        Messages  | |
          | |                  | |
          | |                  | |
  +-------+-+---------+--------+-+-------+ System Bus
                      | Interrupt Messages
         +--------------------------------+
         |            |                   |
         |       +----+---+               |
         |       | Bridge |               |
         |       +----+---+               |
         |            |         PCI       |
         |   +--------+---------+-----+   |
         |                      |         |
         |                +-----+----+    |
         |                | I/O APIC | <-----+ External
         |                +----------+    |    Interrupts
         |                                |
         +--------------------------------+
                    System Chip Set

Relationship of Local APIC and I/O APIC 
```

### 10.4 Local APIC

#### 10.4.1 The Local APIC Block Diagram

Software interacts with local APIC by r/w registers. The registers are memory-mapped to 4KB region of physical address space, starting from `0xFEE00000H`. It's never cacheable!!!

`ISR`: In-service register

### 10.5 Handling Local Interrupts

#### 10.5.1 Local Vector Table

The local vector table (LVT) allows OS to specify the local interrupts to be delivered to processor core:

**LVT Timer Register**: (`0xFEE00320`). Specifies interrupt delivery when APIC timer signals an interrupt.

The delivery protocol:

-   Vector: the interrupt vector number
-   Delivery Mode: SMI, NMI, INIT, etc.

#### 10.5.2 Valid Interrupt Vectors

The 256 interrupt vector numbers in Intel 64 and IA32. Vector from `0` to `15` is sent or received through local APIC. 

```
Exception number    Description                 Exception class
---------------------------------------------------------------
0                   Divide error                Fault
13                  General protection fault    Fault
14                  Page fault                  Fault
```

#### 10.5.5 Local Interrupt Acceptance

If interrupt is accepted, logged into `IRR` register and handled by the processor by its priority. If rejected, sent back to local APIC and retried.

### 10.6 Issuing InterProcessor Interrupts

Primary facility for IPIs: **interrupt command register (ICR)**. It's used for:

-   To send an int to another processor;
-   To allow a processor to forward an int to another processor;
-   To direct the processor to interrupt itself (self interrupt);
-   To deliver special IPIs.

#### 10.6.1 Interrupt Command Register (ICR)

Processor send IPI: write the `ICR` register (64 bits) by the protocol:

-   Vector
-   Delivery Mode
-   Destination Mode
-   etc

### 10.8 Handling Interrupts

When local APIC receives int from local source, int message from I/O APIC or IPI, based on the processor implementation, it will handle the interrupts.