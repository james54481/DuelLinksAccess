using System.Text.Json;

namespace DuelLinksAccess.Updater;

// Legacy JSON's Automatic field is intentionally ignored and never written.
public sealed record UpdaterPreferences(string GamePath)
{
    public static UpdaterPreferences Parse(string json) => JsonSerializer.Deserialize<UpdaterPreferences>(json)
        ?? new UpdaterPreferences("");
    public string ToJson() => JsonSerializer.Serialize(this);
}
