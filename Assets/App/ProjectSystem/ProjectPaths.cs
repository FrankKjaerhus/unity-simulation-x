using System;
using System.IO;
using System.Runtime.InteropServices;

namespace UnitySimulationX.App.ProjectSystem
{
    public static class ProjectPaths
    {
        public const string DocumentFileName = "project.viewer.json";
        public const string ImportedAssetsDirectory = "assets/imported";

        public static string DocumentPath(string rootPath) =>
            ResolveInsideRoot(rootPath, DocumentFileName);

        public static string ImportedPath(string rootPath) =>
            ResolveInsideRoot(rootPath, ImportedAssetsDirectory);

        public static string ResolveInsideRoot(string rootPath, string relativePath)
        {
            var normalizedRoot = NormalizeRoot(rootPath);

            if (string.IsNullOrWhiteSpace(relativePath))
                throw new ArgumentException("Relative path is required.", nameof(relativePath));

            if (Path.IsPathRooted(relativePath))
                throw new ArgumentException("Path must be project-relative.", nameof(relativePath));

            var normalizedRelative = relativePath.Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar);
            if (ContainsParentTraversal(normalizedRelative))
                throw new ArgumentException("Path must not contain parent traversal segments.", nameof(relativePath));

            var absolutePath = Path.GetFullPath(Path.Combine(normalizedRoot, normalizedRelative));

            if (!IsInsideRoot(normalizedRoot, absolutePath))
                throw new ArgumentException("Path resolves outside the project root.", nameof(relativePath));

            return absolutePath;
        }

        public static string MakeRelative(string rootPath, string absolutePath)
        {
            var normalizedRoot = NormalizeRoot(rootPath);

            if (string.IsNullOrWhiteSpace(absolutePath))
                throw new ArgumentException("Absolute path is required.", nameof(absolutePath));

            var normalizedAbsolutePath = Path.GetFullPath(absolutePath);
            if (!IsInsideRoot(normalizedRoot, normalizedAbsolutePath))
                throw new ArgumentException("Absolute path must be inside the project root.", nameof(absolutePath));

            var relativePath = Path.GetRelativePath(normalizedRoot, normalizedAbsolutePath);
            if (string.Equals(relativePath, ".", StringComparison.Ordinal))
                return string.Empty;

            if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
                throw new ArgumentException("Unable to create a safe project-relative path.", nameof(absolutePath));

            if (ContainsParentTraversal(relativePath))
                throw new ArgumentException("Relative path must not contain parent traversal segments.", nameof(absolutePath));

            return relativePath.Replace('\\', '/');
        }

        static string NormalizeRoot(string rootPath)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
                throw new ArgumentException("Project root is required.", nameof(rootPath));

            return Path.GetFullPath(rootPath);
        }

        static bool IsInsideRoot(string rootPath, string absolutePath)
        {
            var normalizedRoot = EnsureTrailingSeparator(rootPath);
            var normalizedAbsolutePath = Path.GetFullPath(absolutePath);
            var comparison = GetPathComparison();

            return string.Equals(
                       normalizedAbsolutePath,
                       rootPath,
                       comparison) ||
                   normalizedAbsolutePath.StartsWith(normalizedRoot, comparison);
        }

        static string EnsureTrailingSeparator(string path)
        {
            if (path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) ||
                path.EndsWith(Path.AltDirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                return path;

            return path + Path.DirectorySeparatorChar;
        }

        static bool ContainsParentTraversal(string relativePath)
        {
            var segments = relativePath.Split(
                new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
                StringSplitOptions.None);

            foreach (var segment in segments)
            {
                if (string.Equals(segment, "..", StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        static StringComparison GetPathComparison() =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
    }
}
