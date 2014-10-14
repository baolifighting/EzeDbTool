using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace EzeDbTools
{
    public delegate object ArgumentConverter(string arg);
    public class CommandContext
    {
        #region Constants

        private const string UnexpectedArgumentError = "Unexpected argument '{0}'.";
        private const string UnexpectedParameterError = "Unexpected parameter '{0}'.";
        private const string DuplicateParameterError = "Duplicate parameter '{0}'.";
        private const string MissingParameterError = "Required parameter '{0}' is undefinied.";
        private const string IncorrectArgumentType = "Couldn't convert '{0}' to the expected type.";

        #endregion Constants

        #region Variables

        private readonly Dictionary<Type, ArgumentConverter> _convertersByType;
        private readonly Dictionary<string, OptionEntry> _nameToEntry;
        private readonly string _parameter;
        private readonly Dictionary<string, OptionEntry> _unSetRequiredOptions;
        protected string ErrorMessage;
        private List<string> _postParseArgs;
        private string _specifiedUsageString;

        #endregion Variables

        #region Lifetime

        public CommandContext(OptionEntry[] entries, string parameter)
        {
            _nameToEntry = new Dictionary<string, OptionEntry>();
            _unSetRequiredOptions = new Dictionary<string, OptionEntry>();
            _parameter = parameter;

            foreach (OptionEntry entry in entries)
            {
                _nameToEntry.Add(entry.LongName, entry);

                if (entry.ShortName != char.MinValue)
                {
                    _nameToEntry.Add(entry.ShortName.ToString(CultureInfo.InvariantCulture), entry);
                }

                if ((entry.Flags & OptionEntryFlags.Required) != OptionEntryFlags.None)
                {
                    _unSetRequiredOptions.Add(entry.LongName, entry);
                }
            }

            _convertersByType = new Dictionary<Type, ArgumentConverter>
                {
                    {typeof (string[]), DefaultArrayConverter},
                    {typeof (FileInfo), DefaultFileInfoConverter},
                    {typeof (DirectoryInfo), DefaultDirectoryInfoConverter}
                };
        }

        #endregion Lifetime

        #region Properties

        public string ParseErrorMessage
        {
            get { return ErrorMessage; }
        }

        public string[] PostParseArguments
        {
            get { return _postParseArgs.ToArray(); }
        }

        public string SpecifiedUsageString
        {
            get { return _specifiedUsageString; }
            set { _specifiedUsageString = value; }
        }

        #endregion Properties

        #region Methods

        public void OverrideConverter(Type type, ArgumentConverter converter)
        {
            _convertersByType[type] = converter;
        }

        public bool Parse(string[] args)
        {
            return Parse(args, false);
        }

        public bool Parse(string[] args, bool showHelpWindow)
        {
            Dictionary<string, string> argsByName;
            string lastParameter;
            if (!ScanArgs(args, out argsByName, out lastParameter))
            {
                return false;
            }

            var setOptions = new List<OptionEntry>();

            foreach (var argByName in argsByName)
            {
                string name = argByName.Key;
                string arg = argByName.Value;

                if (name == "help" || name == "?")
                {
                    if (showHelpWindow)
                    {
                       // GuiManager.Info(GenerateHelpText());
                    }
                    else
                    {
                        DisplayHelp();
                    }
                    Environment.Exit(0);
                }

                OptionEntry entry;
                if (!_nameToEntry.TryGetValue(name, out entry))
                {
                    ErrorMessage = string.Format(UnexpectedParameterError, name);
                    return false;
                }

                if (setOptions.Contains(entry))
                {
                    ErrorMessage = string.Format(DuplicateParameterError, entry.LongName);
                    return false;
                }

                if (name == lastParameter &&
                    !(StringComparer.CurrentCultureIgnoreCase.Equals(bool.TrueString, arg) ||
                      StringComparer.CurrentCultureIgnoreCase.Equals(bool.FalseString, arg)) &&
                    _nameToEntry[name].Argument.ArgumentType == typeof(bool))
                {
                    _postParseArgs.Insert(0, arg);
                    arg = bool.TrueString;
                }

                object correctType = ConvertArg(arg, entry.Argument.ArgumentType);
                if (correctType == null || !entry.Argument.ArgumentType.IsInstanceOfType(correctType))
                {
                    ErrorMessage = string.Format(IncorrectArgumentType, arg);
                    return false;
                }

                entry.Argument.Value = correctType;

                _unSetRequiredOptions.Remove(entry.LongName);
                setOptions.Add(entry);
            }

            if (_unSetRequiredOptions.Count > 0)
            {
                string parameterName = string.Empty;

                foreach (string key in _unSetRequiredOptions.Keys)
                {
                    parameterName = key;
                    break;
                }

                ErrorMessage = string.Format(MissingParameterError, parameterName);
                return false;
            }

            return true;
        }

        public void DisplayHelp()
        {
            Console.WriteLine(GenerateHelpText());
        }

        public virtual string GenerateHelpText()
        {
            var builder = new StringBuilder();

            if (_specifiedUsageString != null)
            {
                builder.AppendLine(_specifiedUsageString);
            }
            else
            {
                builder.Append("Usage:");
                builder.Append(Environment.NewLine);
                builder.AppendFormat("  {0} [OPTION...] {1}", Path.GetFileName(Environment.GetCommandLineArgs()[0]),
                                     _parameter ?? "");
                builder.Append(Environment.NewLine);
            }

            var values = new List<OptionEntry>();
            foreach (OptionEntry value in _nameToEntry.Values)
            {
                if (!values.Contains(value) && (value.Flags & OptionEntryFlags.Hidden) == OptionEntryFlags.None)
                {
                    values.Add(value);
                }
            }

            if (values.Count > 0)
            {
                builder.Append(Environment.NewLine);
                builder.Append("Options:");
                builder.Append(Environment.NewLine);
            }

            foreach (OptionEntry optionEntry in values)
            {
                string shortParameterArg = string.Empty;
                string longParameterArg = string.Empty;
                if (optionEntry.Argument.ArgumentType != typeof(bool))
                {
                    string parameter = optionEntry.DescriptionArg == char.MinValue
                                           ? optionEntry.LongName.ToUpper()
                                           : optionEntry.DescriptionArg.ToString(CultureInfo.InvariantCulture);
                    shortParameterArg = " " + parameter;
                    longParameterArg = "=" + parameter;
                }

                if (optionEntry.ShortName != char.MinValue)
                {
                    builder.AppendFormat("-{0}{1} OR --{2}{3}", optionEntry.ShortName, shortParameterArg,
                                         optionEntry.LongName, longParameterArg);
                }
                else
                {
                    builder.AppendFormat("--{0}{1}", optionEntry.LongName, longParameterArg);
                }

                builder.Append(Environment.NewLine);
                builder.AppendFormat("     {0}", optionEntry.Description);
                builder.Append(Environment.NewLine);
                builder.Append(Environment.NewLine);
            }

            builder.Append(Environment.NewLine);

            return builder.ToString();
        }

        #endregion Methods

        #region Default Converters

        //Converts to a string[], assumes arg list is seperated by commas or spaces
        private object DefaultArrayConverter(string arg)
        {
            var spliter = new Regex(@" |,", RegexOptions.Compiled);
            return spliter.Split(arg);
        }

        private object DefaultFileInfoConverter(string arg)
        {
            return new FileInfo(arg);
        }

        private object DefaultDirectoryInfoConverter(string arg)
        {
            return new DirectoryInfo(arg);
        }

        #endregion Default Converters

        #region Private Implementation

        protected object ConvertArg(string arg, Type targetType)
        {
            try
            {
                ArgumentConverter converter;
                if (_convertersByType.TryGetValue(targetType, out converter))
                {
                    return converter(arg);
                }
                TypeConverter typeConverter = TypeDescriptor.GetConverter(targetType);
                if (typeConverter.CanConvertFrom(typeof(string)))
                {
                    return typeConverter.ConvertFrom(arg);
                }
                return Convert.ChangeType(arg, targetType);
            }
            catch
            {
                return null;
            }
        }

        protected virtual bool ScanArgs(string[] args, out Dictionary<string, string> argsByName,
                                        out string lastParameter)
        {
            argsByName = new Dictionary<string, string>();
            lastParameter = null;
            int i = 0;
            if (args.Length > 0 && Environment.GetCommandLineArgs()[0] == args[0])
            {
                i = 1;
            }

            _postParseArgs = new List<string>();
            string parameter = null;
            var startsWith = new Regex(@"^-{1,2}", RegexOptions.Compiled);
            var spliter = new Regex(@"^--|=", RegexOptions.Compiled);
            var remover = new Regex(@"^['""]?(.*)['""]?$", RegexOptions.Compiled);

            for (; i < args.Length; ++i)
            {
                if (startsWith.IsMatch(args[i]))
                {
                    if (_postParseArgs.Count != 0)
                    {
                        ErrorMessage = string.Format(UnexpectedArgumentError, _postParseArgs[0]);
                        return false;
                    }

                    if (parameter != null)
                    {
                        if (!argsByName.ContainsKey(parameter))
                        {
                            argsByName.Add(parameter, bool.TrueString);
                        }
                        else
                        {
                            ErrorMessage = string.Format(DuplicateParameterError, parameter);
                            return false;
                        }
                        lastParameter = parameter;
                        parameter = null;
                    }

                    if (args[i].StartsWith("--"))
                    {
                        if (args[i] == "--")
                        {
                            ++i;
                            break;
                        }

                        string[] parts = spliter.Split(args[i], 3);

                        switch (parts.Length)
                        {
                            // Found just a parameter
                            case 2:
                                parameter = parts[1];
                                break;

                            // Parameter with enclosed value
                            case 3:
                                // Remove possible enclosing characters (",')
                                if (!argsByName.ContainsKey(parts[1]))
                                {
                                    parts[2] = remover.Replace(parts[2], "$1");
                                    argsByName.Add(parts[1], parts[2]);
                                }
                                else
                                {
                                    ErrorMessage = string.Format(DuplicateParameterError, parameter);
                                    return false;
                                }
                                break;
                        }
                    }
                    else
                    {
                        if (args[i].Length == 1)
                        {
                            ErrorMessage = string.Format(UnexpectedArgumentError, args[i]);
                            return false;
                        }
                        char[] shortParameters = args[i].ToCharArray();
                        for (int j = 1; j < shortParameters.Length - 1; ++j)
                        {
                            if (!argsByName.ContainsKey(shortParameters[j].ToString(CultureInfo.InvariantCulture)))
                            {
                                argsByName.Add(shortParameters[j].ToString(CultureInfo.InvariantCulture),
                                               bool.TrueString);
                            }
                            else
                            {
                                ErrorMessage = string.Format(DuplicateParameterError, shortParameters[j]);
                                return false;
                            }
                        }

                        parameter = shortParameters[shortParameters.Length - 1].ToString(CultureInfo.InvariantCulture);
                    }
                }
                else if (parameter != null)
                {
                    if (!argsByName.ContainsKey(parameter))
                    {
                        string paramValue = remover.Replace(args[i], "$1");
                        argsByName.Add(parameter, paramValue);
                    }
                    else
                    {
                        ErrorMessage = string.Format(DuplicateParameterError, parameter);
                        return false;
                    }
                    lastParameter = parameter;
                    parameter = null;
                }
                else
                {
                    string argValue = remover.Replace(args[i], "$1");
                    _postParseArgs.Add(argValue);
                }
            }

            if (parameter != null)
            {
                if (!argsByName.ContainsKey(parameter))
                {
                    argsByName.Add(parameter, bool.TrueString);
                }
                else
                {
                    ErrorMessage = string.Format(DuplicateParameterError, parameter);
                    return false;
                }
                lastParameter = parameter;
            }

            //anything left over after the "--" argument.
            for (; i < args.Length; ++i)
            {
                _postParseArgs.Add(args[i]);
            }

            return true;
        }

        #endregion Private Implementation
    }
}
