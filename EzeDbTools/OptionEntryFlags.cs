using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzeDbTools
{
    [Flags]
    public enum OptionEntryFlags
    {
        None = 0x00,
        Required = 0x01,
        Hidden = 0x02,
    }
}