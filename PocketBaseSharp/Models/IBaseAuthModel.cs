namespace PocketBaseSharp.Models
{
    public interface IBaseAuthModel : IBaseModel
    {
        string? Email { get; }

        bool? EmailVisibility { get; }

        string? UserName { get; }

        bool? Verified { get; }
    }
}
