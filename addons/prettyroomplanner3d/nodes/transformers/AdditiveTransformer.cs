using System.Collections.Generic;
using Godot;

namespace PrettyRoomGen3D;

[Tool]
[GlobalClass]
public partial class AdditiveTransformer : PrettyPlannerTransformer
{
    public override Transform3D[] GetTransformations()
    {
        List<Transform3D> transforms = new();
        foreach (var child in GetPlannerNodeChildren(this))
            if (child is PrettyPlannerTransformer transformer)
                transforms.AddRange(transformer.GetTransformations());

        return transforms.ToArray();
    }
}
