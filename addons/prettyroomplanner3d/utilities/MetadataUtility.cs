using Godot;

public static class MetadataUtility
{
    public static void CopyMetadata(GodotObject from, GodotObject to)
    {
        foreach (string metaName in from.GetMetaList())
        {
            // Do not override script data...
            if (metaName == "_custom_type_script")
                continue;

            to.SetMeta(metaName, from.GetMeta(metaName));
        }
    }
}
