using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Libraries.EzeDbCommon
{
    /// <summary>
    ///     The interface for classes implementing set semantics. A given value is only
    ///     in the set once, now matter how many times it has been added.
    ///     Membership tests are based on Object.Equals.
    /// </summary>
    public interface ISet : ICollection, IEnumerable
    {
        /// <summary>
        ///     True if this set can only be read, false if it is read/write
        /// </summary>
        bool IsReadOnly { get; }

        /// <summary>
        ///     Return a new set that represents the union of this set with otherSet.
        /// </summary>
        /// <param name="otherSet">The set to combine with this one</param>
        /// <returns>The new set</returns>
        ISet Union(ISet otherSet);


        /// <summary>
        ///     Returns a new set representing the intesection of this set with otherSet
        /// </summary>
        /// <param name="otherSet">The set to intersect with this set</param>
        /// <returns>The new set</returns>
        ISet Intersection(ISet otherSet);

        /// <summary>
        ///     Subtracts otherSet from this set and returns a new set with the result
        /// </summary>
        /// <param name="otherSet">The set to subtract from this one</param>
        /// <returns>THe new set</returns>
        ISet Subtract(ISet otherSet);

        // The following methods have the same signatures as the corresponding members
        // of the IList interface. Classic Microsift - they really should have had a 
        // type of collection that supported these operations but was not indexable.
        // Of course, they should have had a set class, too. :-)

        /// <summary>
        ///     Remove an item from the set. If the item is not in the set, this
        ///     method does nothing.
        /// </summary>
        /// <param name="value">The item to remove</param>
        void Remove(object value);

        /// <summary>
        ///     Determine whether the set contains the value requested.
        /// </summary>
        /// <param name="value">The value to test</param>
        /// <returns>true if the value is in the collection, false otherwise</returns>
        bool Contains(object value);

        /// <summary>
        ///     Clear the set to the empty set.
        /// </summary>
        void Clear();

        /// <summary>
        ///     Add the item to the set. Note that this differs from IList in not returning
        ///     a value, because a set has no meaningful index. If the value is already in the
        ///     set, this method may replace it or do nothing at the discretion of the
        ///     implementation.
        /// </summary>
        /// <param name="value">The value to add</param>
        void Add(object value);

        /// <summary>
        ///     Add each element of the colelction to the set, preserving set semantics.
        /// </summary>
        /// <param name="collection">The collection containing the values to add</param>
        void AddRange(ICollection collection);
    }
}
