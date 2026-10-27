using System.Windows;
using QuanLySinhVien.Models;
using QuanLySinhVien.Services;

namespace QuanLySinhVien.Dialogs;

public partial class ClassDialog : Window
{
    private readonly Classroom? _original;

    public Classroom? ResultClass { get; private set; }

    public ClassDialog()
        : this(null)
    {
    }

    public ClassDialog(Classroom? classroom)
    {
        InitializeComponent();
        SourceInitialized += (_, _) => ThemeManager.ApplyTitleBar(this);
        _original = classroom;
        MajorBox.ItemsSource = DataStore.LecturerDepartments;

        if (classroom is null)
        {
            Title = "Thêm lớp học";
            HeaderTitle.Text = "Thêm lớp học";
            HeaderSubtitle.Text = "Tạo không gian lớp học mới cho sinh viên của bạn.";
            SaveButton.Content = "Lưu lớp học";
            SchoolYearBox.Text = $"{DateTime.Now.Year} - {DateTime.Now.Year + 4}";
            Loaded += (_, _) => IdBox.Focus();
        }
        else
        {
            Title = "Cập nhật lớp học";
            HeaderTitle.Text = "Cập nhật lớp học";
            HeaderSubtitle.Text = "Chỉnh sửa thông tin lớp học và lưu lại thay đổi.";
            SaveButton.Content = "Lưu thay đổi";

            IdBox.Text = classroom.Id;
            NameBox.Text = classroom.Name;
            SubjectBox.Text = classroom.Subject;
            var departments = DataStore.LecturerDepartments.ToList();
            if (!string.IsNullOrWhiteSpace(classroom.Major) && !departments.Contains(classroom.Major))
                departments.Add(classroom.Major);
            MajorBox.ItemsSource = departments;
            MajorBox.SelectedItem = classroom.Major;
            SchoolYearBox.Text = classroom.SchoolYear;
            RoomBox.Text = classroom.Room;

            Loaded += (_, _) =>
            {
                NameBox.Focus();
                NameBox.SelectAll();
            };
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var id = IdBox.Text.Trim().ToUpperInvariant();
        var name = NameBox.Text.Trim();
        var subject = SubjectBox.Text.Trim();
        var major = MajorBox.SelectedItem as string ?? MajorBox.Text.Trim();
        var schoolYear = SchoolYearBox.Text.Trim();
        var room = RoomBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(id))
        {
            ShowError("Vui lòng nhập mã lớp.", IdBox);
            return;
        }

        if (id.Any(char.IsWhiteSpace))
        {
            ShowError("Mã lớp không được chứa khoảng trắng.", IdBox);
            return;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            ShowError("Vui lòng nhập tên lớp.", NameBox);
            return;
        }

        if (string.IsNullOrWhiteSpace(subject))
        {
            ShowError("Vui lòng nhập tên môn học.", SubjectBox);
            return;
        }

        var isSameId = _original is not null &&
                       string.Equals(id, _original.Id, StringComparison.OrdinalIgnoreCase);
        if (!isSameId && DataStore.IsClassIdTaken(id))
        {
            ShowError($"Mã lớp \"{id}\" đã tồn tại. Vui lòng chọn mã khác.", IdBox);
            return;
        }

        var result = _original?.Clone() ?? new Classroom();
        result.Id = id;
        result.Name = name;
        result.Subject = subject;
        result.Major = major;
        result.SchoolYear = schoolYear;
        result.Room = room;
        result.LecturerId = DataStore.CurrentLecturer?.Id ?? _original?.LecturerId ?? string.Empty;

        ResultClass = result;
        DialogResult = true;
        Close();
    }

    private void ShowError(string message, System.Windows.Controls.TextBox focusTarget)
    {
        ErrorText.Text = message;
        ErrorBorder.Visibility = Visibility.Visible;
        focusTarget.Focus();
    }
}
