using Godot;
using PrettyDunGen3D;

[Tool]
[GlobalClass]
public partial class ConnectorSpawnAction : PrettyPlannerAction
{
    [Export]
    public string DeleteRoomResourcesWithCategory { get; set; } = "default";

    [Export(PropertyHint.Range, "0.01,0.25,0.01")]
    public float Padding { get; set; } = 0.05f;

    protected override async void OnPostExecute(
        PrettyRoomPlanner roomPlanner,
        PrettyPlannerNode previousExecuter
    )
    {
        PrettyPlannerTransformer transformer = FindLastPlannerTransformer();
        if (transformer != null)
        {
            var transformations = transformer.GetTransformations();

            if (transformations.Length <= 0)
                return;

            Aabb aabb = new Aabb(transformations[0].Origin, Vector3.One);
            for (int i = 1; i < transformations.Length; i++)
            {
                aabb = aabb.Expand(transformations[i].Origin);
            }

            aabb.Size -= Vector3.One * Padding;

            if (!Engine.IsInPhysicsFrame())
                await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

            var spaceState = roomPlanner.GetWorld3D().DirectSpaceState;
            var query = new PhysicsShapeQueryParameters3D
            {
                CollideWithAreas = false,
                CollideWithBodies = true,
                Shape = new BoxShape3D { Size = aabb.Size },
                Transform = new Transform3D(
                    Basis.Identity,
                    aabb.GetCenter() + roomPlanner.GlobalPosition
                ),
            };

            var result = spaceState.IntersectShape(query);

            foreach (var intersection in result)
            {
                Node3D collider = (Node3D)intersection["collider"];

                // Safety check. We do not want to delete a room planner by accident...
                if (collider.Owner is PrettyRoomPlanner || collider.Owner == Owner)
                    continue;

                // roomPlanner.SceneInstanceDictionary

                Node instanceToRemove = collider.Owner;
                if (instanceToRemove == null)
                {
                    instanceToRemove = collider;
                }

                if (instanceToRemove.IsQueuedForDeletion())
                    continue;

                roomPlanner.TryQueueFreeRoomResourceInstance(
                    DeleteRoomResourcesWithCategory,
                    instanceToRemove
                );
            }
        }
    }
}
