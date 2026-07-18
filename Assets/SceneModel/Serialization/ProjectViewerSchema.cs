using System;
using System.Collections.Generic;
using UnityEngine;
using UnitySimulationX.SceneModel;

namespace UnitySimulationX.SceneModel.Serialization
{
    [Serializable]
    public sealed class ProjectViewerDocument
    {
        public string version = "1.0";
        public int schemaVersion = 1;
        public SceneDocumentData scene = new();
        public ViewSettingsData viewSettings = new();
        public RuntimeDocumentData runtime = new();
        public List<DiagnosticMarkerStub> diagnostics = new();
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
        public string type;
        public string typeId;
        public string parentId;
        public List<string> childrenIds = new();
        public TransformData transform = new();
        public bool visible = true;
        public string primitiveMeshTypeKey;
        public Color baseColor = Color.white;
    }

    [Serializable]
    public sealed class ViewSettingsData
    {
        public string activeViewMode = "Perspective3D";
        public List<CameraBookmarkStub> cameraBookmarks = new();
    }

    [Serializable]
    public sealed class RuntimeDocumentData
    {
        public List<global::UnitySimulationX.SceneModel.RuntimeBinding> bindings = new();
    }

    [Serializable]
    public sealed class CameraBookmarkStub
    {
        public string name;
        public float[] position;
        public float[] rotation;
    }

    [Serializable]
    public sealed class DiagnosticMarkerStub
    {
        public string id;
        public string targetObjectId;
        public string severity;
        public string message;
    }
}
