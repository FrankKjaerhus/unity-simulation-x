using System;
using System.Collections.Generic;
using UnityEngine;
using UnitySimulationX.SceneModel;

namespace UnitySimulationX.SceneModel.Serialization
{
    [Serializable]
    public sealed class ProjectSchemaV1Document
    {
        public string version = "1.0";
        public int schemaVersion = 1;
        public ProjectSchemaV1SceneDocumentData scene = new();
        public ViewSettingsData viewSettings = new();
        public ProjectSchemaV1RuntimeDocumentData runtime = new();
        public List<ProjectSchemaV1DiagnosticMarkerStub> diagnostics = new();
    }

    [Serializable]
    public sealed class ProjectSchemaV1SceneDocumentData
    {
        public List<ProjectSchemaV1SceneObjectDocumentData> objects = new();
    }

    [Serializable]
    public sealed class ProjectSchemaV1SceneObjectDocumentData
    {
        public string id;
        public string name;
        public string type;
        public string parentId;
        public List<string> childrenIds = new();
        public TransformData transform = new();
        public bool visible = true;
        public string primitiveMeshTypeKey;
        public Color baseColor = Color.white;
    }

    [Serializable]
    public sealed class ProjectSchemaV1RuntimeDocumentData
    {
        public List<ProjectSchemaV1RuntimeBindingStub> bindings = new();
    }

    [Serializable]
    public sealed class ProjectSchemaV1RuntimeBindingStub
    {
        public string targetObjectId;
        public string targetProperty;
        public string sourceName;
        public string address;
        public string mode;
    }

    [Serializable]
    public sealed class ProjectSchemaV1DiagnosticMarkerStub
    {
        public string id;
        public string targetObjectId;
        public string severity;
        public string message;
    }
}
