# TimeShift

A [MelonLoader](https://melonwiki.xyz/) mod for **Flyout** that lets you save and restore your craft's complete state mid-flight.

## Features

- **Quick save** with F5.
- **Quick load** by holding F9 for 1 second.
- **State browser** with F10 to browse, load, and delete saves
- **Time warp** 1x/2x/5x/10x with physics-safe warp
- **Configurable keybinds** through the menu, saved to disk
- **Notifications** that pop up at the top center of the screen
- **Safe teleport** with deferred physics so your craft doesn't explode

## Install

1. Install [MelonLoader](https://melonwiki.xyz/) for Flyout
2. Set your game path:
   ```bash
   export FLYOUT_DIR="/path/to/Steam/steamapps/common/Flyout"
   ```
   Or pass it directly: `dotnet build -c Release -p:GameDir="/path/to/Flyout"`
3. Build with `dotnet build -c Release`
4. Copy `bin/Release/net6.0/TimeShift.dll` to your Flyout `Mods/` folder

## Default Controls

| Key | Action |
|-----|--------|
| F5  | Quick save |
| F9  | Hold to load last save |
| F10 | Open/close state browser |

All keybinds can be changed through the in-game menu.

## Project Structure

```
src/
  TimeShiftMod.cs      Main mod entry point
  TimeState.cs         Save state data and serialization
  StateCapture.cs      Captures craft state into a TimeState
  StateApply.cs        Applies a TimeState back onto a craft
  PhysicsSettler.cs    Deferred physics re-enable after teleport
  MenuGUI.cs           IMGUI menu for browsing states
  KeybindManager.cs    Configurable persistent keybinds
  Notifier.cs          Top-center notification toasts
  CraftHelper.cs       Active craft lookup
  StateFileHelper.cs   File naming and listing
```

## License

[MIT](LICENSE)
