﻿namespace JPB.DataAccess.Manager
{
	/// <summary>
	///     Defines a Common set of DBTypes
	/// </summary>
	public enum DbAccessType
	{
		/// <summary>
		///     For Developing
		///     Not itend for your use!
		/// </summary>
		Experimental = -1,

		/// <summary>
		///     default and undefined bevhaior
		/// </summary>
		Unknown = 0,

		/// <summary>
		///     Defines the MsSQL Type as a Target database
		/// </summary>
		MsSql,

		/// <summary>
		///     Defines the MySQL Type as a Target database
		///     Not as tested as the MsSQL type
		/// </summary>
		MySql,

		/// <summary>
		///     Defines the MsSQL Type as a Target database
		///     Not tested!
		/// </summary>
		OleDb,

		/// <summary>
		///     Defines the MsSQL Type as a Target database
		///     Not Tested!
		/// </summary>
		Obdc,

		/// <summary>
		///     Defines the MsSQL Type as a Target database
		///     Not Tested
		/// </summary>
		SqLite
	}
}