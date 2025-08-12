using System.IO;
using System.IO.Compression;
using GaymController.Diagnostics;
using Xunit;

namespace Gc.Tests {
    public class LogBundlerTests {
        [Fact]
        public void Bundler_Zips_LogFiles() {
            var tempDir = Path.Combine(Path.GetTempPath(), "gc_logs");
            Directory.CreateDirectory(tempDir);
            File.WriteAllText(Path.Combine(tempDir, "a.log"), "hello");
            var bundler = new LogBundler();
            var zipPath = Path.Combine(Path.GetTempPath(), "bundle.zip");
            bundler.CreateBundle(zipPath, tempDir);
            using var zip = ZipFile.OpenRead(zipPath);
            Assert.Contains(zip.Entries, e => e.FullName == "logs/a.log");
        }
    }
}
