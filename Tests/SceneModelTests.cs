using FluentAssertions;
using Xunit;

namespace Engine.Tests.Scenes;

/// <summary>
/// Tests for the backend-agnostic scene model in <c>3DEngine.Scenes</c>:
/// <see cref="Scene"/>, <see cref="SceneNode"/>, <see cref="SceneAsset"/>, and the
/// <see cref="SceneReaderRegistry"/> dispatch.
/// </summary>
[Trait("Category", "Unit")]
public class SceneModelTests
{
    [Fact]
    public void SceneNode_Defaults_Are_Identity_Transform()
    {
        var node = new SceneNode();

        node.Enabled.Should().BeTrue();
        node.Children.Should().BeEmpty();
        node.Components.Should().BeEmpty();
        node.LocalTransform.Rotation.Should().Be(System.Numerics.Quaternion.Identity);
        node.LocalTransform.Scale.Should().Be(System.Numerics.Vector3.One);
    }

    [Fact]
    public void Scene_Traverse_Visits_Hierarchy_DepthFirst()
    {
        var scene = new Scene { Name = "T" };
        var root = new SceneNode { Name = "root" };
        var a = new SceneNode { Name = "a" };
        var b = new SceneNode { Name = "b" };
        var aa = new SceneNode { Name = "aa" };
        a.Children.Add(aa);
        root.Children.Add(a);
        root.Children.Add(b);
        scene.Roots.Add(root);

        var names = scene.Traverse().Select(n => n.Name).ToArray();

        names.Should().Equal("root", "a", "aa", "b");
    }

    [Fact]
    public void SceneNode_GetComponent_Returns_First_Match()
    {
        var node = new SceneNode();
        var marker = new TagComponent("hi");
        node.Components.Add(marker);
        node.Components.Add(new TagComponent("ignored"));

        node.GetComponent<TagComponent>().Should().BeSameAs(marker);
        node.GetComponent<OtherComponent>().Should().BeNull();
    }

    [Fact]
    public void SceneAsset_Carries_Provenance()
    {
        var asset = new SceneAsset
        {
            Scene = new Scene { Name = "s" },
            SourcePath = "scenes/s.usda",
            SourceFormat = "usd",
        };

        asset.Scene.Name.Should().Be("s");
        asset.SourcePath.Should().Be("scenes/s.usda");
        asset.SourceFormat.Should().Be("usd");
    }

    [Fact]
    public void SceneReaderRegistry_Dispatches_By_Extension_And_Format()
    {
        var reg = new SceneReaderRegistry();
        var reader = new FakeSceneReader();
        var writer = new FakeSceneWriter();

        reg.RegisterReader(reader);
        reg.RegisterWriter(writer);

        reg.FindReaderByExtension(".fake").Should().BeSameAs(reader);
        reg.FindReaderByExtension(".FAKE").Should().BeSameAs(reader, "extension lookup is case-insensitive");
        reg.FindReaderByFormat("fake").Should().BeSameAs(reader);
        reg.FindWriterByFormat("fake").Should().BeSameAs(writer);
        reg.FindReaderByExtension(".missing").Should().BeNull();
        reg.FindReaderByFormat("missing").Should().BeNull();
    }

    [Fact]
    public void ScenesPlugin_Inserts_SceneReaderRegistry_Resource()
    {
        using var app = new App();
        app.AddPlugin(new ScenesPlugin());

        app.World.ContainsResource<SceneReaderRegistry>().Should().BeTrue();
    }

    private sealed record TagComponent(string Value);
    private sealed record OtherComponent;

    private sealed class FakeSceneReader : ISceneReader
    {
        public string[] Extensions => [".fake"];
        public string FormatId => "fake";
        public Task<Scene> ReadAsync(AssetLoadContext ctx, SceneImportSettings s, CancellationToken ct)
            => Task.FromResult(new Scene { Name = "fake" });
    }

    private sealed class FakeSceneWriter : ISceneWriter
    {
        public string FormatId => "fake";
        public Task WriteAsync(Scene scene, string targetPath, SceneExportSettings s, CancellationToken ct)
            => Task.CompletedTask;
    }
}

