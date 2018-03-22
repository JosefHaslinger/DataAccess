﻿using System;
using System.Collections.Generic;
using JPB.DataAccess.Contacts;

namespace JPB.DataAccess.ModelsAnotations
{
	/// <summary>
	///     Provieds a QueryCommand ( parametes not used ) for selection
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, Inherited = false)]
	public sealed class SelectFactoryAttribute : DbAccessTypeAttribute, IQueryFactoryResult
	{
		/// <summary>
		///     Ctor
		/// </summary>
		/// <param name="query"></param>
		public SelectFactoryAttribute(string query)
		{
			Query = query;
			Parameters = null;
		}

		/// <summary>
		///     The Select QueryCommand that are used for selection of this Class
		/// </summary>
		public string Query { get; private set; }

		/// <summary>
		///     Not in USE
		/// </summary>
		public IEnumerable<IQueryParameter> Parameters { get; private set; }
	}
}