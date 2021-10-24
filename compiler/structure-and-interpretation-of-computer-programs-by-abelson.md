# Chapter 5. Computing with Register Machines

## 5.3 Storage Allocation and Garbage Collection

list-structured memory: list-structured data

2 considerations:

1.  representing Lisp pair with storage and address
2.  memory management

Lisp: automatic storage allocation. Purpose: abstraction for +inf memory capacity.

### 5.3.1 Memory as Vectors

primitive operations: `read(memory, address)`, `write(memory, address, data)`

memory address be treated as data and stored in memory & registers. **address arithmetic**

vector: `get_field(vector_base_address, field_offset)`. Access time to different field should be the same $O(1)$

#### Representing Lisp data

**Typed pointers** tagged data. generic pointer + data type information. data type in primitive machine level. 

type information encoding varies depending on machine. execution efficiency dependent on strategy. 

user maintains a table: `obarray`: symbols it has encountered. Replace object with existing pointers: interning symbols:

```
request(object)
{
    if (obarray.contains(object))
    {
        return object;
    }
    allocate(object);
    obarray.add(object);
}
```

#### Implementing the primitive list operations

1.  identify memory vectors
2.  memory primitive operations
3.  pointer arithemetic does not change type

`free` register holds a pair pointer containing the next available index. use it to find the next free location. other implementation: free list.

#### Implementing stacks

stack: modeled in lists: a list of saved values

## 5.3.2 Maintaining the Illusion of Infinite Memory

most of the pairs hold intermeddiate result only, when no longer needed, garbage. collect garbage periodically, illusion: infinite amount of memory.

detect: which pair are not needed - the content can no longer influence the future of the computation.

GC: at any moment, the object that can affect the future of the computation are those can be reached. not reachable can be recycled.

**Stop and Copy**

*working memory* and *free memory*:

`cons`: construct

`car`: contents of the address part of the register `(a, d) => a`

`cdr`: content of the decrement part of the register `(a, d) => d`

```
construct(pair)
{
    if (working memory is not full)
    {
        construct pair in working memory
    }
    else
    {
        // GC
        // locating useful data
        tracing car cdr pointers (machine registers)

        copy useful pairs to free memory (compating copy)

        swap working and free memory
    }
}
```

another GC tech: mark-sweep

#### Implementation of a stop-and-copy garbage collector

`root` register

```
// Just before GC
car, cdr: <working memory>
+---------------------------------------+
|   Mixture of useful data and garbage  |   (full)
+---------------------------------------+

new-car, new-cdr: <free memory>
+---------------------------------------+
|   Free memory                         |
+---------------------------------------+

// Just after GC
car, cdr: <new free memory>
+---------------------------------------+
|   Discarded memory                    |
+---------------------------------------+

new-car, new-cdr: <new working memory>
+-------------------+-------------------+
|   Useful data     |   Free data       |
+-------------------+-------------------+
```