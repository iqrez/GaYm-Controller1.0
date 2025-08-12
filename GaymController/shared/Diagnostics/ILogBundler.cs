namespace GaymController.Diagnostics {
    public interface ILogBundler {
        /// <summary>Create a zip bundle containing log files and ETW traces.</summary>
        /// <param name="outputPath">Path to the resulting .zip file.</param>
        /// <param name="logDirectory">Directory containing plain log files.</param>
        /// <param name="etwDirectory">Optional directory containing ETW trace files (.etl).</param>
        /// <returns>Full path to the created bundle.</returns>
        string CreateBundle(string outputPath, string logDirectory, string? etwDirectory = null);
    }
}
