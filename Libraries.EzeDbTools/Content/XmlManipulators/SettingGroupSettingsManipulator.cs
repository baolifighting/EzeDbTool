using System.Xml.XPath;

namespace Libraries.EzeDbCommon.Content.XmlManipulators
{
    class SettingGroupSettingsManipulator : DBXmlManipulator
    {
        #region Lifetime
        /// <param name="dbAccess">Used only to submit queries during instatiation.</param>
        /// <param name="navigableXml">The xml that holds the content for that table.</param>
        /// <param name="expr">The XPathExpression that returns the nodeset containing settinggroupsetting rows</param>
        public SettingGroupSettingsManipulator(DataReader dbAccess, IXPathNavigable navigableXml, XPathExpression expr)
            : base(GetContext(dbAccess, navigableXml, expr))
        {
        }

        private static DbXmlManipulatorContext GetContext(DataReader dataReader, IXPathNavigable navigableXml, XPathExpression expr)
        {
            DbXmlManipulatorContext context = new DbXmlManipulatorContext(dataReader, "settinggroupsettings", navigableXml);
            context.RowExpression = expr;
            context.UniqueColumnNames.Add("group");
            context.UniqueColumnNames.Add("name");
            context.AliasesByName["group"] = new XmlColumnAlias("settingGroupId", "name", "settinggroups", "settingGroupId");
            context.AliasesByName["name"] = new XmlColumnAlias("settingId", "name", "settings", "settingId");
            return context;
        }

        #endregion
    }
}
