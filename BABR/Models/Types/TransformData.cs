namespace BABR.Models.Types;

public readonly record struct TransformData(
    Vector4Data m_LocalRotation,
    Vector3Data m_LocalPosition,
    Vector3Data m_LocalScale);

public readonly record struct Vector4Data(float x, float y, float z, float w);

public readonly record struct Vector3Data(float x, float y, float z);
