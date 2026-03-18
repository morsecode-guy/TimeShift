using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TimeShift
{
    public static class StateFileHelper
    {
        // Build a filename like "MyPlane_3_250115143022.ts"
        public static string MakeFileName(Il2Cpp.Craft craft, string statesFolder)
        {
            string planeName = SanitizeFileName(craft.vName ?? "Unknown");
            int number = GetNextSaveNumber(planeName, statesFolder);
            string date = DateTime.Now.ToString("yyMMddHHmmss");
            return $"{planeName}_{number}_{date}.ts";
        }

        // Find the next save number for this plane name
        public static int GetNextSaveNumber(string planeName, string statesFolder)
        {
            if (!Directory.Exists(statesFolder)) return 1;

            var existing = Directory.GetFiles(statesFolder, $"{planeName}_*.ts");
            if (existing.Length == 0) return 1;

            int max = 0;
            foreach (var f in existing)
            {
                string name = Path.GetFileNameWithoutExtension(f);
                var parts = name.Split('_');
                if (parts.Length >= 2 && int.TryParse(parts[1], out int num))
                {
                    if (num > max) max = num;
                }
            }
            return max + 1;
        }

        // Replace unsafe characters for filenames
        public static string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name.Replace(' ', '-');
        }

        // Load all state files from disk, newest first
        public static List<TimeStateEntry> RefreshStateList(string statesFolder)
        {
            var entries = new List<TimeStateEntry>();
            if (!Directory.Exists(statesFolder)) return entries;

            var files = Directory.GetFiles(statesFolder, "*.ts")
                .OrderByDescending(f => File.GetCreationTime(f))
                .ToArray();

            foreach (var file in files)
            {
                try
                {
                    string data = File.ReadAllText(file);
                    var state = TimeState.Deserialize(data);
                    entries.Add(new TimeStateEntry
                    {
                        FileName = Path.GetFileName(file),
                        FilePath = file,
                        DisplayName = state.CraftName,
                        Timestamp = state.Timestamp
                    });
                }
                catch
                {
                    entries.Add(new TimeStateEntry
                    {
                        FileName = Path.GetFileName(file),
                        FilePath = file,
                        DisplayName = Path.GetFileNameWithoutExtension(file),
                        Timestamp = "?"
                    });
                }
            }

            return entries;
        }
    }
}
