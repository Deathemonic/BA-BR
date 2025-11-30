using AssetsTools.NET;
using AssetsTools.NET.Texture;
using BABU.Models.Types;
using BABU.Utilities;

namespace BABU.CLI;

public static class Arguments
{
    /// <summary>
    ///     Blue Archive - Bundle Updater
    /// </summary>
    /// <param name="modded">-m, Path to the modded asset bundle, a directory of assets, or a single asset file.</param>
    /// <param name="patch">-p, Path to the patch assetbundle.</param>
    /// <param name="include">Asset types to include (e.g., --include texture2d audioclip).</param>
    /// <param name="exclude">Asset types to exclude (e.g., --exclude gameobject transform).</param>
    /// <param name="only">Only allow these asset types to match (e.g., --only mesh texture2d).</param>
    /// <param name="output">-o, Output directory for Dumps and Modded folders.</param>
    /// <param name="export">-e, Export assets only without importing.</param>
    /// <param name="imageFormat">--image, Image format for texture export (Tga, Png, Bmp, Jpg).</param>
    /// <param name="textFormat">--text, Content format for text asset export (Txt or Bytes).</param>
    /// <param name="compress">-c, Compression type for output bundle (None, LZMA, LZ4, LZ4Fast).</param>
    /// <param name="verbose">-v, Enable verbose debug output.</param>
    /// <param name="types">-t, List all available asset types.</param>
    public static void Run(
        string modded = "",
        string patch = "",
        string[]? include = null,
        string[]? exclude = null,
        string[]? only = null,
        string? output = null,
        bool export = false,
        bool verbose = false,
        ImageExportType imageFormat = ImageExportType.Tga,
        TextFormat textFormat = TextFormat.Txt,
        AssetBundleCompressionType compress = AssetBundleCompressionType.LZ4,
        bool types = false)
    {
        if (types)
        {
            Logger.Info("Available asset types:");
            foreach (var type in TypeMapper.GetAllAssetTypes()) Console.WriteLine($"  {type}");
            return;
        }

        Logger.SetVerbose(verbose);
        Parser.Execute(modded, patch, include, exclude, only, output, export, imageFormat, textFormat, compress).Wait();
    }
}