using System.Collections.Frozen;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using BABR.Utilities;
using ZLinq;

namespace BABR.Services.Asset;

public static class AssetComponentLookupService
{
    public static Dictionary<string, long> BuildGameObjectToComponentMap(
        AssetsFileInstance assetsFileInstance,
        AssetsManager assetsManager,
        AssetClassID componentType,
        bool firstOnly = true)
    {
        var typeIdLookup = assetsFileInstance.file.AssetInfos
            .AsValueEnumerable()
            .ToFrozenDictionary(a => a.PathId, a => (AssetClassID)a.TypeId);

        var goToComponent = new Dictionary<string, long>();

        foreach (var goInfo in assetsFileInstance.file.AssetInfos.AsValueEnumerable()
                     .Where(a => a.TypeId == (int)AssetClassID.GameObject))
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

                var componentPathId = firstOnly
                    ? GetFirstComponentPathId(componentsField, typeIdLookup, componentType)
                    : GetComponentPathId(componentsField, typeIdLookup, componentType);

                if (componentPathId != 0)
                    goToComponent[goName] = componentPathId;
            }
            catch (Exception ex)
            {
                Logger.Trace($"Failed to process GameObject for {componentType}", ex.Message);
            }

        return goToComponent;
    }

    private static long GetFirstComponentPathId(
        AssetTypeValueField componentsField,
        FrozenDictionary<long, AssetClassID> typeIdLookup,
        AssetClassID targetType)
    {
        var firstComponent = componentsField.Children[0];
        var componentRef = firstComponent["component"];
        if (componentRef.IsDummy) return 0;

        var pathId = componentRef["m_PathID"].AsLong;
        if (pathId == 0) return 0;

        return typeIdLookup.TryGetValue(pathId, out var typeId) && typeId == targetType ? pathId : 0;
    }

    private static long GetComponentPathId(
        AssetTypeValueField componentsField,
        FrozenDictionary<long, AssetClassID> typeIdLookup,
        AssetClassID targetType)
    {
        var pathIds = componentsField.Children.AsValueEnumerable()
            .Select(c => c["component"])
            .Where(cr => !cr.IsDummy)
            .Select(cr => cr["m_PathID"].AsLong)
            .Where(id => id != 0);

        foreach (var pathId in pathIds)
            if (typeIdLookup.TryGetValue(pathId, out var typeId) && typeId == targetType)
                return pathId;

        return 0;
    }
}