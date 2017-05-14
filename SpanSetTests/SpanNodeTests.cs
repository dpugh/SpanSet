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

            var newTrees1 = SpanTree<int>.Add(trees, 100, Span.FromBounds(104, 107), -1);
            AssertExpected(expected, newTrees1);

            var newTrees2 = SpanTree<int>.Add(trees, 100, Span.FromBounds(105, 107), -1);
            AssertExpected(expected, newTrees2);

            var newTrees3 = SpanTree<int>.Add(trees, 100, Span.FromBounds(104, 106), -1);
            AssertExpected(expected, newTrees3);


            var newTrees4 = SpanTree<int>.Add(trees, 100, Span.FromBounds(105, 106), -1);
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

            var newTrees1 = SpanTree<int>.Add(trees, 100, Span.FromBounds(100, 102), -1);
            AssertExpected(expected, newTrees1);

            var newTrees2 = SpanTree<int>.Add(trees, 100, Span.FromBounds(100, 101), -1);
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

            var newTrees1 = SpanTree<int>.Add(trees, 100, Span.FromBounds(110, 112), -1);
            AssertExpected(expected, newTrees1);

            var newTrees2 = SpanTree<int>.Add(trees, 100, Span.FromBounds(111, 112), -1);
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

            var newTrees = SpanTree<int>.Add(trees, 100, Span.FromBounds(103, 105), -1);
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

            var newTrees = SpanTree<int>.Add(trees, 100, Span.FromBounds(106, 108), -1);
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

            var newTrees1 = SpanTree<int>.Add(trees, 100, Span.FromBounds(103, 104), -1);
            AssertExpected(expected, newTrees1);
            AssertExpected(expectedChildren1, newTrees1[0].Children);

            var newTrees2 = SpanTree<int>.Add(trees, 100, Span.FromBounds(103, 105), -1);
            AssertExpected(expected, newTrees2);
            AssertExpected(expectedChildren2, newTrees2[0].Children);

            var newTrees3 = SpanTree<int>.Add(trees, 100, Span.FromBounds(102, 104), -1);
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
            var expectedChildren2 = new SpanTree<int>[] { new SpanTree<int>(Span.FromBounds(0, 3), 1) };
            var expectedChildren3 = new SpanTree<int>[] { new SpanTree<int>(Span.FromBounds(1, 4), 1) };
            var expectedChildren4 = new SpanTree<int>[] { new SpanTree<int>(Span.FromBounds(1, 4), 1) };
            var expectedChildren5 = new SpanTree<int>[] { new SpanTree<int>(Span.FromBounds(2, 5), 1) };

            var newTrees1 = SpanTree<int>.Add(trees, 100, Span.FromBounds(102, 105), -1);
            AssertExpected(expected, newTrees1);
            AssertExpected(expectedChildren1, newTrees1[0].Children);

            var newTrees2 = SpanTree<int>.Add(trees, 100, Span.FromBounds(102, 106), -1);
            AssertExpected(expected, newTrees2);
            AssertExpected(expectedChildren2, newTrees2[0].Children);

            var newTrees3 = SpanTree<int>.Add(trees, 100, Span.FromBounds(101, 105), -1);
            AssertExpected(expected, newTrees3);
            AssertExpected(expectedChildren3, newTrees3[0].Children);

            var newTrees4 = SpanTree<int>.Add(trees, 100, Span.FromBounds(101, 106), -1);
            AssertExpected(expected, newTrees3);
            AssertExpected(expectedChildren4, newTrees4[0].Children);

            var newTrees5 = SpanTree<int>.Add(trees, 100, Span.FromBounds(100, 106), -1);
            AssertExpected(expected, newTrees5);
            AssertExpected(expectedChildren5, newTrees5[0].Children);
            Assert.IsTrue(object.ReferenceEquals(trees[0], newTrees5[0].Children[0]));
        }


        [TestMethod()]
        public void SpanTreesAddToLeadingStacked()
        {
            var trees = new SpanTree<int>[]
                            { new SpanTree<int>(Span.FromBounds(2, 20), 1),
                              new SpanTree<int>(Span.FromBounds(4, 22), 2),
                              new SpanTree<int>(Span.FromBounds(6, 24), 3),
                              new SpanTree<int>(Span.FromBounds(8, 26), 4)};

            var expected = new int[] { -1, 1, 2, 3, 4 };


            for (int end = 101; (end < 110); ++end)
            {
                var newTrees1 = SpanTree<int>.Add(trees, 100, Span.FromBounds(100, end), -1);
                AssertExpected(expected, newTrees1);
            }
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