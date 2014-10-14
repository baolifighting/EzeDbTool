using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.XPath;

namespace Libraries.EzeDbCommon.Content.XmlManipulators
{
	class SettingCategoriesManipulator : DBXmlManipulator
	{
		#region Instance Fields

		private TableValue[] _categoryDeletes;
		private Tuple<IXPathNavigable, XPathExpression> _settings;

		#endregion Instance Fields

		#region Lifetime

		public SettingCategoriesManipulator(DataReader dbAccess, string xmlPath)
			: base(GetContext(dbAccess, xmlPath))
		{
			_settings = new Tuple<IXPathNavigable, XPathExpression>(_context.NavigableXml, XPathExpression.Compile("/settingcategories/row/settings/row"));

			List<TableValue> categoryDeletes = new List<TableValue>();

			foreach (ChangedRow row in _changedRows)
			{
				if (row.Type == ChangedRow.ChangeType.REMOVE)
				{
					categoryDeletes.Add(row.UniqueIdentifier);
				}
			}

			_categoryDeletes = categoryDeletes.ToArray();
		}

		private static DbXmlManipulatorContext GetContext(DataReader dataReader, string xmlPath)
		{
			XmlDocument xmlDoc = new XmlDocument();
			xmlDoc.Load(xmlPath);
			DbXmlManipulatorContext context = new DbXmlManipulatorContext(dataReader, "settingcategories", xmlDoc);
			context.UniqueColumnNames.Add("name");
			context.XmlElementsToIgnore.Add("settings");
			ApplyDynamicAttributes(context);
			return context;
		}

		#endregion Lifetime

		public Tuple<IXPathNavigable, XPathExpression> Settings
		{
			get { return _settings; }
		}

		#region Methods

		// sets the screen order of settings by their order in the xml and pushes the category names down to the settings
		private static void ApplyDynamicAttributes(DbXmlManipulatorContext context)
		{
			XPathNavigator nav = context.NavigableXml.CreateNavigator();
			XPathNodeIterator categoryIterator = nav.Select(context.RowExpression);

			int categoryOrderCounter = 0;

			while (categoryIterator.MoveNext())
			{
				string currentCategory = categoryIterator.Current.GetAttribute("name", string.Empty);
				if (currentCategory == string.Empty)
				{
					throw new ApplicationException("No attribute 'name' in scopes row.");
				}

				categoryIterator.Current.CreateAttribute(string.Empty, "screenOrder", string.Empty, categoryOrderCounter.ToString());
				categoryOrderCounter++;

				var settingsNav = categoryIterator.Current.Clone();

				settingsNav.MoveToChild("settings", String.Empty);
				var settingRowsItr = settingsNav.Select("row");
				int settingsOrderCounter = 0;
				while (settingRowsItr.MoveNext())
				{
					settingRowsItr.Current.CreateAttribute(string.Empty, "category", string.Empty, currentCategory);
					settingRowsItr.Current.CreateAttribute(string.Empty, "screenOrder", string.Empty, settingsOrderCounter.ToString());
					settingsOrderCounter++;
				}
			}
		}



		public string CategorySettingsUpdateSql()
		{
			StringBuilder sqlStmt = new StringBuilder();

			// We don't want to delete settingdefinitions records just
			// because the category has been deleted, so set the
			// foreign key to NULL on the settingdefinitions record.
			foreach (TableValue categoryValue in _categoryDeletes)
			{
				//sqlStmt.Append("UPDATE settings ");
				//sqlStmt.Append("SET settingCategoryId = NULL ");                               
				//"WHERE settingCategoryId = (SELECT settingCategoryId FROM settingcategories WHERE {0} LIMIT 1);{1}",
				//"WHERE settingCategoryId = (SELECT TOP 1 settingCategoryId FROM settingcategories WHERE {0});{1}",
				var strSql = "";
				//= DbFactory.Instance.Driver.SqlStatementWarehouse.GetCategorySettingsUpdateSql(_dbXmlTable.UniqueColumnName.ToWhereEqualString(_context.DbTableName, categoryValue));
				sqlStmt.AppendFormat(strSql);
			}

			return sqlStmt.ToString();
		}

		#endregion Methods
	}
}
