using Godot;

namespace PrettyRoomGen3D;

/// <summary>
/// Base type for planner nodes that represent conditional logic.
///
/// Although the helper methods are available on <see cref="PrettyPlannerNode"/>, conditional checks
/// should be implemented in subclasses of <see cref="PrettyPlannerConditional"/> to keep rule authoring
/// intuitive and to ensure consistent execution behavior.
/// </summary>
[GlobalClass]
public abstract partial class PrettyPlannerConditional : PrettyPlannerNode
{
    [Export]
    public bool Invert { get; set; } = false;

    protected sealed override bool AllowChildrenToExecute()
    {
        return Invert ? !Evaluate() : Evaluate();
    }

    protected abstract bool Evaluate();
}
