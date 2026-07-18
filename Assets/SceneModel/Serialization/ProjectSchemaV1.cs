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
        public RuntimeDocumentData runtime = new();
        public List<DiagnosticMarkerStub> diagnostics = new();
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
}
