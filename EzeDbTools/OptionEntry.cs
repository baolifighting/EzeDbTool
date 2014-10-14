using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzeDbTools
{
    public class OptionEntry
    {
        #region Variables

        public readonly IArgument Argument;
        public readonly string Description;
        public readonly char DescriptionArg;
        public readonly OptionEntryFlags Flags;
        public readonly string LongName;
        public readonly char ShortName;

        #endregion Variables

        #region Lifetime

        /// <summary>
        ///     Defines a command line option to be passed to a CommandContext
        /// </summary>
        /// <param name="longName">Required: the name of the option</param>
        /// <param name="argument">Required: a constructed generic Argument type representing the type and default value of the argument</param>
        /// <param name="description">Recommended: a description of the option. Set to null to hide the option from the user.</param>
        public OptionEntry(string longName, IArgument argument, string description)
        {
            if (string.IsNullOrEmpty(longName))
            {
                throw new ArgumentException("OptionEntry must have a longName");
            }
            if (longName.Contains("="))
            {
                throw new ArgumentException("OptionEntry longName cannot contain an '='");
            }

            LongName = longName;
            Argument = argument;
            if (description == null)
            {
                description = string.Empty;
            }
            Description = description;
        }

        /// <summary>
        ///     Defines a command line option that can be passed to a CommandContext
        /// </summary>
        /// <param name="longName">Required: the name of the option</param>
        /// <param name="shortName">Optional: the short name of the option, pass char.MinValue to ignore</param>
        /// <param name="argument">Required: a constructed generic Argument type representing the type and default value of the argument</param>
        /// <param name="description">Recommended: a description of the option. Set to null to hide the option from the user.</param>
        public OptionEntry(string longName, char shortName, IArgument argument, string description)
            : this(longName, argument, description)
        {
            ShortName = shortName;
        }

        /// <summary>
        ///     Defines a command line option that can be passed to a CommandContext
        /// </summary>
        /// <param name="longName">Required: the name of the option</param>
        /// <param name="argument">Required: a constructed generic Argument type representing the type and default value of the argument</param>
        /// <param name="description">Recommended: a description of the option. Set to null to hide the option from the user.</param>
        /// <param name="descriptionArg">Optional: if the argument is not a bool, this character is used to represent the value in the help output</param>
        public OptionEntry(string longName, IArgument argument, string description, char descriptionArg)
            : this(longName, argument, description)
        {
            DescriptionArg = descriptionArg;
        }

        /// <summary>
        ///     Defines a command line option that can be passed to a CommandContext
        /// </summary>
        /// <param name="longName">Required: the name of the option</param>
        /// <param name="argument">Required: a constructed generic Argument type representing the type and default value of the argument</param>
        /// <param name="description">Recommended: a description of the option. Set to null to hide the option from the user.</param>
        /// <param name="flags">Optional: argument flags</param>
        public OptionEntry(string longName, IArgument argument, string description, OptionEntryFlags flags)
            : this(longName, argument, description)
        {
            Flags = flags;
        }


        /// <summary>
        ///     Defines a command line option that can be passed to a CommandContext
        /// </summary>
        /// <param name="longName">Required: the name of the option</param>
        /// <param name="shortName">Optional: the short name of the option, pass char.MinValue to ignore</param>
        /// <param name="argument">Required: a constructed generic Argument type representing the type and default value of the argument</param>
        /// <param name="description">Recommended: a description of the option. Set to null to hide the option from the user.</param>
        /// <param name="descriptionArg">Optional: if the argument is not a bool, this character is used to represent the value in the help output</param>
        public OptionEntry(string longName, char shortName, IArgument argument, string description, char descriptionArg)
            : this(longName, argument, description)
        {
            ShortName = shortName;
            DescriptionArg = descriptionArg;
        }

        /// <summary>
        ///     Defines a command line option that can be passed to a CommandContext
        /// </summary>
        /// <param name="longName">Required: the name of the option</param>
        /// <param name="shortName">Optional: the short name of the option, pass char.MinValue to ignore</param>
        /// <param name="argument">Required: a constructed generic Argument type representing the type and default value of the argument</param>
        /// <param name="description">Recommended: a description of the option. Set to null to hide the option from the user.</param>
        /// <param name="descriptionArg">Optional: if the argument is not a bool, this character is used to represent the value in the help output</param>
        /// <param name="flags">Optional: argument flags</param>
        public OptionEntry(string longName, char shortName, IArgument argument, string description, char descriptionArg,
                           OptionEntryFlags flags)
            : this(longName, argument, description)
        {
            ShortName = shortName;
            DescriptionArg = descriptionArg;
            Flags = flags;
        }

        #endregion Lifetime
    }
}
