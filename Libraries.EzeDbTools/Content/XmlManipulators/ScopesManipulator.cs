using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.XPath;

namespace Libraries.EzeDbCommon.Content.XmlManipulators
{
    public sealed class ScopesManipulator : DBXmlManipulator
    {
        #region Variables

        private readonly List<ScopeInformation> _scopeInfos;
        private readonly List<TableValue> _scopesToIgnore;
        private SettingsManipulator _settings;
        private ComponentsManipulator _components;
        
        #endregion

        #region Lifetime
        /// <param name="dbAccess">Used only to submit queries during instantiation.</param>
        /// <param name="xmlPath">The xml file that holds the content for that table.</param>
        public ScopesManipulator(DataReader dbAccess, string xmlPath, Tuple<IXPathNavigable, XPathExpression> settings)
            :
            base(GetContext(dbAccess, xmlPath))
        {
            _scopeInfos = new List<ScopeInformation>();
            _scopesToIgnore = (List<TableValue>)_context.State;
            CreateSubManipulators(settings);
        }

        private static DbXmlManipulatorContext GetContext(DataReader dataReader, string xmlPath)
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlPath);
            var context = new DbXmlManipulatorContext(dataReader, "scopes", xmlDoc);
            context.UniqueColumnNames.Add("name");
            context.XmlElementsToIgnore.Add("key");
            context.XmlElementsToIgnore.Add("references");
            context.XmlElementsToIgnore.Add("where");
            context.XmlElementsToIgnore.Add("channeldriverkey");

            var type = new TableColumnName("type");
            var userDefined = new TableValue("2"); // type 2 is custom, see ThreeVR.DbTransport.Objects.Types.ScopeType
            var scopeNamesToIgnore = new List<TableValue>();
            var nameColumn = new TableColumnName("name");
            context.State = scopeNamesToIgnore;
            context.DbRowParsed = delegate(ref IDictionary<TableColumnName, TableValue> row)
            {
                if (row[type].Equals(userDefined))
                {
                    // if this user defined we skip it, we don't want these values to be deleted out of the database.
                    //  maybe we should also be checking for type 3 here?
                    scopeNamesToIgnore.Add(row[nameColumn]);
                    row = null;
                }
            };

            return context;
        }

        #endregion

        #region Properties

        public ScopeInformation[] Info
        {
            get { return _scopeInfos.ToArray(); }
        }

        public SettingsManipulator Settings
        {
            get { return _settings; }
        }

        #endregion Properties

        #region Setting Values

        public string ComponentInsertSql()
        {
            StringBuilder sqlStmt = new StringBuilder();

            sqlStmt.Append(_components.InsertSql());
            return sqlStmt.ToString();
        }

        public string ComponentDeleteSql()
        {
            StringBuilder sqlStmt = new StringBuilder();

            sqlStmt.Append(_components.DeleteSql());
            
            return sqlStmt.ToString();
        }

        #endregion Setting Values

        #region Private Implementation

        private void CreateSubManipulators(Tuple<IXPathNavigable, XPathExpression> settingsXml)
        {
            XPathNavigator nav = _context.NavigableXml.CreateNavigator();
            XPathNodeIterator scopeIterator = nav.Select(_context.RowExpression);
            var componentXml = new XmlDocument();
            componentXml.AppendChild(componentXml.CreateElement("components"));

            var settingLookup = settingsXml.Item1.CreateNavigator().Select(settingsXml.Item2).Cast<XPathNavigator>().ToLookup(settingNav => settingNav.GetAttribute("scope", string.Empty), settingNav => settingNav.GetAttribute("name", string.Empty));

            while (scopeIterator.MoveNext())
            {
                string name = scopeIterator.Current.GetAttribute("name", string.Empty);
                if (name == string.Empty)
                {
                    throw new ApplicationException("No attribute 'name' in scopes row.");
                }

                //get components
                Reference scopeComponentReference = new Reference();
                scopeComponentReference.FromXmlContext(scopeIterator.Current);
                List<ComponentInfo> components = GetScopeComponentNumbersAndAddToComponentXml(componentXml, name, scopeComponentReference);

                //get settings
                var settings = settingLookup[name].ToArray();

                //create scope info
                var scopeInfo = new ScopeInformation(name, settings.Length == 0 ? null : settings, components.ToArray());
                _scopeInfos.Add(scopeInfo);
            }


            _components = new ComponentsManipulator(_context.DataReader, componentXml, _scopesToIgnore);
            _settings = new SettingsManipulator(_context.DataReader, settingsXml.Item1, settingsXml.Item2, _scopesToIgnore);
        }


        private List<ComponentInfo> GetScopeComponentNumbersAndAddToComponentXml(XmlDocument componentXml, string scope, Reference scopeComponentReference)
        {
            XmlAttribute scopeAttrib = componentXml.CreateAttribute("scope");
            XmlAttribute componentAttrib = componentXml.CreateAttribute("componentNumber");
            List<int> componentNumbers = new List<int>();
            List<int?> channelDrivers = new List<int?>();
            scopeAttrib.Value = scope;
            int componentNumber = 1;
            int? channelDriverId = -1;
            if (scopeComponentReference.References != null && scopeComponentReference.Key != null)
            {
                StringBuilder sqlStmt = new StringBuilder();
                sqlStmt.AppendFormat("SELECT {0}", scopeComponentReference.Key);
                if (scopeComponentReference.ChannelDriverKey != null)
                {
                    sqlStmt.AppendFormat(", {0}", scopeComponentReference.ChannelDriverKey);
                }
                sqlStmt.AppendFormat(" FROM {0}", scopeComponentReference.References);
                if (scopeComponentReference.Where != null)
                {
                    sqlStmt.AppendFormat(" WHERE {0}", scopeComponentReference.Where);
                }
                sqlStmt.Append(";");
                using (IDataReader reader = _context.DataReader.ExecuteQuery(sqlStmt.ToString()))
                {
                    while (reader.Read())
                    {
                        XmlElement rowElement = componentXml.CreateElement("row");
                        rowElement.SetAttributeNode((XmlAttribute) scopeAttrib.Clone());
                        componentNumber = int.Parse(reader.GetValue(0).ToString());
                        if (scopeComponentReference.ChannelDriverKey != null)
                        {
                            if (reader.IsDBNull(1))
                            {
                                channelDriverId = null;
                            }
                            else
                            {
                                channelDriverId = int.Parse(reader.GetValue(1).ToString());
                            }
                        }
                        else
                        {
                            channelDriverId = -1;
                        }
                        componentAttrib.Value = componentNumber.ToString();
                        rowElement.SetAttributeNode((XmlAttribute) componentAttrib.Clone());
                        componentXml.DocumentElement.AppendChild(rowElement);
                        componentNumbers.Add(componentNumber);
                        channelDrivers.Add(channelDriverId);
                    }
                }
            }
            else
            {
                XmlElement rowElement = componentXml.CreateElement("row");
                rowElement.SetAttributeNode((XmlAttribute) scopeAttrib.Clone());
                componentAttrib.Value = componentNumber.ToString();
                rowElement.SetAttributeNode((XmlAttribute) componentAttrib.Clone());
                componentXml.DocumentElement.AppendChild(rowElement);
                componentNumbers.Add(componentNumber);
                channelDrivers.Add(channelDriverId);
            }
            List<ComponentInfo> components = new List<ComponentInfo>();
            for (int i = 0; i < componentNumbers.Count; ++i)
            {
                ComponentInfo component = new ComponentInfo(componentNumbers[i]);
                if (channelDrivers[i] != -1)
                {
                    if (!channelDrivers[i].HasValue)
                    {
                        component.AddChannelDriver(null);
                    }
                    else
                    {
                        component.AddChannelDriver(_context.DataReader.ExecuteScalar("SELECT name FROM channeldrivers WHERE channeldriverid=" + channelDrivers[i].Value).ToString());
                    }
                }
                components.Add(component);
            }
            return components;
        }

        #endregion

        #region Reference
        private class Reference
        {
            #region Variables
            public string References;
            public string Key;
            public string ChannelDriverKey;
            public string Where;
            #endregion

            #region Lifetime
            public Reference()
            {
                References = null;
                Key = null;
                ChannelDriverKey = null;
                Where = null;
            }
            #endregion

            #region Methods
            public void FromXmlContext(XPathNavigator nav)
            {
                if (nav.MoveToFirstAttribute())
                {
                    do
                    {
                        if (!nav.Value.Contains(";"))
                        {
                            switch (nav.Name)
                            {
                                case "references":
                                    References = nav.Value;
                                    break;
                                case "key":
                                    Key = nav.Value;
                                    break;
                                case "where":
                                    Where = nav.Value;
                                    break;
                                case "channeldriverkey":
                                    ChannelDriverKey = nav.Value;
                                    break;
                                default:
                                    break;
                            }
                        }
                    } while (nav.MoveToNextAttribute());

                    nav.MoveToParent();
                }
            }

            #endregion Methods
        }
        #endregion

        public class ComponentsManipulator : DBXmlManipulator
        {
            #region Lifetime

            public ComponentsManipulator(DataReader dataReader, IXPathNavigable navigableXml, IEnumerable<TableValue> scopesToIgnore)
                : base(GetContext(dataReader, navigableXml, scopesToIgnore))
            {
            }

            private static DbXmlManipulatorContext GetContext(DataReader dataReader, IXPathNavigable navigableXml, IEnumerable<TableValue> scopesToIgnore)
            {
                var context = new DbXmlManipulatorContext(dataReader, "components", navigableXml);
                context.UniqueColumnNames.Add("scope");
                context.UniqueColumnNames.Add("componentNumber");
                context.AliasesByName["scope"] = new XmlColumnAlias("scopeid", "name", "scopes", "scopeid");
                var uniqueColumn = new TableColumnName("scope", "componentNumber");
                context.DbRowParsed = delegate(ref IDictionary<TableColumnName, TableValue> row)
                {
                    var uniqueColumnValue = row[uniqueColumn];
                    string scope = ((string[])uniqueColumnValue)[0];
                    if (scopesToIgnore.Select(toIgnore => ((string[])toIgnore)[0]).FirstOrDefault(toIgnore => toIgnore == scope) != null)
                    {
                        row = null;
                    }
                };

                return context;
            }
            #endregion Lifetime
        }

        public class ScopeInformation
        {
            #region Variables

            public readonly string Scope;
            public readonly string[] Settings;
            public readonly ComponentInfo[] Components;

            #endregion Variables

            #region Lifetime

            public ScopeInformation(string scope, string[] settings, ComponentInfo[] components)
            {
                Scope = scope;
                Settings = settings;
                Components = components;
            }

            #endregion Lifetime

            #region Methods

            public override string ToString()
            {
                return Scope;
            }

            #endregion Methods
        }

        public class ComponentInfo
        {
            #region Variables

            public readonly int ComponentNumber;
            private bool _hasChannelDriver;
            private string _channelDriver;

            #endregion Variables

            #region Lifetime

            public ComponentInfo(int componentNumber)
            {
                ComponentNumber = componentNumber;
                _hasChannelDriver = false;
            }

            #endregion Lifetime

            #region Properties

            public string ChannelDriver
            {
                get { return _channelDriver; }
            }

            public bool HasChannelDriver
            {
                get { return _hasChannelDriver; }
            }

            #endregion Properties

            #region Methods

            public void AddChannelDriver(string channelDriver)
            {
                _channelDriver = channelDriver;
                _hasChannelDriver = true;
            }

            #endregion Methods
        }
    }
}
