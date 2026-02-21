using Godot;

// Helpful Resource:
// https://www.khronos.org/opengl/wiki/Primitive

public partial class Draw3DMeshInstance : MeshInstance3D
{
    ImmediateMesh immediateMesh;
    StandardMaterial3D defaultMaterial;
    Color drawColor = new Color(0, 0.125f, 0.5f);

    public Draw3DMeshInstance()
    {
        defaultMaterial = CreateDefaultMaterial();
        immediateMesh = new ImmediateMesh();
        Mesh = immediateMesh;
        TopLevel = true;
    }

    public bool IsAnySelectedNodeAnDecendantOf(Node node)
    {
        if (!Engine.IsEditorHint())
            return false;
        if (node == null)
            return false;

        var selection = EditorInterface.Singleton.GetSelection();
        if (selection == null)
            return false;

        var selectedNodes = selection.GetSelectedNodes();
        foreach (var selected in selectedNodes)
            if (node.IsAncestorOf(selected))
                return true;

        return false;
    }

    public bool IsDirectChildSelected(Node node)
    {
        if (!Engine.IsEditorHint())
            return false;
        if (node == null)
            return false;
        var selection = EditorInterface.Singleton.GetSelection();
        if (selection == null)
            return false;

        var selectedNodes = selection.GetSelectedNodes();
        foreach (var selected in selectedNodes)
            if (selected.GetParent() == node)
                return true;
        return false;
    }

    public bool IsNodeSelectedInEditor(Node node)
    {
        if (!Engine.IsEditorHint())
            return false;
        if (node == null)
            return false;

        var selection = EditorInterface.Singleton.GetSelection();
        if (selection == null)
            return false;

        var selectedNodes = selection.GetSelectedNodes();

        if (selectedNodes.Contains(node))
            return true;

        return false;
    }

    public void DrawLine(Vector3[] points)
    {
        DrawPrimitive(Mesh.PrimitiveType.LineStrip, points, drawColor);
    }

    public void DrawBox(Vector3 position, Quaternion rotation, Vector3 size)
    {
        Transform3D transform = new(new Basis(rotation.Normalized()), position);
        // Half extent results in a box with provided size.
        transform = transform.ScaledLocal(size * 0.5f);

        Vector3[] verticesFront =
        [
            transform * new Vector3(-1, -1, 1),
            transform * new Vector3(1, -1, 1),
            transform * new Vector3(1, 1, 1),
            transform * new Vector3(-1, 1, 1),
            transform * new Vector3(-1, -1, 1), // Loop back to first vertex
        ];

        Vector3[] verticesBack =
        [
            transform * new Vector3(-1, -1, -1),
            transform * new Vector3(1, -1, -1),
            transform * new Vector3(1, 1, -1),
            transform * new Vector3(-1, 1, -1),
            transform * new Vector3(-1, -1, -1), // Loop back to first vertex
        ];

        DrawLine(verticesFront);
        DrawLine(verticesBack);

        DrawLine([verticesFront[0], verticesBack[0]]);
        DrawLine([verticesFront[1], verticesBack[1]]);
        DrawLine([verticesFront[2], verticesBack[2]]);
        DrawLine([verticesFront[3], verticesBack[3]]);
    }

    public void DrawPlane(Vector3 position, Quaternion rotation, Vector3 size)
    {
        Transform3D transform = new(new Basis(rotation.Normalized()), position);
        transform = transform.ScaledLocal(size * 0.5f);

        Vector3[] vertices =
        [
            transform * new Vector3(-1, 0, 1),
            transform * new Vector3(1, 0, 1),
            transform * new Vector3(1, 0, -1),
            transform * new Vector3(-1, 0, -1),
            transform * new Vector3(-1, 0, 1),
        ];

        DrawLine(vertices);
    }

    public void SetDrawColor(Color color) => drawColor = color;

    public void Clear()
    {
        immediateMesh.ClearSurfaces();
    }

    private void DrawPrimitive(Mesh.PrimitiveType primitiveType, Vector3[] vertices, Color color)
    {
        immediateMesh.SurfaceBegin(primitiveType, defaultMaterial);
        foreach (Vector3 vertex in vertices)
        {
            immediateMesh.SurfaceSetColor(color);
            immediateMesh.SurfaceAddVertex(vertex);
        }
        immediateMesh.SurfaceEnd();
    }

    private StandardMaterial3D CreateDefaultMaterial()
    {
        return new StandardMaterial3D
        {
            VertexColorUseAsAlbedo = true,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoColor = new Color(1, 1, 1),
            NoDepthTest = true,
        };
    }
}
