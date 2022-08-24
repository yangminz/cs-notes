3rd edition by Thomas H. Cormen, Charles E. Leiserson, Ronald L. Rivest, Clifford Stein

# III Data Structures

## 13 Red-Black Trees

### 13.1 Properties of red-black trees

Define the tree in pre-order dfs format: Color(Red/Black), link to left-subtree, link to right-subtree -- $(X^{R/B}, Left, Right)$. The tree with **black height** $n$: $T_n$. Then red-black tree follows these reduction constraints:

1.  $Root = T_{n \geq 1} \Longrightarrow (X^B, T_{n-1}, T_{n-1})$
2.  $T_0 \Longrightarrow \otimes | (X^R, \otimes, \otimes)$
3.  $T_{n \geq 1} \Longrightarrow (X^B, T_{n-1}, T_{n-1})  | (Y^R, (X^B, T_{n-1}, T_{n-1}), (Z^B, T_{n-1}, T_{n-1}))$

### 13.2 Rotations

Type 1:

$$(G, (P, (X, \alpha, \beta), \gamma), \delta) \Longrightarrow (P, (X, \alpha, \beta), (G, \gamma, \delta))$$

$$(G, \alpha, (P, \beta, (X, \gamma, \delta))) \Longrightarrow (P, (G, \alpha, \beta), (X, \gamma, \delta))$$

Type 2:

$$(G, (P, \alpha, (X, \beta, \gamma)), \delta) \Longrightarrow (X, (P, \alpha, \beta), (G, \gamma, \delta))$$

$$(G, \alpha, (P, (X, \beta, \gamma), \delta)) \Longrightarrow (X, (G, \alpha, \beta), (P, \gamma, \delta))$$

### 13.3 Insertion

Insert to leaf node position as BST. Insert the new node as red node $(X^R, \otimes, \otimes)$ to avoid increase in black height.

#### Case 1: When the parent leaf node is BLACK

For example, $(P^{B}, \otimes, T)$. Then the black height should be 1, so the right sub-tree should be $T_0$, i.e.,

$$(G, (P^B, \otimes, T_0)_1, T_1)$$

Now insert the new red node $X^R$ to $P^B.Left$:

$$(G, (P^B, (X^R, \otimes, \otimes)_0, T_0)_1, T_1)$$

This is good and will not break any rule. Other situations in this case are:

$$(G, (P^B, T_0, \otimes)_1, T_1) \Longrightarrow (G, (P^B, T_0, (X^R, \otimes, \otimes)_0)_1, T_1)$$

$$(G, T_1, (P^B, T_0, \otimes)_1) \Longrightarrow (G, T_1, (P^B, T_0, (X^R, \otimes, \otimes)_0)_1)$$

$$(G, T_1, (P^B, \otimes, T_0)_0) \Longrightarrow (G, T_1, (P^B, (X^R, \otimes, \otimes)_0, T_0)_1)$$

#### Case 2: When the parent leaf node is RED

Then the direct insertion would violate the rule of non-consecutive red nodes:

$$(G^{B}, (P^R, \otimes, \otimes)_0, T_0)_1 \Longrightarrow (G^{B}, (P^R, (X^R, \otimes, \otimes), \otimes), T_0)$$

Then we need to rotate the nodes $\{X^R, P^R, G^B\}$:

$$(G^{B}, (P^R, (X^R, \otimes, \otimes), \otimes), T_0) \Longrightarrow (P^R, (X^R, \otimes, \otimes)_0, (G^B, \otimes, T_0)_1) $$

And recolor:

$$(P^R, (X^R, \otimes, \otimes)_0, (G^B, \otimes, T_0)_1) \Longrightarrow (P^R, (X^B, \otimes, \otimes)_1, (G^B, \otimes, T_0)_1)_1$$

Now the root node is $P^R$ compared with $G^B$ before. So $P^R$ may conflict with its new parent, need to recursively go up and fix. Now we consider this general situation. 

*Shape 1*: First rotate:

$$\Big(G^{B}, (P^R, (X^R, T_n^B, T_n^B)_n, T_n^B)_n, T_n\Big)_{n+1} \Longrightarrow \Big(P^R, (X^R, T_n^B, T_n^B)_n, (G^B, T_n^B, T_n)_{n+1}\Big) $$

Then recolor:

$$\Big(P^R, (X^R, T_n^B, T_n^B)_n, (G^B, T_n^B, T_n)_{n+1}\Big) \Longrightarrow \Big(P^R, (X^B, T_n^B, T_n^B)_{n+1}, (G^B, T_n^B, T_n)_{n+1}\Big)_{n+1}$$

*Shape 2*: First rotate:

$$\Big(G^{B}, (P^R, T_n^B, (X^R, T_n^B, T_n^B)_{n})_{n}, T_n\Big)_{n+1} \Longrightarrow \Big(X^R, (P^R, T_n^B, T_n^B)_{n}, (G^B, T_n^B, T_n)_{n+1}\Big)_{n+1}$$

Then recolor:

$$\Big(X^R, (P^R, T_n^B, T_n^B)_{n}, (G^B, T_n^B, T_n)_{n+1}\Big)_{n+1} \Longrightarrow \Big(X^R, (P^B, T_n^B, T_n^B)_{n+1}, (G^B, T_n^B, T_n)_{n+1}\Big)_{n+1} $$

One possibility is that we do not need to rotate if the parent's sibling is also red:

$$\Big(G^B, (P^R, T_n^B, (X^R, T_n^B, T_n^B)_{n})_{n}, (H^R, T_n^B, T_n^B)_{n}\Big)_{n+1}$$

We just need to recolor the root node of $H^R$ from red to black:

$$\Longrightarrow \Big(G^R, (P^B, T_n^B, (X^R, T_n^B, T_n^B)_{n})_{n+1}, (H^B, T_n^B, T_n^B)_{n+1}\Big)_{n+1}$$

Recursively fix up until there is no such pattern. If the final pattern is root, just recolor the red root to black.

### 13.4 Deletion

Consider the deletion of BST. 

#### Case 1: Delete a leaf node

*1.1: when the leaf node is red*, i.e., delete $(X^R, \otimes, \otimes)$. Then its parent must be black $P^B$ and another subtree must be $T_0$:

$$(P^B, (X^R, \otimes, \otimes)_0, T_0)_1$$

or 

$$(P^B, T_0, (X^R, \otimes, \otimes)_0)_1$$

In either case, we just delete the red node $X^R$ and this will not break any rule nor decrease the black height:

$$(P^B, (X^R, \otimes, \otimes)_0, T_0)_1 \Longrightarrow (P^B, \otimes, T_0)_1$$

*1.2: when the leaf node is black*, i.e., delete $(X^B, \otimes, \otimes)$. The pattern would be:

$$(P, (X^B, \otimes, \otimes)_1, T_1)$$

If we delete $X^B$, it will create a **Double Black Node**, i.e., the black height for this node is count twice to make the overall black height balanced:

$$\Longrightarrow (P, \otimes^{DB}, T_1)$$

**Fix Up of Double Black Node**

Consider the nearing nodes of double black node $U^{DB}$:

$$\Big(P, (U^{DB}, \alpha_{n-1}, \beta_{n-1})_{n+1}, (V, (N, \gamma, \delta), (F, \epsilon, \zeta))_{n+1}\Big)$$

So the left subtree black height is $(n-1) + 2 = n+1$, the balanced right subtree black height is also $n+1$.

But actually we do not know the colors of $\{P, V, N, F\}$. If there is any red node among them, we can rebalance the black in the double black to it. To do so, we need to discuss all possibilities. There are 9 situations and we can group them into 4 groups:

| P | V | N | F | Group |
|---|---|---|---|-------|
| B | B | B | B | 1     |
| B | B | B | R | 2     |
| B | B | R | B | 2     |
| B | B | R | R | 2     |
| B | R | B | B | 3     |
| R | B | B | B | 4     |
| R | B | B | R | 2     |
| R | B | R | B | 2     |
| R | B | R | R | 2     |

*Double Black Group 1*

All black. Then there is no red node in $\{P, V, N, F\}$ to rebalance. So the double black node should recursively go up to the parent $P$:

$$\Big(P^B, (U^{DB}, \alpha_{n-1}, \beta_{n-1})_{n+1}, (V^B, (N^B, \gamma_{n-1}, \delta_{n-1})_{n}, (F^B, \epsilon_{n-1}, \zeta_{n-1})_{n})_{n+1}\Big)_{n+2}$$

Recolor $V^B$ to red and assign the additional black from $U$ to $P$:

$$\Longrightarrow \Big(P^{DB}, (U^B, \alpha_{n-1}, \beta_{n-1})_n, (V^R, (N^B, \gamma_{n-1}, \delta_{n-1})_n, (F^B, \epsilon_{n-1}, \zeta_{n-1})_n)_n\Big)_{n+2}$$

Now the double black node is $P^{DB}$.

*Double Black Group 2*

$V$ is black, and there is at least one red node in $\{N, F\}$. Suppose that the color of $P$ is $A$, it can be black or red.

Suppose the red node is $N^R$:

$$\Big(P^A, (U^{DB}, \alpha_{n-1}, \beta_{n-1})_{n+1}, (V^B, (N^R, \gamma_{n}, \delta_{n})_{n}, (F, \epsilon, \zeta)_{n})_{n+1}\Big)$$

Rotate $\{ P^A, V^B, N^R \}$:

$$\Longrightarrow \Big(N^R, (P^A, (U^{DB}, \alpha_{n-1}, \beta_{n-1})_{n+1}, \gamma_{n})_{n+1}, (V^B, \delta_{n}, (F, \epsilon, \zeta)_{n})_{n+1}\Big)$$

Focus on path $\{N^R, P^A, U^{DB}\}$, we can swap the color of $N^R$ and $P^A$:

$$\{N^A, P^R, U^{DB}\}$$

And then rebalance the additional black to $P^R$:

$$\{N^A, P^B, U^B\}$$

Then,

$$\Longrightarrow \Big(N^A, (P^B, (U^B, \alpha_{n-1}, \beta_{n-1})_{n}, \gamma_{n})_{n+1}, (V^B, \delta_{n}, (F, \epsilon, \zeta)_{n})_{n+1}\Big)_{n+1}$$

Suppose the red node is $F$:

$$\Big(P^A, (U^{DB}, \alpha_{n-1}, \beta_{n-1})_{n+1}, (V^B, (N, \gamma, \delta)_n, (F^R, \epsilon_n, \zeta_n)_n)_{n+1}\Big)$$

Rotate the red node branch $\{P^A, V^B, F^R\}$:

$$\Longrightarrow \Big(V^B, (P^A, (U^{DB}, \alpha_{n-1}, \beta_{n-1})_{n+1}, (N, \gamma, \delta)_{n}), (F^R, \epsilon_n, \zeta_n)_{n}\Big)$$

Focus on the branch $\{P^A, V^B, F^R\}$, swap the color of $\{P^A, V^B\}$:

$$(V^A, P^B, F^R)$$

Now rebalance the additional black on $U^{DB}$ to $F^R$:

$$\Longrightarrow \Big(V^A, (P^B, (U^B, \alpha_{n-1}, \beta_{n-1})_{n}, (N, \gamma, \delta)_{n})_{n+1}, (F^B, \epsilon_n, \zeta_n)_{n+1}\Big)$$

*Double Black Group 3*

$V$ is red. Then $P$, $N$, $F$ must be black:

$$\Big(P^B, (U^{DB}, \alpha_{n-1}, \beta_{n-1})_{n+1}, (V^R, (N^B, \gamma_{n}, \delta_{n})_{n+1}, (F^B, \epsilon_{n}, \zeta_{n})_{n+1})_{n+1}\Big)_{n+2}$$

Then rotate the far branch contains the red node, e.g., $\{P^B, V^R, F^B\}$:

$$\Big(V^R, (P^B, (U^{DB}, \alpha_{n-1}, \beta_{n-1})_{n+1}, (N^B, \gamma_{n}, \delta_{n})_{n+1})_{n+2}, (F^B, \epsilon_{n}, \zeta_{n})_{n+1}\Big)_{n+2}$$

Then we need to recolor to fix the $\{V^R, F^B\}$ branch to black height $n+2$:

$$\Big(V^B, (P^R, (U^{DB}, \alpha_{n-1}, \beta_{n-1})_{n+1}, (N^B, \gamma_{n}, \delta_{n})_{n+1})_{n+2}, (F^B, \epsilon_{n}, \zeta_{n})_{n+1}\Big)_{n+2}$$

Now consider the double black node $U^{DB}$, we have new set of $\{P, V, N, F\}$ and recursively fix it.

*Double Black Group 4*

The last case, when $P$ is red and $V$, $N$, $F$ are all black:

$$\Big(P^R, (U^{DB}, \alpha_{n-1}, \beta_{n-1})_{n+1}, (V^B, (N^B, \gamma_{n-1}, \delta_{n-1})_{n}, (F^B, \epsilon_{n-1}, \zeta_{n-1})_{n})_{n+1}\Big)_{n+1}$$

This is a simple case and we only need to recolor. Give the additional black to $P$, this will increase one black height in branch of $N$ and $F$, thus deduct one black height from $V$:

$$\Longrightarrow \Big(P^B, (U^B, \alpha_{n-1}, \beta_{n-1})_{n}, (V^R, (N^B, \gamma_{n-1}, \delta_{n-1})_{n}, (F^B, \epsilon_{n-1}, \zeta_{n-1})_{n})_{n}\Big)_{n+1}$$

Now we have discussed all cases of adjusting double black nodes.

#### Case 2: Delete a node with only one subtree

In this case, we want to delete $(X, \otimes, Y)$. It's clear that subtree $Y$ must be $T_0$ and it's not null. Thus it must be $(Y^R, \otimes, \otimes)$. So $X$ must be black:

$$(X^B, \otimes, (Y^R, \otimes, \otimes)_0)_1$$

or

$$(X^B, (Y^R, \otimes, \otimes)_0, \otimes)_1$$

Consider the parent:

$$(G, (X^B, \otimes, (Y^R, \otimes, \otimes)_0)_1, T_1)$$

When delete $X^B$, just replace it by $Y^R$ and recolor $Y^R$ to black to keep the black balanced:

$$\Longrightarrow (G, (Y^B, \otimes, \otimes)_1, T_1)$$

#### Case 3: Delete a node without null child

When both left and right subtrees are not null, we use the **successor** of $X$ to replace it. Suppose its color is $A$, black or red: $X^A$. There are 2 situations of `X.Right.Left`, null or non-null:

1.  $(X^A, \alpha, (Y, \otimes, \beta_0))$
2.  $(X^A, \alpha, (Y_1, \beta, \gamma))$

*3.1*: when `X.Right.Left` is null. In this case, the successor node is $Y$. Suppose the color of $Y$ is $C$:

$$(X^A, \alpha, (Y^C, \otimes, \beta_0))$$

Swap $X^A$ with $Y^C$ without color:

$$\Longrightarrow (Y^A, \alpha, (X^C, \otimes, \beta_0))$$

This will break the property of BST ($Y > X$), but we will immediately delete $X^C$. This is discussed in case 1 and case 2 as deleting one node with at least one null link.

*3.2*: when `X.Right.Left` is not null. In this case, the successor node is the left most node of right subtree:

$$(X^A, \alpha, R_1)$$

The right subtree: $R_i = (Y_i, R_{i+1}, \beta_n)$ and the left-most node/subtree $R_n = (Y_n, \otimes, \beta_n)$. The successor node of $X^A$ is $Y_n^C$(suppose the color of $Y_n$ is C). Then we swap $Y_n^C$ and $X^A$ without color, i.e.:

$$(X^A, \alpha, R_1) \Longrightarrow (Y_n^A, \alpha, R_1)$$

$$R_n = (X^C, \otimes, \beta_n)$$

At the same time, we need to adjust the left most nodes of right subtree:

$$R_{n-1} = (Y_{n-1}, (X^C, \otimes, \beta_n), \beta_{n-1})$$

Same, this is discussed in case 1 and case 2 as removing one node with at least one null link $(X^C, \otimes, \beta_n)$.

```cs
using System;

namespace RedBlackTree
{
    public enum RedBlackColor
    {
        Red,
        Black,
    }

    public interface IRedBlackNode<TKey, TValue>
        where TKey : IComparable<TKey>
    {
        public IRedBlackNode<TKey, TValue> Parent { get; set; }
        public IRedBlackNode<TKey, TValue> Left { get; set; }
        public IRedBlackNode<TKey, TValue> Right { get; set; }
        public RedBlackColor Color { get; set; }
        public TKey Key { get; set; }
        public TValue Value { get; set; }
    }

    public class RedBlackTree<TKey, TValue>
        where TKey : IComparable<TKey>
    {
        public IRedBlackNode<TKey, TValue> Root;
        public int BlackHeight { get; private set; } = 0;

        private void Replace(IRedBlackNode<TKey, TValue> a, IRedBlackNode<TKey, TValue> b)
        {
            if (a == null || b == null)
            {
                throw new Exception();
            }

            // replace a by b
            // before call this method, all links of b should be backed up
            if (a.Parent == null)
            {
                if (a == this.Root)
                {
                    b.Parent = null;
                    this.Root = b;
                }
                else
                {
                    throw new Exception();
                }
            }
            else
            {
                if (a.Parent.Left == a)
                {
                    a.Parent.Left = b;
                    b.Parent = a.Parent;
                }
                else if (a.Parent.Right == a)
                {
                    a.Parent.Right = b;
                    b.Parent = a.Parent;
                }
                else
                {
                    throw new Exception();
                }
            }
        }

        private IRedBlackNode<TKey, TValue> Rotate(
            IRedBlackNode<TKey, TValue> g,
            IRedBlackNode<TKey, TValue> p,
            IRedBlackNode<TKey, TValue> n)
        {
            if (n == null || p == null || g == null ||  
                n.Parent != p || p.Parent != g ||
                (p.Left != n && p.Right != n) ||
                (g.Left != p && g.Right != p))
            {
                throw new Exception();
            }

            if (g.Left == p)
            {
                if (p.Left == n)
                {
                    var a = p.Right;
                    Replace(g, p);
                    p.Right = g;
                    g.Parent = p;
                    g.Left = a;
                    if (a != null)
                    {
                        a.Parent = g;
                    }
                    return p;
                }
                else if (p.Right == n)
                {
                    var a = n.Left;
                    var b = n.Right;
                    Replace(g, n);
                    n.Left = p;
                    p.Parent = n;
                    n.Right = g;
                    g.Parent = n;
                    p.Right = a;
                    g.Left = b;
                    if (a != null)
                    {
                        a.Parent = p;
                    }
                    if (b != null)
                    {
                        b.Parent = g;
                    }
                    return n;
                }
            }
            else if (g.Right == p)
            {
                if (p.Left == n)
                {
                    var a = n.Left;
                    var b = n.Right;
                    Replace(g, n);
                    n.Left = g;
                    g.Parent = n;
                    n.Right = p;
                    p.Parent = n;
                    g.Right = a;
                    p.Left = b;
                    if (a != null)
                    {
                        a.Parent = g;
                    }
                    if (b != null)
                    {
                        b.Parent = p;
                    }
                    return n;
                }
                else if (p.Right == n)
                {
                    var a = p.Left;
                    Replace(g, p);
                    p.Left = g;
                    g.Parent = p;
                    g.Right = a;
                    if (a != null)
                    {
                        a.Parent = g;
                    }
                    return p;
                }
            }
            throw new Exception();
        }

        private void InsertionFixUp(IRedBlackNode<TKey, TValue> u)
        {
            if (u == null || u.Color == RedBlackColor.Black)
            {
                throw new Exception();
            }

            if (u.Parent == null)
            {
                if (this.Root == u)
                {
                    u.Color = RedBlackColor.Black;
                    this.BlackHeight += 1;
                    return;
                }
                else
                {
                    throw new Exception();
                }
            }

            if (u.Parent.Color == RedBlackColor.Black)
            {
                // no need to fix
                // also return here if parent is tree root
                return;
            }

            // the parent color is red and this node is red
            // so the grandparent must exist and be black
            if (u.Parent.Parent == null || 
                u.Parent.Parent.Color == RedBlackColor.Red)
            {
                throw new Exception();
            }

            // check uncle
            IRedBlackNode<TKey, TValue> uncle = (u.Parent == u.Parent.Parent.Left) ?
                uncle = u.Parent.Parent.Right :
                uncle = u.Parent.Parent.Left;

            IRedBlackNode<TKey, TValue> subroot = (uncle?.Color == RedBlackColor.Red) ?
                // no rotate
                subroot = u.Parent.Parent :
                // rotate
                subroot = this.Rotate(u.Parent.Parent, u.Parent, u);

            // recolor
            subroot.Color = RedBlackColor.Red;
            if (subroot.Left != null)
            {
                subroot.Left.Color = RedBlackColor.Black;
            }
            if (subroot.Right != null)
            {
                subroot.Right.Color = RedBlackColor.Black;
            }

            // recursively fix up
            this.InsertionFixUp(subroot);
        }

        private void DoubleBlackFixUp(IRedBlackNode<TKey, TValue> d)
        {
            if (d == null)
            {
                throw new Exception();
            }

            if (d.Parent == null)
            {
                if (d == this.Root)
                {
                    this.BlackHeight -= 1;
                    return;
                }
                throw new Exception();
            }

            IRedBlackNode<TKey, TValue> p = d.Parent, s, n, f;
            if (p.Left == d && p.Right != null)
            {
                s = p.Right;
                n = s.Left;
                f = s.Right;
            }
            else if (p.Left != null && p.Right == d)
            {
                s = p.Left;
                n = s.Right;
                f = s.Left;
            }
            else
            {
                throw new Exception();
            }

            RedBlackColor pColor = p.Color;
            RedBlackColor sColor = s.Color;
            RedBlackColor nColor = (n == null) ? RedBlackColor.Black : n.Color;
            RedBlackColor fColor = (f == null) ? RedBlackColor.Black : f.Color;

            if (pColor == RedBlackColor.Black && sColor == RedBlackColor.Black &&
                nColor == RedBlackColor.Black && fColor == RedBlackColor.Black)
            {
                // case 1: all black
                s.Color = RedBlackColor.Red;
                DoubleBlackFixUp(p);
            }
            else if (nColor == RedBlackColor.Red || fColor == RedBlackColor.Red)
            {
                // case 2: s black, n or f at least one red
                // the red node must be not null
                IRedBlackNode<TKey, TValue> r = null;
                if (nColor == RedBlackColor.Red)
                {
                    r = Rotate(p, s, n);
                }
                else if (fColor == RedBlackColor.Red)
                {
                    r = Rotate(p, s, f);
                }

                r.Color = pColor;
                r.Left.Color = RedBlackColor.Black;
                r.Right.Color = RedBlackColor.Black;
            }
            else if (sColor == RedBlackColor.Red)
            {
                // case 3: s red, p, n, f black

                // we know BH(s) = BH(d) >= 1
                // then if s is red, n, f must be black nodes and thus not null
                if (n == null || f == null)
                {
                    throw new Exception();
                }

                var r = Rotate(p, s, f);
                r.Color = RedBlackColor.Black;
                // d may be null, so set p
                p.Color = RedBlackColor.Red;
                // go to p red cases
                DoubleBlackFixUp(d);
            }
            else if (pColor == RedBlackColor.Red && sColor == RedBlackColor.Black &&
                nColor == RedBlackColor.Black && fColor == RedBlackColor.Black)
            {
                // case 4: p red, v, n, f black
                p.Color = RedBlackColor.Black;
                s.Color = RedBlackColor.Red;
            }
            else
            {
                throw new Exception();
            }
        }

        public void Insert(IRedBlackNode<TKey, TValue> u)
        {
            if (u == null || u.Left != null || u.Right != null)
            {
                throw new Exception();
            }

            if (this.Root == null)
            {
                this.Root = u;
                this.Root.Color = RedBlackColor.Black;
                this.BlackHeight = 1;
                return;
            }

            u.Color = RedBlackColor.Red;

            IRedBlackNode<TKey, TValue> p = this.Root;
            while (p != null)
            {
                if (p.Key.CompareTo(u.Key) <= 0)
                {
                    if (p.Right == null)
                    {
                        p.Right = u;
                        u.Parent = p;
                        break;
                    }
                    p = p.Right;
                }
                else
                {
                    if (p.Left == null)
                    {
                        p.Left = u;
                        u.Parent = p;
                        break;
                    }
                    p = p.Left;
                }
            }
            // return here for BST

            // Fix up
            this.InsertionFixUp(u);
        }

        public void Delete(IRedBlackNode<TKey, TValue> u)
        {
            if (u == null)
            {
                throw new Exception();
            }

            if (u.Left == null && u.Right == null)
            {
                // case 1: leaf node
                if (u.Parent == null)
                {
                    if (this.Root == u)
                    {
                        this.Root = null;
                        this.BlackHeight = 0;
                        return;
                    }
                    else
                    {
                        throw new Exception();
                    }
                }

                if (u.Color == RedBlackColor.Black)
                {
                    // Double black node
                    // This null double black and has black height 1
                    // so the sibling tree must be not null with black height 1
                    DoubleBlackFixUp(u);
                }

                // p is always u's parent node even after the fix
                if (u.Parent.Left == u)
                {
                    u.Parent.Left = null;
                }
                else if (u.Parent.Right == u)
                {
                    u.Parent.Right = null;
                }
                else
                {
                    throw new Exception();
                }
            }
            else if (u.Left == null || u.Right == null)
            {
                // case 2: one null child
                if (u.Color != RedBlackColor.Black)
                {
                    throw new Exception();
                }

                if (u.Left == null)
                {
                    // node.Right is not null
                    if (u.Right == null || u.Right.Color != RedBlackColor.Red)
                    {
                        throw new Exception();
                    }

                    Replace(u, u.Right);
                    u.Right.Color = RedBlackColor.Black;
                    u.Right.Left = null;
                    u.Right.Right = null;
                }
                else
                {
                    // node.Left is not null
                    if (u.Left == null || u.Left.Color != RedBlackColor.Red)
                    {
                        throw new Exception();
                    }

                    Replace(u, u.Left);
                    u.Left.Color = RedBlackColor.Black;
                    u.Left.Left = null;
                    u.Left.Right = null;
                }
            }
            else
            {
                // case 3: no null child
                IRedBlackNode<TKey, TValue> sc = u.Right;
                while (sc.Left != null)
                {
                    sc = sc.Left;
                }
                
                RedBlackColor sColor = sc.Color;
                sc.Color = u.Color;
                u.Color = sColor;

                var sr = sc.Right;
                var sp = sc.Parent;

                Replace(u, sc);
                sc.Left = u.Left;
                if (sc.Left != null)
                {
                    sc.Left.Parent = sc;
                }

                if (sc == u.Right)
                {
                    sc.Right = u;
                    u.Parent = sc;
                }
                else
                {
                    sc.Right = u.Right;
                    sc.Right.Parent = sc;

                    u.Parent = sp;
                    sp.Left = u;
                }

                u.Left = null;
                u.Right = sr;
                if (u.Right != null)
                {
                    u.Right.Parent = u;
                }

                Delete(u);
            }
        }
    }
}
```
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
