using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using PocketBaseSharp;

namespace Example.Pages.Admin.Components
{
    public partial class AdminNotLoggedIn
    {
        [Inject]
        public NavigationManager NavigationManager { get; set; } = null!;

        [Inject]
        public PocketBase PocketBase { get; set; } = null!;

        [Inject]
        public ISnackbar Snackbar { get; set; } = null!;

        [Inject]
        public AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;

        public string? Email { get; set; }
        public string? Password { get; set; }

        protected async Task AdminLoginAsync()
        {
            var valid = CheckInputs();
            if (valid)
            {
                try
                {
                    var result = await PocketBase.Admin.AuthenticateWithPasswordAsync(Email!, Password!);
                    if (result.IsSuccess)
                    {
                        Snackbar.Add("Admin authenticated!", Severity.Success);
                        var claims = PocketBaseAuthenticationStateProvider.ParseClaimsFromJwt(result.Value.Token);
                        ((PocketBaseAuthenticationStateProvider)AuthenticationStateProvider).MarkUserAsAuthenticated(claims);
                        NavigationManager.NavigateTo("/admin/dashboard");
                    }
                    else
                    {
                        Snackbar.Add("Admin authentication failed", Severity.Error);
                    }
                }
                catch
                {
                    Snackbar.Add("Admin login failed, please check your credentials", Severity.Error);
                }
            }
        }

        private bool CheckInputs()
        {
            var emailEmpty = string.IsNullOrWhiteSpace(Email);
            var passwordEmpty = string.IsNullOrWhiteSpace(Password);

            if (emailEmpty || passwordEmpty)
            {
                Snackbar.Add("The Email and Password fields are required.", Severity.Warning);
                return false;
            }
            return true;
        }
    }
}
