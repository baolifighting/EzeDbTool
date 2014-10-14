using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Libraries.EzeDbCommon
{
	public enum ConnectionStatus
	{
		Unknown,
		Valid,
		InvalidUsernameOrPassword,
		InvalidDatabase,
		InvalidTable
	}
}
