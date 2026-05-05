using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.UserPKG
{
	public class NewUserDTO
	{
		public string UserName { get; set; } = null!;

		public string Password { get; set; } = null!;

		public UserRole NewUserRole { get; set; }
	}
}
