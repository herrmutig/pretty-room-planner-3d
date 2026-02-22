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

    public PackedScene GetScene(int relativeIndex)
    {
        if (scenes == null || scenes.Count == 0)
            return null;

        if (relativeIndex < 0)
            relativeIndex = Mathf.Abs(relativeIndex);

        return scenes[relativeIndex % scenes.Count];
    }

    private Array<PackedScene> scenes = new();
}
