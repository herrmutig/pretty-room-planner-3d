using Godot;
using PrettyDunGen3D;

[GlobalClass]
[Tool]
public partial class SpawnSceneAction : PrettyPlannerAction
{
    [ExportGroup("Spawn Settings")]
    [Export]
    public string RoomResourceCategory { get; set; } = "floor";

    protected override void OnExecute(
        PrettyRoomPlanner roomPlanner,
        PrettyPlannerNode previousExecuter
    )
    {
        PrettyRoomResource roomResource = roomPlanner.GetRandomRoomResource(RoomResourceCategory);
        if (roomResource == null)
            return;

        PrettyPlannerTransformer transformer = FindLastPlannerTransformer();

        if (transformer != null)
        {
            foreach (Transform3D transform in transformer.GetTransformations())
            {
                var randomScene = roomResource.Scenes.PickRandom();
                Node3D instance = (Node3D)randomScene.Instantiate();
                instance.Transform = transform;
                roomPlanner.AddSceneInstance(roomResource.Category, instance);
            }
        }
    }
}
