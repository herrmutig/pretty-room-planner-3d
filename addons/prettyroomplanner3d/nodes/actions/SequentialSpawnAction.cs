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
    class PrettyRoomResourceNode
    {
        public Transform3D transform;
        public PackedScene scene;
        public PrettyRoomResource resource;
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

        var roomResourceNodes = CreateRoomResourceNodeList(transformer);
        // Spawn Scenes
        foreach (var roomResourceNode in roomResourceNodes)
        {
            Transform3D transform = roomResourceNode.transform;
            Node instance = InstantiatePackedScene(roomResourceNode.scene, transform);
            roomPlanner.AddSceneInstance(
                roomResourceNode.category,
                instance,
                roomResourceNode.resource
            );

            // Note: similar metadata will override metadata from the provided RoomResource - see AddSceneInstance()
            MetadataUtility.CopyMetadata(this, instance);
        }
    }

    private Node InstantiatePackedScene(PackedScene scene, Transform3D transform)
    {
        Node3D instance = (Node3D)scene.Instantiate();
        instance.Position = transform.Origin;
        instance.Quaternion = transform.Basis.GetRotationQuaternion();
        return instance;
    }

    private List<PrettyRoomResourceNode> CreateRoomResourceNodeList(
        PrettyPlannerTransformer transformer
    )
    {
        var roomPlanner = RoomPlanner;
        var transformations = transformer.GetTransformations();
        var library = roomPlanner.RoomResourceLibrary;

        List<PrettyRoomResourceNode> roomResourceNodes = new();
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

                        roomResourceNodes.Add(
                            new PrettyRoomResourceNode
                            {
                                transform = transformations[transformationIndex],
                                scene = scene,
                                category = category,
                                resource = roomResource,
                            }
                        );
                        sceneFound = true;
                        transformationIndex++;

                        if (transformationIndex >= transformations.Length)
                            return roomResourceNodes;
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

                        roomResourceNodes.Add(
                            new PrettyRoomResourceNode
                            {
                                transform = transformations[transformationIndex],
                                scene = scene,
                                category = category,
                                resource = kvp.Value,
                            }
                        );

                        sceneFound = true;
                        transformationIndex++;

                        if (transformationIndex >= transformations.Length)
                            return roomResourceNodes;
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

        return roomResourceNodes;
    }
}
