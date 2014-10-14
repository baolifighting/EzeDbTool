using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzeDbTools
{
    /// <summary>
    /// Constant definitions for DBTools.
    /// </summary>
    public sealed class DbToolsConstants
    {
        public const string DBTOOLS_MYSQL_DATABASE = "mysql";
        public const string DBTOOLS_SQLSERVER_DATABASE = "master";
        public const string DBTOOLS_NO_DB_VERSION = "0.0";
        public const string DBTOOLS_PARAMETER_PREFIX = "-d";
        public const string DBTOOLS_PARAMETER_SEPARATOR = "=";
        public const string DBTOOLS_SELECT_MOD_XPATH_EXPRESSION = "/" + XML_MODS_ELEMENT + "/" + XML_MOD_ELEMENT;
        public const string DBTOOLS_PARAMETER_CHARACTER_PREFIX = "%";
        public const string DBTOOLS_PARAMETER_CHARACTER_SUFFIX = "%";
        public const string DBTOOLS_EXECUTE_METHOD = "Execute";
        public const string DBTOOLS_ASSEMBLIES_PREFIX = "[assemblies:";
        public const string DBTOOLS_ASSEMBLIES_SUFFIX = "]";
        public const string DBTOOLS_ASSEMBLIES_SEPARATOR = ";";

        public const string XML_MODS_ELEMENT = "mods";
        public const string XML_MOD_ELEMENT = "mod";
        public const string XML_MOD_AUTHOR_ATTRIBUTE = "author";
        public const string XML_MOD_TO_ATTRIBUTE = "to";
        public const string XML_MOD_FROM_ATTRIBUTE = "from";
        public const string XML_MOD_DATE_ATTRIBUTE = "date";
        public const string XML_MOD_COMMENT_ELEMENT = "comment";
        public const string XML_MOD_STEPS_ELEMENT = "steps";
        public const string XML_MOD_STEPS_STEP_ELEMENT = "step";
        public const string XML_STEP_TYPE_ATTRIBUTE = "type";

        public const string XML_STEP_TYPE_VALUE_INLINE_SQL = "inline_sql";
        public const string XML_STEP_TYPE_VALUE_EXTERNAL_SQL = "external_sql";
        public const string XML_STEP_TYPE_VALUE_INLINE_CSHARP = "inline_csharp";
        public const string XML_STEP_TYPE_VALUE_EXTERNAL_CSHARP = "external_csharp";
        public const string XML_STEP_TYPE_VALUE_INLINE_VISUALBASIC = "inline_visualbasic";
        public const string XML_STEP_TYPE_VALUE_EXTERNAL_VISUALBASIC = "external_visualbasic";
        public const string XML_STEP_TYPE_VALUE_INLINE_JS = "inline_js";
        public const string XML_STEP_TYPE_VALUE_EXTERNAL_JS = "external_js";
        public const string XML_STEP_TYPE_VALUE_PROCESS = "process";
    }
}
