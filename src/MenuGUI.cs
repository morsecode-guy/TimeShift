using System;
using System.Collections.Generic;
using System.IO;
using Il2Cpp;
using MelonLoader;
using UnityEngine;

namespace TimeShift
{
    public class MenuGUI
    {
        readonly TimeShiftMod _mod;

        bool _showMenu;
        List<TimeStateEntry> _stateEntries = new();
        int _selectedIndex = -1;

        // Window geometry
        Rect _windowRect = new(100, 100, 420, 500);
        bool _isDragging;
        Vector2 _dragOffset;
        float _scrollOffset;

        // Layout constants
        const float ItemHeight = 50f;
        const float WinWidth = 420f;
        const float WinHeight = 740f;
        const float ListHeight = 260f;

        public MenuGUI(TimeShiftMod mod)
        {
            _mod = mod;
        }

        public void Toggle()
        {
            _showMenu = !_showMenu;
            if (_showMenu)
                RefreshList();
        }

        // Check if we are in the flight scene
        bool InFlight => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "PlanetScene2";

        // Called from OnGUI every frame
        public void Draw()
        {
            if (!_showMenu) return;

            bool inFlight = InFlight;

            // Calculate window height based on what sections are visible
            float winH = inFlight ? WinHeight : 220f;

            float wx = _windowRect.x;
            float wy = _windowRect.y;

            // Dragging the title bar
            var headerRect = new Rect(wx, wy, WinWidth, 28);
            var evt = Event.current;
            if (evt != null)
            {
                if (evt.type == EventType.MouseDown && headerRect.Contains(evt.mousePosition))
                {
                    _isDragging = true;
                    _dragOffset = evt.mousePosition - new Vector2(wx, wy);
                    evt.Use();
                }
                else if (evt.type == EventType.MouseUp)
                {
                    _isDragging = false;
                }
                else if (evt.type == EventType.MouseDrag && _isDragging)
                {
                    _windowRect.x = evt.mousePosition.x - _dragOffset.x;
                    _windowRect.y = evt.mousePosition.y - _dragOffset.y;
                    evt.Use();
                }

                // Mouse wheel scrolling in the list area (only in flight)
                if (inFlight)
                {
                    var listRect = new Rect(wx + 10, wy + 94, WinWidth - 20, ListHeight);
                    if (evt.type == EventType.ScrollWheel && listRect.Contains(evt.mousePosition))
                    {
                        _scrollOffset += evt.delta.y * 25f;
                        float maxScroll = Mathf.Max(0, _stateEntries.Count * ItemHeight - ListHeight);
                        _scrollOffset = Mathf.Clamp(_scrollOffset, 0, maxScroll);
                        evt.Use();
                    }
                }
            }

            // Window background and title
            GUI.Box(new Rect(wx, wy, WinWidth, winH), "");
            GUI.Box(new Rect(wx, wy, WinWidth, 28), inFlight ? "TimeShift" : "TimeShift - Settings");

            float y = wy + 34;

            // Flight-only sections: save state list, time warp
            if (inFlight)
            {
                GUI.Label(new Rect(wx + 10, y, WinWidth - 20, 22), "Save States (" + _stateEntries.Count + ")");
                y += 26;

                if (GUI.Button(new Rect(wx + 10, y, WinWidth - 20, 28), "Refresh List"))
                    RefreshList();
                y += 34;

                // Scrollable state list
                float listX = wx + 10;
                float listY = y;
                float listW = WinWidth - 40;

                GUI.BeginGroup(new Rect(listX, listY, WinWidth - 20, ListHeight));

                float totalHeight = _stateEntries.Count * ItemHeight;
                float maxScroll2 = Mathf.Max(0, totalHeight - ListHeight);
                _scrollOffset = Mathf.Clamp(_scrollOffset, 0, maxScroll2);

                for (int i = 0; i < _stateEntries.Count; i++)
                {
                    float itemY = i * ItemHeight - _scrollOffset;
                    if (itemY + ItemHeight < 0 || itemY > ListHeight) continue;

                    var entry = _stateEntries[i];
                    bool selected = (i == _selectedIndex);

                    if (selected)
                        GUI.color = new Color(0.5f, 0.8f, 1f);

                    string label = entry.DisplayName + "\n" + entry.Timestamp + "  |  " + entry.FileName;
                    if (GUI.Button(new Rect(0, itemY, listW, ItemHeight - 2), label))
                        _selectedIndex = i;

                    if (selected)
                        GUI.color = Color.white;
                }

                // Scrollbar thumb
                if (totalHeight > ListHeight)
                {
                    float barH = ListHeight * (ListHeight / totalHeight);
                    float barY2 = (ListHeight - barH) * (_scrollOffset / maxScroll2);
                    GUI.Box(new Rect(listW + 2, barY2, 14, barH), "");
                }

                GUI.EndGroup();

                y = listY + ListHeight + 8;

                // Load and delete buttons for the selected state
                bool hasSelection = _selectedIndex >= 0 && _selectedIndex < _stateEntries.Count;

                if (hasSelection)
                {
                    if (GUI.Button(new Rect(wx + 10, y, WinWidth - 20, 36), "Load Selected State"))
                    {
                        var entry = _stateEntries[_selectedIndex];
                        _mod.ApplyStateFromFile(entry.FilePath);
                        _showMenu = false;
                    }
                    y += 40;

                    if (GUI.Button(new Rect(wx + 10, y, WinWidth - 20, 28), "Delete Selected"))
                    {
                        var entry = _stateEntries[_selectedIndex];
                        try
                        {
                            File.Delete(entry.FilePath);
                            _mod.Notifier.Show("Deleted: " + entry.FileName);
                            MelonLogger.Msg("Deleted: " + entry.FileName);
                            RefreshList();
                        }
                        catch (Exception ex)
                        {
                            MelonLogger.Error("Failed to delete: " + ex.Message);
                        }
                    }
                    y += 32;
                }

                // Time warp controls
                y += 6;
                GUI.Box(new Rect(wx, y - 2, WinWidth, 62), "");
                GUI.Label(new Rect(wx + 10, y, WinWidth - 20, 22),
                    "Time Warp: " + TimeShiftMod.WarpSpeeds[_mod.CurrentWarpIndex] + "x");
                y += 24;

                float warpBtnW = (WinWidth - 20 - (TimeShiftMod.WarpSpeeds.Length - 1) * 4) / TimeShiftMod.WarpSpeeds.Length;
                for (int i = 0; i < TimeShiftMod.WarpSpeeds.Length; i++)
                {
                    float bx = wx + 10 + i * (warpBtnW + 4);
                    bool isActive = (i == _mod.CurrentWarpIndex);
                    if (isActive) GUI.color = new Color(0.4f, 1f, 0.4f);
                    if (GUI.Button(new Rect(bx, y, warpBtnW, 28), TimeShiftMod.WarpSpeeds[i] + "x"))
                        _mod.SetTimeWarp(i);
                    if (isActive) GUI.color = Color.white;
                }
                y += 36;
            }

            // Keybind settings (always visible)
            y += 4;
            GUI.Box(new Rect(wx, y - 2, WinWidth, 30 + KeybindManager.ActionNames.Length * 32), "");
            GUI.Label(new Rect(wx + 10, y, WinWidth - 20, 22), "Keybinds");
            y += 26;

            var keybinds = _mod.Keybinds;
            float labelW = 160f;
            float btnW = WinWidth - 20 - labelW - 8;

            for (int i = 0; i < KeybindManager.ActionNames.Length; i++)
            {
                GUI.Label(new Rect(wx + 10, y, labelW, 26), KeybindManager.ActionNames[i]);

                // Show current key or "press a key" if rebinding this action
                bool rebinding = keybinds.IsRebinding && keybinds.RebindingAction == i;
                string btnLabel;
                if (rebinding)
                {
                    GUI.color = new Color(1f, 0.8f, 0.3f);
                    btnLabel = "[ Press any key ]";
                }
                else
                {
                    btnLabel = KeybindManager.KeyName(keybinds.GetKey((KeybindManager.Action)i));
                }

                if (GUI.Button(new Rect(wx + 10 + labelW + 4, y, btnW, 26), btnLabel))
                {
                    if (!rebinding)
                        keybinds.StartRebind(i);
                }

                if (rebinding)
                    GUI.color = Color.white;

                y += 30;
            }

            y += 8;

            if (GUI.Button(new Rect(wx + 10, y, WinWidth - 20, 28), "Close"))
                _showMenu = false;
        }

        void RefreshList()
        {
            _stateEntries = StateFileHelper.RefreshStateList(_mod.StatesFolder);
            _selectedIndex = -1;
        }
    }
}
