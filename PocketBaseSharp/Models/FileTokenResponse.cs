using System.Text.Json.Serialization;

namespace PocketBaseSharp.Models
{
    public class FileTokenResponse
    {
        [JsonPropertyName("token")]
        public string? Token { get; set; }
    }
}
