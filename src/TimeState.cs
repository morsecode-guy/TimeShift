using System;
using System.Collections.Generic;
using System.Globalization;

namespace TimeShift
{
    public class TimeStateEntry
    {
        public string FileName;
        public string FilePath;
        public string DisplayName;
        public string Timestamp;
    }

    [Serializable]
    public class TimeState
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public string CraftName;
        public string Timestamp;

        // Rotation
        public float RotX, RotY, RotZ, RotW;

        // Position (game coords are float, real coords are double for floating origin)
        public float GamePosX, GamePosY, GamePosZ;
        public double RealPosX, RealPosY, RealPosZ;

        // Velocity
        public float VelX, VelY, VelZ;
        public double RealVelX, RealVelY, RealVelZ;
        public double StoredVelX, StoredVelY, StoredVelZ;
        public double BodyRelVelX, BodyRelVelY, BodyRelVelZ;

        // Rigidbody velocity
        public float RbVelX, RbVelY, RbVelZ;
        public float RbAngVelX, RbAngVelY, RbAngVelZ;
        public float MasterAngVelX, MasterAngVelY, MasterAngVelZ;

        // Flight info (read only, for display)
        public float Mach;
        public float Altitude;
        public float FuelMass;

        // Controls
        public float PitchTrim, RollTrim, YawTrim;
        public float Throttle;
        public float FlapSetting;
        public bool ParkingBrake;
        public float Collective;
        public float Brake;
        public bool GearRetracted;

        // Dictionaries for variable-count stuff
        public Dictionary<string, float> CustomAxes = new();
        public Dictionary<string, float> Channels = new();
        public Dictionary<string, float> FuelTanks = new();
        public Dictionary<string, string> FuelTankNames = new();

        // Serialize to key=value text

        public string Serialize()
        {
            var lines = new List<string>
            {
                $"CraftName={CraftName}",
                $"Timestamp={Timestamp}",
                $"GamePosX={GamePosX.ToString(Inv)}",
                $"GamePosY={GamePosY.ToString(Inv)}",
                $"GamePosZ={GamePosZ.ToString(Inv)}",
                $"RotX={RotX.ToString(Inv)}",
                $"RotY={RotY.ToString(Inv)}",
                $"RotZ={RotZ.ToString(Inv)}",
                $"RotW={RotW.ToString(Inv)}",
                $"RealPosX={RealPosX.ToString(Inv)}",
                $"RealPosY={RealPosY.ToString(Inv)}",
                $"RealPosZ={RealPosZ.ToString(Inv)}",
                $"VelX={VelX.ToString(Inv)}",
                $"VelY={VelY.ToString(Inv)}",
                $"VelZ={VelZ.ToString(Inv)}",
                $"RealVelX={RealVelX.ToString(Inv)}",
                $"RealVelY={RealVelY.ToString(Inv)}",
                $"RealVelZ={RealVelZ.ToString(Inv)}",
                $"StoredVelX={StoredVelX.ToString(Inv)}",
                $"StoredVelY={StoredVelY.ToString(Inv)}",
                $"StoredVelZ={StoredVelZ.ToString(Inv)}",
                $"BodyRelVelX={BodyRelVelX.ToString(Inv)}",
                $"BodyRelVelY={BodyRelVelY.ToString(Inv)}",
                $"BodyRelVelZ={BodyRelVelZ.ToString(Inv)}",
                $"RbVelX={RbVelX.ToString(Inv)}",
                $"RbVelY={RbVelY.ToString(Inv)}",
                $"RbVelZ={RbVelZ.ToString(Inv)}",
                $"RbAngVelX={RbAngVelX.ToString(Inv)}",
                $"RbAngVelY={RbAngVelY.ToString(Inv)}",
                $"RbAngVelZ={RbAngVelZ.ToString(Inv)}",
                $"MasterAngVelX={MasterAngVelX.ToString(Inv)}",
                $"MasterAngVelY={MasterAngVelY.ToString(Inv)}",
                $"MasterAngVelZ={MasterAngVelZ.ToString(Inv)}",
                $"Mach={Mach.ToString(Inv)}",
                $"Altitude={Altitude.ToString(Inv)}",
                $"FuelMass={FuelMass.ToString(Inv)}",
                $"PitchTrim={PitchTrim.ToString(Inv)}",
                $"RollTrim={RollTrim.ToString(Inv)}",
                $"YawTrim={YawTrim.ToString(Inv)}",
                $"Throttle={Throttle.ToString(Inv)}",
                $"FlapSetting={FlapSetting.ToString(Inv)}",
                $"ParkingBrake={ParkingBrake}",
                $"Collective={Collective.ToString(Inv)}",
                $"Brake={Brake.ToString(Inv)}",
                $"GearRetracted={GearRetracted}",
            };

            foreach (var kvp in CustomAxes)
                lines.Add($"CA:{kvp.Key}={kvp.Value.ToString(Inv)}");

            foreach (var kvp in Channels)
                lines.Add($"CH:{kvp.Key}={kvp.Value.ToString(Inv)}");

            foreach (var kvp in FuelTanks)
                lines.Add($"FT:{kvp.Key}={kvp.Value.ToString(Inv)}");

            foreach (var kvp in FuelTankNames)
                lines.Add($"FN:{kvp.Key}={kvp.Value}");

            return string.Join("\n", lines);
        }

        // Deserialize from key=value text

        public static TimeState Deserialize(string data)
        {
            var ts = new TimeState();
            var dict = new Dictionary<string, string>();

            foreach (var line in data.Split('\n'))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;
                int eq = trimmed.IndexOf('=');
                if (eq < 0) continue;
                string key = trimmed[..eq];
                string val = trimmed[(eq + 1)..];

                if (key.StartsWith("CA:"))
                {
                    if (float.TryParse(val, NumberStyles.Float, Inv, out float f))
                        ts.CustomAxes[key[3..]] = f;
                }
                else if (key.StartsWith("CH:"))
                {
                    if (float.TryParse(val, NumberStyles.Float, Inv, out float f))
                        ts.Channels[key[3..]] = f;
                }
                else if (key.StartsWith("FT:"))
                {
                    if (float.TryParse(val, NumberStyles.Float, Inv, out float f))
                        ts.FuelTanks[key[3..]] = f;
                }
                else if (key.StartsWith("FN:"))
                {
                    ts.FuelTankNames[key[3..]] = val;
                }
                else
                {
                    dict[key] = val;
                }
            }

            ts.CraftName = GetStr(dict, "CraftName", "Unknown");
            ts.Timestamp = GetStr(dict, "Timestamp", "?");

            ts.GamePosX = GetFloat(dict, "GamePosX");
            ts.GamePosY = GetFloat(dict, "GamePosY");
            ts.GamePosZ = GetFloat(dict, "GamePosZ");

            // Backwards compat with old save format
            if (ts.GamePosX == 0 && ts.GamePosY == 0 && ts.GamePosZ == 0)
            {
                ts.GamePosX = GetFloat(dict, "PosX");
                ts.GamePosY = GetFloat(dict, "PosY");
                ts.GamePosZ = GetFloat(dict, "PosZ");
            }

            ts.RotX = GetFloat(dict, "RotX");
            ts.RotY = GetFloat(dict, "RotY");
            ts.RotZ = GetFloat(dict, "RotZ");
            ts.RotW = GetFloat(dict, "RotW");

            ts.RealPosX = GetDouble(dict, "RealPosX");
            ts.RealPosY = GetDouble(dict, "RealPosY");
            ts.RealPosZ = GetDouble(dict, "RealPosZ");

            ts.VelX = GetFloat(dict, "VelX");
            ts.VelY = GetFloat(dict, "VelY");
            ts.VelZ = GetFloat(dict, "VelZ");
            ts.RealVelX = GetDouble(dict, "RealVelX");
            ts.RealVelY = GetDouble(dict, "RealVelY");
            ts.RealVelZ = GetDouble(dict, "RealVelZ");
            ts.StoredVelX = GetDouble(dict, "StoredVelX");
            ts.StoredVelY = GetDouble(dict, "StoredVelY");
            ts.StoredVelZ = GetDouble(dict, "StoredVelZ");
            ts.BodyRelVelX = GetDouble(dict, "BodyRelVelX");
            ts.BodyRelVelY = GetDouble(dict, "BodyRelVelY");
            ts.BodyRelVelZ = GetDouble(dict, "BodyRelVelZ");

            ts.RbVelX = GetFloat(dict, "RbVelX");
            ts.RbVelY = GetFloat(dict, "RbVelY");
            ts.RbVelZ = GetFloat(dict, "RbVelZ");
            ts.RbAngVelX = GetFloat(dict, "RbAngVelX");
            ts.RbAngVelY = GetFloat(dict, "RbAngVelY");
            ts.RbAngVelZ = GetFloat(dict, "RbAngVelZ");
            ts.MasterAngVelX = GetFloat(dict, "MasterAngVelX");
            ts.MasterAngVelY = GetFloat(dict, "MasterAngVelY");
            ts.MasterAngVelZ = GetFloat(dict, "MasterAngVelZ");

            ts.Mach = GetFloat(dict, "Mach");
            ts.Altitude = GetFloat(dict, "Altitude");
            ts.FuelMass = GetFloat(dict, "FuelMass");

            ts.PitchTrim = GetFloat(dict, "PitchTrim");
            ts.RollTrim = GetFloat(dict, "RollTrim");
            ts.YawTrim = GetFloat(dict, "YawTrim");
            ts.Throttle = GetFloat(dict, "Throttle");
            ts.FlapSetting = GetFloat(dict, "FlapSetting");
            ts.ParkingBrake = GetBool(dict, "ParkingBrake");
            ts.Collective = GetFloat(dict, "Collective");
            ts.Brake = GetFloat(dict, "Brake");
            ts.GearRetracted = GetBool(dict, "GearRetracted");

            return ts;
        }

        // Parse helpers

        private static string GetStr(Dictionary<string, string> d, string key, string def)
            => d.TryGetValue(key, out var v) ? v : def;

        private static float GetFloat(Dictionary<string, string> d, string key)
            => d.TryGetValue(key, out var v) && float.TryParse(v, NumberStyles.Float, Inv, out float f) ? f : 0f;

        private static double GetDouble(Dictionary<string, string> d, string key)
            => d.TryGetValue(key, out var v) && double.TryParse(v, NumberStyles.Float, Inv, out double f) ? f : 0.0;

        private static bool GetBool(Dictionary<string, string> d, string key)
            => d.TryGetValue(key, out var v) && bool.TryParse(v, out bool b) && b;
    }
}
