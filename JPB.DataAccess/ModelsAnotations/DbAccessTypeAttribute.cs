﻿using JPB.DataAccess.Manager;

namespace JPB.DataAccess.ModelsAnotations
{
	/// <summary>
	///     Defines an function or property that is specific for an single database
	/// </summary>
	public abstract class DbAccessTypeAttribute : DataAccessAttribute
	{
		/// <summary>
		///     Defines the Target database this Method creates an Script for
		/// </summary>
		public DbAccessType TargetDatabase { get; set; }
	}
}