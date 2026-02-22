#if TOOLS
using Godot;

namespace PrettyRoomGen3D;

[Tool]
public partial class PrettyRoomPlanner3DPlugin : EditorPlugin
{
    const string ACTION_PATH = "res://addons/prettyroomplanner3d/nodes/actions/";
    const string TRANSFORMER_PATH = "res://addons/prettyroomplanner3d/nodes/transformers/";
    const string ICONS_PATH = "res://addons/prettyroomplanner3d/icons/";

    public override void _EnterTree()
    {
        // Load Icons
        var gridIcon = GD.Load<Texture2D>(ICONS_PATH + "grid.svg");
        var plusSqureIcon = GD.Load<Texture2D>(ICONS_PATH + "plus-square.svg");
        var arrowsMoveIcon = GD.Load<Texture2D>(ICONS_PATH + "arrows-move.svg");
        var dropboxIcon = GD.Load<Texture2D>(ICONS_PATH + "dropbox.svg");
        var boxArrowInDownRightIcon = GD.Load<Texture2D>(
            ICONS_PATH + "box-arrow-in-down-right.svg"
        );
        var houseFillIcon = GD.Load<Texture2D>(ICONS_PATH + "house-fill.svg");

        // Load Scripts
        var additiveTransformerScript = GD.Load<Script>(
            TRANSFORMER_PATH + "AdditiveTransformer.cs"
        );
        var gridTransformerScript = GD.Load<Script>(TRANSFORMER_PATH + "GridTransformer.cs");
        var positionTransformerScript = GD.Load<Script>(
            TRANSFORMER_PATH + "PositionTransformer.cs"
        );

        var proabilitySpawnSceneAction = GD.Load<Script>(ACTION_PATH + "SpawnSceneAction.cs");
        var sequentialSpawnSceneAction = GD.Load<Script>(ACTION_PATH + "SequentialSpawnAction.cs");
        var removeOverlappingSceneAction = GD.Load<Script>(
            ACTION_PATH + "RemoveOverlappingSceneAction.cs"
        );

        var prettyroomPlanner = GD.Load<Script>(
            "res://addons/prettyroomplanner3d/nodes/PrettyRoomPlanner.cs"
        );

        // Add Types and assign Icons
        AddCustomType(
            nameof(AdditiveTransformer),
            nameof(Node3D),
            additiveTransformerScript,
            plusSqureIcon
        );
        AddCustomType(nameof(GridTransformer), nameof(Node3D), gridTransformerScript, gridIcon);
        AddCustomType(
            nameof(PositionTransformer),
            nameof(Node3D),
            positionTransformerScript,
            arrowsMoveIcon
        );

        AddCustomType(
            nameof(SpawnSceneAction),
            nameof(Node3D),
            proabilitySpawnSceneAction,
            dropboxIcon
        );

        AddCustomType(
            nameof(SequentialSpawnAction),
            nameof(Node3D),
            sequentialSpawnSceneAction,
            dropboxIcon
        );
        AddCustomType(
            nameof(RemoveOverlappingSceneAction),
            nameof(Node3D),
            removeOverlappingSceneAction,
            boxArrowInDownRightIcon
        );
        AddCustomType(nameof(PrettyRoomPlanner), nameof(Node3D), prettyroomPlanner, houseFillIcon);
    }

    public override void _ExitTree()
    {
        // Clean-up of the plugin goes here.
        RemoveCustomType(nameof(AdditiveTransformer));
        RemoveCustomType(nameof(GridTransformer));
        RemoveCustomType(nameof(PositionTransformer));
        RemoveCustomType(nameof(SpawnSceneAction));
        RemoveCustomType(nameof(SequentialSpawnAction));
        RemoveCustomType(nameof(RemoveOverlappingSceneAction));
        RemoveCustomType(nameof(PrettyRoomPlanner));
    }
}
#endif
