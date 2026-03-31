using System.Text.Json.Serialization;
using PocketBaseSharp.Models;

namespace PocketBaseSharp.FlowbiteDemo.Models;

public sealed class EntryRecord : BaseModel
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("is_done")]
    public bool? IsDone { get; set; }

    [JsonPropertyName("Todo_Id")]
    public List<string>? TodoIds { get; set; }

    [JsonIgnore]
    public string? TodoId
    {
        get => TodoIds is { Count: > 0 } todoIds ? todoIds[0] : null;
        set => TodoIds = string.IsNullOrWhiteSpace(value) ? null : [value];
    }
}
