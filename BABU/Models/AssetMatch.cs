using BABU.Utilities;

namespace BABU.Models;

public record AssetMatch(
    long ModdedId,
    long PatchId,
    string Name,
    string Type,
    int TypeId
)
{
    public string CleanName => FileManager.Clean(Name);
    public string JsonFileName => FileManager.CreateJsonName(Name, Type);
    public string DisplayName => $"{Name} ({Type})";
    public bool HasValidName => Name != "Unknown";
}