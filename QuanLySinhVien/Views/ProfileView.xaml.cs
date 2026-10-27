using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using QuanLySinhVien.Dialogs;
using QuanLySinhVien.Models;
using QuanLySinhVien.Services;

namespace QuanLySinhVien.Views;

public partial class ProfileView : UserControl
{
    private static readonly string[] DegreeOptions = { "Giảng viên", "Cử nhân", "Thạc sĩ", "Tiến sĩ", "Phó Giáo sư, Tiến sĩ", "Giáo sư, Tiến sĩ" };
    private static readonly string[] GenderOptions = { "Nam", "Nữ", "Khác" };
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    private static readonly Regex PhoneRegex = new(@"^[0-9+\s().-]{8,20}$");

    private Lecturer? _draft;

    public event EventHandler? ProfileSaved;

    public ProfileView()
    {
        InitializeComponent();
        DegreeBox.ItemsSource = DegreeOptions;
        DepartmentBox.ItemsSource = DataStore.LecturerDepartments;
        GenderBox.ItemsSource = GenderOptions;
        Refresh();
    }

    public void Refresh()
    {
        if (DataStore.CurrentLecturer is null)
        {
            return;
        }

        LoadCurrentIntoForm();
        UpdateSummary();
        ProfileErrorBorder.Visibility = Visibility.Collapsed;
        ProfileMessage.Visibility = Visibility.Collapsed;
        PasswordMessage.Visibility = Visibility.Collapsed;
        CurrentPasswordBox.Clear();
        NewPasswordBox.Clear();
        ConfirmPasswordBox.Clear();
    }

    private void LoadCurrentIntoForm()
    {
        if (DataStore.CurrentLecturer is null)
        {
            return;
        }

        _draft = DataStore.CurrentLecturer.Clone();

        var degree = ExpandDegree(_draft.Degree);
        var degrees = DegreeOptions.ToList();
        if (!string.IsNullOrWhiteSpace(degree) && !degrees.Contains(degree))
            degrees.Add(degree);
        DegreeBox.ItemsSource = degrees;
        DegreeBox.SelectedItem = degree;
        var departments = DataStore.LecturerDepartments.ToList();
        if (!string.IsNullOrWhiteSpace(_draft.Department) && !departments.Contains(_draft.Department))
            departments.Add(_draft.Department);
        DepartmentBox.ItemsSource = departments;
        DepartmentBox.SelectedItem = _draft.Department;
        GenderBox.SelectedItem = GenderOptions.Contains(_draft.Gender) ? _draft.Gender : "Khác";

        FullNameBox.Text = _draft.FullName;
        UsernameBox.Text = _draft.Username;
        EmailBox.Text = _draft.Email;
        PhoneBox.Text = _draft.Phone;
        BirthDatePicker.SelectedDate = _draft.DateOfBirth;
    }

    private void UpdateSummary()
    {
        var lecturer = DataStore.CurrentLecturer;
        if (lecturer is null)
        {
            return;
        }

        SummaryInitials.Text = lecturer.Initials;
        ShowAvatar(lecturer.AvatarPath);
        SummaryName.Text = lecturer.FullName;
        SummaryTitle.Text = string.IsNullOrWhiteSpace(lecturer.Degree)
            ? "Giảng viên"
            : $"Giảng viên · {ExpandDegree(lecturer.Degree)}";
        SummaryDepartment.Text = lecturer.Department;
        SummaryAccount.Text = $"{lecturer.Username}  ·  {lecturer.Id}";
        SummaryEmail.Text = lecturer.Email;
        SummaryEmail.ToolTip = lecturer.Email;
        SummaryPhone.Text = string.IsNullOrWhiteSpace(lecturer.Phone) ? "Chưa cập nhật" : lecturer.Phone;
        SummaryBirth.Text = lecturer.DateOfBirth.ToString("dd/MM/yyyy");
        SummaryClasses.Text = DataStore.MyClasses.Count.ToString();
        SummaryStudents.Text = DataStore.MyStudents.Count.ToString();
        HeaderAccountText.Text = $"{lecturer.Username} · {lecturer.Id}";
    }

    private void ChangeAvatar_Click(object sender, RoutedEventArgs e)
    {
        if (_draft is null)
        {
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = "Chọn ảnh đại diện giảng viên",
            Filter = "Tệp ảnh (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif|Tất cả tệp (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(Window.GetWindow(this)) != true)
        {
            return;
        }

        try
        {
            var cropDialog = new AvatarCropDialog(dialog.FileName)
            {
                Owner = Window.GetWindow(this)
            };

            if (cropDialog.ShowDialog() != true || cropDialog.CroppedImageBytes is null)
            {
                return;
            }

            var avatarDirectory = Path.Combine(Path.GetDirectoryName(DataStore.DataFilePath)!, "Avatars");
            Directory.CreateDirectory(avatarDirectory);
            var destinationPath = Path.Combine(avatarDirectory, $"{_draft.Id}.png");
            File.WriteAllBytes(destinationPath, cropDialog.CroppedImageBytes);

            _draft.AvatarPath = destinationPath;
            ShowAvatar(destinationPath);
            ProfileMessage.Text = "Ảnh đã chọn. Nhấn Lưu thay đổi để áp dụng.";
            ProfileMessage.Visibility = Visibility.Visible;
            ProfileErrorBorder.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            MessageBox.Show(Window.GetWindow(this), $"Không thể dùng ảnh này làm ảnh đại diện.\n{ex.Message}", "Lỗi ảnh đại diện", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ShowAvatar(string? path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                SummaryAvatarImage.Fill = null;
                SummaryAvatarImage.Visibility = Visibility.Collapsed;
                SummaryInitials.Visibility = Visibility.Visible;
                return;
            }

            using var imageStream = new MemoryStream(File.ReadAllBytes(path));
            var image = BitmapFrame.Create(imageStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            image.Freeze();

            SummaryAvatarImage.Fill = new ImageBrush(image) { Stretch = Stretch.UniformToFill };
            SummaryAvatarImage.Visibility = Visibility.Visible;
            SummaryInitials.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Không thể hiển thị ảnh đại diện '{path}': {ex.Message}");
            SummaryAvatarImage.Fill = null;
            SummaryAvatarImage.Visibility = Visibility.Collapsed;
            SummaryInitials.Visibility = Visibility.Visible;
        }
    }

    private void SaveProfile_Click(object sender, RoutedEventArgs e)
    {
        if (_draft is null)
        {
            return;
        }

        var fullName = FullNameBox.Text.Trim();
        var username = UsernameBox.Text.Trim();
        var email = EmailBox.Text.Trim();
        var phone = PhoneBox.Text.Trim();
        var department = DepartmentBox.SelectedItem as string ?? string.Empty;
        var degree = DegreeBox.SelectedItem as string ?? DegreeBox.Text.Trim();
        var gender = GenderBox.SelectedItem as string ?? _draft.Gender;
        var birthDate = BirthDatePicker.SelectedDate ?? _draft.DateOfBirth;

        if (string.IsNullOrWhiteSpace(fullName))
        {
            ShowProfileError("Vui lòng nhập họ và tên giảng viên.", FullNameBox);
            return;
        }

        if (username.Length < 4)
        {
            ShowProfileError("Tên đăng nhập phải có ít nhất 4 ký tự.", UsernameBox);
            return;
        }

        if (DataStore.Storage.Lecturers.Any(lecturer => lecturer.Id != _draft.Id &&
            string.Equals(lecturer.Username, username, StringComparison.OrdinalIgnoreCase)))
        {
            ShowProfileError("Tên đăng nhập này đã được sử dụng.", UsernameBox);
            return;
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            ShowProfileError("Vui lòng nhập địa chỉ email.", EmailBox);
            return;
        }

        if (!EmailRegex.IsMatch(email))
        {
            ShowProfileError("Địa chỉ email không hợp lệ. Ví dụ: giangvien@university.edu.vn", EmailBox);
            return;
        }

        if (!string.IsNullOrWhiteSpace(phone) && !PhoneRegex.IsMatch(phone))
        {
            ShowProfileError("Số điện thoại không hợp lệ. Vui lòng chỉ nhập số, khoảng trắng hoặc dấu +.", PhoneBox);
            return;
        }

        if (birthDate > DateTime.Today)
        {
            ShowProfileError("Ngày sinh không thể lớn hơn ngày hiện tại.", null);
            return;
        }

        if (birthDate < new DateTime(1940, 1, 1))
        {
            ShowProfileError("Ngày sinh không hợp lệ.", null);
            return;
        }

        _draft.FullName = fullName;
        _draft.Username = username;
        _draft.Email = email;
        _draft.Phone = phone;
        _draft.Department = department;
        _draft.Degree = degree;
        _draft.Gender = gender;
        _draft.DateOfBirth = birthDate;

        DataStore.UpdateLecturer(_draft);
        LoadCurrentIntoForm();
        UpdateSummary();

        ProfileErrorBorder.Visibility = Visibility.Collapsed;
        ProfileMessage.Text = "Đã lưu thông tin giảng viên.";
        ProfileMessage.Visibility = Visibility.Visible;
        ProfileSaved?.Invoke(this, EventArgs.Empty);
    }

    private static string ExpandDegree(string? degree) => degree?.Trim() switch
    {
        "CN." => "Cử nhân",
        "ThS." => "Thạc sĩ",
        "TS." => "Tiến sĩ",
        "PGS.TS." => "Phó Giáo sư, Tiến sĩ",
        "GS.TS." => "Giáo sư, Tiến sĩ",
        "GV." => "Giảng viên",
        _ => degree?.Trim() ?? string.Empty
    };

    private void ResetProfile_Click(object sender, RoutedEventArgs e)
    {
        Refresh();
    }

    private void ChangePassword_Click(object sender, RoutedEventArgs e)
    {
        var lecturer = DataStore.CurrentLecturer;
        if (lecturer is null)
        {
            return;
        }

        var current = CurrentPasswordBox.Password;
        var newPassword = NewPasswordBox.Password;
        var confirm = ConfirmPasswordBox.Password;

        if (string.IsNullOrEmpty(current))
        {
            ShowPasswordMessage("Vui lòng nhập mật khẩu hiện tại.", false);
            CurrentPasswordBox.Focus();
            return;
        }

        if (current != lecturer.Password)
        {
            ShowPasswordMessage("Mật khẩu hiện tại không đúng.", false);
            CurrentPasswordBox.Focus();
            return;
        }

        if (newPassword.Length < 6)
        {
            ShowPasswordMessage("Mật khẩu mới phải có ít nhất 6 ký tự.", false);
            NewPasswordBox.Focus();
            return;
        }

        if (newPassword == current)
        {
            ShowPasswordMessage("Mật khẩu mới phải khác mật khẩu hiện tại.", false);
            NewPasswordBox.Focus();
            return;
        }

        if (newPassword != confirm)
        {
            ShowPasswordMessage("Xác nhận mật khẩu không khớp.", false);
            ConfirmPasswordBox.Focus();
            return;
        }

        lecturer.Password = newPassword;
        if (_draft is not null)
        {
            _draft.Password = newPassword;
        }

        DataStore.Save();
        CurrentPasswordBox.Clear();
        NewPasswordBox.Clear();
        ConfirmPasswordBox.Clear();
        ShowPasswordMessage("Đổi mật khẩu thành công. Hãy sử dụng mật khẩu mới cho lần đăng nhập tiếp theo.", true);
    }

    private void ShowProfileError(string message, TextBox? focusTarget)
    {
        ProfileErrorText.Text = message;
        ProfileErrorBorder.Visibility = Visibility.Visible;
        ProfileMessage.Visibility = Visibility.Collapsed;
        focusTarget?.Focus();
    }

    private void ShowPasswordMessage(string message, bool success)
    {
        PasswordMessage.Text = message;
        PasswordMessage.Foreground = success
            ? (Brush)new BrushConverter().ConvertFromString("#059669")!
            : (Brush)new BrushConverter().ConvertFromString("#DC2626")!;
        PasswordMessage.Visibility = Visibility.Visible;
    }
}
