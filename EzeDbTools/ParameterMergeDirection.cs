using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzeDbTools
{
    internal enum ParameterMergeDirection
    {
        /// <summary>
        /// Overrides whatever the argument is with the parameter if the parameter was passed
        /// </summary>
        ParameterToArgument,
        /// <summary>
        /// Sets the argument value to the parameter if the parameter was not passed in.  Does nothing if the parameter was passed
        /// </summary>
        ArgumentToParameter,
        /// <summary>
        /// Sets the argument value to the parameter if the parameter was not passed in.  If the parameter was passed, override whatever the argument is with the parameter
        /// </summary>
        Any,
    }
}
