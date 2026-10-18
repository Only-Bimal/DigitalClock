# Changelog
All notable changes to this project will be documented in this file.

## Releases
### v2.6
* **Modernized to .NET 10** with SDK-style project and high-performance WPF.
* **Full Settings Dialog**: Comprehensive configuration dialog with General, Appearance, Behavior, and About/Reset tabs.
* **Windows Aero Glass**: Native DWM desktop blur behind support alongside pill and transparent backplates.
* **Smart Portable & AppData Storage**: Saves configuration in portable mode (`settings.json` next to `.exe`) if writeable, or falls back to `%AppData%\DigitalClock\settings.json`.
* **Bundled Curated Fonts**: Embedded open-source Digital 7-Segment (DSEG7) font + support for all Windows system fonts.
* **Rich Notification Area (System Tray) Icon**: Context menu with quick toggles, format submenus, snap-to-position options, and settings launcher.
* **Click-Through (Ghost) Mode**: Allows mouse clicks to pass directly through to desktop icons.
* **Position Snapping**: Instant snap to corners, edges, or center of screen.
* **Fixed Position Restore**: Saved window coordinates are now correctly restored.
* **Live Settings Preview**: Real-time visualization of appearance, fonts, colors, and scale.
* **One-Click Reset to Defaults**: Restores factory settings safely.
* **Fixed Aero Glass Outer Region**: Removed outer margin across all background styles to maintain a constant window area, and applied Win32 rounded region clipping to eliminate rectangular translucent corners in Aero Glass mode.
* **Enhanced Dark Frosted Readability**: Automatically boosts text luminance and applies soft luminous rim glow when using Windows Accent color on dark frosted backgrounds.
* **Stable Window Layout**: Added OpenType tabular numerals and fixed Viewbox aspect sizing to eliminate window jitter/resizing on digit changes.
* **Instant Live Preview**: Enabled real-time preview of all settings (seconds toggle, 24-hr format, colon blinking, date format, colors, fonts).
* **Refined Typography Spacing**: Tightened vertical spacing between time digits and date text for balanced composition.
* **Native Windows Settings Dialog**: Conformed settings dialog styling to native Windows design with standard controls and clear dropdown lists.
* **Safe Dialog Interaction**: Fixed crash on button click by supporting modeless dialog lifecycle without DialogResult errors.
* **Multi-Monitor Positioning & Snapping**: Window arrangement and snapping now detects the specific monitor the clock is currently on instead of defaulting to the primary display.
* **Protected Live Preview during System Events**: Prevented background Windows `UserPreferenceChanged` events (such as wallpaper slideshow ticks) from reverting unsaved settings while the configuration dialog is open.
* **Cross-Monitor & Wallpaper Slideshow Backdrop Refresh**: Added automatic DWM frame recalculation and blur re-sampling on drag between monitors with different wallpapers and upon Windows wallpaper change notifications (`WM_SETTINGCHANGE` / `WM_DISPLAYCHANGE`).
* **Safe Modern Windows 11 DWM Backdrop**: Replaced legacy GDI window region clipping with native WPF rounded vector borders and updated composition policy to prevent Windows 11 24H2 DWM crashes.
* **Direct High-Res Tray Icon**: Loaded embedded `accessories-clock.ico` via WPF pack URI to ensure notification icon is always visible and sharp.
* **Taskbar Recovery Handling**: Registered `TaskbarCreated` window message to automatically restore the system tray icon if Windows Explorer or DWM resets.
* **Primary Screen Centering & Quick Snap**: Ensured fallback positioning centers on `Screen.PrimaryScreen` with a dedicated "Snap to Primary Screen" option in both context and tray menus.
* **Interactive Window Resizing**: Clock window is now fully resizable by dragging any border or corner with the mouse; supports 8-direction sizing with custom cursors and visual resize indicator.
* **Smart Initial Screen Fit**: On first launch without saved dimensions (or when reset), dynamically scales from border to border across the larger screen dimension (width on landscape, height on portrait) centered on the starting monitor.
* **Persistent Window Size**: Window dimensions are remembered across application restarts.
* **Window Size Reset Setting**: Added a dedicated "Reset Size to Default (Clear Saved Size)" action in Settings Dialog (`Behavior` tab), Context Menu, and System Tray Menu to clear saved dimensions back to dynamic screen fitting.
* Cleaned up legacy dead code and fixed recursive event handlers.

### v2.4
* Added more 3 contextmenu: **ChangeOpacity**, **DisplayOnTop** and **StartWithWindows**
* Changed **ChangeColor** option from **Black (Default)** and **White** to multiple color of windows option.
* Now you can using StartWithWindows instead of using start tricks.
* **ChangeOpacity** had multi-option to customize.
* Changed LINCENSE from GNU to MIT.

### v2.0
* Added more 2 contextmenu: **ChangeColor** and **ChangeSize**
* Added 2 color mode: **Black (Default)** and **White**. You can switch in contextmenu
* Added 3 size: **Small, Medium (Default), Large**. You can switch in contextmenu
* Increased delay time from 2000ms to 5000ms
* Changed data file's name from config.ini to config.setting
* Changed seting file format
* Added 2 new files: README.md and CHANGELOG.md
* Changed icon

### v1.1-beta
* Added **Close** contextmenu
* Fixed bug: Right click not responding
* Added icon