#region

using System;
using System.Data;
using JPB.DataAccess.Contacts;
using JPB.DataAccess.Manager;

#endregion

namespace JPB.DataAccess.Helper
{
	/// <summary>
	///     Example Implimentation of IQueryParameter
	/// </summary>
	public class QueryParameter : IQueryParameter
	{
		private Type _sourceType;
		private object _value;


		/// <summary>
		///     Wraps a Query Parameter with a name and value. This defines the type based on the value
		/// </summary>
		public QueryParameter(string name, object value)
		{
			Name = name;
			Value = value ?? DBNull.Value;
			if (value != null)
			{
				SourceType = value.GetType();
			}
			else
			{
				SourceType = DBNull.Value.GetType();
			}
		}

		/// <summary>
		///     Wraps a Query Parameter with a name and value
		/// </summary>
		public QueryParameter(string name, object value, Type valType)
		{
			Name = name;
			Value = value;
			SourceType = valType;
		}

		/// <summary>
		///     Wraps a Query Parameter with a name and value
		/// </summary>
		public QueryParameter(string name, object value, DbType valType)
		{
			Name = name;
			Value = value;
			SourceDbType = valType;
		}


		/// <summary>
		///     Renders the current object
		/// </summary>
		/// <returns></returns>
		public string Render()
		{
			var sb = new ConsoleStringBuilderInterlaced();
			Render(sb);
			return sb.ToString();
		}

		internal void Render(ConsoleStringBuilderInterlaced sb)
		{
			var value = "{Null}";
			if (Value != null)
			{
				value = Value.ToString();
			}
			sb.AppendInterlacedLine("new QueryParameter {")
				.Up()
				.AppendInterlacedLine("Name = {0},", Name)
				.AppendInterlacedLine("Value.ToString = {0}", value)
				.AppendInterlacedLine("SourceType = {0}", SourceType.ToString())
				.AppendInterlacedLine("SourceDbType = {0}", SourceDbType)
				.Down()
				.AppendInterlaced("}");
		}

		/// <summary>
		///     Returns a <see cref="System.String" /> that represents this instance.
		/// </summary>
		/// <returns>
		///     A <see cref="System.String" /> that represents this instance.
		/// </returns>
		public override string ToString()
		{
			return Render();
		}

		#region IQueryParameter Members

		/// <summary>
		///     The name of the Parameter
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		///     The value of the Parameter
		/// </summary>
		public object Value
		{
			get { return _value; }
			set
			{
				SourceType = value == null ? DBNull.Value.GetType() : value.GetType();
				_value = value;
			}
		}

		/// <summary>
		///     The C# Type of the Parameter generated from SourceDbType
		/// </summary>
		public Type SourceType
		{
			get { return _sourceType; }
			set
			{
				_sourceType = value;
				var dbType = DbAccessLayer.Map(value);
				if (dbType != null)
				{
					SourceDbType = dbType.Value;
				}
			}
		}

		/// <summary>
		///     The SQL Type of the Parameter generated from SourceType
		/// </summary>
		public DbType SourceDbType { get; set; }

		/// <inheritdoc />
		public IQueryParameter Clone()
		{
			return new QueryParameter(Name, Value, SourceDbType);
		}

		#endregion
	}
}