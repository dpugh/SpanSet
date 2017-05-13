using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.Diagnostics;

namespace SpanSet
{

    public class SpanSet<T>
    {
        public readonly SpanTrackingMode Mode;
        public readonly bool DeleteZeroLengthSpans;

        private IReadOnlyList<SpanTree<T>> _trees = SpanTree<T>.Empty;

        public SpanSet(SpanTrackingMode mode, bool deleteZeroLengthSpans)
        {
            this.Mode = mode;
            this.DeleteZeroLengthSpans = deleteZeroLengthSpans;
        }

        public SpanSet(SpanSet<T> source, IReadOnlyList<SpanTree<T>> trees)
        {
            this.Mode = source.Mode;
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
            SpanTree<T>[] newTrees = null;

            // We're interested in overlapping spans so offset the endpoints
            int firstIndex = FirstEndIndexOnOrAfterPosition(trees, span.Start + 1 - offset);
            int lastIndex = LastStartIndexBeforeOrOnPosition(trees, span.End - 1 - offset);

            if (lastIndex < firstIndex)
            {
                // The new span falls into the gap between firstIndex and lastIndex
                Assert(lastIndex + 1 == firstIndex);
                newTrees = trees.InsertAt(firstIndex, new SpanTree<T>(new Span(span.Start - offset, span.Length), data));
            }
            else
            {
                // the new span overlaps or more existing spans
                Assert((firstIndex >= 0) && (firstIndex < trees.Count));
                Assert((lastIndex >= 0) && (lastIndex < trees.Count));

                int firstPossiblyContainedIndex = (span.Start <= trees[firstIndex].Span.Start + offset) ? firstIndex : (firstIndex + 1);
                int lastPossiblyContainedIndex = (span.End >= trees[lastIndex].Span.End + offset) ? lastIndex : (lastIndex - 1);


                if (lastPossiblyContainedIndex < firstPossiblyContainedIndex)
                {
                    // the new span is contained by an existing lastPossiblyContainedIndex
                    var tree = trees[firstIndex];
                    var newChildren = Add(tree.Children, offset + tree.Span.Start, span, data);

                    newTrees = trees.Copy();
                    newTrees[firstIndex] = new SpanTree<T>(tree, newChildren);
                }
                else
                {
                    // the new span contains zero or more existing nodes.
                }


                /*
                if (span.Start > trees[firstIndex].Span.Start + offset)
                {
                    // new span starts after start of the span it overlaps.
                    if (span.End > trees[firstIndex].Span.End + offset)
                    {
                        // and ends afterwards too ... so the two spans partially overlap and must be added to trees
                        newTrees = trees.InsertAt(firstIndex + 1, new SpanTree<T>(new Span(span.Start - offset, span.Length), data));
                    }
                    else
                    {
                        // the new span ends before or at the end of the span it overlaps so it gets contained with it.
                        var tree = trees[firstIndex];
                        var newChildren = Add(tree.Children, offset + tree.Span.Start, span, data);

                        newTrees = trees.Copy();
                        newTrees[firstIndex] = new SpanTree<T>(tree, newChildren);
                    }
                }
                else
                {
                    // new span starts before the start of the existing span.
                    if (span.End < trees[firstIndex].Span.End + offset)
                    {
                        // new span ends before the end of the existing span, insert the new span before the existing span.
                        newTrees = trees.InsertAt(firstIndex, new SpanTree<T>(new Span(span.Start - offset, span.Length), data));
                    }
                    else
                    {
                        // new span ends on or after the existing span. move existing span into the new space.
                        var tree = trees[firstIndex];

                        var newChildren = Add(tree.Children, offset + tree.Span.Start, span, data);


                        newTrees = trees.Copy();
                        newTrees[firstIndex] = new SpanTree<T>(tree, newChildren);
                    }
                }
                */
            }

            return newTrees;
        }

        internal static int FirstEndIndexOnOrAfterPosition(IReadOnlyList<SpanTree<T>> trees, int position)
        {
            int lo = 0;
            int hi = trees.Count - 1;
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

        internal static int LastStartIndexBeforeOrOnPosition(IReadOnlyList<SpanTree<T>> trees, int position)
        {
            int lo = 0;
            int hi = trees.Count - 1;
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

        public override string ToString()
        {
            return string.Format("{0}:[{1},{2})", this.Data, this.Span.Start, this.Span.End);
        }

        [Conditional("DEBUG")]
        public static void Assert(bool condition)
        {
            if (!condition)
            {
                Debugger.Break();
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
    }
}
