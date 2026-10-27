using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace QuanLySinhVien.Services;

public static class WindowLayoutStore
{
    private static readonly string SettingsFile = Path.Combine(Path.GetDirectoryName(DataStore.DataFilePath)!, "window-size.json");

    public static void Restore(Window window)
    {
        try
        {
            if (!File.Exists(SettingsFile))
            {
                return;
            }

            var saved = JsonSerializer.Deserialize<WindowLayout>(File.ReadAllText(SettingsFile));
            if (saved is null || saved.Width < window.MinWidth || saved.Height < window.MinHeight)
            {
                return;
            }

            window.WindowState = WindowState.Normal;
            window.Width = Math.Min(saved.Width, SystemParameters.WorkArea.Width);
            window.Height = Math.Min(saved.Height, SystemParameters.WorkArea.Height);
            if (saved.IsMaximized)
            {
                window.WindowState = WindowState.Maximized;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Không thể tải kích thước cửa sổ: {ex.Message}");
        }
    }

    public static bool RestoreSidebarCollapsed()
    {
        try
        {
            if (File.Exists(SettingsFile))
            {
                var saved = JsonSerializer.Deserialize<WindowLayout>(File.ReadAllText(SettingsFile));
                return saved?.SidebarCollapsed ?? true;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Không thể tải trạng thái thanh bên: {ex.Message}");
        }

        return true;
    }

    public static void Save(Window window, bool sidebarCollapsed)
    {
        try
        {
            var bounds = window.WindowState == WindowState.Normal
                ? new Rect(window.Left, window.Top, window.ActualWidth, window.ActualHeight)
                : window.RestoreBounds;
            var width = bounds.Width > 0 ? bounds.Width : window.Width;
            var height = bounds.Height > 0 ? bounds.Height : window.Height;
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsFile)!);
            File.WriteAllText(SettingsFile, JsonSerializer.Serialize(new WindowLayout(width, height, window.WindowState == WindowState.Maximized, sidebarCollapsed), new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Không thể lưu kích thước cửa sổ: {ex.Message}");
        }
    }

    private sealed record WindowLayout(double Width, double Height, bool IsMaximized, bool? SidebarCollapsed = null);
}
