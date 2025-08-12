using System;
using System.IO;
using System.Text.RegularExpressions;

namespace WootMouseRemap.Utilities
{
    /// <summary>
    /// Input validation and sanitization utilities
    /// </summary>
    public static class ValidationHelper
    {
        private static readonly Regex FileNameRegex = new(@"^[a-zA-Z0-9_\-\.\s]+$", RegexOptions.Compiled);
        private static readonly Regex ProfileNameRegex = new(@"^[a-zA-Z0-9_\-\s]{1,50}$", RegexOptions.Compiled);

        /// <summary>
        /// Validate and sanitize a filename
        /// </summary>
        public static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("Filename cannot be empty", nameof(fileName));

            // Remove invalid characters
            var invalidChars = Path.GetInvalidFileNameChars();
            foreach (var c in invalidChars)
            {
                fileName = fileName.Replace(c, '_');
            }

            // Limit length
            if (fileName.Length > 100)
                fileName = fileName.Substring(0, 100);

            // Ensure it's not a reserved name
            var reservedNames = new[] { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" };
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName).ToUpperInvariant();
            
            if (Array.Exists(reservedNames, name => name == nameWithoutExtension))
                fileName = "_" + fileName;

            return fileName;
        }

        /// <summary>
        /// Validate a profile name
        /// </summary>
        public static bool IsValidProfileName(string profileName)
        {
            return !string.IsNullOrWhiteSpace(profileName) && 
                   ProfileNameRegex.IsMatch(profileName) &&
                   profileName.Trim().Length > 0;
        }

        /// <summary>
        /// Validate numeric range
        /// </summary>
        public static T ClampValue<T>(T value, T min, T max) where T : IComparable<T>
        {
            if (value.CompareTo(min) < 0) return min;
            if (value.CompareTo(max) > 0) return max;
            return value;
        }

        /// <summary>
        /// Validate and clamp percentage (0-100)
        /// </summary>
        public static double ClampPercentage(double value)
        {
            return ClampValue(value, 0.0, 100.0);
        }

        /// <summary>
        /// Validate and clamp opacity (0.0-1.0)
        /// </summary>
        public static double ClampOpacity(double value)
        {
            return ClampValue(value, 0.0, 1.0);
        }

        /// <summary>
        /// Validate coordinate values
        /// </summary>
        public static bool IsValidCoordinate(int x, int y)
        {
            // Allow reasonable coordinate ranges (including negative for multi-monitor setups)
            return x >= -10000 && x <= 10000 && y >= -10000 && y <= 10000;
        }

        /// <summary>
        /// Validate window size
        /// </summary>
        public static (int width, int height) ValidateWindowSize(int width, int height)
        {
            const int minSize = 200;
            const int maxSize = 4000;

            width = ClampValue(width, minSize, maxSize);
            height = ClampValue(height, minSize, maxSize);

            return (width, height);
        }

        /// <summary>
        /// Validate file path for security
        /// </summary>
        public static bool IsValidFilePath(string path, string allowedDirectory)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            try
            {
                var fullPath = Path.GetFullPath(path);
                var allowedPath = Path.GetFullPath(allowedDirectory);
                
                // Ensure the path is within the allowed directory
                return fullPath.StartsWith(allowedPath, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validate color hex string
        /// </summary>
        public static bool IsValidHexColor(string hexColor)
        {
            if (string.IsNullOrWhiteSpace(hexColor))
                return false;

            if (!hexColor.StartsWith("#"))
                return false;

            if (hexColor.Length != 7 && hexColor.Length != 9) // #RRGGBB or #AARRGGBB
                return false;

            return Regex.IsMatch(hexColor.Substring(1), @"^[0-9A-Fa-f]+$");
        }

        /// <summary>
        /// Sanitize user input for logging
        /// </summary>
        public static string SanitizeForLogging(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Remove potential log injection characters
            return input.Replace('\r', ' ')
                       .Replace('\n', ' ')
                       .Replace('\t', ' ')
                       .Trim();
        }

        /// <summary>
        /// Validate timeout values
        /// </summary>
        public static TimeSpan ValidateTimeout(TimeSpan timeout, TimeSpan min, TimeSpan max)
        {
            if (timeout < min) return min;
            if (timeout > max) return max;
            return timeout;
        }

        /// <summary>
        /// Validate port number
        /// </summary>
        public static bool IsValidPort(int port)
        {
            return port >= 1 && port <= 65535;
        }
    }
}