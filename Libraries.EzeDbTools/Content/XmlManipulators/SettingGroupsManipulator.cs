using System;
using System.Text;
using System.Xml;
using System.Xml.XPath;

namespace Libraries.EzeDbCommon.Content.XmlManipulators
{
    class SettingGroupsManipulator : DBXmlManipulator
    {
        #region Variables
        private SettingGroupSettingsManipulator _settingGroupSettings;
        #endregion

        #region Lifetime
        /// <param name="dbAccess">Used only to submit queries during instatiation.</param>
        /// <param name="xmlPath">The xml file that holds the content for that table.</param>
        public SettingGroupsManipulator(DataReader dbAccess, string xmlPath)
            : base(GetContext(dbAccess, xmlPath))
        {
            GetSettingGroupSettings();
        }

        private static DbXmlManipulatorContext GetContext(DataReader dataReader, string xmlPath)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlPath);
            DbXmlManipulatorContext context = new DbXmlManipulatorContext(dataReader, "settinggroups", xmlDoc);
            context.UniqueColumnNames.Add("name");
            context.XmlElementsToIgnore.Add("settinggroupsettings");
            return context;
        }

        #endregion

        #region Properties

        public SettingGroupSettingsManipulator SettingGroupSettings
        {
            get { return _settingGroupSettings; }
        }

        #endregion Properties

        #region methods

        public string SettingGroupSettingsInsertSql()
        {
            StringBuilder sqlStmt = new StringBuilder();

            sqlStmt.Append(_settingGroupSettings.InsertSql());

            return sqlStmt.ToString();
        }

        public string SettingGroupSettingsUpdateSql()
        {
            StringBuilder sqlStmt = new StringBuilder();

            sqlStmt.Append(_settingGroupSettings.UpdateSql());

            return sqlStmt.ToString();
        }

        public string SettingGroupSettingsDeleteSql()
        {
            StringBuilder sqlStmt = new StringBuilder();

            sqlStmt.Append(_settingGroupSettings.DeleteSql());

            return sqlStmt.ToString();
        }

        #endregion

        #region Private Implementation

        private void GetSettingGroupSettings()
        {
            XPathNodeIterator settingGroupIterator = _context.NavigableXml.CreateNavigator().Select(_context.RowExpression);
            XPathExpression settingGroupSettingsExpression = XPathExpression.Compile("settinggroupsettings/row");

            while (settingGroupIterator.MoveNext())
            {
                string group = settingGroupIterator.Current.GetAttribute("name", string.Empty);
                if (group == string.Empty)
                {
                    throw new ApplicationException("No attribute 'name' in settinggroups row.");
                }

                XPathNodeIterator settingGroupSettingsIterator = settingGroupIterator.Current.Clone().Select(settingGroupSettingsExpression);
                while (settingGroupSettingsIterator.MoveNext())
                {
                    settingGroupSettingsIterator.Current.CreateAttribute(string.Empty, "group", string.Empty, group);
                }
            }

            _settingGroupSettings = new SettingGroupSettingsManipulator(_context.DataReader, _context.NavigableXml, XPathExpression.Compile("//settinggroupsettings/row"));
        }

        #endregion
    }
}
 