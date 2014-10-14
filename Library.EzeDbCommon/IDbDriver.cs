using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Libraries.EzeDbCommon
{
	public interface IDbDriver
	{
		/// <summary>
		///     Gets or sets the connection string builder.
		/// </summary>
		/// <value>
		///     The connection string builder.
		/// </value>
		ConnectionStringBuilder ConnectionStrBuilder { get; set; }

		/// <summary>
		/// Gets or sets the connection string.
		/// </summary>
		/// <value>
		/// The connection string.
		/// </value>
		string ConnectionString { get; set; }

		/// <summary>
		///     Gets the database driver name.
		/// </summary>
		/// <value>
		///     The database driver name.
		/// </value>
		string Name { get; }

		/// <summary>
		///     Tests the connection.
		/// </summary>
		/// <returns></returns>
		ConnectionStatus TestConnection(DbConnection connection);

		/// <summary>
		///     Tests the connection.
		/// </summary>
		/// <returns></returns>
		ConnectionStatus TestConnection();

		/// <summary>
		///     the Master data base connection.
		/// </summary>
		/// <returns></returns>
		DbConnection MasterDataBaseConnection();

		/// <summary>
		///     the current data base(Not Master DataBase) connection.
		/// </summary>
		/// <returns></returns>
		DbConnection CurrentDataBaseConnection();

		/// <summary>
		///     News the connection(Not master database connection).
		/// </summary>
		/// <returns></returns>
		DbConnection NewConnection();

		/// <summary>
		///     News the command.
		/// </summary>
		/// <param name="statement">The command statement.</param>
		/// <param name="connection">The database connection.</param>
		/// <returns></returns>
		DbCommand NewCommand(string statement, DbConnection connection);

		/// <summary>
		///     News the parameter.
		/// </summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <returns></returns>
		DbParameter NewParameter(string name, object value);

		/// <summary>
		///     Wraps the exception.
		/// </summary>
		/// <param name="message">The message.</param>
		/// <param name="ex">The exception.</param>
		/// <param name="wrapped">The wrapped exception.</param>
		/// <returns></returns>
		bool WrapException(string message, Exception ex, out Exception wrapped);

		/// <summary>
		///     the Databases schema changed.
		/// </summary>
		/// <param name="newSchema">The new schema.</param>
		void DatabaseSchemaChanged(int newSchema);

		/// <summary>
		///     Creates the database.
		/// </summary>
		/// <param name="databaseName">The database name.</param>
		void CreateDatabase(string databaseName);

		/// <summary>
		///     Creates the database.
		/// </summary>
		/// <param name="databaseName">The database name.</param>
		/// <param name="connection"></param>
		void CreateDatabase(string databaseName, DbConnection connection);

		/// <summary>
		/// Drops the database.
		/// </summary>
		/// <param name="databaseName">Name of the database.</param>
		void DropDatabase(string databaseName);

		/// <summary>
		/// Adds the user.
		/// </summary>
		/// <param name="dbPass">The database pass.</param>
		/// <param name="connection">The connection.</param>
		/// <param name="userName">Name of the user.</param>
		void AddUser(string dbPass, DbConnection connection, string userName = "root");

		/// <summary>
		///     Determines whether [is database exist] [the specified database name].
		/// </summary>
		/// <param name="databaseName">Name of the database.</param>
		/// <returns>
		///     <c>true</c> if [is database exist] [the specified database name]; otherwise, <c>false</c>.
		/// </returns>
		bool IsDatabaseExist(string databaseName);

		/// <summary>
		/// Determines whether [is table exist] [the specified table name].
		/// </summary>
		/// <param name="tableName">Name of the table.</param>
		/// <returns>
		///   <c>true</c> if [is table exist] [the specified table name]; otherwise, <c>false</c>.
		/// </returns>
		bool IsTableExist(string tableName);

		/// <summary>
		/// Determines whether [is column exists in table] [the specified table name].
		/// </summary>
		/// <param name="tableName">Name of the table.</param>
		/// <param name="columnName">Name of the column.</param>
		/// <returns>
		///   <c>true</c> if [is column exists in table] [the specified table name]; otherwise, <c>false</c>.
		/// </returns>
		bool IsColumnExistsInTable(string tableName, string columnName);

		/// <summary>
		/// Wraps the data reader.
		/// </summary>
		/// <param name="dataReader">The data reader.</param>
		/// <returns></returns>
		IDataReader WrapDataReader(IDataReader dataReader);
	}
}
