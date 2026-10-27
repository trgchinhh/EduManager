using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using QuanLySinhVien.Dialogs;
using QuanLySinhVien.Models;
using QuanLySinhVien.Services;

namespace QuanLySinhVien.Views;

public partial class StudentsView : UserControl
{
    private readonly ICollectionView _view;
    private bool _isRefreshing;
    private bool _filtersExpanded;
    private Classroom? _selectedClass;

    public StudentsView()
    {
        InitializeComponent();

        _view = CollectionViewSource.GetDefaultView(DataStore.MyStudents);
        _view.Filter = FilterStudent;
        StudentsGrid.ItemsSource = _view;

        BuildStaticFilters();
        ApplySort();
        Refresh();
    }

    public void Refresh()
    {
        _isRefreshing = true;
        try
        {
            RebuildClassFilter();
            if (_selectedClass is not null && !DataStore.MyClasses.Any(item => item.Id == _selectedClass.Id))
            {
                _selectedClass = null;
            }

            UpdateClassCards();
            UpdateBrowseMode();
            UpdateSummary();
            _view.Refresh();
            UpdateGridStatus();
            UpdateActionButtons();
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    public void AddStudent()
    {
        if (DataStore.MyClasses.Count == 0)
        {
            MessageBox.Show(
                Window.GetWindow(this),
                "Bạn chưa có lớp học nào. Vui lòng tạo lớp học trước khi thêm sinh viên.",
                "Chưa có lớp học",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        SearchBox.Clear();
        var defaultClassId = _selectedClass?.Id ?? (ClassFilter.SelectedItem as FilterOption)?.Id;
        var dialog = new StudentDialog(null, string.IsNullOrWhiteSpace(defaultClassId) ? null : defaultClassId)
        {
            Owner = Window.GetWindow(this)
        };

        if (dialog.ShowDialog() == true && dialog.ResultStudent is not null)
        {
            DataStore.AddStudent(dialog.ResultStudent);
            Refresh();
            StudentsGrid.SelectedItem = dialog.ResultStudent;
            StudentsGrid.ScrollIntoView(dialog.ResultStudent);
        }
    }

    private void AddStudent_Click(object sender, RoutedEventArgs e)
    {
        AddStudent();
    }

    private void ClassCard_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: Classroom classroom })
        {
            return;
        }

        _selectedClass = classroom;
        _filtersExpanded = false;
        _isRefreshing = true;
        ClassFilter.SelectedItem = ClassFilter.Items
            .Cast<FilterOption>()
            .FirstOrDefault(option => option.Id == classroom.Id);
        _isRefreshing = false;

        SelectedClassTitle.Text = classroom.Name;
        _view.Refresh();
        UpdateBrowseMode();
        UpdateSummary();
        UpdateGridStatus();
        UpdateActionButtons();
    }

    private void BackToClasses_Click(object sender, RoutedEventArgs e)
    {
        _selectedClass = null;
        _filtersExpanded = false;
        ResetFilters_Click(sender, e);
        UpdateClassCards();
        UpdateBrowseMode();
        UpdateSummary();
    }

    private void UpdateBrowseMode()
    {
        var isInClass = _selectedClass is not null;
        ClassesPanel.Visibility = isInClass ? Visibility.Collapsed : Visibility.Visible;
        StudentListPanel.Visibility = isInClass ? Visibility.Visible : Visibility.Collapsed;
        StudentFilterControls.Visibility = isInClass && _filtersExpanded ? Visibility.Visible : Visibility.Collapsed;
        FilterToggleButton.Visibility = isInClass ? Visibility.Visible : Visibility.Collapsed;
        FilterToggleText.Text = _filtersExpanded ? "Ẩn lọc" : "Bộ lọc";
        ClassFilter.Visibility = isInClass ? Visibility.Collapsed : Visibility.Visible;
        SearchWatermark.Text = isInClass
            ? "Tìm theo tên, mã sinh viên, email, số điện thoại..."
            : "Tìm lớp theo tên, mã lớp hoặc môn học...";
        SearchWatermark.Visibility = string.IsNullOrEmpty(SearchBox.Text)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void ToggleFilters_Click(object sender, RoutedEventArgs e)
    {
        _filtersExpanded = !_filtersExpanded;
        UpdateBrowseMode();
    }

    private void UpdateClassCards()
    {
        var keyword = SearchBox.Text.Trim();
        var iconSet = new (string Glyph, string LightForeground, string LightBackground, string DarkForeground, string DarkBackground)[]
        {
            ("\U0001F4BB", "#D64E21", "#FFF0E9", "#FF9A78", "#492B20"),
            ("\U0001F4DA", "#6D45C4", "#F3EDFF", "#C5A9FF", "#392B50"),
            ("\U0001F310", "#087EA4", "#E3F7FC", "#6DD6F2", "#203B4C"),
            ("\U0001F4CA", "#16845D", "#E3F7EC", "#69D5A6", "#1D3B31"),
            ("\U0001F9EE", "#A96E00", "#FFF4D9", "#F2C461", "#45391F"),
            ("\U0001F512", "#C13B58", "#FCE8ED", "#FF8AA3", "#482A30"),
            ("\U0001F4E1", "#4167C5", "#EAF0FF", "#9BB6FF", "#26365A"),
            ("\u2699\uFE0F", "#A34E1D", "#FFF0E5", "#FFAA79", "#4A3022"),
            ("\U0001F52C", "#547A19", "#F0F7E4", "#B4DC72", "#33431F"),
            ("\U0001F9EC", "#B33B8D", "#FCEAF5", "#F294CB", "#4A2C43"),
            ("\U0001F9ED", "#087F79", "#E4F7F5", "#69D8CF", "#1D403E"),
            ("\U0001F3DB\uFE0F", "#775B36", "#F5F0E8", "#D5BC93", "#40382B"),
            ("\U0001F680", "#C04835", "#FCECE8", "#FF9B88", "#4B302B"),
            ("\U0001F4DD", "#3E6C8F", "#EAF3F9", "#8EC6E8", "#263C4C"),
            ("\U0001F3A8", "#9A4A61", "#F8EAF0", "#E59DB5", "#452A35"),
            ("\U0001F6E0\uFE0F", "#54616E", "#EDF1F5", "#B0C0D0", "#323B45")
        };
        var allClasses = DataStore.MyClasses
            .OrderBy(classroom => classroom.Id, StringComparer.OrdinalIgnoreCase)
            .Select((classroom, index) =>
            {
                var icon = iconSet[index % iconSet.Length];
                return new ClassCardItem
                {
                    Classroom = classroom,
                    IconGlyph = icon.Glyph,
                    IconForeground = BrushFrom(ThemeManager.IsDarkMode ? icon.DarkForeground : icon.LightForeground),
                    IconBackground = BrushFrom(ThemeManager.IsDarkMode ? icon.DarkBackground : icon.LightBackground)
                };
            })
            .ToList();

        var classes = allClasses
            .Where(classroom => string.IsNullOrWhiteSpace(keyword) ||
                classroom.Classroom.Id.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                classroom.Classroom.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                classroom.Classroom.Subject.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                classroom.Classroom.Major.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            .ToList();

        ClassCards.ItemsSource = classes;
        ClassCountText.Text = $"{classes.Count} / {DataStore.MyClasses.Count} lớp";
        NoClassesState.Visibility = DataStore.MyClasses.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        NoMatchingClassesState.Visibility = DataStore.MyClasses.Count > 0 && classes.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ExportStudents_Click(object sender, RoutedEventArgs e)
    {
        var students = _view.Cast<Student>().ToList();
        if (students.Count == 0)
        {
            MessageBox.Show(Window.GetWindow(this), "Không có sinh viên nào trong danh sách hiện tại để xuất.", "Xuất CSV", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Chọn nơi lưu danh sách sinh viên",
            Filter = "Tệp CSV (*.csv)|*.csv",
            DefaultExt = ".csv",
            AddExtension = true,
            OverwritePrompt = true,
            FileName = "DanhSachSinhVien.csv"
        };

        if (dialog.ShowDialog(Window.GetWindow(this)) != true)
        {
            return;
        }

        try
        {
            var lines = new List<string>
            {
                string.Join(",", new[] { "Mã sinh viên", "Họ và tên", "Ngày sinh", "Giới tính", "Email", "Số điện thoại", "Địa chỉ", "Mã lớp", "Tên lớp", "Điểm", "Trạng thái" }.Select(CsvEscape))
            };

            lines.AddRange(students.Select(student => string.Join(",", new[]
            {
                student.Id,
                student.FullName,
                student.DateOfBirth.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                student.Gender,
                student.Email,
                student.Phone,
                student.Address,
                student.ClassId,
                student.ClassName,
                student.Score.ToString("0.0", CultureInfo.InvariantCulture),
                student.Status
            }.Select(CsvEscape))));

            File.WriteAllText(dialog.FileName, string.Join(Environment.NewLine, lines), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            MessageBox.Show(Window.GetWindow(this), $"Đã xuất {students.Count} sinh viên ra tệp CSV.", "Xuất thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(Window.GetWindow(this), $"Không thể xuất tệp CSV.\n{ex.Message}", "Lỗi xuất tệp", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static string CsvEscape(string? value)
    {
        value ??= string.Empty;
        return value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }

    private static Brush BrushFrom(string color)
    {
        var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(color)!;
        brush.Freeze();
        return brush;
    }

    private void EditStudent_Click(object sender, RoutedEventArgs e)
    {
        if (StudentsGrid.SelectedItem is Student student)
        {
            EditStudent(student);
        }
    }

    private void DeleteStudent_Click(object sender, RoutedEventArgs e)
    {
        if (StudentsGrid.SelectedItem is Student student)
        {
            DeleteStudent(student);
        }
    }

    private void EditStudentRow_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Student student })
        {
            EditStudent(student);
        }
    }

    private void DeleteStudentRow_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Student student })
        {
            DeleteStudent(student);
        }
    }

    private void StudentsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (StudentsGrid.SelectedItem is Student student)
        {
            EditStudent(student);
        }
    }

    private void EditStudent(Student student)
    {
        var dialog = new StudentDialog(student.Clone())
        {
            Owner = Window.GetWindow(this)
        };

        if (dialog.ShowDialog() == true && dialog.ResultStudent is not null)
        {
            DataStore.UpdateStudent(student, dialog.ResultStudent);
            Refresh();
            StudentsGrid.SelectedItem = student;
            StudentsGrid.ScrollIntoView(student);
        }
    }

    private void DeleteStudent(Student student)
    {
        var message = $"Bạn có chắc chắn muốn xóa sinh viên \"{student.FullName}\" ({student.Id})?\n\nHành động này không thể hoàn tác.";
        var owner = Window.GetWindow(this);
        var result = owner is null
            ? MessageBox.Show(message, "Xác nhận xóa sinh viên", MessageBoxButton.YesNo, MessageBoxImage.Warning)
            : MessageBox.Show(owner, message, "Xác nhận xóa sinh viên", MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        DataStore.DeleteStudent(student);
        Refresh();
    }

    private void BuildStaticFilters()
    {
        _isRefreshing = true;
        try
        {
            GenderFilter.ItemsSource = new List<FilterOption>
            {
                new(string.Empty, "Tất cả giới tính"),
                new("Nam", "Nam"),
                new("Nữ", "Nữ"),
                new("Khác", "Khác")
            };
            GenderFilter.SelectedIndex = 0;

            var statuses = new List<FilterOption> { new(string.Empty, "Tất cả trạng thái") };
            statuses.AddRange(StudentStatuses.All.Select(status => new FilterOption(status, status)));
            StatusFilter.ItemsSource = statuses;
            StatusFilter.SelectedIndex = 0;

            SortFilter.ItemsSource = new List<FilterOption>
            {
                new("name_asc", "Tên A → Z"),
                new("name_desc", "Tên Z → A"),
                new("score_desc", "Điểm cao → thấp"),
                new("score_asc", "Điểm thấp → cao"),
                new("id_asc", "Mã sinh viên A → Z"),
                new("dob_desc", "Ngày sinh mới nhất")
            };
            SortFilter.SelectedIndex = 0;
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    private void RebuildClassFilter()
    {
        var selectedId = (ClassFilter.SelectedItem as FilterOption)?.Id ?? string.Empty;
        var options = new List<FilterOption> { new(string.Empty, "Tất cả lớp") };
        options.AddRange(DataStore.MyClasses.Select(classroom => new FilterOption(classroom.Id, classroom.Name)));

        ClassFilter.ItemsSource = options;
        ClassFilter.SelectedItem = options.FirstOrDefault(option => option.Id == selectedId) ?? options[0];
    }

    private bool FilterStudent(object item)
    {
        if (item is not Student student)
        {
            return false;
        }

        var keyword = SearchBox.Text.Trim();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var matchesKeyword =
                student.Id.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                student.FullName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                student.Email.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                student.Phone.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                student.ClassName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                student.Address.Contains(keyword, StringComparison.OrdinalIgnoreCase);

            if (!matchesKeyword)
            {
                return false;
            }
        }

        if (_selectedClass is not null && student.ClassId != _selectedClass.Id)
        {
            return false;
        }

        var classId = (ClassFilter.SelectedItem as FilterOption)?.Id ?? string.Empty;
        if (!string.IsNullOrEmpty(classId) && student.ClassId != classId)
        {
            return false;
        }

        var gender = (GenderFilter.SelectedItem as FilterOption)?.Id ?? string.Empty;
        if (!string.IsNullOrEmpty(gender) && student.Gender != gender)
        {
            return false;
        }

        var status = (StatusFilter.SelectedItem as FilterOption)?.Id ?? string.Empty;
        if (!string.IsNullOrEmpty(status) && student.Status != status)
        {
            return false;
        }

        return true;
    }

    private void ApplySort()
    {
        if (_view is not ListCollectionView view)
        {
            return;
        }

        var key = (SortFilter.SelectedItem as FilterOption)?.Id ?? "name_asc";
        view.SortDescriptions.Clear();

        switch (key)
        {
            case "name_desc":
                view.SortDescriptions.Add(new SortDescription(nameof(Student.FullName), ListSortDirection.Descending));
                break;
            case "score_desc":
                view.SortDescriptions.Add(new SortDescription(nameof(Student.Score), ListSortDirection.Descending));
                view.SortDescriptions.Add(new SortDescription(nameof(Student.FullName), ListSortDirection.Ascending));
                break;
            case "score_asc":
                view.SortDescriptions.Add(new SortDescription(nameof(Student.Score), ListSortDirection.Ascending));
                view.SortDescriptions.Add(new SortDescription(nameof(Student.FullName), ListSortDirection.Ascending));
                break;
            case "id_asc":
                view.SortDescriptions.Add(new SortDescription(nameof(Student.Id), ListSortDirection.Ascending));
                break;
            case "dob_desc":
                view.SortDescriptions.Add(new SortDescription(nameof(Student.DateOfBirth), ListSortDirection.Descending));
                break;
            default:
                view.SortDescriptions.Add(new SortDescription(nameof(Student.FullName), ListSortDirection.Ascending));
                break;
        }
    }

    private void ApplyFilters()
    {
        if (_isRefreshing)
        {
            return;
        }

        _view.Refresh();
        UpdateGridStatus();
        UpdateActionButtons();
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (SearchWatermark is null)
        {
            return;
        }

        SearchWatermark.Visibility = string.IsNullOrEmpty(SearchBox.Text)
            ? Visibility.Visible
            : Visibility.Collapsed;

        ApplyFilters();
        if (_selectedClass is null)
        {
            UpdateClassCards();
        }
    }

    private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyFilters();
    }

    private void SortFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isRefreshing)
        {
            return;
        }

        ApplySort();
        UpdateGridStatus();
    }

    private void ResetFilters_Click(object sender, RoutedEventArgs e)
    {
        _isRefreshing = true;
        try
        {
            SearchBox.Clear();
            if (ClassFilter.Items.Count > 0)
            {
                ClassFilter.SelectedIndex = 0;
            }

            GenderFilter.SelectedIndex = 0;
            StatusFilter.SelectedIndex = 0;
            SortFilter.SelectedIndex = 0;
        }
        finally
        {
            _isRefreshing = false;
        }

        ApplySort();
        ApplyFilters();
        SearchWatermark.Visibility = Visibility.Visible;
    }

    private void StudentsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateActionButtons();
    }

    private void UpdateSummary()
    {
        var students = _selectedClass is null
            ? DataStore.MyStudents.AsEnumerable()
            : DataStore.MyStudents.Where(s => s.ClassId == _selectedClass.Id);
        var studentList = students.ToList();
        StatTotalText.Text = studentList.Count.ToString();
        StatActiveText.Text = studentList.Count(s => s.Status == StudentStatuses.Active).ToString();
        StatAverageText.Text = studentList.Count > 0
            ? studentList.Average(s => s.Score).ToString("0.0")
            : "--";
        StatAtRiskText.Text = studentList.Count(s => s.Score < 5).ToString();
    }

    private void UpdateGridStatus()
    {
        var filtered = _view.Cast<object>().Count();
        var total = _selectedClass is null
            ? DataStore.MyStudents.Count
            : DataStore.MyStudents.Count(s => s.ClassId == _selectedClass.Id);
        StudentsCountText.Text = filtered == total
            ? $"{total} sinh viên"
            : $"{filtered} / {total} sinh viên";

        EmptyState.Visibility = filtered == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateActionButtons()
    {
        var hasSelection = StudentsGrid.SelectedItem is Student;
        EditStudentButton.IsEnabled = hasSelection;
        DeleteStudentButton.IsEnabled = hasSelection;
    }

    private sealed class FilterOption
    {
        public FilterOption(string id, string name)
        {
            Id = id;
            Name = name;
        }

        public string Id { get; }
        public string Name { get; }

        public override string ToString() => Name;
    }

    public class ClassCardItem
    {
        public Classroom Classroom { get; set; } = null!;
        public string IconGlyph { get; set; } = string.Empty;
        public Brush IconForeground { get; set; } = Brushes.Gray;
        public Brush IconBackground { get; set; } = Brushes.Transparent;
    }
}
