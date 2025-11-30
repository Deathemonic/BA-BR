using AssetsTools.NET;
using AssetsTools.NET.Extra;

namespace BABU.Services.Bundle;

public class BundleResourceService
{
    private const string CustomResourceName = "CAB-BABR.resource";
    private readonly List<byte> _resourceData = [];
    private readonly Dictionary<string, (long offset, long size)> _assetOffsets = [];

    public (string resourcePath, long offset, long size) AddAsset(string assetName, byte[] data)
    {
        var offset = _resourceData.Count;
        _resourceData.AddRange(data);
        var size = data.Length;

        _assetOffsets[assetName] = (offset, size);

        return (CustomResourceName, offset, size);
    }

    public void WriteToBundle(BundleFileInstance bundleInst)
    {
        if (_resourceData.Count == 0)
            return;

        var bundle = bundleInst.file;
        var resourceData = _resourceData.ToArray();

        var existingIndex = bundle.GetFileIndex(CustomResourceName);
        if (existingIndex != -1)
        {
            var existingDirInfo = bundle.BlockAndDirInfo.DirectoryInfos[existingIndex];
            existingDirInfo.Replacer = new ContentReplacerFromBuffer(resourceData);
        }
        else
        {
            var newDirInfo = new AssetBundleDirectoryInfo
            {
                Name = CustomResourceName,
                DecompressedSize = resourceData.Length,
                Flags = 0x04,
                Replacer = new ContentReplacerFromBuffer(resourceData)
            };

            bundle.BlockAndDirInfo.DirectoryInfos.Add(newDirInfo);
        }
    }
}
