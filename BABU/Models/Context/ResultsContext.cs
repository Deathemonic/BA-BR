namespace BABU.Models.Context;

public readonly record struct ExportResults(int ExportedCount, int TextureExportCount, int TextAssetExportCount)
{
    public int TotalExported => ExportedCount + TextureExportCount + TextAssetExportCount;
}

public readonly record struct ImportResults(int ImportedCount, int ImportedTextureCount, int ImportedTextAssetCount)
{
    public int TotalImported => ImportedCount + ImportedTextureCount + ImportedTextAssetCount;
}