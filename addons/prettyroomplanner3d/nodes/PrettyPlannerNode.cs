using Godot;
using Godot.Collections;

namespace PrettyRoomGen3D;

[GlobalClass]
[Tool]
public abstract partial class PrettyPlannerNode : Node3D
{
    public PrettyPlannerNode PreviousExecuter { get; private set; }
    public PrettyRoomPlanner RoomPlanner
    {
        get => GetOrFindRoomPlanner();
        private set => roomPlanner = value;
    }

    PrettyRoomPlanner roomPlanner;

    public PrettyRoomPlanner GetOrFindRoomPlanner()
    {
        if (roomPlanner != null && !roomPlanner.IsQueuedForDeletion())
            return roomPlanner;
        if (Owner is PrettyRoomPlanner planner)
            roomPlanner = planner;

        if (Engine.IsEditorHint())
        {
            // Only relevant for the editor, room planner is set on execute anyways
            // helpful to always have a reference to draw debugging geometry.
            Node parent = GetParent();
            while (parent != null)
            {
                if (parent is PrettyRoomPlanner)
                {
                    roomPlanner = (PrettyRoomPlanner)parent;
                    break;
                }

                parent = parent.GetParent();
            }
        }

        return roomPlanner;
    }

    public void Execute(PrettyRoomPlanner roomPlanner, PrettyPlannerNode previousExecuter = null)
    {
        PreviousExecuter = previousExecuter;
        RoomPlanner = roomPlanner;

        OnExecute(roomPlanner, previousExecuter);

        if (!AllowChildrenToExecute())
            return;

        foreach (var plannerNode in GetPlannerNodeChildren(this))
        {
            plannerNode.Execute(roomPlanner, this);
        }
    }

    // Oh not code duplication! :OOOOOOOO - lul
    public void PostExecute(
        PrettyRoomPlanner roomPlanner,
        PrettyPlannerNode previousExecuter = null
    )
    {
        PreviousExecuter = previousExecuter;
        RoomPlanner = roomPlanner;
        OnPostExecute(roomPlanner, previousExecuter);
        if (!AllowChildrenToExecute())
            return;

        foreach (var plannerNode in GetPlannerNodeChildren(this))
        {
            plannerNode.PostExecute(roomPlanner, this);
        }
    }

    // Especially useful for Conditional Nodes...
    protected virtual bool AllowChildrenToExecute()
    {
        return true;
    }

    protected virtual void OnExecute(
        PrettyRoomPlanner roomPlanner,
        PrettyPlannerNode previousExecuter
    ) { }

    protected virtual void OnPostExecute(
        PrettyRoomPlanner roomPlanner,
        PrettyPlannerNode previousExecuter
    ) { }

    protected Array<PrettyPlannerNode> GetPlannerNodeChildren(PrettyPlannerNode root)
    {
        Array<PrettyPlannerNode> plannerNodes = new();
        foreach (var child in root.GetChildren())
        {
            if (child is PrettyPlannerNode plannerNode)
            {
                plannerNodes.Add(plannerNode);
            }
        }

        return plannerNodes;
    }

    /// <summary>
    /// Searches the executer chain backwards and returns the most recent
    /// <see cref="PrettyPlannerTransformer"/> in the chain.
    /// </summary>
    /// <returns>
    /// The last <see cref="PrettyPlannerTransformer"/> found in the chain,
    /// or <c>null</c> if no transformer exists.
    /// </returns>
    protected PrettyPlannerTransformer FindLastPlannerTransformer()
    {
        Node current = GetParent();
        while (current != null)
        {
            if (current is PrettyPlannerTransformer)
                return (PrettyPlannerTransformer)current;

            current = current.GetParent();
        }

        return null;
    }
}
