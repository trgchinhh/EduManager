using System.Windows;
using QuanLySinhVien.Services;

namespace QuanLySinhVien.Dialogs;

public partial class HelpWindow : Window
{
    public HelpWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) => ThemeManager.ApplyTitleBar(this);
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
