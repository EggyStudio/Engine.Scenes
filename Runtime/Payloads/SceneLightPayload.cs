using System.Numerics;

namespace Engine;

/// <summary>
/// Backend-agnostic light payload covering the <c>UsdLux</c> shape vocabulary
/// (distant / sphere / rect / disk / cylinder / dome). Carried on a <see cref="SceneNode"/>
/// for now as an inert payload until a runtime <c>Light</c> ECS component lands and the
/// renderer grows light support; spawn systems may attach the payload directly so future
/// systems can pick it up without re-reading the source.
/// </summary>
/// <remarks>
/// <para>
/// Field semantics follow the <c>UsdLuxLightAPI</c> base inputs (<c>color</c>,
/// <c>intensity</c>, <c>exposure</c>) plus the per-shape inputs (e.g.
/// <c>UsdLuxSphereLight.radius</c>, <c>UsdLuxRectLight.width</c>/<c>height</c>).
/// All shape parameters are nullable so the payload type itself is shape-agnostic and
/// only the fields relevant to <see cref="Type"/> are populated by the reader.
/// </para>
/// <para>
/// <b>Energy units:</b> the effective radiant intensity is <c>color * intensity * 2^exposure</c>,
/// matching the USD luminance convention. Renderer-side normalization (e.g. converting to
/// nits / lumens) is the consumer's responsibility.
/// </para>
/// </remarks>
public sealed class SceneLightPayload
{
    /// <summary>Display name (typically the source light prim's leaf name).</summary>
    public string Name { get; init; } = "Light";

    /// <summary>Which UsdLux shape this light represents.</summary>
    public required SceneLightType Type { get; init; }

    /// <summary>Linear-RGB color (multiplied with <see cref="Intensity"/> and 2^<see cref="Exposure"/>).</summary>
    public Vector3 Color { get; init; } = Vector3.One;

    /// <summary>Scalar intensity multiplier (<c>UsdLuxLightAPI.intensity</c>, default 1.0).</summary>
    public float Intensity { get; init; } = 1f;

    /// <summary>Exposure stops applied as a power of two (<c>UsdLuxLightAPI.exposure</c>, default 0).</summary>
    public float Exposure { get; init; }

    /// <summary>Sphere / disk radius in stage units (<see cref="SceneLightType.Sphere"/>, <see cref="SceneLightType.Disk"/>).</summary>
    public float? Radius { get; init; }

    /// <summary>Rect / dome width in stage units (<see cref="SceneLightType.Rect"/>).</summary>
    public float? Width { get; init; }

    /// <summary>Rect / dome height in stage units (<see cref="SceneLightType.Rect"/>).</summary>
    public float? Height { get; init; }

    /// <summary>Cylinder length in stage units (<see cref="SceneLightType.Cylinder"/>).</summary>
    public float? Length { get; init; }

    /// <summary>Shaping cone angle in degrees (<c>inputs:shaping:cone:angle</c>), or <c>null</c> if no cone shaping.</summary>
    public float? ConeAngle { get; init; }

    /// <summary>Shaping cone softness 0..1 (<c>inputs:shaping:cone:softness</c>), or <c>null</c>.</summary>
    public float? ConeSoftness { get; init; }

    /// <summary>Path to an IES profile file for shaping (<c>inputs:shaping:ies:file</c>), or <c>null</c>.</summary>
    public string? IesProfilePath { get; init; }

    /// <summary>Texture map for dome lights (<c>UsdLuxDomeLight.texture:file</c>), or <c>null</c>.</summary>
    public string? DomeTexturePath { get; init; }
}

/// <summary>UsdLux shape vocabulary.</summary>
public enum SceneLightType
{
    /// <summary><c>UsdLuxDistantLight</c>: parallel rays from infinity (sun-style).</summary>
    Distant,

    /// <summary><c>UsdLuxSphereLight</c>: omnidirectional spherical area light.</summary>
    Sphere,

    /// <summary><c>UsdLuxRectLight</c>: planar rectangular area light.</summary>
    Rect,

    /// <summary><c>UsdLuxDiskLight</c>: planar circular area light.</summary>
    Disk,

    /// <summary><c>UsdLuxCylinderLight</c>: capped cylindrical area light.</summary>
    Cylinder,

    /// <summary><c>UsdLuxDomeLight</c>: image-based environment light at infinity.</summary>
    Dome,
}