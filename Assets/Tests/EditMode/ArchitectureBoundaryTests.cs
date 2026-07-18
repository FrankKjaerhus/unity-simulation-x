using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnitySimulationX.SceneModel;

namespace UnitySimulationX.Tests.EditMode
{
    public sealed class ArchitectureBoundaryTests
    {
        static readonly System.Type[] ForbiddenUnityTypes =
        {
            typeof(GameObject),
            typeof(Transform),
            typeof(Mesh),
            typeof(Material),
            typeof(Renderer),
            typeof(Shader)
        };

        [Test]
        public void SceneModelAssembly_ContainsNoMonoBehaviours()
        {
            var offenders = typeof(SceneObjectModel).Assembly.GetTypes()
                .Where(type => typeof(MonoBehaviour).IsAssignableFrom(type))
                .Select(type => type.FullName)
                .ToArray();

            CollectionAssert.IsEmpty(offenders);
        }

        [Test]
        public void SceneModelAssembly_ContainsNoGameObjectFieldsOrProperties()
        {
            var offenders = typeof(SceneObjectModel).Assembly.GetTypes()
                .Where(ContainsForbiddenUnityMember)
                .Select(type => type.FullName)
                .ToArray();

            CollectionAssert.IsEmpty(offenders);
        }

        static bool ContainsForbiddenUnityMember(System.Type type)
        {
            const BindingFlags Flags =
                BindingFlags.Instance |
                BindingFlags.Static |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly;

            return type.GetFields(Flags).Any(field => ForbiddenUnityTypes.Contains(field.FieldType)) ||
                   type.GetProperties(Flags).Any(property =>
                       property.GetIndexParameters().Length == 0 &&
                       ForbiddenUnityTypes.Contains(property.PropertyType));
        }
    }
}
