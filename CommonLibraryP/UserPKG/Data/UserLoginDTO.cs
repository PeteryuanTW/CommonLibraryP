using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.UserPKG
{
    public class UserLoginDTO
    {
        [Required]
        public string UserName { get; set; } = string.Empty;
		[Required]
		public string Password { get; set; } = string.Empty;

        public bool IsDeveloper => UserName == UserRole.Admin.ToString() && Password == UserRole.Admin.ToString();
    }
}
