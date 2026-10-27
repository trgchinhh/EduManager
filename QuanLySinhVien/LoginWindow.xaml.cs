using System.Windows;
using System.Windows.Controls;
using QuanLySinhVien.Services;

namespace QuanLySinhVien;

public partial class LoginWindow : Window
{
    private static readonly string[] DegreeOptions = { "Giảng viên", "Cử nhân", "Thạc sĩ", "Tiến sĩ", "Phó Giáo sư, Tiến sĩ", "Giáo sư, Tiến sĩ" };
    private bool _isRegistrationMode;

    public LoginWindow()
    {
        InitializeComponent();
        ThemeManager.Apply(Resources);
        DepartmentBox.ItemsSource = DataStore.LecturerDepartments;
        DegreeBox.ItemsSource = DegreeOptions;
        DegreeBox.SelectedIndex = 0;
        UpdateThemeIcon();
        SourceInitialized += (_, _) => ThemeManager.ApplyTitleBar(this);
        Loaded += (_, _) =>
        {
            UsernameBox.Focus();
        };
    }

    private void Login_Click(object sender, RoutedEventArgs e)
    {
        if (_isRegistrationMode)
        {
            CompleteRegistration();
            return;
        }

        var username = UsernameBox.Text.Trim();
        var password = PasswordVisibleBox.Visibility == Visibility.Visible
            ? PasswordVisibleBox.Text
            : PasswordBox.Password;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            ShowError("Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.");
            return;
        }

        if (!DataStore.Login(username, password))
        {
            ShowError("Tên đăng nhập hoặc mật khẩu không đúng. Vui lòng thử lại.");
            PasswordBox.Clear();
            PasswordBox.Focus();
            return;
        }

        DialogResult = true;
        Close();
    }

    private void DemoAccount_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string username)
        {
            return;
        }

        UsernameBox.Text = username;
        PasswordBox.Password = "123456";
        PasswordVisibleBox.Text = "123456";
        ErrorBorder.Visibility = Visibility.Collapsed;
        LoginButton.Focus();
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

    private void Register_Click(object sender, RoutedEventArgs e)
    {
        if (_isRegistrationMode)
        {
            ShowLoginMode();
            return;
        }

        _isRegistrationMode = true;
        WelcomeText.Text = "TẠO TÀI KHOẢN GIẢNG VIÊN";
        FormTitleText.Text = "Đăng ký";
        FormSubtitleText.Text = "Điền thông tin để tạo tài khoản giảng viên.";
        PasswordLabel.Text = "Mật khẩu (ít nhất 6 ký tự)";
        LoginButton.Content = "Tạo tài khoản";
        ModeButton.Content = "Quay lại";
        RegistrationFieldsPanel.Visibility = Visibility.Visible;
        DemoPanel.Visibility = Visibility.Collapsed;
        DataNoteText.Visibility = Visibility.Collapsed;
        ErrorBorder.Visibility = Visibility.Collapsed;
        ErrorBorder.Margin = new Thickness(0, 12, 0, 0);
        DepartmentBox.SelectedIndex = -1;
        Height = 820;
        MinHeight = 700;
        MainFormPanel.VerticalAlignment = VerticalAlignment.Top;
        MainFormPanel.Margin = new Thickness(0, 54, 0, 18);
        FullNameBox.Focus();
    }

    private void ShowLoginMode()
    {
        _isRegistrationMode = false;
        WelcomeText.Text = "CHÀO MỪNG TRỞ LẠI";
        FormTitleText.Text = "Đăng nhập";
        FormSubtitleText.Text = "Sử dụng tài khoản giảng viên để tiếp tục.";
        PasswordLabel.Text = "Mật khẩu";
        LoginButton.Content = "Đăng nhập";
        ModeButton.Content = "Đăng ký";
        RegistrationFieldsPanel.Visibility = Visibility.Collapsed;
        DemoPanel.Visibility = Visibility.Visible;
        DataNoteText.Visibility = Visibility.Visible;
        ErrorBorder.Visibility = Visibility.Collapsed;
        ErrorBorder.Margin = new Thickness(0, 20, 0, 0);
        Height = 600;
        MinHeight = 560;
        MainFormPanel.VerticalAlignment = VerticalAlignment.Center;
        MainFormPanel.Margin = new Thickness(0.0, 18.0, 0, 0);
        UsernameBox.Focus();
    }

    private void CompleteRegistration()
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

        var registeredUsername = UsernameBox.Text.Trim();
        ShowLoginMode();
        UsernameBox.Text = registeredUsername;
        PasswordBox.Clear();
        PasswordVisibleBox.Clear();
        ConfirmPasswordBox.Clear();
        ConfirmPasswordVisibleBox.Clear();
        FullNameBox.Clear();
        MessageBox.Show(this, "Đăng ký thành công. Hãy nhập mật khẩu để đăng nhập.", "EduManager", MessageBoxButton.OK, MessageBoxImage.Information);
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

    private void ThemeToggle_Click(object sender, RoutedEventArgs e)
    {
        ThemeManager.Toggle(this);
        UpdateThemeIcon();
    }

    private void UpdateThemeIcon()
    {
        ThemeToggleIcon.Text = ThemeManager.IsDarkMode ? "\uE706" : "\uE708";
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorBorder.Visibility = Visibility.Visible;
    }
}
