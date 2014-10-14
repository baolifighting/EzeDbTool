#region Imports

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Reflection;

#endregion

namespace Libraries.EzeDbCommon
{
	public delegate void SqlExecutionBlock(DataReader ddr);

	#region Class DataReader

	public class DataReader : IDisposable
	{
		#region Static Fields

		public static IStatisticsFunctionWrapperFactory QueryWrapperFactory;
		public static IStatisticsFunctionWrapperFactory NonQueryWrapperFactory;
		public static IStatisticsFunctionWrapperFactory ScalarWrapperFactory;
		public static IStatisticsFunctionWrapperFactory CommitWrapperFactory;
		public static IStatisticsFunctionWrapperFactory WaitingForLockWrapperFactory;

		private static readonly bool SettingQueryLogging;

		private static readonly string Separator = new string('-', 80);

		#endregion

		#region Fields

		private readonly DbConnection _sqlConnection;
		protected DbTransaction SqlTransaction;
		private bool _logQueries;

		#endregion

		#region Properties

		public bool LogQueries
		{
			get { return _logQueries; }
			set { _logQueries = value; }
		}

		public DbConnection Connection
		{
			get { return _sqlConnection; }
		}

		public bool InTransaction
		{
			get { return SqlTransaction != null; }
		}

		#endregion

		#region Lifetime

		static DataReader()
		{
			// Initialize a static boolean that will indicate whether we want query logging in the data reader. 
			// We can't use the QueryLogging class because then we would go infinitely recursive.
			//var settingValueQueryStatement = DbFactory.Instance.Driver.SqlStatementWarehouse.GetSelectSettingValueQuery("QueryLogging");
			//try
			//{
			//	using (DbConnection sqlConnection = DbFactory.Instance.NewConnection())
			//	{
			//		object result;
			//		sqlConnection.Open();
			//		using (
			//			DbCommand sqlCommand =
			//				DbFactory.Instance.NewCommand(settingValueQueryStatement, sqlConnection)
			//			)
			//		{
			//			result = sqlCommand.ExecuteScalar();
			//		}

			//		if (result != null && result != DBNull.Value)
			//		{
			//			SettingQueryLogging = ((string)result).ToLower() == bool.TrueString.ToLower();
			//		}
			//		else
			//		{
			//			SettingQueryLogging = true;
			//		}
			//	}
			//}
			//catch (Exception e)
			//{
			//	//Runtime.Log.Error("failed to read query logging value: " + e);
			//	SettingQueryLogging = true;
			//}
		}

		public DataReader()
			: this(true)
		{
		}

		public DataReader(bool logQueries)
		{
			_sqlConnection = DbFactory.Instance.NewConnection();
			_sqlConnection.Open();

			_logQueries = logQueries;
		}

		#endregion

		#region Methods

		public void Dispose()
		{
			try
			{
				if (_sqlConnection != null)
				{
					if (SqlTransaction != null)
					{
						Rollback();
					}
					_sqlConnection.Dispose();
				}
			}
			catch (Exception ex)
			{
				//Runtime.Log.Error("Error closing SQL connection!", ex);
			}
		}

		/// <summary>
		///     Create a data reader and use it to execute the block that gets passed in. If it throws
		///     a deadlock exception, toss that data reader out and try again with a new one up until
		///     DeadlockException.MaxRetryCount
		/// </summary>
		/// <param name="block"></param>
		public static void WithDeadlockRetry(SqlExecutionBlock block)
		{
			int tryCount = 0;
			bool success = false;

			while (!success)
			{
				tryCount++;
				try
				{
					using (var ddr = new DataReader())
					{
						block(ddr);
						success = true;
					}
				}
				catch (DeadlockException dex)
				{
					if (tryCount < DeadlockException.MaxRetryCount)
					{
						//Runtime.Log.Warn("Deadlock Exception in WithDeadlockRetry. Try count is " + tryCount);
					}
					else
					{
						//Runtime.Log.Error("Max retry count exceeded in WithDeadlockRetry", dex);
						throw;
					}
				}
			}
		}

		public void BeginTransaction(bool isReadUnCommited = false)
		{
			//TODO: this may be stricter than we need
			//SqlTransaction = _sqlConnection.BeginTransaction(IsolationLevel.RepeatableRead);
			// Use ReadUncommitted when retrieving monikers or references to resolve deadlock between SELECT and UPDATE which happens in high load
			//For example: I have a stored procedure that performs a join of TableB to TableA:
			// SELECT <--- Nested <--- TableA
			//             Loop   <--
			//                      |
			//                      ---TableB
			//At the same time, in a transaction, rows are inserted into TableA, and then into TableB.
			//This situation is occasionally causing deadlocks, as the stored procedure select grabs rows from TableB, 
			// while the insert adds rows to TableA, and then each wants the other to let go of the other table:
			//INSERT     SELECT
			//=========  ========
			//Lock A     Lock B
			//Insert A   Select B
			//Want B     Want A
			//....deadlock...
			if (isReadUnCommited)
			{
				SqlTransaction = _sqlConnection.BeginTransaction(IsolationLevel.ReadUncommitted);
			}
			else
			{
						SqlTransaction = _sqlConnection.BeginTransaction(IsolationLevel.ReadCommitted);
			}
		}

		public void Commit()
		{
			if (SqlTransaction != null)
			{
				using (GetCommitFunctionWrapper())
				{
					SqlTransaction.Commit();
				}
				SqlTransaction = null;
			}
			else
			{
				throw new ApplicationException("Tried to commit when no transaction was active");
			}
		}

		public void Rollback()
		{
			if (SqlTransaction != null)
			{
				SqlTransaction.Rollback();
				SqlTransaction = null;
			}
			else
			{
				throw new ApplicationException("Tried to commit when no transaction was active");
			}
		}
		/// <summary>
		/// Add 'N' prefix to support unicode string in sqlserver
		/// </summary>
		/// <param name="inputSql">Input SQL</param>
		/// <returns></returns>
		private string addNPrefix(string inputSql)
		{
			//if (DbFactory.DbType == DataBaseType.MySql)
			//{
			//	return inputSql;
			//}

			string[] array = inputSql.Split('\'');
			for (int i = 0; i < array.Length; i++)
			{
				if (i > 0)
				{
					//Skip if already has N prefix or continuous quotation mark which is part of a string column
					if (!string.IsNullOrEmpty(array[i - 1]) && !array[i - 1].EndsWith("N"))
					{
						array[i - 1] += "N";
					}
					i++;
				}
			}
			string outputSql = string.Join("'", array);
			return outputSql;
		}

		public int ExecuteNonQuery(string statementIn, SqlParameter parameter)
		{
			return ExecuteNonQuery(statementIn, new[] { parameter });
		}

		public int ExecuteNonQuery(string statementIn, IEnumerable<SqlParameter> parameters = null)
		{
			if (statementIn == null)
			{
				return 0;
			}
			string temp = statementIn.Trim();
			if (temp.Length == 0)
			{
				return 0;
			}
			string statement = addNPrefix(temp);
			if (_logQueries && SettingQueryLogging)
			{
				MethodBase method = new StackFrame(1, false).GetMethod();
				Console.WriteLine("Calling Method: " + method.DeclaringType + "." + method.Name + " Non-query:" +
								  Environment.NewLine + statement);
				//Runtime.Log.Info("Calling Method: " + method.DeclaringType + "." + method.Name + " Non-query:" +
				//				 Environment.NewLine + statement);
				Console.WriteLine(Separator);
			}

			using (DbCommand sqlCommand = DbFactory.Instance.NewCommand(statement, _sqlConnection))
			{
				sqlCommand.CommandType = CommandType.Text;

				if (SqlTransaction != null)
				{
					sqlCommand.Transaction = SqlTransaction;
				}

				AddParameters(sqlCommand, parameters);

				try
				{
					//#if DEBUG
					using (GetNonQueryFunctionWrapper(statement))
					{
						//#endif
						return sqlCommand.ExecuteNonQuery();
						//#if DEBUG
					}
					//#endif
				}
				catch (Exception ess)
				{
					string errorString = string.Format("Exception on non-query '{0}'", statement);
					Exception wrapped;
					if (DbFactory.Instance.WrapException(errorString, ess, out wrapped))
					{
						throw wrapped;
					}
					throw new ApplicationException(errorString, ess);
				}
			}
		}

		public IDataReader ExecuteQuery(string queryIn, SqlParameter parameter = null)
		{
			SqlParameter[] parameters = null;

			if (parameter != null)
			{
				parameters = new[] { parameter };
			}

			return ExecuteQuery(queryIn, parameters);
		}

		public IDataReader ExecuteQuery(string queryIn, IEnumerable<SqlParameter> parameters)
		{
			if (queryIn == null)
			{
				return null;
			}
			string temp = queryIn.Trim();
			if (temp.Length == 0)
			{
				return null;
			}
			string query = addNPrefix(temp);
			if (_logQueries && SettingQueryLogging)
			{
				MethodBase method = new StackFrame(1, false).GetMethod();
				Console.WriteLine("Calling Method: " + method.DeclaringType + "." + method.Name + " Query:" +
								  Environment.NewLine + query);
				//Runtime.Log.Info("Calling Method: " + method.DeclaringType + "." + method.Name + " Query:" +
				//				 Environment.NewLine + query);
				Console.WriteLine(Separator);
			}

			DbCommand sqlCommand = DbFactory.Instance.NewCommand(query, _sqlConnection);
			sqlCommand.CommandType = CommandType.Text;
			if (SqlTransaction != null)
			{
				sqlCommand.Transaction = SqlTransaction;
			}

			AddParameters(sqlCommand, parameters);

			IDataReader sqlDataReader;
			//#if DEBUG
			using (GetQueryFunctionWrapper(query))
			{
				//#endif
				try
				{
					sqlDataReader = sqlCommand.ExecuteReader();
				}
				catch (Exception ess)
				{
					string errorString = string.Format("Exception on query '{0}'", query);
					Exception wrapped;
					if (DbFactory.Instance.WrapException(errorString, ess, out wrapped))
					{
						throw wrapped;
					}
					throw new ApplicationException(errorString, ess);
				}
				//#if DEBUG
			}
			//#endif
			//return new DataReaderWrapper(sqlDataReader);
			return new SqlServerDataReaderWrapperImpl(sqlDataReader);
		}

		private void AddParameters(DbCommand sqlCommand, IEnumerable<SqlParameter> parameters)
		{
			if (parameters == null)
			{
				return;
			}

			foreach (SqlParameter parameter in parameters)
			{
				try
				{
					DbParameter dbParameter = DbFactory.Instance.NewParameter(parameter.Name, parameter.Value);
					sqlCommand.Parameters.Add(dbParameter);
				}
				catch (Exception ex)
				{
					string errorString = ex.Message +
										 string.Format(
											 " DataReader line 224: sqlCommand.Parameters.Add ERROR in parameter {0} value {1}",
											 parameter.Name, parameter.Value);
					//Runtime.Log.Error(errorString);
				}
			}
		}

		public object ExecuteScalar(string query, SqlParameter parameter = null)
		{
			SqlParameter[] parameters = null;

			if (parameter != null)
			{
				parameters = new[] { parameter };
			}

			return ExecuteScalar(query, parameters);
		}

		public object ExecuteScalar(string inpQuery, IEnumerable<SqlParameter> parameters)
		{
			string query = addNPrefix(inpQuery);
			object scalar = null;
			try
			{
				using (DbCommand sqlCommand = DbFactory.Instance.NewCommand(query, _sqlConnection))
				{
					if (SqlTransaction != null)
					{
						sqlCommand.Transaction = SqlTransaction;
					}

					AddParameters(sqlCommand, parameters);

					//#if DEBUG
					using (GetScalarFunctionWrapper(query))
					{
						//#endif
						try
						{
							scalar = sqlCommand.ExecuteScalar();
						}
						catch (Exception ess)
						{
							string errorString = string.Format("Exception on ExecuteScalar '{0}'", query);
							Exception wrapped;
							if (DbFactory.Instance.WrapException(errorString, ess, out wrapped))
							{
								throw wrapped;
							}
							throw new ApplicationException(errorString, ess);
						}
						//#if DEBUG
					}
					//#endif
				}
			}
			finally
			{
				if (_logQueries && SettingQueryLogging)
				{
					MethodBase method = new StackFrame(1, false).GetMethod();
					Console.WriteLine("Calling Method: " + method.DeclaringType + "." + method.Name + " Scalar:" +
									  Environment.NewLine + query + Environment.NewLine + "Result: " + scalar);
					//Runtime.Log.Info("Calling Method: " + method.DeclaringType + "." + method.Name + " Scalar:" +
					//				 Environment.NewLine + query + Environment.NewLine + "Result: " + scalar);
					Console.WriteLine(Separator);
				}
			}
			return scalar;
		}

		public static string DateTimeToSQL(DateTimeOffset time)
		{
			return String.Format("'{0}'", time.ToString("yyyy-MM-dd HH:mm:ss"));
		}

		#endregion

		#region Private Implementation

		private static IDisposable GetQueryFunctionWrapper(string stmt)
		{
			if (QueryWrapperFactory != null)
			{
				return QueryWrapperFactory.CreateFunctionWrapper(stmt);
			}
			return new DummyWrapper();
		}

		private static IDisposable GetNonQueryFunctionWrapper(string stmt)
		{
			if (NonQueryWrapperFactory != null)
			{
				return NonQueryWrapperFactory.CreateFunctionWrapper(stmt);
			}
			return new DummyWrapper();
		}

		private static IDisposable GetScalarFunctionWrapper(string stmt)
		{
			if (ScalarWrapperFactory != null)
			{
				return ScalarWrapperFactory.CreateFunctionWrapper(stmt);
			}
			return new DummyWrapper();
		}

		private static IDisposable GetCommitFunctionWrapper()
		{
			if (CommitWrapperFactory != null)
			{
				return CommitWrapperFactory.CreateFunctionWrapper(null);
			}
			return new DummyWrapper();
		}

		private static IDisposable GetWaitForLockWrapper()
		{
			if (WaitingForLockWrapperFactory != null)
			{
				return WaitingForLockWrapperFactory.CreateFunctionWrapper("Waiting for DB lock");
			}
			return new DummyWrapper();
		}

		internal class DummyWrapper : IDisposable
		{
			#region IDisposable Members

			public void Dispose()
			{
				// do nothing
			}

			#endregion
		}

		#endregion
	}

	#endregion
}