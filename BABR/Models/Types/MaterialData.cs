namespace BABR.Models.Types;

public readonly record struct MaterialData(
    uint m_LightmapFlags,
    bool m_EnableInstancingVariants,
    bool m_DoubleSidedGI,
    int m_CustomRenderQueue,
    MaterialSavedProperties m_SavedProperties);

public readonly record struct MaterialSavedProperties(
    MaterialTexEnv[] m_TexEnvs,
    MaterialNamedFloat[] m_Floats,
    MaterialNamedInt[] m_Ints,
    MaterialNamedColor[] m_Colors);

public readonly record struct MaterialTexEnv(
    string first,
    MaterialVector2 m_Scale,
    MaterialVector2 m_Offset);

public readonly record struct MaterialVector2(float x, float y);

public readonly record struct MaterialNamedFloat(string first, float second);

public readonly record struct MaterialNamedInt(string first, int second);

public readonly record struct MaterialNamedColor(string first, MaterialColor second);

public readonly record struct MaterialColor(float r, float g, float b, float a);
