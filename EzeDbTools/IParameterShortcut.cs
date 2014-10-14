using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzeDbTools
{
    internal interface IParameterShortcut : IArgument
    {
        string ParameterName { get; }
        ParameterMergeDirection MergeDirection { get; }
    }
}
