using System.Collections.Frozen;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using BABR.Utilities;
using ZLinq;

namespace BABR.Handlers.Transforms;

public static class TransformLookup
{
    private const int GameObjectTypeId = 1;
    private const int TransformTypeId = 4;

    public static Dictionary<string, long> BuildGameObjectToTransformMap(
        AssetsFileInstance assetsFileInstance,
        AssetsManager assetsManager)
    {
        var typeIdLookup = assetsFileInstance.file.AssetInfos
            .AsValueEnumerable()
            .ToFrozenDictionary(a => a.PathId, a => a.TypeId);

        var goToTransform = new Dictionary<string, long>();

        foreach (var goInfo in assetsFileInstance.file.AssetInfos.AsValueEnumerable()
                     .Where(a => a.TypeId == GameObjectTypeId))
        {
            try
            {
                var baseField = assetsManager.GetBaseField(assetsFileInstance, goInfo);
                if (baseField == null) continue;

                var nameField = baseField["m_Name"];
                if (nameField.IsDummy) continue;

                var goName = nameField.AsString;
                if (string.IsNullOrEmpty(goName)) continue;

                var componentsField = baseField["m_Component"]["Array"];
                if (componentsField.IsDummy || componentsField.Children.Count == 0) continue;

                var transformPathId = GetFirstTransformPathId(componentsField, typeIdLookup);
                if (transformPathId != 0)
                    goToTransform[goName] = transformPathId;
            }
            catch (Exception ex)
            {
                Logger.Trace("Failed to process GameObject", ex.Message);
            }
        }

        return goToTransform;
    }

    private static long GetFirstTransformPathId(AssetTypeValueField componentsField,
        FrozenDictionary<long, int> typeIdLookup)
    {
        var firstComponent = componentsField.Children[0];
        var componentRef = firstComponent["component"];
        if (componentRef.IsDummy) return 0;

        var pathId = componentRef["m_PathID"].AsLong;
        if (pathId == 0) return 0;

        return typeIdLookup.TryGetValue(pathId, out var typeId) && typeId == TransformTypeId ? pathId : 0;
    }

    public static Dictionary<long, string> BuildTransformToGameObjectMap(
        AssetsFileInstance assetsFileInstance,
        AssetsManager assetsManager)
    {
        var goToTransform = BuildGameObjectToTransformMap(assetsFileInstance, assetsManager);
        return goToTransform.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
    }
}
