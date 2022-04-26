Original Edition By Hal Abelson, Jerry Sussman and Julie Sussman

[JavaScript Edition](https://sourceacademy.org/sicpjs/) By Martin Henz and Tobias Wrigstad

# Chapter 1. Building Abstractions with Functions

## Programming in JavaScript

Just-In-Time (JIT) compilation

first-class functions: function as variable. Lambda expression, functional abstraction.

## 1.1  The Elements of Programming

- **primitive expressions**, which represent the simplest entities the language is concerned with,
- **means of combination**, by which compound elements are built from simpler ones, and
- **means of abstraction**, by which compound elements can be named and manipulated as units.

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

1. Evaluate the operand expressions of the combination. Evaluation rule is **Recursive**!! Finally to leaf nodes (primitive):
   
       -   the values of numerals are the numbers that they name
       -   the values of names are the objects associated with those names in the environment

2. Apply the function that is denoted by the operator to the arguments that are the values of the operands.

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

- Applicative-order evaluation model: evaluate the arguments and then apply (js)
- Normal-order evaluation model: fully expand and then reduce (memory costly)

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

- Applicative Order: `test(x, y)`, `y = p()` would never be evaluated, never return.
- Normal Order: `test(0, p()) ==> 0 === 0 ? 0 : p()`, short cut to `0`, return `0`.

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

- Bound
- Bind
- Free
- Scope

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

$$
k_1 \times f(n) \leq \Theta(f(n)) \leq k_2 \times f(n) 
$$

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

$$
[F_n, F_{n-1}] \times [[1, 0], [1, 0]] = [F_{n+1}, F_{n}]
$$

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

$$
a^{n-1} \mod n = 1
$$

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

$$
\int_a^b f = dx \cdot \left[ f\left(a + \frac{dx}{2}\right) + f\left(a + dx + \frac{dx}{2}\right) + f\left(a + 2dx + \frac{dx}{2}\right) + \cdots \right] 
$$

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

Lambda expression: syntactic form for creating functions.

```
(parameters) => expression
```

With name or without name:

```js
const sqrt_sum = ((x, y) => x * x + y * y);
sqrt_sum(3, 4); // with name
((x, y) => x * x + y * y)(3, 4); // without name
```

### 1.3.3   Functions as General Methods

Functions used to express general methods of computation, independent of the particular functions involved.

#### Finding roots of equations by the half-interval method

$$
f(x) = 0
$$

```js
function average(x, y) {
    return (x + y) / 2;
}

function positive(x) { return x > 0; }
function negative(x) { return x < 0; }

function abs(x) {
    return x >= 0 ? x : - x;
}

function close_enough(x, y) {
    return abs(x - y) < 0.001;
}

function search(f, neg_point, pos_point) {
    const midpoint = average(neg_point, pos_point);
    if (close_enough(neg_point, pos_point)) {
        return midpoint;
    } else {
        const test_value = f(midpoint);
        return positive(test_value)
               ? search(f, neg_point, midpoint)
               : negative(test_value)
               ? search(f, midpoint, pos_point)
               : midpoint;
    }
}

search(x => x * x - 1, 0, 2);
```

#### Finding fixed points of functions

$$
f(x) = x
$$

```js
function abs(x) {
    return x >= 0 ? x : - x;
}

const tolerance = 0.00001;
function fixed_point(f, first_guess) {
    function close_enough(x, y) {
        return abs(x - y) < tolerance;
    }
    function try_with(guess) {
        const next = f(guess);
        return close_enough(guess, next)
               ? next
               : try_with(next);
    }
    return try_with(first_guess);
}

fixed_point(math_cos, 1);
```

### 1.3.4   Functions as Returned Values

Return lambda:

```js
function average_damp(f) {
    return x => average(x, f(x));
} 
```

> Expert programmers know how to choose the level of abstraction appropriate to their task.

Elements with the fewest restrictions are said to have **first-class status**. Some of the "rights and privileges" of first-class elements are:

- They may be referred to using names.
- They may be passed as arguments to functions.
- They may be returned as the results of functions.
- They may be included in data structures.

Functions are first-class in JS.

# Chapter 2.  Building Abstractions with Data

**Compound data**: E.g. rational number (a numerator and a denominator).

**Data abstraction**: how data objects are represented; how data objects are used.

```js
// a,b,x,y can be rational number, complex number, ...
function linear_combination(a, b, x, y) {
    return add(mul(a, x), mul(b, y));
}
```

Data abstraction enables us to erect suitable *abstraction barriers* between different parts of a program.

1. **Closure**: combine data objects with (primitive, compound) data objects.
2. **Conventional Interfaces**: compound data objects can serve as interface.
3. **Symbolic Expressions**: data composed of arbitrary symbols rather than numbers only.
4. **Generic Operations**: handling different types of data - more powerful abstraction barriers. *data-directed programming*.

## 2.1  Introduction to Data Abstraction

Abstract data.

### 2.1.1   Example: Arithmetic Operations for Rational Numbers

For a rational number, can *construct*, can extract/**select** the numberator & denominator. Define the interface of **constructor** and **selector**:

- `make_rat(n, d)` returns the rational number whose numerator is the integer nn and whose denominator is the integer `d`.
- `numer(x)` returns the numerator of the rational number `x`.
- `denom(x)` returns the denominator of the rational number `x`.

With interface for constructor & selector, can implement add/sub/multiple/divide/test operations:

```js
function add_rat(x, y) {
    return make_rat(numer(x) * denom(y) + numer(y) * denom(x),
                    denom(x) * denom(y));
}
function sub_rat(x, y) {
    return make_rat(numer(x) * denom(y) - numer(y) * denom(x),
                    denom(x) * denom(y));
}
function mul_rat(x, y) {
    return make_rat(numer(x) * numer(y),
                    denom(x) * denom(y));
}
function div_rat(x, y) {
    return make_rat(numer(x) * denom(y),
                    denom(x) * numer(y));
}
function equal_rat(x, y) {
    return numer(x) * denom(y) === numer(y) * denom(x);
}
```

#### Pairs

JS pair:

```js
const x = pair(1,2);
head(x); // 1
tail(x); // 2
```

This is the pair implementation of lambda calculus. Lambda expression has 3 forms:

1. `x`: Variable. Representing the value.
2. `(x => E)`: Abstraction. Definition of function. Variable `x` is the bound in expression.
3. `E(F)`: Application. Apply lambda term `F` to expression `E`.

The reduction operations:

1. `(x => E[x])`$\rightarrow$`(y => E[y])`: $\alpha$-conversion, rename the bound variables `x` to `y` to avoid name confliction.
2. `(x => E)(F)` $\rightarrow$ `(E[x := F])`: $\beta$-reduction, replace the bound variables `x` with expression `F`.

Then a pair

```js
pair = a => (b => (f => (f(a))(b)));
```

`f` is the operator for the pair. Then select:

```js
head = p => p(x => (y => x));
tail = p => p(x => (y => y));
```

While we can take these 2 higher-order function as bool:

```js
true  = x => (y => x);
false = x => (y => y);
```

Then get head operation:

```js
p12 = (pair(1))(2)
= ((a => (b => (f => (f(a))(b))))(1))(2)
```

$\beta$-reduction `a := 1`:

```js
((a => (b => (f => (f(a))(b))))(1))(2)
= (a => (b => (f => (f(1))(b))))(2)
```

Then the sub-expression `(a => (b => (f => (f(1))(b))))` is no longer bounded by variable `a`, so the parameter can be removed:

```js
(a => (b => (f => (f(1))(b))))(2)
= (b => (f => (f(1))(b)))(2)
```

$\beta$-reduction `b := 2`, also sub-expression is not bounded by parameter `b`:

```js
(b => (f => (f(1))(b)))(2)
= b => (f => (f(1))(2))
= f => (f(1))(2)
```

Select `head` by applying the pair `p12`:

```js
head(p12)
= (p => p(x => (y => x)))(p12)
= p12(x => (y => x))
= (f => (f(1))(2))(x => (y => x))    // apply `true` to `f`
= ((x => (y => x))(1))(2)
= (y => 1)(2)
= 1
```

Data objects constructed from pairs are called *list-structured data*.

### Representing rational numbers

Implement the interfaces:

```js
function make_rat(n, d) {
    const g = gcd(n, d);
    return pair(n / g, d / g);
} 
function numer(x) { return head(x); }
function denom(x) { return tail(x); }
numer(make_rat(2, 3));
```

### 2.1.2   Abstraction Barriers

Abstraction barriers isolate different "levels" of the system. Maintain the flexibility to consider alternate implementations. E.g. another implementation, `gcd` when get: 

```js
function make_rat(n, d) {
    return pair(n, d);
}
function numer(x) {
    const g = gcd(head(x), tail(x));
    return head(x) / g;
}
function denom(x) {
    const g = gcd(head(x), tail(x));
    return tail(x) / g;
} 
```

### 2.1.3   What Is Meant by Data?

Data: constructors, selectors, conditions of these functions. E.g. for rational number, the condition is: For any `x = make_rat(n, d)`, then `numer(x)/denom(x) == n/d`.

Define `pair` in this way: if `z = pair(x, y)`, then `head(z) == x` and `tail(z) == y`. Any triple of functions `<pair, head, tail>` satisfies the condition can be used to implement `pair`:

```js
function pair(x, y) {
    function dispatch(m) {
        return m === 0 
               ? x
               : m === 1 
               ? y
               : error(m, "argument not 0 or 1 -- pair");
    }
    return dispatch;          
}
function head(z) { return z(0); }

function tail(z) { return z(1); } 
```

The functional representation of `pair` is above. Behind is lambda-calculus. `dispatch` is True/False selector.

#### Exercise

Another implementation:

```js
function pair(x, y) {
    return f => f(x, y);
}

function head(z) {
    return z((p, q) => p);
}

function tail(z) {
    return z((p, q) => q);
}
```

**Church numerals**

Integer system without number but only functions:

```js
const zero = f => (x => x);

function add_1(n)
{
    return f => (x => f(n(f)(x)));
}
```

And define one and two:

```js
const one = f => (x => f(x));
const two = f => (x => f(f(x)));
```

Define plus of non-negative integers, and we get three as one plus two:

```js
function plus(n, m)
{
    return f => (x => n(f)(m(f)(x)));
}
const three = plus(one, two);
```

Note that this function `plus` is **currying** function:

```js
plus = (n, m) => (f => (x => n(f)(m(f)(x))));
const three = plus(one, two);

plus = n => (m => (f => (x => n(f)(m(f)(x)))));
const three = (plus(one))(two);
```

We check three with currying :

```js
const three
= (plus(one))(two)
= (m => (f => (x => one(f)(m(f)(x)))))(two)         // beta: apply one to n
= (m => (f => (x => (z => f(z))(m(f)(x)))))(two)    // alpha: x in one to z
= (m => (f => (x => f(m(f)(x)))))(two)              // beta: apply (m(f)(x)) to z
= f => (x => f(two(f)(x)))                          // beta: apply two to m
= f => (x => f((g => (t => g(g(t))))(f)(x)))        // alpha: replace f,x in two
= f => (x => f((t => f(f(t)))(x)))                  // beta: apply f to g
= f => (x => f(f(f(x))))                            // beta: apply x to t
```

`three` is a function, we can evaluate as non-negative number. Function `f` for recursive relationship is `+`, this is the operator. Starting number `x` would be zero: 

```js
function evaluate(num_func) {
    return num_func(n => n + 1)(0);
}
evaluate(three)
= (three(n => n + 1))(0)
= (x => (t => t + 1)((m => m + 1)((n => n + 1)(x))))(0)
= (x => (t => t + 1)((m => m + 1)(x + 1)))(0)
= (x => (t => t + 1)((x + 1) + 1))(0)
= (x => ((x + 1) + 1) + 1)(0)
= (((0 + 1) + 1) + 1)
```

We do not reduce the result `(((0 + 1) + 1) + 1)` to `3` intentionally to see the calculation steps.

## 2.2  Hierarchical Data and the Closure Property

Since everything is function/lambda-calculus, `pair` can not only pair a number but also a `pair`. As a consequence, pairs provide a universal building block from which we can construct all sorts of data structures.

Binary tree node:

```js
pair(
    2,    // value
    pair(
        pair(1, ...),    // left child
        pair(3, ...)     // right child
    )
);
```

This is the ***closure property*** of `pair`: the ability to create `pair` whose elements are `pair`. Closure property permits us to create *hierarchical* structures.

A note: Any part of a graph itself is a graph / Any part of a tree itself is a tree / Any part of a list itself is a list.

Another note: closure -- set $S$, operation $f$, for all elements in set $\forall e \in S$, the operation result is closed: $f(e) \in S$. A tree $e$ from tree set ($S$), take a part of it ($f$), is still a tree ($f(e) \in S$).



# Chapter 5. Computing with Register Machines

## 5.3 Storage Allocation and Garbage Collection

list-structured memory: list-structured data

2 considerations:

1. representing Lisp pair with storage and address
2. memory management

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

1. identify memory vectors
2. memory primitive operations
3. pointer arithemetic does not change type

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