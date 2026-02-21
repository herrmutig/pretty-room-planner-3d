using System.Linq;
using Godot;
using Godot.Collections;
using PrettyDunGen3D;

[GlobalClass]
[Tool]
public partial class SpawnSceneAction : PrettyPlannerAction
{
    [ExportGroup("Spawn Settings")]
    [Export]
    public PrettyPlannerTransformer OverrideTransformer { get; set; }

    [Export]
    public Array<CategoryProbabilityResource> SpawnCategories { get; set; }

    public override string[] _GetConfigurationWarnings()
    {
        if (OverrideTransformer == null && FindLastPlannerTransformer() == null)
            return
            [
                $"{nameof(SpawnSceneAction)} has no transformer attached, please set an OverrideTransformer or add one as parent to this node",
            ];
        return [];
    }

    protected override void OnExecute(
        PrettyRoomPlanner roomPlanner,
        PrettyPlannerNode previousExecuter
    )
    {
        PrettyPlannerTransformer transformer = OverrideTransformer;
        if (transformer == null)
            transformer = FindLastPlannerTransformer();
        if (transformer == null)
        {
            GD.PushWarning(
                $"{nameof(SpawnSceneAction)} has no transformer attached, please set an OverrideTransformer or add one as parent to this node"
            );
            return;
        }

        IOrderedEnumerable<CategoryProbabilityResource> sortedSpawnCategories = null;
        if (SpawnCategories != null && SpawnCategories.Count > 0)
        {
            sortedSpawnCategories = SpawnCategories
                .Where(c => c != null)
                .OrderBy(c => c.Probability);
        }

        string category = "";

        foreach (Transform3D transform in transformer.GetTransformations())
        {
            if (sortedSpawnCategories != null)
                category = RollCategory(sortedSpawnCategories);

            PackedScene scene = GetRandomPackedScene(category);

            if (scene == null)
            {
                GD.Print(
                    $"A RoomResource with the category '{category}' was not found. Consider adding it to the RoomResourceLibrary in the {nameof(PrettyRoomPlanner)} Node"
                );
                continue;
            }

            Node3D instance = (Node3D)scene.Instantiate();
            instance.Position = transform.Origin;
            instance.Quaternion = transform.Basis.GetRotationQuaternion();
            roomPlanner.AddSceneInstance(category, instance);
        }
    }

    PackedScene GetRandomPackedScene(string roomResourceCategory = "")
    {
        var roomResources = RoomPlanner.RoomResourceLibrary;
        var numGen = RoomPlanner.NumberGenerator;

        if (string.IsNullOrWhiteSpace(roomResourceCategory))
        {
            var randomNumber = numGen.RandiRange(0, roomResources.Count - 1);
            int counter = 0;
            foreach (var kvp in roomResources)
            {
                if (counter == randomNumber)
                {
                    var scenes = kvp.Value.Scenes;
                    if (scenes.Count == 0)
                        return null;

                    return kvp.Value.Scenes[numGen.RandiRange(0, scenes.Count - 1)];
                }
            }
        }

        if (roomResources.TryGetValue(roomResourceCategory, out PrettyRoomResource resource))
        {
            if (resource.Scenes.Count == 0)
                return null;

            return resource.Scenes[numGen.RandiRange(0, resource.Scenes.Count - 1)];
        }

        return null;
    }

    string RollCategory(IOrderedEnumerable<CategoryProbabilityResource> spawnCategories)
    {
        float totalWeight = 0f;
        foreach (var probResource in spawnCategories)
        {
            if (probResource == null)
                continue;

            totalWeight += probResource.Probability;
        }

        float weight = totalWeight * RoomPlanner.NumberGenerator.Randf();
        float cumulative = 0;

        GD.Print(weight);

        foreach (var probResource in spawnCategories)
        {
            if (probResource == null)
                continue;

            cumulative += probResource.Probability;

            if (weight <= cumulative)
                return probResource.Category;
        }

        // Only happens when probResources are not setup correctly
        // Fallback to no category preference seems reasonable...
        return "";
    }
}
