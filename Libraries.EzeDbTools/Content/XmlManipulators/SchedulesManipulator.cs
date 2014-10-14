using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Xml.XPath;

namespace Libraries.EzeDbCommon.Content.XmlManipulators
{
    class SchedulesManipulator : DBXmlManipulator
    {
        private readonly List<ChangedRow> _valueSetsToAdd;

        #region Lifetime
        /// <param name="dbAccess">Used only to submit queries during instatiation.</param>
        /// <param name="xmlPath">The xml file that holds the content for tha ttable.</param>
        public SchedulesManipulator(DataReader dbAccess, string xmlPath)
            : base(GetContext(dbAccess, xmlPath))
        {
            _valueSetsToAdd = new List<ChangedRow>();
            var valuesets = (List<TableValue>)_context.State;
            var nameColumnName = new TableColumnName("name");
            foreach (TableValue valueset in valuesets)
            {
                var count = Convert.ToInt32(dbAccess.ExecuteScalar(string.Format("SELECT count(*) FROM valuesets WHERE name={0};", valueset.ElementAsSQLValue(0))));
                if (count == 0)
                {
                    _valueSetsToAdd.Add(new ChangedRow(nameColumnName, valueset, true, false, null, new Dictionary<TableColumnName, TableValue> { { nameColumnName, valueset } }));
                }
            }
        }

        private static DbXmlManipulatorContext GetContext(DataReader dataReader, string xmlPath)
        {
            DbXmlManipulatorContext context = new DbXmlManipulatorContext(dataReader, "schedules", new XPathDocument(xmlPath));
            context.UniqueColumnNames.Add("name");

            context.AliasesByName["settingGroup"] = new XmlColumnAlias("settinggroupId", "name", "settinggroups", "settinggroupId");
            context.AliasesByName["valueset"] = new XmlColumnAlias("valuesetid", "name", "valuesets", "valuesetid");

            var valuesets = new List<TableValue>();
            var valueSetColumnName = new TableColumnName("valueset");
            context.XmlRowParsed = delegate(ref IDictionary<TableColumnName, TableValue> row)
                {
                    valuesets.Add(row[valueSetColumnName]);
                };

            context.State = valuesets;
            return context;
        }

        #endregion

        #region Methods

        public override string InsertSql()
        {
            var sb = new StringBuilder();

            foreach (ChangedRow changedRow in _valueSetsToAdd)
            {
                sb.AppendLine(changedRow.GenerateSQL("valuesets"));
            }

            sb.AppendLine(base.InsertSql());

            //Create a default schedule time entry for every new schedule
            var column = new TableColumnName("name");
            foreach (ChangedRow insertRow in _changedRows)
            {
                if (insertRow.Type == ChangedRow.ChangeType.ADD)
                {
                    string selectScheduleId = string.Format("SELECT scheduleId FROM schedules WHERE {0}", column.ToWhereEqualString("schedules", insertRow.UniqueIdentifier));
                    sb.AppendFormat("INSERT INTO scheduletimes(scheduleid, starttime, endtime) VALUES (({0}), '{1}', '{2}');", selectScheduleId, "00:00:00", "7.00:00:00").AppendLine();
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Create default schedule times for schedules that do not have any schedule times.
        /// A default schedule time spans the entire week.
        /// </summary>
        public string CreateDefaultScheduleTimesIfNeeded(DataReader dbAccess)
        {
            string sqlStmt = "";

            List<long> scheduleIds = new List<long>();
            using (IDataReader reader = dbAccess.ExecuteQuery("SELECT scheduleId FROM schedules"))
            {
                while (reader.Read())
                {
                    scheduleIds.Add(reader.GetInt64(0));
                }
            }

            foreach (long scheduleId in scheduleIds) 
            {
                long scheduleTimesCount = long.Parse(dbAccess.ExecuteScalar("SELECT COUNT(*) FROM scheduletimes WHERE scheduleid = " + scheduleId).ToString());

                if (scheduleTimesCount == 0)
                {
                    sqlStmt += string.Format("INSERT INTO scheduletimes(scheduleid, starttime, endtime) VALUES ('{0}', '{1}', '{2}');",
                                                                        scheduleId, "00:00:00", "7.00:00:00");
                }
                
            }

            return sqlStmt;
        }

        #endregion 
    }
    
}
