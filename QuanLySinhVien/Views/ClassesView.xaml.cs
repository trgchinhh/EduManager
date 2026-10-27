using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using QuanLySinhVien.Dialogs;
using QuanLySinhVien.Models;
using QuanLySinhVien.Services;

namespace QuanLySinhVien.Views;

public partial class ClassesView : UserControl
{
    private readonly ICollectionView _view;

    public ClassesView()
    {
        InitializeComponent();

        _view = CollectionViewSource.GetDefaultView(DataStore.MyClasses);
        _view.Filter = FilterClass;
        ClassesGrid.ItemsSource = _view;
        Refresh();
    }

    public void Refresh()
    {
        _view.Refresh();
        UpdateSummary();
        UpdateEmptyState();
        UpdateActionButtons();
    }

    public void AddClass()
    {
        SearchBox.Clear();
        var dialog = new ClassDialog
        {
            Owner = Window.GetWindow(this)
        };

        if (dialog.ShowDialog() == true && dialog.ResultClass is not null)
        {
            DataStore.AddClass(dialog.ResultClass);
            Refresh();
            ClassesGrid.SelectedItem = dialog.ResultClass;
            ClassesGrid.ScrollIntoView(dialog.ResultClass);
        }
    }

    private void AddClass_Click(object sender, RoutedEventArgs e)
    {
        AddClass();
    }

    private void EditClass_Click(object sender, RoutedEventArgs e)
    {
        if (ClassesGrid.SelectedItem is Classroom classroom)
        {
            EditClass(classroom);
        }
    }

    private void DeleteClass_Click(object sender, RoutedEventArgs e)
    {
        if (ClassesGrid.SelectedItem is Classroom classroom)
        {
            DeleteClass(classroom);
        }
    }

    private void EditClassRow_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Classroom classroom })
        {
            EditClass(classroom);
        }
    }

    private void DeleteClassRow_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Classroom classroom })
        {
            DeleteClass(classroom);
        }
    }

    private void ClassesGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ClassesGrid.SelectedItem is Classroom classroom)
        {
            EditClass(classroom);
        }
    }

    private void EditClass(Classroom classroom)
    {
        var dialog = new ClassDialog(classroom.Clone())
        {
            Owner = Window.GetWindow(this)
        };

        if (dialog.ShowDialog() == true && dialog.ResultClass is not null)
        {
            DataStore.UpdateClass(classroom, dialog.ResultClass);
            Refresh();
            ClassesGrid.SelectedItem = classroom;
            ClassesGrid.ScrollIntoView(classroom);
        }
    }

    private void DeleteClass(Classroom classroom)
    {
        var studentCount = DataStore.Storage.Students.Count(s => s.ClassId == classroom.Id);
        var message = studentCount > 0
            ? $"Lớp \"{classroom.Name}\" hiện có {studentCount} sinh viên.\n\nXóa lớp sẽ xóa luôn toàn bộ sinh viên của lớp này. Bạn có chắc chắn muốn tiếp tục?"
            : $"Bạn có chắc chắn muốn xóa lớp \"{classroom.Name}\"?";

        if (!Confirm("Xác nhận xóa lớp học", message))
        {
            return;
        }

        DataStore.DeleteClass(classroom);
        Refresh();
    }

    private bool FilterClass(object item)
    {
        if (item is not Classroom classroom)
        {
            return false;
        }

        var keyword = SearchBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return true;
        }

        return classroom.Id.Contains(keyword, StringComparison.OrdinalIgnoreCase)
               || classroom.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase)
               || classroom.Subject.Contains(keyword, StringComparison.OrdinalIgnoreCase)
               || classroom.Major.Contains(keyword, StringComparison.OrdinalIgnoreCase)
               || classroom.Room.Contains(keyword, StringComparison.OrdinalIgnoreCase);
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

        _view.Refresh();
        UpdateEmptyState();
    }

    private void ClassesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateActionButtons();
    }

    private void UpdateSummary()
    {
        StatClassesText.Text = DataStore.MyClasses.Count.ToString();
        StatStudentsText.Text = DataStore.MyStudents.Count.ToString();
        StatAverageText.Text = DataStore.MyStudents.Count > 0
            ? DataStore.MyStudents.Average(s => s.Score).ToString("0.0")
            : "--";
    }

    private void UpdateEmptyState()
    {
        var count = _view.Cast<object>().Count();
        ClassesCountText.Text = $"{count} lớp";
        EmptyState.Visibility = count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateActionButtons()
    {
        var hasSelection = ClassesGrid.SelectedItem is Classroom;
        EditClassButton.IsEnabled = hasSelection;
        DeleteClassButton.IsEnabled = hasSelection;
    }

    private bool Confirm(string title, string message)
    {
        var owner = Window.GetWindow(this);
        var result = owner is null
            ? MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning)
            : MessageBox.Show(owner, message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning);
        return result == MessageBoxResult.Yes;
    }
}
