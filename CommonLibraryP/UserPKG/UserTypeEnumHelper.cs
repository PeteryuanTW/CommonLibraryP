using CommonLibraryP.Data;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.UserPKG
{
	public static class UserTypeEnumHelper
	{
		public static IEnumerable<UserRoleWrapperClass> GetGeneralUserRoleWrapperClass(UserRole currentRole, IStringLocalizer localizer)
		{
			return Enum.GetValues(typeof(UserRole)).OfType<UserRole>().Where(x => x != UserRole.Guest && x <= currentRole)
				.Select(x => new UserRoleWrapperClass(x, localizer));
		}
	}

	public class UserRoleWrapperClass : EnumWrapper
	{
		public UserRoleWrapperClass(UserRole userRole, IStringLocalizer localizer)
		{
			UserRole = userRole;
			index = (int)userRole;
			displayName = localizer[userRole.ToString()];
		}

		public UserRole UserRole { get; init; }
	}

	public enum UserRole
	{
		Guest = 0,
		User = 1,
		Admin = 10,
		Developer = 99,
	}
}
