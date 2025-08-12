using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using WootMouseRemap.Diagnostics;
using WootMouseRemap.Utilities;

namespace WootMouseRemap
{
    public sealed class ProfileManager
    {
        private readonly string _dir = "Profiles";
        private readonly string _historyDir = Path.Combine("Profiles", "_history");
        private readonly string _backupDir = Path.Combine("Backups", "Profiles");
        private const int MaxHistoryFiles = 50;
        private const int MaxBackupFiles = 20;
        
        public ProfileSchemaV1 Current { get; private set; } = new();
        public string CurrentPath { get; private set; } = Path.Combine("Profiles", "default.json");

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter(allowIntegerValues: true) }
        };

        public ProfileManager()
        {
            InitializeDirectories();
            if (!File.Exists(CurrentPath)) Save();
            Load(CurrentPath);
        }

        private void InitializeDirectories()
        {
            ExceptionHandler.SafeExecute(() => Directory.CreateDirectory(_dir), "creating profiles directory");
            ExceptionHandler.SafeExecute(() => Directory.CreateDirectory(_historyDir), "creating history directory");
            ExceptionHandler.SafeExecute(() => Directory.CreateDirectory(_backupDir), "creating backup directory");
        }

        public IEnumerable<string> ListProfiles()
        {
            foreach (var f in Directory.GetFiles(_dir, "*.json", SearchOption.TopDirectoryOnly))
                if (!f.Contains("_history")) yield return f;
        }

        public void Load(string path)
        {
            try
            {
                Current = JsonSerializer.Deserialize<ProfileSchemaV1>(File.ReadAllText(path), _jsonOptions) ?? new ProfileSchemaV1();
                CurrentPath = path;
                // Migration: ensure new nullable fields have defaults
                if (Current.CurrentAppMode == null)
                {
                    Current.CurrentAppMode = AppMode.KbmToPad;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Profile load failed", ex);
                Current = new ProfileSchemaV1();
            }
        }

        public void Save()
        {
            ExceptionHandler.SafeExecute(() =>
            {
                // Validate profile before saving
                if (!ValidateProfile(Current))
                {
                    Logger.Warn("Profile validation failed, using defaults for invalid values");
                    SanitizeProfile(Current);
                }

                var json = JsonSerializer.Serialize(Current, _jsonOptions);
                
                // Create backup before overwriting
                CreateBackup(CurrentPath);
                
                // Write to temporary file first, then move (atomic operation)
                var tempPath = CurrentPath + ".tmp";
                File.WriteAllText(tempPath, json);
                File.Move(tempPath, CurrentPath, overwrite: true);
                
                // Save to history
                SaveToHistory(json);
                
                // Cleanup old files
                CleanupOldFiles();
                
                Logger.Debug($"Profile saved: {Path.GetFileName(CurrentPath)}");
            }, "saving profile");
        }

        private void CreateBackup(string profilePath)
        {
            if (!File.Exists(profilePath)) return;

            ExceptionHandler.SafeExecute(() =>
            {
                var fileName = Path.GetFileNameWithoutExtension(profilePath);
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupPath = Path.Combine(_backupDir, $"{fileName}_backup_{timestamp}.json");
                
                File.Copy(profilePath, backupPath, overwrite: true);
                Logger.Debug($"Profile backup created: {Path.GetFileName(backupPath)}");
            }, "creating profile backup");
        }

        private void SaveToHistory(string json)
        {
            ExceptionHandler.SafeExecute(() =>
            {
                var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var historyPath = Path.Combine(_historyDir, $"{Path.GetFileNameWithoutExtension(CurrentPath)}_{stamp}.json");
                File.WriteAllText(historyPath, json);
            }, "saving to history");
        }

        private void CleanupOldFiles()
        {
            // Cleanup history files
            ExceptionHandler.SafeExecute(() =>
            {
                var historyFiles = Directory.GetFiles(_historyDir, "*.json")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTime)
                    .Skip(MaxHistoryFiles)
                    .ToArray();

                foreach (var file in historyFiles)
                {
                    file.Delete();
                }

                if (historyFiles.Length > 0)
                {
                    Logger.Debug($"Cleaned up {historyFiles.Length} old history files");
                }
            }, "cleaning up history files");

            // Cleanup backup files
            ExceptionHandler.SafeExecute(() =>
            {
                var backupFiles = Directory.GetFiles(_backupDir, "*_backup_*.json")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTime)
                    .Skip(MaxBackupFiles)
                    .ToArray();

                foreach (var file in backupFiles)
                {
                    file.Delete();
                }

                if (backupFiles.Length > 0)
                {
                    Logger.Debug($"Cleaned up {backupFiles.Length} old backup files");
                }
            }, "cleaning up backup files");
        }

        public string Create(string nameLike)
        {
            string name = Sanitize(nameLike);
            string path = Path.Combine(_dir, $"{name}.json");
            int i = 1;
            while (File.Exists(path)) { path = Path.Combine(_dir, $"{name}_{i}.json"); i++; }
            var p = new ProfileSchemaV1 { Name = Path.GetFileNameWithoutExtension(path) };
            File.WriteAllText(path, JsonSerializer.Serialize(p, _jsonOptions));
            return path;
        }

        public string CloneCurrent(string nameLike)
        {
            string path = Create(nameLike);
            File.WriteAllText(path, JsonSerializer.Serialize(Current, _jsonOptions));
            return path;
        }

        public void Delete(string path)
        {
            if (File.Exists(path) && !path.Equals(CurrentPath, StringComparison.OrdinalIgnoreCase)) File.Delete(path);
        }

        private static string Sanitize(string s)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
            return string.IsNullOrWhiteSpace(s) ? "profile" : s.Trim();
        }

        private bool ValidateProfile(ProfileSchemaV1 profile)
        {
            try
            {
                // Basic validation - just check if profile is not null and has a name
                return profile != null && !string.IsNullOrWhiteSpace(profile.Name);
            }
            catch (Exception ex)
            {
                Logger.Error("Error validating profile", ex);
                return false;
            }
        }

        private void SanitizeProfile(ProfileSchemaV1 profile)
        {
            // Basic sanitization - ensure profile has a valid name
            if (string.IsNullOrWhiteSpace(profile.Name))
                profile.Name = "default";

            Logger.Debug("Profile sanitized with valid values");
        }

        public bool RestoreFromBackup(string backupFileName = "")
        {
            return ExceptionHandler.SafeExecute(() =>
            {
                string backupPath;
                
                if (string.IsNullOrEmpty(backupFileName))
                {
                    // Get the most recent backup
                    var backupFiles = Directory.GetFiles(_backupDir, "*_backup_*.json")
                        .Select(f => new FileInfo(f))
                        .OrderByDescending(f => f.CreationTime)
                        .FirstOrDefault();

                    if (backupFiles == null)
                    {
                        Logger.Warn("No backup files found");
                        return false;
                    }

                    backupPath = backupFiles.FullName;
                }
                else
                {
                    backupPath = Path.Combine(_backupDir, backupFileName);
                    if (!File.Exists(backupPath))
                    {
                        Logger.Error($"Backup file not found: {backupFileName}");
                        return false;
                    }
                }

                // Load and validate backup
                var backupContent = File.ReadAllText(backupPath);
                var backupProfile = JsonSerializer.Deserialize<ProfileSchemaV1>(backupContent, _jsonOptions);
                
                if (backupProfile == null)
                {
                    Logger.Error("Failed to deserialize backup profile");
                    return false;
                }

                // Validate backup profile
                if (!ValidateProfile(backupProfile))
                {
                    Logger.Warn("Backup profile validation failed, sanitizing...");
                    SanitizeProfile(backupProfile);
                }

                // Restore the profile
                Current = backupProfile;
                Save();

                Logger.Info($"Profile restored from backup: {Path.GetFileName(backupPath)}");
                return true;
            }, false, "restoring from backup");
        }

        public string[] GetAvailableBackups()
        {
            return ExceptionHandler.SafeExecute(() =>
            {
                return Directory.GetFiles(_backupDir, "*_backup_*.json")
                    .Select(Path.GetFileName)
                    .OrderByDescending(f => f)
                    .ToArray();
            }, Array.Empty<string>(), "getting available backups");
        }
    }
}
