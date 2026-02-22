using System.Collections.Generic;
using Godot;

namespace PrettyRoomGen3D;

// TODO: Create an alternative Rule that checks for Transform Positions instead of finding a collision and removes an already spawned node
[Tool]
[GlobalClass]
public sealed partial class RemoveOverlappingSceneAction : PrettyPlannerAction
{
    [Export]
    public PrettyPlannerTransformer OverrideTransformer { get; set; }

    [Export]
    public string DeleteRoomResourcesWithCategory { get; set; } = "default";

    [Export]
    public Vector3 OverlapSize { get; set; } = Vector3.One;

    [Export]
    public uint MaxBodiesToDetect { get; set; } = 32;

    [Export(PropertyHint.Layers3DPhysics)]
    public uint CollisionMask { get; set; } = 1;

    Draw3DMeshInstance debugDrawer;

    public override void _Process(double delta)
    {
        if (!Engine.IsEditorHint())
            return;

        DrawDebugInEditor();
    }

    protected override async void OnPostExecute(
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
                $"{nameof(RemoveOverlappingSceneAction)} has no transformer attached, please set an OverrideTransformer or add one as parent to this node"
            );
            return;
        }

        if (transformer != null)
        {
            if (!Engine.IsInPhysicsFrame())
                await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

            var transformations = transformer.GetTransformations();
            if (transformations.Length <= 0)
                return;

            var shape = new BoxShape3D { Size = OverlapSize };

            for (int i = 0; i < transformations.Length; i++)
            {
                var intersections = QueryAndFindIntersections(
                    transformations[i].Origin + roomPlanner.Position,
                    shape
                );

                foreach (var intersection in intersections)
                {
                    Node3D collider = intersection;
                    // Safety check. We do not want to delete a room planner by accident...
                    if (collider.Owner is PrettyRoomPlanner || collider.Owner == Owner)
                        continue;

                    Node instanceToRemove = collider.Owner;

                    if (instanceToRemove == null)
                        instanceToRemove = collider;

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

    private List<CollisionObject3D> QueryAndFindIntersections(Vector3 position, Shape3D shape)
    {
        // Create Shape Query
        var spaceState = RoomPlanner.GetWorld3D().DirectSpaceState;
        var query = new PhysicsShapeQueryParameters3D
        {
            CollideWithAreas = false,
            CollideWithBodies = true,
            Shape = shape,
            Transform = new Transform3D(Basis.Identity, position),
            CollisionMask = CollisionMask,
        };

        // Prepare intersections
        List<CollisionObject3D> intersections = new();
        List<ProcessModeEnum> tempProcessModes = new();

        for (int j = 0; j < 32; j++)
        {
            var result = spaceState.IntersectShape(query, 1);
            if (result.Count > 0)
            {
                var collider = (CollisionObject3D)result[0]["collider"];
                tempProcessModes.Add(collider.ProcessMode);
                intersections.Add((CollisionObject3D)result[0]["collider"]);

                // Only way (besides disabiling the CollisionShape3D instance)
                // to get same result in editor and runtime when detecting bodies...
                collider.ProcessMode = ProcessModeEnum.Disabled;
            }
        }

        // Resetting process mode
        for (int i = 0; i < intersections.Count; i++)
            intersections[i].ProcessMode = tempProcessModes[i];

        return intersections;
    }

    private void DrawDebugInEditor()
    {
        if (!Engine.IsEditorHint() || RoomPlanner == null)
            return;

        EnsureDebugDrawer();
        debugDrawer.Clear();

        if (!debugDrawer.IsNodeSelectedInEditor(this) && !debugDrawer.IsDirectChildSelected(this))
            return;

        var transformer = FindLastPlannerTransformer();
        if (transformer == null)
            return;

        var transformations = transformer.GetTransformations();
        if (transformations.Length <= 0)
            return;

        for (int i = 0; i < transformations.Length; i++)
        {
            debugDrawer.SetDrawColor(new Color(0.3f, 0.5f, 1f));
            debugDrawer.DrawBox(
                transformations[i].Origin + RoomPlanner.Position,
                Quaternion.Identity,
                OverlapSize
            );
        }
    }

    private void EnsureDebugDrawer()
    {
        if (debugDrawer == null)
        {
            debugDrawer = new Draw3DMeshInstance();
        }
    }
}
