using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EzeDbTools
{
    internal class DbToolsCommandContext : CommandContext
    {
        private readonly Dictionary<string, string> _parameters;
        private readonly List<IParameterShortcut> _parameterShortcuts;

        public DbToolsCommandContext(OptionEntry[] entries)
            : base(entries, " - Updates a 3VR database")
        {
            _parameters = new Dictionary<string, string>();
            _parameterShortcuts = new List<IParameterShortcut>();
            foreach (var item in entries.Select(entry => entry.Argument).OfType<IParameterShortcut>())
            {
                _parameterShortcuts.Add(item);
            }
        }

        public override string GenerateHelpText()
        {
            string baseHelp = base.GenerateHelpText();
            if (baseHelp.EndsWith(Environment.NewLine))
            {
                baseHelp = baseHelp.Substring(0, baseHelp.Length - Environment.NewLine.Length);
            }
            StringBuilder parameterHelp = new StringBuilder();
            parameterHelp.Append(baseHelp);
            parameterHelp.AppendLine("-dPARAMETER=VALUE").Append("     Replaces occurences of PARAMETER in dbmods with VALUE before applying the mod");
            return parameterHelp.AppendLine().AppendLine().AppendLine().ToString();
        }

        protected override bool ScanArgs(string[] args, out Dictionary<string, string> argsByName, out string lastParameter)
        {
            List<string> argList = new List<string>();
            foreach (string arg in args)
            {
                if (arg.StartsWith(DbToolsConstants.DBTOOLS_PARAMETER_PREFIX) && arg.IndexOf(DbToolsConstants.DBTOOLS_PARAMETER_SEPARATOR, StringComparison.Ordinal) >= 3)
                {
                    string key = arg.Substring(2, arg.IndexOf(DbToolsConstants.DBTOOLS_PARAMETER_SEPARATOR, StringComparison.Ordinal) - 2);
                    string val = arg.Substring(arg.IndexOf(DbToolsConstants.DBTOOLS_PARAMETER_SEPARATOR, StringComparison.Ordinal) + 1);
                    _parameters.Add(key, val);
                }
                else
                {
                    argList.Add(arg);
                }
            }
            return base.ScanArgs(argList.ToArray(), out argsByName, out lastParameter);
        }

        public IDictionary<string, string> GetParameters(bool listParameters)
        {
            MergeParameters();

            if (listParameters)
            {
                foreach (KeyValuePair<string, string> keyValuePair in _parameters)
                {
                    Console.WriteLine(keyValuePair.Key + DbToolsConstants.DBTOOLS_PARAMETER_SEPARATOR + keyValuePair.Value);
                }
            }
            return new Dictionary<string, string>(_parameters);
        }

        private void MergeParameters()
        {
            foreach (IParameterShortcut parameterShortcut in _parameterShortcuts)
            {
                string value = null;
                if (parameterShortcut.Value != null)
                {
                    if (parameterShortcut.Value is bool)
                    {
                        value = (bool)parameterShortcut.Value ? "true" : "false";
                    }
                    else
                    {
                        value = parameterShortcut.Value.ToString();
                    }
                }

                switch (parameterShortcut.MergeDirection)
                {
                    case ParameterMergeDirection.ParameterToArgument:
                        if (_parameters.ContainsKey(parameterShortcut.ParameterName))
                        {
                            parameterShortcut.Value = ConvertArg(_parameters[parameterShortcut.ParameterName], parameterShortcut.ArgumentType);
                        }
                        break;
                    case ParameterMergeDirection.ArgumentToParameter:
                        if (!_parameters.ContainsKey(parameterShortcut.ParameterName) && !string.IsNullOrEmpty(value))
                        {
                            _parameters.Add(parameterShortcut.ParameterName, value);
                        }
                        break;
                    case ParameterMergeDirection.Any:
                        if (_parameters.ContainsKey(parameterShortcut.ParameterName))
                        {
                            parameterShortcut.Value = ConvertArg(_parameters[parameterShortcut.ParameterName], parameterShortcut.ArgumentType);
                        }
                        else if (!string.IsNullOrEmpty(value))
                        {
                            _parameters.Add(parameterShortcut.ParameterName, value);
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            Console.WriteLine("************************");
            foreach (var item in _parameters)
            {
                Console.WriteLine(item.Key + " : " + item.Value);
            }
            Console.WriteLine("************************");
            foreach (var item in _parameterShortcuts)
            {
                Console.WriteLine(item.ParameterName + " : " + item.Value);
            }
        }
    }
}
