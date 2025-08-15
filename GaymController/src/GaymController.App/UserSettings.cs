using System;
using System.IO;
using System.Text.Json;

namespace GaymController.App {
    public class UserSettings {
        private const string FileName = "user_settings.json";
        public bool IsFirstRun { get; set; } = true;
        public bool IsCalibrated { get; set; } = false;
        public CalibrationData? Calibration { get; set; }
        public static UserSettings Load(){
            try{
                var path = GetPath();
                if(File.Exists(path)){
                    var json = File.ReadAllText(path);
                    var opts = new JsonSerializerOptions{PropertyNameCaseInsensitive=true};
                    var loaded = JsonSerializer.Deserialize<UserSettings>(json, opts);
                    if(loaded!=null) return loaded;
                }
            }catch{}
            return new UserSettings();
        }
        public void Save(){
            try{
                var path = GetPath();
                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions{WriteIndented=true});
                File.WriteAllText(path, json);
            }catch{}
        }
        private static string GetPath(){
            var dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(dir, FileName);
        }
    }

    public struct CalibrationData {
        public float OffsetX { get; set; }
        public float OffsetY { get; set; }
    }
}
