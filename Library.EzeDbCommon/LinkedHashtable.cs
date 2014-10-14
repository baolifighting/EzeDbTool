#region $File: //depot/BRANCHES/7.0.2/Libraries/Common/Common/LinkedHashtable.cs $

//
// <copyright file="$File: //depot/BRANCHES/7.0.2/Libraries/Common/Common/LinkedHashtable.cs $" company="3VR, Inc.">
//     Copyright (c) 2004-2011 3VR, Inc. All rights reserved.
// </copyright>
//
// $File: //depot/BRANCHES/7.0.2/Libraries/Common/Common/LinkedHashtable.cs $
// $DateTime: 2012/06/06 11:03:25 $
// $Change: 99204 $
// $Revision: #2 $
// $Author: opoquet $
//

#endregion $File: //depot/BRANCHES/7.0.2/Libraries/Common/Common/LinkedHashtable.cs $

#region Imports

using System;
using System.Collections;

#endregion Imports

namespace Libraries.EzeDbCommon
{
	/// <summary>
	///     Implements a hashtable that can be traversed in insertion order or
	///     in least recently used order, depending how it's constructed. This is
	///     essentially the C# implementation of Java's LinkedHashMap.
	/// </summary>
	[Serializable]
	public class LinkedHashtable : Hashtable
	{
		#region Fields

		public readonly bool LruOrdered;
		private LinkedHashtableElement _queueHead;
		private int generation;

		#endregion Fields

		#region Lifetime

		public LinkedHashtable()
		{
			LruOrdered = false;
			initialize();
		}

		public LinkedHashtable(bool lruOrdered)
		{
			LruOrdered = lruOrdered;
			initialize();
		}

		public LinkedHashtable(int capacity, float loadFactor, bool lruOrdered)
			: base(capacity, loadFactor)
		{
			LruOrdered = lruOrdered;
			initialize();
		}

		private void initialize()
		{
			_queueHead = new LinkedHashtableElement("null", null); // the key can not be null
			_queueHead.Next = _queueHead;
			_queueHead.Previous = _queueHead;
		}

		#endregion Lifetime

		#region Properties

		// internal stuff to support enumeration
		private LinkedHashtableElement QueueHead
		{
			get { return _queueHead; }
		}

		private int Generation
		{
			get { return generation; }
		}

		public override ICollection Keys
		{
			get
			{
				var result = new ArrayList(Count);

				LinkedHashtableElement element = _queueHead.Next;
				while (element != _queueHead)
				{
					result.Add(element.Entry.Key);
					element = element.Next;
				}
				return result;
			}
		}

		public override ICollection Values
		{
			get
			{
				var result = new ArrayList(Count);

				LinkedHashtableElement element = _queueHead.Next;
				while (element != _queueHead)
				{
					result.Add(element.Entry.Value);
					element = element.Next;
				}
				return result;
			}
		}

		public override object this[object key]
		{
			get
			{
				var element = (LinkedHashtableElement)base[key];

				if (element != null)
				{
					if (LruOrdered)
					{
						element.Remove();
						element.InsertBefore(_queueHead);
						generation++;
					}
					return element.Value;
				}
				else
				{
					return null;
				}
			}
			set
			{
				var oldElement = (LinkedHashtableElement)base[key];
				if (oldElement != null)
				{
					oldElement.Remove();
				}

				var newElement = new LinkedHashtableElement(key, value);
				base[key] = newElement;
				newElement.InsertBefore(_queueHead);
				generation++;
			}
		}

		#endregion Properties

		#region Methods

		public override void Remove(object key)
		{
			var element = (LinkedHashtableElement)base[key];
			if (element != null)
			{
				element.Remove();
				base.Remove(key);
				generation++;
			}
		}

		public override void Add(object key, object value)
		{
			var oldElement = (LinkedHashtableElement)base[key];
			if (oldElement != null)
			{
				throw new ArgumentException("Key already exists in table during Add()");
			}

			var newElement = new LinkedHashtableElement(key, value);
			base[key] = newElement;
			newElement.InsertBefore(_queueHead);
			generation++;
		}


		public override void Clear()
		{
			_queueHead.Next = _queueHead;
			_queueHead.Previous = _queueHead;

			base.Clear();
			generation++;
		}

		public override bool ContainsValue(object value)
		{
			LinkedHashtableElement element = _queueHead.Next;

			while (element != _queueHead)
			{
				if (element.Value.Equals(value)) // use the comparator here if we support different comparators
				{
					return true;
				}
				element = element.Next;
			}
			return false;
		}

		public override object Clone()
		{
			var newTable = new LinkedHashtable(10, 0.75f, LruOrdered); // Should get capacity and load

			LinkedHashtableElement element = _queueHead.Previous;

			// Go backwards to preserve iteration order
			while (element != _queueHead)
			{
				newTable[element.Key] = element.Value;
				element = element.Previous;
			}

			return newTable;
		}

		public override void CopyTo(Array array, int arrayIndex)
		{
			if (array == null)
			{
				throw new ArgumentNullException("array", "Array to copy to must be non-null");
			}
			if (arrayIndex < 0)
			{
				throw new ArgumentOutOfRangeException("arrayIndex", "arrayIndex must be >= 0");
			}
			if (array.Rank > 1)
			{
				throw new ArgumentException("Array must be one-dimensional", "array");
			}
			if (arrayIndex > array.Length)
			{
				throw new ArgumentException("arrayIndex must be within array", "arrayIndex");
			}
			if (Count + arrayIndex > array.Length)
			{
				throw new ArgumentException("Not enough room to copy table to array", "arrayIndex");
			}

			LinkedHashtableElement element = _queueHead.Next;
			while (element != _queueHead)
			{
				array.SetValue(element.Entry, arrayIndex++);
				element = element.Next;
			}
		}

		/// <summary>
		///     A non-blocking dequeue that removes the first element in the iteration order for the
		///     table. It throws an InvalidOperationException if the table is empty.
		/// </summary>
		/// <returns>The key/value pair of the element removed</returns>
		public DictionaryEntry Dequeue()
		{
			LinkedHashtableElement element = _queueHead.Next;

			if (element == _queueHead)
			{
				throw new InvalidOperationException("Tried to dequeue from an empty LinkedHashList");
			}

			element.Remove();
			base.Remove(element.Key);
			generation++;

			return element.Entry;
		}


		/// <summary>
		///     A non-blocking peek that gets the first element in the iteration order for the
		///     table without removing it. It throws an InvalidOperationException if the table is empty.
		/// </summary>
		/// <returns>The key/value pair of the element removed</returns>
		public DictionaryEntry Peek()
		{
			LinkedHashtableElement element = _queueHead.Next;

			if (element == _queueHead)
			{
				throw new InvalidOperationException("Tried to dequeue from an empty LinkedHashList");
			}

			return element.Entry;
		}

		#endregion Methods

		#region IEnumerable Members

		public override IDictionaryEnumerator GetEnumerator()
		{
			return new LinkedHashtableEnumerator(this);
		}

		#endregion

		#region class LinkedHashtableElement

		internal class LinkedHashtableElement
		{
			public readonly DictionaryEntry Entry;
			public LinkedHashtableElement Next;
			public LinkedHashtableElement Previous;

			public LinkedHashtableElement(object key, object value)
			{
				Entry = new DictionaryEntry(key, value);
				Next = null;
				Previous = null;
			}

			public object Key
			{
				get { return Entry.Key; }
			}

			public object Value
			{
				get { return Entry.Value; }
			}

			public void AddAfter(LinkedHashtableElement originalElement)
			{
				Next = originalElement.Next;
				originalElement.Next = this;
				Next.Previous = this;
				Previous = originalElement;
			}

			public void InsertBefore(LinkedHashtableElement originalElement)
			{
				Previous = originalElement.Previous;
				originalElement.Previous = this;
				Next = originalElement;
				Previous.Next = this;
			}

			public void Remove()
			{
				if (Next != null)
				{
					Next.Previous = Previous;
				}
				if (Previous != null)
				{
					Previous.Next = Next;
				}
			}
		}

		#endregion class LinkedHashtableElement

		#region class LinkedHashtableEnumerator

		private class LinkedHashtableEnumerator : IDictionaryEnumerator
		{
			private readonly int generation;
			private readonly LinkedHashtable table;
			private bool atEnd;
			private LinkedHashtableElement element;

			public LinkedHashtableEnumerator(LinkedHashtable table)
			{
				this.table = table;
				element = table.QueueHead;
				generation = table.Generation;
			}

			public bool MoveNext()
			{
				if (atEnd)
				{
					return false;
				}

				element = element.Next;

				if (element == table.QueueHead)
				{
					atEnd = true;
					return false;
				}
				return true;
			}

			public void Reset()
			{
				element = table.QueueHead;
				atEnd = false;
			}

			public object Key
			{
				get
				{
					check();
					return element.Entry.Key;
				}
			}

			public object Value
			{
				get
				{
					check();
					return element.Entry.Value;
				}
			}

			public DictionaryEntry Entry
			{
				get
				{
					check();
					return element.Entry;
				}
			}

			public object Current
			{
				get
				{
					check();
					return element.Entry;
				}
			}

			private void check()
			{
				if (atEnd || element == table.QueueHead)
				{
					throw new InvalidOperationException("Enumerator at invalid position");
				}
				if (generation != table.Generation)
				{
					throw new InvalidOperationException("LinkedHashtable modified during enumeration");
				}
			}
		}

		#endregion class LinkedHashtableEnumerator
	}
}
