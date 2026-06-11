using UnityEngine;

namespace UnitySimulationX.Viewer.Settings
{
    [CreateAssetMenu(
        fileName = "ViewerPresentationSettings",
        menuName = "Unity Simulation X/Viewer Presentation Settings")]
    public sealed class ViewerPresentationSettings : ScriptableObject
    {
        [Header("Floor Grid")]
        [Min(1)] public int halfLineCount = 20;
        [Min(0.01f)] public float spacing = 1f;
        [Min(1)] public int majorLineEvery = 5;
        [Range(0.5f, 4f)] public float lineWidth = 1.35f;
        public Color axisXColor = new(0.86f, 0.27f, 0.27f, 1f);
        public Color axisYColor = new(0.29f, 0.78f, 0.35f, 1f);
        public Color majorLineColor = new(0.42f, 0.42f, 0.42f, 0.8f);
        public Color minorLineColor = new(0.28f, 0.28f, 0.28f, 0.45f);
        public bool distanceFade = true;
        [Range(0f, 1f)] public float fadeStart = 0.35f;

        [Header("Selection Outline")]
        public Color selectedOutlineColor = new(1f, 0.55f, 0.05f, 1f);
        public Color grabbingOutlineColor = Color.white;
        [Min(1.01f)] public float outlineScale = 1.05f;
        [Range(-0.1f, 0.1f)] public float smallMeshScaleOffset = 0.02f;
        [Range(-0.1f, 0.1f)] public float largeMeshScaleOffset = -0.02f;

        void OnValidate()
        {
            outlineScale = Mathf.Max(outlineScale, 1.01f);
        }
    }
}
