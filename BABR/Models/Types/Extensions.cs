using System.Collections.Frozen;

namespace BABR.Models.Types;

public static class Extensions
{
    public static readonly FrozenSet<string> TextureExtensions =
        FrozenSet.ToFrozenSet([".png", ".tga", ".bmp", ".jpg"]);

    public static readonly FrozenSet<string> TextAssetExtensions = FrozenSet.ToFrozenSet([".txt", ".bytes"]);

    public static readonly FrozenSet<string> AudioExtensions =
        FrozenSet.ToFrozenSet([".wav", ".ogg", ".mp3", ".flac", ".aiff", ".m4a"]);
}