using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Libraries.EzeDbCommon
{
	public class ConnectionStringBuilder
	{
		/// <summary>
		///     The database config
		/// </summary>
		public string DatabaseConfig;

		/// <summary>
		///     The host config
		/// </summary>
		public string HostConfig;

		/// <summary>
		///     The password config
		/// </summary>
		public string PasswordConfig;

		/// <summary>
		///     The user config
		/// </summary>
		public string UserConfig;

		/// <summary>
		///     Gets the name of the master database.
		/// </summary>
		/// <value>
		///     The name of the master database.
		/// </value>
		public string MasterDatabaseName
		{
			get
			{
				return "master";
			}
		}

		/// <summary>
		///     Gets the create data base string.
		/// </summary>
		/// <value>
		///     The create data base string.
		/// </value>
		public string MasterDataBaseString
		{
			get
			{
				return string.Format("Data Source={0};Database={1};User ID={2};Password={3}", HostConfig,
									 MasterDatabaseName, UserConfig, PasswordConfig);
			}
		}

		/// <summary>
		///     Gets the connection data base string.
		/// </summary>
		/// <value>
		///     The connection data base string.
		/// </value>
		public string CurrentDataBaseString
		{
			get
			{
				return string.Format("Data Source={0};Database={1};User ID={2};Password={3}", HostConfig,
									 DatabaseConfig, UserConfig, PasswordConfig);
			}
		}
	}
}