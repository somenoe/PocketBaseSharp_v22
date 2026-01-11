using PocketBaseSharp.Models;
using PocketBaseSharp.Models.Auth;
using PocketBaseSharp.Services.Base;

namespace PocketBaseSharp.Services
{
    /// <summary>
    /// Service for administrating PocketBase superusers (admins).
    /// Handles authentication with the _superusers collection endpoint.
    /// </summary>
    public class AdminService : BaseAuthService<RecordAuthModel<AdminModel>>
    {
        protected override string BasePath(string? url = null) => "/api/collections/_superusers";

        public AdminService(PocketBase client) : base(client)
        {
        }
    }
}
