using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using QuanLySinhVien.Services;

namespace QuanLySinhVien.Dialogs;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        LoadAuthorAvatar();
        DataPathText.Text = $"Tệp dữ liệu: {DataStore.DataFilePath}";
        SourceInitialized += (_, _) => ThemeManager.ApplyTitleBar(this);
    }

    private void LoadAuthorAvatar()
    {
        try
        {
            var bitmap = LoadEmbeddedAuthorAvatar();
            AuthorAvatarImage.Source = bitmap;
            AuthorAvatarImage.Clip = new EllipseGeometry(new Point(99, 99), 99, 99);
            AuthorAvatarImage.Visibility = Visibility.Visible;
            AuthorInitials.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex) when (ex is IOException or NotSupportedException or InvalidOperationException or UriFormatException)
        {
            // Giữ chữ viết tắt nếu ảnh chưa sẵn sàng hoặc không đọc được.
        }
    }

    private static BitmapSource LoadEmbeddedAuthorAvatar()
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.UriSource = new Uri("pack://application:,,,/Assets/author-avatar.png", UriKind.Absolute);
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    private void SocialLink_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string url })
            return;

        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            MessageBox.Show(this, $"Không thể mở liên kết.\n{ex.Message}", "Lỗi mở liên kết", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
