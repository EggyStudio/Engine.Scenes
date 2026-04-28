using UniversalSceneDescription;

namespace Engine;

/// <summary>
/// Plugin that bootstraps the OpenUSD (UniversalSceneDescription) runtime so the rest of the
/// engine can author, load, and serialize <c>UsdStage</c>s through the Pixar bindings.
/// </summary>
/// <remarks>
/// <para>
/// <b>What it does:</b>
/// <list type="bullet">
///   <item><description>
///     Calls <see cref="UsdRuntime.Initialize"/> once during <see cref="IPlugin.Build"/>.
///     This configures the native loader and registers the bundled Pixar plugin tree
///     (<c>plugInfo.json</c> discovery, schema registration, file format plugins, etc.).
///     The call is idempotent and thread-safe, so registering the plugin multiple times
///     (or letting tests re-enter it) is safe.
///   </description></item>
///   <item><description>
///     Inserts a <see cref="UsdRuntimeHandle"/> marker resource into the <see cref="World"/> so
///     other systems can declare a dependency (<c>.Read&lt;UsdRuntimeHandle&gt;()</c>) and be
///     guaranteed the native runtime is live before they touch USD types.
///   </description></item>
/// </list>
/// </para>
/// <para>
/// This plugin intentionally does <b>not</b> open or own any <c>UsdStage</c>. Stage lifetime
/// is the responsibility of higher-level scene/asset systems (e.g. a future <c>UsdSceneLoader</c>
/// registered with the <see cref="AssetServer"/>).
/// </para>
/// </remarks>
/// <example>
/// Quick-start authoring a USD stage once the plugin is active:
/// <code>
/// using var stage = UsdStage.CreateNew("hello.usda");
/// UsdGeomXform.Define(stage, new SdfPath("/Hello"));
/// UsdGeomSphere.Define(stage, new SdfPath("/Hello/World"));
/// stage.Save();
/// </code>
/// </example>
/// <seealso cref="UsdRuntime"/>
/// <seealso cref="DefaultPlugins"/>
public sealed class ScenesPlugin : IPlugin
{
    private static readonly ILogger Logger = Log.Category("Engine.Scenes");

    /// <inheritdoc />
    public void Build(App app)
    {
        Logger.Info("ScenesPlugin: Initializing UniversalSceneDescription runtime...");

        try
        {
            // Idempotent and thread-safe per UniversalSceneDescription contract.
            UsdRuntime.Initialize();
        }
        catch (Exception ex)
        {
            Logger.Error($"ScenesPlugin: UsdRuntime.Initialize() failed: {ex.Message}");
            throw;
        }

        // Marker resource so other systems can express "depends on USD runtime" via
        // SystemDescriptor.Read<UsdRuntimeHandle>() and order/parallelize correctly.
        app.World.InsertResource(new UsdRuntimeHandle());

        Logger.Info("ScenesPlugin: USD runtime ready (Pixar plugin tree registered).");
    }
}

/// <summary>
/// Marker resource indicating that the OpenUSD native runtime has been initialized
/// by <see cref="ScenesPlugin"/>. Systems that touch USD types should declare a
/// <c>Read&lt;UsdRuntimeHandle&gt;()</c> dependency on their <see cref="SystemDescriptor"/>
/// to guarantee initialization order.
/// </summary>
public sealed class UsdRuntimeHandle
{
}

