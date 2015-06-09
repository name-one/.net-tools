using System.Collections.Generic;

namespace InoSoft.Tools
{
    public static class HashSetHelper
    {
        /// <summary>
        ///   Creates a set that is an intersection of the two provided sets and deletes all elements belonging to this
        ///   intersection from the sets <paramref name="a"/> and <paramref name="b"/>.
        /// </summary>
        /// <typeparam name="T">The type of elements in the sets.</typeparam>
        /// <param name="a">The first set.</param>
        /// <param name="b">The second set.</param>
        /// <returns>
        ///   A set containing the intersection of <paramref name="a"/> with <paramref name="b"/>, containing only
        ///   those elements of <paramref name="a"/> that are present in <paramref name="b"/> according to
        ///   the equality comparer of <paramref name="a"/>.
        /// </returns>
        public static HashSet<T> SplitSets<T>(HashSet<T> a, HashSet<T> b)
        {
            // Create a set that is an intersection of A and B.
            var c = new HashSet<T>(a, a.Comparer);
            c.IntersectWith(b);

            // Now the sets' contents look like this:
            //     a      |      c      |      b
            // [X][X][ ]  |  [ ][X][ ]  |  [ ][X][X]

            // Leave only objects that are elements of A in the A set:
            // [X][ ][ ]  |  [ ][X][ ]  |  [ ][X][X]
            a.ExceptWith(c);

            // Leave only objects that are elements of B in the B set:
            // [X][ ][ ]  |  [ ][X][ ]  |  [ ][ ][X]
            b.ExceptWith(c);

            return c;
        }
    }
}