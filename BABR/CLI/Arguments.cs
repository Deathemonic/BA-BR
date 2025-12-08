using AssetsTools.NET;
using AssetsTools.NET.Texture;
using BABR.Models.Types;
using BABR.Utilities;

namespace BABR.CLI;

public static class Arguments
{
    /// <summary>
    ///     Blue Archive - Bundle Updater
    /// </summary>
    /// <param name="include">Asset types to include (e.g., --include texture2d,audioclip).</param>
    /// <param name="exclude">Asset types to exclude (e.g., --exclude gameobject,transform).</param>
    /// <param name="only">Only allow these asset types to match (e.g., --only mesh,texture2d).</param>
    /// <param name="output">-o, Output directory for Dumps and Modded folders.</param>
    /// <param name="export">-e, Export assets only without importing.</param>
    /// <param name="compress">-c, Compression type for output bundle (None, LZMA, LZ4, LZ4Fast).</param>
    /// <param name="imageFormat">--image, Image format for texture export (Tga, Png, Bmp, Jpg).</param>
    /// <param name="textFormat">--text, Content format for text asset export (Txt or Bytes).</param>
    /// <param name="noCrc">-nc, Skip CRC matching on output bundles.</param>
    /// <param name="types">-t, List all available asset types.</param>
    /// <param name="verbose">-v, Enable verbose debug output.</param>
    /// <param name="modded">-m, Path to the modded asset bundle, a directory of assets, or a single asset file.</param>
    /// <param name="patch">-p, Path(s) to the patch assetbundle.</param>
    public static void Run(
        string[]? include = null,
        string[]? exclude = null,
        string[]? only = null,
        string? output = null,
        bool export = false,
        AssetBundleCompressionType compress = AssetBundleCompressionType.LZ4,
        ImageExportType imageFormat = ImageExportType.Tga,
        TextFormat textFormat = TextFormat.Txt,
        bool noCrc = false,
        bool types = false,
        bool verbose = false,
        string modded = "",
        params string[]? patch)
    {
        Logger.Initialize(verbose);

        if (types)
        {
            Logger.Info("Available asset types:");
            foreach (var type in TypeMapper.GetAllAssetTypes()) Console.WriteLine($"  {type}");
            return;
        }

        Parser.Execute(modded, patch, include, exclude, only, output, export, imageFormat, textFormat, compress, noCrc).Wait();
    }
}