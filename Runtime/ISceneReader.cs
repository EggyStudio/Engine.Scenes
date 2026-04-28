namespace Engine;

/// <summary>
/// Backend-agnostic reader interface that converts a stream / file of a particular format
/// into a normalized engine <see cref="Scene"/>. Implementations live in backend modules
/// (e.g. <c>UsdSceneReader</c> in <c>3DEngine.Scenes.Usd</c>).
/// </summary>
/// <remarks>
/// <para>
/// Readers are responsible for normalization at load time:
/// <list type="bullet">
///   <item><description>Coordinate axes - swap to engine canonical (right-handed, Y-up).</description></item>
///   <item><description>Units - scale to meters (USD <c>metersPerUnit</c>, glTF defaults to meters).</description></item>
///   <item><description>Hierarchy flattening / composition resolution where applicable.</description></item>
/// </list>
/// Per the project's threading model, <see cref="ReadAsync"/> is called from an
/// <see cref="AssetServer"/> background worker. Implementations must publish only immutable
/// data via the returned <see cref="Scene"/> - no live native handles in the snapshot.
/// </para>
/// </remarks>
public interface ISceneReader
{
    /// <summary>
    /// File extensions this reader handles, including the leading dot
    /// (e.g. <c>[".usd", ".usda", ".usdc"]</c>).
    /// </summary>
    string[] Extensions { get; }

    /// <summary>Identifier used by <see cref="SceneAsset.SourceFormat"/> (e.g. <c>"usd"</c>).</summary>
    string FormatId { get; }

    /// <summary>
    /// Reads a scene from <paramref name="context"/>. Called on a background thread.
    /// </summary>
    Task<Scene> ReadAsync(AssetLoadContext context, SceneImportSettings settings, CancellationToken ct);
}

/// <summary>
/// Backend-agnostic writer interface for serializing a <see cref="Scene"/> back to a source format.
/// Implementations live in backend modules (e.g. <c>UsdSceneWriter</c>).
/// </summary>
public interface ISceneWriter
{
    /// <summary>Identifier matching <see cref="SceneAsset.SourceFormat"/> (e.g. <c>"usd"</c>).</summary>
    string FormatId { get; }

    /// <summary>Writes <paramref name="scene"/> to <paramref name="targetPath"/> in this writer's format.</summary>
    Task WriteAsync(Scene scene, string targetPath, SceneExportSettings settings, CancellationToken ct);
}

/// <summary>
/// Settings forwarded to an <see cref="ISceneReader"/>. Defaults are the engine's canonical
/// conventions; callers can override per-load (e.g. force a unit scale for legacy assets).
/// </summary>
public sealed class SceneImportSettings
{
    /// <summary>Engine canonical coordinate system. Reader normalizes the source to this.</summary>
    public SceneCoordinateSystem TargetCoordinateSystem { get; init; } = SceneCoordinateSystem.YUp;

    /// <summary>Engine canonical scale (meters per unit). Reader normalizes the source to this.</summary>
    public double TargetMetersPerUnit { get; init; } = 1.0;

    /// <summary>
    /// When <c>true</c>, the reader resolves references / payloads / variant selections
    /// and produces a flattened snapshot. Always <c>true</c> for runtime; the editor may
    /// also keep a live native handle alongside (owned outside <see cref="SceneAsset"/>).
    /// </summary>
    public bool FlattenComposition { get; init; } = true;

    /// <summary>Reusable default settings.</summary>
    public static SceneImportSettings Default { get; } = new();
}

/// <summary>Settings forwarded to an <see cref="ISceneWriter"/>.</summary>
public sealed class SceneExportSettings
{
    /// <summary>Format-native coordinate system the writer should emit (defaults to engine canonical).</summary>
    public SceneCoordinateSystem CoordinateSystem { get; init; } = SceneCoordinateSystem.YUp;

    /// <summary>Format-native unit scale the writer should emit.</summary>
    public double MetersPerUnit { get; init; } = 1.0;

    /// <summary>Reusable default settings.</summary>
    public static SceneExportSettings Default { get; } = new();
}

/// <summary>
/// Resource that holds the set of <see cref="ISceneReader"/> / <see cref="ISceneWriter"/>
/// implementations registered by backend plugins (USD, future glTF, etc.). Used by spawn
/// systems and the editor to dispatch by format / extension without hard-referencing a
/// specific backend.
/// </summary>
/// <remarks>
/// Inserted into the <see cref="World"/> by <see cref="ScenesPlugin"/>. Backend plugins
/// (<c>UsdScenesPlugin</c>, ...) call <see cref="RegisterReader"/> / <see cref="RegisterWriter"/>
/// during <see cref="IPlugin.Build"/>.
/// </remarks>
public sealed class SceneReaderRegistry
{
    private readonly Dictionary<string, ISceneReader> _readersByExtension = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ISceneReader> _readersByFormat = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ISceneWriter> _writersByFormat = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Registers a reader for all of its declared extensions and its format id.</summary>
    public void RegisterReader(ISceneReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        _readersByFormat[reader.FormatId] = reader;
        foreach (var ext in reader.Extensions)
            _readersByExtension[ext] = reader;
    }

    /// <summary>Registers a writer for its format id.</summary>
    public void RegisterWriter(ISceneWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        _writersByFormat[writer.FormatId] = writer;
    }

    /// <summary>Looks up a reader by file extension (e.g. <c>".usda"</c>).</summary>
    public ISceneReader? FindReaderByExtension(string extension)
        => _readersByExtension.TryGetValue(extension, out var r) ? r : null;

    /// <summary>Looks up a reader by format id (e.g. <c>"usd"</c>).</summary>
    public ISceneReader? FindReaderByFormat(string formatId)
        => _readersByFormat.TryGetValue(formatId, out var r) ? r : null;

    /// <summary>Looks up a writer by format id (e.g. <c>"usd"</c>).</summary>
    public ISceneWriter? FindWriterByFormat(string formatId)
        => _writersByFormat.TryGetValue(formatId, out var w) ? w : null;
}

