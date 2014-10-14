using System.Xml.XPath;
using Libraries.EzeDbCommon;

namespace Libraries.EzeDbCommon.Content.XmlManipulators
{
    class HealthAlertTypesManipulator : DBXmlManipulator
    {
        public HealthAlertTypesManipulator(DataReader dbAccess, string xmlPath)
            : base(GetContext(dbAccess, xmlPath))
        {
        }

        private static DbXmlManipulatorContext GetContext(DataReader dataReader, string xmlPath)
        {
            DbXmlManipulatorContext context = new DbXmlManipulatorContext(dataReader, "healthalerttypes", new XPathDocument(xmlPath));
            context.UniqueColumnNames.Add("name");
            return context;
        }
    }
}
