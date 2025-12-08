namespace BABR.Models.Context;

public readonly record struct ExportResults(
    int ExportedCount,
    int TextureExportCount,
    int TextAssetExportCount,
    int AudioClipExportCount,
    int TransformExportCount,
    int SkinnedMeshRendererExportCount)
{
    public int TotalExported => ExportedCount + TextureExportCount + TextAssetExportCount + AudioClipExportCount + TransformExportCount + SkinnedMeshRendererExportCount;
}

public readonly record struct ImportResults(
    int ImportedCount,
    int ImportedTextureCount,
    int ImportedTextAssetCount,
    int ImportedAudioClipCount,
    int ImportedTransformCount,
    int ImportedSkinnedMeshRendererCount)
{
    public int TotalImported => ImportedCount + ImportedTextureCount + ImportedTextAssetCount + ImportedAudioClipCount + ImportedTransformCount + ImportedSkinnedMeshRendererCount;
}