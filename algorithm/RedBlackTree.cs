using System;

namespace BalancedTree
{
    public enum RedBlackColor
    {
        Red,
        Black,
    }

    public interface IBalancedTreeNode<TKey, TValue>
        where TKey : IComparable<TKey>
    {
        public IRedBlackNode<TKey, TValue> Parent { get; set; }
        public IRedBlackNode<TKey, TValue> Left { get; set; }
        public IRedBlackNode<TKey, TValue> Right { get; set; }
        public TKey Key { get; set; }
        public TValue Value { get; set; }
    }

    public interface IRedBlackNode<TKey, TValue> : IBalancedTreeNode<TKey, TValue>
        where TKey : IComparable<TKey>
    {
        public RedBlackColor Color { get; set; }
    }

    public class RedBlackTree<TKey, TValue>
        where TKey : IComparable<TKey>
    {
        private static Action<IRedBlackNode<TKey, TValue>, IRedBlackNode<TKey, TValue>> setLeft = 
            (x, y) => { x.Left = y; if (y != null) y.Parent = x; };
        private static Action<IRedBlackNode<TKey, TValue>, IRedBlackNode<TKey, TValue>> setRight = 
            (x, y) => { x.Right = y; if (y != null) y.Parent = x; };
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
                    setLeft(a.Parent, b);
                }
                else if (a.Parent.Right == a)
                {
                    setRight(a.Parent, b);
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
                    setRight(p, g);
                    setLeft(g, a);
                    return p;
                }
                else if (p.Right == n)
                {
                    var a = n.Left;
                    var b = n.Right;
                    Replace(g, n);
                    setLeft(n, p);
                    setRight(n, g);
                    setRight(p, a);
                    setLeft(g, b);
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
                    setLeft(n, g);
                    setRight(n, p);
                    setRight(g, a);
                    setLeft(p, b);
                    return n;
                }
                else if (p.Right == n)
                {
                    var a = p.Left;
                    Replace(g, p);
                    setLeft(p, g);
                    setRight(g, a);
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
                        setRight(p, u);
                        break;
                    }
                    p = p.Right;
                }
                else
                {
                    if (p.Left == null)
                    {
                        setLeft(p, u);
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
                setLeft(sc, u.Left);

                if (sc == u.Right)
                {
                    setRight(sc, u);
                }
                else
                {
                    setRight(sc, u.Right);

                    u.Parent = sp;
                    sp.Left = u;
                }

                u.Left = null;
                setRight(u, sr);

                Delete(u);
            }
        }
    }
}