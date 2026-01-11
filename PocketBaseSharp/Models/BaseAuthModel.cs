using System.Text.Json.Serialization;

namespace PocketBaseSharp.Models
{
    public class BaseAuthModel : BaseModel, IBaseAuthModel
    {
        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("emailVisibility")]
        public bool? EmailVisibility { get; set; }

        [JsonPropertyName("username")]
        public string? UserName { get; set; }
        [JsonPropertyName("verified")]

        public bool? Verified { get; set; }
    }
}
