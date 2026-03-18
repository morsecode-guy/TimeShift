using System;
using System.IO;
using System.Linq;
using Il2Cpp;
using MelonLoader;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[assembly: MelonInfo(typeof(TimeShift.TimeShiftMod), "TimeShift", "4.0.0", "Morse Code Guy")]
[assembly: MelonGame("Stonext Games", "Flyout")]

namespace TimeShift
{
    public class TimeShiftMod : MelonMod
    {
        // Paths
        string _gameFolder;
        string _dataFolder;
        string _statesFolder;

        // Time warp
        public static readonly float[] WarpSpeeds = { 1f, 2f, 5f, 10f };
        int _currentWarpIndex;
        float _defaultFixedDeltaTime;
        bool _savedFixedDT;

        // Physics settle after state apply
        PhysicsSettler _settler;

        // GUI
        MenuGUI _menu;

        // Keybinds and notifications
        KeybindManager _keybinds;
        Notifier _notifier;

        // Hold-to-load tracking
        float _holdTimer;
        bool _holdNotified;

        public string StatesFolder => _statesFolder;
        public string DataFolder => _dataFolder;
        public KeybindManager Keybinds => _keybinds;
        public Notifier Notifier => _notifier;

        public override void OnInitializeMelon()
        {
            _gameFolder = Path.GetDirectoryName(Application.dataPath);
            _dataFolder = Path.Combine(_gameFolder, "TimeShiftData");
            _statesFolder = Path.Combine(_dataFolder, "TimeStates");

            if (!Directory.Exists(_dataFolder))
                Directory.CreateDirectory(_dataFolder);
            if (!Directory.Exists(_statesFolder))
                Directory.CreateDirectory(_statesFolder);

            _keybinds = new KeybindManager(_dataFolder);
            _notifier = new Notifier();
            _settler = new PhysicsSettler();
            _menu = new MenuGUI(this);

            MelonLogger.Msg($"TimeShift v4 initialized - {_statesFolder}");
        }

        public override void OnUpdate()
        {
            bool inFlight = SceneManager.GetActiveScene().name == "PlanetScene2";

            // Reset warp when leaving flight
            if (!inFlight && _currentWarpIndex != 0)
                SetTimeWarp(0);

            var kb = Keyboard.current;
            if (kb == null) return;

            // Rebinding works in any scene
            if (_keybinds.IsRebinding)
            {
                _keybinds.UpdateRebind(kb);
                return;
            }

            // Menu toggle works in any scene
            if (_keybinds.WasPressed(KeybindManager.Action.ToggleMenu, kb))
                _menu.Toggle();

            // Everything below only works in flight
            if (!inFlight) return;

            // Grab the default fixed delta time once
            if (!_savedFixedDT)
            {
                _defaultFixedDeltaTime = Time.fixedDeltaTime;
                _savedFixedDT = true;
            }

            _settler.Tick();

            // Quick save
            if (_keybinds.WasPressed(KeybindManager.Action.QuickSave, kb))
                SaveState();

            // Quick load - hold key for 1 second to load last save
            if (_keybinds.IsHeld(KeybindManager.Action.QuickLoad, kb))
            {
                _holdTimer += Time.unscaledDeltaTime;

                // Show a "keep holding" notification once
                if (!_holdNotified && _holdTimer > 0.15f)
                {
                    _notifier.Show("Hold to load last save...", 1.5f);
                    _holdNotified = true;
                }

                if (_holdTimer >= KeybindManager.HoldDuration)
                {
                    LoadLastSave();
                    _holdTimer = 0f;
                    _holdNotified = false;
                }
            }
            else
            {
                _holdTimer = 0f;
                _holdNotified = false;
            }
        }

        public override void OnGUI()
        {
            _notifier.Draw();
            _menu.Draw();
        }

        // Save

        public void SaveState()
        {
            var craft = CraftHelper.GetActiveCraft();
            if (craft == null)
            {
                _notifier.Show("No active craft found");
                MelonLogger.Warning("No active craft found");
                return;
            }

            var state = StateCapture.Capture(craft);
            var fileName = StateFileHelper.MakeFileName(craft, _statesFolder);
            var filePath = Path.Combine(_statesFolder, fileName);

            File.WriteAllText(filePath, state.Serialize());
            _notifier.Show("State saved");
            MelonLogger.Msg($"State saved: {fileName}");
        }

        // Load a specific file

        public void ApplyStateFromFile(string filePath)
        {
            try
            {
                string data = File.ReadAllText(filePath);
                var state = TimeState.Deserialize(data);
                StateApply.Apply(state, _settler);
                _notifier.Show("State loaded: " + state.CraftName);
            }
            catch (Exception ex)
            {
                _notifier.Show("Failed to load state");
                MelonLogger.Error("Failed to load state: " + ex.Message);
            }
        }

        // Load the most recent save file

        void LoadLastSave()
        {
            if (!Directory.Exists(_statesFolder))
            {
                _notifier.Show("No saves found");
                return;
            }

            var files = Directory.GetFiles(_statesFolder, "*.ts");
            if (files.Length == 0)
            {
                _notifier.Show("No saves found");
                return;
            }

            // Pick the newest file
            var newest = files.OrderByDescending(f => File.GetCreationTime(f)).First();
            _notifier.Show("Loading last save...");
            ApplyStateFromFile(newest);
        }

        // Time warp

        public int CurrentWarpIndex => _currentWarpIndex;

        public void SetTimeWarp(int index)
        {
            _currentWarpIndex = index;
            float speed = WarpSpeeds[index];

            // Only change timescale, keep fixedDeltaTime the same so physics stays stable
            Time.timeScale = speed;
            Time.fixedDeltaTime = _defaultFixedDeltaTime;

            // Turn on unbreakable joints when warping fast
            if (speed > 1f)
            {
                Cheats.unbreakableJoints = true;
                Cheats.SetUnbreakableJoints();
            }

            if (speed != 1f)
                _notifier.Show($"Time warp: {speed}x");
            MelonLogger.Msg($"Time warp set to {speed}x");
        }
    }
}
