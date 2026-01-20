using BCrypt.Net;
using BitzArt.Blazor.Cookies;
using CommonLibraryP.API;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace CommonLibraryP.UserPKG
{
    public class UserService
    {
        private string cookieName = "TMMAuthCookie";
        private string encryptKey = "TMMAuthEncryptKey";
        private readonly int expiredHour = 1;

        private readonly IServiceScopeFactory scopeFactory;
        private readonly ICookieService cookieService;
        private readonly IDataProtector dataProtector;


        private UserInfoDTO? userInfoDTO;
        public UserInfoDTO? UserInfoDTO => userInfoDTO;
        public bool IsAuth => userInfoDTO is not null && userInfoDTO.Role is not UserRole.Guest;

        private bool isProcessing = false;

        public bool IsPorcossing => isProcessing;

        private void StartProcessing()
        {
			isProcessing = true;
			ProcessStatusChanged();
		}
        private void StopProcessing()
        {
			isProcessing = false;
			ProcessStatusChanged();
		} 

		public Action? UserStatusChangedAct;
        private void UserStatusChanged() => UserStatusChangedAct?.Invoke();

		public Action? ProcessStatusChangedAct;
		private void ProcessStatusChanged() => ProcessStatusChangedAct?.Invoke();

		public UserService(IServiceScopeFactory scopeFactory, ICookieService cookieService, IDataProtectionProvider provider)
        {
            this.scopeFactory = scopeFactory;
            this.cookieService = cookieService;
            dataProtector = provider.CreateProtector(encryptKey);
        }

        private void SetUserInfoDTO(UserInfoDTO userInfoDTO)
        {
            this.userInfoDTO = userInfoDTO;
            UserStatusChanged();
        }

        public async Task<RequestResult> LoginAsync(UserLoginDTO userLoginDTO)
        {
            StartProcessing();
            if (userLoginDTO.IsDeveloper)
            {
                SetUserInfoDTO(new UserInfoDTO(UserRole.Admin));
                await EncryptUserInfoDTOToCookie(userInfoDTO);
				StopProcessing();
				return new(2, "Developer admin login success");
            }
            else
            {
                var res = await VerifyGeneralUser(userLoginDTO);
                if (res.IsSuccess)
                {
                    var userInfo = res.Obj!;
                    SetUserInfoDTO(new UserInfoDTO(userInfo));
                    await EncryptUserInfoDTOToCookie(userInfoDTO);
					StopProcessing();
					return new(2, $"User {userInfoDTO.Username} login success");
                }
                else
                {
					StopProcessing();
					return res;
                }
            }
            
        }

        public async Task<RequestResult> LogutAsync()
        {
            StartProcessing();
			await cookieService.RemoveAsync(cookieName);
            GuestLogin();
            StopProcessing();
            return new(2, "Logout success");
		}

		private async Task<RequestResult<UserInfo?>> VerifyGeneralUser(UserLoginDTO userLoginDTO)
        {
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<UserDBContext>();
            var targetUser = await dbContext.UserInfos.FirstOrDefaultAsync(u => u.Username == userLoginDTO.UserName);

            if (targetUser is null)
            {
                return new(4, $"User {userLoginDTO.UserName} not found", null);
            }

            if (!BCrypt.Net.BCrypt.Verify(userLoginDTO.Password, targetUser.PasswordHash))
            {
                return new(4, $"User {targetUser.Username} password not match", null);
            }
            return new(2, $"User {targetUser.Username} verify success", targetUser);
        }

        private void GuestLogin()
        {
            SetUserInfoDTO(new UserInfoDTO(UserRole.Guest));
        }

        private async Task EncryptUserInfoDTOToCookie(UserInfoDTO userInfoDTO)
        {
            var jsonString = JsonSerializer.Serialize(userInfoDTO);
            var encryptedUserInfoDTO = dataProtector.Protect(jsonString);
            await cookieService.SetAsync(cookieName, encryptedUserInfoDTO.ToString(), DateTime.Now.AddHours(expiredHour));
        }

        public async Task DecryptUserInfoDTOFromCookie()
        {
            try
            {
                StartProcessing();
                var cookie = await cookieService.GetAsync(cookieName);
                if (cookie is not null)
                {
                    var decryptedUserInfoDTOStr = cookie.Value;
                    var jsonString = dataProtector.Unprotect(decryptedUserInfoDTOStr);
                    SetUserInfoDTO(JsonSerializer.Deserialize<UserInfoDTO>(jsonString));
                }
                else
                {
                    GuestLogin();
                }
            }
            catch(Exception e)
            {
                GuestLogin();
            }
            finally
            {
                StopProcessing();
			}
		}

        public bool IsRole(UserRole userRole)
        {
            return IsAuth && !isProcessing && userInfoDTO!.Role == userRole;
		}
    }
}
