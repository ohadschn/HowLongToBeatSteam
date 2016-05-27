using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UITests.Util
{
    public static class CollectionAssert
    {
        public static void AssertEqualSequences<T>([NotNull] IEnumerable<T> expected, [NotNull] IEnumerable<T> actual, [NotNull] string message)
        {
            if (expected == null) throw new ArgumentNullException(nameof(expected));
            if (actual == null) throw new ArgumentNullException(nameof(actual));
            if (message == null) throw new ArgumentNullException(nameof(message));

            ICollection<T> expectedCollection = expected as ICollection<T> ?? expected.ToArray();
            ICollection<T> actualCollection = actual as ICollection<T> ?? actual.ToArray();

            Assert.IsTrue(expectedCollection.SequenceEqual(actualCollection),
                $"{message}. Expected sequence : {expectedCollection.StringJoin()}; Actual sequence: {actualCollection.StringJoin()}");
        }

        public static void AssertEqualSets<T>([NotNull] IEnumerable<T> expected, [NotNull] IEnumerable<T> actual, [NotNull] string message)
        {
            if (expected == null) throw new ArgumentNullException(nameof(expected));
            if (actual == null) throw new ArgumentNullException(nameof(actual));
            if (message == null) throw new ArgumentNullException(nameof(message));

            ISet<T> expectedSet = expected as ISet<T> ?? new HashSet<T>(expected);
            ISet<T> actualSet = actual as ISet<T> ?? new HashSet<T>(actual);

            Assert.IsTrue(expectedSet.SetEquals(actualSet), $"{message}. Expected set: {expectedSet.StringJoin()}; Actual set: {actualSet.StringJoin()}");
        }

        public static void AssertDistinctSets<T>([NotNull] IEnumerable<T> first, [NotNull] IEnumerable<T> second, [NotNull] string message)
        {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));
            if (message == null) throw new ArgumentNullException(nameof(message));

            ISet<T> firstSet = first as ISet<T> ?? new HashSet<T>(first);
            ISet<T> secondSet = second as ISet<T> ?? new HashSet<T>(second);

            var intersection = firstSet.Intersect(secondSet).ToArray();
            Assert.AreEqual(0, intersection.Length, $"{message}. Non-empty intersection for set: {firstSet.StringJoin()} and set: {secondSet.StringJoin()} - {intersection.StringJoin()}");
        }

        public static void StringContains([NotNull] string str, [NotNull] string substring, [NotNull] string message)
        {
            if (str == null) throw new ArgumentNullException(nameof(str));
            if (substring == null) throw new ArgumentNullException(nameof(substring));
            if (message == null) throw new ArgumentNullException(nameof(message));

            Assert.IsTrue(str.Contains(substring), $"{message}. Expected '{str}' to contain '{substring}'");
        }
    }
}
