namespace Engine;

/// <summary>
/// Backend-agnostic scene plugin. Registers the in-engine scene model
/// (<see cref="Scene"/>, <see cref="SceneNode"/>, <see cref="SceneAsset"/>) and a
/// <see cref="SceneReaderRegistry"/> resource that concrete loaders
/// (e.g. <c>UsdSceneLoader</c> in <c>3DEngine.Scenes.Usd</c>) plug into.
/// </summary>
/// <remarks>
/// <para>
/// <b>Module split (matches the project pattern of e.g. <c>UI.WebView</c> + <c>UI.WebView.Vulkan</c>):</b>
/// </para>
/// <list type="bullet">
///   <item><description>
///     <c>3DEngine.Scenes</c> (this module) - format-agnostic scene model, asset wrapper,
///     reader/writer interfaces, and registry. No native dependencies, headless-safe.
///   </description></item>
///   <item><description>
///     <c>3DEngine.Scenes.Usd</c> - opt-in OpenUSD backend (<c>UsdSceneLoader</c>,
///     <c>UsdSceneReader</c>, <c>UsdSceneWriter</c>, <c>UsdScenesPlugin</c>) that initializes
///     the native runtime and registers itself with both the <see cref="AssetServer"/> and
///     the <see cref="SceneReaderRegistry"/>.
///   </description></item>
/// </list>
/// <para>
/// Multiple backends can coexist: each registers for its own file extensions
/// (<c>.usd/.usda/.usdc</c>, future <c>.gltf</c>, custom JSON, ...) and the registry
/// dispatches by extension. <see cref="AssetServer"/> auto-creates <see cref="Assets{T}"/>
/// on first load, so this plugin does not need to insert it explicitly.
/// </para>
/// </remarks>
/// <seealso cref="Scene"/>
/// <seealso cref="SceneAsset"/>
/// <seealso cref="ISceneReader"/>
/// <seealso cref="ISceneWriter"/>
public sealed class ScenesPlugin : IPlugin
{
    private static readonly ILogger Logger = Log.Category("Engine.Scenes");

    /// <inheritdoc />
    public void Build(App app)
    {
        Logger.Info("ScenesPlugin: Registering scene model (backend-agnostic)...");

        // Backend-agnostic registry. Backends call Register(...) from their own plugin
        // (e.g. UsdScenesPlugin) to opt-in their format support.
        app.World.InsertResource(new SceneReaderRegistry());

        Logger.Info("ScenesPlugin: Scene model ready. Add a backend plugin (e.g. UsdScenesPlugin) to enable file loading.");
    }
}

