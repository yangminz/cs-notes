Original Edition By Hal Abelson, Jerry Sussman and Julie Sussman

[JavaScript Edition](https://sourceacademy.org/sicpjs/) By Martin Henz and Tobias Wrigstad

# Chapter 1. Building Abstractions with Functions

## Programming in JavaScript

Just-In-Time (JIT) compilation

first-class functions: function as variable. Lambda expression, functional abstraction.

## 1.1  The Elements of Programming

-   **primitive expressions**, which represent the simplest entities the language is concerned with,
-   **means of combination**, by which compound elements are built from simpler ones, and
-   **means of abstraction**, by which compound elements can be named and manipulated as units.

### 1.1.1  Expressions

Combinations: `operand operator operand ...` *Infix Notation*: `(1 + 2) * 3`

> JavaScript programmers know the value of everything but the cost of nothing.

### 1.1.2  Naming and the Environment

Constant declarations:

```js
const size = 2;
size = 4;
//  VM367:1 Uncaught TypeError: Assignment to constant variable.
//      at <anonymous>:1:6
```

Program environment: the memory to keep track of name-object pairs.

### 1.1.3   Evaluating Operator Combinations

Evaluate one operator combination:

1.  Evaluate the operand expressions of the combination. Evaluation rule is **Recursive**!! Finally to leaf nodes (primitive):
        -   the values of numerals are the numbers that they name
        -   the values of names are the objects associated with those names in the environment
2.  Apply the function that is denoted by the operator to the arguments that are the values of the operands.

`const x = 3;` is not a combination.

### 1.1.4   Compound Functions

Function declarations:

```js
function square(x) {
    return x * x;
}
```

Function application expression:

```js
square(14);
```

### 1.1.5   The Substitution Model for Function Application

```
f(5)
sum_of_squares(a + 1, a * 2) (5)    // substitue `f`
sum_of_squares(5 + 1, 5 * 2)        // substitue `a`
sum_of_squares(6, 10)               // evaluate parameters
square(6) + square(10)              // substitue `sum_of_squares`
(6 * 6) + (10 * 10)                 // substitue `square`
36 + 100                            // evaluate leaf nodes
136
```

Substitution is not on text or string, but on **MEMORY**.

#### Applicative order versus normal order

-   Applicative-order evaluation model: evaluate the arguments and then apply (js)
-   Normal-order evaluation model: fully expand and then reduce (memory costly)

```
f(5)
sum_of_squares(5 + 1, 5 * 2)                // substitue `a`
square(5 + 1) + square(5 * 2)               // substitue `sum_of_squares`
((5 + 1) * (5 + 1)) + ((5 * 2) * (5 * 2))   // substitue `square`
136
```

### 1.1.6   Conditional Expressions and Predicates

Conditional expression:

```js
function abs(x) {
    // predict ? expression-true : expression-false
    return x >= 0 ? x : x === 0 ? 0 : - x;
} 
```

Right-associative.

`l && r` or `l || r`, `&&` and `||` are **syntactic forms**, not operators; their right-hand expression `r` is not always evaluated. *SHORT CUT.* `!a`, `!` is unary operator. `a` would be evaluated.

#### Exercise

Test applicative-order or normal-order evaluation with short cut:

```js
function p() { return p(); }

function test(x, y) {
    return x === 0 ? 0 : y;
} 

test(0, p());
```

`p()` is infinite recursive that never returns.

-   Applicative Order: `test(x, y)`, `y = p()` would never be evaluated, never return.
-   Normal Order: `test(0, p()) ==> 0 === 0 ? 0 : p()`, short cut to `0`, return `0`.

### 1.1.7  Example: Square Roots by Newton's Method

In mathematics we are usually concerned with declarative (what is) descriptions, whereas in computer science we are usually concerned with imperative (how to) descriptions.

```js
function sqrt(x) {
    return sqrt_iter(1, x);
} 

function sqrt_iter(guess, x) {
    return is_good_enough(guess, x)
           ? guess
           : sqrt_iter(improve(guess, x), x);
}

function improve(guess, x) {
    return average(guess, x / guess);
} 

function average(x, y) {
    return (x + y) / 2;
} 

function is_good_enough(guess, x) {
    return abs(square(guess) - x) < 0.001;
} 
```

The function `sqrt_iter`, on the other hand, demonstrates how iteration can be accomplished using no special construct other than the ordinary ability to call a function. **Tail Recursion** ensures efficiency.

#### Exercise

```js
function conditional(predicate, then_clause, else_clause) {		    
    return predicate ? then_clause : else_clause;
} 
```

In this case, short cut will not work (for applicative-order). Must evaluate all parameters:

```js
conditional(true, good(), dead_loop());
```

### 1.1.8   Functions as Black-Box Abstractions

Recursive: function is defined in terms of itself. 

The decomposition of the problem into subproblems. Take sub-solutions as black box. 

> So a function should be able to suppress detail. The users of the function may not have written the function themselves, but may have obtained it from another programmer as a black box. A user should not need to know how the function is implemented in order to use it.

#### Local names

The meaning of a function should be independent of the parameter names:

```js
function square(x) {
    return x * x;
} 

function square(y) {
    return y * y;
} 
```

*Thus the parameter names of a function must be local to the body of the function.* If not, then not black box. -- Name isolation: parameter names are local to function body.

-   Bound
-   Bind
-   Free
-   Scope

#### Internal declarations and block structure

Localize the subfunctions to not mix up with other functions:

```js
function sqrt(x) {
    function is_good_enough(guess) {
        return abs(square(guess) - x) < 0.001;
    }
    function improve(guess) {
        return average(guess, x / guess);
    }
    function sqrt_iter(guess) {
        return is_good_enough(guess)
               ? guess
               : sqrt_iter(improve(guess));
    }
    return sqrt_iter(1);
} 
```

`x` **Lexical scoping**. 

## 1.2   Functions and the Processes They Generate

### 1.2.1  Linear Recursion and Iteration

Factorial:

```js
function factorial(n) {
    return n === 1 
           ? 1
           : n * factorial(n - 1);
} 
```

Substitution model, recursive. Deferred operations: a chain of multiplications. Need to keep track of operations to be performed later on. **Linear recursive process**.

```
factorial(4)
4 * factorial(3)
4 * (3 * factorial(2))
4 * (3 * (2 * factorial(1)))
4 * (3 * (2 * 1))
4 * (3 * 2)
4 * 6
24
```

Iteration does not grow and shrink. **Iterative process**: maintain *state variables*, e.g. loop counter. **Linkear iterative process**. 

```js
function factorial(n) {
    return fact_iter(1, 1, n);
}
function fact_iter(product, counter, max_count) {
    return counter > max_count
           ? product
           : fact_iter(counter * product,
                       counter + 1,
                       max_count);
}
```

C, Java, Python, etc. stack memory usage grows with number of calls (call stack), even when the process is iterative. **Tail recursive** implementation: no difference. Iteration is syntatic sugar for recursion.

#### Exercise

Ackermann's function:

```js
function A(x, y) {
    return y === 0
           ? 0
           : x === 0
           ? 2 * y
           : y === 1
           ? 2
           : A(x - 1, A(x, y - 1));
} 
```

### 1.2.2  Tree Recursion

Fibonacci:

```js
function fib(n) {
    return n === 0
           ? 0
           : n === 1
           ? 1
           : fib(n - 1) + fib(n - 2);
} 
```

Iterative, much more efficient:

```js
function fib(n) {
    return fib_iter(1, 0, n);
}
function fib_iter(a, b, count) {
    return count === 0
           ? b
           : fib_iter(a + b, a, count - 1);
} 
```

#### Example: Counting change

Recursive

```js
function count_change(amount) {
    return cc(amount, 5);
}

function cc(amount, kinds_of_coins) {
    return amount === 0
           ? 1
           : amount < 0 || kinds_of_coins === 0
           ? 0
           : cc(amount, kinds_of_coins - 1)
             +
             cc(amount - first_denomination(kinds_of_coins),
                kinds_of_coins);
}

function first_denomination(kinds_of_coins) {
    return kinds_of_coins === 1 ? 1
         : kinds_of_coins === 2 ? 5
         : kinds_of_coins === 3 ? 10
         : kinds_of_coins === 4 ? 25
         : kinds_of_coins === 5 ? 50
         : 0;   
}

count_change(100);
```

Same problem: duplicate calculation. Iterative method:

```
f(n) =    f(n - 1) * 1  // +1
        + f(n - 2) * 2  // +1, +1; +2
        + f(n - 3) * 3  // +1, +1, +1; +1, +2; +3
        + ...
```

### 1.2.3  Orders of Growth

$$ k_1 \times f(n) \leq \Theta(f(n)) \leq k_2 \times f(n) $$

### 1.2.4  Exponentiation

$O(N)$ exp:

```js
function expt(b, n) {
    return expt_iter(b, n, 1);
}
function expt_iter(b, counter, product) {
    return counter === 0
           ? product
           : expt_iter(b, counter - 1, b * product);
} 
```

Linear:

```
b^8 = b * b^7
b^7 = b * b^6
b^6 = b * b^5
b^5 = b * b^4
b^4 = b * b^3
b^3 = b * b^2
b^2 = b * b
```

$O(\log N)$ exp:

```js
function square(x) {
    return x * x;
}

function is_even(n) {
    return n % 2 === 0;
}

function fast_expt(b, n) {
    return n === 0
           ? 1
           : is_even(n)
           ? square(fast_expt(b, n / 2))
           : b * fast_expt(b, n - 1);
}
```

Binary:

```
b^8 = b^4 * b^4
b^4 = b^2 * b^2
b^2 = b * b
```

#### Exercise

Binary Fibbonacci with power of matrix:

$$ [F_n, F_{n-1}] \times [[1, 0], [1, 0]] = [F_{n+1}, F_{n}]$$

```js
function fib(n) {
    return fib_iter(1, 0, 0, 1, n);
}

function fib_iter(a, b, p, q, count) {
    return count === 0
           ? b
           : is_even(count)
           ? fib_iter(a,
                      b,
                      p * p + q * q,
                      2 * p * q + q * q,
                      count / 2)
           : fib_iter(b * q + a * q + a * p,
                      b * p + a * q,
                      p,
                      q,
                      count - 1);
} 
```

### 1.2.5  Greatest Common Divisors

`GCD(a, b)` is the largest integer that divides both `a` and `b` with no remainder. E.g. `GCD(16, 28)` is `4`.

Euclid's algorithm:

```js
function gcd(a, b) {
    return b === 0 ? a : gcd(b, a % b);
} 
```

> Lamé's Theorem: If Euclid's Algorithm requires $k$ steps to compute the GCD of some pair, then the smaller number in the pair must be greater than or equal to the $k^{th}$ Fibonacci number.

Thus the complexity is $\Theta(\log N)$.

### 1.2.6  Example: Testing for Primality

Linear search for prime testing:

```js
function is_prime(n) {
    return n === smallest_divisor(n);
} 
function smallest_divisor(n) {
    return find_divisor(n, 2);
}
function find_divisor(n, test_divisor) {
    return square(test_divisor) > n
           ? n
           : divides(test_divisor, n)
           ? test_divisor
           : find_divisor(n, test_divisor + 1);
}
function divides(a, b) {
    return b % a === 0;
} 
```

$O(\sqrt{N})$ for `is_prime(n)`: $\sqrt{n} \times \sqrt{n} = n$.

> Fermat's Little Theorem: If $n$ is a prime number and $a$ is any positive integer less than $n$, then $a$ raised to the $n^{th}$ power is congruent to $a$ modulo $n$.
> Two numbers are said to be **congruent modulo** $n$ if they both have the same remainder when divided by $n$.

Example. Prime number $n = 5$, positive integer $a = 4$, $a^n = 4^5 = 1024$. $a^n \mod n = 1024 \mod 5 = 4$, $a \mod n = 4 \mod 5 = 4$.

Use Fermat's Little Theorem to test prime number: pick some $a < n$ and check $a^n \mod n$ with $a \mod n$:

```js
function square(x) {
    return x * x;
}

function is_even(n) {
    return n % 2 === 0;
}

function expmod(base, exp, m) {
    return exp === 0
           ? 1
           : is_even(exp)
           ? square(expmod(base, exp / 2, m)) % m
           : (base * expmod(base, exp - 1, m)) % m;
}

function random(n) {
    return math_floor(math_random() * n);
}

function fermat_test(n) {
    function try_it(a) {
        return expmod(a, n, n) === a;
    }
    return try_it(1 + math_floor(math_random() * (n - 1)));
}

function fast_is_prime(n, times) {
    return times === 0
           ? true
           : fermat_test(n)
           ? fast_is_prime(n, times - 1)
           : false;
}
```

The non-prime number meets the condition that $a^n \mod n = a \mod n$ is rare (*Carmichael numbers*, 561, 1105, 1729, 2465, 2821, 6601 ...). So Fermat test is reliable in practice. **Probabilistic algorithms**.

#### Exercise

Miller-Rabin test (variant of Fermat test): For $a < n$ and $n$ prime: 

$$a^{n-1} \mod n = 1$$

code:

```js
function random(n) {
    return math_floor(math_random() * n);
}
function miller_rabin_test(n) {
    function expmod(base, exp, m) {
        return exp === 0
               ? 1
               : is_even(exp)
               ? square(trivial_test(expmod(base,
                                            exp / 2,
                                            m), 
                                     m))
                 % m
               : (base * expmod(base, exp - 1, m)) 
                 % m;
    }
    function trivial_test(r, m) {
        return r === 1 || r === m - 1
               ? r
               : square(r) % m === 1
               ? 0  // Nontrivial square root of 1 mod n
               : r;
    }
    function try_it(a) {
        return expmod(a, n - 1, n) === 1;
    }
    return try_it(1 + random(n - 1));
}
function do_miller_rabin_test(n, times) {
    return times === 0
           ? true   // pass all n tests
           : miller_rabin_test(n)   // pass previous tests, test this time
           ? do_miller_rabin_test(n, times - 1) // pass this test, goto next
           : false; // fail this time
} 
```

## 1.3   Formulating Abstractions with Higher-Order Functions

Build abstractions by assigning names to common patterns and then to work in terms of the abstractions directly.

Construct functions that can accept functions as arguments or return functions as values. Functions that manipulate functions are called **higher-order functions**.

### 1.3.1   Functions as Arguments

$\sum_i f(i)$ is a high order function takes $f$ as parameters:

```js
function sum(f, index, next, end) {
    return index > end
           ? 0
           : f(a) + sum(f, next(a), next, b);
}
```

Numerical integral:

$$\int_a^b f = dx \cdot \left[ f\left(a + \frac{dx}{2}\right) + f\left(a + dx + \frac{dx}{2}\right) + f\left(a + 2dx + \frac{dx}{2}\right) + \cdots \right] $$

Then

```js
function sum(f, a, next, b) {
    return a > b
           ? 0
           : f(a) + sum(f, next(a), next, b);
}

function cube(x) {
    return x * x * x;
}

function integral(f, a, b, dx) {
    function add_dx(x) {
        return x + dx;
    }
    return sum(f, a + dx / 2, add_dx, b) * dx;
}

integral(cube, 0, 1, 0.01);
```

#### Exercise

Even more abstract than $\sum_i f(i)$:

```js
function accumulate(combiner, null_value, f, a, next, b) {
    function iter(a, result) {
        return a > b
               ? result
               : iter(next(a), combiner(f(a), result));
    }
    return iter(a, null_value);
}
```

Even more abstract:

```js
function filtered_accumulate(combiner, null_value,
                             f, a, next, b, filter) {
    return a > b
           ? null_value
           : filter(a)
             ? combiner(f(a), 
                   filtered_accumulate(combiner, null_value, 
                                       f, next(a), next, 
                                       b, filter))
             : filtered_accumulate(combiner, null_value, 
                                   f, next(a), next, 
                                   b, filter);
} 
```

### 1.3.2   Constructing Functions using Lambda Expressions



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