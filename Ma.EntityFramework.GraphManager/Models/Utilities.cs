using System.Linq;
using Ma.ExtensionMethods.Reflection;
using System.Collections;

namespace Ma.EntityFramework.GraphManager.Models
{
    public class Utilities
    {
        /// <summary>
        /// Check if first and second specified objects are equal or not
        /// </summary>
        /// <typeparam name="T">Type of objects</typeparam>
        /// <param name="first">First object to compare</param>
        /// <param name="second">Second object to compare</param>
        /// <returns></returns>
        public static bool IsEqual<T>(T first, T second)
        {
            bool isEqual = false;

            if (first != null && second == null)
                isEqual = false;
            else if (first == null && second != null)
                isEqual = false;
            else if (first == null && second == null)
                isEqual = true;
            else if (first.GetType().IsCollectionType())
                isEqual = Enumerable.SequenceEqual(((IEnumerable)first).Cast<object>(),
                    ((IEnumerable)second).Cast<object>());
            else
                isEqual = first.Equals(second);

            return isEqual;
        }
    }
}
