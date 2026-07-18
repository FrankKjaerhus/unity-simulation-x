using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UnitySimulationX.App.ProjectSystem
{
    public static class ProjectFileWriter
    {
        static readonly UTF8Encoding Utf8WithoutBom = new(false);

        public static async Task WriteAtomicAsync(
            string path,
            string content,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path is required.", nameof(path));

            var directory = Path.GetDirectoryName(path);
            if (string.IsNullOrWhiteSpace(directory))
                throw new ArgumentException("Path must include a parent directory.", nameof(path));

            Directory.CreateDirectory(directory);

            var temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";

            try
            {
                var bytes = Utf8WithoutBom.GetBytes(content ?? string.Empty);
                using (var stream = new FileStream(
                           temporaryPath,
                           FileMode.CreateNew,
                           FileAccess.Write,
                           FileShare.None,
                           4096,
                           useAsync: true))
                {
                    await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
                    await stream.FlushAsync(cancellationToken);
                    stream.Flush(flushToDisk: true);
                }

                if (File.Exists(path))
                    File.Replace(temporaryPath, path, destinationBackupFileName: null);
                else
                    File.Move(temporaryPath, path);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }
    }
}
