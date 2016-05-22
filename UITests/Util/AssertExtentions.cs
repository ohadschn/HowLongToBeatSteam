using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UITests.Util
{
    public static class AssertExtentions
    {
        public static void AssertEqualSequences<T>(IEnumerable<T> expected, IEnumerable<T> actual)
        {
            ICollection<T> expectedCollection = expected as ICollection<T> ?? expected.ToArray();
            ICollection<T> actualCollection = actual as ICollection<T> ?? actual.ToArray();

            Assert.IsTrue(expectedCollection.SequenceEqual(actualCollection),
                $"Expected sequence : {expectedCollection.StringJoin()}; Actual sequence: {actualCollection.StringJoin()}");
        }

        public static void AssertEqualSets<T>(IEnumerable<T> expected, IEnumerable<T> actual)
        {
            ISet<T> expectedSet = expected as ISet<T> ?? new HashSet<T>(expected);
            ISet<T> actualSet = actual as ISet<T> ?? new HashSet<T>(actual);

            Assert.IsTrue(expectedSet.SetEquals(actualSet), $"Expected set: {expectedSet.StringJoin()}; Actual set: {actualSet.StringJoin()}");
        }

        public static void AssertDistinctSets<T>(IEnumerable<T> first, IEnumerable<T> second)
        {
            ISet<T> firstSet = first as ISet<T> ?? new HashSet<T>(first);
            ISet<T> secondSet = second as ISet<T> ?? new HashSet<T>(second);

            var intersection = firstSet.Intersect(secondSet).ToArray();
            Assert.AreEqual(0, intersection.Length, $"Non-empty intersection for set: {firstSet.StringJoin()} and set: {secondSet.StringJoin()} - {intersection.StringJoin()}");
        }

        public static void StringContains(string str, string substring)
        {
            Assert.IsTrue(str.Contains(substring), $"Expected '{str}' to contain '{substring}'");
        }
    }
}
