using System.Collections.Generic;
using Godot;
using Godot.Collections;

namespace PrettyRoomGen3D;

/// <summary>
/// Action to sequencially spawnn room resources. Foreach transformer position it spawns the next scene in the specified Categories (in order).
/// </summary>
[Tool]
[GlobalClass]
public sealed partial class SequentialSpawnAction : PrettyPlannerAction
{
    class PackedSceneNode
    {
        public Transform3D transform;
        public PackedScene scene;
        public string category;
    }

    [Export]
    public PrettyPlannerTransformer OverrideTransformer { get; set; }

    [Export]
    public bool SpawnAnyCategoryAsFallback { get; set; } = true;

    [Export]
    public Array<string> Categories { get; set; } = new();

    protected override void OnExecute(
        PrettyRoomPlanner roomPlanner,
        PrettyPlannerNode previousExecuter
    )
    {
        var transformer = OverrideTransformer;
        transformer ??= FindLastPlannerTransformer();

        if (transformer == null)
            return;

        var packedSceneNodes = CreatePackedSceneNodeList(transformer);
        // Spawn Scenes
        foreach (var packedSceneNode in packedSceneNodes)
        {
            Transform3D transform = packedSceneNode.transform;
            Node instance = InstantiateRoomResource(packedSceneNode.scene, transform);
            roomPlanner.AddSceneInstance(packedSceneNode.category, instance);
        }
    }

    private Node InstantiateRoomResource(PackedScene scene, Transform3D transform)
    {
        Node3D instance = (Node3D)scene.Instantiate();
        instance.Position = transform.Origin;
        instance.Quaternion = transform.Basis.GetRotationQuaternion();
        return instance;
    }

    private List<PackedSceneNode> CreatePackedSceneNodeList(PrettyPlannerTransformer transformer)
    {
        var roomPlanner = RoomPlanner;
        var transformations = transformer.GetTransformations();
        var library = roomPlanner.RoomResourceLibrary;

        List<PackedSceneNode> packedSceneNodes = new();
        int transformationIndex = 0;
        bool sceneFound = true;

        // Creates a sequenctial list of scenes to spawn...
        while (transformationIndex < transformations.Length && sceneFound == true)
        {
            sceneFound = false;

            if (Categories != null)
            {
                foreach (var category in Categories)
                {
                    if (!library.TryGetValue(category, out PrettyRoomResource roomResource))
                        continue;
                    if (roomResource.Scenes == null)
                        continue;

                    foreach (var scene in roomResource.Scenes)
                    {
                        if (scene == null)
                            continue;

                        packedSceneNodes.Add(
                            new PackedSceneNode
                            {
                                transform = transformations[transformationIndex],
                                scene = scene,
                                category = category,
                            }
                        );
                        sceneFound = true;
                        transformationIndex++;

                        if (transformationIndex >= transformations.Length)
                            return packedSceneNodes;
                    }
                }
            }
            if (sceneFound == false && SpawnAnyCategoryAsFallback)
            {
                foreach (var kvp in library)
                {
                    var category = kvp.Key;
                    var scenes = kvp.Value?.Scenes;

                    if (scenes == null)
                        continue;

                    foreach (var scene in scenes)
                    {
                        if (scene == null)
                            continue;

                        if (transformationIndex >= transformations.Length)
                            break;

                        packedSceneNodes.Add(
                            new PackedSceneNode
                            {
                                transform = transformations[transformationIndex],
                                scene = scene,
                                category = category,
                            }
                        );

                        sceneFound = true;
                        transformationIndex++;

                        if (transformationIndex >= transformations.Length)
                            return packedSceneNodes;
                    }
                }
            }

            if (sceneFound == false)
            {
                GD.Print(
                    "No RoomResources spawned, did you forget to add scenes in the RoomResources or setup categories?"
                );
                // Nothing to do we do not want to spawn anything
                break;
            }
        }

        return packedSceneNodes;
    }
}
