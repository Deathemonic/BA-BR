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
        var goToTransform = new Dictionary<string, long>();

        var gameObjects = assetsFileInstance.file.AssetInfos
            .AsValueEnumerable()
            .Where(a => a.TypeId == GameObjectTypeId);

        foreach (var goInfo in gameObjects)
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

                // First component is typically the Transform
                var firstComponent = componentsField.Children[0];
                var componentRef = firstComponent["component"];
                if (componentRef.IsDummy) continue;

                var transformPathId = componentRef["m_PathID"].AsLong;
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

    public static Dictionary<long, string> BuildTransformToGameObjectMap(
        AssetsFileInstance assetsFileInstance,
        AssetsManager assetsManager)
    {
        var goToTransform = BuildGameObjectToTransformMap(assetsFileInstance, assetsManager);
        return goToTransform.AsValueEnumerable().ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
    }

    public static string? GetGameObjectNameForTransform(
        long transformPathId,
        AssetsFileInstance assetsFileInstance,
        AssetsManager assetsManager)
    {
        var transformToGo = BuildTransformToGameObjectMap(assetsFileInstance, assetsManager);
        return transformToGo.GetValueOrDefault(transformPathId);
    }

    public static List<(long PathId, string GameObjectName)> GetAllTransformsWithNames(
        AssetsFileInstance assetsFileInstance,
        AssetsManager assetsManager)
    {
        var transformToGo = BuildTransformToGameObjectMap(assetsFileInstance, assetsManager);

        return assetsFileInstance.file.AssetInfos
            .AsValueEnumerable()
            .Where(a => a.TypeId == TransformTypeId && transformToGo.ContainsKey(a.PathId))
            .Select(a => (a.PathId, transformToGo[a.PathId]))
            .ToList();
    }
}
