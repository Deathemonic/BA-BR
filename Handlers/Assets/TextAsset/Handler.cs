using BABU.Models.Context;

namespace BABU.Handlers.Assets.TextAsset;

public static class TextAssetHandler
{
    public static Task<int> ExportTextAssets(TextAssetExportContext context)
    {
        return Exporter.ExportTextAssets(context);
    }

    public static async Task<int> ImportTextAssets(ImportContext context)
    {
        return await Importer.ImportTextAssets(context);
    }
}