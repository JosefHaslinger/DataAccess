#region

using System;
using System.Configuration;
using System.Text;
using JPB.DataAccess.DbInfoConfig;
using JPB.DataAccess.Manager;
using JPB.DataAccess.Tests.Base.TestModels.CheckWrapperBaseTests.MetaData;
using NUnit.Framework.Internal.Commands;
using JPB.DataAccess.Tests.Overwrite.Framework.MySql;

#endregion

namespace JPB.DataAccess.Tests.Overwrite
{
	public class MySqlManager : IManagerImplementation
	{
		private DbAccessLayer _expectWrapper;
		private string _connectionString;
		private readonly StringBuilder _errorText = new StringBuilder();

		public MySqlManager()
		{
			ConnectionType = "RDBMS.MySql.DefaultConnection";
			if (Environment.GetEnvironmentVariable("APPVEYOR") == "True")
			{
				ConnectionType = "CiConnectionMySQL";
			}
		}

		public string ConnectionType { get; set; }

		public DbAccessLayer GetWrapper(DbAccessType type, string testName)
		{
			if (ConfigurationManager.AppSettings["RDBMS.MySql.StartManagedServer"] == "True")
			{
				if (!MySqlConnectorInstance.Instance.HasStarted)
				{
					MySqlConnectorInstance.Instance.CreateDatabaseFiles().Wait();
					MySqlConnectorInstance.Instance.RunMySql();
				}
			}
			var dbname = string.Format("YAORM_TestDb_{0}", testName);
			if (_expectWrapper != null)
			{
				_expectWrapper.Database.CloseAllConnection();
			}

			//Darn MySql with only 64 chars?
			if (dbname.Length > 64)
			{
				dbname = dbname.Substring(dbname.Length - 64);
			}

			var redesginDatabase = string.Format(
				"DROP DATABASE IF EXISTS {0};",
				dbname);

			_expectWrapper = new DbAccessLayer(DbAccessType, ConnectionString, new DbConfig(true));
			_expectWrapper.ExecuteGenericCommand(_expectWrapper.Database.CreateCommand(redesginDatabase));
			_expectWrapper.ExecuteGenericCommand(
				_expectWrapper.Database.CreateCommand(string.Format("CREATE DATABASE {0};", dbname)));
			_expectWrapper = new DbAccessLayer(DbAccessType, string.Format(ConnectionString + "Database={0};", dbname));
			foreach (var databaseMeta in MetaManager.DatabaseMetas)
			{
				_expectWrapper.ExecuteGenericCommand(_expectWrapper.Database.CreateCommand(databaseMeta.Value.CreationCommand(DbAccessType)));
			}
			//_expectWrapper.ExecuteGenericCommand(_expectWrapper.Database.CreateCommand("CREATE PROC TestProcA " +
			//																		 "AS BEGIN " +
			//																		 "SELECT * FROM Users " +
			//																		 "END"));
			//_expectWrapper.ExecuteGenericCommand(_expectWrapper.Database.CreateCommand("CREATE PROC TestProcB @bigThen INT " +
			//																		 "AS BEGIN " +
			//																		 "SELECT * FROM Users us WHERE @bigThen > us.User_ID " +
			//																		 "END "));
			return _expectWrapper;
		}

		public DbAccessType DbAccessType
		{
			get { return DbAccessType.MySql; }
		}

		public string ConnectionString
		{
			get
			{
				if (_connectionString != null)
				{
					return _connectionString;
				}
				_connectionString = ConfigurationManager.ConnectionStrings[ConnectionType].ConnectionString;
				_errorText.AppendLine("-------------------------------------------");
				_errorText.AppendLine("Connection String");
				_errorText.AppendLine(_connectionString);
				return _connectionString;
			}
		}

		public void FlushErrorData()
		{
			Console.WriteLine(_errorText.ToString());
			_errorText.Clear();
		}

		public void Clear()
		{
			if (_expectWrapper != null)
			{
				_expectWrapper.Database.CloseAllConnection();
			}
		}
	}
}