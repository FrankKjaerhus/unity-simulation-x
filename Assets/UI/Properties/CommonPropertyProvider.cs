using System;
using System.Collections.Generic;
using UnityEngine;
using UnitySimulationX.Editing;
using UnitySimulationX.SceneModel;

namespace UnitySimulationX.UI.Properties
{
    public sealed class CommonPropertyProvider : IPropertyProvider
    {
        public string ProviderId => "common";
        public int Order => 0;

        public bool Supports(SceneObjectModel snapshot) => snapshot != null;

        public IEnumerable<PropertyDescriptor> GetProperties(SceneObjectModel snapshot, ISceneEditService edits)
        {
            if (snapshot == null || edits == null)
                yield break;

            yield return new PropertyDescriptor
            {
                Key = "name",
                DisplayName = "Name",
                Category = "Common",
                ValueType = typeof(string),
                Value = snapshot.Name,
                Apply = value => ApplyName(snapshot, edits, value)
            };

            yield return new PropertyDescriptor
            {
                Key = "type",
                DisplayName = "Type",
                Category = "Common",
                ValueType = typeof(string),
                Value = snapshot.TypeId.Value,
                IsReadOnly = true
            };

            yield return new PropertyDescriptor
            {
                Key = "position",
                DisplayName = "Position",
                Category = "Transform",
                ValueType = typeof(Vector3),
                Value = snapshot.Transform?.Position ?? Vector3.zero,
                Apply = value => ApplyTransform(snapshot, edits, value, (transform, vector) => transform.Position = vector)
            };

            yield return new PropertyDescriptor
            {
                Key = "rotation",
                DisplayName = "Rotation",
                Category = "Transform",
                ValueType = typeof(Vector3),
                Value = snapshot.Transform?.RotationEuler ?? Vector3.zero,
                Apply = value => ApplyTransform(snapshot, edits, value, (transform, vector) => transform.RotationEuler = vector)
            };

            yield return new PropertyDescriptor
            {
                Key = "scale",
                DisplayName = "Scale",
                Category = "Transform",
                ValueType = typeof(Vector3),
                Value = snapshot.Transform?.Scale ?? Vector3.one,
                Apply = value => ApplyTransform(snapshot, edits, value, (transform, vector) => transform.Scale = vector)
            };

            yield return new PropertyDescriptor
            {
                Key = "visible",
                DisplayName = "Visibility",
                Category = "Common",
                ValueType = typeof(bool),
                Value = snapshot.Visible,
                Apply = value => ApplyVisible(snapshot, edits, value)
            };
        }

        static SceneEditResult ApplyName(SceneObjectModel snapshot, ISceneEditService edits, object value)
        {
            if (value is not string name)
                return InvalidValue(nameof(value), typeof(string));

            return edits.Rename(snapshot.Id, name);
        }

        static SceneEditResult ApplyVisible(SceneObjectModel snapshot, ISceneEditService edits, object value)
        {
            if (value is not bool visible)
                return InvalidValue(nameof(value), typeof(bool));

            return edits.SetVisible(snapshot.Id, visible);
        }

        static SceneEditResult ApplyTransform(
            SceneObjectModel snapshot,
            ISceneEditService edits,
            object value,
            Action<TransformData, Vector3> applyVector)
        {
            if (value is not Vector3 vector)
                return InvalidValue(nameof(value), typeof(Vector3));

            var transform = snapshot.Transform?.Clone() ?? new TransformData();
            applyVector(transform, vector);
            return edits.SetTransform(snapshot.Id, transform);
        }

        static SceneEditResult InvalidValue(string parameterName, Type expectedType)
        {
            return new SceneEditResult
            {
                Succeeded = false,
                ErrorCode = "properties.value.invalid",
                Message = $"Expected property value of type {expectedType.Name} for {parameterName}."
            };
        }
    }
}
