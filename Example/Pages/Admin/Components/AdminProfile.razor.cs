using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using PocketBaseSharp;
using PocketBaseSharp.Extensions;
using PocketBaseSharp.Models;

namespace Example.Pages.Admin.Components
{
    public partial class AdminProfile
    {
        [Inject]
        public NavigationManager NavigationManager { get; set; } = null!;

        [Inject]
        public PocketBase PocketBase { get; set; } = null!;

        [Inject]
        public ISnackbar Snackbar { get; set; } = null!;

        [Inject]
        public AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;

        protected AdminModel? _currentAdmin = null;

        protected override async Task OnInitializedAsync()
        {
            var currentAdminResult = await PocketBase.GetCurrentAdminAsync();
            if (currentAdminResult.IsSuccess)
            {
                _currentAdmin = currentAdminResult.Value;
            }

            await base.OnInitializedAsync();
        }

        protected async Task LogoutAsync()
        {
            PocketBase.AuthStore.Clear();
            ((PocketBaseAuthenticationStateProvider)AuthenticationStateProvider).MarkUserAsLoggedOut();
            Snackbar.Add("Logged out successfully", Severity.Info);
            NavigationManager.NavigateTo("/admin/login");
        }
    }
}
