using System;
using System.Collections.Generic;
using System.Xml.XPath;

namespace Libraries.EzeDbCommon.Content
{
    public class DbXmlManipulatorContext
    {
        public delegate void RowParsed(ref IDictionary<DBXmlManipulator.TableColumnName, DBXmlManipulator.TableValue> row);

        private readonly DataReader _dataReader;
        private readonly string _dbTableName;
        private readonly IXPathNavigable _navigableXml;
        private readonly List<string> _uniqueColumnNames;
        private readonly List<string> _xmlElementsToIgnore;
        private XPathExpression _rowExpression;
        private readonly Dictionary<string, XmlColumnAlias> _aliases;
        private RowParsed _dbRowParsed;
        private RowParsed _xmlRowParsed;

        public DbXmlManipulatorContext(DataReader dataReader, string tableName, IXPathNavigable navigableXml)
        {
            if (dataReader == null || tableName == null || navigableXml == null)
            {
                throw new ArgumentNullException();
            }
            _dataReader = dataReader;
            _dbTableName = tableName;
            _navigableXml = navigableXml;
            _uniqueColumnNames = new List<string>();
            _xmlElementsToIgnore = new List<string>();
            _aliases = new Dictionary<string, XmlColumnAlias>();
        }

        public DataReader DataReader
        {
            get { return _dataReader; }
        }

        public string DbTableName
        {
            get { return _dbTableName; }
        }

        public IXPathNavigable NavigableXml
        {
            get { return _navigableXml; }
        }

        public ICollection<string> UniqueColumnNames
        {
            get { return _uniqueColumnNames; }
        }

        public ICollection<string> XmlElementsToIgnore
        {
            get { return _xmlElementsToIgnore; }
        }

        public XPathExpression RowExpression
        {
            get
            {
                if (_rowExpression == null)
                {
                    _rowExpression = XPathExpression.Compile(string.Format("/{0}/row", _dbTableName));
                }
                return _rowExpression;
            }
            set { _rowExpression = value; }
        }

        public IDictionary<string, XmlColumnAlias> AliasesByName
        {
            get { return _aliases; }
        }

        public RowParsed DbRowParsed
        {
            get { return _dbRowParsed; }
            set { _dbRowParsed = value; }
        }

        public RowParsed XmlRowParsed
        {
            get { return _xmlRowParsed; }
            set { _xmlRowParsed = value; }
        }

        public object State { get; set; }
    }
}