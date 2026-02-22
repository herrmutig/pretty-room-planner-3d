using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;

namespace PrettyRoomGen3D;

// TODO Improve FromCenter Logic -> It should start from the very center (considering of row and column count)
// TODO -> AnchorPoint (Start Left, Right, Forward, Back, Center)

[Tool]
[GlobalClass]
public sealed partial class GridTransformer : PrettyPlannerTransformer
{
    public enum CellTakeStrategy
    {
        FromStart,
        FromCenter,
        FromEnd,
        InverseFromCenter,
    }

    [ExportGroup("Grid Settings")]
    [Export]
    public AnchorStrategy Anchor { get; set; }

    [Export(PropertyHint.Range, "0.1,1,")]
    public float RelativeSizeX { get; set; } = 1f;

    [Export(PropertyHint.Range, "0.1,1,")]
    public float RelativeSizeZ { get; set; } = 1f;

    [Export(PropertyHint.Range, "-1, 10,1,or_greater")]
    public float MaxColumns { get; set; } = -1;

    [Export]
    public bool InvertColumnOrder { get; set; } = false;

    [Export(PropertyHint.Range, "-1, 10,1,or_greater")]
    public float MaxRows { get; set; } = -1;

    [Export]
    public bool InvertRowOrder { get; set; } = false;

    [ExportGroup("Cell Settings")]
    [Export]
    public bool AlwaysRoundToNextCell { get; set; } = true;

    [ExportSubgroup("Cell Size")]
    [Export]
    public float CellSizeX = 1f;

    [Export]
    public float CellSizeZ = 1f;

    [Export]
    public Vector3 CellRotation { get; set; } = Vector3.Zero;

    [ExportSubgroup("Cell Filter")]
    [Export]
    public int CellCount = -1;

    [Export]
    public CellTakeStrategy CellPickStrategy { get; set; } = CellTakeStrategy.FromCenter;

    Draw3DMeshInstance debugDrawer;

    public override void _Process(double delta)
    {
        if (!Engine.IsEditorHint())
            return;

        DrawDebugInEditor();
    }

    public override Transform3D[] GetTransformations()
    {
        int maxColumns = (int)MaxColumns;
        int maxRows = (int)MaxRows;
        int cellCount = CellCount;

        if (maxColumns == 0 || maxRows == 0 || cellCount == 0)
            return [];

        Vector2I iterations = GetCellCount();
        int xLimit = CalculateCellCountLimit(iterations.X, maxColumns);
        int yLimit = CalculateCellCountLimit(iterations.Y, maxRows);

        var result = BuildGrid(iterations, xLimit, yLimit);

        if (cellCount > 0)
        {
            cellCount = Mathf.Clamp(cellCount, 1, result.Count);
            switch (CellPickStrategy)
            {
                case CellTakeStrategy.FromStart:
                    return result.Take(cellCount).ToArray();
                case CellTakeStrategy.FromCenter:
                {
                    Vector3 center = result[result.Count / 2].Origin;
                    result.Sort(
                        (a, b) =>
                        {
                            return center
                                .DistanceSquaredTo(a.Origin)
                                .CompareTo(center.DistanceSquaredTo(b.Origin));
                        }
                    );
                    return result.Take(cellCount).ToArray();
                }
                case CellTakeStrategy.InverseFromCenter:
                {
                    Vector3 center = result[result.Count / 2].Origin;
                    result.Sort(
                        (a, b) =>
                        {
                            return center
                                .DistanceSquaredTo(b.Origin)
                                .CompareTo(center.DistanceSquaredTo(a.Origin));
                        }
                    );
                    return result.Take(cellCount).ToArray();
                }
                case CellTakeStrategy.FromEnd:
                    return result.TakeLast(cellCount).ToArray();
                default:
                    throw new NotImplementedException(
                        $"{nameof(CellPickStrategy)} is not implemented"
                    );
            }
        }

        return result.ToArray();
        // Filters result dependent on CellCount and Strategy

        /*  */

        //return result.ToArray();
    }

    private List<Transform3D> BuildGrid(Vector2I iterations, int xLimit, int yLimit)
    {
        List<Transform3D> result = new();
        int x = InvertColumnOrder ? iterations.X - 1 : 0;
        int xStep = InvertColumnOrder ? -1 : 1;
        int zStep = InvertRowOrder ? -1 : 1;

        Vector3 radAngles = new Vector3(
            Mathf.DegToRad(CellRotation.X),
            Mathf.DegToRad(CellRotation.Y),
            Mathf.DegToRad(CellRotation.Z)
        );

        Vector3 origin = GetOriginPosition();

        while (IterationCheck(x, xLimit, iterations.X, InvertColumnOrder))
        {
            int z = InvertRowOrder ? iterations.Y - 1 : 0;
            while (IterationCheck(z, yLimit, iterations.Y, InvertRowOrder))
            {
                Transform3D transform = new Transform3D(
                    Basis.FromEuler(radAngles),
                    origin + new Vector3(x * CellSizeX, 0f, z * CellSizeZ)
                );

                result.Add(transform);
                z += zStep;
            }
            x += xStep;
        }

        return result;
    }

    /// <summary>
    /// Checks whether a iteration (column or row) is within a <c>limit</c>,
    /// either from the start or from the end of the <c>cellCountDimension</c>.
    /// </summary>
    /// <remarks>
    /// if invert is <c>false</c> it checks if the <c>index</c> is between limit (excluded) and cellCountDimension.
    /// <c>invert</c> = <c>true</c> checks if the <c>index</c> is outside of limit (included).
    /// </remarks>
    private bool IterationCheck(int index, int limit, int cellCountDimension, bool invert) =>
        invert
            ? (index >= 0 && index >= cellCountDimension - limit)
            : (index >= 0 && index < limit);

    /// <summary>
    /// Gets back the maximum number of cells possible for the grid.
    /// </summary>
    /// <remarks>
    /// This method utilizes <see cref="AlwaysRoundToNextCell"/> to ceil to the next integer if needed.
    /// </remarks>
    private Vector2I GetCellCount()
    {
        Vector2 size = GetGridSize();
        return new Vector2I(
            AlwaysRoundToNextCell
                ? Mathf.CeilToInt(size.X / CellSizeX)
                : Mathf.RoundToInt(size.X / CellSizeX),
            AlwaysRoundToNextCell
                ? Mathf.CeilToInt(size.Y / CellSizeZ)
                : Mathf.RoundToInt(size.Y / CellSizeZ)
        );
    }

    /// <summary>
    /// Calculates the effective grid size for the GridTransformer.
    /// </summary>
    /// <remarks>
    /// This method utilizes <see cref="RelativeSize"/> and the <see cref="RoomPlanner.Size"/>
    /// to determine the grid size.
    /// </remarks>
    private Vector2 GetGridSize() =>
        new Vector2(RoomPlanner.Size.X * RelativeSizeX, RoomPlanner.Size.Z * RelativeSizeZ);

    private Vector3 GetOriginPosition()
    {
        Vector2 size = GetGridSize();
        Vector3 origin = Vector3.Zero;
        origin.X = -size.X * 0.5f + 0.5f * CellSizeX;
        origin.Z = -size.Y * 0.5f + 0.5f * CellSizeZ;
        return origin + Position;
    }

    private int CalculateCellCountLimit(int iterationDimension, int limit)
    {
        if (limit < 0)
            return iterationDimension;

        return Mathf.Clamp(Mathf.Clamp(iterationDimension, 0, limit), 0, iterationDimension);
    }

    private void DrawDebugInEditor()
    {
        if (!Engine.IsEditorHint() || RoomPlanner == null)
            return;

        EnsureDebugDrawer();
        debugDrawer.Clear();

        if (!debugDrawer.IsNodeSelectedInEditor(this) && !debugDrawer.IsDirectChildSelected(this))
            return;

        foreach (var transform in GetTransformations())
        {
            Vector3 origin = RoomPlanner.GlobalPosition + transform.Origin;
            debugDrawer.SetDrawColor(new Color(1f, 0.1f, 0f));
            debugDrawer.DrawPlane(
                origin,
                Quaternion.Identity,
                new Vector3(CellSizeX, 0, CellSizeZ)
            );

            Vector3 xDir = transform.Basis.X * 0.3f;
            Vector3 yDir = transform.Basis.Y * 0.3f;
            Vector3 zDir = transform.Basis.Z * 0.3f;

            debugDrawer.SetDrawColor(new Color(1, 0.1f, 0f));
            debugDrawer.DrawLine([origin, origin + xDir]);
            debugDrawer.SetDrawColor(new Color(0.05f, 1, 0.05f));
            debugDrawer.DrawLine([origin, origin + yDir]);
            debugDrawer.SetDrawColor(new Color(0f, 0.1f, 1));
            debugDrawer.DrawLine([origin, origin + zDir]);
        }
    }

    private void EnsureDebugDrawer()
    {
        if (debugDrawer == null)
        {
            debugDrawer = new Draw3DMeshInstance();
            AddChild(debugDrawer, false, InternalMode.Front);
        }
    }
}
