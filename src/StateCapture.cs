using System;
using Il2Cpp;
using MelonLoader;
using UnityEngine;

namespace TimeShift
{
    public static class StateCapture
    {
        public static TimeState Capture(Craft craft)
        {
            var ts = new TimeState();
            var go = craft.gameObject;
            var t = go.transform;

            ts.CraftName = craft.vName ?? "Unknown";

            // Rotation
            ts.RotX = t.rotation.x;
            ts.RotY = t.rotation.y;
            ts.RotZ = t.rotation.z;
            ts.RotW = t.rotation.w;

            // Position
            ts.GamePosX = craft.gamePosition.x;
            ts.GamePosY = craft.gamePosition.y;
            ts.GamePosZ = craft.gamePosition.z;
            ts.RealPosX = craft.realPosition.x;
            ts.RealPosY = craft.realPosition.y;
            ts.RealPosZ = craft.realPosition.z;

            // Velocity
            ts.VelX = craft.velocity.x;
            ts.VelY = craft.velocity.y;
            ts.VelZ = craft.velocity.z;
            ts.RealVelX = craft.realVelocity.x;
            ts.RealVelY = craft.realVelocity.y;
            ts.RealVelZ = craft.realVelocity.z;
            ts.StoredVelX = craft.storedVelocity.x;
            ts.StoredVelY = craft.storedVelocity.y;
            ts.StoredVelZ = craft.storedVelocity.z;
            ts.BodyRelVelX = craft.bodyRelativeVelocity.x;
            ts.BodyRelVelY = craft.bodyRelativeVelocity.y;
            ts.BodyRelVelZ = craft.bodyRelativeVelocity.z;

            // Rigidbody
            if (craft.masterRB != null)
            {
                var rb = craft.masterRB.rigidbody;
                if (rb != null)
                {
                    ts.RbVelX = rb.velocity.x;
                    ts.RbVelY = rb.velocity.y;
                    ts.RbVelZ = rb.velocity.z;
                    ts.RbAngVelX = rb.angularVelocity.x;
                    ts.RbAngVelY = rb.angularVelocity.y;
                    ts.RbAngVelZ = rb.angularVelocity.z;
                }
                ts.MasterAngVelX = craft.masterRB.angularVelocity.x;
                ts.MasterAngVelY = craft.masterRB.angularVelocity.y;
                ts.MasterAngVelZ = craft.masterRB.angularVelocity.z;
            }

            // Flight info
            ts.Mach = craft.mach;
            ts.Altitude = craft.altitude;
            ts.FuelMass = craft.fuelMass;

            // Controls
            CaptureControls(craft, ts);

            // Custom axes
            CaptureCustomAxes(craft, ts);

            // Fuel
            CaptureFuel(go, ts);

            // Landing gear
            CaptureGear(go, ts);

            ts.Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            return ts;
        }

        static void CaptureControls(Craft craft, TimeState ts)
        {
            if (craft.craftControls == null) return;

            var cc = craft.craftControls;
            var trims = cc.Trims;
            ts.PitchTrim = trims.x;
            ts.RollTrim = trims.y;
            ts.YawTrim = trims.z;
            ts.Throttle = cc.RawThrottle;
            ts.FlapSetting = cc.FlapSettings;
            ts.ParkingBrake = cc.ParkingBrake;
            ts.Collective = cc.Collective;
            ts.Brake = cc.Brake;

            try
            {
                if (cc.channels != null)
                {
                    for (int i = 0; i < cc.channels.Length; i++)
                    {
                        var ch = cc.channels[i];
                        ts.Channels[$"ch_{i}_{ch.name ?? i.ToString()}"] = ch.value;
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("Could not capture channels: " + ex.Message);
            }
        }

        static void CaptureCustomAxes(Craft craft, TimeState ts)
        {
            if (craft.customAxes == null) return;

            try
            {
                foreach (var axis in craft.customAxes)
                {
                    if (axis != null && axis.name != null)
                        ts.CustomAxes[axis.name] = axis.Value;
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("Could not capture custom axes: " + ex.Message);
            }
        }

        static void CaptureFuel(GameObject go, TimeState ts)
        {
            try
            {
                var res = go.GetComponent<CraftResources>();
                if (res == null || res.fuelTanks == null) return;

                for (int i = 0; i < res.fuelTanks.Count; i++)
                {
                    var tank = res.fuelTanks[i];
                    if (tank == null) continue;

                    string key = $"tank_{i}";
                    ts.FuelTanks[key] = tank.Fill;
                    if (tank.title != null)
                        ts.FuelTankNames[key] = tank.title;
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("Could not capture fuel tanks: " + ex.Message);
            }
        }

        static void CaptureGear(GameObject go, TimeState ts)
        {
            try
            {
                var gears = go.GetComponentsInChildren<LandingGear>();
                if (gears != null && gears.Length > 0)
                    ts.GearRetracted = gears[0].gearRetracted;
            }
            catch { }
        }
    }
}
