using BABU.Models.Context;

namespace BABU.Handlers.Assets.Texture2D;

public static class Texture2DHandler
{
    public static Task<int> ExportTextures(Texture2DExportContext context)
    {
        return Exporter.ExportTextures(context);
    }

    public static async Task<int> ImportTextures(ImportContext context)
    {
        return await Importer.ImportTextures(context);
    }
}