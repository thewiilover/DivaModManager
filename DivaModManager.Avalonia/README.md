# DivaModManager.Avalonia

Linux-friendly Avalonia UI port scaffold with a DataGrid-based mod list and drag-drop reordering.

## Wayland

If a Wayland session is detected, the app sets `AVALONIA_USE_WAYLAND=1` automatically. To force Wayland manually:

```bash
AVALONIA_USE_WAYLAND=1 dotnet run
```

## Quick start

```bash
cd /home/wii/RiderProjects/DivaModManager/DivaModManager.Avalonia
dotnet run
```

## Smoke test

```bash
cd /home/wii/RiderProjects/DivaModManager/DivaModManager.Smoke
dotnet run
```