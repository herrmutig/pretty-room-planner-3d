using Godot;
using Godot.Collections;

namespace PrettyRoomGen3D;

[Tool]
[GlobalClass]
public partial class PrettyRoomResource : Resource
{
    [Export]
    public Array<PackedScene> Scenes
    {
        get => scenes;
        private set
        {
            if (value == null)
                scenes.Clear();
            else
                scenes = value;

            EmitChanged();
        }
    }
    private Array<PackedScene> scenes = new();
}
