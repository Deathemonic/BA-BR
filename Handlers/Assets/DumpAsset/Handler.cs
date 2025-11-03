using BABU.Handlers.Bundle;
using BABU.Models;
using BABU.Models.Context;

namespace BABU.Handlers.Assets.DumpAsset;

public static class GenericAssetHandler
{
    public static async Task<int> ExportAssets(ExportContext context)
    {
        return await Exporter.ExportAssets(context);
    }

    public static async Task<int> ImportAssets(ImportContext context)
    {
        return await Importer.ImportAssets(context);
    }
}

