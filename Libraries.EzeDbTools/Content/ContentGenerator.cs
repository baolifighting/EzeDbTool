#region Imports

using System.Text;
using System.Xml;
using Libraries.EzeDbCommon.Content.XmlManipulators;

#endregion Imports

namespace Libraries.EzeDbCommon.Content
{

	/// <summary>
	/// Generates content. Duh.
	/// </summary>
	public class Generator
	{
		private static IExecutionEngine _engine;

		private Generator()
		{ }

		public static bool UpdateContent(DataReader dbAccess, IExecutionEngine engine, bool isEnterprise)
		{
			_engine = engine;
			return engine.ExecuteSql(GetInsertContentStatement(dbAccess, isEnterprise));
		}

		//public static bool UpdateContent(DataReader dbAccess, IExecutionEngine engine, bool isEnterprise)
		//{
		//	_engine = engine;
		//	return engine.ExecuteSql(GetInsertContentStatement(dbAccess, isEnterprise));
		//}

		private static string GetInsertContentStatement(DataReader dbAccess, bool isEnterprise)
		{
		    string sql = isEnterprise ? EnterpriseDbSql(dbAccess) : VipDbSql(dbAccess);

		    return sql;
		}

	    private static string EnterpriseDbSql(DataReader dbAccess)
		{
			StringBuilder sqlStmt = new StringBuilder(150);
					 
			#region HealthAlertTypes

			HealthAlertTypesManipulator healthAlertTypes =
				new HealthAlertTypesManipulator(dbAccess,
													 SupportFileFactory.Instance.FullPath(Constants.EHealthAlertTypesXml));

			sqlStmt.Append(healthAlertTypes.InsertSql());
			sqlStmt.Append(healthAlertTypes.UpdateSql());
			sqlStmt.Append(healthAlertTypes.DeleteSql());

			#endregion HealthAlertTypes

			#region SettingCategories Scopes Settings SettingDefinitiopns SettingValues
						
			SettingCategoriesManipulator categories = new SettingCategoriesManipulator(dbAccess, SupportFileFactory.Instance.FullPath(Constants.ECategoriesXml));
			ScopesManipulator scopes = new ScopesManipulator(dbAccess, SupportFileFactory.Instance.FullPath(Constants.EScopesXml), categories.Settings);
			SettingsManipulator settings = scopes.Settings;
			SettingDefinitionsManipulator settingdefinitions = new SettingDefinitionsManipulator(dbAccess, SupportFileFactory.Instance.FullPath(Constants.ESettingsXml), scopes.Info, settings);

			sqlStmt.Append(categories.InsertSql());
			sqlStmt.Append(categories.UpdateSql());

			sqlStmt.Append(scopes.InsertSql());
			sqlStmt.Append(scopes.UpdateSql());

			sqlStmt.Append(settings.InsertSql());
			sqlStmt.Append(settings.UpdateSql());

			sqlStmt.Append(settingdefinitions.InsertSql());
			sqlStmt.Append(settingdefinitions.UpdateSql());

			sqlStmt.Append(scopes.ComponentInsertSql());
			sqlStmt.Append(settingdefinitions.SettingValuesChangesSql());
			sqlStmt.Append(scopes.ComponentDeleteSql());

			sqlStmt.Append(settingdefinitions.DeleteSql());
			sqlStmt.Append(settings.DeleteSql());
			sqlStmt.Append(scopes.DeleteSql());
			sqlStmt.Append(categories.DeleteSql());

			#endregion

			return sqlStmt.ToString();

		}

		private static string VipDbSql(DataReader dbAccess)
		{
			StringBuilder sqlStmt = new StringBuilder(150);

			#region SettingCategories Scopes Settings SettingDefinitions SettingGroups

			var categories = new SettingCategoriesManipulator(dbAccess, SupportFileFactory.Instance.FullPath(Constants.CategoriesXml));
			var scopes = new ScopesManipulator(dbAccess, SupportFileFactory.Instance.FullPath(Constants.ScopesXml), categories.Settings);
			SettingsManipulator settings = scopes.Settings;
			var groups = new SettingGroupsManipulator(dbAccess, SupportFileFactory.Instance.FullPath(Constants.SettingGroupsXml));
			SettingGroupSettingsManipulator groupSettings = groups.SettingGroupSettings;
			var settingDefinitionXml = new XmlDocument();
			settingDefinitionXml.Load(SupportFileFactory.Instance.FullPath(Constants.SettingsXml));
			var settingDefinitions = new SettingDefinitionsManipulator(dbAccess, settingDefinitionXml.DocumentElement, scopes.Info, settings);

			sqlStmt.Append(categories.InsertSql());
			sqlStmt.Append(categories.UpdateSql());
			sqlStmt.Append(categories.CategorySettingsUpdateSql());

			sqlStmt.Append(scopes.InsertSql());
			sqlStmt.Append(scopes.UpdateSql());

			sqlStmt.Append(settings.InsertSql());
			sqlStmt.Append(settings.UpdateSql());

			sqlStmt.Append(groups.InsertSql());
			sqlStmt.Append(groups.UpdateSql());

			sqlStmt.Append(groupSettings.InsertSql());
			sqlStmt.Append(groupSettings.UpdateSql());

			sqlStmt.Append(settingDefinitions.InsertSql());
			sqlStmt.Append(settingDefinitions.UpdateSql());

			sqlStmt.Append(scopes.ComponentInsertSql());
			sqlStmt.Append(settingDefinitions.SettingValuesChangesSql());
			sqlStmt.Append(scopes.ComponentDeleteSql());

			sqlStmt.Append(settingDefinitions.DeleteSql());

			sqlStmt.Append(groupSettings.DeleteSql());

			sqlStmt.Append(groups.DeleteSql());

			sqlStmt.Append(settings.DeleteSql());

			sqlStmt.Append(scopes.DeleteSql());

			sqlStmt.Append(categories.DeleteSql());

			#endregion SettingCategories Scopes Settings SettingDefinitions SettingGroups

			#region HealthAlerts

			HealthAlertTypesManipulator healthAlertTypes =
				new HealthAlertTypesManipulator(dbAccess,
													 SupportFileFactory.Instance.FullPath(Constants.HealthAlertTypesXml));

			sqlStmt.Append(healthAlertTypes.InsertSql());
			sqlStmt.Append(healthAlertTypes.UpdateSql());
			sqlStmt.Append(healthAlertTypes.DeleteSql());

			#endregion HealthAlerts

			#region Schedules

			SchedulesManipulator schedules = new SchedulesManipulator(dbAccess, SupportFileFactory.Instance.FullPath(Constants.SchedulesXml));

			sqlStmt.Append(schedules.InsertSql());
			sqlStmt.Append(schedules.UpdateSql());
			sqlStmt.Append(schedules.DeleteSql());
			sqlStmt.Append(schedules.CreateDefaultScheduleTimesIfNeeded(dbAccess));

			#endregion

			return sqlStmt.ToString();
		}
	}
}
