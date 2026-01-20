using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.UserPKG
{
    public class UserInfoDTO
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = null!;
        public UserRole Role { get; set; }

        public UserInfoDTO(){ }

        public UserInfoDTO(UserRole userRole)
        {
            Id = Guid.NewGuid();
            Username = userRole.ToString();
            Role = userRole;
        }
        public UserInfoDTO(UserInfo userInfo)
        {
            Id = userInfo.Id;
            Username = userInfo.Username;
            Role = userInfo.Role;
        }
    }
}
