using Godot;

namespace PrettyRoomGen3D;

// TODO
// -> Get Positions from a ReferenceTransformer
// -> Substract Child-Transformers from the positions
// -> Meet an Epislon (Basically if the target position is between an epsilon => remove it.)
public partial class SubstractiveTransformer : PrettyPlannerTransformer
{
    public override Transform3D[] GetTransformations()
    {
        throw new System.NotImplementedException();
    }
}
