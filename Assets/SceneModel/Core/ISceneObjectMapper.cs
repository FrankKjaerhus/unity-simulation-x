using UnityEngine;

namespace UnitySimulationX.SceneModel
{
    public interface ISceneObjectMapper
    {
        GameObject CreateGameObject(SceneObjectModel model);
        GameObject RegisterExistingGameObject(SceneObjectModel model, GameObject target);
        void UpdateGameObject(SceneObjectModel model, GameObject target);
        void DestroyGameObject(string sceneObjectId);
        GameObject GetGameObject(string sceneObjectId);
        SceneObjectModel GetModel(GameObject gameObject);
        Transform SceneRoot { get; }
    }
}
