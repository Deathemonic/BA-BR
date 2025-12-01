namespace BABR.Models.Context;

public readonly record struct ExportResults(int ExportedCount, int TextureExportCount, int TextAssetExportCount, int AudioClipExportCount)
{
    public int TotalExported => ExportedCount + TextureExportCount + TextAssetExportCount + AudioClipExportCount;
}

public readonly record struct ImportResults(int ImportedCount, int ImportedTextureCount, int ImportedTextAssetCount, int ImportedAudioClipCount)
{
    public int TotalImported => ImportedCount + ImportedTextureCount + ImportedTextAssetCount + ImportedAudioClipCount;
}