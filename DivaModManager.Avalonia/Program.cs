using System;
using Avalonia;

namespace DivaModManager.Avalonia;

internal static class Program
{
    public static void Main(string[] args)
    {
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY"))
            && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AVALONIA_USE_WAYLAND")))
        {
            Environment.SetEnvironmentVariable("AVALONIA_USE_WAYLAND", "1");
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}