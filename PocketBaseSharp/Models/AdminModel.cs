namespace PocketBaseSharp.Models
{
    /// <summary>
    /// Represents a PocketBase superuser (admin) model.
    /// Inherits authentication properties from BaseAuthModel (email, emailVisibility, verified).
    /// </summary>
    public class AdminModel : BaseAuthModel
    {
        // Email, EmailVisibility, Verified are inherited from BaseAuthModel
        // Avatar and Username are not present in the _superusers collection
    }
}
