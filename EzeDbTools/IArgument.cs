using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzeDbTools
{
    public interface IArgument
    {
        Type ArgumentType { get; }
        object Value { get; set; }
    }
}
