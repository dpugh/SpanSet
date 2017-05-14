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
            Assert(span.Start >= offset);
            span = new Span(span.Start - offset, span.Length);

            SpanTree<T>[] newTrees = null;

            // We're interested in overlapping spans so offset the endpoints
            int firstOverlappedIndex = FirstEndIndexOnOrAfterPosition(trees, span.Start + 1);
            int lastOverlappedIndex = LastStartIndexBeforeOrOnPosition(trees, span.End - 1);

            if (lastOverlappedIndex < firstOverlappedIndex)
            {
                // The new span falls into the gap between firstOverlappedIndex and lastOverlappedIndex
                Assert(lastOverlappedIndex + 1 == firstOverlappedIndex);
                newTrees = trees.InsertAt(firstOverlappedIndex, new SpanTree<T>(new Span(span.Start, span.Length), data));
            }
            else
            {
                // the new span overlaps one or more existing spans
                Assert((firstOverlappedIndex >= 0) && (firstOverlappedIndex < trees.Count));
                Assert((lastOverlappedIndex >= 0) && (lastOverlappedIndex < trees.Count));

                int firstContainedStartIndex = FirstStartIndexOnOrAfterPosition(trees, firstOverlappedIndex, lastOverlappedIndex, span.Start);
                int lastContainedEndIndex = LastEndIndexBeforeOrOnPosition(trees, firstOverlappedIndex, lastOverlappedIndex, span.End);

                if (lastContainedEndIndex < firstContainedStartIndex)
                {
                    var tree = trees[firstOverlappedIndex];
                    if (span.Start < tree.Span.Start)
                    {
                        newTrees = trees.InsertAt(firstOverlappedIndex, new SpanTree<T>(span, data));
                    }
                    else if (span.End > tree.Span.End)
                    {
                        newTrees = trees.InsertAt(firstOverlappedIndex + 1, new SpanTree<T>(span, data));
                    }
                    else
                    {
                        // the new span is contained by an existing lastPossiblyContainedIndex or starts before it.
                        var newChildren = Add(tree.Children, tree.Span.Start, span, data);

                        newTrees = trees.Copy();
                        newTrees[firstOverlappedIndex] = new SpanTree<T>(tree, newChildren);
                    }
                }
                else
                {
                    // nodes [firstContainedStartIndex ... lastContainedEndIndex] are contained by the new span.
                    var newChildren = new SpanTree<T>[1 + lastContainedEndIndex - firstContainedStartIndex];
                    for (int i = firstContainedStartIndex; (i <= lastContainedEndIndex); ++i)
                    {
                        var tree = trees[i - firstContainedStartIndex];
                        newChildren[i - firstContainedStartIndex] = (span.Start == 0) ? tree : new SpanTree<T>(new Span(tree.Span.Start - span.Start, tree.Span.Length), tree.Data, tree.Children);
                    }

                    var newTree = new SpanTree<T>(new Span(span.Start, span.Length), data, newChildren);

                    newTrees = new SpanTree<T>[trees.Count - (lastContainedEndIndex - firstContainedStartIndex)];
                    trees.CopyTo(0, newTrees, 0, firstContainedStartIndex);
                    newTrees[firstContainedStartIndex] = newTree;
                    trees.CopyTo(lastContainedEndIndex + 1, newTrees, firstContainedStartIndex + 1, trees.Count - (lastContainedEndIndex + 1));
                }
            }

            AssertDTI(newTrees, int.MaxValue);
            return newTrees;
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

        [Conditional("DEBUG")]
        internal static void AssertDTI(IReadOnlyList<SpanTree<T>> trees, int length)
        {
            if (trees.Count > 0)
            {
                for (int i = 1; (i < trees.Count); ++i)
                {
                    Assert(trees[i - 1].Span.Start < trees[i].Span.Start);
                    Assert(trees[i - 1].Span.End < trees[i].Span.End);
                }

                Assert(trees[trees.Count - 1].Span.End < length);

                for (int i = 0; (i < trees.Count); ++i)
                {
                    AssertDTI(trees[i].Children, trees[i].Span.Length);
                }
            }
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
