using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static System.FormattableString;

namespace UITests.Util
{
    public static class CollectionAssert
    {
        public static void AssertEqualSequences<T>([NotNull] IEnumerable<T> expected, [NotNull] IEnumerable<T> actual, [NotNull] string message)
        {
            if (expected == null) throw new ArgumentNullException(nameof(expected));
            if (actual == null) throw new ArgumentNullException(nameof(actual));
            if (message == null) throw new ArgumentNullException(nameof(message));

            var expectedCollection = expected as ICollection<T> ?? expected.ToArray();
            var actualCollection = actual as ICollection<T> ?? actual.ToArray();

            Assert.IsTrue(expectedCollection.SequenceEqual(actualCollection),
                Invariant($"{message}. Expected sequence : {expectedCollection.StringJoin()}; Actual sequence: {actualCollection.StringJoin()}"));
        }

        public static void AssertEqualSets<T>([NotNull] IEnumerable<T> expected, [NotNull] IEnumerable<T> actual, [NotNull] string message)
        {
            if (expected == null) throw new ArgumentNullException(nameof(expected));
            if (actual == null) throw new ArgumentNullException(nameof(actual));
            if (message == null) throw new ArgumentNullException(nameof(message));

            var expectedSet = expected as ISet<T> ?? new HashSet<T>(expected);
            var actualSet = actual as ISet<T> ?? new HashSet<T>(actual);

            Assert.IsTrue(expectedSet.SetEquals(actualSet), Invariant($"{message}. Expected set: {expectedSet.StringJoin()}; Actual set: {actualSet.StringJoin()}"));
        }

        public static void AssertDistinctSets<T>([NotNull] IEnumerable<T> first, [NotNull] IEnumerable<T> second, [NotNull] string message)
        {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));
            if (message == null) throw new ArgumentNullException(nameof(message));

            var firstSet = first as ISet<T> ?? new HashSet<T>(first);
            var secondSet = second as ISet<T> ?? new HashSet<T>(second);

            var intersection = firstSet.Intersect(secondSet).ToArray();
            Assert.AreEqual(0, intersection.Length, 
                Invariant($"{message}. Non-empty intersection for set: {firstSet.StringJoin()} and set: {secondSet.StringJoin()} - {intersection.StringJoin()}"));
        }
    }
}
