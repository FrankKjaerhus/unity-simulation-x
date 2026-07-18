using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnitySimulationX.Editing;
using UnitySimulationX.SceneModel;
using UnitySimulationX.UI.Hierarchy;
using UnitySimulationX.UI.Properties;

namespace UnitySimulationX.Tests.EditMode
{
    public sealed class ContributionRegistryTests
    {
        [Test]
        public void PropertyProviderRegistry_ReturnsAllSupportingProvidersInOrder()
        {
            var registry = new PropertyProviderRegistry();
            registry.Register(new FakeProvider("common", 0));
            registry.Register(new FakeProvider("vendor", 10));
            registry.Freeze();

            CollectionAssert.AreEqual(
                new[] { "common", "vendor" },
                registry.GetProviders(Model()).Select(provider => provider.ProviderId));
        }

        [Test]
        public void SceneTypeDescriptorRegistry_RejectsDuplicateTypeId()
        {
            var registry = new SceneTypeDescriptorRegistry();
            registry.Register(new SceneTypeDescriptor(SceneObjectTypeIds.Group, "Group", "GR"));

            Assert.Throws<InvalidOperationException>(() =>
                registry.Register(new SceneTypeDescriptor(SceneObjectTypeIds.Group, "Duplicate", "XX")));
        }

        [Test]
        public void FrozenRegistry_RejectsLateRegistration()
        {
            var registry = new SceneComponentCodecRegistry();
            registry.Freeze();

            Assert.Throws<InvalidOperationException>(() =>
                registry.Register(new FakeComponentCodec()));
        }

        static SceneObjectModel Model()
        {
            return new SceneObjectModel
            {
                Id = "object",
                Name = "Object",
                TypeId = SceneObjectTypeIds.Group
            };
        }

        sealed class FakeProvider : IPropertyProvider
        {
            public FakeProvider(string providerId, int order)
            {
                ProviderId = providerId;
                Order = order;
            }

            public string ProviderId { get; }
            public int Order { get; }

            public bool Supports(SceneObjectModel snapshot) => snapshot != null;

            public IEnumerable<PropertyDescriptor> GetProperties(
                SceneObjectModel snapshot,
                ISceneEditService edits)
            {
                return Array.Empty<PropertyDescriptor>();
            }
        }

        sealed class FakeComponentCodec : ISceneComponentCodec
        {
            public string ComponentTypeId => "com.vendor.product.fake-component";
            public int CurrentSchemaVersion => 1;
            public Type ComponentClrType => typeof(FakeComponent);

            public object Decode(SceneComponentData data) => new FakeComponent();

            public SceneComponentData Encode(object component) =>
                new(ComponentTypeId, CurrentSchemaVersion, "{}");
        }

        sealed class FakeComponent
        {
        }
    }
}
