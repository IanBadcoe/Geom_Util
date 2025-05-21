using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Geom_Util.Interfaces;

using Geom_Util.Immutable;

using Godot_Util;

namespace Geom_Util
{
    public interface IReadOnlyRTree
    {
        public enum SearchMode
        {
            ContainedWithin,    //< find leaves whose bounds are contained within the search-bounds (identity counts)
            Contains,           //< find leaves whose bounds contain the search-bounds
            Overlaps,           //< find leaves whose bounds overlap the search-bounds
            ExactMatch          //< find leaves with identical bounds to the search-bounds
        }
    }

    public interface IReadOnlyRTree<T> : IReadOnlyRTree, IEnumerable<T>
    {
        ImBounds GetBounds();
        IEnumerable<T> Search(ImBounds search_bounds, SearchMode mode);
    }

    public class RTree<T> : IReadOnlyRTree<T>
        where T : class, IBounded
    {
        const int MinChildren = 4;
        const int MaxChildren = MinChildren * 2 - 1;

        class Node
        {
            public Node Parent { get; private set; }
            public List<Node> Children = null;
            public T Item;
            ImBounds Bounds = new ImBounds();
            public int Level { get; private set; } = 0;

            public bool IsDirty { get; private set; } = false;                       // when Bound needs recalculation

            public Node()
            {
                // empty tree
            }

            public Node(T item)
            {
                Item = item;
                IsDirty = true;
            }

            public Node(List<Node> nodes)
            {
                SetChildren(nodes);
            }

            public ImBounds GetBounds()
            {
                if (IsDirty)
                {
                    RefreshBound();
                }

                return Bounds;
            }

            public bool IsValid => Item == null || Children == null;

            public bool IsEmpty => Item == null && Children == null;

            public bool IsLeaf => Item != null;

            public void SetParent(Node node)
            {
                Parent = node;

                if (Parent != null)
                {
                    Parent.Level = Level + 1;
                }
            }

            public void SetChildren(List<Node> nodes)
            {
                Children = nodes;

                foreach (var child in Children)
                {
                    child.SetParent(this);
                }

                SetIsDirty();
            }

            public void SetIsDirty()
            {
                var n = this;

                while (n != null)
                {
                    if (!n.IsDirty)
                    {
                        n.IsDirty = true;
                        n = n.Parent;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            void RefreshBound()
            {
                Bounds = RecalculateBound();

                IsDirty = false;
            }

            ImBounds RecalculateBound(bool refreshing_children = true)
            {
                if (Item != null)
                {
                    return Item.GetBounds();
                }
                else if (Children != null)
                {
                    // for unit-testing, we do not want to fix anything that might be wrong with a child, so we just peak the recalculated
                    // bound, but for real use, if we see a dirty child we refresh them too
                    if (refreshing_children)
                    {
                        // normal
                        return Children.Aggregate(new ImBounds(), (b, c) => b.UnionedWith(c.GetBounds()));
                    }
                    else
                    {
                        // unit-test
                        return Children.Aggregate(new ImBounds(), (b, c) => b.UnionedWith(c.RecalculateBound(false)));
                    }
                }

                IsDirty = false;
                return new ImBounds();
            }

            internal bool ValidateBounds()
            {
                // if we know we need refreshing, nothing to check
                if (IsDirty)
                {
                    return true;
                }

                var check_bound = RecalculateBound(false);

                return Bounds.Equals(check_bound);
            }
        }

        Node Root = new Node();

        public RTree()
        {
            // nothing, initialisers give us an empty tree
        }

        #region IEnumerable
        public IEnumerator<T> GetEnumerator()
        {
            return Enumerate().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Enumerate().GetEnumerator();
        }
        #endregion

        public ImBounds GetBounds()
        {
            return Root.GetBounds();
        }

        public IEnumerable<T> Enumerate()
        {
            foreach (Node node in EnumerateNodes(Root, false, false, true))
            {
                yield return node.Item;
            }
        }

        public IEnumerable<T> Search(ImBounds b, IReadOnlyRTree.SearchMode mode)
        {
            foreach (Node n in Search(Root, b, mode))
            {
                yield return n.Item;
            }
        }

        public void Insert(T item)
        {
            if (Root.IsEmpty)
            {
                Root.Item = item;
                Root.SetIsDirty();
                return;
            }

            var item_node = new Node(item);

            if (Root.IsLeaf)
            {
                Root = new Node(new List<Node> { item_node, Root });
            }
            else
            {
                // we are inserting a new leaf (Level == 0) so it goes in level 1
                var new_node = InsertNode(Root, item_node, 1);

                if (new_node != null)
                {
                    Root = new Node(new List<Node> { Root, new_node });
                }
            }
        }

        public void Remove(T t)
        {
            Node t_n = null;
            foreach (Node n in Search(Root, t.GetBounds(), IReadOnlyRTree.SearchMode.ExactMatch))
            {
                if (ReferenceEquals(n.Item, t))
                {
                    t_n = n;
                    break;
                }
            }

            // uniquely a single item tree does not remove the Root node, it just sets it empty...
            if (t_n == Root)
            {
                Root.Item = null;
                Root.SetIsDirty();
            }
            else
            {
                RemoveFrom(t_n.Parent, t_n);
            }
        }

        public bool IsValid()
        {
            foreach (Node n in EnumerateNodes(true, true, true))
            {
                if (!n.IsValid)
                {
                    return false;
                }

                if (Root == n && n.Parent != null)
                {
                    return false;
                }

                if (Root != n)
                {
                    if (n.Parent == null)
                    {
                        return false;
                    }

                    if (n.Level != n.Parent.Level - 1)
                    {
                        return false;
                    }
                }

                if (n.Children != null)
                {
                    if (n.Children.Count > MaxChildren)
                    {
                        return false;
                    }

                    // root node is allowed < MinChildren as that is how we accommodate small trees
                    if (n != Root && n.Children.Count < MinChildren)
                    {
                        return false;
                    }
                }

                if (n.IsLeaf)
                {
                    if (n.Level != 0)
                    {
                        return false;
                    }
                }

                if (!n.ValidateBounds())
                {
                    return false;
                }
            }

            return true;
        }

        IEnumerable<Node> EnumerateNodes(bool include_empty, bool include_internal, bool include_leaves)
        {
            return EnumerateNodes(Root, include_empty, include_internal, include_leaves);
        }

        IEnumerable<Node> EnumerateNodes(Node node, bool include_empty, bool include_internal, bool include_leaves)
        {
            // empty node is only ever the root, if we hit it and we did not want to return it
            // we are done...
            if (node.IsEmpty && include_empty)
            {
                yield return node;
            }
            else if (node.IsLeaf && include_leaves)
            {
                yield return node;
            }
            else if (include_internal)
            {
                yield return node;
            }

            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    foreach (Node child_node in EnumerateNodes(child, include_empty, include_internal, include_leaves))
                    {
                        yield return child_node;
                    }
                }
            }
        }

        private static bool SearchCondition(ImBounds search_bounds, Node node, IReadOnlyRTree.SearchMode mode)
        {
            if (node.IsLeaf)
            {
                switch(mode)
                {
                    case IReadOnlyRTree.SearchMode.ContainedWithin:
                        return search_bounds.Contains(node.GetBounds());
                    case IReadOnlyRTree.SearchMode.Contains:
                        return node.GetBounds().Contains(search_bounds);
                    case IReadOnlyRTree.SearchMode.Overlaps:
                        return search_bounds.Overlaps(node.GetBounds());
                    case IReadOnlyRTree.SearchMode.ExactMatch:
                        return search_bounds.Equals(node.GetBounds());
                }
            }

            // intrernal nodes
            switch(mode)
            {
                case IReadOnlyRTree.SearchMode.ContainedWithin:
                case IReadOnlyRTree.SearchMode.Overlaps:
                    // to find leaves which are either contained withing the bounds, or overlap the search-bounds, we
                    // need to look inside nodes which overlap the search-bounds
                    return search_bounds.Overlaps(node.GetBounds());
                case IReadOnlyRTree.SearchMode.Contains:
                case IReadOnlyRTree.SearchMode.ExactMatch:
                    // to find leaves which either contain the search-bounds, or are identical to them, we need to look inside
                    // notes which contain the search-bounds
                    return node.GetBounds().Contains(search_bounds);
            }

            Util.Assert(false);

            return false;
        }

        IEnumerable<Node> Search(Node node, ImBounds b, IReadOnlyRTree.SearchMode mode)
        {
            if (node.IsEmpty)
                yield break;

            if (SearchCondition(b, node, mode))
            {
                if (node.IsLeaf)
                {
                    yield return node;
                }
                else
                {
                    foreach (var child in node.Children)
                    {
                        foreach (Node n in Search(child, b, mode))
                        {
                            yield return n;
                        }
                    }
                }
            }
        }

        Node InsertNode(Node search_node, Node insert_node, int level)
        {
            Node insert_here_node;

            if (search_node.Level == level)
            {
                // we reached the penultimate level, insert into this node
                insert_here_node = insert_node;
            }
            else
            {
                // recurse into children and get any (split) node to insert here back
                var search_further_node = ChooseNode(search_node.Children, insert_node);

                insert_here_node = InsertNode(search_further_node, insert_node, level);
            }

            if (insert_here_node == null)
                return null;

            if (search_node.Children.Count < MaxChildren)
            {
                search_node.Children.Add(insert_here_node);
                insert_here_node.SetParent(search_node);
                search_node.SetIsDirty();

                return null;
            }

            return SplitNode(search_node, insert_here_node);
        }

        Node SplitNode(Node node, Node new_node)
        {
            var all_nodes = node.Children.Append(new_node).ToList();

            Tuple<Node, Node> extremes = FindExtremes(all_nodes);

            List<Node> set1 = new List<Node> { extremes.Item1 };
            List<Node> set2 = new List<Node> { extremes.Item2 };

            ImBounds bound1 = extremes.Item1.GetBounds();
            ImBounds bound2 = extremes.Item2.GetBounds();

            float volume1 = bound1.Volume + 1;
            float volume2 = bound2.Volume + 1;

            foreach (var child in all_nodes.Where(n => n != extremes.Item1 && n != extremes.Item2))
            {
                if (set1.Count == MinChildren)
                {
                    set2.Add(child);
                }
                else if (set2.Count == MinChildren)
                {
                    set1.Add(child);
                }
                else
                {
                    ImBounds new_bound1 = bound1.UnionedWith(child.GetBounds());
                    ImBounds new_bound2 = bound2.UnionedWith(child.GetBounds());

                    float new_volume1 = new_bound1.Volume + 1;
                    float new_volume2 = new_bound2.Volume + 1;

                    bool choose1 = false;

                    // if adding it to set 1 increases the total volume by less than adding it to set 2
                    if (new_volume1 + volume2 < new_volume2 + volume1)
                    {
                        choose1 = true;
                    }
                    // if adding it to set 1 increases the total volume by mode than adding it to set 2
                    else if (new_volume1 + volume2 > new_volume2 + volume1)
                    {
                        choose1 = false;
                    }
                    // otherwise if the change in volume is the same, go for the one which leads to the smaller new volume
                    else if (new_volume1 < new_volume2)
                    {
                        choose1 = true;
                    }
                    // and if that was a draw too, we do not care...
                    else
                    {
                        choose1 = false;
                    }

                    if (choose1)
                    {
                        set1.Add(child);
                        bound1 = new_bound1;
                        volume1 = new_volume1;
                    }
                    else
                    {
                        set2.Add(child);
                        bound2 = new_bound2;
                        volume2 = new_volume2;
                    }
                }
            }

            // put half the children in the original node and return a node containing the other half for insertion into
            // our parent
            node.SetChildren(set1);

            return new Node(set2);
        }

        Tuple<Node, Node> FindExtremes(List<Node> nodes)
        {
            // max_x will be the highest, _minimum_ x on a node's bounds
            // and min_x will be the lowest _maximum_ x
            // and that finds the bounds with the highest separation
            // (and cannot find the same node twice)

            float max_x = float.MinValue;
            float max_y = float.MinValue;
            float max_z = float.MinValue;

            float min_x = float.MaxValue;
            float min_y = float.MaxValue;
            float min_z = float.MaxValue;

            Node max_x_node = null;
            Node max_y_node = null;
            Node max_z_node = null;

            Node min_x_node = null;
            Node min_y_node = null;
            Node min_z_node = null;

            foreach (var node in nodes)
            {
                ImBounds b = node.GetBounds();

                if (max_x < b.Min.X)
                {
                    max_x = b.Min.X;
                    max_x_node = node;
                }

                if (max_y < b.Min.Y)
                {
                    max_y = b.Min.Y;
                    max_y_node = node;
                }

                if (max_z < b.Min.Z)
                {
                    max_z = b.Min.Z;
                    max_z_node = node;
                }

                if (min_x > b.Max.X)
                {
                    min_x = b.Max.X;
                    min_x_node = node;
                }

                if (min_y > b.Max.Y)
                {
                    min_y = b.Max.Y;
                    min_y_node = node;
                }

                if (min_z > b.Max.Z)
                {
                    min_z = b.Max.Z;
                    min_z_node = node;
                }
            }

            var ret = new Tuple<Node, Node>(min_x_node, max_x_node);
            var range = max_x - min_x;
            float range_y = max_y - min_y;
            float range_z = max_z - min_z;

            if (range_y > range)
            {
                ret = new Tuple<Node, Node>(min_y_node, max_y_node);
                range = range_y;
            }

            if (range_z > range)
            {
                ret = new Tuple<Node, Node>(min_z_node, max_z_node);
            }

            // if all 8 items were identical, this happens
            if (ret.Item1 == ret.Item2)
            {
                // and it really does not matter what we return...
                ret = new Tuple<Node, Node>(nodes[0], nodes[1]);
            }

            return ret;
        }

        Node ChooseNode(List<Node> children, Node insert_leaf_node)
        {
            Node chosen = children[0];
            float chosen_growth_ratio = 1;
            float chosen_volume = 0;

            // the +1s allow us to divide, even if the original volume was zero
            // without changing which ratios are bigger that each other...
            float new_volume = chosen.GetBounds().UnionedWith(insert_leaf_node.GetBounds()).Volume + 1;
            float old_volume = chosen.GetBounds().Volume + 1;

            chosen_growth_ratio = new_volume / old_volume;
            if (chosen_growth_ratio == 1)
            {
                chosen_volume = new_volume;
            }

            foreach (var child in children.Skip(1))
            {
                new_volume = child.GetBounds().UnionedWith(insert_leaf_node.GetBounds()).Volume + 1;

                if (chosen_volume != 0 && new_volume < chosen_volume)
                {
                    chosen_volume = new_volume;
                    chosen = child;
                    continue;
                }

                old_volume = child.GetBounds().Volume + 1;

                float growth_ratio = new_volume / old_volume;
                if (growth_ratio < chosen_growth_ratio)
                {
                    chosen_growth_ratio = growth_ratio;
                    chosen = child;

                    if (chosen_growth_ratio == 1)
                    {
                        chosen_volume = new_volume;
                    }
                }
            }

            return chosen;
        }

        void RemoveFrom(Node remove_from, Node node)
        {
            Godot_Util.Util.Assert(remove_from != null, "Removing from what-now?");
            Godot_Util.Util.Assert(remove_from.Children != null, "Removing from node with no Children list...");
            Godot_Util.Util.Assert(remove_from.Children.Count > 0, "Removing from node with empty Children list...");
            remove_from.Children.Remove(node);
            remove_from.SetIsDirty();

            if (remove_from == Root)
            {
                if (remove_from.Children.Count == 1)
                {
                    // if the root is not required anymore, shorten the tree
                    Root = remove_from.Children[0];
                    Root.SetParent(null);
                }
            }
            else if (remove_from.Children.Count < MinChildren)
            {
                RemoveFrom(remove_from.Parent, remove_from);

                // reinsert the children of the removed node at the same level then came from...
                foreach (Node child in remove_from.Children)
                {
                    var new_node = InsertNode(Root, child, remove_from.Level);

                    if (new_node != null)
                    {
                        // can this happen? we are making the tree shorter, but I maybe we could end up filling one branch by more
                        // and need to put back a level of root we just thought we could remove?
                        Root = new Node(new List<Node> { Root, new_node });
                    }
                }
            }
        }
    }
}
