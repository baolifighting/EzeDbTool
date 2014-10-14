using Libraries.EzeDbCommon;
using System;

namespace EzeDbTools
{
	/// <summary>
	/// Summary description for ExecutionEngine.
	/// </summary>
	public class SimpleExecutionEngine : IExecutionEngine
	{
		#region Variables

		protected DataReader _dbAccess;
		protected Exception _lastException;

		#endregion Variables

		#region Lifetime

		/// <summary>
		/// Provides a bunch of methods for Executing various types of code.
		/// </summary>
		/// <param name="dbAccess">A ThreeVR DataReader object.</param>
		/// <remarks>It's your job to dispose the DataReader when your done with it, as this object just holds on to it and never gets rid of it.</remarks>
		public SimpleExecutionEngine(DataReader dbAccess)
		{
			_dbAccess = dbAccess;
		}

		#endregion Lifetime

		#region Properties

		public DataReader DbAccess
		{
			get
			{
				return _dbAccess;
			}
		}

		#endregion Properties

		#region Methods

		public void NewConnection(DataReader dbAccess)
		{
			_dbAccess = dbAccess;
		}

		/// <summary>
		/// Execute an SQL statement (or a serie of...) contained in a string passed as an argument using the DataReader
		/// helper class.
		/// </summary>
		/// <param name="content">a string representing the statement to execute</param>
		/// <returns>true if OK, false if any error</returns>
		public virtual bool ExecuteSql(string content)
		{
			try
			{
				// For some reason, I've been having trouble with long, multi-action statements
				// Running them one at a time seems to make things better.
				//string[] splitContent = content.Split(";".ToCharArray());
				string[] splitContent = content.Split(new[] { "GO\r\n", "GO\n", "GO\r", ";" }, StringSplitOptions.RemoveEmptyEntries);
				for (int i = 0; i < splitContent.Length; ++i)
				{
					if (splitContent[i] == string.Empty)
					{
						continue;
					}

					// This silly hack looks to see if the ";" was escaped.  
					// If it was, we don't split on it, we just remove the \
					if (splitContent[i].EndsWith(@"\") && i + 1 < splitContent.Length)
					{
						string join = splitContent[i].TrimEnd(@"\".ToCharArray());
						do
						{
							join += ";" + splitContent[++i].TrimEnd(@"\".ToCharArray());
						} while (splitContent[i].EndsWith(@"\") && i + 1 < splitContent.Length);
						string foo = join.Trim();
						if (!string.IsNullOrEmpty(foo))
						{
							_dbAccess.ExecuteNonQuery(foo);
						}
					}
					else
					{
						string foo = splitContent[i].Trim();
						if (!string.IsNullOrEmpty(foo))
						{
							_dbAccess.ExecuteNonQuery(foo);
						}
					}
				}

				return true;
			}
			catch (Exception e)
			{
				_lastException = e;
				return false;
			}
		}

		/// <summary>
		/// Execute C# code contained in a string.
		/// 
		/// The method being called is always named Execute()
		/// </summary>
		/// <param name="content">a string containing the class with the Execute method to run</param>
		/// <returns>true if OK, false if any error</returns>
		//public virtual bool ExecuteCSharp(string content)
		//{
		//	try
		//	{
		//		Assembly assembly = new CompilerManager().CompileSource(content, CompilerManager.Language.Cs);

		//		foreach (Type t in assembly.GetTypes())
		//		{
		//			MethodInfo mi = t.GetMethod(
		//				Constants.DbToolsExecuteMethod, // Method name
		//				BindingFlags.Static | BindingFlags.Public, // Look for a public static method
		//				null,			// Use default binder
		//				new[] { _dbAccess.GetType() },	// Void argument list
		//				null);			// Not used by default binder
		//			if (mi != null)
		//			{
		//				object retVal = mi.Invoke(t, new Object[] { _dbAccess });

		//				if (retVal is bool)
		//				{
		//					return (bool)retVal;
		//				}
		//				return true;
		//			}
		//			Console.WriteLine("The inline_csharp step does not contain a correctly formated Execute method.");
		//			return false;
		//		}
		//		return true;
		//	}
		//	catch (Exception e)
		//	{
		//		_lastException = e;
		//		return false;
		//	}
		//}

		/// <summary>
		/// Execute VisualBasic code contained in a string.
		/// 
		/// The method being called is always named Execute()
		/// </summary>
		/// <param name="content">a string containing the class with the Execute method to run</param>
		/// <returns>true if OK, false if any error</returns>
		//public virtual bool ExecuteVisualBasic(string content)
		//{
		//	try
		//	{
		//		Assembly assembly = new CompilerManager().CompileSource(content, CompilerManager.Language.Js);

		//		foreach (Type t in assembly.GetTypes())
		//		{
		//			MethodInfo mi = t.GetMethod(
		//				Constants.DbToolsExecuteMethod,
		//				BindingFlags.Static | BindingFlags.Public,
		//				null,			// Use default binder
		//				new Type[] { },	// Void argument list
		//				null);			// Not used by default binder
		//			if (mi != null)
		//			{
		//				mi.Invoke(t, null);
		//			}
		//		}

		//		return true;
		//	}
		//	catch (Exception e)
		//	{
		//		_lastException = e;
		//		return false;
		//	}
		//}

		/// <summary>
		/// Execute Js code contained in a string.
		/// 
		/// The method being called is always named Execute()
		/// </summary>
		/// <param name="content">a string containing the class with the Execute method to run</param>
		/// <returns>true if OK, false if any error</returns>
		//public virtual bool ExecuteJs(string content)
		//{
		//	try
		//	{
		//		Assembly assembly = new CompilerManager().CompileSource(content, CompilerManager.Language.Js);

		//		foreach (Type t in assembly.GetTypes())
		//		{
		//			MethodInfo mi = t.GetMethod(
		//				Constants.DbToolsExecuteMethod,
		//				BindingFlags.Static | BindingFlags.Public,
		//				null,			// Use default binder
		//				new Type[] { },	// Void argument list
		//				null);			// Not used by default binder
		//			if (mi != null)
		//			{
		//				mi.Invoke(t, null);
		//			}
		//		}

		//		return true;
		//	}
		//	catch (Exception e)
		//	{
		//		_lastException = e;
		//		return false;
		//	}
		//}

		/// <summary>
		/// Execute an external process (spawning it using a separate shell)
		/// </summary>
		/// <param name="fileLocation">the external process file location (must be accessible somewhere on the FS...)</param>
		/// <returns>true if OK, false if any error</returns>
		//public virtual bool ExecuteProcess(string fileLocation)
		//{
		//	try
		//	{
		//		int index = fileLocation.IndexOf(" ", StringComparison.Ordinal);

		//		if (index != -1)
		//		{
		//			string program = fileLocation.Substring(0, fileLocation.IndexOf(" ", StringComparison.Ordinal));
		//			string arguments = fileLocation.Substring(fileLocation.IndexOf(" ", StringComparison.Ordinal) + 1, fileLocation.Length - index - 1);
		//			Process.Start(program, arguments);
		//		}
		//		else
		//		{
		//			Process.Start(fileLocation, "");
		//		}

		//		return true;
		//	}
		//	catch (Exception e)
		//	{
		//		_lastException = e;
		//		return false;
		//	}
		//}

		/// <summary>
		/// Get the actual DB version from the version table
		/// </summary>
		/// <returns>DB version string from the DB version table</returns>
		public string GetDbVersion()
		{
			//const string strSql = "SELECT max(version) FROM versions";		    
			string version = null;
			try
			{
				//version = _dbAccess.ExecuteScalar(DbFactory.Instance.Driver.SqlStatementWarehouse.GetSelectLastVersionStatement()) as string;
			}
			catch
			{
				return "0.0";
			}

			string revision = null;
			try
			{
				//revision = _dbAccess.ExecuteScalar(DbFactory.Instance.Driver.SqlStatementWarehouse.GetSelectLastRevisionStatement()).ToString();
				//revision = _dbAccess.ExecuteScalar("SELECT revision FROM versions ORDER BY versionid DESC LIMIT 1").ToString();
				//revision = _dbAccess.ExecuteScalar("SELECT TOP 1 revision FROM versions  ORDER BY versionid DESC").ToString();
			}
			catch
			{
				revision = null;
			}

			return revision != null && revision != "0" ? version + "_" + revision : version;
		}

		public bool DoesTableExist(string table, string column)
		{
			try
			{
				if (string.IsNullOrEmpty(table))
				{
					return false;
				}
				if (string.IsNullOrEmpty(column))
				{
					return DbFactory.Instance.Driver.IsTableExist(table);
				}
				return DbFactory.Instance.Driver.IsColumnExistsInTable(table, column);
			}
			catch (Exception)
			{
				return false;
			}
		}
		#endregion
	}
}