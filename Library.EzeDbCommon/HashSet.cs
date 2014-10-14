using System;
using System.Collections;
using System.Text;

namespace Libraries.EzeDbCommon
{
    /// <summary>
    ///     A set based on an underlying HashTable. Object operations
    ///     (such as Add, Contains, and Remove) are ~O(1) and set operations
    ///     (such as Union and Intersection) are O(n+m) where n and m are
    ///     the sizes of the two sets. This class can be used for larger sets.
    /// </summary>
    public class HashSet : ISet
    {
        #region Fields

        private const int DefaultCapacity = 16;

        private readonly Hashtable _elements;

        #endregion

        #region Lifetime

        public HashSet()
            : this(DefaultCapacity)
        {
            // nothing to do
        }

        public HashSet(int capacity)
        {
            _elements = new Hashtable(capacity);
        }

        public HashSet(IEnumerable collection)
            : this(collection, DefaultCapacity)
        {
        }

        public HashSet(IEnumerable collection, int count)
            : this(collection, count, true)
        {
        }

        public HashSet(IEnumerable collection, int count, bool mightDuplicate)
            : this(count)
        {
            if (mightDuplicate)
            {
                foreach (object obj in collection)
                {
                    Add(obj);
                }
            }
            else
            {
                foreach (object obj in collection)
                {
                    addIgnoreDuplicate(obj);
                }
            }
        }

        public HashSet(ICollection collection)
            : this(collection, collection.Count)
        {
        }

        public HashSet(ICollection collection, bool mightDuplicate)
            : this(collection, collection.Count, mightDuplicate)
        {
        }

        #endregion

        #region Set Operations

        public ISet Union(ISet otherSet)
        {
            var array = new object[_elements.Count + otherSet.Count];
            CopyTo(array, 0);
            otherSet.CopyTo(array, _elements.Count);

            return new HashSet(array);
        }

        public ISet Intersection(ISet otherSet)
        {
            ISet result = new HashSet();

            foreach (DictionaryEntry entry in _elements)
            {
                object element = entry.Key;

                if (otherSet.Contains(element))
                {
                    result.Add(element);
                }
            }

            return result;
        }

        public ISet Subtract(ISet otherSet)
        {
            ISet result = new HashSet();

            foreach (DictionaryEntry entry in _elements)
            {
                object element = entry.Key;

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
                _elements.Add(value, value);
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

        private void addIgnoreDuplicate(object value)
        {
            _elements.Add(value, value);
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
            lock (_elements.SyncRoot)
            {
                foreach (DictionaryEntry entry in _elements)
                {
                    object element = entry.Key;

                    array.SetValue(element, index++);
                }
            }
        }

        public object SyncRoot
        {
            get { return _elements.SyncRoot; }
        }

        #endregion

        #region IEnumerable Members

        public IEnumerator GetEnumerator()
        {
            return _elements.Keys.GetEnumerator();
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
                    foreach (DictionaryEntry entry in _elements)
                    {
                        object element = entry.Key;

                        if (!otherSet.Contains(element))
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
            foreach (DictionaryEntry entry in _elements)
            {
                object element = entry.Key;

                hashCode += element.GetHashCode();
            }

            return hashCode;
        }

        public override string ToString()
        {
            var result = new StringBuilder();

            result.Append("Set: {");
            bool needComma = false;

            foreach (DictionaryEntry entry in _elements)
            {
                object element = entry.Key;

                if (needComma)
                {
                    result.Append(", ");
                }
                needComma = true;
                result.Append(element);
            }

            result.Append("}");

            return result.ToString();
        }

        #endregion
    }
}