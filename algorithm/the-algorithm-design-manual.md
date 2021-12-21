2rd by Steven S. Skiena

# Chapter 12. Data Structures

## 12.1 Dictionaries

### Unsorted linked lists or arrays

Small data sets (50 - 100 items). Linked list is bad in cache compared with array. One improvement due to uneven access frequencies & locality: LRU linked list or array, MRU is at position `[0]`.

### Sorted linked lists or arrays

Iff not many insertions or deletions. Usually not worth the effort.

### Hash tables

Moderate large data sets (100 - 10M). Hash function to identify the buckets and do operation. But for well-tuned hash table, several problems:

-   Collisions: open addressing? bucketing?
-   The size of the table: 30% buckets more than data set?
-   Hash function

### Binary search trees

Fast insertions, deletions, queries. Major differences between different versions: Balancing operations. _AVL tree_, _Red-Black tree_, self-organizaing _Splay tree_.

### B-trees

Large data set (more than 1M items) that cannot fit in main memory. Idea of B-tree: collapse several levels of BST into a single large node. Make the equivalent of several search before disk access. 

### Skip lists

Maintain a hierarchy of sorted linked lists.

## 12.2 Priority Queues

Useful in simulations, particularly for events ordered by time. If no more insertions, use a sorted list is enough. But for insertions, deletions, quries, need a priority queue.

### Sorted array or list

Efficient to identify the smallest element and delete it. But insertion new item is slow. Good for large deletions but few insertions.

### Binary heaps

Insertion and extract-min in $O(\lg(n))$ time. Heaps maintain an implicit BST in array. The minimum key is always the top of heap.

Binary heap is good when know the priority queue size has upper bound. 

### Bounded height priority queue

$O(1)$ insertion and find-min. If all keys are in range 1 ... n, then an array with n slots, each with a linked list. List `[i]` in slot `[i]` is for key `i`. 

Useful in maintaining the vertices of a graph sorted by degree. Good for small, discrete range of keys.

### Binary search trees

Smallest: leftmost leaf; Largest: rightmost leaf. $O(\lg(N))$ time for searching if balanced.

### Fibonacci and pairing heaps

Complicated priority queuesto speed up _decrease key_ operations. Decrease the priority key of an existing node in the queue. Good for large computations.

## 12.3 Suffix Trees and Arrays

Used in string problems, from $O(n^2)$ to linear time. 

