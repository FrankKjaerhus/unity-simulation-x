using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnitySimulationX.Core;
using UnitySimulationX.Editing;
using UnitySimulationX.SceneModel.Serialization;
using UnityEngine;

namespace UnitySimulationX.App.ProjectSystem
{
    public sealed class ProjectPersistenceService : IProjectPersistenceService
    {
        readonly ISceneEditService _edits;
        readonly IProjectWorkspace _workspace;
        readonly ProjectDocumentValidator _validator;

        public ProjectPersistenceService(
            ISceneEditService edits,
            IProjectWorkspace workspace,
            ProjectDocumentValidator validator = null)
        {
            _edits = edits ?? throw new ArgumentNullException(nameof(edits));
            _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
            _validator = validator ?? new ProjectDocumentValidator();
        }

        public string CurrentProjectRoot => _workspace.RootPath;

        public Task<ProjectOperationResult> SaveAsync(CancellationToken cancellationToken)
        {
            return SaveInternalAsync(_workspace.RootPath, updateWorkspaceRoot: false, cancellationToken);
        }

        public Task<ProjectOperationResult> SaveAsAsync(
            string projectRoot,
            CancellationToken cancellationToken)
        {
            return SaveInternalAsync(projectRoot, updateWorkspaceRoot: true, cancellationToken);
        }

        public async Task<ProjectOperationResult> LoadAsync(
            string projectRoot,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(projectRoot))
                return Failure("project.load.root.required", "Project root is required.");

            try
            {
                var documentPath = ProjectPaths.DocumentPath(projectRoot);
                if (!File.Exists(documentPath))
                {
                    return Failure(
                        "project.load.document.missing",
                        $"Project document not found at '{documentPath}'.");
                }

                var json = await ReadAllTextAsync(documentPath, cancellationToken);
                var document = ProjectSchemaMigrator.DecodeAndMigrate(json);
                var issues = _validator.Validate(document, projectRoot);
                if (HasErrors(issues))
                    return ResultFromIssues(issues);

                var snapshots = ProjectSerializer.CreateSnapshots(document);
                var replaceResult = _edits.ReplaceScene(snapshots);
                if (!replaceResult.Succeeded)
                {
                    return Failure(
                        replaceResult.ErrorCode ?? "project.load.scene-replace.failed",
                        replaceResult.Message ?? "Scene replacement failed.");
                }

                _workspace.UsePersistentRoot(projectRoot);
                return ResultFromIssues(issues);
            }
            catch (ProjectFormatException ex)
            {
                return Failure("project.load.format.invalid", ex.Message);
            }
            catch (Exception ex)
            {
                return Failure("project.load.failed", ex.Message);
            }
        }

        async Task<ProjectOperationResult> SaveInternalAsync(
            string projectRoot,
            bool updateWorkspaceRoot,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(projectRoot))
                return Failure("project.save.root.required", "Project root is required.");

            try
            {
                var document = ProjectSerializer.CreateDocument(
                    _edits.Registry,
                    Array.Empty<ProjectAssetDocumentData>());
                var issues = _validator.Validate(document, projectRoot);
                if (HasErrors(issues))
                    return ResultFromIssues(issues);

                var json = JsonUtility.ToJson(document, prettyPrint: true);
                await ProjectFileWriter.WriteAtomicAsync(
                    ProjectPaths.DocumentPath(projectRoot),
                    json,
                    cancellationToken);

                if (updateWorkspaceRoot)
                    _workspace.UsePersistentRoot(projectRoot);

                return ResultFromIssues(issues);
            }
            catch (Exception ex)
            {
                return Failure("project.save.failed", ex.Message);
            }
        }

        static async Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken)
        {
            using var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                4096,
                useAsync: true);
            using var reader = new StreamReader(stream);

            cancellationToken.ThrowIfCancellationRequested();
            return await reader.ReadToEndAsync();
        }

        static bool HasErrors(IReadOnlyList<ProjectIssue> issues)
        {
            if (issues == null)
                return false;

            foreach (var issue in issues)
            {
                if (issue.IsError)
                    return true;
            }

            return false;
        }

        static ProjectOperationResult ResultFromIssues(IReadOnlyList<ProjectIssue> issues)
        {
            var issueList = issues ?? Array.Empty<ProjectIssue>();
            return new ProjectOperationResult
            {
                Succeeded = !HasErrors(issueList),
                Issues = issueList
            };
        }

        static ProjectOperationResult Failure(string code, string message) =>
            new()
            {
                Succeeded = false,
                Issues = new List<ProjectIssue>
                {
                    new()
                    {
                        Code = code,
                        Message = message,
                        IsError = true
                    }
                }
            };
        }
    }
}
