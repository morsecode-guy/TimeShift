using System;
using System.Collections.Generic;
using Il2Cpp;
using MelonLoader;
using UnityEngine;

namespace TimeShift
{
    public static class StateApply
    {
        public static void Apply(TimeState ts, PhysicsSettler settler)
        {
            var craft = CraftHelper.GetActiveCraft();
            if (craft == null)
            {
                MelonLogger.Warning("No active craft to apply state to");
                return;
            }

            var go = craft.gameObject;
            var t = go.transform;

            // Make all joints unbreakable so nothing snaps during teleport
            Cheats.unbreakableJoints = true;
            SetAllJointsUnbreakable(go);

            // Disable colliders so nothing collides mid teleport
            var disabledColliders = DisableAllColliders(go);

            // Freeze all rigidbodies
            var frozenRBs = FreezeAllRigidbodies(craft, go);

            // Teleport the craft to the saved position
            Teleport(craft, t, ts);

            // Set craft velocity fields (these are just data, safe to set immediately)
            SetCraftVelocities(craft, ts);

            // Queue up the physics re-enable for a few frames from now
            settler.Begin(craft, frozenRBs, disabledColliders,
                new Vector3(ts.RbVelX, ts.RbVelY, ts.RbVelZ),
                new Vector3(ts.RbAngVelX, ts.RbAngVelY, ts.RbAngVelZ),
                new Vector3(ts.MasterAngVelX, ts.MasterAngVelY, ts.MasterAngVelZ));

            // Restore controls and other stuff right away
            RestoreControls(craft, ts);
            RestoreCustomAxes(craft, ts);
            RestoreFuel(go, ts);
            RestoreGear(craft, go, ts);

            MelonLogger.Msg($"State applied: {ts.CraftName} (to {craft.vName ?? "?"})");
        }

        static void SetAllJointsUnbreakable(GameObject go)
        {
            try
            {
                var joints = go.GetComponentsInChildren<ConfigurableJoint>();
                if (joints == null) return;

                foreach (var joint in joints)
                {
                    if (joint != null)
                    {
                        joint.breakForce = float.PositiveInfinity;
                        joint.breakTorque = float.PositiveInfinity;
                    }
                }
                MelonLogger.Msg($"Set {joints.Length} joints to unbreakable");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("Could not set joints unbreakable: " + ex.Message);
            }
        }

        static List<Collider> DisableAllColliders(GameObject go)
        {
            var disabled = new List<Collider>();
            try
            {
                var colliders = go.GetComponentsInChildren<Collider>();
                if (colliders == null) return disabled;

                foreach (var col in colliders)
                {
                    if (col != null && col.enabled)
                    {
                        col.enabled = false;
                        disabled.Add(col);
                    }
                }
                MelonLogger.Msg($"Disabled {disabled.Count} colliders for teleport");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("Could not disable colliders: " + ex.Message);
            }
            return disabled;
        }

        static List<Rigidbody> FreezeAllRigidbodies(Craft craft, GameObject go)
        {
            var frozen = new List<Rigidbody>();

            // Get everything via GetComponentsInChildren first
            try
            {
                var allRBs = go.GetComponentsInChildren<Rigidbody>();
                if (allRBs != null)
                {
                    foreach (var rb in allRBs)
                    {
                        if (rb != null)
                        {
                            rb.isKinematic = true;
                            frozen.Add(rb);
                        }
                    }
                }
            }
            catch { }

            // Also make sure we got the masters list
            if (craft.masters != null)
            {
                foreach (var master in craft.masters)
                {
                    if (master != null && master.rigidbody != null && !frozen.Contains(master.rigidbody))
                    {
                        master.rigidbody.isKinematic = true;
                        frozen.Add(master.rigidbody);
                    }
                }
            }

            // And the main master rb
            if (craft.masterRB != null && craft.masterRB.rigidbody != null && !frozen.Contains(craft.masterRB.rigidbody))
            {
                craft.masterRB.rigidbody.isKinematic = true;
                frozen.Add(craft.masterRB.rigidbody);
            }

            MelonLogger.Msg($"Froze {frozen.Count} rigidbodies");
            return frozen;
        }

        static void Teleport(Craft craft, Transform t, TimeState ts)
        {
            // Figure out the floating origin offset
            double originX = craft.realPosition.x - (double)craft.gamePosition.x;
            double originY = craft.realPosition.y - (double)craft.gamePosition.y;
            double originZ = craft.realPosition.z - (double)craft.gamePosition.z;

            float newGameX = (float)(ts.RealPosX - originX);
            float newGameY = (float)(ts.RealPosY - originY);
            float newGameZ = (float)(ts.RealPosZ - originZ);

            t.position = new Vector3(newGameX, newGameY, newGameZ);
            t.rotation = new Quaternion(ts.RotX, ts.RotY, ts.RotZ, ts.RotW);
            craft.gamePosition = new Vector3(newGameX, newGameY, newGameZ);
            craft.realPosition = new UnityEngine.Vector3d(ts.RealPosX, ts.RealPosY, ts.RealPosZ);
        }

        static void SetCraftVelocities(Craft craft, TimeState ts)
        {
            craft.velocity = new Vector3(ts.VelX, ts.VelY, ts.VelZ);
            craft.realVelocity = new UnityEngine.Vector3d(ts.RealVelX, ts.RealVelY, ts.RealVelZ);
            craft.storedVelocity = new UnityEngine.Vector3d(ts.StoredVelX, ts.StoredVelY, ts.StoredVelZ);
            craft.bodyRelativeVelocity = new UnityEngine.Vector3d(ts.BodyRelVelX, ts.BodyRelVelY, ts.BodyRelVelZ);
        }

        static void RestoreControls(Craft craft, TimeState ts)
        {
            if (craft.craftControls == null) return;

            var cc = craft.craftControls;
            cc.SetPitchTrim(ts.PitchTrim);
            cc.SetRollTrim(ts.RollTrim);
            cc.SetYawTrim(ts.YawTrim);
            cc.SetThrottle(ts.Throttle);
            cc.SetFlapSetting((int)ts.FlapSetting);

            try
            {
                if (cc.ParkingBrake != ts.ParkingBrake)
                    cc.ToggleParkingBrake();
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("Could not toggle parking brake: " + ex.Message);
            }
        }

        static void RestoreCustomAxes(Craft craft, TimeState ts)
        {
            if (craft.customAxes == null || ts.CustomAxes.Count == 0) return;

            try
            {
                // Match by name so it works across different planes
                foreach (var axis in craft.customAxes)
                {
                    if (axis != null && axis.name != null && ts.CustomAxes.TryGetValue(axis.name, out float val))
                    {
                        axis.SetValue(val);
                        MelonLogger.Msg($"  Custom axis '{axis.name}' = {val}");
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("Could not restore custom axes: " + ex.Message);
            }
        }

        static void RestoreFuel(GameObject go, TimeState ts)
        {
            if (ts.FuelTanks.Count == 0) return;

            try
            {
                var res = go.GetComponent<CraftResources>();
                if (res == null || res.fuelTanks == null) return;

                for (int i = 0; i < res.fuelTanks.Count; i++)
                {
                    var tank = res.fuelTanks[i];
                    if (tank == null) continue;

                    string key = $"tank_{i}";
                    if (ts.FuelTanks.TryGetValue(key, out float fill))
                    {
                        tank.SetFill(fill);
                        continue;
                    }

                    // Cross plane matching by tank title
                    if (tank.title != null)
                    {
                        foreach (var kvp in ts.FuelTankNames)
                        {
                            if (kvp.Value == tank.title && ts.FuelTanks.TryGetValue(kvp.Key, out float crossFill))
                            {
                                tank.SetFill(crossFill);
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("Could not restore fuel tanks: " + ex.Message);
            }
        }

        static void RestoreGear(Craft craft, GameObject go, TimeState ts)
        {
            try
            {
                var gears = go.GetComponentsInChildren<LandingGear>();
                if (gears == null || gears.Length == 0) return;

                bool currentRetracted = gears[0].gearRetracted;
                if (currentRetracted != ts.GearRetracted && craft.craftControls != null)
                {
                    craft.craftControls.ToggleGear();
                    MelonLogger.Msg($"  Landing gear toggled to {(ts.GearRetracted ? "UP" : "DOWN")}");
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("Could not restore gear state: " + ex.Message);
            }
        }
    }
}
