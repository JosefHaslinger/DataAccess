﻿using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JPB.DataAccess.Contacts;
using JPB.DataAccess.DbInfoConfig;
using JPB.DataAccess.Manager;

namespace JPB.DataAccess.Helper.LocalDb
{
	/// <summary>
	/// Provides an wrapper for the non Generic LocalDbReposetory 
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class LocalDbReposetory<T> : LocalDbReposetoryBase, ICollection<T>
	{
		/// <summary>
		/// Creates a new LocalDB Repro by using <typeparamref name="T"/>
		/// </summary>
		public LocalDbReposetory(DbConfig config, params ILocalDbConstraint[] constraints)
			: base(typeof(T), null, config, constraints)
		{
		}
		/// <summary>
		/// Creates a new LocalDB Repro by using <typeparamref name="T"/> that uses the DbAccessLayer as fallback if the requested item was not found localy
		/// </summary>
		public LocalDbReposetory(DbAccessLayer db)
			: base(db, typeof(T))
		{
		}
		/// <summary>
		/// Creates a new LocalDB Repro by using <typeparamref name="T"/> and uses the KeyProvider to generate Primarykeys
		/// </summary>
		public LocalDbReposetory(DbConfig config, ILocalPrimaryKeyValueProvider keyProvider, params ILocalDbConstraint[] constraints)
			: base(typeof(T), keyProvider, config, constraints)
		{
		}

		/// <summary>
		/// Adds a new Item to the Table
		/// </summary>
		/// <param name="item"></param>
		public void Add(T item)
		{
			base.Add(item);
		}

		/// <summary>
		/// Checks if the item is ether localy stored or on database
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public bool Contains(T item)
		{
			return base.Contains(item);
		}

		/// <summary>
		/// Thread save
		/// </summary>
		/// <returns></returns>
		public new T[] ToArray()
		{
			lock (SyncRoot)
			{
				return _base.Values.Cast<T>().ToArray();
			}
		}

		/// <summary>
		/// Copys the current collection the an Array
		/// </summary>
		/// <param name="array"></param>
		/// <param name="arrayIndex"></param>
		public void CopyTo(T[] array, int arrayIndex)
		{
			base.CopyTo(array, arrayIndex);
		}

		/// <summary>
		/// Removes the item from this collection
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public bool Remove(T item)
		{
			return base.Remove(item);
		}

		/// <summary>
		/// Returns an object with the given Primarykey
		/// </summary>
		/// <param name="primaryKey"></param>
		/// <returns></returns>
		public new T this[object primaryKey]
		{
			get { return (T)base[primaryKey]; }
		}

		/// <summary>
		/// Gets an enumerator
		/// </summary>
		/// <returns></returns>
		public new IEnumerator<T> GetEnumerator()
		{
			return _base.Values.Cast<T>().GetEnumerator();
		}

		public Contacts.Pager.IDataPager<T> CreatePager()
		{
			return new LocalDataPager<T>(this);
		}
	}
}
