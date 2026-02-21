using Godot;

namespace PrettyRoomGen3D;

[GlobalClass]
public abstract partial class PrettyPlannerTransformer : PrettyPlannerNode
{
    public abstract Transform3D[] GetTransformations();
}
