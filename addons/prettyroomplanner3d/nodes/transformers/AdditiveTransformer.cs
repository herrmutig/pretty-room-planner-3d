using System.Collections.Generic;
using Godot;
using PrettyDunGen3D;

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
