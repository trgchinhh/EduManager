using System.Windows;
using QuanLySinhVien.Services;

namespace QuanLySinhVien.Dialogs;

public partial class LecturerRegistrationWindow : Window
{
    private static readonly string[] DegreeOptions = { "Giảng viên", "Cử nhân", "Thạc sĩ", "Tiến sĩ", "Phó Giáo sư, Tiến sĩ", "Giáo sư, Tiến sĩ" };
    public string RegisteredUsername { get; private set; } = string.Empty;

    public LecturerRegistrationWindow()
    {
        InitializeComponent();
        DepartmentBox.ItemsSource = DataStore.LecturerDepartments;
        DepartmentBox.SelectedIndex = -1;
        DegreeBox.ItemsSource = DegreeOptions;
        DegreeBox.SelectedIndex = 0;
        ThemeManager.Apply(Resources);
        UpdateThemeIcon();
        SourceInitialized += (_, _) => ThemeManager.ApplyTitleBar(this);
        Loaded += (_, _) => FullNameBox.Focus();
    }

    private void Register_Click(object sender, RoutedEventArgs e)
    {
        var password = PasswordVisibleBox.Visibility == Visibility.Visible ? PasswordVisibleBox.Text : PasswordBox.Password;
        var confirmPassword = ConfirmPasswordVisibleBox.Visibility == Visibility.Visible ? ConfirmPasswordVisibleBox.Text : ConfirmPasswordBox.Password;
        if (password != confirmPassword)
        {
            ShowError("Mật khẩu xác nhận không khớp.");
            ConfirmPasswordBox.Focus();
            return;
        }

        if (DepartmentBox.SelectedItem is not string department)
        {
            ShowError("Vui lòng chọn khoa / bộ môn.");
            DepartmentBox.Focus();
            return;
        }

        if (!DataStore.RegisterLecturer(FullNameBox.Text, UsernameBox.Text, password, department, DegreeBox.SelectedItem as string ?? "Giảng viên", out var error))
        {
            ShowError(error);
            return;
        }

        RegisteredUsername = UsernameBox.Text.Trim();
        DialogResult = true;
    }

    private void TogglePasswordVisibility_Click(object sender, RoutedEventArgs e)
    {
        var showPassword = PasswordVisibleBox.Visibility != Visibility.Visible;
        if (showPassword)
        {
            PasswordVisibleBox.Text = PasswordBox.Password;
            PasswordBox.Visibility = Visibility.Collapsed;
            PasswordVisibleBox.Visibility = Visibility.Visible;
            PasswordVisibleBox.Focus();
            PasswordVisibleBox.CaretIndex = PasswordVisibleBox.Text.Length;
            PasswordEyeSlash.Visibility = Visibility.Visible;
        }
        else
        {
            PasswordBox.Password = PasswordVisibleBox.Text;
            PasswordVisibleBox.Visibility = Visibility.Collapsed;
            PasswordBox.Visibility = Visibility.Visible;
            PasswordBox.Focus();
            PasswordEyeSlash.Visibility = Visibility.Collapsed;
        }
    }

    private void ThemeToggle_Click(object sender, RoutedEventArgs e)
    {
        ThemeManager.Toggle(this);
        UpdateThemeIcon();
    }

    private void UpdateThemeIcon()
    {
        ThemeToggleIcon.Text = ThemeManager.IsDarkMode ? "\uE706" : "\uE708";
    }

    private void ToggleConfirmPasswordVisibility_Click(object sender, RoutedEventArgs e)
    {
        var showPassword = ConfirmPasswordVisibleBox.Visibility != Visibility.Visible;
        if (showPassword)
        {
            ConfirmPasswordVisibleBox.Text = ConfirmPasswordBox.Password;
            ConfirmPasswordBox.Visibility = Visibility.Collapsed;
            ConfirmPasswordVisibleBox.Visibility = Visibility.Visible;
            ConfirmPasswordVisibleBox.Focus();
            ConfirmPasswordVisibleBox.CaretIndex = ConfirmPasswordVisibleBox.Text.Length;
            ConfirmEyeSlash.Visibility = Visibility.Visible;
        }
        else
        {
            ConfirmPasswordBox.Password = ConfirmPasswordVisibleBox.Text;
            ConfirmPasswordVisibleBox.Visibility = Visibility.Collapsed;
            ConfirmPasswordBox.Visibility = Visibility.Visible;
            ConfirmPasswordBox.Focus();
            ConfirmEyeSlash.Visibility = Visibility.Collapsed;
        }
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
}
