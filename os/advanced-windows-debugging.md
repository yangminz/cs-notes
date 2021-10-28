By Mario Hewardt and Daniel Pravat

# Chapter 5. Memory Corruption Part I - Stacks

difficult to track:

1.  corruption be far apart
2.  unusual conditions to trigger

possible source:

-   thread writes to block it does not own
-   thread writes its own block but corrupts the state of the memory

## Memory Corruptin Detection Process

