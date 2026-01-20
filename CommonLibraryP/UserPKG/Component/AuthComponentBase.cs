using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.UserPKG.Component
{
    public class AuthComponentBase : ComponentBase
    {
        [Inject]
        public UserService userService { get; set; } = null!;

        protected override async Task OnInitializedAsync()
        {
            await userService.DecryptUserInfoDTOFromCookie();
			userService.UserStatusChangedAct += UserUpdate;
			userService.ProcessStatusChangedAct += ProcessingUpdate;
			await base.OnInitializedAsync();
        }

        private void UserUpdate()
        {
            InvokeAsync(StateHasChanged);
        }
		private void ProcessingUpdate()
        {
			InvokeAsync(StateHasChanged);
		}

	}
}
