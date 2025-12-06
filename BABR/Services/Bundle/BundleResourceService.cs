using AssetsTools.NET;
using AssetsTools.NET.Extra;

namespace BABR.Services.Bundle;

public class BundleResourceService
{
    private readonly Dictionary<string, (long offset, long size)> _assetOffsets = [];
    private readonly List<byte> _newResourceData = [];
    private string? _cachedArchivePath;
    private long _existingResourceSize;

    public (string resourcePath, long offset, long size) AddAsset(string assetName, byte[] data,
        BundleFileInstance bundleInst)
    {
        if (_cachedArchivePath == null)
        {
            _cachedArchivePath = GetArchivePath(bundleInst);
            _existingResourceSize = GetExistingResourceSize(bundleInst);
        }

        var offset = _existingResourceSize + _newResourceData.Count;
        _newResourceData.AddRange(data);
        var size = data.Length;

        _assetOffsets[assetName] = (offset, size);

        return (_cachedArchivePath, offset, size);
    }

    private static string GetArchivePath(BundleFileInstance bundleInst)
    {
        var existingResource = bundleInst.file.BlockAndDirInfo.DirectoryInfos
            .FirstOrDefault(d => d.Name.EndsWith(".resource"));

        if (existingResource == null) return "archive:/CAB-unknown/CAB-unknown.resource";

        var existingName = existingResource.Name;
        var cabHash = existingName.Replace(".resource", "");
        return $"archive:/{cabHash}/{existingName}";
    }

    private static long GetExistingResourceSize(BundleFileInstance bundleInst)
    {
        var existingResource = bundleInst.file.BlockAndDirInfo.DirectoryInfos
            .FirstOrDefault(d => d.Name.EndsWith(".resource"));

        return existingResource?.DecompressedSize ?? 0;
    }

    public void WriteToBundle(BundleFileInstance bundleInst)
    {
        if (_newResourceData.Count == 0)
            return;

        var bundle = bundleInst.file;
        var existingResource = bundle.BlockAndDirInfo.DirectoryInfos
            .FirstOrDefault(d => d.Name.EndsWith(".resource"));

        if (existingResource == null)
            return;

        var existingData = ReadExistingResourceData(bundleInst, existingResource);
        var combinedData = new byte[existingData.Length + _newResourceData.Count];

        Array.Copy(existingData, 0, combinedData, 0, existingData.Length);
        _newResourceData.CopyTo(combinedData, existingData.Length);

        existingResource.Replacer = new ContentReplacerFromBuffer(combinedData);
        existingResource.DecompressedSize = combinedData.Length;
    }

    private static byte[] ReadExistingResourceData(BundleFileInstance bundleInst, AssetBundleDirectoryInfo dirInfo)
    {
        var bundle = bundleInst.file;
        var reader = bundle.DataReader;

        lock (reader)
        {
            reader.Position = dirInfo.Offset;
            return reader.ReadBytes((int)dirInfo.DecompressedSize);
        }
    }
}