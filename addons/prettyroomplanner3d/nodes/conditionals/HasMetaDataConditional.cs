using Godot;

namespace PrettyRoomGen3D;

[Tool]
[GlobalClass]
public partial class HasMetaDataConditional : PrettyPlannerConditional
{
    [Export]
    public Node MetaDataNode { get; set; }

    [Export]
    public StringName MetaDataString { get; set; }

    protected override bool Evaluate()
    {
        if (MetaDataNode == null)
        {
            GD.PushWarning("HasMetaDataConditional is missing a MetaDataNode");
            return true;
        }

        return MetaDataNode.HasMeta(MetaDataString);
    }
}
