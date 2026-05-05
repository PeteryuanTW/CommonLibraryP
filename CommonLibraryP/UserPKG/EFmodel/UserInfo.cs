using BC = BCrypt.Net;
namespace CommonLibraryP.UserPKG
{
	

	public class UserInfo
	{
		public Guid Id { get; set; }
		public string Username { get; set; } = null!;
		public string PasswordHash { get; set; } = null!;
		public UserRole Role { get; set; }

		public UserInfo()
		{
		}

		public UserInfo(NewUserDTO newUserDTO)
		{
			Id = Guid.NewGuid();
			Username = newUserDTO.UserName;
			PasswordHash = BC.BCrypt.HashPassword(newUserDTO.Password);
			Role = newUserDTO.NewUserRole;
		}

	}
}
