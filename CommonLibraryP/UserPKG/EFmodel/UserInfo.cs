using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.UserPKG
{
    public enum UserRole
    {
        Guest = 0,
        Admin = 1,
        User = 2
    }

    public class UserInfo
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public UserRole Role { get; set; }

    }
}
