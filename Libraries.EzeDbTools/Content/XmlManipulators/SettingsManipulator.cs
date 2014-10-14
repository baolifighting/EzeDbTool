using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.XPath;

namespace Libraries.EzeDbCommon.Content.XmlManipulators
{
    public sealed class SettingsManipulator : DBXmlManipulator
    {
        #region Fields

        public readonly HashSet<TableValue> KnownSettingNames;
        public readonly List<TableValue> SettingsToIgnore;

        #endregion

        #region Lifetime

        /// <param name="dbAccess">Used only to submit queries during instatiation.</param>
        /// <param name="navigableXml">The xml that holds the content for that table.</param>
        /// <param name="expr">The XPathExpression to access rows for this table in the xml</param>
        /// <param name="scopesToIgnore"></param>
        public SettingsManipulator(DataReader dbAccess, IXPathNavigable navigableXml, XPathExpression expr, List<TableValue> scopesToIgnore)
            :
            base(GetContext(dbAccess, navigableXml, expr, scopesToIgnore))
        {
            var tuple = (Tuple<HashSet<TableValue>, List<TableValue>>)_context.State;
            KnownSettingNames = tuple.Item1;
            SettingsToIgnore = tuple.Item2;
        }

        private static DbXmlManipulatorContext GetContext(DataReader dataReader, IXPathNavigable navigableXml, XPathExpression expr, List<TableValue> scopesToIgnore)
        {
            DbXmlManipulatorContext context = new DbXmlManipulatorContext(dataReader, "settings", navigableXml)
                {
                    RowExpression = expr
                };
            context.UniqueColumnNames.Add("name");
            context.AliasesByName["category"] = new XmlColumnAlias("settingCategoryId", "name", "settingcategories", "settingCategoryId");
            context.AliasesByName["scope"] = new XmlColumnAlias("scopeId", "name", "scopes", "scopeId");
            var scopeName = new TableColumnName("scope");
            var settingName = new TableColumnName("name");
            var knownSettingNames = new HashSet<TableValue>();
            var settingToIgnore = new List<TableValue>();
            context.State = new Tuple<HashSet<TableValue>, List<TableValue>>(knownSettingNames, settingToIgnore);

            context.XmlRowParsed = (ref IDictionary<TableColumnName, TableValue> row) =>
                {
                    if (!knownSettingNames.Add(row[settingName]))
                    {
                        throw new ArgumentException(
                            string.Format("Setting '{0}' appears multiple times in the categories xml file.",
                                          ((string[]) row[settingName])[0]));
                    }
                };

            context.DbRowParsed = (ref IDictionary<TableColumnName, TableValue> row) =>
                {
                    if (scopesToIgnore.Contains(row[scopeName]))
                    {
                        settingToIgnore.Add(row[settingName]);
                        row = null;
                    }
                };

            return context;
        }

        #endregion

        #region Methods
      
        /// <summary>
        /// When we delete from settings, we have to be sure that there are not any rows referencing that setting in
        /// the settingdefinitions table, or we violate a foreign key constraint.
        /// </summary>
        /// <returns>settings deletes with settingdefinitions deletes, just in case.</returns>
        public override string DeleteSql()
        {
            StringBuilder sqlStmt = new StringBuilder();
            foreach (ChangedRow changedSettingRow in _changedRows)
            {
                if (changedSettingRow.Type == ChangedRow.ChangeType.REMOVE)
                {
                    //sqlStmt.AppendFormat("DELETE FROM settingdefinitions WHERE {0}=(SELECT {0} FROM {1} WHERE {2} LIMIT 1);{3}", _dbXmlTable.DbPrimaryKeyName, _context.DbTableName, _dbXmlTable.UniqueColumnName.ToWhereEqualString(_context.DbTableName, changedSettingRow.UniqueIdentifier), Environment.NewLine);
					//sqlStmt.Append(
					//	DbFactory.Instance.Driver.SqlStatementWarehouse.GetDeleteSettingDefinitionStatement(
					//		_dbXmlTable.DbPrimaryKeyName, _context.DbTableName,
					//		_dbXmlTable.UniqueColumnName.ToWhereEqualString(_context.DbTableName,
					//														changedSettingRow.UniqueIdentifier)));
                    sqlStmt.Append(changedSettingRow.GenerateSQL(_context.DbTableName));
                }
            }

            return sqlStmt.ToString();
        }

        public override string UpdateSql()
        {
            string sqlStmt = "";
            foreach (ChangedRow row in _changedRows)
            {
                if (row.Type == ChangedRow.ChangeType.UPDATE)
                {
                    sqlStmt += row.GenerateSQL(_context.DbTableName);
                }
            }
            return sqlStmt;
        }

        #endregion
    }
}
