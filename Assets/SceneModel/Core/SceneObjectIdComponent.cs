using UnityEngine;

namespace UnitySimulationX.SceneModel
{
    /// <summary>
    /// Stores the domain object id on a runtime GameObject.
    /// </summary>
    public sealed class SceneObjectIdComponent : MonoBehaviour
    {
        [SerializeField] string sceneObjectId;

        public string SceneObjectId
        {
            get => sceneObjectId;
            set => sceneObjectId = value;
        }
    }
}
