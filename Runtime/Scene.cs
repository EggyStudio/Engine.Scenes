using System.Numerics;

namespace Engine;

/// <summary>
/// Backend-agnostic, in-memory representation of a scene. Produced by an <see cref="ISceneReader"/>
/// (e.g. <c>UsdSceneReader</c>) and consumed by spawn systems that translate it into ECS entities
/// via <c>EcsCommands</c>.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="Scene"/> is an <b>immutable snapshot</b> by convention: once a reader returns one,
/// the spawning side may iterate it freely from any thread. Mutating editor sessions should
/// re-author the source (e.g. live <c>UsdStage</c>) and re-emit a new snapshot, instead of
/// mutating <see cref="Scene"/> in place. This keeps the cross-thread contract simple and
/// matches how the <see cref="AssetServer"/> publishes results from background loaders.
/// </para>
/// <para>
/// All transforms are normalized at load time to the engine's canonical convention:
/// <list type="bullet">
///   <item><description><b>Coordinate system:</b> right-handed, <b>Y-up</b>.</description></item>
///   <item><description><b>Units:</b> meters (1 engine unit = 1 meter).</description></item>
/// </list>
/// Source-format-specific axes (Z-up USD stages, etc.) and units (<c>metersPerUnit</c>) are
/// converted by the reader; downstream code never has to think about them.
/// </para>
/// </remarks>
/// <seealso cref="SceneNode"/>
/// <seealso cref="SceneAsset"/>
/// <seealso cref="ISceneReader"/>
public sealed class Scene
{
    /// <summary>Logical name (often the source file stem). Diagnostic only.</summary>
    public string Name { get; init; } = "Scene";

    /// <summary>Top-level nodes. Each node owns its own children recursively via <see cref="SceneNode.Children"/>.</summary>
    public List<SceneNode> Roots { get; } = new();

    /// <summary>
    /// Coordinate system the scene was authored in, for diagnostics / round-tripping.
    /// All transforms in <see cref="Roots"/> are already normalized to engine canonical (Y-up, meters).
    /// </summary>
    public SceneCoordinateSystem SourceCoordinateSystem { get; init; } = SceneCoordinateSystem.YUp;

    /// <summary>Source <c>metersPerUnit</c> (or <c>1.0</c> if unknown). Diagnostic only.</summary>
    public double SourceMetersPerUnit { get; init; } = 1.0;

    /// <summary>Depth-first enumeration of every node in the scene.</summary>
    public IEnumerable<SceneNode> Traverse()
    {
        foreach (var root in Roots)
        {
            foreach (var n in TraverseRecursive(root))
                yield return n;
        }
    }

    private static IEnumerable<SceneNode> TraverseRecursive(SceneNode node)
    {
        yield return node;
        foreach (var c in node.Children)
            foreach (var n in TraverseRecursive(c))
                yield return n;
    }
}

/// <summary>Coordinate system convention for a source scene (engine canonical is Y-up).</summary>
public enum SceneCoordinateSystem
{
    /// <summary>Right-handed, Y-up (engine canonical).</summary>
    YUp,
    /// <summary>Right-handed, Z-up (common for USD, Blender, Unreal).</summary>
    ZUp,
}

