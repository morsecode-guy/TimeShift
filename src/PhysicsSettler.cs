using System.Collections.Generic;
using Il2Cpp;
using MelonLoader;
using UnityEngine;

namespace TimeShift
{
    public class PhysicsSettler
    {
        int _framesLeft;
        List<Rigidbody> _rigidbodies;
        List<Collider> _colliders;
        Vector3 _rbVel;
        Vector3 _rbAngVel;
        Vector3 _masterAngVel;
        Craft _craft;

        // Joint grace timer - keeps joints unbreakable for a bit after re-enable
        float _jointTimer;

        public void Begin(
            Craft craft,
            List<Rigidbody> rigidbodies,
            List<Collider> colliders,
            Vector3 rbVel,
            Vector3 rbAngVel,
            Vector3 masterAngVel)
        {
            _craft = craft;
            _rigidbodies = rigidbodies;
            _colliders = colliders;
            _rbVel = rbVel;
            _rbAngVel = rbAngVel;
            _masterAngVel = masterAngVel;
            _framesLeft = 5;
        }

        // Call this every frame from OnUpdate
        public void Tick()
        {
            // Count down settle frames
            if (_framesLeft > 0)
            {
                _framesLeft--;
                if (_framesLeft == 0)
                    Finish();
            }

            // Count down joint grace period
            if (_jointTimer > 0f)
            {
                _jointTimer -= Time.deltaTime;
                if (_jointTimer <= 0f)
                {
                    _jointTimer = 0f;
                    Cheats.unbreakableJoints = false;
                    MelonLogger.Msg("Unbreakable joints disabled after grace period");
                }
            }
        }

        void Finish()
        {
            MelonLogger.Msg("Re-enabling physics after settle");

            // Set joints unbreakable again in case the game reset them
            Cheats.unbreakableJoints = true;
            if (_craft != null)
            {
                try
                {
                    var joints = _craft.gameObject.GetComponentsInChildren<ConfigurableJoint>();
                    if (joints != null)
                    {
                        foreach (var joint in joints)
                        {
                            if (joint != null)
                            {
                                joint.breakForce = float.PositiveInfinity;
                                joint.breakTorque = float.PositiveInfinity;
                            }
                        }
                    }
                }
                catch { }
            }

            // Turn colliders back on
            if (_colliders != null)
            {
                foreach (var col in _colliders)
                {
                    try { if (col != null) col.enabled = true; } catch { }
                }
                MelonLogger.Msg($"Re-enabled {_colliders.Count} colliders");
                _colliders = null;
            }

            // Unfreeze rigidbodies and set their velocities
            if (_rigidbodies != null)
            {
                foreach (var rb in _rigidbodies)
                {
                    try
                    {
                        if (rb != null)
                        {
                            rb.isKinematic = false;
                            rb.velocity = _rbVel;
                            rb.angularVelocity = _rbAngVel;
                        }
                    }
                    catch { }
                }
                MelonLogger.Msg($"Unfroze {_rigidbodies.Count} rigidbodies");
                _rigidbodies = null;
            }

            // Set master rb velocity
            if (_craft != null && _craft.masterRB != null)
            {
                try
                {
                    _craft.masterRB.SetVelocity(_rbVel, _masterAngVel);
                }
                catch { }
            }

            _craft = null;

            // Keep joints unbreakable for half a second after re-enable
            _jointTimer = 0.5f;
            MelonLogger.Msg("Physics re-enabled, joints will unlock in 0.5s");
        }
    }
}
