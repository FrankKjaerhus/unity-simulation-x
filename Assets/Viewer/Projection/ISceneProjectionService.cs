using UnityEngine;
using UnitySimulationX.Editing;
using UnitySimulationX.SceneModel;

namespace UnitySimulationX.Viewer.Projection
{
    public interface ISceneProjectionService : ISceneProjectionWriter
    {
        Transform SceneRoot { get; }
        GameObject GetGameObject(string objectId);
        string GetObjectId(GameObject gameObject);
        void RegisterExistingTarget(string objectId, GameObject target);
        void PreviewTransform(string objectId, TransformData transform);
    }
}
