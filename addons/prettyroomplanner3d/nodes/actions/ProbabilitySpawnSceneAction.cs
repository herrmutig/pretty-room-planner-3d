using System.Linq;
using Godot;
using Godot.Collections;

namespace PrettyRoomGen3D;

[GlobalClass]
[Tool]
public partial class ProbabilitySpawnSceneAction : PrettyPlannerAction
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
                $"{nameof(ProbabilitySpawnSceneAction)} has no transformer attached, please set an OverrideTransformer or add one as parent to this node",
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
                $"{nameof(ProbabilitySpawnSceneAction)} has no transformer attached, please set an OverrideTransformer or add one as parent to this node"
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

            int randomIndex = roomPlanner.NumberGenerator.RandiRange(0, int.MaxValue);
            PrettyRoomResource roomResource = roomPlanner.GetRoomResource(category);
            PackedScene scene = roomResource != null ? roomResource.GetScene(randomIndex) : null;

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
            roomPlanner.AddSceneInstance(category, instance, roomResource);

            // Note: similar metadata will override metadata from the provided RoomResource - see AddSceneInstance()
            MetadataUtility.CopyMetadata(this, instance);
        }
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
