using System.Collections.Generic;
using Godot;

namespace PrettyRoomGen3D;

[Tool]
[GlobalClass]
public partial class AdditiveTransformer : PrettyPlannerTransformer
{
    Draw3DMeshInstance debugDrawer;

    public override Transform3D[] GetTransformations()
    {
        List<Transform3D> transforms = new();
        foreach (var child in GetPlannerNodeChildren(this))
            if (child is PrettyPlannerTransformer transformer)
                transforms.AddRange(transformer.GetTransformations());

        return transforms.ToArray();
    }

    public override void _Process(double delta)
    {
        if (!Engine.IsEditorHint())
            return;

        DrawDebugInEditor();
    }

    private void DrawDebugInEditor()
    {
        if (!Engine.IsEditorHint() || RoomPlanner == null)
            return;

        EnsureDebugDrawer();
        debugDrawer.Clear();

        if (!debugDrawer.IsNodeSelectedInEditor(this))
            return;

        foreach (var transform in GetTransformations())
        {
            Vector3 origin = RoomPlanner.GlobalPosition + transform.Origin;
            debugDrawer.SetDrawColor(new Color(1f, 0.1f, 0f));
            debugDrawer.DrawBox(
                origin,
                transform.Basis.GetRotationQuaternion(),
                Vector3.One * 0.25f
            );

            Vector3 xDir = transform.Basis.X * 0.3f;
            Vector3 yDir = transform.Basis.Y * 0.3f;
            Vector3 zDir = transform.Basis.Z * 0.3f;

            debugDrawer.SetDrawColor(new Color(1, 0.1f, 0f));
            debugDrawer.DrawLine([origin, origin + xDir]);
            debugDrawer.SetDrawColor(new Color(0.05f, 1, 0.05f));
            debugDrawer.DrawLine([origin, origin + yDir]);
            debugDrawer.SetDrawColor(new Color(0f, 0.1f, 1));
            debugDrawer.DrawLine([origin, origin + zDir]);
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
