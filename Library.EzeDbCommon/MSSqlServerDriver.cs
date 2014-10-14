using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Libraries.EzeDbCommon
{
	internal class MSSqlServerDriver : IDbDriver
	{
		#region lifetime

		public MSSqlServerDriver(ConnectionStringBuilder connectionStringBuilder)
		{
			if (connectionStringBuilder == null)
			{
				throw new ArgumentNullException("connectionStringBuilder",
												"The parameter connectionStringBuilder is null. Please check it.");
			}
			ConnectionStrBuilder = connectionStringBuilder;
		}

		public MSSqlServerDriver(string connectionString)
		{
			ConnectionString = connectionString;
		}

		#endregion

		public const string DriverName = "SqlServer";

		/// <summary>
		///     Gets or sets the connection string builder.
		/// </summary>
		/// <value>
		///     The connection string builder.
		/// </value>
		public ConnectionStringBuilder ConnectionStrBuilder { get; set; }

		public string ConnectionString { get; set; }

		/// <summary>
		///     Gets the database driver name.
		/// </summary>
		/// <value>
		///     The database driver name.
		/// </value>
		public string Name
		{
			get { return "SqlServer"; }
		}

		/// <summary>
		/// Gets or sets the database backuprestore helper.
		/// </summary>
		/// <value>
		/// The database backuprestore helper.
		/// </value>
		//public IDBBackupRestore DatabaseBackupRestore
		//{
		//	get
		//	{
		//		return databaseBackupRestore;
		//	}
		//}

		public ConnectionStatus TestConnection()
		{
			return TestConnection(NewConnection());
		}

		public ConnectionStatus TestConnection(DbConnection connection)
		{
			try
			{
				if (connection.State == ConnectionState.Closed || connection.State == ConnectionState.Broken)
				{
					connection.Open();
				}
				return ConnectionStatus.Valid;
			}
			catch (SqlException ex)
			{
				switch (ex.Number)
				{
					case 15343:
					case 18456:
						return ConnectionStatus.InvalidUsernameOrPassword;
					case 911:
					case 4060:
						return ConnectionStatus.InvalidDatabase;
					case 208:
						return ConnectionStatus.InvalidTable;
					default:
						return ConnectionStatus.Unknown;
				}
			}
			catch
			{
				return ConnectionStatus.Unknown;
			}
		}

		public DbConnection MasterDataBaseConnection()
		{
			//if (string.IsNullOrEmpty(ConnectionString))
			//{
			//    return new SqlConnection(ConnectionStrBuilder.MasterDataBaseString);
			//}
			//return new SqlConnection(ConnectionString);
			return new SqlConnection(ConnectionStrBuilder.MasterDataBaseString);
		}

		public DbConnection CurrentDataBaseConnection()
		{
			return new SqlConnection(ConnectionStrBuilder.CurrentDataBaseString);
		}

		public DbConnection NewConnection()
		{
			if (string.IsNullOrEmpty(ConnectionString))
			{
				return new SqlConnection(ConnectionStrBuilder.CurrentDataBaseString);
			}
			return new SqlConnection(ConnectionString);
		}

		public DbCommand NewCommand(string statement, DbConnection connection)
		{
			if (!(connection is SqlConnection))
			{
				throw new ArgumentException(@"Not a SqlServerConnection", "connection");
			}
			return new SqlCommand(statement, (SqlConnection)connection);
		}

		public DbParameter NewParameter(string name, object value)
		{
			if (value is Guid)
			{
				var guid = (Guid)value;
				if (guid == Guid.Empty)
				{
					//value = null;
					value = DBNull.Value;
					var sqlParameter = new System.Data.SqlClient.SqlParameter(name, SqlDbType.VarBinary, 16)
					   {
						   Value = value
					   };
					return sqlParameter;
				}
				//if (DbFactory.Instance.Schema == 2)
				//{
				//	byte[] guidBytes = guid.ToByteArray();
				//	if (guidBytes.Length > 0)
				//	{
				//		// mysql before 5.0.15 strips trailing spaces, so when we query (like for concurrency checks), we need to strip them too.
				//		int lengthWithoutSpaces = guidBytes.Length;
				//		while (guidBytes[lengthWithoutSpaces - 1] == 0x20)
				//		{
				//			--lengthWithoutSpaces;
				//		}
				//		if (lengthWithoutSpaces != guidBytes.Length)
				//		{
				//			Array.Resize(ref guidBytes, lengthWithoutSpaces);
				//		}
				//	}
				//	value = guidBytes;
				//}
				//else
				//{
				//	value = guid.ToString("D");
				//}
			}
			else if (value is DateTimeOffset)
			{
				var baseDateTime = (DateTimeOffset)value;

				value = Config.ToDateTimeStamp(baseDateTime);
			}
			else if (value is DateTime)
			{
				var baseDateTime = (DateTime)value;

				value = Config.ToDateTimeStamp(baseDateTime);
			}

			return new System.Data.SqlClient.SqlParameter(name, value);
		}

		public bool WrapException(string message, Exception ex, out Exception wrapped)
		{
			var sqlServerException = ex as SqlException;
			if (sqlServerException == null)
			{
				wrapped = null;
				return false;
			}

			message = message + Environment.NewLine + "SqlServer Error: " + sqlServerException.Number;

			if (sqlServerException.Number == 1205 || sqlServerException.Number == 1222)
			{
				// 1205 is deadlock and 1222 is lock timeout exceeded
				//wrapped = new DeadlockException(message, ex);
				wrapped = new Exception(message, ex);
			}
			else
			{
				wrapped = new ApplicationException(message, ex);
			}
			return true;
		}

		public void DatabaseSchemaChanged(int newSchema)
		{
		}

		public void CreateDatabase(string databaseName)
		{
			using (DbConnection connection = MasterDataBaseConnection())
			{
				connection.Open();
				CreateDatabase(databaseName, connection);
			}
		}

		public void CreateDatabase(string databaseName, DbConnection connection)
		{
			if (connection.State == ConnectionState.Closed || connection.State == ConnectionState.Broken)
			{
				connection.Open();
			}
			DbCommand command = NewCommand(string.Format(@"IF EXISTS ( SELECT	*
																	   FROM	    sys.databases d
																	   WHERE    d.name = '{0}' ) 
															  ALTER DATABASE [{0}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE", databaseName), connection);
			command.ExecuteNonQuery();
			command = NewCommand(string.Format(@"IF EXISTS ( SELECT	*
															 FROM	sys.databases d
															 WHERE	d.name = '{0}' ) 
													DROP DATABASE [{0}]", databaseName), connection);
			command.ExecuteNonQuery();
			command = NewCommand(string.Format("CREATE DATABASE [{0}]", databaseName), connection);
			command.ExecuteNonQuery();
			command = NewCommand(string.Format("USE [{0}]", databaseName), connection);
			command.ExecuteNonQuery();
		}

		/// <summary>
		/// Drops the database.
		/// </summary>
		/// <param name="databaseName">Name of the database.</param>
		public void DropDatabase(string databaseName)
		{
			using (DbConnection connection = MasterDataBaseConnection())
			{
				connection.Open();
				DbCommand command = NewCommand(string.Format(@"IF EXISTS ( SELECT	*
																	   FROM	    sys.databases d
																	   WHERE    d.name = '{0}' ) 
															  ALTER DATABASE [{0}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE", databaseName), connection);
				command.ExecuteNonQuery();
				command = NewCommand(string.Format(@"IF EXISTS ( SELECT	*
															 FROM	sys.databases d
															 WHERE	d.name = '{0}' ) 
													DROP DATABASE [{0}]", databaseName), connection);
				command.ExecuteNonQuery();
				connection.Close();
			}
		}

		/// <summary>
		/// Adds the user.
		/// </summary>
		/// <param name="dbPass">The database pass.</param>
		/// <param name="connection">The connection.</param>
		/// <param name="userName">Name of the user.</param>
		public void AddUser(string dbPass, DbConnection connection, string userName = "root")
		{
			if (connection.State == ConnectionState.Closed || connection.State == ConnectionState.Broken)
			{
				connection.Open();
			}
			var command = connection.CreateCommand();
			foreach (string statement in new[]
				{
					string.Format(@"IF EXISTS ( SELECT	* FROM	sys.syslogins WHERE	name = '{0}') 
										DROP LOGIN [{0}]",userName),					
					string.Format(@"IF EXISTS ( SELECT	* FROM	sys.sysusers WHERE	name = '{0}' ) 
										DROP USER [{0}]",userName),					
					string.Format("CREATE LOGIN [{0}] WITH PASSWORD='{1}'",userName,dbPass),
					string.Format("ALTER SERVER ROLE [sysadmin] ADD MEMBER [{0}]",userName),
					string.Format("CREATE USER [{0}] FOR LOGIN [{0}]",userName),
					string.Format("exec sp_addrolemember 'db_owner', '{0}'",userName)
				})
			{
				command.CommandText = statement;
				command.ExecuteNonQuery();
			}
		}

		/// <summary>
		///     Determines whether [is database exist] [the specified database name].
		/// </summary>
		/// <param name="databaseName">Name of the database.</param>
		/// <returns>
		///     <c>true</c> if [is database exist] [the specified database name]; otherwise, <c>false</c>.
		/// </returns>
		public bool IsDatabaseExist(string databaseName)
		{
			using (DbConnection conn = MasterDataBaseConnection())
			{
				conn.Open();
				DbCommand command = conn.CreateCommand();
				command.CommandText = "SELECT name FROM sys.sysdatabases s WHERE s.name=@databaseName";
				command.Parameters.Add(new System.Data.SqlClient.SqlParameter("@databaseName", databaseName));
				DbDataReader reader = command.ExecuteReader();
				return reader.HasRows;
			}
		}

		/// <summary>
		/// Determines whether [is table exist] [the specified table name].
		/// </summary>
		/// <param name="tableName">Name of the table.</param>
		/// <returns>
		///   <c>true</c> if [is table exist] [the specified table name]; otherwise, <c>false</c>.
		/// </returns>
		public bool IsTableExist(string tableName)
		{
			using (DbConnection conn = MasterDataBaseConnection())
			{
				conn.Open();
				DbCommand command = conn.CreateCommand();
				command.CommandText = "SELECT name FROM sys.sysobjects WHERE name=@tableName";
				command.Parameters.Add(new System.Data.SqlClient.SqlParameter("@tableName", tableName));
				DbDataReader reader = command.ExecuteReader();
				return reader.HasRows;
			}
		}

		/// <summary>
		/// Determines whether [is column exists in table] [the specified table name].
		/// </summary>
		/// <param name="tableName">Name of the table.</param>
		/// <param name="columnName">Name of the column.</param>
		/// <returns>
		///   <c>true</c> if [is column exists in table] [the specified table name]; otherwise, <c>false</c>.
		/// </returns>
		public bool IsColumnExistsInTable(string tableName, string columnName)
		{
			using (DbConnection conn = MasterDataBaseConnection())
			{
				conn.Open();
				DbCommand command = conn.CreateCommand();
				command.CommandText = "SELECT name FROM sys.syscolumns WHERE name=@columnName AND id=OBJECT_ID(@tableName)";
				command.Parameters.Add(new System.Data.SqlClient.SqlParameter("@columnName", columnName));
				command.Parameters.Add(new System.Data.SqlClient.SqlParameter("@tableName", tableName));
				DbDataReader reader = command.ExecuteReader();
				return reader.HasRows;
			}
		}

		/// <summary>
		/// Wraps the data reader.
		/// </summary>
		/// <param name="dataReader">The data reader.</param>
		/// <returns></returns>
		public IDataReader WrapDataReader(IDataReader dataReader)
		{
			//return new SqlServerDataReaderWrapperImpl(dataReader);
			return null;
		}
	}
}
