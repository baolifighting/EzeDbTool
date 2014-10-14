using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.XPath;

namespace Libraries.EzeDbCommon.Content
{
	public delegate IDictionary<string, XmlColumnAlias> GetColumnAliasesDelegate();
	/// <summary>
	/// Helps find and resolve the differences between a 3VR db table and an xml file that defines the same db.
	/// </summary>
	public class DBXmlManipulator
	{
		#region Constants

		//Todo: get rid of this
		public const string DbNullReplacementString = "dbnull";

		private const System.Globalization.NumberStyles _dbNumberStyle = System.Globalization.NumberStyles.Integer |
			System.Globalization.NumberStyles.AllowDecimalPoint;

		#endregion Constants

		#region Variables
		protected DbXmlManipulatorContext _context;
		protected readonly TableSchema _dbXmlTable;

		protected ChangedRow[] _changedRows;

		#endregion

		#region Lifetime
		public DBXmlManipulator(DbXmlManipulatorContext context)
		{
			if (context == null)
			{
				throw new ArgumentNullException();
			}
			_context = context;
			try
			{
				if (_context.RowExpression.ReturnType != XPathResultType.NodeSet)
				{
					throw new ArgumentException("expr must return a node set");
				}

				if (_context.DbRowParsed == null)
				{
					_context.DbRowParsed = delegate { };
				}
				if (_context.XmlRowParsed == null)
				{
					_context.XmlRowParsed = delegate { };
				}

				bool validUnique = false;

				foreach (string name in _context.UniqueColumnNames)
				{
					if (name != null)
					{
						validUnique = true;
					}
					else
					{
						validUnique = false;
						break;
					}
				}

				if (!validUnique)
				{
					_changedRows = new ChangedRow[0];
					return;
				}

				Dictionary<string, string> aliasNamesByDbColumn = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
				foreach (KeyValuePair<string, XmlColumnAlias> pair in _context.AliasesByName)
				{
					aliasNamesByDbColumn.Add(pair.Value.DbColumnName, pair.Key);
				}

				IDataReader reader;
				try
				{
					//reader = _context.DataReader.ExecuteQuery(DbFactory.Instance.Driver.SqlStatementWarehouse.GetColumnsInfoQueryStatement(context.DbTableName));
					reader = null;
				}
				catch (Exception ex)
				{
					throw new ApplicationException("Invalid table or db.", ex);
				}

				List<TableColumnName> columnNames = new List<TableColumnName>();
				List<string> uniqueColumnNameList = new List<string>();
				List<string> uniqueColumnAliasNames = new List<string>();
				List<DbColumnType> uniqueColumnType = new List<DbColumnType>();
				string dbPrimaryKey = null;
				int rows = 0;
				string actualColumnStr = "";
				while (reader.Read())
				{
					rows++;
					string columnString = reader.GetString(0);

					actualColumnStr += columnString + ",";
					DbColumnType type = new DbColumnType(reader.GetString(1));
					if (reader.GetString(3) == "PRI")
					{
						// Assuming that there's only one of these since all of our tables use auto incrementing primary keys.
						//  If that changes this will need to change too
						dbPrimaryKey = columnString;
					}
					if (columnString != null)
					{
						string alias = null;
						if (aliasNamesByDbColumn.ContainsKey(columnString))
						{
							alias = aliasNamesByDbColumn[columnString];
						}
						bool isUnique = false;
						foreach (string uniqueColumnName in _context.UniqueColumnNames)
						{
							if (uniqueColumnName.Equals(columnString, StringComparison.InvariantCultureIgnoreCase) || uniqueColumnName.Equals(alias, StringComparison.InvariantCultureIgnoreCase))
							{
								isUnique = true;
								uniqueColumnNameList.Add(alias ?? uniqueColumnName);
								uniqueColumnType.Add(type);
								if (alias != null)
								{
									uniqueColumnAliasNames.Add(alias);
								}
								break;
							}
						}

						if (!isUnique)
						{
							TableColumnName columnName = new TableColumnName(alias ?? columnString);
							columnName.AssociateDbColumnType(_context.DbTableName, type);
							columnNames.Add(columnName);
							if (alias != null)
							{
								columnNames[columnNames.Count - 1].SetAliases(alias, _context.AliasesByName[alias]);
							}
						}
					}
				}
				reader.Close();
				
				if (uniqueColumnNameList.Count == _context.UniqueColumnNames.Count)
				{
					TableColumnName uniqueTableColumnName = new TableColumnName(_context.UniqueColumnNames.ToArray());
					uniqueTableColumnName.AssociateDbColumnType(_context.DbTableName, uniqueColumnType.ToArray());
					foreach (string alias in uniqueColumnAliasNames)
					{
						uniqueTableColumnName.SetAliases(alias, _context.AliasesByName[alias]);
					}
					columnNames.Add(uniqueTableColumnName);
				}
				else
				{
					throw new ArgumentException("Length mismatch: unique columns found vs unique column supplied");
				}
				
				_dbXmlTable = new TableSchema(_context.DbTableName, dbPrimaryKey, columnNames.ToArray(), columnNames.Count - 1);

				Compare(_context.NavigableXml);
			}
			catch (Exception ex)
			{
				throw new ApplicationException(string.Format("Error updating {0} from xml: {1}.", _context.DbTableName, ex.Message), ex);
			}
		}

		#endregion

		#region Methods

		public virtual string InsertSql()
		{
			return GetSql(ChangedRow.ChangeType.ADD);
		}

		public virtual string DeleteSql()
		{
			return GetSql(ChangedRow.ChangeType.REMOVE);
		}

		public virtual string UpdateSql()
		{
			return GetSql(ChangedRow.ChangeType.UPDATE);
		}

		protected virtual string GetSql(ChangedRow.ChangeType changeType)
		{
			StringBuilder sqlStmt = new StringBuilder();
			foreach (ChangedRow row in _changedRows)
			{
				if (row.Type == changeType)
				{
					sqlStmt.Append(row.GenerateSQL(_context.DbTableName));
				}
			}
			return sqlStmt.ToString();
		}

		/// <summary>
		/// We need to escape some characters so that they don't get missread by the database when we send commands
		/// </summary>
		/// <param name="text">The source string to be escaped</param>
		/// <returns>The source string + '/' characters where appropriate</returns>
		protected static string EscapeSqlValues(string text)
		{
			string temp = text;

			temp = temp.Replace("'", @"\'");

			temp = temp.Replace(";", @"\;");

			return temp;
		}

		#endregion

		#region Private Implementation
		private IDictionary<TableColumnName, TableValue> NextDB(IDataReader dbReader)
		{
			while (dbReader.Read())
			{
				Dictionary<TableColumnName, List<object>> dbRawValues = new Dictionary<TableColumnName, List<object>>(_dbXmlTable.Length);

				for (int i = 0; i < _dbXmlTable.ElementLength; ++i)
				{
					List<object> values;
					DbColumnType columnType;
					TableColumnName columnWithThisElement = _dbXmlTable.GetByElementIndex(i, out columnType);
					if (!dbRawValues.TryGetValue(columnWithThisElement, out values))
					{
						values = new List<object>();
						dbRawValues.Add(columnWithThisElement, values);
					}
					if (dbReader.IsDBNull(i))
					{
						values.Add(DbNullReplacementString);
					}
					else if (columnType.Type == DbColumnType.ColumnType.Binary)
					{
						byte[] buffer = new byte[columnType.FieldLength == 0 ? 255 : columnType.FieldLength];
						long length = dbReader.GetBytes(i, 0, buffer, 0, buffer.Length);
						for (long j = length; j < buffer.Length; ++j)
						{
							buffer[j] = 0x20;
						}
						values.Add(buffer);
					}
					else
					{
						values.Add(dbReader.GetString(i));
					}
				}

				IDictionary<TableColumnName, TableValue> dbValues = new Dictionary<TableColumnName, TableValue>();
				foreach (KeyValuePair<TableColumnName, List<object>> keyValuePair in dbRawValues)
				{
					// Only supporting binary/numeric values on single column TableColumnNames
					List<object> rawValues = keyValuePair.Value;
					if (rawValues.Count == 1 && rawValues[0] is byte[])
					{
						dbValues.Add(keyValuePair.Key, new TableBinaryValue((byte[])rawValues[0]));
					}
					else
					{
						dbValues.Add(keyValuePair.Key, new TableValue(Array.ConvertAll(keyValuePair.Value.ToArray(), obj => (string)obj)));
					}
				}
				_context.DbRowParsed(ref dbValues);
				if (dbValues != null)
				{
					return dbValues;
				}
			}
			return null;
		}

		private IDictionary<TableColumnName, TableValue> NextXml(XPathNodeIterator iterator)
		{
			IDictionary<TableColumnName, TableValue> xmlValues = new Dictionary<TableColumnName, TableValue>(_dbXmlTable.Length);

			while (iterator.MoveNext())
			{
				XPathNavigator settingNav = iterator.Current;
				if (settingNav.MoveToFirstAttribute())
				{
					do
					{
						if (!_context.XmlElementsToIgnore.Contains(settingNav.Name))
						{
							HandleXmlStrings(xmlValues, settingNav.Name, settingNav.Value);
						}
					} while (settingNav.MoveToNextAttribute());
					settingNav.MoveToParent();
				}
				if (settingNav.MoveToFirstChild())
				{
					do
					{
						if (!_context.XmlElementsToIgnore.Contains(settingNav.Name))
						{
							HandleXmlStrings(xmlValues, settingNav.Name, settingNav.Value);
						}
					} while (settingNav.MoveToNext());
					settingNav.MoveToParent();
				}
				_context.XmlRowParsed(ref xmlValues);
				if (xmlValues != null)
				{
					return xmlValues;
				}
			}

			return null;
		}

		private void HandleXmlStrings(IDictionary<TableColumnName, TableValue> xmlValues, string name, string value)
		{
			value = value.Trim();

			TableColumnName columnName = _dbXmlTable.FindByElementName(name);
			if (columnName == null)
			{
				throw new ArgumentException(string.Format("Unknown column '{0}' with value '{1}' in xml", name, value));
			}

#if DEBUG
			if (columnName == _dbXmlTable.UniqueColumnName && value.FirstOrDefault(c => !char.IsLetterOrDigit(c) && c != '.') != char.MinValue)
			{
				//ConsoleExtention.WriteLineWithLineBreaks(@"WARNING! Db xml for table {0} contains a unique identifier '{1}' which may not sort properly compared to sorted values in the database. Unique identifiers should be made up of letters and numbers only.", _dbXmlTable.TableName, value);
			}
#endif

			TableValue currentValue = null;
			if (xmlValues.ContainsKey(columnName))
			{
				currentValue = xmlValues[columnName];
			}
			currentValue = columnName.SetValueForColumn(name, value, currentValue, false);
			if (currentValue != null)
			{
				xmlValues[columnName] = currentValue;
			}
			else
			{
				throw new ApplicationException("Got null TableValue back from SetValueForColumn call");
			}
		}

		private ChangedRow[] CompareXmlToDB(XPathNodeIterator iterator, IDataReader dbReader)
		{
			List<ChangedRow> changedRows = new List<ChangedRow>();
			IDictionary<TableColumnName, TableValue> dbValues = NextDB(dbReader);
			IDictionary<TableColumnName, TableValue> xmlValues = NextXml(iterator);

			while (true)
			{
				if (xmlValues == null)
				{
					while (dbValues != null)
					{
						changedRows.Add(new ChangedRow(_dbXmlTable.UniqueColumnName,
							dbValues[_dbXmlTable.UniqueColumnName],
							false,
							true,
							dbValues,
							null));
						dbValues = NextDB(dbReader);
					}
					break;
				}
				if (dbValues == null)
				{
					while (xmlValues != null)
					{
						changedRows.Add(new ChangedRow(_dbXmlTable.UniqueColumnName,
							xmlValues[_dbXmlTable.UniqueColumnName],
							true,
							false,
							null,
							xmlValues));
						xmlValues = NextXml(iterator);
					}
					break;
				}

				// ignore case on the unique comparisons so that we just update the unique value if it's case got changed.  
				int compare = dbValues[_dbXmlTable.UniqueColumnName].CompareTo(xmlValues[_dbXmlTable.UniqueColumnName], true);
				if (compare != 0)
				{
					if (compare > 0)
					{
						changedRows.Add(new ChangedRow(_dbXmlTable.UniqueColumnName,
							xmlValues[_dbXmlTable.UniqueColumnName],
							true,
							false,
							null,
							xmlValues));
						xmlValues = NextXml(iterator);
					}
					else if (compare < 0)
					{
						changedRows.Add(new ChangedRow(_dbXmlTable.UniqueColumnName,
							dbValues[_dbXmlTable.UniqueColumnName],
							false,
							true,
							dbValues,
							null));
						dbValues = NextDB(dbReader);
					}
				}
				else
				{
					Dictionary<TableColumnName, TableValue> changedValues = new Dictionary<TableColumnName, TableValue>(xmlValues);
					foreach (TableColumnName column in xmlValues.Keys)
					{
						if (xmlValues[column].Equals(dbValues[column]))
						{
							changedValues.Remove(column);
						}
					}
					if (changedValues.Count != 0)
					{
						changedRows.Add(new ChangedRow(_dbXmlTable.UniqueColumnName,
							dbValues[_dbXmlTable.UniqueColumnName],
							true,
							true,
							dbValues,
							changedValues));
					}
					xmlValues = NextXml(iterator);
					dbValues = NextDB(dbReader);
				}
			}

			return changedRows.ToArray();
		}

		private void Compare(IXPathNavigable xmlRoot)
		{
			IDataReader reader = null;

			try
			{
				List<XmlDataType> uniqueColumnTypes = new List<XmlDataType>();
				reader = _context.DataReader.ExecuteQuery(_dbXmlTable.UniqueColumnName.ToValuesSql(_context.DbTableName, 1));
				if (reader.Read())
				{
					for (int i = 0; i < _dbXmlTable.UniqueColumnName.Length; ++i)
					{
						string uniqueColumnMember = Convert.ToString(reader.GetValue(i));
						double testDouble;
						if (Double.TryParse(uniqueColumnMember, NumberStyles.Float, CultureInfo.InvariantCulture, out testDouble))
						{
							uniqueColumnTypes.Add(XmlDataType.Number);
						}
						else
						{
							uniqueColumnTypes.Add(XmlDataType.Text);
						}
					}
				}
				else
				{
					for (int i = 0; i < _dbXmlTable.UniqueColumnName.Length; ++i)
					{
						uniqueColumnTypes.Add(XmlDataType.Text);
					}
				}
				reader.Close();
				reader = null;

				string sqlStmt = _dbXmlTable.ToValuesSelectSql(_dbXmlTable.UniqueColumnName);
				reader = _context.DataReader.ExecuteQuery(sqlStmt);
				_dbXmlTable.UniqueColumnName.AddXPathSort(xmlRoot, _context.RowExpression, uniqueColumnTypes.ToArray());

				XPathNavigator nav = xmlRoot.CreateNavigator();
				XPathNodeIterator iterator = nav.Select(_context.RowExpression);

				_changedRows = CompareXmlToDB(iterator, reader);
			}
			finally
			{
				if (reader != null)
				{
					reader.Close();
				}
			}
		}

		#endregion

		#region ChangedRow
		protected class ChangedRow : IComparable
		{
			#region Variables
			private IDictionary<TableColumnName, TableValue> _oldValues, _changedValues;
			private TableColumnName _keyColumnName;
			private TableValue _keyValue;
			public readonly ChangeType Type;
			#endregion

			#region Lifetime
			public ChangedRow(TableColumnName primaryKeyName, TableValue primaryKeyValue, bool existsInOurs, bool existsInTheirs, IDictionary<TableColumnName, TableValue> oldValues, IDictionary<TableColumnName, TableValue> changedValues)
			{
				_keyColumnName = primaryKeyName;
				_keyValue = primaryKeyValue;
				if (existsInOurs && !existsInTheirs)
				{
					Type = ChangeType.ADD;
				}
				else if (!existsInOurs && existsInTheirs)
				{
                    var values = (string[])primaryKeyValue;
                    // make sure that the count is more than one before we get the name to compare with our dynamic driver
					//if ((values.Count() > 1) && (!string.IsNullOrEmpty(values[1])) && (values[1] == ChannelDriver.OnvifDriverName 
					//																|| values[1] == ChannelDriver.AxisDefaultDriverName
					//																|| values[1] == ChannelDriver.ArecontDefaultDriverName
					//																|| values[1] == ChannelDriver.VivotekDriverName
					//																//|| values[1] == ChannelDriver.HikvisionDynamicDriverName
					//																//|| values[1] == ChannelDriver.PanasonicDefaultDriverName
					//																))

					//{
					//	// In the case of dynamic drivers, we are adding/removing setting definitions to the DB ourselves
					//	// We do not want DbTools to remove any dynamic setting definitions from the database
					//	Type = ChangeType.NOTHING;
					//}
					//else
					//{
					//	Type = ChangeType.REMOVE;
					//}
				}
				else
				{
					Type = ChangeType.UPDATE;
				}
				_oldValues = oldValues;
				_changedValues = changedValues;
			}
			#endregion

			#region Methods
			public TableValue UniqueIdentifier
			{
				get
				{
					return _keyValue;
				}
			}

			public TableColumnName[] ChangedColumnNames
			{
				get
				{
					TableColumnName[] keys = new TableColumnName[_changedValues.Keys.Count];

					int i = 0;
					foreach (TableColumnName key in _changedValues.Keys)
					{
						keys[i] = key;
						++i;
					}

					return keys;
				}
			}

			public TableValue[] ChangedValues
			{
				get
				{
					TableValue[] values = new TableValue[_changedValues.Values.Count];

					int i = 0;
					foreach (TableValue val in _changedValues.Values)
					{
						values[i] = val;
						++i;
					}

					return values;
				}
			}

			public TableValue ChangedValue(TableColumnName columnName)
			{
				if (_changedValues.ContainsKey(columnName))
				{
					return _changedValues[columnName];
				}
				return null;
			}

			public TableValue OldValue(TableColumnName columnName)
			{
				if (_oldValues.ContainsKey(columnName))
				{
					return _oldValues[columnName];
				}
				return null;
			}

			public virtual string GenerateSQL(string tableName)
			{
				string sqlStmt;

				switch (Type)
				{
					case ChangeType.ADD:
						if (_changedValues == null || _changedValues.Count == 0)
						{
							return string.Empty;
						}

						StringBuilder insertColumns = new StringBuilder();
						StringBuilder insertValues = new StringBuilder();
						foreach (TableColumnName column in _changedValues.Keys)
						{
							insertColumns.AppendFormat("{0},", column.ToSelectString());
							insertValues.AppendFormat("{0},", column.ToInsertValuesString(_changedValues[column]));
						}
						--insertColumns.Length;
						--insertValues.Length;

						sqlStmt = string.Format("INSERT INTO {0} ({1}) VALUES ({2});{3}", tableName, insertColumns, insertValues, Environment.NewLine);
						break;
					case ChangeType.REMOVE:
						//sqlStmt = string.Format("DELETE FROM {0} WHERE {1} LIMIT 1;{2}", tableName, _keyColumnName.ToWhereEqualString(tableName, _keyValue), Environment.NewLine);
						//sqlStmt = string.Format(DbFactory.Instance.Driver.SqlStatementWarehouse.GetDeleteStatementByLimitCondition(tableName, _keyColumnName.ToWhereEqualString(tableName, _keyValue)));
						sqlStmt = null;
						break;
					case ChangeType.UPDATE:
						if (_changedValues == null || _changedValues.Count == 0)
						{
							return string.Empty;
						}

						StringBuilder setClause = new StringBuilder();
						foreach (TableColumnName column in _changedValues.Keys)
						{
							setClause.AppendFormat("{0},", column.ToSetString(_changedValues[column]));
						}
						--setClause.Length;

						sqlStmt = string.Format("UPDATE {0} SET {1} WHERE {2};{3}", tableName, setClause, _keyColumnName.ToWhereEqualString(tableName, _keyValue), Environment.NewLine);
						break;
                    case ChangeType.NOTHING:
					default:
						sqlStmt = string.Empty;
						break;
				}

				return sqlStmt;
			}
			#endregion

			#region IComparable Members

			public int CompareTo(ChangedRow row)
			{
				return _keyValue.CompareTo(row._keyValue);
			}

			public int CompareTo(object obj)
			{
				return CompareTo((ChangedRow)obj);
			}

			#endregion

			#region ChangeType
			public enum ChangeType
			{
				ADD,
				REMOVE,
				UPDATE,

                // This is intended for the Onvif driver.
                // Since we are adding/removing setting definitions to
                // the Onvif driver ourselves, we do not want DbTools
                // getting in the way and changing things.
                NOTHING
			}
			#endregion
		}

		#endregion

		#region TableAbstraction
		#region TableColumnName
		public class TableColumnName : TableElement, ITableColumnNameCollectable
		{
			private readonly XmlColumnAlias[] _aliases;
			private readonly DbColumnType[] _columnTypes;

			private string _dbTableName;

			public TableColumnName(params string[] elements)
				: base(elements)
			{
				_aliases = new XmlColumnAlias[elements.Length];
				if (_elements.Length == 0)
				{
					throw new ArgumentException("A column name must have at least one element");
				}
				for (int i = 0; i < _elements.Length; ++i)
				{
					_elements[i] = _elements[i].ToLowerInvariant();
				}
				_columnTypes = new DbColumnType[_elements.Length];
				for (int i = 0; i < _columnTypes.Length; ++i)
				{
					_columnTypes[i] = new DbColumnType(DbColumnType.ColumnType.Text, 0);
				}
			}

			public int Length
			{
				get { return _elements.Length; }
			}

			public void AssociateDbColumnType(string dbTableName, params DbColumnType[] types)
			{
				_dbTableName = dbTableName;
				Array.Copy(types, 0, _columnTypes, 0, _columnTypes.Length);
			}

			public void SetAliases(string name, XmlColumnAlias alias)
			{
				for (int i = 0; i < _elements.Length; ++i)
				{
					if (_elements[i].Equals(name, StringComparison.InvariantCultureIgnoreCase))
					{
						_aliases[i] = alias;
						return;
					}
				}
			}

			public TableValue SetValueForColumn(string columnName, string elementValue, TableValue tableValue, bool overwrite)
			{
				int index = -1;
				for (int i = 0; i < _elements.Length; ++i)
				{
					if (_elements[i] == columnName.ToLowerInvariant())
					{
						index = i;
						break;
					}
				}

				if (index >= 0)
				{
					string[] valueElements;
					if (tableValue == null)
					{
						valueElements = new string[_elements.Length];
					}
					else
					{
						valueElements = Elements(tableValue);
						if (valueElements.Length != _elements.Length)
						{
							throw new ArgumentException("tableValue does not have the same number of elements as this TableColumn");
						}
					}
					if (valueElements[index] == null || overwrite)
					{
						// try to throw an exception here if the value we're going to be setting is obviously wrong.
						DbColumnType columnType = _columnTypes[index];
						Decimal ignore;
						if (elementValue != DbNullReplacementString && _aliases[index] == null && columnType.Type == DbColumnType.ColumnType.Numeric && !Decimal.TryParse(elementValue, _dbNumberStyle, CultureInfo.InvariantCulture, out ignore))
						{
							throw new ArgumentException(string.Format("Invalid value for column `{0}` in table `{1}`. XML has value '{2}' which is invalid on db column type {3}.", columnName, _dbTableName, elementValue, columnType.RawType));
						}
						//valueElements[index] = elementValue.RemoveEscapeCharacterForXml();
					}
					return new TableValue(valueElements);
				}
				return null;
			}

			public string ToSelectString()
			{
				StringBuilder builder = new StringBuilder();
				for (int i = 0; i < _elements.Length; ++i)
				{
					builder.AppendFormat("{0},", _aliases[i] == null ? _elements[i] : _aliases[i].DbColumnName);
				}
				builder.Length = builder.Length - 1;
				return builder.ToString();
			}

			public string ToSetString(TableValue value)
			{
				string[] otherElements = Elements(value);

				if (otherElements.Length != _elements.Length)
				{
					throw new ArgumentException("length missmatch");
				}

				return ToSetString(i => otherElements[i] == DbNullReplacementString ? DbNullReplacementString : value.ElementAsSQLValue(i));
			}

			public string ToSetString(string dbTableName, TableColumnName columnReference)
			{
				string[] referenceElements = Elements(columnReference);

				if (referenceElements.Length != _elements.Length)
				{
					throw new ArgumentException("length missmatch");
				}

				return ToSetString(i => string.Format("{0}.{1}", dbTableName, referenceElements[i]));
			}

			public string ToInsertValuesString(TableValue value)
			{
				StringBuilder builder = new StringBuilder();
				string[] valueElements = Elements(value);
				for (int i = 0; i < valueElements.Length; ++i)
				{
					if (valueElements[i] == DbNullReplacementString)
					{
						builder.Append("null,");
					}
					else
					{
						if (_aliases[i] != null)
						{
							builder.AppendFormat("(SELECT {0} FROM {1} WHERE {2}={3}),",
								_aliases[i].DbColumnName2,
								_aliases[i].DbTableEqual,
								_aliases[i].DbColumnEqual,
								value.ElementAsSQLValue(i));
						}
						else
						{
							builder.Append(value.ElementAsSQLValue(i)).Append(",");
						}
					}
				}
				builder.Length = builder.Length - 1;
				return builder.ToString();
			}

			public string ToWhereEqualString(string dbTableName, TableValue value)
			{
				string[] otherElements = Elements(value);

				if (otherElements.Length != _elements.Length)
				{
					throw new ArgumentException("length missmatch");
				}

				StringBuilder builder = new StringBuilder();
				for (int i = 0; i < _elements.Length; ++i)
				{
					if (builder.Length != 0)
					{
						builder.Append(" AND ");
					}
					if (_aliases[i] == null)
					{
						if (otherElements[i] == DbNullReplacementString)
						{
							builder.AppendFormat("{0}.{1} is null", dbTableName, _elements[i]);
						}
						else
						{
							builder.AppendFormat("{0}.{1}={2}", dbTableName, _elements[i], value.ElementAsSQLValue(i));
						}
					}
					else
					{
						if (otherElements[i] == DbNullReplacementString)
						{
							builder.AppendFormat("{0}.{1} is null", dbTableName, _aliases[i].DbColumnName);
						}
						else
						{
							builder.AppendFormat("{0}.{1}=(SELECT {2} FROM {3} WHERE {4}={5})", dbTableName, _aliases[i].DbColumnName, _aliases[i].DbColumnName2, _aliases[i].DbTableEqual, _aliases[i].DbColumnEqual, value.ElementAsSQLValue(i));
						}
					}
				}
				return builder.ToString();
			}

			public string ToValuesSql(string dbTableName, int limit)
			{
				StringBuilder builder = new StringBuilder();
				//builder.Append("SELECT ");
				//for (int i = 0; i < _elements.Length; ++i)
				//{
				//    string tableName = dbTableName;
				//    string column = _elements[i];
				//    if (_aliases[i] != null)
				//    {
				//        column = _aliases[i].DbColumnEqual;
				//        tableName = "alias" + i;
				//    }
				//    builder.AppendFormat("{0}.{1},", tableName, column);
				//}
				//--builder.Length;
				//builder.AppendFormat(" FROM {0}", dbTableName);
				//for (int i = 0; i < _aliases.Length; ++i)
				//{
				//    if (_aliases[i] != null)
				//    {
				//        builder.AppendFormat(" LEFT JOIN {0} as {1} ON ({1}.{2} = {3}.{4})", _aliases[i].DbTableEqual, "alias" + i, _aliases[i].DbColumnName2, dbTableName, _aliases[i].DbColumnName);
				//    }
				//}
				//builder.AppendFormat(" LIMIT {0};", limit);

				for (int i = 0; i < _elements.Length; ++i)
				{
					string tableName = dbTableName;
					string column = _elements[i];
					if (_aliases[i] != null)
					{
						column = _aliases[i].DbColumnEqual;
						tableName = "alias" + i;
					}
					builder.AppendFormat("{0}.{1},", tableName, column);
				}
				--builder.Length;
				builder.AppendFormat(" FROM {0}", dbTableName);
				for (int i = 0; i < _aliases.Length; ++i)
				{
					if (_aliases[i] != null)
					{
						builder.AppendFormat(" LEFT JOIN {0} as {1} ON ({1}.{2} = {3}.{4})", _aliases[i].DbTableEqual, "alias" + i, _aliases[i].DbColumnName2, dbTableName, _aliases[i].DbColumnName);
					}
				}
				//return DbFactory.Instance.Driver.SqlStatementWarehouse.GetLimitStatement(limit, builder.ToString());
				return null;
			}

			public void AddXPathSort(IXPathNavigable xml, XPathExpression expression, params XmlDataType[] types)
			{
				if (_elements.Length != types.Length)
				{
					throw new ArgumentException("length of xml data types does not match length of elements when adding XPath sort");
				}

				// we need to check the xml to make sure that our column names are in the xml and that our case is right since xml is case sensitive
				string[] xmlElements = new string[_elements.Length];
				XPathNavigator nav = xml.CreateNavigator();
				XPathNodeIterator iterator = nav.Select(expression.Expression + "/@*");
				int found = 0;
				while (iterator.MoveNext())
				{
					for (int i = 0; i < _elements.Length; ++i)
					{
						if (xmlElements[i] == null && _elements[i].Equals(iterator.Current.Name, StringComparison.InvariantCultureIgnoreCase))
						{
							xmlElements[i] = iterator.Current.Name;
							++found;
							break;
						}
					}
					if (found == xmlElements.Length)
					{
						break;
					}
				}
				for (int i = 0; i < xmlElements.Length; ++i)
				{
					if (xmlElements[i] != null)
					{
						// mysql ignores case on text, so we're doing the same thing here
						expression.AddSort("@" + xmlElements[i], XmlSortOrder.Ascending, XmlCaseOrder.None, "", types[i]);
					}
				}
			}

			private string ToSetString(Func<int, string> getSetValue)
			{
				var builder = new StringBuilder();
				for (int i = 0; i < _elements.Length; ++i)
				{
					if (builder.Length != 0)
					{
						builder.Append(",");
					}
					builder.AppendFormat("{0}=", _aliases[i] == null ? _elements[i] : _aliases[i].DbColumnName);
					string value = getSetValue(i);
					if (value == DbNullReplacementString)
					{
						builder.Append("null");
					}
					else if (_aliases[i] != null)
					{
						builder.AppendFormat("(SELECT {0} FROM {1} WHERE {2} = {3})", _aliases[i].DbColumnName2, _aliases[i].DbTableEqual, _aliases[i].DbColumnEqual, value);
					}
					else
					{
						builder.Append(value);
					}
				}
				return builder.ToString();
			}

			string[] ITableColumnNameCollectable.Elements
			{
				get { return _elements; }
			}

			DbColumnType[] ITableColumnNameCollectable.Types
			{
				get { return _columnTypes; }
			}

			XmlColumnAlias[] ITableColumnNameCollectable.Aliases
			{
				get { return _aliases; }
			}

			public static explicit operator string[](TableColumnName key)
			{
				if (key == null)
				{
					return null;
				}

				return (string[])key._elements.Clone();
			}
		}
		#endregion TableColumnName

		#region TableValue

		public class TableValue : TableElement
		{
			public TableValue(params string[] elements)
				: base(elements)
			{
			}

			public virtual string ElementAsSQLValue(int index)
			{
				return string.Format("'{0}'", EscapeSqlValues(_elements[index]));
			}

			public static explicit operator string[](TableValue key)
			{
				if (key == null)
				{
					return null;
				}

				return (string[])key._elements.Clone();
			}
		}

		public class TableBinaryValue : TableValue
		{
			public TableBinaryValue(byte[] array)
				: base(ToHexString(array))
			{
			}

			public override string ElementAsSQLValue(int index)
			{
				return _elements[index];
			}

			private static string ToHexString(byte[] array)
			{
				if (array.Length == 0)
				{
					return string.Empty;
				}

				StringBuilder sb = new StringBuilder((array.Length * 2) + 2);
				sb.Append("0x");
				foreach (byte b in array)
				{
					sb.Append(b.ToString("X2"));
				}
				return sb.ToString();
			}
		}

		public class TableGuidValue : TableBinaryValue
		{
			public TableGuidValue(TableValue value)
				: this(TableValueAsGuid(value))
			{
			}

			public TableGuidValue(Guid guid)
				: base(guid.ToByteArray())
			{
			}

			private static Guid TableValueAsGuid(TableValue value)
			{
				string[] elements = Elements(value);
				if (elements.Length != 1)
				{
					throw new ArgumentException();
				}
				return new Guid(elements[0]);
			}
		}

		#endregion TableValue

		#region TableElement
		public abstract class TableElement : IComparable<TableElement>, IComparable
		{
			#region Variables

			protected readonly string[] _elements;

			#endregion Variables

			#region Lifetime

			protected TableElement(params string[] elements)
			{
				_elements = elements ?? new string[0];
			}

			#endregion Lifetime

			#region Overrides and Interface Implementations

			public int CompareTo(TableElement other)
			{
				return CompareTo(other, false);
			}

			public virtual int CompareTo(TableElement other, bool ignoreCase)
			{
				if (other == null)
				{
					return 1;
				}
				return CompareToRecursive(other, 0, new ElementComparer(ignoreCase));
			}

			int IComparable.CompareTo(object obj)
			{
				return (CompareTo(obj as TableElement));
			}

			public override int GetHashCode()
			{
				int hashCode = 0;
				foreach (string element in _elements)
				{
					if (element != null)
					{
						hashCode ^= element.GetHashCode();
					}
				}
				return hashCode;
			}

			public override bool Equals(object obj)
			{
				if (ReferenceEquals(this, obj))
				{
					return true;
				}
				TableElement element = obj as TableElement;
				return CompareTo(element) == 0;
			}

			#endregion Overrides and Interface Implementations

			#region Private/Protected Implementation

			protected static string[] Elements(TableElement other)
			{
				return other._elements;
			}

			private int CompareToRecursive(TableElement other, int index, IComparer<string> elementComparer)
			{
				if (_elements.Length == index && other._elements.Length == index)
				{
					return 0;
				}
				if (_elements.Length <= index)
				{
					return -1;
				}
				if (other._elements.Length <= index)
				{
					return 1;
				}

				int compare;
				if (_elements[index] == null && other._elements[index] == null)
				{
					compare = 0;
				}
				else if (_elements[index] == null)
				{
					compare = -1;
				}
				else if (other._elements[index] == null)
				{
					compare = 1;
				}
				else
				{
					//compare = _elements[index].CompareTo(other._elements[index]);
					compare = elementComparer.Compare(_elements[index], other._elements[index]);
				}

				if (compare == 0)
				{
					return CompareToRecursive(other, index + 1, elementComparer);
				}

				if (_elements[index] == DbNullReplacementString)
				{
					return -1;
				}
				if (other._elements[index] == DbNullReplacementString)
				{
					return 1;
				}
				return compare;
			}

			private class ElementComparer : IComparer<string>
			{
				private readonly IComparer<string> _baseComparer;

				public ElementComparer(bool ignoreCase)
				{
					_baseComparer = ignoreCase ? StringComparer.InvariantCultureIgnoreCase : StringComparer.InvariantCulture;
				}

				public int Compare(string x, string y)
				{
					Decimal dX, dY;
					if (Decimal.TryParse(x, _dbNumberStyle, CultureInfo.InvariantCulture, out dX) &&
						Decimal.TryParse(y, _dbNumberStyle, CultureInfo.InvariantCulture, out dY))
					{
						return dX.CompareTo(dY);
					}
					return _baseComparer.Compare(x, y);
				}
			}

#if DEBUG
			/// <summary>
			/// Don't use this, it's just here for debugging
			/// </summary>
			public override string ToString()
			{
				return string.Join(",", _elements);
			}
#endif

			#endregion Private/Protected Implementation
		}
		#endregion TableElement

		#region ITableColumnNameCollectable
		private interface ITableColumnNameCollectable
		{
			string[] Elements { get; }
			DbColumnType[] Types { get; }
			XmlColumnAlias[] Aliases { get; }
		}
		#endregion ITableColumnNameCollectable

		#region TableSchema

		public struct DbColumnType
		{
			private static Regex _regex = new Regex(@"(?<type>[A-Za-z]+)(\((?<length>\d+)\))?", RegexOptions.ExplicitCapture | RegexOptions.Compiled);

			public enum ColumnType
			{
				Text,
				Binary,
				Numeric,
			}

			public ColumnType Type;
			public int FieldLength;
			public string RawType;

			public DbColumnType(string typeString)
			{
				RawType = typeString;

				Match m = _regex.Match(typeString);
				if (!m.Success)
				{
					throw new ArgumentException();
				}

				string typeGroup = m.Groups["type"].Value;
				if (typeGroup.Contains("binary"))
				{
					Type = ColumnType.Binary;
				}
				else if (typeGroup.Contains("int") || typeGroup.Contains("decimal") || typeGroup.Contains("double") || typeGroup.Contains("float"))
				{
					Type = ColumnType.Numeric;
				}
				else
				{
					Type = ColumnType.Text;
				}

				FieldLength = m.Groups["length"].Success ? int.Parse(m.Groups["length"].Value, CultureInfo.InvariantCulture) : 0;
			}

			public DbColumnType(ColumnType type, int fieldLength)
			{
				RawType = string.Empty;
				Type = type;
				FieldLength = fieldLength;
			}
		}

		public class TableSchema : IEnumerable<TableColumnName>
		{
			private readonly string _tableName;
			private readonly string _dbPrimaryKeyName;
			private readonly TableColumnName[] _tableColumnNames;
			private readonly int _uniqueColumnIndex;
			private readonly int[] _internalElementOffsets;

			public TableSchema(string tablename, string dbPrimaryKeyName, TableColumnName[] tableColumnNames, int uniqueColumnIndex)
			{
				_tableName = tablename;
				_dbPrimaryKeyName = dbPrimaryKeyName;
				_tableColumnNames = tableColumnNames;
				_uniqueColumnIndex = uniqueColumnIndex;
				_internalElementOffsets = new int[_tableColumnNames.Length];
				int lastOffset = 0;
				for (int i = 0; i < _tableColumnNames.Length; ++i)
				{
					lastOffset = ((ITableColumnNameCollectable)_tableColumnNames[i]).Elements.Length + lastOffset;
					_internalElementOffsets[i] = lastOffset - 1;
				}
			}

			public string TableName
			{
				get { return _tableName; }
			}

			public string DbPrimaryKeyName
			{
				get { return _dbPrimaryKeyName; }
			}

			public TableColumnName UniqueColumnName
			{
				get { return _tableColumnNames[_uniqueColumnIndex]; }
			}

			public int Length
			{
				get { return _tableColumnNames.Length; }
			}

			public int ElementLength
			{
				get { return _internalElementOffsets[_internalElementOffsets.Length - 1] + 1; }
			}

			public TableColumnName this[int index]
			{
				get { return _tableColumnNames[index]; }
			}

			public TableColumnName GetByElementIndex(int elementIndex, out DbColumnType elementType)
			{
				if (elementIndex < 0)
				{
					throw new IndexOutOfRangeException("The elementIndex is less then 0");
				}
				int index = Array.BinarySearch(_internalElementOffsets, elementIndex);
				if (index < 0)
				{
					index = ~index;
					if (index == _internalElementOffsets.Length)
					{
						throw new IndexOutOfRangeException("The elementIndex is greater then the index of the last element");
					}
				}
				int relativeIndex = elementIndex;
				if (index > 0)
				{
					relativeIndex = _internalElementOffsets[index] - relativeIndex;
				}
				elementType = ((ITableColumnNameCollectable)_tableColumnNames[index]).Types[relativeIndex];
				return _tableColumnNames[index];
			}

			public TableColumnName FindByElementName(string name)
			{
				foreach (ITableColumnNameCollectable tableColumnName in _tableColumnNames)
				{
					if (tableColumnName.Elements.Any(t => t == name.ToLowerInvariant()))
					{
						return tableColumnName as TableColumnName;
					}
				}
				return null;
			}

			public TableColumnName FindByDbColumnName(string name)
			{
				foreach (ITableColumnNameCollectable tableColumnName in _tableColumnNames)
				{
					for (int i = 0; i < tableColumnName.Aliases.Length; ++i)
					{
						if (tableColumnName.Elements[i] == name.ToLowerInvariant() || tableColumnName.Aliases[i] != null && tableColumnName.Aliases[i].DbColumnName.Equals(name, StringComparison.InvariantCultureIgnoreCase))
						{
							return tableColumnName as TableColumnName;
						}
					}
				}
				return null;
			}

			public string ToValuesSelectSql(TableColumnName orderByColumnName)
			{
				StringBuilder selectBuilder = new StringBuilder();
				StringBuilder fromBuilder = new StringBuilder();
				StringBuilder orderByBuilder = null;
				fromBuilder.AppendFormat("{0}", _tableName);
				for (int tableColumnIndex = 0; tableColumnIndex < _tableColumnNames.Length; ++tableColumnIndex)
				{
					ITableColumnNameCollectable tableColumnName = _tableColumnNames[tableColumnIndex];
					bool orderByColumn = false;
					if (tableColumnName.Equals(orderByColumnName))
					{
						if (orderByBuilder != null)
						{
							throw new ApplicationException("schema has duplicate column names");
						}
						orderByBuilder = new StringBuilder();
						orderByBuilder.Append("ORDER BY ");
						orderByColumn = true;
					}
					for (int i = 0; i < tableColumnName.Elements.Length; ++i)
					{
						string tableAlias = _tableName, column = tableColumnName.Elements[i];
						if (tableColumnName.Aliases[i] != null)
						{
							column = tableColumnName.Aliases[i].DbColumnEqual;
							tableAlias = "alias" + (tableColumnIndex == 0 ? i : _internalElementOffsets[tableColumnIndex] + i);
							fromBuilder.AppendFormat(" LEFT JOIN {0} as {1} ON ({1}.{2} = {3}.{4})", tableColumnName.Aliases[i].DbTableEqual, tableAlias, tableColumnName.Aliases[i].DbColumnName2, _tableName, tableColumnName.Aliases[i].DbColumnName);
						}
						string tableDotColumnComma = string.Format("{0}.{1},", tableAlias, column);
						selectBuilder.Append(tableDotColumnComma);
						if (orderByColumn)
						{
							orderByBuilder.Append(tableDotColumnComma);
						}
					}
				}
				--selectBuilder.Length;
				if (orderByBuilder == null)
				{
					throw new ArgumentException("orderByColumnName must be in this schema");
				}
				--orderByBuilder.Length;
				return string.Format("SELECT {0} FROM {1} {2};", selectBuilder, fromBuilder, orderByBuilder);
			}

			public IEnumerator<TableColumnName> GetEnumerator()
			{
				return ((IEnumerable<TableColumnName>)_tableColumnNames).GetEnumerator();
			}

			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}
		}
		#endregion TableSchema
		#endregion TableAbstraction
	}
}
