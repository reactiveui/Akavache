//
// BlockingCollection.cs
//
// Copyright (c) 2008 Jérémie "Garuma" Laval
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//

using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Akavache.Sqlite3.Internal
{
    public interface IProducerConsumerCollection<T> : IEnumerable<T>, ICollection, IEnumerable
	{
		bool TryAdd (T item);
		bool TryTake (out T item);
		T[] ToArray ();
		void CopyTo (T[] array, int index);
	}

	[ComVisible (false)]
	[DebuggerDisplay ("Count={Count}")]
	public class BlockingCollection<T> : IEnumerable<T>, ICollection, IEnumerable, IDisposable
	{
		const int spinCount = 5;

		readonly IProducerConsumerCollection<T> underlyingColl;

		/* These events are used solely for the purpose of having an optimized sleep cycle when
		 * the BlockingCollection have to wait on an external event (Add or Remove for instance)
		 */
		ManualResetEventSlim mreAdd = new ManualResetEventSlim (true);
		ManualResetEventSlim mreRemove = new ManualResetEventSlim (true);
		AtomicBoolean isComplete;

		readonly int upperBound;

		int completeId;

		/* The whole idea of the collection is to use these two long values in a transactional
		 * way to track and manage the actual data inside the underlying lock-free collection
		 * instead of directly working with it or using external locking.
		 *
		 * They are manipulated with CAS and are guaranteed to increase over time and use
		 * of the instance thus preventing ABA problems.
		 */
		int addId = int.MinValue;
		int removeId = int.MinValue;


		/* For time based operations, we share this instance of Stopwatch and base calculation
		   on a time offset at each of these method call */
		static Stopwatch watch = Stopwatch.StartNew ();

		#region ctors
		public BlockingCollection ()
			: this (new ConcurrentQueue<T> (), -1)
		{
		}

		public BlockingCollection (int boundedCapacity)
			: this (new ConcurrentQueue<T> (), boundedCapacity)
		{
		}

		public BlockingCollection (IProducerConsumerCollection<T> collection)
			: this (collection, -1)
		{
		}

		public BlockingCollection (IProducerConsumerCollection<T> collection, int boundedCapacity)
		{
			this.underlyingColl = collection;
			this.upperBound     = boundedCapacity;
			this.isComplete     = new AtomicBoolean ();
		}
		#endregion

		#region Add & Remove (+ Try)
		public void Add (T item)
		{
			Add (item, CancellationToken.None);
		}

		public void Add (T item, CancellationToken cancellationToken)
		{
			TryAdd (item, -1, cancellationToken);
		}

		public bool TryAdd (T item)
		{
			return TryAdd (item, 0, CancellationToken.None);
		}

		public bool TryAdd (T item, int millisecondsTimeout, CancellationToken cancellationToken)
		{
			if (millisecondsTimeout < -1)
				throw new ArgumentOutOfRangeException ("millisecondsTimeout");

			long start = millisecondsTimeout == -1 ? 0 : watch.ElapsedMilliseconds;
			SpinWait sw = new SpinWait ();

			do {
				cancellationToken.ThrowIfCancellationRequested ();

				int cachedAddId = addId;
				int cachedRemoveId = removeId;
				int itemsIn = cachedAddId - cachedRemoveId;

				// If needed, we check and wait that the collection isn't full
				if (upperBound != -1 && itemsIn > upperBound) {
					if (millisecondsTimeout == 0)
						return false;

					if (sw.Count <= spinCount) {
						sw.SpinOnce ();
					} else {
						mreRemove.Reset ();
						if (cachedRemoveId != removeId || cachedAddId != addId) {
							mreRemove.Set ();
							continue;
						}

						mreRemove.Wait (ComputeTimeout (millisecondsTimeout, start), cancellationToken);
					}

					continue;
				}

				// Check our transaction id against completed stored one
				if (isComplete.Value && cachedAddId >= completeId)
					ThrowCompleteException ();

				// Validate the steps we have been doing until now
				if (Interlocked.CompareExchange (ref addId, cachedAddId + 1, cachedAddId) != cachedAddId)
					continue;

				// We have a slot reserved in the underlying collection, try to take it
				if (!underlyingColl.TryAdd (item))
					throw new InvalidOperationException ("The underlying collection didn't accept the item.");

				// Wake up process that may have been sleeping
				mreAdd.Set ();

				return true;
			} while (millisecondsTimeout == -1 || (watch.ElapsedMilliseconds - start) < millisecondsTimeout);

			return false;
		}

		public bool TryAdd (T item, TimeSpan timeout)
		{
			return TryAdd (item, (int)timeout.TotalMilliseconds);
		}

		public bool TryAdd (T item, int millisecondsTimeout)
		{
			return TryAdd (item, millisecondsTimeout, CancellationToken.None);
		}

		public T Take ()
		{
			return Take (CancellationToken.None);
		}

		public T Take (CancellationToken cancellationToken)
		{
			T item;
			TryTake (out item, -1, cancellationToken, true);

			return item;
		}

		public bool TryTake (out T item)
		{
			return TryTake (out item, 0, CancellationToken.None);
		}

		public bool TryTake (out T item, int millisecondsTimeout, CancellationToken cancellationToken)
		{
			return TryTake (out item, millisecondsTimeout, cancellationToken, false);
		}

		bool TryTake (out T item, int milliseconds, CancellationToken cancellationToken, bool throwComplete)
		{
			if (milliseconds < -1)
				throw new ArgumentOutOfRangeException ("milliseconds");

			item = default (T);
			SpinWait sw = new SpinWait ();
			long start = milliseconds == -1 ? 0 : watch.ElapsedMilliseconds;

			do {
				cancellationToken.ThrowIfCancellationRequested ();

				int cachedRemoveId = removeId;
				int cachedAddId = addId;

				// Empty case
				if (cachedRemoveId == cachedAddId) {
					if (milliseconds == 0)
						return false;

					if (IsCompleted) {
						if (throwComplete)
							ThrowCompleteException ();
						else
							return false;
					}

					if (sw.Count <= spinCount) {
						sw.SpinOnce ();
					} else {
						mreAdd.Reset ();
						if (cachedRemoveId != removeId || cachedAddId != addId) {
							mreAdd.Set ();
							continue;
						}

						mreAdd.Wait (ComputeTimeout (milliseconds, start), cancellationToken);
					}

					continue;
				}

				if (Interlocked.CompareExchange (ref removeId, cachedRemoveId + 1, cachedRemoveId) != cachedRemoveId)
					continue;

				while (!underlyingColl.TryTake (out item));

				mreRemove.Set ();

				return true;

			} while (milliseconds == -1 || (watch.ElapsedMilliseconds - start) < milliseconds);

			return false;
		}

		public bool TryTake (out T item, TimeSpan timeout)
		{
			return TryTake (out item, (int)timeout.TotalMilliseconds);
		}

		public bool TryTake (out T item, int millisecondsTimeout)
		{
			item = default (T);

			return TryTake (out item, millisecondsTimeout, CancellationToken.None, false);
		}

		static int ComputeTimeout (int millisecondsTimeout, long start)
		{
			return millisecondsTimeout == -1 ? 500 : (int)Math.Max (watch.ElapsedMilliseconds - start - millisecondsTimeout, 1);
		}
		#endregion

		#region static methods
		static void CheckArray (BlockingCollection<T>[] collections)
		{
			if (collections == null)
				throw new ArgumentNullException ("collections");
			if (collections.Length == 0 || IsThereANullElement (collections))
				throw new ArgumentException ("The collections argument is a 0-length array or contains a null element.", "collections");
		}

		static bool IsThereANullElement (BlockingCollection<T>[] collections)
		{
			foreach (BlockingCollection<T> e in collections)
				if (e == null)
					return true;
			return false;
		}

		public static int AddToAny (BlockingCollection<T>[] collections, T item)
		{
			CheckArray (collections);
			int index = 0;
			foreach (var coll in collections) {
				try {
					coll.Add (item);
					return index;
				} catch {}
				index++;
			}
			return -1;
		}

		public static int AddToAny (BlockingCollection<T>[] collections, T item, CancellationToken cancellationToken)
		{
			CheckArray (collections);
			int index = 0;
			foreach (var coll in collections) {
				try {
					coll.Add (item, cancellationToken);
					return index;
				} catch {}
				index++;
			}
			return -1;
		}

		public static int TryAddToAny (BlockingCollection<T>[] collections, T item)
		{
			CheckArray (collections);
			int index = 0;
			foreach (var coll in collections) {
				if (coll.TryAdd (item))
					return index;
				index++;
			}
			return -1;
		}

		public static int TryAddToAny (BlockingCollection<T>[] collections, T item, TimeSpan timeout)
		{
			CheckArray (collections);
			int index = 0;
			foreach (var coll in collections) {
				if (coll.TryAdd (item, timeout))
					return index;
				index++;
			}
			return -1;
		}

		public static int TryAddToAny (BlockingCollection<T>[] collections, T item, int millisecondsTimeout)
		{
			CheckArray (collections);
			int index = 0;
			foreach (var coll in collections) {
				if (coll.TryAdd (item, millisecondsTimeout))
					return index;
				index++;
			}
			return -1;
		}

		public static int TryAddToAny (BlockingCollection<T>[] collections, T item, int millisecondsTimeout,
		                               CancellationToken cancellationToken)
		{
			CheckArray (collections);
			int index = 0;
			foreach (var coll in collections) {
				if (coll.TryAdd (item, millisecondsTimeout, cancellationToken))
					return index;
				index++;
			}
			return -1;
		}

		public static int TakeFromAny (BlockingCollection<T>[] collections, out T item)
		{
			item = default (T);
			CheckArray (collections);
			WaitHandle[] wait_table = null;
			while (true) {
				for (int i = 0; i < collections.Length; ++i) {
					if (collections [i].TryTake (out item))
						return i;
				}
				if (wait_table == null) {
					wait_table = new WaitHandle [collections.Length];
					for (int i = 0; i < collections.Length; ++i)
						wait_table [i] = collections [i].mreRemove.WaitHandle;
				}
				WaitHandle.WaitAny (wait_table);
			}
		}

		public static int TakeFromAny (BlockingCollection<T>[] collections, out T item, CancellationToken cancellationToken)
		{
			item = default (T);
			CheckArray (collections);
			WaitHandle[] wait_table = null;
			while (true) {
				for (int i = 0; i < collections.Length; ++i) {
					if (collections [i].TryTake (out item))
						return i;
				}
				cancellationToken.ThrowIfCancellationRequested ();
				if (wait_table == null) {
					wait_table = new WaitHandle [collections.Length + 1];
					for (int i = 0; i < collections.Length; ++i)
						wait_table [i] = collections [i].mreRemove.WaitHandle;
					wait_table [collections.Length] = cancellationToken.WaitHandle;
				}
				WaitHandle.WaitAny (wait_table);
				cancellationToken.ThrowIfCancellationRequested ();
			}
		}

		public static int TryTakeFromAny (BlockingCollection<T>[] collections, out T item)
		{
			item = default (T);

			CheckArray (collections);
			int index = 0;
			foreach (var coll in collections) {
				if (coll.TryTake (out item))
					return index;
				index++;
			}
			return -1;
		}

		public static int TryTakeFromAny (BlockingCollection<T>[] collections, out T item, TimeSpan timeout)
		{
			item = default (T);

			CheckArray (collections);
			int index = 0;
			foreach (var coll in collections) {
				if (coll.TryTake (out item, timeout))
					return index;
				index++;
			}
			return -1;
		}

		public static int TryTakeFromAny (BlockingCollection<T>[] collections, out T item, int millisecondsTimeout)
		{
			item = default (T);

			CheckArray (collections);
			int index = 0;
			foreach (var coll in collections) {
				if (coll.TryTake (out item, millisecondsTimeout))
					return index;
				index++;
			}
			return -1;
		}

		public static int TryTakeFromAny (BlockingCollection<T>[] collections, out T item, int millisecondsTimeout,
		                                  CancellationToken cancellationToken)
		{
			item = default (T);

			CheckArray (collections);
			int index = 0;
			foreach (var coll in collections) {
				if (coll.TryTake (out item, millisecondsTimeout, cancellationToken))
					return index;
				index++;
			}
			return -1;
		}
		#endregion

		public void CompleteAdding ()
		{
			// No further add beside that point
			completeId = addId;
			isComplete.Value = true;
			// Wakeup some operation in case this has an impact
			mreAdd.Set ();
			mreRemove.Set ();
		}

		void ThrowCompleteException ()
		{
			throw new InvalidOperationException ("The BlockingCollection<T> has"
			                                     + " been marked as complete with regards to additions.");
		}

		void ICollection.CopyTo (Array array, int index)
		{
			underlyingColl.CopyTo (array, index);
		}

		public void CopyTo (T[] array, int index)
		{
			underlyingColl.CopyTo (array, index);
		}

		public IEnumerable<T> GetConsumingEnumerable ()
		{
			return GetConsumingEnumerable (CancellationToken.None);
		}

		public IEnumerable<T> GetConsumingEnumerable (CancellationToken cancellationToken)
		{
			while (true) {
				T item = default (T);

				try {
					item = Take (cancellationToken);
				} catch {
					// Then the exception is perfectly normal
					if (IsCompleted)
						break;
					// otherwise rethrow
					throw;
				}

				yield return item;
			}
		}

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return ((IEnumerable)underlyingColl).GetEnumerator ();
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator ()
		{
			return ((IEnumerable<T>)underlyingColl).GetEnumerator ();
		}

		public void Dispose ()
		{

		}

		protected virtual void Dispose (bool disposing)
		{

		}

		public T[] ToArray ()
		{
			return underlyingColl.ToArray ();
		}

		public int BoundedCapacity {
			get {
				return upperBound;
			}
		}

		public int Count {
			get {
				return underlyingColl.Count;
			}
		}

		public bool IsAddingCompleted {
			get {
				return isComplete.Value;
			}
		}

		public bool IsCompleted {
			get {
				return isComplete.Value && addId == removeId;
			}
		}

		object ICollection.SyncRoot {
			get {
				return underlyingColl.SyncRoot;
			}
		}

		bool ICollection.IsSynchronized {
			get {
				return underlyingColl.IsSynchronized;
			}
		}
	}

	struct AtomicBooleanValue
	{
		int flag;
		const int UnSet = 0;
		const int Set = 1;

		public bool CompareAndExchange (bool expected, bool newVal)
		{
			int newTemp = newVal ? Set : UnSet;
			int expectedTemp = expected ? Set : UnSet;

			return Interlocked.CompareExchange (ref flag, newTemp, expectedTemp) == expectedTemp;
		}

		public static AtomicBooleanValue FromValue (bool value)
		{
			AtomicBooleanValue temp = new AtomicBooleanValue ();
			temp.Value = value;

			return temp;
		}

		public bool TrySet ()
		{
			return !Exchange (true);
		}

		public bool TryRelaxedSet ()
		{
			return flag == UnSet && !Exchange (true);
		}

		public bool Exchange (bool newVal)
		{
			int newTemp = newVal ? Set : UnSet;
			return Interlocked.Exchange (ref flag, newTemp) == Set;
		}

		public bool Value {
			get {
				return flag == Set;
			}
			set {
				Exchange (value);
			}
		}

		public bool Equals (AtomicBooleanValue rhs)
		{
			return this.flag == rhs.flag;
		}

		public override bool Equals (object rhs)
		{
			return rhs is AtomicBooleanValue ? Equals ((AtomicBooleanValue)rhs) : false;
		}

		public override int GetHashCode ()
		{
			return flag.GetHashCode ();
		}

		public static explicit operator bool (AtomicBooleanValue rhs)
		{
			return rhs.Value;
		}

		public static implicit operator AtomicBooleanValue (bool rhs)
		{
			return AtomicBooleanValue.FromValue (rhs);
		}
	}

	class AtomicBoolean
	{
		int flag;
		const int UnSet = 0;
		const int Set = 1;

		public bool CompareAndExchange (bool expected, bool newVal)
		{
			int newTemp = newVal ? Set : UnSet;
			int expectedTemp = expected ? Set : UnSet;

			return Interlocked.CompareExchange (ref flag, newTemp, expectedTemp) == expectedTemp;
		}

		public static AtomicBoolean FromValue (bool value)
		{
			AtomicBoolean temp = new AtomicBoolean ();
			temp.Value = value;

			return temp;
		}

		public bool TrySet ()
		{
			return !Exchange (true);
		}

		public bool TryRelaxedSet ()
		{
			return flag == UnSet && !Exchange (true);
		}

		public bool Exchange (bool newVal)
		{
			int newTemp = newVal ? Set : UnSet;
			return Interlocked.Exchange (ref flag, newTemp) == Set;
		}

		public bool Value {
			get {
				return flag == Set;
			}
			set {
				Exchange (value);
			}
		}

		public bool Equals (AtomicBoolean rhs)
		{
			return this.flag == rhs.flag;
		}

		public override bool Equals (object rhs)
		{
			return rhs is AtomicBoolean ? Equals ((AtomicBoolean)rhs) : false;
		}

		public override int GetHashCode ()
		{
			return flag.GetHashCode ();
		}

		public static explicit operator bool (AtomicBoolean rhs)
		{
			return rhs.Value;
		}

		public static implicit operator AtomicBoolean (bool rhs)
		{
			return AtomicBoolean.FromValue (rhs);
		}
	}

	[System.Diagnostics.DebuggerDisplay ("Count={Count}")]
	public class ConcurrentQueue<T> : IProducerConsumerCollection<T>, IEnumerable<T>, ICollection,
	                                  IEnumerable
	{
		class Node
		{
			public T Value;
			public Node Next;
		}
		
		Node head = new Node ();
		Node tail;
		int count;

		public ConcurrentQueue ()
		{
			tail = head;
		}
		
		public ConcurrentQueue (IEnumerable<T> collection): this()
		{
			foreach (T item in collection)
				Enqueue (item);
		}
		
		public void Enqueue (T item)
		{
			Node node = new Node ();
			node.Value = item;
			
			Node oldTail = null;
			Node oldNext = null;
			
			bool update = false;
			while (!update) {
				oldTail = tail;
				oldNext = oldTail.Next;
				
				// Did tail was already updated ?
				if (tail == oldTail) {
					if (oldNext == null) {
						// The place is for us
						update = Interlocked.CompareExchange (ref tail.Next, node, null) == null;
					} else {
						// another Thread already used the place so give him a hand by putting tail where it should be
						Interlocked.CompareExchange (ref tail, oldNext, oldTail);
					}
				}
			}
			// At this point we added correctly our node, now we have to update tail. If it fails then it will be done by another thread
			Interlocked.CompareExchange (ref tail, node, oldTail);
			Interlocked.Increment (ref count);
		}
		
		bool IProducerConsumerCollection<T>.TryAdd (T item)
		{
			Enqueue (item);
			return true;
		}

		public bool TryDequeue (out T result)
		{
			result = default (T);
			Node oldNext = null;
			bool advanced = false;

			while (!advanced) {
				Node oldHead = head;
				Node oldTail = tail;
				oldNext = oldHead.Next;
				
				if (oldHead == head) {
					// Empty case ?
					if (oldHead == oldTail) {
						// This should be false then
						if (oldNext != null) {
							// If not then the linked list is mal formed, update tail
							Interlocked.CompareExchange (ref tail, oldNext, oldTail);
							continue;
						}
						result = default (T);
						return false;
					} else {
						result = oldNext.Value;
						advanced = Interlocked.CompareExchange (ref head, oldNext, oldHead) == oldHead;
					}
				}
			}

			oldNext.Value = default (T);

			Interlocked.Decrement (ref count);

			return true;
		}
		
		public bool TryPeek (out T result)
		{
			result = default (T);
			bool update = true;
			
			while (update)
			{
				Node oldHead = head;
				Node oldNext = oldHead.Next;

				if (oldNext == null) {
					result = default (T);
					return false;
				}

				result = oldNext.Value;
				
				//check if head has been updated
				update = head != oldHead;
			}
			return true;
		}
		
		internal void Clear ()
		{
			count = 0;
			tail = head = new Node ();
		}
		
		IEnumerator IEnumerable.GetEnumerator ()
		{
			return (IEnumerator)InternalGetEnumerator ();
		}
		
		public IEnumerator<T> GetEnumerator ()
		{
			return InternalGetEnumerator ();
		}
		
		IEnumerator<T> InternalGetEnumerator ()
		{
			Node my_head = head;
			while ((my_head = my_head.Next) != null) {
				yield return my_head.Value;
			}
		}
		
		void ICollection.CopyTo (Array array, int index)
		{
			if (array == null)
				throw new ArgumentNullException ("array");
			if (array.Rank > 1)
				throw new ArgumentException ("The array can't be multidimensional");
			if (array.GetLowerBound (0) != 0)
				throw new ArgumentException ("The array needs to be 0-based");

			T[] dest = array as T[];
			if (dest == null)
				throw new ArgumentException ("The array cannot be cast to the collection element type", "array");
			CopyTo (dest, index);
		}
		
		public void CopyTo (T[] array, int index)
		{
			if (array == null)
				throw new ArgumentNullException ("array");
			if (index < 0)
				throw new ArgumentOutOfRangeException ("index");
			if (index >= array.Length)
				throw new ArgumentException ("index is equals or greather than array length", "index");

			IEnumerator<T> e = InternalGetEnumerator ();
			int i = index;
			while (e.MoveNext ()) {
				if (i == array.Length - index)
					throw new ArgumentException ("The number of elememts in the collection exceeds the capacity of array", "array");
				array[i++] = e.Current;
			}
		}
		
		public T[] ToArray ()
		{
			return new List<T> (this).ToArray ();
		}
		
		bool ICollection.IsSynchronized {
			get { return true; }
		}

		bool IProducerConsumerCollection<T>.TryTake (out T item)
		{
			return TryDequeue (out item);
		}
		
		object syncRoot = new object();
		object ICollection.SyncRoot {
			get { return syncRoot; }
		}
		
		public int Count {
			get {
				return count;
			}
		}
		
		public bool IsEmpty {
			get {
				return count == 0;
			}
		}
	}
}