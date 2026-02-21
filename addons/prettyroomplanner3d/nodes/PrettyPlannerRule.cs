using Godot;

namespace PrettyRoomGen3D;

[GlobalClass]
[Tool]
public sealed partial class PrettyPlannerRule : PrettyPlannerNode
{
    [Export]
    public bool Mute
    {
        get => mute;
        set
        {
            mute = value;

            if (value)
            {
                if (!Name.ToString().EndsWith("(MUTED)"))
                    Name += "(MUTED)";
                return;
            }

            if (Name.ToString().EndsWith("(MUTED)"))
                Name = Name.ToString()[..^7]; // Remove 7 characters.
        }
    }
    private bool mute = false;

    protected override bool AllowChildrenToExecute()
    {
        return !Mute;
    }
}
