5th Edition By John L. Hennessy and David A. Patterson

# Chapter 4. The Processor

## Introduction

Task: construct the datapath and control unit for two diff erent implementations of the MIPS instruction set. Pipelined MIPS.

### A Basic MIPS Implementation

core MIPS insruction set illustrating the key principles in creating datapath and designing control.

-   `lw` load word, `sw` store word
-   `add`, `sub`, `AND`, `OR`, `slt`: arithmetic and logical instructions
-   `beq` branch equal, `j` jump

ISA affects CPI *clock cycles per instruction* and clock rate.

#### An Overview of the Implementation

First 2 steps for each instruction:

1.  Index **Program Counter** `PC` in memory and fetch the instruction bytes from memory
2.  Register operands detected by the fields of the instruction. Register operands fetched.

3 classes of instructions: Memory-Reference, Arithmetic-Logical, Branches. Take **ALU** Arithmetic-Logical Unit as example. After reading the registers, the instructions use ALU for:

-   Memory-Reference instruction: address calculation
-   Arithmetic-Logical instruction: operation execution
-   Branch instruction: comparison

Register Files input: multiplexer or data selector. Selects input from ALU or data cache, register or immediate field of the instruction.

```csharp
instruction_cycle()
{
    // fetch
    Instruction_t inst = GetInstructionBytes(ProgramCounter.rip);

    AluResult_t alu_result = ALU.Compute(inst);

    switch (inst.operator)
    {
        case LOAD_STORE_INSTRUCTION:
            // treat it as address to load or store memory content
            Address_t address = (Address_t)alu_result;
            ProgramCounter.rip = ProgramCounter.Next();
            RegisterFiles.Select(inst.fields.registers).ReadOrWriteFromMemoryToRegister(address);
            break;
        case ARITHMETIC_LOGICAL_INSTRUCTION:
            Data_t data = (Data_t)alu_result;
            RegisterFiles.Select(inst.fields.registers).WriteImmediateToRegister(data);
            break;
        case BRANCH_INSTRUCTION:
            ProgramCounter.rip = Jump(Flags.GetFlags(alu_result));
            break;
        default:
            break;
    }
}
```

Use **Control Unit** to select with instructions as input.

## 4.2 Logica Design Conventions

Datapath element types:

1.  Elements operating on data values. Combinational element = operational element = AND gate or ALU. Give same input, output is the same.
2.  Elements containing state. State element = register or memory. Reset by power on/off. Same input, output may vary.

### Clocking Methodology






Pipeline: multiple instructions are overlapped at the same time. Making processor fast in throughput (not execution time).

Pipeline stage/segment. CPU designer: balance the length of each stage to eliminate idle time. Ideally, 5-stage pipeline is 5x faster. But due to overhead, different latencies on different instructions, etc., the acceleration < 5x.

But it will increase the execution time of an individual instruction. Meet the latency of most time-consuming instruction.

Instruction level parallelism. 

## A Pipelined Datapath

5-stage instruction:

1.  IF: Instruction Fetch - Memory and cache
2.  ID: Instruction Decode and Register Fetch - register read
3.  EX: Execution and Effective Address Calculation - ALU's work
4.  ME: Memory Access
5.  WB: Write Back - register write: Place the result back into register file


