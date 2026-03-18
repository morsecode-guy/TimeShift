using System;
using System.Collections.Generic;
using System.IO;
using MelonLoader;
using UnityEngine.InputSystem;

namespace TimeShift
{
    public class KeybindManager
    {
        // The three actions you can rebind
        public enum Action
        {
            QuickSave,
            QuickLoad,
            ToggleMenu
        }

        // Display names
        public static readonly string[] ActionNames =
        {
            "Quick Save",
            "Quick Load (hold)",
            "Toggle Menu"
        };

        // Defaults
        static readonly Key[] DefaultKeys =
        {
            Key.F5,       // Quick save
            Key.F9,       // Quick load (hold)
            Key.F10       // Toggle menu
        };

        // How long you have to hold the quick load key
        public const float HoldDuration = 1.0f;

        // Current bindings
        readonly Key[] _keys;
        readonly string _configPath;

        // Rebinding state
        bool _waitingForKey;
        int _rebindAction;

        public bool IsRebinding => _waitingForKey;
        public int RebindingAction => _rebindAction;

        public KeybindManager(string dataFolder)
        {
            _keys = new Key[DefaultKeys.Length];
            Array.Copy(DefaultKeys, _keys, DefaultKeys.Length);
            _configPath = Path.Combine(dataFolder, "keybinds.cfg");
            Load();
        }

        // Get the key for an action
        public Key GetKey(Action action)
        {
            return _keys[(int)action];
        }

        // Get a display name for a key
        public static string KeyName(Key key)
        {
            return key.ToString();
        }

        // Check if the key for an action was just pressed this frame
        public bool WasPressed(Action action, Keyboard kb)
        {
            return kb[_keys[(int)action]].wasPressedThisFrame;
        }

        // Check if the key for an action is currently held down
        public bool IsHeld(Action action, Keyboard kb)
        {
            return kb[_keys[(int)action]].isPressed;
        }

        // Start waiting for a new key press for this action
        public void StartRebind(int actionIndex)
        {
            _waitingForKey = true;
            _rebindAction = actionIndex;
        }

        // List of rebindable keys
        static readonly Key[] RebindableKeys =
        {
            Key.A, Key.B, Key.C, Key.D, Key.E, Key.F, Key.G, Key.H, Key.I,
            Key.J, Key.K, Key.L, Key.M, Key.N, Key.O, Key.P, Key.Q, Key.R,
            Key.S, Key.T, Key.U, Key.V, Key.W, Key.X, Key.Y, Key.Z,
            Key.Digit0, Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4,
            Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9,
            Key.F1, Key.F2, Key.F3, Key.F4, Key.F5, Key.F6,
            Key.F7, Key.F8, Key.F9, Key.F10, Key.F11, Key.F12,
            Key.Space, Key.Enter, Key.Tab, Key.Backspace, Key.Delete,
            Key.Insert, Key.Home, Key.End, Key.PageUp, Key.PageDown,
            Key.UpArrow, Key.DownArrow, Key.LeftArrow, Key.RightArrow,
            Key.LeftShift, Key.RightShift, Key.LeftCtrl, Key.RightCtrl,
            Key.LeftAlt, Key.RightAlt, Key.Comma, Key.Period, Key.Slash,
            Key.Backslash, Key.LeftBracket, Key.RightBracket,
            Key.Minus, Key.Equals, Key.Semicolon, Key.Quote, Key.Backquote,
            Key.Numpad0, Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4,
            Key.Numpad5, Key.Numpad6, Key.Numpad7, Key.Numpad8, Key.Numpad9,
            Key.NumpadPlus, Key.NumpadMinus, Key.NumpadMultiply, Key.NumpadDivide,
            Key.NumpadEnter, Key.NumpadPeriod
        };

        // Call every frame while rebinding, returns true when done
        public bool UpdateRebind(Keyboard kb)
        {
            if (!_waitingForKey) return false;

            // Check escape to cancel
            if (kb.escapeKey.wasPressedThisFrame)
            {
                _waitingForKey = false;
                return true;
            }

            // Only check known safe keys to avoid IL2CPP crash
            foreach (var key in RebindableKeys)
            {
                try
                {
                    if (kb[key].wasPressedThisFrame)
                    {
                        _keys[_rebindAction] = key;
                        _waitingForKey = false;
                        Save();
                        MelonLogger.Msg($"Rebound {ActionNames[_rebindAction]} to {KeyName(key)}");
                        return true;
                    }
                }
                catch { }
            }

            return false;
        }

        // Save bindings to file
        void Save()
        {
            try
            {
                var lines = new List<string>();
                for (int i = 0; i < _keys.Length; i++)
                    lines.Add($"{(Action)i}={_keys[i]}");
                File.WriteAllLines(_configPath, lines);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("Could not save keybinds: " + ex.Message);
            }
        }

        // Load bindings from file
        void Load()
        {
            if (!File.Exists(_configPath)) return;

            try
            {
                var lines = File.ReadAllLines(_configPath);
                foreach (var line in lines)
                {
                    var parts = line.Split('=');
                    if (parts.Length != 2) continue;

                    if (Enum.TryParse<Action>(parts[0].Trim(), out var action) &&
                        Enum.TryParse<Key>(parts[1].Trim(), out var key))
                    {
                        _keys[(int)action] = key;
                    }
                }
                MelonLogger.Msg("Loaded keybinds from config");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("Could not load keybinds: " + ex.Message);
            }
        }
    }
}
