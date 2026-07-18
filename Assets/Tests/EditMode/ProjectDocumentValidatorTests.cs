using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnitySimulationX.App.ProjectSystem;
using UnitySimulationX.Core;
using UnitySimulationX.SceneModel.Serialization;

namespace UnitySimulationX.Tests.EditMode
{
    public sealed class ProjectDocumentValidatorTests
    {
        readonly List<string> _roots = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var root in _roots)
            {
                if (Directory.Exists(root))
                    Directory.Delete(root, recursive: true);
            }

            _roots.Clear();
        }

        [Test]
        public void Validate_RejectsDuplicateObjectIds()
        {
            var validator = new ProjectDocumentValidator();
            var root = CreateRoot();
            var document = CreateValidDocument();
            document.scene.objects.Add(new SceneObjectDocumentData
            {
                id = "root",
                name = "Duplicate",
                typeId = "com.unitysimulationx.scene.group"
            });

            var issues = validator.Validate(document, root);

            AssertHasError(issues, "project.scene.object-id.duplicate");
        }

        [Test]
        public void Validate_RejectsMissingParent()
        {
            var validator = new ProjectDocumentValidator();
            var root = CreateRoot();
            var document = CreateValidDocument();
            document.scene.objects[0].parentId = "missing-parent";

            var issues = validator.Validate(document, root);

            AssertHasError(issues, "project.scene.parent.missing");
        }

        [Test]
        public void Validate_RejectsUnknownAssetCatalogId()
        {
            var validator = new ProjectDocumentValidator();
            var root = CreateRoot();
            var document = CreateValidDocument();
            document.scene.objects[0].assetId = "missing-asset";

            var issues = validator.Validate(document, root);

            AssertHasError(issues, "project.assets.reference.missing");
        }

        static ProjectViewerDocument CreateValidDocument()
        {
            var document = new ProjectViewerDocument();
            document.scene.objects.Add(new SceneObjectDocumentData
            {
                id = "root",
                name = "Root",
                typeId = "com.unitysimulationx.scene.group"
            });
            return document;
        }

        static void AssertHasError(IReadOnlyList<ProjectIssue> issues, string code)
        {
            Assert.IsTrue(issues.Count > 0);
            Assert.IsTrue(HasIssue(issues, code, isError: true), $"Expected error code '{code}'.");
        }

        static bool HasIssue(IReadOnlyList<ProjectIssue> issues, string code, bool isError)
        {
            foreach (var issue in issues)
            {
                if (issue.IsError == isError && issue.Code == code)
                    return true;
            }

            return false;
        }

        string CreateRoot()
        {
            var root = Path.Combine(
                Application.temporaryCachePath,
                $"project-validator-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(root);
            _roots.Add(root);
            return root;
        }
    }
}
