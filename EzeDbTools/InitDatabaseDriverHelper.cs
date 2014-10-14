using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Libraries.EzeDbCommon;
using Libraries.EzeDbTools;

namespace EzeDbTools
{
	public class InitDatabaseDriverHelper
	{

		#region  Property

		/// <summary>
		/// Gets or sets the connection string builder item.
		/// </summary>
		/// <value>
		/// The connection string builder item.
		/// </value>
		public ConnectionStringBuilder ConnectionStringBuilderItem { get; set; }
		/// <summary>
		/// Gets the database initialization item.
		/// </summary>
		/// <value>
		/// The database initialization item.
		/// </value>
		public DbInitialization DbInitializationItem { get; private set; }
		/// <summary>
		/// Gets or sets a value indicating whether [is allow create database].
		/// </summary>
		/// <value>
		/// <c>true</c> if [is allow create database]; otherwise, <c>false</c>.
		/// </value>
		public bool IsAllowCreateDatabase { get; set; }
		/// <summary>
		/// Gets or sets a value indicating whether [is allow create password]. if the password is nor correct.
		/// </summary>
		/// <value>
		/// <c>true</c> if [is allow create password]; otherwise, <c>false</c>.
		/// </value>
		public bool IsAllowCreatePassword { get; set; }
		/// <summary>
		/// Gets or sets a value indicating whether [is force latin].
		/// </summary>
		/// <value>
		///   <c>true</c> if [is force latin]; otherwise, <c>false</c>.
		/// </value>
		public bool IsForceLatin { get; set; }
		/// <summary>
		/// Gets or sets the schema value.
		/// </summary>
		/// <value>
		/// The schema value.
		/// </value>
		public int? SchemaValue { get; set; }
		/// <summary>
		/// Gets the database driver.
		/// </summary>
		/// <value>
		/// The database driver.
		/// </value>
		public IDbDriver DbDriver { get; private set; }

		#endregion

		#region LifeTime

		/// <summary>
		/// Initializes a new instance of the <see cref="InitDatabaseDriverHelper"/> class.
		/// </summary>
		/// <param name="dbConnectionStringBuilder">The database connection string builder.</param>
		/// <param name="isAllowCreateDatabase">if set to <c>true</c> [is allow create database].</param>
		/// <param name="isAllowCreatePassword">if set to <c>true</c> [is allow create password].</param>
		/// <param name="isForceLatin">if set to <c>true</c> [is force latin].</param>
		/// <param name="schemaValue">The schema value.</param>
		public InitDatabaseDriverHelper(ConnectionStringBuilder dbConnectionStringBuilder, bool isAllowCreateDatabase = true, bool isAllowCreatePassword = true, bool isForceLatin = false, int? schemaValue = null)
		{
			ConnectionStringBuilderItem = dbConnectionStringBuilder;
			IsAllowCreateDatabase = isAllowCreateDatabase;
			IsAllowCreatePassword = isAllowCreatePassword;
			IsForceLatin = isForceLatin;
			SchemaValue = schemaValue;
		}

		#endregion

		#region Method

		/// <summary>
		/// Initializes the database driver.
		/// </summary>
		/// <returns></returns>
		/// <exception cref="System.ApplicationException">Couldn't initialize connection to database.</exception>
		public bool InitializeDatabaseDriver()
		{
			bool isConnectSuccess = false;
			bool isConnectedToDefaultDatabase = false;
			DbConnectionStringBuilder dbConnectionStringBuilder = new DbConnectionStringBuilder
			{
				ConnectionString = ConnectionStringBuilderItem.CurrentDataBaseString
			};
			//ConnectionStringBuilder connectionStringBuilder = dbConnectionStringBuilder.GetConnectionStringBuilder();
			string databaseName = ConnectionStringBuilderItem.DatabaseConfig;
			string dbUserName = ConnectionStringBuilderItem.UserConfig;
			string dbPass = ConnectionStringBuilderItem.PasswordConfig;
			//DataBaseType dataBaseType = ConnectionStringBuilderItem.DbType;
			string masterDatabaseName = ConnectionStringBuilderItem.MasterDatabaseName;
			if (string.IsNullOrEmpty(databaseName))
			{
				DbInitializationItem = DbInitialization.InvalidDatabase;
			}

			while (!isConnectSuccess)
			{
				DbDriver = DbFactory.Instance.GetDbDriver(dbConnectionStringBuilder, IsForceLatin);
				if (SchemaValue.HasValue)
				{
					DbDriver.DatabaseSchemaChanged(SchemaValue.Value);
				}
				DbDriver.ConnectionStrBuilder = ConnectionStringBuilderItem;
				DbFactory.Instance.UnInitialize();
				DbFactory.Instance.Initialize(DbDriver);

				switch (DbDriver.TestConnection())
				{
					case ConnectionStatus.Valid:
						if (isConnectedToDefaultDatabase || !dbConnectionStringBuilder.ContainsKey(ConnectionStringConst.PasswordConfig))
						{
							if (!IsAllowCreateDatabase)
							{
								DbInitializationItem = DbInitialization.NewDatabase;
								return false;
							}
							using (DbConnection conn = DbDriver.NewConnection())
							{
								conn.Open();
								if (isConnectedToDefaultDatabase)
								{
									DbDriver.CreateDatabase(databaseName, conn);
									DbInitializationItem = DbInitialization.NewDatabase;
									dbConnectionStringBuilder[ConnectionStringConst.DatabaseConfig] = databaseName;
									isConnectedToDefaultDatabase = false;
								}
								if (IsAllowCreatePassword)
								{
									if (!dbConnectionStringBuilder.ContainsKey(ConnectionStringConst.PasswordConfig))
									{
										DbDriver.AddUser(dbPass, conn, dbUserName);
										dbConnectionStringBuilder.Add(ConnectionStringConst.PasswordConfig, dbPass);
										dbConnectionStringBuilder.Add(ConnectionStringConst.UserConfig, dbUserName);
										if (dbConnectionStringBuilder.ContainsKey(ConnectionStringConst.TrustedConnectionConfig))
										{
											dbConnectionStringBuilder.Remove(ConnectionStringConst.TrustedConnectionConfig);
										}
									}
								}
							}
						}
						else
						{
							isConnectSuccess = true;
						}
						break;
					case ConnectionStatus.InvalidUsernameOrPassword:
						if (!IsAllowCreatePassword)
						{
							if (dbConnectionStringBuilder.ContainsKey(ConnectionStringConst.UserConfig) && string.IsNullOrEmpty(dbConnectionStringBuilder[ConnectionStringConst.PasswordConfig] as string))
							{
								DbInitializationItem = DbInitialization.InvalidUserNameOrPassword;
								return false;
							}
							dbConnectionStringBuilder[ConnectionStringConst.PasswordConfig] = string.Empty;
							Console.WriteLine("Couldn't connect to db, retrying with no password.");
						}
						else
						{
							if (dbPass != string.Empty && dbConnectionStringBuilder.Remove(ConnectionStringConst.PasswordConfig))
							{
								if (dbConnectionStringBuilder.ContainsKey(ConnectionStringConst.UserConfig))
								{
									dbConnectionStringBuilder.Remove(ConnectionStringConst.UserConfig);
								}
								dbConnectionStringBuilder[ConnectionStringConst.TrustedConnectionConfig] = true;
								Console.WriteLine("Couldn't connect to db, retrying with no password.");
							}
							else
							{
								DbInitializationItem = DbInitialization.InvalidUserNameOrPassword;
								Console.WriteLine("Couldn't connect to db server with user {0} (not using password).", dbUserName);
								goto default;
							}
						}
						break;
					case ConnectionStatus.InvalidDatabase:
						if (dbConnectionStringBuilder.ContainsKey(ConnectionStringConst.DatabaseConfig))
						{
							dbConnectionStringBuilder[ConnectionStringConst.DatabaseConfig] = masterDatabaseName;
							isConnectedToDefaultDatabase = true;
							Console.WriteLine("Couldn't connect to db {0}.", databaseName);
						}
						else
						{
							Console.WriteLine("Couldn't connect with no db specified?.");
							goto default;
						}
						break;
					default:
						DbInitializationItem = DbInitialization.OtherError;
						DbFactory.Instance.UnInitialize();
						string databaseConnectionInfo =
							string.Format(
								"Couldn't init db connection!{0}ConnectionString:{1}{0}",
								Environment.NewLine, DbDriver.NewConnection().ConnectionString);
						Console.WriteLine(databaseConnectionInfo);
						throw new ApplicationException("Couldn't initialize connection to database.");
				}
			}
			return true;
		}

		#endregion
	}
}
