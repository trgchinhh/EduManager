using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace QuanLySinhVien.Services;

public static class ThemeManager
{
    private const int DwmwaCaptionColor = 35;
    private const int DwmwaTextColor = 36;
    private static readonly string SettingsFile = Path.Combine(Path.GetDirectoryName(DataStore.DataFilePath)!, "theme.json");

    public static bool IsDarkMode { get; private set; }

    public static void Load()
    {
        try
        {
            if (File.Exists(SettingsFile))
            {
                using var document = JsonDocument.Parse(File.ReadAllText(SettingsFile));
                if (document.RootElement.TryGetProperty("darkMode", out var value) &&
                    (value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False))
                {
                    IsDarkMode = value.GetBoolean();
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Không thể tải cài đặt giao diện: {ex.Message}");
        }

        Apply(Application.Current.Resources);
    }

    public static void Toggle(Window window)
    {
        IsDarkMode = !IsDarkMode;
        Apply(Application.Current.Resources);
        Apply(window.Resources);
        Save();
        ApplyTitleBar(window);
    }

    public static void Apply(ResourceDictionary resources)
    {
        var colors = IsDarkMode
            ? new Dictionary<string, string>
            {
                ["AppBackgroundBrush"] = "#1F1F1F", ["SurfaceBrush"] = "#1F1F1F", ["SurfaceSoftBrush"] = "#292929",
                ["TextPrimaryBrush"] = "#F1F2F5", ["TextSecondaryBrush"] = "#C2C4CD", ["TextMutedBrush"] = "#9296A2",
                ["AccentBrush"] = "#F4622A", ["AccentDarkBrush"] = "#FF9A78", ["AccentSoftBrush"] = "#492B20", ["AccentSoft2Brush"] = "#593426",
                ["AppBorderBrush"] = "#41434D", ["AppBorderSoftBrush"] = "#34363F",
                ["SuccessBrush"] = "#55D49A", ["SuccessSoftBrush"] = "#1D3B31",
                ["WarningBrush"] = "#F0BF58", ["WarningSoftBrush"] = "#45391F",
                ["DangerBrush"] = "#FF7777", ["DangerSoftBrush"] = "#482A30",
                ["InfoBrush"] = "#62BDEB", ["InfoSoftBrush"] = "#203B4C",
                ["PurpleBrush"] = "#C39AFF", ["PurpleSoftBrush"] = "#3B2B50"
            }
            : new Dictionary<string, string>
            {
                ["AppBackgroundBrush"] = "#F1F5F9", ["SurfaceBrush"] = "#FFFFFF", ["SurfaceSoftBrush"] = "#F8FAFC",
                ["TextPrimaryBrush"] = "#0F172A", ["TextSecondaryBrush"] = "#64748B", ["TextMutedBrush"] = "#94A3B8",
                ["AccentBrush"] = "#F4622A", ["AccentDarkBrush"] = "#D64E21", ["AccentSoftBrush"] = "#FFF0E9", ["AccentSoft2Brush"] = "#FFE1D4",
                ["AppBorderBrush"] = "#E2E8F0", ["AppBorderSoftBrush"] = "#EEF2F7",
                ["SuccessBrush"] = "#059669", ["SuccessSoftBrush"] = "#D1FAE5",
                ["WarningBrush"] = "#D97706", ["WarningSoftBrush"] = "#FEF3C7",
                ["DangerBrush"] = "#DC2626", ["DangerSoftBrush"] = "#FEE2E2",
                ["InfoBrush"] = "#0284C7", ["InfoSoftBrush"] = "#E0F2FE",
                ["PurpleBrush"] = "#7C3AED", ["PurpleSoftBrush"] = "#F3E8FF"
            };

        foreach (var (key, hex) in colors)
        {
            resources[key] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)!);
        }

        var sidebar = new LinearGradientBrush();
        sidebar.StartPoint = new Point(0, 0);
        sidebar.EndPoint = new Point(1, 1);
        sidebar.GradientStops.Add(new GradientStop(ColorFrom(IsDarkMode ? "#1B1D24" : "#0F172A"), 0));
        sidebar.GradientStops.Add(new GradientStop(ColorFrom(IsDarkMode ? "#262239" : "#1E1B4B"), 0.55));
        sidebar.GradientStops.Add(new GradientStop(ColorFrom(IsDarkMode ? "#30284A" : "#312E81"), 1));
        resources["SidebarGradientBrush"] = sidebar;

        var accent = new LinearGradientBrush(ColorFrom(IsDarkMode ? "#FF8056" : "#FF8056"), ColorFrom("#E94C1E"), 45);
        resources["AccentGradientBrush"] = accent;
    }

    public static void ApplyTitleBar(Window window)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
        {
            return;
        }

        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        var caption = ToColorRef(ColorFrom(IsDarkMode ? "#1F1F1F" : "#F7F6F9"));
        var text = ToColorRef(ColorFrom(IsDarkMode ? "#F1F2F5" : "#25242A"));
        try
        {
            _ = DwmSetWindowAttribute(handle, DwmwaCaptionColor, ref caption, sizeof(int));
            _ = DwmSetWindowAttribute(handle, DwmwaTextColor, ref text, sizeof(int));
        }
        catch (DllNotFoundException)
        {
        }
        catch (EntryPointNotFoundException)
        {
        }
    }

    private static void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsFile)!);
            File.WriteAllText(SettingsFile, JsonSerializer.Serialize(new ThemeSettings(IsDarkMode), new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Không thể lưu cài đặt giao diện: {ex.Message}");
        }
    }

    private static Color ColorFrom(string hex) => (Color)ColorConverter.ConvertFromString(hex)!;

    private static int ToColorRef(Color color) => color.R | (color.G << 8) | (color.B << 16);

    private sealed record ThemeSettings(bool DarkMode);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int valueSize);
}
