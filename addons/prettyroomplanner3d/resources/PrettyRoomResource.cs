using Godot;
using Godot.Collections;

namespace PrettyDunGen3D;

[Tool]
[GlobalClass]
public partial class PrettyRoomResource : Resource
{
    [Export]
    public string Category { get; set; } = "wall";

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
