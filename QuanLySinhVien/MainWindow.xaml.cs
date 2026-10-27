using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using QuanLySinhVien.Dialogs;
using QuanLySinhVien.Services;
using QuanLySinhVien.Views;

namespace QuanLySinhVien;

public partial class MainWindow : Window
{
    private bool _sidebarCollapsed;
    private readonly DashboardView _dashboardView = new();
    private readonly ClassesView _classesView = new();
    private readonly StudentsView _studentsView = new();
    private readonly StatisticsView _statisticsView = new();
    private readonly ProfileView _profileView = new();

    public MainWindow()
    {
        InitializeComponent();
        ThemeManager.Apply(Resources);
        WindowLayoutStore.Restore(this);
        ApplySidebarState(WindowLayoutStore.RestoreSidebarCollapsed());
        SourceInitialized += (_, _) => ThemeManager.ApplyTitleBar(this);
        UpdateThemeButton();

        _profileView.ProfileSaved += (_, _) =>
        {
            UpdateHeader();
            _dashboardView.Refresh();
        };

        DateText.Text = DateTime.Now.ToString("dddd, dd/MM/yyyy", CultureInfo.GetCultureInfo("vi-VN"));
        UpdateHeader();
        ShowView("dashboard");
    }

    private void ShowView(string key, bool syncNavigation = true)
    {
        UserControl view = key switch
        {
            "classes" => _classesView,
            "students" => _studentsView,
            "statistics" => _statisticsView,
            "profile" => _profileView,
            _ => _dashboardView
        };

        ContentHost.Content = view;

        if (syncNavigation)
        {
            NavDashboard.IsChecked = key == "dashboard";
            NavClasses.IsChecked = key == "classes";
            NavStudents.IsChecked = key == "students";
            NavStatistics.IsChecked = key == "statistics";
            NavProfile.IsChecked = key == "profile";
        }

        switch (key)
        {
            case "classes":
                _classesView.Refresh();
                break;
            case "students":
                _studentsView.Refresh();
                break;
            case "statistics":
                _statisticsView.Refresh();
                break;
            case "profile":
                _profileView.Refresh();
                break;
            default:
                _dashboardView.Refresh();
                break;
        }
    }

    private void RefreshAllViews()
    {
        _dashboardView.Refresh();
        _classesView.Refresh();
        _studentsView.Refresh();
        _statisticsView.Refresh();
        _profileView.Refresh();
    }

    private void UpdateHeader()
    {
        Title = "EduManager";
    }

    private void Nav_Click(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton radio && radio.CommandParameter is string key)
        {
            ShowView(key, syncNavigation: false);
        }
    }

    private void SidebarToggle_Click(object sender, RoutedEventArgs e)
    {
        ApplySidebarState(!_sidebarCollapsed);
        WindowLayoutStore.Save(this, _sidebarCollapsed);
    }

    private void ApplySidebarState(bool collapsed)
    {
        _sidebarCollapsed = collapsed;
        SidebarColumn.Width = new GridLength(_sidebarCollapsed ? 68 : 220);
        Tag = _sidebarCollapsed;
        SidebarToggleIcon.Text = _sidebarCollapsed ? "\uE700" : "\uE76B";
        SidebarToggleButton.HorizontalAlignment = _sidebarCollapsed ? HorizontalAlignment.Center : HorizontalAlignment.Left;
        SidebarToggleButton.Margin = _sidebarCollapsed ? new Thickness(0) : new Thickness(9, 0, 0, 0);
        SidebarToggleButton.ToolTip = _sidebarCollapsed ? "Mở rộng thanh bên" : "Thu gọn thanh bên";
        LogoutText.Visibility = _sidebarCollapsed ? Visibility.Collapsed : Visibility.Visible;
        LogoutIcon.Visibility = _sidebarCollapsed ? Visibility.Visible : Visibility.Collapsed;
    }

    private void MenuClasses_Click(object sender, RoutedEventArgs e)
    {
        ShowView("classes");
    }

    private void MenuStudents_Click(object sender, RoutedEventArgs e)
    {
        ShowView("students");
    }

    private void MenuStatistics_Click(object sender, RoutedEventArgs e)
    {
        ShowView("statistics");
    }

    private void AddStudent_Click(object sender, RoutedEventArgs e)
    {
        ShowView("students");
        _studentsView.AddStudent();
    }

    private void AddClass_Click(object sender, RoutedEventArgs e)
    {
        ShowView("classes");
        _classesView.AddClass();
    }

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        if (DataStore.CurrentLecturer is null)
        {
            return;
        }

        DataStore.Save();
        DataStore.Logout();
        Hide();
        var loginWindow = new LoginWindow
        {
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        if (loginWindow.ShowDialog() == true)
        {
            UpdateHeader();
            RefreshAllViews();
            ShowView("dashboard");
            Show();
            Activate();
            return;
        }

        Close();
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ThemeToggle_Click(object sender, RoutedEventArgs e)
    {
        ThemeManager.Toggle(this);
        UpdateThemeButton();
    }

    private void UpdateThemeButton()
    {
        ThemeToggleIcon.Text = ThemeManager.IsDarkMode ? "\uE706" : "\uE708";
    }

    private void Help_Click(object sender, RoutedEventArgs e)
    {
        var window = new HelpWindow { Owner = this };
        window.ShowDialog();
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        var window = new AboutWindow { Owner = this };
        window.ShowDialog();
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        WindowLayoutStore.Save(this, _sidebarCollapsed);
        DataStore.Save();
        base.OnClosing(e);
    }
}
