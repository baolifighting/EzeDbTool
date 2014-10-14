using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EzeDbTools
{
	public enum DbInitialization
	{
		Valid,
		NewDatabase, // valid but a new database will be created

		InvalidUserNameOrPassword,
		InvalidDatabase, // database with this name already exists and doesn't seem to be a Eze db.
		OtherError,
	}
}
