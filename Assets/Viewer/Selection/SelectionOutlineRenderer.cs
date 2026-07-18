using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnitySimulationX.SceneModel;
using UnitySimulationX.Viewer.Projection;
using UnitySimulationX.Viewer.Settings;

namespace UnitySimulationX.Viewer.Selection
{
    /// <summary>
    /// Draws a visible shell outline around selected scene objects.
    /// </summary>
    public static class SelectionOutlineRenderer
    {
        const float MinEffectiveOutlineScale = 1.02f;

        public enum OutlineStyle
        {
            Selected,
            Grabbing
        }

        static readonly List<GameObject> OutlineObjects = new();

        public static void ApplyOutline(
            ISceneProjectionService projection,
            IReadOnlyList<string> selectedIds,
            ViewerPresentationSettings settings,
            OutlineStyle style = OutlineStyle.Selected)
        {
            Clear();

            var material = style == OutlineStyle.Grabbing
                ? CreateOutlineMaterial(GetGrabbingColor(settings))
                : CreateOutlineMaterial(GetSelectedColor(settings));

            foreach (var id in selectedIds)
            {
                var go = projection.GetGameObject(id);
                if (go == null)
                    continue;

                foreach (var meshFilter in go.GetComponentsInChildren<MeshFilter>())
                {
                    if (meshFilter.sharedMesh == null || meshFilter.gameObject.name == "SelectionOutline")
                        continue;

                    CreateOutline(meshFilter, material, settings);
                }
            }
        }

        public static void Clear()
        {
            for (var i = 0; i < OutlineObjects.Count; i++)
            {
                if (OutlineObjects[i] != null)
                    Object.Destroy(OutlineObjects[i]);
            }

            OutlineObjects.Clear();
        }

        static void CreateOutline(MeshFilter source, Material material, ViewerPresentationSettings settings)
        {
            var outlineGo = new GameObject("SelectionOutline");
            outlineGo.transform.SetParent(source.transform, false);
            outlineGo.transform.localPosition = Vector3.zero;
            outlineGo.transform.localRotation = Quaternion.identity;
            outlineGo.transform.localScale = Vector3.one * GetOutlineScale(source.sharedMesh, settings);
            outlineGo.layer = source.gameObject.layer;

            var meshFilter = outlineGo.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = source.sharedMesh;

            var meshRenderer = outlineGo.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.allowOcclusionWhenDynamic = false;

            OutlineObjects.Add(outlineGo);
        }

        static float GetOutlineScale(Mesh mesh, ViewerPresentationSettings settings)
        {
            var baseScale = settings != null ? settings.outlineScale : 1.05f;
            var smallOffset = settings != null ? settings.smallMeshScaleOffset : 0.02f;
            var largeOffset = settings != null ? settings.largeMeshScaleOffset : -0.02f;

            baseScale = Mathf.Max(baseScale, MinEffectiveOutlineScale);

            var extent = mesh.bounds.extents.magnitude;
            var scale = extent switch
            {
                < 0.75f => baseScale + smallOffset,
                < 3f => baseScale,
                _ => baseScale + largeOffset
            };

            return Mathf.Max(scale, MinEffectiveOutlineScale);
        }

        static Color GetSelectedColor(ViewerPresentationSettings settings)
        {
            return settings != null ? settings.selectedOutlineColor : new Color(1f, 0.55f, 0.05f, 1f);
        }

        static Color GetGrabbingColor(ViewerPresentationSettings settings)
        {
            return settings != null ? settings.grabbingOutlineColor : Color.white;
        }

        static Material CreateOutlineMaterial(Color color)
        {
            var shader = Shader.Find("UnitySimulationX/SelectionOutline")
                         ?? Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Unlit/Color");

            var material = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);

            return material;
        }
    }
}
