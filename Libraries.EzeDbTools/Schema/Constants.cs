using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Libraries.EzeDbTools.Schema
{
    /// <summary>
    /// Constant definitions for DBTools.
    /// </summary>
    public sealed class Constants
    {
        public const string DbToolsMySqlPatchFile = "dbmods.xml";
        public const string DbToolsSqlServerPatchFile = "SqlServerDbMods.xml";
        public const string DbToolsSelectModXPathExpression = "/" + XmlModsElement + "/" + XmlModElement;
        public const string DbtoolsParameterCharacterPrefix = "%";
        public const string DbToolsParameterCharacterSuffix = "%";

        public const string XmlModsElement = "mods";
        public const string XmlModElement = "mod";
        public const string XmlModAuthorAttribute = "author";
        public const string XmlModToAttribute = "to";
        public const string XmlModToMinorAttribute = "tminor";
        public const string XmlModFromAttribute = "from";
        public const string XmlModFromMinorAttribute = "fminor";
        public const string XmlModDateAttribute = "date";
        public const string XmlModSchemaAttribute = "schema";
        public const string XmlModCommentElement = "comment";
        public const string XmlModStepsElement = "steps";
        public const string XmlModStepsStepElement = "step";
        public const string XmlStepTypeAttribute = "type";

        public const string XmlStepIfTableNotExistsAttribute = "ifTableNotExists";
        public const string XmlStepIfTableExistsAttribute = "ifTableExists";
        public const string XmlStepIfColumnNotExistsAttribute = "ifColumnNotExists";
        public const string XmlStepIfColumnExistsAttribute = "ifColumnExists";


        public const string XmlStepTypeValueInlineSql = "inline_sql";
        public const string XmlStepTypeValueExternalSql = "external_sql";
        public const string XmlStepTypeValueInlineCSharp = "inline_csharp";
        public const string XmlStepTypeValueExternalCSharp = "external_csharp";
        public const string XmlStepTypeValueInlineVisualBasic = "inline_visualbasic";
        public const string XmlStepTypeValueExternalVisualBasic = "external_visualbasic";
        public const string XmlStepTypeValueInlineJs = "inline_js";
        public const string XmlStepTypeValueExternalJs = "external_js";
        public const string XmlStepTypeValueProcess = "process";
        public const string XmlStepTypeValueMod = "mod";

        public const string ExternalSqlDirectoryName = "ExternalSql";
        public const string ExternalCSharpDirectoryName = "ExternalCSharp";
        public const string ExternalVisualBasicDirectoryName = "ExternalVisualBasic";
        public const string ExternalJsDirectoryName = "ExternalJs";
    }
}

