using System;
using System.Collections.Generic;
using UnitySimulationX.SceneModel;

namespace UnitySimulationX.SceneModel.Serialization
{
    [Serializable]
    public sealed class ProjectViewerDocument
    {
        public int schemaVersion = 2;
        public ProjectAssetsDocumentData assets = new();
        public SceneDocumentData scene = new();
        public ViewSettingsData viewSettings = new();
    }

    [Serializable]
    public sealed class ProjectAssetsDocumentData
    {
        public List<ProjectAssetDocumentData> imported = new();
    }

    [Serializable]
    public sealed class ProjectAssetDocumentData
    {
        public string assetId;
        public string relativePath;
        public string originalFileName;
        public string mediaType;
        public string contentHash;
        public string importerId;
        public int importerVersion;
        public string importSettingsJson;
    }

    [Serializable]
    public sealed class SceneDocumentData
    {
        public List<SceneObjectDocumentData> objects = new();
    }

    [Serializable]
    public sealed class SceneObjectDocumentData
    {
        public string id;
        public string name;
        public string typeId;
        public string parentId;
        public TransformData transform = new();
        public bool visible = true;
        public MaterialDefinition material = new();
        public string assetId;
        public List<SceneComponentDocumentData> components = new();
    }

    [Serializable]
    public sealed class SceneComponentDocumentData
    {
        public string typeId;
        public int schemaVersion;
        public string payloadJson;
    }

    [Serializable]
    public sealed class ViewSettingsData
    {
        public string activeViewMode = "Perspective3D";
        public List<CameraBookmarkStub> cameraBookmarks = new();
    }

    [Serializable]
    public sealed class CameraBookmarkStub
    {
        public string name;
        public float[] position;
        public float[] rotation;
    }
}
