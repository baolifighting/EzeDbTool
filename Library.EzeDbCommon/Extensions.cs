using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Libraries.EzeDbCommon
{
	public static class Extensions
	{
		/// <summary>
		/// Gets the connection string builder.
		/// </summary>
		/// <param name="dbConnectionStringBuilder">The database connection string builder.</param>
		/// <returns></returns>
		public static ConnectionStringBuilder GetConnectionStringBuilder(this DbConnectionStringBuilder dbConnectionStringBuilder)
		{
			ConnectionStringBuilder connectionStringBuilder = new ConnectionStringBuilder
			{
				HostConfig = dbConnectionStringBuilder[ConnectionStringConst.HostConfig].ToString(),
				DatabaseConfig = dbConnectionStringBuilder[ConnectionStringConst.DatabaseConfig].ToString(),
				//UserConfig = dbConnectionStringBuilder[ConnectionStringConst.UserConfig].ToString(),
				//PasswordConfig = dbConnectionStringBuilder[ConnectionStringConst.PasswordConfig].ToString()
			};

			if (dbConnectionStringBuilder.ContainsKey(ConnectionStringConst.UserConfig))
			{
				object userIdConfigValue = dbConnectionStringBuilder[ConnectionStringConst.UserConfig];
				connectionStringBuilder.UserConfig = userIdConfigValue.ToString();
			}

			if (dbConnectionStringBuilder.ContainsKey(ConnectionStringConst.PasswordConfig))
			{
				object passwordConfigValue = dbConnectionStringBuilder[ConnectionStringConst.PasswordConfig];
				connectionStringBuilder.PasswordConfig = passwordConfigValue.ToString();
			}

			object threevrDriver;
			//if (dbConnectionStringBuilder.TryGetValue("3vrdriver", out threevrDriver))
			//{
			//	DataBaseType databaseType;
			//	if (Enum.TryParse(threevrDriver.ToString(), true, out databaseType))
			//	{
			//		connectionStringBuilder.DbType = databaseType;
			//	}
			//}
			return connectionStringBuilder;
		}
	}
}
