using System.Collections.Generic;
using UnitySimulationX.SceneModel;

namespace UnitySimulationX.Editing
{
    public interface ISceneProjectionWriter
    {
        void CreateProjection(SceneObjectModel snapshot);
        void UpdateProjection(SceneObjectModel snapshot);
        void RemoveProjection(string objectId);
        void ReplaceAllProjections(IReadOnlyList<SceneObjectModel> snapshots);
    }
}
