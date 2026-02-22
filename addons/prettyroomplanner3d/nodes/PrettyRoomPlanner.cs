using System.Linq;
using Godot;
using Godot.Collections;

namespace PrettyRoomGen3D;

// TODO ADD DOCUMENTATION FOR EVERY CLASS

[GlobalClass]
[Tool]
public partial class PrettyRoomPlanner : Node3D
{
    public const string SPAWN_CONTAINER_NAME = "SpawnContainer (Generated)";
    public const string METADATA_PLANNER_NODEPATH = "pplanner3d_nodepath";

    public const string METADATA_PLANNER_ROOMRESOURCE_CATEGORY = "pplanner_roomresource_category";

    [ExportGroup("General")]
    [Export]
    public Vector3 Size
    {
        get => size;
        set => UpdateSizeInternal(value);
    }

    [Export]
    public bool GenerateOnReady { get; set; } = false;

    [Export]
    public ulong Seed { get; set; } = 0;

    [Export]
    public bool RandomizeSeed { get; set; } = false;

    [Export]
    public Dictionary<string, PrettyRoomResource> RoomResourceLibrary { get; set; } = new();

    [ExportGroup("Generation")]
    [ExportToolButton("Generate!")]
    Callable GenerateButton => Callable.From(Generate);

    [ExportToolButton("Clear")]
    Callable ClearGenerationButton => Callable.From(FreeGeneration);

    [ExportGroup("Debugging")]
    [Export]
    Dictionary<string, Array<NodePath>> SceneInstanceDictionary { get; set; } = new();

    Vector3 size = Vector3.One * 2f;
    Draw3DMeshInstance debugDrawer;
    public readonly RandomNumberGenerator NumberGenerator = new RandomNumberGenerator();

    public override void _Ready()
    {
        if (Engine.IsEditorHint())
            return;

        if (GenerateOnReady)
            Generate();
    }

    public override void _Process(double delta)
    {
        if (!Engine.IsEditorHint())
            return;
        DrawDebugInEditor();
    }

    public override string[] _GetConfigurationWarnings() => Validate();

    public void Generate()
    {
        FreeGeneration();
        if (RandomizeSeed)
        {
            Seed = NumberGenerator.Randi();
        }

        NumberGenerator.Seed = Seed;

        if (SceneInstanceDictionary == null)
            SceneInstanceDictionary = new();

        string[] messages = Validate();
        if (messages.Length > 0)
        {
            GD.PushWarning(messages);
            return;
        }

        GenerationPass();
        PostGenerationPass();

        if (Engine.IsEditorHint())
        {
            EditorInterface.Singleton.MarkSceneAsUnsaved();
        }
    }

    public void FreeGeneration()
    {
        if (SceneInstanceDictionary == null)
            SceneInstanceDictionary = new();

        SceneInstanceDictionary.Clear();
        var spawnContainer = GetOrCreateSpawnContainer();
        if (spawnContainer != null)
        {
            spawnContainer.Free();
        }

        if (Engine.IsEditorHint())
        {
            EditorInterface.Singleton.MarkSceneAsUnsaved();
        }
    }

    public Node3D[] GetRoomResourceInstancesByCategory(string category)
    {
        if (SceneInstanceDictionary.ContainsKey(category))
        {
            return SceneInstanceDictionary[category]
                .Select(i => (Node3D)GetNode(i))
                .Where(i => !i.IsQueuedForDeletion())
                .ToArray();
        }

        return [];
    }

    public bool TryQueueFreeRoomResourceInstance(string category, Node instance)
    {
        if (instance == null)
            return false;

        if (SceneInstanceDictionary.ContainsKey(category))
        {
            NodePath path = GetPathTo(instance);
            Node roomResourceInstance = GetNodeOrNull(
                SceneInstanceDictionary[category].FirstOrDefault(np => np == path)
            );

            if (roomResourceInstance == instance)
            {
                SceneInstanceDictionary[category].Remove(path);
                instance.QueueFree();
                return true;
            }
        }

        return false;
    }

    public void AddSceneInstance(string category, Node instance)
    {
        instance.SetMeta(METADATA_PLANNER_NODEPATH, GetPath());
        instance.SetMeta(METADATA_PLANNER_ROOMRESOURCE_CATEGORY, category);

        if (!instance.IsInsideTree())
        {
            // As stated in AddChild, maintaining readable names could influcence generation performance
            // Though, it is not heavy enough to bother... Can't be that slow can it? :)
            GetOrCreateSpawnContainerCategoryNode(category).AddChild(instance, true);
            instance.Owner = this;
        }

        if (!SceneInstanceDictionary.ContainsKey(category))
        {
            SceneInstanceDictionary.Add(category, [GetPathTo(instance)]);
            return;
        }

        var instances = SceneInstanceDictionary[category];
        if (instances.Contains(GetPathTo(instance)))
            return;

        SceneInstanceDictionary[category].Add(GetPathTo(instance));
    }

    private void GenerationPass()
    {
        foreach (var child in GetChildren())
        {
            if (child is PrettyPlannerRule plannerRule)
                plannerRule.Execute(this);
        }
    }

    private void PostGenerationPass()
    {
        foreach (var child in GetChildren())
        {
            if (child is PrettyPlannerRule plannerRule)
                plannerRule.PostExecute(this);
        }
    }

    private Node3D GetOrCreateSpawnContainer()
    {
        var container = GetNodeOrNull(SPAWN_CONTAINER_NAME);
        if (container == null)
        {
            container = new Node3D { Name = SPAWN_CONTAINER_NAME };
            AddChild(container);
            container.Owner = this;
        }

        return (Node3D)container;
    }

    private Node3D GetOrCreateSpawnContainerCategoryNode(string category)
    {
        var spawnContainer = GetOrCreateSpawnContainer();
        var categoryNode3D = (Node3D)spawnContainer.GetNodeOrNull(category);

        if (string.IsNullOrWhiteSpace(category))
            return spawnContainer;

        if (categoryNode3D == null)
        {
            categoryNode3D = new Node3D { Name = category };
            spawnContainer.AddChild(categoryNode3D);
            categoryNode3D.Owner = this;
        }

        return categoryNode3D;
    }

    private void UpdateSizeInternal(Vector3 size)
    {
        this.size = size;

        // Same as Validate(), but seems overkill to create a method for it...
        if (Size.X < 0 || Size.Y < 0 || Size.Z < 0)
        {
            GD.PushWarning($"Cancelled Generation: Can not generate a room with Size: {Size}");
            if (Engine.IsEditorHint())
                debugDrawer?.Clear();
            return;
        }

        if (Engine.IsEditorHint())
            DrawDebugInEditor();
    }

    private string[] Validate()
    {
        System.Collections.Generic.List<string> failures = [];
        if (Size.X < 0 || Size.Y < 0 || Size.Z < 0)
            failures.Add($"Cancelled Generation: Can not generate a room with Size: {Size}");
        if (RoomResourceLibrary.Count < 1)
            failures.Add("RoomResourceLibrary is empty. RoomPlanner can not spawn any geometry!");

        foreach (var child in GetChildren())
            if (child is PrettyPlannerNode && child is not PrettyPlannerRule)
                failures.Add(
                    $"PrettyFloorPlanner expects direct children to be of type '{nameof(PrettyPlannerRule)}'. Moving '{child.Name}' into a '{nameof(PrettyPlannerRule)}' will fix the issue."
                );

        return failures.ToArray();
    }

    private void DrawDebugInEditor()
    {
        if (!Engine.IsEditorHint())
            return;

        EnsureDebugDrawer();
        debugDrawer.Clear();
        bool shouldRedraw =
            debugDrawer.IsNodeSelectedInEditor(this)
            || debugDrawer.IsAnySelectedNodeAnDecendantOf(this);

        if (shouldRedraw)
        {
            debugDrawer.SetDrawColor(new Color(0f, 0.3f, 1f));
            debugDrawer.DrawBox(GlobalPosition, Quaternion.Identity, Size);
        }
    }

    private void EnsureDebugDrawer()
    {
        if (debugDrawer == null)
        {
            debugDrawer = new Draw3DMeshInstance();
            AddChild(debugDrawer, false, InternalMode.Front);
        }
    }
}
