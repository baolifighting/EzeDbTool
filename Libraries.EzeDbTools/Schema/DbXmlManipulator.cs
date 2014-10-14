using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.XPath;
using Libraries.EzeDbCommon;
using Libraries.EzeDbTools.Schema;

namespace Libraries.EzeDbTools
{
    public class DbXmlManipulator
    {
        #region Variables

        private int _schema;
        private readonly Dictionary<ModTransition, Mod> _modDB = new Dictionary<ModTransition, Mod>();
        private readonly IDictionary<string, string> _parameters;
        private static bool _updatingDb;
        private readonly string _patchfilePath;
        private WeightedGraph _versionGraph;

        #endregion Variables

        #region Events

        private EventHandler _startingApplyModEvent;
        private EventHandler _finishedApplyModEvent;
        private EventHandler _startingApplyStepEvent;
        private EventHandler _finishedApplyStepEvent;
        private EventHandler _schemaChanged;

        public event EventHandler StartingApplyMod
        {
            add
            {
                _startingApplyModEvent += value;
            }
            remove
            {
                _startingApplyModEvent -= value;
            }
        }

        public event EventHandler FinishedApplyMod
        {
            add
            {
                _finishedApplyModEvent += value;
            }
            remove
            {
                _finishedApplyModEvent -= value;
            }
        }

        public event EventHandler StartingApplyStep
        {
            add
            {
                _startingApplyStepEvent += value;
            }
            remove
            {
                _startingApplyStepEvent -= value;
            }
        }

        public event EventHandler FinishedApplyStep
        {
            add
            {
                _finishedApplyStepEvent += value;
            }
            remove
            {
                _finishedApplyStepEvent -= value;
            }
        }

        public event EventHandler SchemaChanged
        {
            add
            {
                _schemaChanged += value;
            }
            remove
            {
                _schemaChanged -= value;
            }
        }

        #endregion Events

        #region Lifetime

        /// <summary>
        /// This class helps create/mod a db from steps in the dbmods.xml file.
        /// </summary>
        /// <param name="parameters">Key - value pairs to use for parameter replacement in the dbmods.xml file.</param></param>
        /// <param name="patchFile">The path to the dbmods.xml file.</param>
        /// <remarks>if the patchFile parameter ends in .xml, that file will be used instead of dbmods.xml.</remarks>
        public DbXmlManipulator(IDictionary<string, string> parameters, string patchFile, int schema)
        {
            _schema = schema;

            if (parameters == null)
            {
                _parameters = new Dictionary<string, string>();
            }
            else
            {
                _parameters = parameters;
            }

            _patchfilePath = SupportFileFactory.Instance.FullPath(patchFile);

            Mod[] mods = GetMods(_patchfilePath);

            if (mods == null)
            {
                throw new ApplicationException("ERROR: No mods found in " + _patchfilePath);
            }

            foreach (Mod mod in mods)
            {
                _modDB.Add(new ModTransition(mod), mod);
            }

            _versionGraph = BuildVersionGraph(_schema);
        }

        #endregion Lifetime

        #region Properties

        public int Schema
        {
            get { return _schema; }
        }

        public IDictionary<string, string> Parameters
        {
            get
            {
                return _parameters;
            }
        }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Outputs a big hunk of text containing all the mods.
        /// </summary>
        /// <returns>all the mods in string form.</returns>
        public string ListMods()
        {
            string modStrings = "<" + Constants.XmlModsElement + ">" + Environment.NewLine;

            foreach (Mod mod in _modDB.Values)
            {
                modStrings += mod + Environment.NewLine;
            }
            modStrings += "</" + Constants.XmlModsElement + ">" + Environment.NewLine;

            return modStrings;
        }

        // it's possible the number returned by these won't be accurate if the schema is 0
        public int GetNumMods(string currentVersion)
        {
            return GetNumMods(currentVersion, GetLatestDbVersion());
        }

        public int GetNumMods(string currentVersion, string requestedVersion)
        {
            return _versionGraph.ShortestDistance(currentVersion, requestedVersion);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="engine">The engine to use for executing all the steps.</param>
        /// <param name="currentVersion">The current version of the db.</param>
        /// <returns>The new database version after all mods have been applied.</returns>
        /// <remarks>The current db version is usually stored in the versions table, and can be extracted easily like so:
        /// SELECT version FROM versions ORDER BY versionid DESC LIMIT 1</remarks>
        public string UpdateDb(IExecutionEngine engine, string currentVersion)
        {
            return UpdateDb(engine, currentVersion, null);
        }

        public string UpdateDb(IExecutionEngine engine, string currentVersion, string requestedVersion)
        {
            lock (this)
            {
                if (_updatingDb)
                {
                    throw new ApplicationException("Another db update is already in progress.");
                }
                _updatingDb = true;
            }

            string updatedVersion;
            try
            {
                if (requestedVersion == null)
                {
                    requestedVersion = GetLatestDbVersion();
                }

                updatedVersion = UpdateToVersion(engine, currentVersion, requestedVersion);
            }
            finally
            {
				lock (this)
				{
					_updatingDb = false;
				}
            }

            return updatedVersion;
        }

        #endregion Methods

        #region Private Implementation

        /// <summary>
        /// This method receive as a parameter the actual DB version and try to update the DB to the latest
        /// version of the Mods known.
        /// If there is any error during the process, the method will throw an Exception.
        /// </summary>
        /// <param name="engine"></param>
        /// <param name="currentVersion">the current version of the DB</param>
        /// <param name="requestedVersion"></param>
        /// <returns>the value of the DB at the end of the method (ie: with Mods all applied)</returns>
        private String UpdateToVersion(IExecutionEngine engine, string currentVersion, string requestedVersion)
        {
            int newSchema = _schema;
            WeightedGraph versionGraph;
            IList modList = BuildModList(ref newSchema, currentVersion, requestedVersion, out versionGraph);

            if (newSchema != _schema)
            {
                _schema = newSchema;
                _versionGraph = versionGraph;
                FireSchemaChanged(this, EventArgs.Empty);
            }

            for (int i = 1; i < modList.Count; ++i)
            {
                string modTo = (string)modList[i];
                var mod = _modDB[new ModTransition
                {
                    ModFrom = currentVersion,
                    ModTo = modTo,
                    Schema = newSchema
                }];
                mod.Schema = newSchema;
                bool failed = !ApplyMod(engine, mod);
                if (failed)
                {
                    break;
                }

                currentVersion = modTo;
                SetDBVersion(engine, modTo);
            }

            return currentVersion;
        }

        /// <summary>
        /// Read all Mods from an XML DB Mod file.
        /// The method uses XPath expressio to first select all the Mods in the DB file vefore parsing
        /// them one after the other.
        /// </summary>
        /// <param name="xmlDBFileLocation">the location of an XML DB Mod file</param>
        /// <returns>an array of Mods read from the XML file passed as an argument</returns>
        private Mod[] GetMods(string xmlDBFileLocation)
        {
            Mod[] mods = null;
            XmlTextReader textReader;

            try
            {
                textReader = new XmlTextReader(xmlDBFileLocation);
            }
            catch
            {
                Console.WriteLine("Xml file read failure!  Did you remember to specify a file?");
                return null;
            }

            try
            {
                XPathDocument xPathDocument = new XPathDocument(textReader);
                XPathNavigator xPathNavigator = xPathDocument.CreateNavigator();
                xPathNavigator.MoveToRoot();
                XPathExpression xPathExpression = xPathNavigator.Compile(Constants.DbToolsSelectModXPathExpression);

                XPathNodeIterator xPathNodeIterator = xPathNavigator.Select(xPathExpression);

                if (xPathNodeIterator.Count != 0)
                {
                    mods = new Mod[xPathNodeIterator.Count];

                    int index = 0;
                    while (xPathNodeIterator.MoveNext())
                    {
                        mods[index++] = ParseMod(xPathNodeIterator.Current);
                    }
                }
            }
            catch (XmlSchemaException ex)
            {
                Console.WriteLine("Error reading " + xmlDBFileLocation);
                Console.WriteLine(ex.Message);
            }
            return mods;
        }

        /// <summary>
        /// Parse the single Mod under the XPathNavigator cursor.
        /// </summary>
        /// <param name="xPathNavigator">an XPathNavigator pointing on the Mod to parse in the XML DB Mod file</param>
        /// <returns>a Mod</returns>
        private Mod ParseMod(XPathNavigator xPathNavigator)
        {
            Mod mod = new Mod
            {
                Author = xPathNavigator.GetAttribute(Constants.XmlModAuthorAttribute, "").Trim(),
                From = xPathNavigator.GetAttribute(Constants.XmlModFromAttribute, "").Trim(),
                FromMinor = xPathNavigator.GetAttribute(Constants.XmlModFromMinorAttribute, "").Trim(),
                To = xPathNavigator.GetAttribute(Constants.XmlModToAttribute, "").Trim(),
                ToMinor = xPathNavigator.GetAttribute(Constants.XmlModToMinorAttribute, "").Trim(),
                Date = xPathNavigator.GetAttribute(Constants.XmlModDateAttribute, "").Trim()
            };

            string schemaString = xPathNavigator.GetAttribute(Constants.XmlModSchemaAttribute, "").Trim();
            if (schemaString != string.Empty)
            {
                mod.Schema = XmlConvert.ToInt32(schemaString);
            }

            mod.IfColumnExists = xPathNavigator.GetAttribute(Constants.XmlStepIfColumnExistsAttribute, "").Trim();
            mod.IfColumnNotExists = xPathNavigator.GetAttribute(Constants.XmlStepIfColumnNotExistsAttribute, "").Trim();
            mod.IfTableNotExists = xPathNavigator.GetAttribute(Constants.XmlStepIfTableNotExistsAttribute, "").Trim();
            mod.IfTableExists = xPathNavigator.GetAttribute(Constants.XmlStepIfTableExistsAttribute, "").Trim();

            if (xPathNavigator.HasChildren)
            {
                xPathNavigator.MoveToFirstChild();

                do
                {
                    switch (xPathNavigator.Name)
                    {
                        case Constants.XmlModCommentElement: mod.Comment = xPathNavigator.Value.Trim(); break;
                        case Constants.XmlModStepsElement: mod.Steps = ParseSteps(xPathNavigator, mod); break;
                    }
                } while (xPathNavigator.MoveToNext());
            }
            return mod;
        }

        /// <summary>
        /// Parse the single Step under the XPathNavigator cursor.
        /// </summary>
        /// <param name="xPathNavigator">an XPathNavigator pointing on the Step to parse in the XML DB Mod file</param>
        /// <param name="mod">the parent mod for this step</param>
        /// <returns>a Step</returns>
        private Step[] ParseSteps(XPathNavigator xPathNavigator, Mod mod)
        {
            if (xPathNavigator.HasChildren)
            {
                Step[] steps = new Step[xPathNavigator.SelectChildren("", "").Count];
                xPathNavigator.MoveToFirstChild();

                int index = 0;
                do
                {
                    Step step = new Step();
                    steps[index++] = step;
                    step.Type = xPathNavigator.GetAttribute(Constants.XmlStepTypeAttribute, "");
                    step.Content = xPathNavigator.Value.Trim();
                    step.Parent = mod;
                } while (xPathNavigator.MoveToNext());

                return steps;
            }
            return null;
        }

        /// <summary>
        /// Replace all _parameters in the content section of Steps that are defined on the command line.
        /// 
        /// Parameters on the command line are defined like:
        /// 
        /// -dPARAMETER_KEY=PARAMETER_VALUE
        /// 
        /// So if a step contains something like that
        /// 
        /// UPDATE some_table SET some_column=%PARAMETER_KEY% WHERE some_column='some_value'
        /// 
        /// it will become:
        /// 
        /// UPDATE some_table SET some_column=PARAMETER_VALUE WHERE some_column='some_value'
        /// 
        /// and all lines in the some_table table where the column some_column='some_value' will have 
        /// the column attribute set to PARAMETER_VALUE
        /// 
        /// </summary>
        /// <param name="content">some content string with _parameters not yet replaced</param>
        /// <returns>the content string with _parameters being replaced</returns>
        private string ReplaceParameters(string content)
        {
            string modifiedContent = content;
            foreach (KeyValuePair<string, string> parameter in _parameters)
            {
                string key = Constants.DbtoolsParameterCharacterPrefix + parameter.Key + Constants.DbToolsParameterCharacterSuffix;
                string val = parameter.Value;
                modifiedContent = modifiedContent.Replace(key, val);
            }

            return modifiedContent;
        }

        /// <summary>
        /// Apply a single Mod by running sequentially all of its Steps.
        /// </summary>
        /// <param name="mod">a Mod to apply</param>
        /// <returns>0 if OK, a non 0 value representing the Step which failed if any error</returns>
        private bool ApplyMod(IExecutionEngine engine, Mod mod)
        {
            bool success = true;

            DbSchemaEventArgs args = new DbSchemaEventArgs
            {
                ModAuthor = mod.Author,
                ModComment = mod.Comment,
                ModDate = mod.Date,
                ModFromVersion = mod.FromConcat,
                ModToVersion = mod.ToConcat
            };

            FireStartingApplyMod(this, args.Clone());

            if (doesModApply(engine, mod))
            {
                for (int ii = 0; ii < mod.Steps.Length; ii++)
                {
                    args.Success = ApplyStep(engine, mod.Steps[ii]);
                    if (!args.Success)
                    {
                        args.StepNumber = ii + 1;
                        args.StepType = mod.Steps[ii].Type;
                        args.StepContent = mod.Steps[ii].Content;
                        success = false;
                        break;
                    }
                }
            }
            else
            {
                args.Success = true;
            }
            FireFinishedApplyMod(this, args.Clone());

            return success;
        }

        private bool doesModApply(IExecutionEngine engine, Mod mod)
        {
            if (mod.Steps == null)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(mod.IfTableExists))
            {
                if (!string.IsNullOrEmpty(mod.IfColumnExists))
                {
                    return engine.DoesTableExist(mod.IfTableExists, mod.IfColumnExists);
                }
                if (!string.IsNullOrEmpty(mod.IfColumnNotExists))
                {
                    return !engine.DoesTableExist(mod.IfTableExists, mod.IfColumnNotExists);
                }
                return engine.DoesTableExist(mod.IfTableExists, string.Empty);
            }
            if (!string.IsNullOrEmpty(mod.IfTableNotExists))
            {
                return !engine.DoesTableExist(mod.IfTableNotExists, string.Empty);
            }
            return true;
        }

        /// <summary>
        /// Apply a single Step.
        /// 
        /// Steps can be of any of those types:
        /// 
        ///  - inline_sql
        ///  - external_sql
        ///  - process
        /// </summary>
        /// <param name="step">s Step to apply</param>
        /// <returns>true if OK, false if any error</returns>
        private bool ApplyStep(IExecutionEngine engine, Step step)
        {
            bool success = true;
            DbSchemaEventArgs args = new DbSchemaEventArgs
            {
                StepType = step.Type,
                StepContent = step.Content
            };

            FireStartingApplyStep(this, args.Clone());

            switch (step.Type)
            {
                case Constants.XmlStepTypeValueInlineSql:
                    {
                        success = engine.ExecuteSql(ReplaceParameters(step.Content));
                        break;
                    }
                case Constants.XmlStepTypeValueExternalSql:
                    {
                        success = engine.ExecuteSql(ReplaceParameters(ReadExternalSqlContentFromFile(step.Content)));
                        break;
                    }
				//case Constants.XmlStepTypeValueInlineCSharp:
				//	{
				//		success = engine.ExecuteCSharp(ReplaceParameters(step.Content));
				//		break;
				//	}
				//case Constants.XmlStepTypeValueExternalCSharp:
				//	{
				//		success = engine.ExecuteCSharp(ReplaceParameters(ReadExternalCSharpContentFromFile(step.Content)));
				//		break;
				//	}
				//case Constants.XmlStepTypeValueInlineVisualBasic:
				//	{
				//		success = engine.ExecuteVisualBasic(ReplaceParameters(step.Content));
				//		break;
				//	}
				//case Constants.XmlStepTypeValueExternalVisualBasic:
				//	{
				//		success = engine.ExecuteVisualBasic(ReplaceParameters(ReadExternalVisualBasicContentFromFile(step.Content)));
				//		break;
				//	}
				//case Constants.XmlStepTypeValueInlineJs:
				//	{
				//		success = engine.ExecuteJs(ReplaceParameters(step.Content));
				//		break;
				//	}
				//case Constants.XmlStepTypeValueExternalJs:
				//	{
				//		success = engine.ExecuteJs(ReplaceParameters(ReadExternalJsContentFromFile(step.Content)));
				//		break;
				//	}
				//case Constants.XmlStepTypeValueProcess:
				//	{
				//		success = engine.ExecuteProcess(ReplaceParameters(step.Content));
				//		break;
				//	}
                case Constants.XmlStepTypeValueMod:
                    {
                        string mods = step.Content;
                        var split = mods.Split(new[] { '-' }, 2);
                        string source, dest = null;
                        if (split.Length > 1)
                        {
                            // we're applying a chain of mods here.
                            source = split[0];
                            dest = split[1];
                        }
                        else
                        {
                            source = mods;
                        }

                        split = source.Split(new[] { ',' }, 2);
                        if (split.Length != 2)
                        {
                            throw new FormatException(string.Format("Couldn't parse content of \"{0}\" step: {1}", Constants.XmlStepTypeValueMod, step.Content));
                        }

                        var fromSource = split[0];
                        var toSource = split[1];

                        if (dest == null)
                        {
                            dest = toSource;
                        }

                        int schema = step.Parent.Schema;
                        var previous = fromSource;
                        WeightedGraph versionGraph;
                        var modList = BuildModList(ref schema, toSource, dest, out versionGraph);
                        try
                        {
                            foreach (string version in modList)
                            {
                                if (!ApplyMod(engine, _modDB[new ModTransition { ModFrom = previous, ModTo = version, Schema = schema }]))
                                {
                                    success = false;
                                    break;
                                }
                                previous = version;
                            }
                        }
                        catch (KeyNotFoundException)
                        {
                            success = false;
                        }

                        break;
                    }
            }

            args.Success = success;
            FireFinishedApplyStep(this, args.Clone());

            return success;
        }

        /// <summary>
        /// Set the version in the DB version table usually called by ApplyMod after having successfully completed
        /// </summary>
        /// <param name="engine"></param>
        /// <param name="versionString">new version to set like "2.24"</param>
        /// <returns>true if OK, false if any error</returns>
        private bool SetDBVersion(IExecutionEngine engine, string versionString)
        {
            DateTime now = DateTime.UtcNow;
            string version, revision;
            SplitDbVersion(versionString, out version, out revision);
            if (revision == null || revision == "0")
            {
                return engine.ExecuteSql(string.Format("INSERT INTO versions(version, applied) VALUES('{0}', '{1}')", version, Config.ToDateTimeStamp(now)));
            }
            return engine.ExecuteSql(string.Format("INSERT INTO versions(version, revision, applied) VALUES('{0}', '{1}', '{2}')", version, revision, Config.ToDateTimeStamp(now)));
        }

        /// <summary>
        /// Helper method to read the content of a file and return it as a string
        /// </summary>
        /// <param name="fileName">the file location of the file to read</param>
        /// <returns>a string containing the content of the file passed as an argument</returns>
        private string ReadContentFromFile(string fileName)
        {
            string fileLocation = SupportFileFactory.Instance.FullPath(fileName);
            StreamReader streamReader = File.OpenText(fileLocation);
            string s = streamReader.ReadToEnd();
            streamReader.Close();
            return s;
        }

        /// <summary>
        /// Helper method to read the content of a cs file and return it as a string.
        /// </summary>
        /// <param name="fileName">the file location of the file to read.</param>
        /// <returns>a string containing the content of the file passed as an argument</returns>
        private string ReadExternalCSharpContentFromFile(string fileName)
        {
            return ReadContentFromFile(Path.Combine(Constants.ExternalCSharpDirectoryName, fileName));
        }

        /// <summary>
        /// Helper method to read the content of a sql file and return it as a string.
        /// </summary>
        /// <param name="fileName">the file location of the file to read.</param>
        /// <returns>a string containing the content of the file passed as an argument</returns>
        private string ReadExternalSqlContentFromFile(string fileName)
        {
            return ReadContentFromFile(Path.Combine(Constants.ExternalSqlDirectoryName, fileName));
        }

        /// <summary>
        /// Helper method to read the content of a visual basic file and return it as a string.
        /// </summary>
        /// <param name="fileName">the file location of the file to read.</param>
        /// <returns>a string containing the content of the file passed as an argument</returns>
        private string ReadExternalVisualBasicContentFromFile(string fileName)
        {
            return ReadContentFromFile(Path.Combine(Constants.ExternalVisualBasicDirectoryName, fileName));
        }

        /// <summary>
        /// Helper method to read the content of a js file and return it as a string.
        /// </summary>
        /// <param name="fileName">the file location of the file to read.</param>
        /// <returns>a string containing the content of the file passed as an argument</returns>
        private string ReadExternalJsContentFromFile(string fileName)
        {
            return ReadContentFromFile(Path.Combine(Constants.ExternalJsDirectoryName, fileName));
        }

        private void FireStartingApplyMod(object sender, EventArgs e)
        {
            EventHandler handler = _startingApplyModEvent;
            if (handler != null)
            {
                handler(sender, e);
            }
        }

        private void FireFinishedApplyMod(object sender, EventArgs e)
        {
            EventHandler handler = _finishedApplyModEvent;
            if (handler != null)
            {
                handler(sender, e);
            }
        }

        private void FireStartingApplyStep(object sender, EventArgs e)
        {
            EventHandler handler = _startingApplyStepEvent;
            if (handler != null)
            {
                handler(sender, e);
            }
        }

        private void FireFinishedApplyStep(object sender, EventArgs e)
        {
            EventHandler handler = _finishedApplyStepEvent;
            if (handler != null)
            {
                handler(sender, e);
            }
        }

        private void FireSchemaChanged(object sender, EventArgs e)
        {
            EventHandler handler = _schemaChanged;
            if (handler != null)
            {
                handler(sender, e);
            }
        }

        private string GetLatestDbVersion()
        {
            Version latestVersion = new Version(Constant.DbToolsNoDbVersion);
            uint latestRevision = 0;

            foreach (Mod mod in _modDB.Values)
            {
                string versionPart, revisionPart;
                SplitDbVersion(mod.ToConcat, out versionPart, out revisionPart);
                Version version = new Version(versionPart);
                uint revision = revisionPart != null ? uint.Parse(revisionPart, CultureInfo.InvariantCulture) : 0;

                // Ensure we're comparing fully qualified versions
                if (version.Revision <= 0)
                {
                    version = new Version(version.Major, version.Minor, 0, version.Build >= 0 ? version.Build : 0);
                }

                if (version > latestVersion)
                {
                    latestVersion = version;
                    latestRevision = revision;
                }
                else if (version == latestVersion && revision > latestRevision)
                {
                    latestRevision = revision;
                }
            }

            Console.WriteLine("GetLatestDbVersion() = {0}{1}", latestVersion, latestRevision == 0 ? string.Empty : "_" + latestRevision);

            return latestRevision == 0 ? latestVersion.ToString() : latestVersion + "_" + latestRevision;
        }

        private void SplitDbVersion(string versionString, out string version, out string revision)
        {
            string[] versionRevisionSplit = versionString.Split('_');
            version = versionRevisionSplit[0];
            if (versionRevisionSplit.Length > 1)
            {
                revision = versionRevisionSplit[1];
            }
            else
            {
                revision = null;
            }
        }

        private WeightedGraph BuildVersionGraph(int schema)
        {
            if (_versionGraph != null && _schema == schema)
            {
                return _versionGraph;
            }

            WeightedGraph versionGraph = new WeightedGraph();
            foreach (Mod mod in _modDB.Values)
            {
                if (mod.Schema == 0 || schema == 0 || mod.Schema == schema)
                {
                    versionGraph.Add(mod.FromConcat);
                    versionGraph.Add(mod.ToConcat);
                    versionGraph.EdgeAdd(mod.FromConcat, mod.ToConcat, mod.Steps.Length);
                }
            }
            return versionGraph;
        }

        private IList BuildModList(ref int schema, string currentVersion, string requestedVersion, out WeightedGraph versionGraph)
        {
            versionGraph = BuildVersionGraph(schema);

            if (!versionGraph.Contains(currentVersion))
            {
                throw new ApplicationException(string.Format("Current version {0} does not exist in dbmod file.", currentVersion));
            }
            if (!versionGraph.Contains(requestedVersion))
            {
                throw new ApplicationException(string.Format("Requested version {0} does not exist in dbmod file.", requestedVersion));
            }

            IList modList = versionGraph.ShortestPath(currentVersion, requestedVersion);

            if (modList != null && schema == 0)
            {
                string previous = currentVersion;
                for (int i = 1; i < modList.Count; ++i)
                {
                    try
                    {
                        Mod mod = _modDB[new ModTransition { ModFrom = previous, ModTo = (string)modList[i] }];
                        if (mod.Schema != 0)
                        {
                            schema = mod.Schema;
                            break;
                        }
                        previous = (string)modList[i];
                    }
                    catch (Exception ex)
                    {
                        System.Console.WriteLine("Error: at previous " + previous + " to " + (string)modList[i]);
                        throw (ex);
                    }
                }

                if (schema != 0)
                {
                    versionGraph = BuildVersionGraph(schema);
                    modList = versionGraph.ShortestPath(currentVersion, requestedVersion);
                }
            }

            if (modList == null)
            {
                throw new ApplicationException(string.Format("No rules to get from version {0} to version {1}", currentVersion, requestedVersion));
            }

            return modList;
        }

        #endregion Private Implementation

        /// <summary>
        /// A simple data class that is holding values for Mod defined in an XMLDB data file.
        /// 
        /// A Mod represent an atomic operation that can be applied to a DB to modify it from
        /// version 'from' to version 'to'
        /// 
        /// All Steps in a Mod need to be run so the DB is in a consistent state (NB: if a Mod fails during
        /// one of its Steps the DB might be in an inconsistent sate)
        /// 
        /// A Mod is defined by:
        /// 
        /// 1 - an 'author' that is a simple string
        /// 2 - a 'from' version that is the version of the DB the Mod can apply to
        /// 3 - a 'to' version that is the version of the DB the Mod will create
        /// 4 - a 'date' that is the date the Mod was create (not applied!)
        /// 5 - a 'comment' that is anything that can fit in a string
        /// 6 - a 'steps' list that can contain any number of steps in a sequential order (see Step.cs)
        /// 7 - a 'schema' number that flags the mod as only being applicable to databases with the same schema
        /// 
        /// </summary>
        private class Mod
        {
            #region Variables

            private string author = null;
            private string to = null;
            private string toMinor = null;
            private string from = null;
            private string fromMinor = null;
            private string date = null;
            private string comment = null;
            private int _schema;
            private Step[] steps = null;
            private string ifTableNotExists = null;
            private string ifTableExists = null;
            private string ifColumnNotExists = null;
            private string ifColumnExists = null;

            #endregion Variables

            #region Properties

            public string Author
            {
                get
                { return author; }
                set
                { author = value; }
            }

            public string To
            {
                get
                { return to; }
                set
                { to = value; }
            }

            public string ToMinor
            {
                get { return toMinor; }
                set { toMinor = value; }
            }

            public string IfTableNotExists
            {
                get { return ifTableNotExists; }
                set { ifTableNotExists = value; }
            }
            public string IfTableExists
            {
                get { return ifTableExists; }
                set { ifTableExists = value; }
            }
            public string IfColumnNotExists
            {
                get { return ifColumnNotExists; }
                set { ifColumnNotExists = value; }
            }
            public string IfColumnExists
            {
                get { return ifColumnExists; }
                set { ifColumnExists = value; }
            }

            public string ToConcat
            {
                get
                {
                    if (toMinor == string.Empty || toMinor == "0")
                    {
                        return to;
                    }
                    else
                    {
                        return to + "_" + toMinor;
                    }
                }
            }

            public string From
            {
                get
                { return from; }
                set
                { from = value; }
            }

            public string FromMinor
            {
                get { return fromMinor; }
                set { fromMinor = value; }
            }

            public string FromConcat
            {
                get
                {
                    if (fromMinor == string.Empty || fromMinor == "0")
                    {
                        return from;
                    }
                    else
                    {
                        return from + "_" + fromMinor;
                    }
                }
            }

            public string Date
            {
                get
                { return date; }
                set
                { date = value; }
            }

            public string Comment
            {
                get
                { return comment; }
                set
                { comment = value; }
            }

            public int Schema
            {
                get { return _schema; }
                set { _schema = value; }
            }

            public Step[] Steps
            {
                get
                { return steps; }
                set
                { steps = value; }
            }

            #endregion Properties

            #region Methods

            override public string ToString()
            {
                string str = string.Format("<mod author=\"{0}\" from=\"{1}\" fminor=\"{4}\" to=\"{2}\" tminor=\"{5}\" date=\"{3}\" schema=\"{6}\">" + Environment.NewLine, author, from, to, date, fromMinor, toMinor, _schema);
                str += "<comment>" + comment + "</comment>" + Environment.NewLine;
                str += "<steps>" + Environment.NewLine;
                for (int ii = 0; ii < steps.Length; ii++)
                {
                    str += steps[ii] + Environment.NewLine;
                }
                str += "</steps>" + Environment.NewLine;
                str += "</mod>" + Environment.NewLine;
                return str;
            }

            public Mod WithSchema(int schema)
            {
                if (schema == Schema)
                {
                    return this;
                }
                if (Schema != 0)
                {
                    throw new InvalidOperationException("Can only tie a mod to a schema if the mod doesn't have the schema set.");
                }
                var clone = (Mod)MemberwiseClone();
                clone._schema = schema;
                return clone;
            }

            #endregion Methods
        }

        /// <summary>
        /// A simple data class that is holding values for Step defined in an XMLDB data file.
        /// 
        /// Steps are part of Mod and a Mod can contain any number of Steps
        /// 
        /// Steps are atomic and cannot always be reversed. Only 'sql' Steps are run under transaction control
        /// and can be rollbacked
        /// 
        /// A Step is defined by:
        /// 
        /// 1 - a 'type' that is a simpel string that can be of the following:
        ///		- inline_sql : this is just SQL statements inlined in the XML file
        ///		- external_sql : instead of inlining SQL statements in the XML file, we inline the name of a file that
        ///		  contains SQL statements
        ///		- process : any external program that can be execute byt the System.Diagnostics.Process.Start(program, arguments);
        ///		
        /// 2 - a 'content' that is version that is either some SQL statements or the name of a file (external SQL source file or 
        ///		exe/batch/etc... file for example
        ///
        /// Steps do not exist outside of the Mod class and cannot be run separately.
        ///
        ///
        /// </summary>
        private sealed class Step
        {
            #region Variables

            private string type = null;
            private string content = null;
            private Mod _parent;

            #endregion Variables

            #region Properties

            public string Type
            {
                get
                { return type; }
                set
                { type = value; }
            }

            public string Content
            {
                get
                { return content; }
                set
                { content = value; }
            }

            public Mod Parent
            {
                get { return _parent; }
                set { _parent = value; }
            }

            #endregion Properties

            #region Methods

            override public string ToString()
            {
                return "<step type=\"" + type + "\">" + content + "</step>";
            }

            #endregion Methods
        }

        private struct ModTransition
        {
            public string ModFrom;
            public string ModTo;
            public int Schema;

            public ModTransition(Mod mod)
            {
                ModFrom = mod.FromConcat;
                ModTo = mod.ToConcat;
                Schema = mod.Schema;
            }

            public bool Equals(ModTransition other)
            {
                return Equals(other.ModFrom, ModFrom) && Equals(other.ModTo, ModTo) && (Schema == 0 || other.Schema == 0 || other.Schema == Schema);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (obj.GetType() != typeof(ModTransition)) return false;
                return Equals((ModTransition)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int result = (ModFrom != null ? ModFrom.GetHashCode() : 0);
                    result = (result * 397) ^ (ModTo != null ? ModTo.GetHashCode() : 0);
                    return result;
                }
            }
        }
    }
    /// <summary>
    /// Constant definitions for DBTools.
    /// </summary>
    public sealed class Constant
    {
        public const string DbToolsNoDbVersion = "0.0";
        public const string DbToolsExecuteMethod = "Execute";
        public const string DbToolsAssembliesPrefix = "[assemblies:";
        public const string DbToolsAssembliesSuffix = "]";
        public const string DbToolsAssembliesSeparator = ";";
    }
}
