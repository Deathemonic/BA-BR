namespace BABR.Models.Types;

public readonly record struct SkinnedMeshRendererData(
    bool m_Enabled,
    byte m_CastShadows,
    byte m_ReceiveShadows,
    byte m_DynamicOccludee,
    byte m_StaticShadowCaster,
    byte m_MotionVectors,
    byte m_LightProbeUsage,
    byte m_ReflectionProbeUsage,
    byte m_RayTracingMode,
    byte m_RayTraceProcedural,
    uint m_RenderingLayerMask,
    int m_RendererPriority,
    int m_SortingLayerID,
    short m_SortingLayer,
    short m_SortingOrder,
    int m_Quality,
    bool m_UpdateWhenOffscreen,
    bool m_SkinnedMotionVectors,
    float[] m_BlendShapeWeights,
    AABB m_AABB,
    bool m_DirtyAABB);

public readonly record struct AABB(Vector3Data m_Center, Vector3Data m_Extent);
