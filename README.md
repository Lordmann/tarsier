<p align="center">
  <img src="docs/logo.png" alt="Tarsier" width="128">
</p>

<h1 align="center">Tarsier</h1>

<p align="center">
  Per-application gamma, brightness, contrast and vibrance for Windows.<br>
  <em style="opacity: 0.3">Named for the primate with exceptional night vision whose eyes are each about the size of its brain.</em>
</p>

<p align="center">
  <a href="https://github.com/Lordmann/tarsier/releases/download/v1.0.0/Tarsier-win-Setup.exe"><strong>Download for Windows</strong></a>
  &nbsp;·&nbsp;
  <a href="https://github.com/Lordmann/tarsier/releases">All releases</a>
</p>

Bind a profile to an executable and its adjustments are applied to the monitor that application's window is
on, only while that window is in the foreground. Alt-tab away and the monitor goes back to normal. No
overlay, no injection, no game-side configuration: it works with anything that has a window.

<!-- TODO: screenshots
<p align="center">
  <img src="docs/screenshot-main.png" alt="Profile editor" width="720">
</p>
-->

## Install

1. Download [Tarsier-win-Setup.exe](https://github.com/Lordmann/tarsier/releases/download/v1.0.0/Tarsier-win-Setup.exe)
   and run it. It installs per user, with no administrator prompt.
2. On first launch Tarsier fetches the latest release. After downloading it will restart.
3. Tarsier lives in the notification area. Double-click the tray icon to open the window, right-click to Open, Pause all profiles or Exit the application.

The installer is not code-signed _yet_, so SmartScreen may show "Windows protected your PC" the first time.
Click **More info**, then **Run anyway**. Windows 10 or 11, 64-bit.

### Updates

Tarsier checks GitHub for a newer release when it starts and every six hours after that. An update is
downloaded silently and installed the next time the app exits; if you would rather not wait, the tray menu
grows a **Restart to update** entry once one is ready. There is nothing to configure and nothing to click
through.

<details>
<summary><h2>How it works</h2></summary>

Windows has no per-application colour pipeline. The only adjustment an external process can make is to a
**monitor's hardware gamma ramp**, so Tarsier works like this:

1. A WinEvent hook reports every foreground change (and window move or restore).
2. The foreground window is resolved to its executable and to the monitor it sits on.
3. If a profile is bound to that executable, its ramp is applied to **that monitor only**.
4. As soon as the application loses the foreground, the monitor is restored.

The consequence worth knowing: while a profiled application is focused, the adjustment covers the whole
monitor it occupies, not just the window rectangle.

### Profiles

Click **Add...** to pick a running application, or browse for an executable. Profiles are keyed by bare
executable name (`game.exe`), so they survive the application being moved or reinstalled to another folder
or drive. They are stored in `%APPDATA%\Tarsier\profiles.json`.

**Preview on this monitor** applies the sliders live to the monitor the Tarsier window is on, so a profile
can be tuned without switching to the game. **Pause all profiles** (window or tray menu) suspends everything
without touching the saved values. **Start with Windows**, under **Settings** at the bottom of the window,
registers Tarsier to start hidden in the tray at sign-in.

#### Sliders

All sliders run from -100 to 100, and **0 leaves the display untouched**. Below 0 darkens or flattens, above
0 brightens or adds punch. Each level is pushed through gamma, then contrast, then brightness, so a profile
left at 0/0/0 reproduces the display's normal output exactly.

**Vibrance** is the odd one out. Saturation mixes the colour channels together, and a gamma ramp is three
independent per-channel curves, so it cannot be reached that way at any curve shape. It is driven instead
through NVIDIA's digital vibrance, the same control NVIDIA Control Panel exposes, and **the slider only
appears when an NVIDIA-driven display is present**. It maps onto whatever range the driver reports, so 0
lands exactly on the driver's own default, and the level you had set in NVIDIA Control Panel is put back when
the profile stops applying. Monitors on Intel or AMD adapters keep working for the other three sliders.

#### Full slider range

Windows only accepts gamma ramps within a band around the identity curve, so past a moderate setting the
sliders stop having any further effect. The default band covers most needs. If you want the whole range,
**Settings** shows whether it is available and offers **Enable full range**, which sets
`HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\ICM\GdiIcmGammaRange` to `256` for you. That one value
needs an administrator prompt and a sign-out to take effect; it is the same change every gamma tool for
Windows relies on, and it is harmless to leave in place.

### Hotkeys

Each profile can carry three combinations: one that turns the profile on and off, and two that make it
stronger or weaker. A combination is any mix of keys, mouse buttons and one wheel direction (`Ctrl + T`,
`Mouse 4`, `F4 + Wheel Up`) and fires once every part of it is down, in whatever order they were pressed; to
record one, click the box and press it.

Listening starts only while that profile's application holds the foreground and stops as soon as it does
not, which means the combinations never interfere with other applications and two profiles may safely use
the same ones. The input is passed on to the application as well, so a game still sees the keys.

**Toggle** is remembered for the session but never written to disk, so restarting Tarsier starts every
profile switched on.

**Increase / decrease intensity** add or subtract 1 from gamma, brightness and contrast, so a profile at
40/-20/0 becomes 41/-19/1 and then 39/-21/-1; holding the combination keeps stepping at the keyboard's repeat
rate. Vibrance is saturation rather than intensity and is left alone. The adjustment is applied to the
monitor immediately and mirrored on the sliders in the window, but it is only an unsaved edit: press **Save**
to keep it, or **Cancel** to go back to the saved profile. Like the toggle it survives alt-tabbing but not
restarting the app.

### Limitations

These are properties of Windows, not of Tarsier:

- **HDR displays ignore gamma ramps entirely.** A monitor in HDR mode cannot be adjusted this way.
- **Exclusive-fullscreen applications may seize the ramp** themselves and override the profile. Borderless
  or windowed-fullscreen is reliable.
- **A hard kill leaves the last ramp applied.** Windows does not own the gamma ramp on a process's behalf,
  so nothing undoes it when that process dies. Exiting from the tray restores every monitor, and so does an
  unhandled exception on its way down, but ending the process from Task Manager cannot. Starting Tarsier
  again clears whatever it inherited, and any display mode change or sign-out clears it too.
- **A hard kill also strands vibrance**, and unlike the ramp it is not swept at startup: a saturation level
  is indistinguishable from one you set deliberately in NVIDIA Control Panel, and clearing it on every
  launch would throw that setting away. It is restored the next time a profile applies and releases that
  monitor, or by setting it back in NVIDIA Control Panel.
- **Vibrance needs an NVIDIA GPU.** See [Sliders](#sliders).

</details>

## Building from source

Requires the [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0).

```
dotnet build Tarsier.sln
dotnet test tests/Tarsier.Core.Tests
dotnet run --project src/Tarsier.App
```

Pass `--tray` to start hidden in the notification area, which is what "Start with Windows" registers. A
build that is not installed through the installer never checks for updates.

| Project | Purpose |
| --- | --- |
| `src/Tarsier.Core` | Ramp maths, profile storage, and the service that decides what to apply. No Win32, fully unit-tested. |
| `src/Tarsier.App` | WPF shell: P/Invoke to GDI, NVAPI and the WinEvent and input hooks, tray icon, settings window. |
| `tests/Tarsier.Core.Tests` | xUnit tests covering the ramp curve, profile matching, persistence, hotkeys and toggle behaviour. |

## Issue reporting

Bug reports and feature requests go in [Issues](https://github.com/Lordmann/tarsier/issues).

## License

[MIT](LICENSE)
