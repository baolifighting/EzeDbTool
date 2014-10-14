using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.XPath;

namespace Libraries.EzeDbCommon.Content.XmlManipulators
{
    public sealed class SettingDefinitionsManipulator : DBXmlManipulator
    {
        #region Variables

        private readonly SettingValuesHelper _settingValueHelper;

        #endregion

        #region Lifetime

        /// <param name="dbAccess">Used only to submit queries during instatiation.</param>
        /// <param name="navigableXml">The xml that holds the content for that table.</param>
        /// <param name="scopeInfos">Info from the scopes manipulator to help generate setting values inserts and deletes as needed</param>
        /// <param name="settingsManipulator"></param>
        public SettingDefinitionsManipulator(DataReader dbAccess, IXPathNavigable navigableXml, ScopesManipulator.ScopeInformation[] scopeInfos, SettingsManipulator settingsManipulator)
            :
            base(GetContext(dbAccess, navigableXml, settingsManipulator))
        {
            Dictionary<string, Policy> userChangedPolicies, userUnchangedPolicies;
            GetXMLPolicies(out userChangedPolicies, out userUnchangedPolicies);
            _settingValueHelper = new SettingValuesHelper(_dbXmlTable, userChangedPolicies, userUnchangedPolicies);
            GetChangedSettingValues(scopeInfos);
        }

        public SettingDefinitionsManipulator(DataReader dbAccess, string xmlPath, ScopesManipulator.ScopeInformation[] scopeInfos, SettingsManipulator settingsManipulator)
            : this(dbAccess, new XPathDocument(xmlPath), scopeInfos, settingsManipulator)
        {
        }

        private static DbXmlManipulatorContext GetContext(DataReader dataReader, IXPathNavigable navigableXml, SettingsManipulator settingsManipulator)
        {
            var context = new DbXmlManipulatorContext(dataReader, "settingdefinitions", navigableXml);
            context.UniqueColumnNames.Add("name");
            context.UniqueColumnNames.Add("channelDriver");
            context.AliasesByName["channelDriver"] = new XmlColumnAlias("channelDriverId", "name", "channeldrivers", "channelDriverId");
            context.AliasesByName["name"] = new XmlColumnAlias("settingId", "name", "settings", "settingId");
            context.XmlElementsToIgnore.Add("update_policy");
            var uniqueColumnName = new TableColumnName("name", "channelDriver");
            var licenseVal = new TableColumnName("licenseVal");
            var emptyVal = new TableValue("");
            
            context.XmlRowParsed = delegate(ref IDictionary<TableColumnName, TableValue> row)
                {
                    TableValue currentValue = row[uniqueColumnName];
                    string settingName = ((string[])currentValue)[0];
                    if (!settingsManipulator.KnownSettingNames.Contains(new TableValue(settingName)))
                    {
                        throw new ArgumentException(string.Format("Setting '{0}' exists in settingdefinition xml, but does not exist in categories xml.", settingName));
                    }
                    row[uniqueColumnName] = uniqueColumnName.SetValueForColumn("channelDriver",DbNullReplacementString,currentValue, false);

                    if (!row.ContainsKey(licenseVal))
                    {
                        row[licenseVal] = emptyVal;
                    }
                };
            context.DbRowParsed = (ref IDictionary<TableColumnName, TableValue> row) =>
                {
                    TableValue currentValue = row[uniqueColumnName];
                    var setting = ((string[]) currentValue)[0];
                    if (
                        settingsManipulator.SettingsToIgnore.Select(toIgnore => ((string[]) toIgnore)[0])
                                           .FirstOrDefault(toIgnore => toIgnore == setting) != null)
                    {
                        row = null;
                    }
                };
            return context;
        }

        #endregion

        #region Methods

        public string SettingValuesChangesSql()
        {
            return _settingValueHelper.GenerateSql();
        }

        #endregion

        #region Private Implementation

        private void GetChangedSettingValues(ScopesManipulator.ScopeInformation[] scopeInfos)
        {
            Dictionary<string, List<string>> settingNamesByChannelDriver = GetSettingsByChannelDriver();
            List<SettingValueRow> settingValues = new List<SettingValueRow>(), settingValuesInDb = new List<SettingValueRow>();
            Dictionary<SettingValueRow, long> settingValueIdsInDb = new Dictionary<SettingValueRow, long>();
            foreach (ScopesManipulator.ScopeInformation scopeInformation in scopeInfos)
            {
                string scope = scopeInformation.Scope;
                string[] scopeSettings = scopeInformation.Settings;

                foreach (ScopesManipulator.ComponentInfo componentInfo in scopeInformation.Components)
                {
                    string channelDriver;
                    if (componentInfo.HasChannelDriver)
                    {
                        channelDriver = componentInfo.ChannelDriver;
                        if (channelDriver == null)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        channelDriver = null;
                    }

                    List<string> settings;
                    if (settingNamesByChannelDriver.TryGetValue(channelDriver ?? string.Empty, out settings))
                    {
                        Array.Sort(scopeSettings, StringComparer.InvariantCultureIgnoreCase);
                        foreach (string setting in settings)
                        {
                            if (Array.BinarySearch(scopeSettings, setting, StringComparer.InvariantCultureIgnoreCase) >= 0)
                            {
                                settingValues.Add(new SettingValueRow(scope, componentInfo.ComponentNumber, setting, channelDriver));
                            }
                        }
                    }
                }                
               // string sqlStmt = string.Format("SELECT components.componentnumber, settings.name, channeldrivers.name, settingvalueid FROM settingvalues LEFT JOIN components USING (componentid) LEFT JOIN settingdefinitions ON (settingvalues.settingdefinitionid=settingdefinitions.settingdefinitionid) LEFT JOIN settings USING (settingid) LEFT JOIN scopes ON (settings.scopeid=scopes.scopeid) LEFT JOIN channeldrivers ON (settingdefinitions.channeldriverid=channeldrivers.channeldriverid) where scopes.name='{0}'", EscapeSqlValues(scope));
                string sqlStmt = string.Format("SELECT components.componentnumber, settings.name, channeldrivers.name, settingvalueid FROM settingvalues LEFT JOIN components ON settingvalues.componentId = components.componentId LEFT JOIN settingdefinitions ON (settingvalues.settingdefinitionid=settingdefinitions.settingdefinitionid) LEFT JOIN settings ON settingdefinitions.settingId = settings.settingId LEFT JOIN scopes ON (settings.scopeid=scopes.scopeid) LEFT JOIN channeldrivers ON (settingdefinitions.channeldriverid=channeldrivers.channeldriverid) where scopes.name='{0}'", EscapeSqlValues(scope));
                using (IDataReader reader = _context.DataReader.ExecuteQuery(sqlStmt))
                {
                    while (reader.Read())
                    {
                        int? component = null;
                        string setting = null, channelDriver = null;
                        if (!reader.IsDBNull(0))
                        {
                            component = reader.GetInt32(0);
                        }
                        if (!reader.IsDBNull(1))
                        {
                            setting = reader.GetString(1);
                        }
                        if (!reader.IsDBNull(2))
                        {
                            channelDriver = reader.GetString(2);
                        }
                        SettingValueRow dbValue = new SettingValueRow(scope, component, setting, channelDriver);
                        if (settingValueIdsInDb.ContainsKey(dbValue))
                        {
                            // if we have a duplicate settingvalue, we just need to delete the duplicates; no use worrying about them.
                            _settingValueHelper.Deletes.Add(reader.GetInt64(3));
                        }
                        else
                        {
                            settingValueIdsInDb.Add(dbValue, reader.GetInt64(3));
                            settingValuesInDb.Add(new SettingValueRow(scope, component, setting, channelDriver));
                        }
                    }
                }
            }

            settingValues.Sort();
            settingValuesInDb.Sort();
            IEnumerator<SettingValueRow> settingValuesEnumerator = settingValues.GetEnumerator(), settingValuesInDbEnumerator = settingValuesInDb.GetEnumerator();
            bool processingSettings = settingValuesEnumerator.MoveNext(), processingSettingsInDb = settingValuesInDbEnumerator.MoveNext();
            while (true)
            {
                if (!processingSettings)
                {
                    while (processingSettingsInDb)
                    {
                        _settingValueHelper.Deletes.Add(settingValueIdsInDb[settingValuesInDbEnumerator.Current]);
                        processingSettingsInDb = settingValuesInDbEnumerator.MoveNext();
                    }
                    break;
                }
                if (!processingSettingsInDb)
                {
                    while (processingSettings)
                    {
                        _settingValueHelper.Inserts.Add(settingValuesEnumerator.Current);
                        processingSettings = settingValuesEnumerator.MoveNext();
                    }
                    break;
                }

                int comparison = settingValuesInDbEnumerator.Current.CompareTo(settingValuesEnumerator.Current);
                if (comparison == 0)
                {
                    processingSettings = settingValuesEnumerator.MoveNext();
                    processingSettingsInDb = settingValuesInDbEnumerator.MoveNext();
                }
                else if (comparison > 0)
                {
                    _settingValueHelper.Inserts.Add(settingValuesEnumerator.Current);
                    processingSettings = settingValuesEnumerator.MoveNext();
                }
                else if (comparison < 0)
                {
                    _settingValueHelper.Deletes.Add(settingValueIdsInDb[settingValuesInDbEnumerator.Current]);
                    processingSettingsInDb = settingValuesInDbEnumerator.MoveNext();
                }
            }

            foreach (ChangedRow row in _changedRows)
            {
                if (row.Type == ChangedRow.ChangeType.UPDATE)
                {
                    _settingValueHelper.Updates.Add(row);
                }
            }
        }

        private Dictionary<string, List<string>> GetSettingsByChannelDriver()
        {
            //TODO: this is silly because we do the exact same iteraction when we call the base constructor. There should be a callback for getting at things like this.
            Dictionary<string, List<string>> settingNamesByChannelDriver = new Dictionary<string, List<string>>();
            XPathNodeIterator iterator = _context.NavigableXml.CreateNavigator().Select(_context.RowExpression);
            while (iterator.MoveNext())
            {
                string setting = iterator.Current.GetAttribute("name", string.Empty);
                string channelDriver = iterator.Current.GetAttribute("channelDriver", string.Empty);
                List<string> settingNames;
                if (!settingNamesByChannelDriver.TryGetValue(channelDriver, out settingNames))
                {
                    settingNames = new List<string>();
                    settingNamesByChannelDriver.Add(channelDriver, settingNames);
                }
                settingNames.Add(setting);
            }
            return settingNamesByChannelDriver;
        }

        private void GetXMLPolicies(out Dictionary<string, Policy> userChangedPolicies, out Dictionary<string, Policy> userUnchangedPolicies)
        {
            userChangedPolicies = new Dictionary<string, Policy>();
            userUnchangedPolicies = new Dictionary<string, Policy>();

            XPathNavigator nav = _context.NavigableXml.CreateNavigator();

            XPathNodeIterator iterator = nav.Select("//update_policy");

            while (iterator.MoveNext())
            {
                string name;

                XPathNavigator nameNav = iterator.Current.Clone();
                nameNav.MoveToParent();
                nameNav.MoveToFirstAttribute();
                while (nameNav.Name != "name" && nameNav.MoveToNextAttribute())
                { }

                if (nameNav.Name != "name")
                {
                    throw new ApplicationException("No attribute 'name' in update_policy parent.");
                }

                name = nameNav.Value;

                XPathNavigator policyNav = iterator.Current;

                if (policyNav.MoveToFirstChild())
                {
                    do
                    {
                        Policy policy = new Policy();
                        string policyType = policyNav.Name;
                        switch (policyType)
                        {
                            case "update_default":
                                if (policyNav.MoveToFirstAttribute())
                                {
                                    do
                                    {
                                        switch (policyNav.Name)
                                        {
                                            case "set_default":
                                                policy.UpdateDefault = Convert.ToBoolean(policyNav.Value);
                                                break;
                                            case "set_min":
                                                policy.UpdateMin = Convert.ToBoolean(policyNav.Value);
                                                break;
                                            case "set_max":
                                                policy.UpdateMax = Convert.ToBoolean(policyNav.Value);
                                                break;
                                            default:
                                                break;
                                        }
                                    } while (policyNav.MoveToNextAttribute());
                                    userUnchangedPolicies.Add(name, policy);
                                    policyNav.MoveToParent();
                                }
                                break;
                            case "update_user_changed":
                                if (policyNav.MoveToFirstAttribute())
                                {
                                    do
                                    {
                                        switch (policyNav.Name)
                                        {
                                            case "set_default":
                                                policy.UpdateDefault = Convert.ToBoolean(policyNav.Value);
                                                break;
                                            case "set_min":
                                                policy.UpdateMin = Convert.ToBoolean(policyNav.Value);
                                                break;
                                            case "set_max":
                                                policy.UpdateMax = Convert.ToBoolean(policyNav.Value);
                                                break;
                                            default:
                                                break;
                                        }
                                    } while (policyNav.MoveToNextAttribute());
                                    userChangedPolicies.Add(name, policy);
                                    policyNav.MoveToParent();
                                }
                                break;
                            default:
                                break;
                        }
                    } while (policyNav.MoveToNext());
                }
            }
        }

        #endregion

        #region Policy
        /// <summary>
        /// Defines a setting update policy.
        /// </summary>
        /// <remarks>
        /// These policies define what should happen to the settingvalues table when the 
        /// defaultValue, minval, or maxval get changed in the settingdefinitions table.  
        /// If the defaultValue is updated, do we set the settingValue to that (bool default?)
        /// If the minval is updated, do we make sure the settingValue is at least that (bool min?)
        /// If the maxval is updated, do we make sure the settingValue is at most that (bool max?)
        /// </remarks>
        private class Policy
        {
            #region Variables

            public bool UpdateDefault;
            public bool UpdateMin;
            public bool UpdateMax;

            #endregion

            #region Methods

            public static Policy DefaultPolicy(bool userChanged, ChangedRow row, TableColumnName accessColumn)
            {
                Policy defaultPolicy = new Policy();

                int access;

                // Find out what access level this setting is
                if (row.ChangedValue(accessColumn) == null)
                {
                    access = Convert.ToInt32(((string[])row.OldValue(accessColumn))[0]);
                }
                else
                {
                    access = Convert.ToInt32(((string[])row.ChangedValue(accessColumn))[0]);
                }

                if (access <= 2 || userChanged)
                {
                    defaultPolicy.UpdateDefault = false;
                    defaultPolicy.UpdateMin = true;
                    defaultPolicy.UpdateMax = true;
                }
                else
                {
                    defaultPolicy.UpdateDefault = true;
                    defaultPolicy.UpdateMin = false;
                    defaultPolicy.UpdateMax = false;
                }

                return defaultPolicy;
            }

            #endregion
        }

        #endregion

        private class SettingValuesHelper
        {
            #region Variables

            private TableSchema _settingDefinitionsSchema;

            private readonly List<SettingValueRow> _settingValuesToInsert;
            private readonly List<long> _settingValuesToDelete;
            private readonly List<ChangedRow> _settingValuesToUpdate;
            private readonly Dictionary<string, Policy> _userChangedPolicies, _userUnchangedPolicies;

            #endregion Variables

            #region Lifetime

            public SettingValuesHelper(TableSchema settingDefinitionsSchema, Dictionary<string, Policy> userChangedPolicies, Dictionary<string, Policy> userUnchangedPolicies)
            {
                _settingDefinitionsSchema = settingDefinitionsSchema;
                _settingValuesToInsert = new List<SettingValueRow>();
                _settingValuesToUpdate = new List<ChangedRow>();
                _settingValuesToDelete = new List<long>();
                _userChangedPolicies = userChangedPolicies;
                _userUnchangedPolicies = userUnchangedPolicies;
            }

            #endregion Lifetime

            #region Properties

            public ICollection<SettingValueRow> Inserts
            {
                get { return _settingValuesToInsert; }
            }

            public ICollection<long> Deletes
            {
                get { return _settingValuesToDelete; }
            }

            public ICollection<ChangedRow> Updates
            {
                get { return _settingValuesToUpdate; }
            }

            #endregion Properties

            #region Methods

            public string GenerateSql()
            {
                StringBuilder sqlStmt = new StringBuilder();
                AppendSettingValuesInsertSql(sqlStmt);
                AppendSettingValuesUpdateSql(sqlStmt);
                AppendSettingValuesDeleteSql(sqlStmt);
                return sqlStmt.ToString();
            }

            #endregion Methods

            #region Private Implementation

            //private void AppendSettingValuesInsertSql(StringBuilder sqlStmt)
            //{
            //    foreach (SettingValueRow settingValueRow in _settingValuesToInsert)
            //    {
            //        sqlStmt.AppendFormat("INSERT INTO settingvalues (value, componentid, settingdefinitionid) SELECT factoryDefaultValue, componentid, settingdefinitionid FROM components INNER JOIN scopes USING(scopeid) INNER JOIN settings USING(scopeid) INNER JOIN settingdefinitions USING (settingid) LEFT JOIN channeldrivers USING (channeldriverid) WHERE scopes.name='{0}' AND componentnumber={1} AND settings.name='{2}' AND channeldrivers.name",
            //                             EscapeSqlValues(settingValueRow.Scope), settingValueRow.ComponentNumber, EscapeSqlValues(settingValueRow.Setting));
            //        if (settingValueRow.ChannelDriver == null)
            //        {
            //            sqlStmt.AppendLine(" is null;");
            //        }
            //        else
            //        {
            //            sqlStmt.AppendFormat("='{0}';", settingValueRow.ChannelDriver).AppendLine();
            //        }
            //    }
            //}
            private void AppendSettingValuesInsertSql(StringBuilder sqlStmt)
            {
                foreach (SettingValueRow settingValueRow in _settingValuesToInsert)
                {
                    sqlStmt.AppendFormat("INSERT INTO settingvalues (value, componentid, settingdefinitionid) SELECT factoryDefaultValue, componentid, settingdefinitionid FROM components INNER JOIN scopes ON components.scopeId = scopes.scopeId INNER JOIN settings ON scopes.scopeId = settings.scopeId INNER JOIN settingdefinitions ON settings.settingId = settingdefinitions.settingId LEFT JOIN channeldrivers ON settingdefinitions.channelDriverId = channeldrivers.channelDriverId WHERE scopes.name='{0}' AND componentnumber={1} AND settings.name='{2}' AND channeldrivers.name",
                                         EscapeSqlValues(settingValueRow.Scope), settingValueRow.ComponentNumber, EscapeSqlValues(settingValueRow.Setting));
                    if (settingValueRow.ChannelDriver == null)
                    {
                        sqlStmt.AppendLine(" is null;");
                    }
                    else
                    {
                        sqlStmt.AppendFormat("='{0}';", settingValueRow.ChannelDriver).AppendLine();
                    }
                }
            }
            private void AppendSettingValuesUpdateSql(StringBuilder sqlStmt)
            {
                TableColumnName defaultValueColumn = _settingDefinitionsSchema.FindByDbColumnName("factoryDefaultValue");
                TableColumnName minvalColumn = _settingDefinitionsSchema.FindByDbColumnName("minval");
                TableColumnName maxvalColumn = _settingDefinitionsSchema.FindByDbColumnName("maxval");
                TableColumnName accessColumn = _settingDefinitionsSchema.FindByDbColumnName("accesslevel");
                TableColumnName isVisibleColumn = _settingDefinitionsSchema.FindByDbColumnName("isVisible");
                TableColumnName settingValuesValueColumn = new TableColumnName("value");

                foreach (ChangedRow settingDefinitionRow in _settingValuesToUpdate)
                {
                    Policy userChangedPolicy;
                    Policy userUnchangedPolicy;
                    if (!_userChangedPolicies.TryGetValue(((string[])settingDefinitionRow.UniqueIdentifier)[0], out userChangedPolicy))
                    {
                        userChangedPolicy = Policy.DefaultPolicy(true, settingDefinitionRow, accessColumn);
                    }
                    if (!_userUnchangedPolicies.TryGetValue(((string[])settingDefinitionRow.UniqueIdentifier)[0], out userUnchangedPolicy))
                    {
                        userUnchangedPolicy = Policy.DefaultPolicy(false, settingDefinitionRow, accessColumn);
                    }

                    // if our visibility changes, we want our setting value to be set to the definitions default,
                    // since the user either couldn't edit it before or can't edit it now.  In this case we ignore the policy and just update the setting.
                    if (settingDefinitionRow.ChangedValue(isVisibleColumn) != null)
                    {
                        //sqlStmt.AppendFormat("UPDATE settingvalues NATURAL JOIN settingdefinitions SET {0} WHERE ({1});", settingValuesValueColumn.ToSetString(_settingDefinitionsSchema.TableName, defaultValueColumn), _settingDefinitionsSchema.UniqueColumnName.ToWhereEqualString(_settingDefinitionsSchema.TableName, settingDefinitionRow.UniqueIdentifier)).AppendLine();
                        //sqlStmt.Append(DbFactory.Instance.Driver.SqlStatementWarehouse.GetUpdateVisibleOrDefaultColumnStatement(settingValuesValueColumn.ToSetString(_settingDefinitionsSchema.TableName, defaultValueColumn), _settingDefinitionsSchema.UniqueColumnName.ToWhereEqualString(_settingDefinitionsSchema.TableName, settingDefinitionRow.UniqueIdentifier))).Append(";").AppendLine();
                    }
                    else if (settingDefinitionRow.ChangedValue(defaultValueColumn) != null)
                    {
                        StringBuilder workingSqlStmt = new StringBuilder();
                        
                        //workingSqlStmt.AppendFormat("UPDATE settingvalues NATURAL JOIN settingdefinitions SET {0} WHERE ({1})", settingValuesValueColumn.ToSetString(settingDefinitionRow.ChangedValue(defaultValueColumn)), _settingDefinitionsSchema.UniqueColumnName.ToWhereEqualString(_settingDefinitionsSchema.TableName, settingDefinitionRow.UniqueIdentifier)).AppendLine();
                       // workingSqlStmt.AppendFormat(DbFactory.Instance.Driver.SqlStatementWarehouse.GetUpdateVisibleOrDefaultColumnStatement(settingValuesValueColumn.ToSetString(settingDefinitionRow.ChangedValue(defaultValueColumn)), _settingDefinitionsSchema.UniqueColumnName.ToWhereEqualString(_settingDefinitionsSchema.TableName, settingDefinitionRow.UniqueIdentifier))).AppendLine();

                        if (userChangedPolicy.UpdateDefault == userUnchangedPolicy.UpdateDefault)
                        {
                            if (userChangedPolicy.UpdateDefault)
                            {
                                workingSqlStmt.AppendLine(";");
                            }
                            else
                            {
                                workingSqlStmt.Length = 0;
                            }
                        }
                        else if (userChangedPolicy.UpdateDefault)
                        {
                            workingSqlStmt.AppendFormat(" AND (NOT ({0}));", settingValuesValueColumn.ToWhereEqualString("settingvalues", settingDefinitionRow.OldValue(defaultValueColumn))).AppendLine();
                        }
                        else
                        {
                            workingSqlStmt.AppendFormat(" AND ({0});", settingValuesValueColumn.ToWhereEqualString("settingvalues", settingDefinitionRow.OldValue(defaultValueColumn))).AppendLine();
                        }

                        sqlStmt.Append(workingSqlStmt);
                    }
                    if (settingDefinitionRow.ChangedValue(minvalColumn) != null)
                    {
                        StringBuilder workingSqlStmt = new StringBuilder();
                        //workingSqlStmt.AppendFormat("UPDATE settingvalues NATURAL JOIN settingdefinitions SET {0} WHERE ({1}) AND minval !='' AND (CAST(value AS SIGNED) < CAST(minval AS SIGNED))", settingValuesValueColumn.ToSetString(settingDefinitionRow.ChangedValue(minvalColumn)), _settingDefinitionsSchema.UniqueColumnName.ToWhereEqualString(_settingDefinitionsSchema.TableName, settingDefinitionRow.UniqueIdentifier));
                        //workingSqlStmt.AppendFormat(DbFactory.Instance.Driver.SqlStatementWarehouse.GetUpdateMinValueColumnStatement(settingValuesValueColumn.ToSetString(settingDefinitionRow.ChangedValue(minvalColumn)), _settingDefinitionsSchema.UniqueColumnName.ToWhereEqualString(_settingDefinitionsSchema.TableName, settingDefinitionRow.UniqueIdentifier)));

                        if (userChangedPolicy.UpdateMin == userUnchangedPolicy.UpdateMin)
                        {
                            if (userChangedPolicy.UpdateMin)
                            {
                                workingSqlStmt.AppendLine(";");
                            }
                            else
                            {
                                workingSqlStmt.Length = 0;
                            }
                        }
                        else if (userChangedPolicy.UpdateMin)
                        {
                            workingSqlStmt.AppendFormat(" AND (NOT ({0}));", settingValuesValueColumn.ToWhereEqualString("settingvalues", settingDefinitionRow.OldValue(defaultValueColumn))).AppendLine();
                        }
                        else
                        {
                            workingSqlStmt.AppendFormat(" AND ({0});", settingValuesValueColumn.ToWhereEqualString("settingvalues", settingDefinitionRow.OldValue(defaultValueColumn))).AppendLine();
                        }

                        sqlStmt.Append(workingSqlStmt);
                    }
                    if (settingDefinitionRow.ChangedValue(maxvalColumn) != null)
                    {
                        StringBuilder workingSqlStmt = new StringBuilder();
                        
                        //workingSqlStmt.AppendFormat("UPDATE settingvalues NATURAL JOIN settingdefinitions SET {0} WHERE ({1}) AND maxval!='' AND (CAST(value AS SIGNED) > CAST(maxval as SIGNED))", settingValuesValueColumn.ToSetString(settingDefinitionRow.ChangedValue(maxvalColumn)), _settingDefinitionsSchema.UniqueColumnName.ToWhereEqualString(_settingDefinitionsSchema.TableName, settingDefinitionRow.UniqueIdentifier));
                        //workingSqlStmt.AppendFormat(DbFactory.Instance.Driver.SqlStatementWarehouse.GetUpdateMaxValueColumnStatement(settingValuesValueColumn.ToSetString(settingDefinitionRow.ChangedValue(maxvalColumn)), _settingDefinitionsSchema.UniqueColumnName.ToWhereEqualString(_settingDefinitionsSchema.TableName, settingDefinitionRow.UniqueIdentifier)));

                        if (userChangedPolicy.UpdateMax == userUnchangedPolicy.UpdateMax)
                        {
                            if (userChangedPolicy.UpdateMax)
                            {
                                workingSqlStmt.AppendLine(";");
                            }
                            else
                            {
                                workingSqlStmt.Length = 0;
                            }
                        }
                        else if (userChangedPolicy.UpdateMax)
                        {
                            workingSqlStmt.AppendFormat(" AND (NOT ({0}));", settingValuesValueColumn.ToWhereEqualString("settingvalues", settingDefinitionRow.OldValue(defaultValueColumn))).AppendLine();
                        }
                        else
                        {
                            workingSqlStmt.AppendFormat(" AND ({0});", settingValuesValueColumn.ToWhereEqualString("settingvalues", settingDefinitionRow.OldValue(defaultValueColumn))).AppendLine();
                        }

                        sqlStmt.Append(workingSqlStmt);
                    }
                }
            }

            private void AppendSettingValuesDeleteSql(StringBuilder sqlStmt)
            {
                foreach (long settingValueId in _settingValuesToDelete)
                {
                    sqlStmt.AppendFormat("DELETE FROM settingvalues WHERE settingvalueid={0};", settingValueId).AppendLine();
                }
            }

            #endregion Private Implementation
        }

        private class SettingValueRow : TableElement
        {
            #region Lifetime

            public SettingValueRow(string scope, int? componentNumber, string setting, string channelDriver)
                : base(scope, componentNumber.HasValue ? componentNumber.ToString() : null, setting, channelDriver)
            {
            }

            #endregion Lifetime

            #region Properties

            public string Scope
            {
                get { return _elements[0]; }
            }

            public int? ComponentNumber
            {
                get
                {
                    if (_elements[1] == null)
                    {
                        return null;
                    }
                    return int.Parse(_elements[1], CultureInfo.InvariantCulture);
                }
            }

            public string Setting
            {
                get { return _elements[2]; }
            }

            public string ChannelDriver
            {
                get { return _elements[3]; }
            }

            #endregion Properties
        }
    }
}
