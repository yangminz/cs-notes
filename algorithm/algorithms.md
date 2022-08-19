4th edition by Robert Sedgewick and Kevin Wayne

# Chapter 3 Searching
# Chapter 4 Graphs

## 4.4 Shortest Paths

### Properties of shortest paths:

-   Paths are directed.
-   The weights are not necessarily distances.
-   Not all vertices need be reachable.
-   Negative weights introduce complications.
-   Shortest paths are normally simple.
-   Shortest paths are not necessarily unique.
-   Parallel edges and self-loops may be present.

The result of computation is a *Shortest-Paths Tree (SPT)* gives a shortest path from source `s` to every vertex reachable from `s`. **This tree always exists**.

### Edge-weighted digraph data types

Two data structures to store the information:

-   `EdgeTo[v]`: Stores the last edge in shortest path from `s` to `v`. i.e., if the shortest path is `s, ..., u, v`, then `EdgeTo[v] = u`.
-   `DistTo[v]`: Stores the path total weights to destination `v`.

**Edge Relaxation**

Test whether the best known way from `s` to `w` will go through `v`. 

```cs
private void Relax(Vertex s, Vertex v, Vertex w, Graph graph)
{
    Edge e = graph.GetDirectedEdge(v, w);
    if (s.DistTo[w] > s.DistTo[v] + e.GetWeight())
    {
        s.DistTo[w] = s.DistTo[v] + e.GetWeight();
        s.EdgeTo[w] = e;
    }
    // else, ignore this edge
}
```

> The term relaxation follows from the idea of a rubber band stretched tight on a path connecting two vertices: relaxing an edge is akin to relaxing the tension on the rubber band along a shorter path, if possible. 

**Vertex Relaxation**: Relax all outgoing edges. The incoming edges have been relaxed previously by preceding verticles.

### Theoretical basis for shortest-paths algorithms

***Optimality conditions***: There is an equivalence between between the *global* condition that the distances are shortest-paths distances, and the *local* condition that we test to relax an edge.

For graph $G = (V, E)$, source vertex $s$ and distance-to table $D(s, \cdot)$ (i.e., `DistTo[]`). For table $D(s, w)$, 

-   $D(s, w)$ is the possible total weight of *some* path $s \rightsquigarrow w$, if $w$ is reachable from $s$;
-   $D(s, w) = +\infty$, if $w$ is not reachable from $s$.

$D(s, w)$ is the weights of shortest paths ($\delta(s, w) = \min\{D(s, w)\}$) iff for any edge $(v, w)$, there is:

$$D(s, w) \leq D(s, v) + W(v, w)$$

i.e., no edge is eligible.

**Proof:** 

Necessary: If there is an edge $(v, w)$, s.t., 

$$D(s, w) > D(s, v) + W(v, w)$$

The path $s \rightsquigarrow v, w$ is shorter than the path of $D(s, w)$, so the path is not the shortest path.

Sufficient: By *triangle inequality*, suppose the shortest path is: $s = u_0, u_1, u_2, \cdots, u_n = w$

$$D(s, w) = D(u_0, u_n) \leq D(u_0, u_{n - 1}) + W(u_{n - 1}, u_n) \leq D(u_0, u_{n - 2}) + W(u_{n - 2}, u_{n - 1}) + W(u_{n - 1}, u_n) \leq \cdots$$

Finally,

$$D(s, w) \leq \sum_{i = 1}^n W(u_{i - 1}, u_i) = \delta(s, w)$$

Since $D(s, w) = W(s \rightsquigarrow w)$, so it must be bigger than the shortest path:

$$\delta(s, w) \leq D(s, w) \leq \delta(s, w)$$

Then $D(s, w) = \delta(s, w)$.