using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using System.Collections.Generic;

namespace SpanSet.Tests
{
    [TestClass()]
    public class SpanForestTests
    {
        [TestMethod()]
        public void SpanTreesAddSimpleInterior()
        {
            var trees = new SpanTree<int>[]
                            { new SpanTree<int>(Span.FromBounds(2, 4), 1),
                              new SpanTree<int>(Span.FromBounds(7, 9), 2),
                              new SpanTree<int>(Span.FromBounds(8, 10), 3)};
            var expected = new int[] { 1, -1, 2, 3 };

            var newTrees1 = SpanTree<int>.Add(trees, 0, Span.FromBounds(4, 7), -1);
            AssertExpected(expected, newTrees1);

            var newTrees2 = SpanTree<int>.Add(trees, 0, Span.FromBounds(5, 7), -1);
            AssertExpected(expected, newTrees2);

            var newTrees3 = SpanTree<int>.Add(trees, 0, Span.FromBounds(4, 6), -1);
            AssertExpected(expected, newTrees3);


            var newTrees4 = SpanTree<int>.Add(trees, 0, Span.FromBounds(5, 6), -1);
            AssertExpected(expected, newTrees4);
        }

        [TestMethod()]
        public void SpanTreesAddSimplePrefix()
        {
            var trees = new SpanTree<int>[]
                            { new SpanTree<int>(Span.FromBounds(2, 4), 1),
                              new SpanTree<int>(Span.FromBounds(7, 9), 2),
                              new SpanTree<int>(Span.FromBounds(8, 10), 3)};
            var expected = new int[] { -1, 1, 2, 3 };

            var newTrees1 = SpanTree<int>.Add(trees, 0, Span.FromBounds(0, 2), -1);
            AssertExpected(expected, newTrees1);

            var newTrees2 = SpanTree<int>.Add(trees, 0, Span.FromBounds(0, 1), -1);
            AssertExpected(expected, newTrees2);
        }

        [TestMethod()]
        public void SpanTreesAddSimpleSuffix()
        {
            var trees = new SpanTree<int>[]
                            { new SpanTree<int>(Span.FromBounds(2, 4), 1),
                              new SpanTree<int>(Span.FromBounds(7, 9), 2),
                              new SpanTree<int>(Span.FromBounds(8, 10), 3)};
            var expected = new int[] { 1, 2, 3, -1 };

            var newTrees1 = SpanTree<int>.Add(trees, 0, Span.FromBounds(10, 12), -1);
            AssertExpected(expected, newTrees1);

            var newTrees2 = SpanTree<int>.Add(trees, 0, Span.FromBounds(11, 12), -1);
            AssertExpected(expected, newTrees2);
        }

        [TestMethod()]
        public void SpanTreesAddSimpleTrailingOverlap()
        {
            var trees = new SpanTree<int>[]
                            { new SpanTree<int>(Span.FromBounds(2, 4), 1),
                              new SpanTree<int>(Span.FromBounds(7, 9), 2),
                              new SpanTree<int>(Span.FromBounds(8, 10), 3)};
            var expected = new int[] { 1, -1, 2, 3 };

            var newTrees = SpanTree<int>.Add(trees, 0, Span.FromBounds(3, 5), -1);
            AssertExpected(expected, newTrees);
        }

        [TestMethod()]
        public void SpanTreesAddSimpleLeadingOverlap()
        {
            var trees = new SpanTree<int>[]
                            { new SpanTree<int>(Span.FromBounds(2, 4), 1),
                              new SpanTree<int>(Span.FromBounds(7, 9), 2)
                            };
            var expected = new int[] { 1, -1, 2 };

            var newTrees = SpanTree<int>.Add(trees, 0, Span.FromBounds(6, 8), -1);
            AssertExpected(expected, newTrees);
        }

        [TestMethod()]
        public void SpanTreesAddSimpleContainedByExisting()
        {
            var trees = new SpanTree<int>[]
                            { new SpanTree<int>(Span.FromBounds(2, 5), 1),
                              new SpanTree<int>(Span.FromBounds(7, 9), 2),
                              new SpanTree<int>(Span.FromBounds(8, 10), 3)};
            var expected = new int[] { 1, 2, 3 };
            var expectedChildren1 = new SpanTree<int>[] { new SpanTree<int>(Span.FromBounds(1, 2), -1) };
            var expectedChildren2 = new SpanTree<int>[] { new SpanTree<int>(Span.FromBounds(1, 3), -1) };
            var expectedChildren3 = new SpanTree<int>[] { new SpanTree<int>(Span.FromBounds(0, 2), -1) };

            var newTrees1 = SpanTree<int>.Add(trees, 0, Span.FromBounds(3, 4), -1);
            AssertExpected(expected, newTrees1);
            AssertExpected(expectedChildren1, newTrees1[0].Children);

            var newTrees2 = SpanTree<int>.Add(trees, 0, Span.FromBounds(3, 5), -1);
            AssertExpected(expected, newTrees2);
            AssertExpected(expectedChildren2, newTrees2[0].Children);

            var newTrees3 = SpanTree<int>.Add(trees, 0, Span.FromBounds(2, 4), -1);
            AssertExpected(expected, newTrees3);
            AssertExpected(expectedChildren3, newTrees3[0].Children);
        }

        [TestMethod()]
        public void SpanTreesAddSimpleContainExisting()
        {
            var trees = new SpanTree<int>[]
                            { new SpanTree<int>(Span.FromBounds(2, 5), 1),
                              new SpanTree<int>(Span.FromBounds(7, 9), 2),
                              new SpanTree<int>(Span.FromBounds(8, 10), 3)};
            var expected = new int[] { -1, 2, 3 };
            var expectedChildren1 = new SpanTree<int>[] { new SpanTree<int>(Span.FromBounds(0, 3), 1) };
            var expectedChildren2 = new SpanTree<int>[] { new SpanTree<int>(Span.FromBounds(1, 3), -1) };
            var expectedChildren3 = new SpanTree<int>[] { new SpanTree<int>(Span.FromBounds(0, 2), -1) };

            var t1 = SpanTree<int>.Add(trees, 0, Span.FromBounds(2, 5), -1);
            var t2 = SpanTree<int>.Add(trees, 0, Span.FromBounds(3, 6), -1);

            var newTrees1 = SpanTree<int>.Add(trees, 0, Span.FromBounds(2, 5), -1);
            AssertExpected(expected, newTrees1);
            AssertExpected(expectedChildren1, newTrees1[0].Children);

            var newTrees2 = SpanTree<int>.Add(trees, 0, Span.FromBounds(3, 6), -1);
            AssertExpected(expected, newTrees2);
            AssertExpected(expectedChildren2, newTrees2[0].Children);

            var newTrees3 = SpanTree<int>.Add(trees, 0, Span.FromBounds(2, 4), -1);
            AssertExpected(expected, newTrees3);
            AssertExpected(expectedChildren3, newTrees3[0].Children);
        }


        private static void AssertExpected<T>(IReadOnlyList<T> expected,
                                              IReadOnlyList<SpanTree<T>> trees,
                                              string label = null)
        {
            Assert.AreEqual(expected.Count, trees.Count,
                               "{0}: Count Expected: {1} Actual: {1}",
                               label ?? string.Empty, expected.Count, trees.Count);

            for (int i = 0; (i < expected.Count); ++i)
            {
                Assert.AreEqual(expected[i], trees[i].Data,
                               "{0}: Index {1} Expected: {2} Actual: {3}",
                               label ?? string.Empty, i, expected[i], trees[i]);
            }
        }

        private static void AssertExpected<T>(IReadOnlyList<SpanTree<T>> expected,
                                              IReadOnlyList<SpanTree<T>> trees,
                                              string label = null)
        {
            Assert.AreEqual(expected.Count, trees.Count,
                               "{0}: Count Expected: {1} Actual: {1}",
                               label ?? string.Empty, expected.Count, trees.Count);

            for (int i = 0; (i < expected.Count); ++i)
            {
                Assert.AreEqual(expected[i].ToString(), trees[i].ToString(),
                               "{0}: Index {1} Expected: {2} Actual: {3}",
                               label ?? string.Empty, i, expected[i], trees[i]);
            }
        }
    }
}