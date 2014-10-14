using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Libraries.EzeDbCommon;
using Libraries.EzeDbTools;
using Libraries.EzeDbCommon.Content;

namespace EzeDbTools
{
	public class EzeDbTools
	{
		private static TransactionalExecutionEngine _engine;
		private static readonly ConnectionStringBuilder connectionStrBuilder = new ConnectionStringBuilder();
		[STAThread]
		static void Main(string[] args)
		{
			//ParameterShortcut<string> dbmodFile = new ParameterShortcut<string>(ThreeVR.Libraries.DbTools.Schema.Constants.DbToolsMySqlPatchFile, "DBTOOLS.PATCHFILE", ParameterMergeDirection.ParameterToArgument);
			ParameterShortcut<string> dbHost = new ParameterShortcut<string>("localhost", "DB.HOST", ParameterMergeDirection.ParameterToArgument);
			ParameterShortcut<string> dbName = new ParameterShortcut<string>("vipdb", "DB.NAME", ParameterMergeDirection.ParameterToArgument);
			ParameterShortcut<string> dbPass = new ParameterShortcut<string>("objectiva123@", "DB.PASSWORD", ParameterMergeDirection.ParameterToArgument);
			ParameterShortcut<string> dbUser = new ParameterShortcut<string>("sa", "DB.USER", ParameterMergeDirection.ParameterToArgument);
			ParameterShortcut<bool> enterpriseDb = new ParameterShortcut<bool>(false, "SETTINGS.ENTERPRISE", ParameterMergeDirection.Any);
			ParameterShortcut<bool> createDb = new ParameterShortcut<bool>(true, "DBTOOLS.CREATEDB", ParameterMergeDirection.ParameterToArgument);
			ParameterShortcut<string> dbVersion = new ParameterShortcut<string>(null, "DB.VERSION", ParameterMergeDirection.ParameterToArgument);
            //ParameterShortcut<string> dbType = new ParameterShortcut<string>("sqlserver", "DB.TYPE", ParameterMergeDirection.ParameterToArgument);
			Argument<bool> mergeTransactions = false;
			Argument<bool> listParameters = false;
			Argument<bool> listMods = false;
			ParameterShortcut<bool> content = new ParameterShortcut<bool>(true, "DBTOOLS.CONTENT", ParameterMergeDirection.ParameterToArgument);
			ParameterShortcut<int?> numCameras = new ParameterShortcut<int?>(null, "SETTINGS.NUMBER_OF_CAMERAS", ParameterMergeDirection.ArgumentToParameter);
			Argument<int?> schema = new Argument<int?>(null);
			Argument<bool> forceLatin1 = new Argument<bool>(false);
            //ParameterShortcut<string> analogInputDeviceTypeArgument = new ParameterShortcut<string>("Unknown", "SETTINGS.ANALOG_INPUT_DEVICE_TYPE", ParameterMergeDirection.Any);

			OptionEntry[] entries =
			{
			    //new OptionEntry("dbmodfile", 'f', dbmodFile, "The xml file that defines the dbmods. Default: " + ThreeVR.Libraries.DbTools.Schema.Constants.DbToolsMySqlPatchFile),
			    new OptionEntry("dbhost", 'h', dbHost, "The host of the db to mod. Default: localhost"),
			    new OptionEntry("dbname", 'n', dbName, "The name of the database to mod"),
			    new OptionEntry("dbpass", 'p', dbPass, "The password to use to connect to the database"),
			    new OptionEntry("dbuser", 'u', dbUser, "The user to use to connect to the database"),
			    new OptionEntry("createdb", 'c', createDb, "If true creates a new database, destroying the old one if it exists"),
			    new OptionEntry("dbversion", 'v', dbVersion, "The target db version. Defaults to the latest version in the xml file"),
                //new OptionEntry("dbtype",'t',dbType,"The target db type. Default:mysql"),
			    new OptionEntry("mergetrans", 'm', mergeTransactions, "If true attempts to merge mods together into one transaction"),
			    new OptionEntry("listparameters", listParameters, "List the parameters that will be used while moding the database"),
			    new OptionEntry("listmods", listMods, "List the mods that will be run"),
			    new OptionEntry("content", 'C', content, "If false dbtools will not update the database content after applying the schema mods"),
			    new OptionEntry("numcameras", 'N', numCameras, "A shortcut to specify the number of analog cameras (SETTINGS.NUMBER_OF_CAMERAS parameter)"),
			    new OptionEntry("schema", 'S', schema, "The database schema to use"),
			    new OptionEntry("latin1", forceLatin1, "Force the database to use latin1 (the default is now utf8)"),
                //new OptionEntry("inputdevicetype", 'i', analogInputDeviceTypeArgument, "Input device type (Analog, StretchAnalog, UsePreviousValue)"),
			    new OptionEntry("enterprisedb", 'e', enterpriseDb, "Build a database for an enterprise server")
			};

			DbToolsCommandContext context = new DbToolsCommandContext(entries);
			if (!context.Parse(args))
			{
				Console.WriteLine(context.ParseErrorMessage);
				context.DisplayHelp();
				Environment.Exit(1);
			}

			if (context.PostParseArguments.Length > 0)
			{
				Console.WriteLine("Unexpected argument: " + context.PostParseArguments[0]);
				context.DisplayHelp();
				Environment.Exit(1);
			}

			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
			IDictionary<string, string> parameters = context.GetParameters(listParameters);

			connectionStrBuilder.HostConfig = dbHost;
			connectionStrBuilder.DatabaseConfig = dbName;
			connectionStrBuilder.UserConfig = dbUser;
			connectionStrBuilder.PasswordConfig = dbPass;

			DatabaseStatus status = InitializeDatabaseConnection(connectionStrBuilder, forceLatin1, schema.Value);
			if (status == DatabaseStatus.ConnectionFailure)
			{
				Console.WriteLine(Environment.NewLine + "ERROR: Cannot initialize Database Connection");
				Environment.Exit(1);
			}

			else if (status == DatabaseStatus.CreatedNewDb)
			{
				createDb.Value = false;
			}
			if (!schema.Value.HasValue && createDb)
			{
				DbFactory.Instance.Schema = 0;
			}

			SupportFileFactory.Instance.FileLoadMessages += Console.WriteLine;
			string dbmodFile = "dbmods.xml";
			DbXmlManipulator dbSchema = new DbXmlManipulator(parameters, dbmodFile, DbFactory.Instance.Schema);
			// TODO: this just outputs everything in the mod file, which isn't very useful.  It'd be nice if it output the content of steps before running them.
			if (listMods.Value)
			{
				Console.WriteLine(dbSchema.ListMods());
			}
			string currentVersion;
			string updatedVersion = "";
			string requestedVersion = dbVersion;
			_engine = new TransactionalExecutionEngine(mergeTransactions);

			dbSchema.StartingApplyMod += dbSchema_StartingApplyMod;
			dbSchema.StartingApplyStep += dbSchema_StartingApplyStep;
			dbSchema.FinishedApplyMod += dbSchema_FinishedApplyMod;
			dbSchema.FinishedApplyStep += dbSchema_FinishedApplyStep;
			dbSchema.SchemaChanged += delegate
			{
				DbFactory.Instance.Schema = dbSchema.Schema;
				_engine.NewConnection();
			};
			if (createDb)
			{
				DbFactory.Instance.Driver.CreateDatabase(dbName);
				currentVersion = "0.0";
			}
			else
			{
				currentVersion = _engine.GetDbVersion();
			}
			try
			{
				updatedVersion = dbSchema.UpdateDb(_engine, currentVersion, requestedVersion);
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error updating db: " + ex);
				Environment.Exit(1);
			}

			if (updatedVersion == currentVersion)
			{
				Console.WriteLine(Environment.NewLine + "No new schema mods available for DB version " + currentVersion);
			}
			else
			{
				Console.WriteLine(Environment.NewLine + "SUCCESS: DB updated from version = " + currentVersion + " to version = " + updatedVersion);
			}
			if (content)
			{
				Console.WriteLine("> Updating Content...");
				try
				{
					if (!Generator.UpdateContent(_engine.DbAccess, _engine, enterpriseDb))
					{
						throw new ApplicationException("Failed to update db content", _engine.LastException);
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex);
					Environment.Exit(1);
				}
			}

			_engine.CommitExistingTransaction();

			Environment.Exit(0);
		}

		private enum DatabaseStatus
		{
			Unknown,
			Connected,
			ConnectionFailure,
			CreatedNewDb
		}

		/// <summary>
		/// Initialize the DB DataReader using the parameters passed on the command line
		/// </summary>
		/// <returns>true if OK, false if any error</returns>
		private static DatabaseStatus InitializeDatabaseConnection(ConnectionStringBuilder connectionStringBuilder, bool forceLatin1, int? schema)
		{
			string dbName = connectionStringBuilder.DatabaseConfig;

			//DatabaseUtility.ValidDatabaseName(dbName, connectionStringBuilder.DbType);

			DatabaseStatus status = DatabaseStatus.Unknown;
			//bool createdNewDb = false;
			//bool isConnectedToDefaultDatabase = false;
			IDbDriver driver = null;
			try
			{
				#region New Codes

				InitDatabaseDriverHelper initDatabaseDriverHelper = new InitDatabaseDriverHelper(connectionStringBuilder, isForceLatin: forceLatin1);
				bool initResult = initDatabaseDriverHelper.InitializeDatabaseDriver();
				if (initResult)
				{
					driver = DbFactory.Instance.Driver;
					status = DatabaseStatus.Connected;
					if (initDatabaseDriverHelper.DbInitializationItem == DbInitialization.NewDatabase)
					{
						status = DatabaseStatus.CreatedNewDb;
					}
				}
				else
				{
					status = DatabaseStatus.ConnectionFailure;
				}

				#endregion
			}
			catch (Exception ex)
			{
			}

			if (driver != null)
			{
				//DbFactory.Instance.Initialize(driver);
				//driver.ConnectionStrBuilder = connectionStringBuilder;
				if (schema != null)
				{
					DbFactory.Instance.Schema = schema.Value;
				}
			}
			return status;
		}
		private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			Exception ex = (Exception)e.ExceptionObject;
			Console.WriteLine("Error caught by exception handler: " + ex);
			Environment.Exit(1);
		}
		private static int _outputLevel;
		private static void dbSchema_StartingApplyMod(object sender, EventArgs e)
		{
			DbSchemaEventArgs args = (DbSchemaEventArgs)e;

			Console.WriteLine(new string(' ', _outputLevel++ * 2) + "> Applying mod from = " + args.ModFromVersion + " to = " + args.ModToVersion);
		}

		private static void dbSchema_StartingApplyStep(object sender, EventArgs e)
		{
			DbSchemaEventArgs args = (DbSchemaEventArgs)e;

			Console.Write(new string(' ', _outputLevel++ * 2) + "> Applying step type = " + args.StepType + ": ");
			if (args.StepType == "mod")
			{
				Console.WriteLine();
			}
		}

		private static void dbSchema_FinishedApplyMod(object sender, EventArgs e)
		{
			DbSchemaEventArgs args = (DbSchemaEventArgs)e;
			--_outputLevel;

			if (!args.Success)
			{
				Console.WriteLine("ERROR: Mod author = " + args.ModAuthor + " from = " + args.ModFromVersion + " to = " + args.ModToVersion + " failed at step = " + args.StepNumber + "!");
				Console.WriteLine("Last Exception: ");
				Exception innerException = _engine.LastException;
				while (innerException != null)
				{
					Console.WriteLine();
					Console.WriteLine("Message: {0}", innerException.Message);
					Console.WriteLine("Call Stack: {0}", innerException.StackTrace);
					Console.WriteLine(args.StepContent);
					innerException = innerException.InnerException;
				}
				Environment.Exit(1);
			}
		}

		private static void dbSchema_FinishedApplyStep(object sender, EventArgs e)
		{
			DbSchemaEventArgs args = (DbSchemaEventArgs)e;

			bool indent;
			try
			{
				indent = Console.CursorLeft == 0;
			}
			catch (System.IO.IOException)
			{
				indent = false;
			}

			string prepend = indent ? new string(' ', _outputLevel * 2) : string.Empty;
			--_outputLevel;

			if (args.Success)
			{
				Console.WriteLine(prepend + "done.");
			}
			else
			{
				Console.WriteLine(prepend + "failed!");
			}
		}
	}


}
