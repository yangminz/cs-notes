3rd edition by Thomas H. Cormen, Charles E. Leiserson, Ronald L. Rivest, Clifford Stein

# III Data Structures

## 13 Red-Black Trees



# VI Graph Algorithms

## 24 Single-Source Shortest Paths

### 24.5 Proofs of shortest-paths properties

**Triangle inequality**

Let $G = (V, E)$ be a weighted, directed graph with weight function $w: E \rightarrow \mathbb{R}$ and source vertex $s$. Then, for all edges $(u, v) \in E$, we have

$$\delta(s, v) \leq \delta(s, u) + w(u, v)$$

**Proof:** For any path from $s$ to $v$, it must go through one neighbor $u$ of $v$: $(s \rightsquigarrow u, v)$. Then the path weight can be divided into 2 parts:

$$w(s \rightsquigarrow u, v) = w(s \rightsquigarrow u) + w(u, v)$$

This is a function about 2 variables:

1.  The neighbor $u$ of $v$;
2.  The path from $s$ to $u$: $(s \rightsquigarrow u)$

So fix the neighbor $u$ first and take the path to get the local minimal weight as function:

$$\varphi(u) = \min\left( w(s \rightsquigarrow u) + w(u, v) \right) = \delta(s, u) + w(u, v)$$

Then choose the neighbor $u$ to get the global minimal weight:

$$\delta(s, v) = \min\{\varphi(u)\} \leq \delta(s, u) + w(u, v)$$
