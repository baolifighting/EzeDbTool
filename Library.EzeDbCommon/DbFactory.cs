using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Libraries.EzeDbCommon
{
	public class DbFactory
	{
		private static DbFactory _instance;
		private IDbDriver _driver;
		private int _schema;
		public static DbFactory Instance
		{
			get
			{
				if (_instance == null)
				{
					lock (typeof(DbFactory))
					{
						if (_instance == null)
						{
							_instance = new DbFactory();
						}
					}
				}
				return _instance;
			}
		}
		public IDbDriver Driver
		{
			get
			{
				DemandInit();
				return _driver;
			}
		}

		public int Schema
		{
			get
			{
				DemandInit();
				return _schema;
			}
			set
			{
				DemandInit();
				_schema = value;
				_driver.DatabaseSchemaChanged(_schema);
			}
		}

		public void Initialize(string connectionString)
		{
			var parser = new DbConnectionStringBuilder
			{
				ConnectionString = connectionString
			};

			Initialize(new MSSqlServerDriver(parser.ConnectionString));
		}

		public void Initialize(IDbDriver driver)
		{
			if (_driver != null)
			{
				throw new InvalidOperationException("We're already been initialized with " + _driver.Name);
			}
			if (driver == null)
			{
				throw new ArgumentNullException();
			}

			using (DbConnection testConn = driver.NewConnection())
			{
				try
				{
					testConn.Open();
					DbCommand command = driver.NewCommand("SELECT * FROM schemaversions", testConn);

					//_schema = Convert.ToInt32(command.ExecuteScalar());
				}
				catch
				{
					//_schema = 0;
				}
			}

			_driver = driver;
		}

		/// <summary>
		/// Gets the database driver.
		/// </summary>
		/// <param name="dbConnectionStringBuilder">The database connection string builder.</param>
		/// <param name="forceLatin1">if set to <c>true</c> [force latin1].</param>
		/// <param name="databaseType">Type of the database.</param>
		/// <returns></returns>
		public IDbDriver GetDbDriver(DbConnectionStringBuilder dbConnectionStringBuilder, bool forceLatin1)
		{
			IDbDriver driver;
			driver = new MSSqlServerDriver(dbConnectionStringBuilder.ConnectionString)
			{
				ConnectionStrBuilder = dbConnectionStringBuilder.GetConnectionStringBuilder()
			};
			return driver;
		}

		public void UnInitialize()
		{
			_driver = null;
		}
		public DbConnection NewConnection()
		{
			DemandInit();
			return _driver.NewConnection();
		}

		public DbCommand NewCommand(string statement, DbConnection connection)
		{
			DemandInit();
			return _driver.NewCommand(statement, connection);
		}

		public DbParameter NewParameter(string name, object value)
		{
			DemandInit();
			return _driver.NewParameter(name, value);
		}

		public IDataReader WrapReader(IDataReader dataReader)
		{
			DemandInit();
			//return new DataReaderWrapper(dataReader);
			return _driver.WrapDataReader(dataReader);
		}

		public bool WrapException(string message, Exception ex, out Exception wrappedException)
		{
			DemandInit();
			return _driver.WrapException(message, ex, out wrappedException);
		}

		private void DemandInit()
		{
			if (_driver == null)
			{
				throw new InvalidOperationException("DbFactory not initialized.");
			}
		}
	}
}
