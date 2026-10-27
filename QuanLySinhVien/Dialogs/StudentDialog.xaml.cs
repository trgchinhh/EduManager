using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using QuanLySinhVien.Models;
using QuanLySinhVien.Services;

namespace QuanLySinhVien.Dialogs;

public partial class StudentDialog : Window
{
    private static readonly string[] GenderOptions = { "Nam", "Nữ", "Khác" };
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    private static readonly Regex PhoneRegex = new(@"^[0-9+\s().-]{8,20}$");

    private readonly Student? _original;

    public Student? ResultStudent { get; private set; }

    public StudentDialog()
        : this(null)
    {
    }

    public StudentDialog(Student? student, string? defaultClassId = null)
    {
        InitializeComponent();
        SourceInitialized += (_, _) => ThemeManager.ApplyTitleBar(this);
        _original = student;

        var classOptions = DataStore.MyClasses
            .Select(classroom => new ClassOption(classroom.Id, classroom.Name))
            .ToList();

        ClassBox.ItemsSource = classOptions;
        GenderBox.ItemsSource = GenderOptions;
        StatusBox.ItemsSource = StudentStatuses.All;

        if (student is null)
        {
            Title = "Thêm sinh viên";
            HeaderTitle.Text = "Thêm sinh viên";
            HeaderSubtitle.Text = "Nhập thông tin sinh viên và gán vào lớp học.";
            SaveButton.Content = "Lưu sinh viên";

            IdBox.Text = DataStore.NextStudentId();
            BirthDatePicker.SelectedDate = new DateTime(2005, 1, 1);
            GenderBox.SelectedItem = "Nam";
            StatusBox.SelectedItem = StudentStatuses.Active;
            ScoreBox.Text = "5.0";

            var selectedClass = classOptions.FirstOrDefault(option => option.Id == defaultClassId)
                                ?? classOptions.FirstOrDefault();
            ClassBox.SelectedItem = selectedClass;

            Loaded += (_, _) =>
            {
                FullNameBox.Focus();
            };
        }
        else
        {
            Title = "Cập nhật sinh viên";
            HeaderTitle.Text = "Cập nhật sinh viên";
            HeaderSubtitle.Text = "Chỉnh sửa thông tin sinh viên và lưu lại thay đổi.";
            SaveButton.Content = "Lưu thay đổi";

            if (classOptions.All(option => option.Id != student.ClassId))
            {
                classOptions.Add(new ClassOption(student.ClassId, student.ClassName));
                ClassBox.ItemsSource = classOptions;
            }

            IdBox.Text = student.Id;
            FullNameBox.Text = student.FullName;
            ClassBox.SelectedItem = classOptions.First(option => option.Id == student.ClassId);
            BirthDatePicker.SelectedDate = student.DateOfBirth;
            GenderBox.SelectedItem = GenderOptions.Contains(student.Gender) ? student.Gender : "Khác";
            EmailBox.Text = student.Email;
            PhoneBox.Text = student.Phone;
            AddressBox.Text = student.Address;
            ScoreBox.Text = student.Score.ToString("0.0", CultureInfo.CurrentCulture);
            StatusBox.SelectedItem = student.Status;

            Loaded += (_, _) =>
            {
                FullNameBox.Focus();
                FullNameBox.SelectAll();
            };
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var id = IdBox.Text.Trim().ToUpperInvariant();
        var fullName = FullNameBox.Text.Trim();
        var email = EmailBox.Text.Trim();
        var phone = PhoneBox.Text.Trim();
        var address = AddressBox.Text.Trim();
        var selectedClass = ClassBox.SelectedItem as ClassOption;
        var gender = GenderBox.SelectedItem as string ?? "Nam";
        var status = StatusBox.SelectedItem as string ?? StudentStatuses.Active;
        var birthDate = BirthDatePicker.SelectedDate ?? new DateTime(2005, 1, 1);

        if (string.IsNullOrWhiteSpace(fullName))
        {
            ShowError("Vui lòng nhập họ và tên sinh viên.", FullNameBox);
            return;
        }

        if (string.IsNullOrWhiteSpace(id))
        {
            ShowError("Vui lòng nhập mã sinh viên.", IdBox);
            return;
        }

        if (id.Any(char.IsWhiteSpace))
        {
            ShowError("Mã sinh viên không được chứa khoảng trắng.", IdBox);
            return;
        }

        var isSameId = _original is not null &&
                       string.Equals(id, _original.Id, StringComparison.OrdinalIgnoreCase);
        if (!isSameId && DataStore.IsStudentIdTaken(id))
        {
            ShowError($"Mã sinh viên \"{id}\" đã tồn tại. Vui lòng chọn mã khác.", IdBox);
            return;
        }

        if (selectedClass is null)
        {
            ShowError("Vui lòng chọn lớp học cho sinh viên.", ClassBox);
            return;
        }

        if (!string.IsNullOrWhiteSpace(email) && !EmailRegex.IsMatch(email))
        {
            ShowError("Địa chỉ email không hợp lệ.", EmailBox);
            return;
        }

        if (!string.IsNullOrWhiteSpace(phone) && !PhoneRegex.IsMatch(phone))
        {
            ShowError("Số điện thoại không hợp lệ. Vui lòng chỉ nhập số, khoảng trắng hoặc dấu +.", PhoneBox);
            return;
        }

        if (birthDate > DateTime.Today)
        {
            ShowError("Ngày sinh không thể lớn hơn ngày hiện tại.", BirthDatePicker);
            return;
        }

        if (birthDate < new DateTime(1940, 1, 1))
        {
            ShowError("Ngày sinh không hợp lệ.", BirthDatePicker);
            return;
        }

        var scoreText = ScoreBox.Text.Trim().Replace(',', '.');
        if (!double.TryParse(scoreText, NumberStyles.Float, CultureInfo.InvariantCulture, out var score))
        {
            ShowError("Điểm trung bình phải là một số từ 0 đến 10.", ScoreBox);
            return;
        }

        if (score < 0 || score > 10)
        {
            ShowError("Điểm trung bình phải nằm trong khoảng từ 0 đến 10.", ScoreBox);
            return;
        }

        var result = _original?.Clone() ?? new Student();
        result.Id = id;
        result.FullName = fullName;
        result.ClassId = selectedClass.Id;
        result.DateOfBirth = birthDate;
        result.Gender = gender;
        result.Email = email;
        result.Phone = phone;
        result.Address = address;
        result.Score = Math.Round(score, 1);
        result.Status = status;

        ResultStudent = result;
        DialogResult = true;
        Close();
    }

    private void ShowError(string message, Control? focusTarget)
    {
        ErrorText.Text = message;
        ErrorBorder.Visibility = Visibility.Visible;
        focusTarget?.Focus();
    }

    private sealed class ClassOption
    {
        public ClassOption(string id, string name)
        {
            Id = id;
            Name = name;
        }

        public string Id { get; }
        public string Name { get; }

        public override string ToString() => Name;
    }
}
