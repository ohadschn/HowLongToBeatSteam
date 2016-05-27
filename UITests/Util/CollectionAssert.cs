using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UITests.Util
{
    public static class CollectionAssert
    {
        public static void AssertEqualSequences<T>([NotNull] IEnumerable<T> expected, [NotNull] IEnumerable<T> actual)
        {
            if (expected == null) throw new ArgumentNullException(nameof(expected));
            if (actual == null) throw new ArgumentNullException(nameof(actual));

            ICollection<T> expectedCollection = expected as ICollection<T> ?? expected.ToArray();
            ICollection<T> actualCollection = actual as ICollection<T> ?? actual.ToArray();

            Assert.IsTrue(expectedCollection.SequenceEqual(actualCollection),
                $"Expected sequence : {expectedCollection.StringJoin()}; Actual sequence: {actualCollection.StringJoin()}");
        }

        public static void AssertEqualSets<T>([NotNull] IEnumerable<T> expected, [NotNull] IEnumerable<T> actual)
        {
            if (expected == null) throw new ArgumentNullException(nameof(expected));
            if (actual == null) throw new ArgumentNullException(nameof(actual));
            ISet<T> expectedSet = expected as ISet<T> ?? new HashSet<T>(expected);
            ISet<T> actualSet = actual as ISet<T> ?? new HashSet<T>(actual);

            Assert.IsTrue(expectedSet.SetEquals(actualSet), $"Expected set: {expectedSet.StringJoin()}; Actual set: {actualSet.StringJoin()}");
        }

        public static void AssertDistinctSets<T>([NotNull] IEnumerable<T> first, [NotNull] IEnumerable<T> second)
        {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));
            ISet<T> firstSet = first as ISet<T> ?? new HashSet<T>(first);
            ISet<T> secondSet = second as ISet<T> ?? new HashSet<T>(second);

            var intersection = firstSet.Intersect(secondSet).ToArray();
            Assert.AreEqual(0, intersection.Length, $"Non-empty intersection for set: {firstSet.StringJoin()} and set: {secondSet.StringJoin()} - {intersection.StringJoin()}");
        }

        public static void StringContains([NotNull] string str, [NotNull] string substring)
        {
            if (str == null) throw new ArgumentNullException(nameof(str));
            if (substring == null) throw new ArgumentNullException(nameof(substring));

            Assert.IsTrue(str.Contains(substring), $"Expected '{str}' to contain '{substring}'");
        }
    }
}
