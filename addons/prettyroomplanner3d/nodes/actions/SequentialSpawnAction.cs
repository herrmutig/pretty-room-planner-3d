using Godot;

namespace PrettyRoomGen3D;

// TODO -> Add RoomResources
// Will spawn Resources sequencially (Each PackedScene gets spawned in it)
// If has more transformations than spawns => loop from beginning
public sealed partial class SequentialSpawnAction : PrettyPlannerAction { }
