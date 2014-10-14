using System;
using System.Collections;
using System.Text;

namespace Libraries.EzeDbCommon
{
    /// <summary>
    ///     A set based on an underlying ArrayList. Object operations
    ///     (such as Add, Contains, and Remove) are O(n) and set operations
    ///     (such as Union and Intersection) are O(n*m) where n and m are
    ///     the sizes of the two sets. This class should be used for small
    ///     sets only.
    /// </summary>
    public class ArrayListSet : ISet
    {
        #region Fields

        private const int DefaultCapacity = 16;

        private readonly ArrayList _elements;

        #endregion

        #region Lifetime

        public ArrayListSet()
            : this(DefaultCapacity)
        {
            // nothing to do
        }

        public ArrayListSet(int capacity)
        {
            _elements = new ArrayList(capacity);
        }

        public ArrayListSet(ICollection collection)
        {
            _elements = new ArrayList(collection == null ? 16 : collection.Count);

            if (collection != null)
            {
                // We need to add each object explicitly rather than just
                // create the ArrayList with the collection in order to supress 
                // duplicates
                foreach (object obj in collection)
                {
                    Add(obj);
                }
            }
        }

        #endregion

        #region Set Operations

        public ISet Union(ISet otherSet)
        {
            var array = new object[_elements.Count + otherSet.Count];
            CopyTo(array, 0);
            otherSet.CopyTo(array, _elements.Count);

            return new ArrayListSet(array);
        }

        public ISet Intersection(ISet otherSet)
        {
            ISet result = new ArrayListSet();

            foreach (object element in _elements)
            {
                if (otherSet.Contains(element))
                {
                    result.Add(element);
                }
            }

            return result;
        }

        public ISet Subtract(ISet otherSet)
        {
            ISet result = new ArrayListSet();

            foreach (object element in _elements)
            {
                if (!otherSet.Contains(element))
                {
                    result.Add(element);
                }
            }

            return result;
        }

        #endregion

        #region IList-like Operations

        public bool IsReadOnly
        {
            get { return false; }
        }

        public void Remove(object value)
        {
            _elements.Remove(value);
        }

        public bool Contains(object value)
        {
            return _elements.Contains(value);
        }

        public void Clear()
        {
            _elements.Clear();
        }

        public void Add(object value)
        {
            if (!Contains(value))
            {
                _elements.Add(value);
            }
        }

        public void AddRange(ICollection collection)
        {
            if (collection != null)
            {
                foreach (object obj in collection)
                {
                    Add(obj);
                }
            }
        }

        public int IndexOf(object value)
        {
            return _elements.IndexOf(value);
        }

        #endregion

        #region ICollection Members

        public bool IsSynchronized
        {
            get { return false; }
        }

        public int Count
        {
            get { return _elements.Count; }
        }

        public void CopyTo(Array array, int index)
        {
            _elements.CopyTo(array, index);
        }

        public object SyncRoot
        {
            get { return _elements.SyncRoot; }
        }

        #endregion

        #region IEnumerable Members

        public IEnumerator GetEnumerator()
        {
            return _elements.GetEnumerator();
        }

        #endregion

        #region Object overrides

        /// <summary>
        ///     Determine whether two sets are equal. Note that the equality will
        ///     return true even if the other set is a different class implementing
        ///     ISet as long as they have the same elements.
        /// </summary>
        /// <param name="obj">The object to which to compare</param>
        /// <returns>True if the two sets have the same elements, flse otherwise.</returns>
        public override bool Equals(object obj)
        {
            if (obj != null && obj is ISet)
            {
                var otherSet = (ISet)obj;

                if (_elements.Count == otherSet.Count)
                {
                    for (int i = 0; i < _elements.Count; i++)
                    {
                        if (!otherSet.Contains(_elements[i]))
                        {
                            return false;
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        ///     This override of the hash code simply sums all the elements hash codes.
        ///     Other implementations of ISet should do the same. Note that this computation needs
        ///     to return the same result irrespective of the order of the elements.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            int hashCode = 0;
            for (int i = 0; i < _elements.Count; i++)
            {
                hashCode += _elements[i].GetHashCode();
            }

            return hashCode;
        }

        public override string ToString()
        {
            var result = new StringBuilder();

            result.Append("Set: {");
            bool needComma = false;

            for (int i = 0; i < _elements.Count; i++)
            {
                if (needComma)
                {
                    result.Append(", ");
                }
                needComma = true;
                result.Append(_elements[i]);
            }
            result.Append("}");

            return result.ToString();
        }

        #endregion
    }
}