using System.IO;
using System.IO.Compression;

namespace GaymController.Diagnostics {
    /// <summary>Bundles plain log files and ETW traces into a single archive.</summary>
    public sealed class LogBundler : ILogBundler {
        public string CreateBundle(string outputPath, string logDirectory, string? etwDirectory = null) {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            if (File.Exists(outputPath)) File.Delete(outputPath);
            using var zip = ZipFile.Open(outputPath, ZipArchiveMode.Create);
            if (Directory.Exists(logDirectory))
                foreach (var file in Directory.GetFiles(logDirectory, "*", SearchOption.AllDirectories))
                    zip.CreateEntryFromFile(file, Path.Combine("logs", Path.GetFileName(file)));
            if (!string.IsNullOrEmpty(etwDirectory) && Directory.Exists(etwDirectory))
                foreach (var file in Directory.GetFiles(etwDirectory, "*.etl", SearchOption.AllDirectories))
                    zip.CreateEntryFromFile(file, Path.Combine("etw", Path.GetFileName(file)));
            return outputPath;
        }
    }
}
