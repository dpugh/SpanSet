using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System;

namespace SpanSet
{
    public class SpanSet<T>
    {
        public readonly bool DeleteZeroLengthSpans;

        private IReadOnlyList<SpanTree<T>> _trees = SpanTree<T>.Empty;

        public SpanSet(bool deleteZeroLengthSpans)
        {
            this.DeleteZeroLengthSpans = deleteZeroLengthSpans;
        }

        public SpanSet(SpanSet<T> source, IReadOnlyList<SpanTree<T>> trees)
        {
            this.DeleteZeroLengthSpans = source.DeleteZeroLengthSpans;

            _trees = trees;
        }

        public SpanSet<T> Add(Span span, T data)
        {
            if (this.DeleteZeroLengthSpans && (span.Length == 0))
                return this;

            var newTrees = SpanTree<T>.Add(_trees, 0, span, data);
            return new SpanSet<T>(this, newTrees);
        }

        public IEnumerable<SpanAndData<T>> GetAllSpans()
        {
            return SpanTree<T>.GetAllSpans(_trees, 0);
        }

        public IEnumerable<SpanAndData<T>> GetSpansIntersecting(Span span)
        {
            return SpanTree<T>.GetSpansIntersecting(_trees, span, 0);
        }

        internal bool IsValid(int length)
        {
            return SpanTree<T>.IsValid(_trees, length);
        }
    }

    public class SpanTree<T>
    {
        public readonly Span Span;
        public readonly T Data;

        internal static readonly IReadOnlyList<SpanTree<T>> Empty = new SpanTree<T>[0];

        public readonly IReadOnlyList<SpanTree<T>> Children;

        public SpanTree(Span span, T data)
            : this(span, data, Empty)
        {
        }

        protected SpanTree(SpanTree<T> spanTree, IReadOnlyList<SpanTree<T>> children)
            : this(spanTree.Span, spanTree.Data, children)
        {
        }

        internal SpanTree(Span span, T data, IReadOnlyList<SpanTree<T>> children)
        {
            this.Span = span;
            this.Data = data;

            this.Children = children;
        }

        internal static IReadOnlyList<SpanTree<T>> Add(IReadOnlyList<SpanTree<T>> trees, int offset, Span span, T data)
        {
            Assert(span.Start >= offset);
            span = new Span(span.Start - offset, span.Length);

            SpanTree<T>[] newTrees = null;

            var first = FirstStartIndexOnOrAfterPosition(trees, span.Start);
            Assert((first == trees.Count) || (span.Start <= trees[first].Span.Start));
            var last = LastEndIndexBeforeOrOnPosition(trees, first, trees.Count - 1, span.End);
            if (last < first)
            {
                Assert(last + 1 == first);
                // span doesn't contain any existing trees but might be contained by first or last

                if ((first < trees.Count) && (span.Start == trees[first].Span.Start))
                {
                    var tree = trees[first];
                    newTrees = trees.Copy();
                    newTrees[first] = new SpanTree<T>(tree,
                                                        Add(tree.Children, tree.Span.Start,
                                                            span, data));
                }
                else if ((last >= 0) && (span.Start >= trees[last].Span.Start) && (span.End <= trees[last].Span.End))
                {
                    var tree = trees[last];
                    newTrees = trees.Copy();
                    newTrees[last] = new SpanTree<T>(tree,
                                                        Add(tree.Children, tree.Span.Start,
                                                            span, data));
                }
                else
                    newTrees = trees.InsertAt(first, new SpanTree<T>(span, data));
            }
            else
            {
                Assert(span.End >= trees[last].Span.End);

                // trees [first ... last] are contained by the new span.
                // copy those nodes to a sublist which will become the children of the new span.
                var newChildren = new SpanTree<T>[1 + last - first];
                for (int i = first; (i <= last); ++i)
                {
                    var newChild = trees[i];
                    newChildren[i - first] = (span.Start == 0)
                                                ? newChild
                                                : new SpanTree<T>(new Span(newChild.Span.Start - span.Start, newChild.Span.Length), newChild.Data, newChild.Children);
                }

                var newTree = new SpanTree<T>(span, data, newChildren);

                newTrees = new SpanTree<T>[trees.Count - (last - first)];
                trees.CopyTo(0, newTrees, 0, first);
                newTrees[first] = newTree;
                trees.CopyTo(last + 1, newTrees, first + 1, trees.Count - (last + 1));
            }

            return newTrees;
        }

        private IEnumerable<SpanAndData<T>> GetAllSpans(int offset)
        {
            yield return new SpanAndData<T>(new Span(this.Span.Start + offset, this.Span.Length), this.Data);

            foreach (var c in GetAllSpans(this.Children, offset + this.Span.Start))
                yield return c;
        }

        private IEnumerable<SpanAndData<T>> GetSpansIntersecting(Span span, int offset)
        {
            yield return new SpanAndData<T>(new Span(this.Span.Start + offset, this.Span.Length), this.Data);

            foreach (var c in GetSpansIntersecting(this.Children, span, offset + this.Span.Start))
                yield return c;
        }

        internal static IEnumerable<SpanAndData<T>> GetAllSpans(IReadOnlyList<SpanTree<T>> trees, int offset)
        {
            for (int i = 0; (i < trees.Count); ++i)
            {
                var child = trees[i];
                if (child.Children.Count == 0)
                {
                    // Handle childless nodes as a special case (avoid the overhead of the foreach below)
                    yield return new SpanAndData<T>(new Span(child.Span.Start + offset, child.Span.Length), child.Data);
                }
                else
                {
                    foreach (var c in child.GetAllSpans(offset))
                        yield return c;
                }
            }
        }

        internal static IEnumerable<SpanAndData<T>> GetSpansIntersecting(IReadOnlyList<SpanTree<T>> trees, Span span, int offset)
        {
            var first = SpanTree<T>.FirstEndIndexOnOrAfterPosition(trees, span.Start - offset);
            var last = SpanTree<T>.LastStartIndexBeforeOrOnPosition(trees, span.End - offset);

            for (int i = first; (i <= last); ++i)
            {
                var child = trees[i];
                if (child.Children.Count == 0)
                {
                    // Handle childless nodes as a special case (avoid the overhead of the foreach below)
                    yield return new SpanAndData<T>(new Span(child.Span.Start + offset, child.Span.Length), child.Data);
                }
                else
                {
                    foreach (var c in child.GetSpansIntersecting(span, offset))
                        yield return c;
                }
            }
        }

        internal static int FirstEndIndexOnOrAfterPosition(IReadOnlyList<SpanTree<T>> trees, int position)
        {
            return FirstEndIndexOnOrAfterPosition(trees, 0, trees.Count - 1, position);
        }

        internal static int FirstEndIndexOnOrAfterPosition(IReadOnlyList<SpanTree<T>> trees, int lo, int hi, int position)
        {
            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                var p = trees[mid].Span.End;
                if (p >= position)
                    hi = mid - 1;
                else
                    lo = mid + 1;
            }

            return lo;
        }

        internal static int FirstStartIndexOnOrAfterPosition(IReadOnlyList<SpanTree<T>> trees, int position)
        {
            return FirstStartIndexOnOrAfterPosition(trees, 0, trees.Count - 1, position);
        }

        internal static int FirstStartIndexOnOrAfterPosition(IReadOnlyList<SpanTree<T>> trees, int lo, int hi, int position)
        {
            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                var p = trees[mid].Span.Start;
                if (p >= position)
                    hi = mid - 1;
                else
                    lo = mid + 1;
            }

            return lo;
        }

        internal static int LastStartIndexBeforeOrOnPosition(IReadOnlyList<SpanTree<T>> trees, int position)
        {
            return LastStartIndexBeforeOrOnPosition(trees, 0, trees.Count - 1, position);
        }

        internal static int LastStartIndexBeforeOrOnPosition(IReadOnlyList<SpanTree<T>> trees, int lo, int hi, int position)
        {
            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                var p = trees[mid].Span.Start;
                if (p > position)
                    hi = mid - 1;
                else
                    lo = mid + 1;
            }

            return hi;
        }

        internal static int LastEndIndexBeforeOrOnPosition(IReadOnlyList<SpanTree<T>> trees, int position)
        {
            return LastEndIndexBeforeOrOnPosition(trees, 0, trees.Count - 1, position);
        }

        internal static int LastEndIndexBeforeOrOnPosition(IReadOnlyList<SpanTree<T>> trees, int lo, int hi, int position)
        {
            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                var p = trees[mid].Span.End;
                if (p > position)
                    hi = mid - 1;
                else
                    lo = mid + 1;
            }

            return hi;
        }


        public override string ToString()
        {
            return string.Format("{0}:[{1},{2})", this.Data, this.Span.Start, this.Span.End);
        }

        internal static bool IsValid(IReadOnlyList<SpanTree<T>> trees, int length)
        {
            if (trees.Count > 0)
            {
                for (int i = 1; (i < trees.Count); ++i)
                {
                    if (trees[i - 1].Span.Start >= trees[i].Span.Start)
                        return false;
                    if (trees[i - 1].Span.End >= trees[i].Span.End)
                        return false;
                }

                if (trees[trees.Count - 1].Span.End > length)
                    return false;

                for (int i = 0; (i < trees.Count); ++i)
                {
                    if (!IsValid(trees[i].Children, trees[i].Span.Length))
                        return false;
                }
            }

            return true;
        }

        [Conditional("DEBUG")]
        public static void AssertDTI(IReadOnlyList<SpanTree<T>> trees, int length)
        {
            Assert(IsValid(trees, length));
        }

        [Conditional("DEBUG")]
        public static void Assert(bool condition)
        {
            if (!condition)
            {
                Debugger.Break();
                throw new InvalidOperationException();
            }
        }
    }

    public static class Extensions
    {
        public static void CopyTo<T>(this IReadOnlyList<T> source, int index, T[] destination, int arrayIndex, int count)
        {
            for (int i = 0; (i < count); ++i)
                destination[i + arrayIndex] = source[i + index];
        }

        public static T[] Copy<T>(this IReadOnlyList<T> source)
        {
            T[] destination = new T[source.Count];

            for (int i = 0; (i < source.Count); ++i)
                destination[i] = source[i];

            return destination;
        }

        public static T[] InsertAt<T>(this IReadOnlyList<T> source, int index, T newElement)
        {
            T[] destination = new T[source.Count + 1];

            source.CopyTo(0, destination, 0, index);
            destination[index] = newElement;
            source.CopyTo(index, destination, index + 1, source.Count - index);

            return destination;
        }
    }

    public struct SpanAndData<T>
    {
        public readonly Span Span;
        public readonly T Data;

        public SpanAndData(Span span, T data)
        {
            this.Span = span;
            this.Data = data;
        }

        public override string ToString()
        {
            return string.Format("{0}:[{1},{2})", this.Data, this.Span.Start, this.Span.End);
        }
    }
}
