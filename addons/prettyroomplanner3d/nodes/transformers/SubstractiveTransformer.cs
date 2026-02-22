using System.Collections.Generic;
using System.Linq;
using Godot;

namespace PrettyRoomGen3D;

[Tool]
[GlobalClass]
public sealed partial class SubstractiveTransformer : PrettyPlannerTransformer
{
    [Export(PropertyHint.Range, "0.1, 3,,or_greater")]
    public float Epsilon { get; set; } = 0.2f;

    [Export]
    public PrettyPlannerTransformer ReferenceTransformer { get; set; }
    Draw3DMeshInstance debugDrawer;

    public override Transform3D[] GetTransformations()
    {
        if (ReferenceTransformer == null || ReferenceTransformer == this)
            return [];

        var substractiveTransformers = GetPlannerNodeChildren(this)
            .Where(n => n is PrettyPlannerTransformer)
            .Select(p => (PrettyPlannerTransformer)p);

        float epsilonSquared = Epsilon * Epsilon;

        List<Transform3D> refTransformations = ReferenceTransformer.GetTransformations().ToList();
        Transform3D[] substrTransformations;

        for (int i = refTransformations.Count - 1; i >= 0; i--)
        {
            Vector3 referenceVector = refTransformations[i].Origin;
            foreach (var st in substractiveTransformers)
            {
                substrTransformations = st.GetTransformations();
                for (int j = 0; j < substrTransformations.Length; j++)
                {
                    Vector3 substrVector = substrTransformations[j].Origin;
                    if (referenceVector.DistanceSquaredTo(substrVector) <= epsilonSquared)
                        refTransformations.RemoveAt(i);
                }
            }
        }

        return refTransformations.ToArray();
    }

    public override string[] _GetConfigurationWarnings()
    {
        if (ReferenceTransformer == null)
            return ["ReferenceTransformer is not set."];

        if (ReferenceTransformer == this)
            return ["ReferenceTransformer can not be self!"];

        return base._GetConfigurationWarnings();
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
