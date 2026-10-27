using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using QuanLySinhVien.Services;

namespace QuanLySinhVien.Dialogs;

public partial class AvatarCropDialog : Window
{
    private const double PreviewSize = 300;
    private BitmapSource _source = null!;
    private double _baseScale;
    private double _imageWidth;
    private double _imageHeight;
    private Point _dragStart;
    private double _imageLeftAtDragStart;
    private double _imageTopAtDragStart;
    private bool _isDragging;

    public byte[]? CroppedImageBytes { get; private set; }

    public AvatarCropDialog(string sourcePath)
        : this(LoadSourceFromFile(sourcePath))
    {
    }

    public AvatarCropDialog(BitmapSource source)
    {
        InitializeComponent();
        SourceInitialized += (_, _) => ThemeManager.ApplyTitleBar(this);
        _source = source;

        _baseScale = Math.Max(PreviewSize / _source.PixelWidth, PreviewSize / _source.PixelHeight);
        PreviewImage.Source = _source;
        UpdateImageSize(1, preserveCenter: false);
    }

    private static BitmapSource LoadSourceFromFile(string sourcePath)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.UriSource = new Uri(sourcePath, UriKind.Absolute);
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_source is null || PreviewImage is null || _baseScale <= 0)
        {
            return;
        }

        UpdateImageSize(e.NewValue, preserveCenter: true);
    }

    private void UpdateImageSize(double zoom, bool preserveCenter)
    {
        var oldCenterX = Canvas.GetLeft(PreviewImage) + _imageWidth / 2;
        var oldCenterY = Canvas.GetTop(PreviewImage) + _imageHeight / 2;

        _imageWidth = _source.PixelWidth * _baseScale * zoom;
        _imageHeight = _source.PixelHeight * _baseScale * zoom;
        PreviewImage.Width = _imageWidth;
        PreviewImage.Height = _imageHeight;

        var left = preserveCenter ? oldCenterX - _imageWidth / 2 : (PreviewSize - _imageWidth) / 2;
        var top = preserveCenter ? oldCenterY - _imageHeight / 2 : (PreviewSize - _imageHeight) / 2;
        Canvas.SetLeft(PreviewImage, Clamp(left, PreviewSize - _imageWidth, 0));
        Canvas.SetTop(PreviewImage, Clamp(top, PreviewSize - _imageHeight, 0));
    }

    private void PreviewViewport_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isDragging = true;
        _dragStart = e.GetPosition(PreviewViewport);
        _imageLeftAtDragStart = Canvas.GetLeft(PreviewImage);
        _imageTopAtDragStart = Canvas.GetTop(PreviewImage);
        PreviewViewport.CaptureMouse();
        e.Handled = true;
    }

    private void PreviewViewport_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging)
        {
            return;
        }

        var current = e.GetPosition(PreviewViewport);
        Canvas.SetLeft(PreviewImage, Clamp(_imageLeftAtDragStart + current.X - _dragStart.X, PreviewSize - _imageWidth, 0));
        Canvas.SetTop(PreviewImage, Clamp(_imageTopAtDragStart + current.Y - _dragStart.Y, PreviewSize - _imageHeight, 0));
    }

    private void PreviewViewport_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isDragging = false;
        PreviewViewport.ReleaseMouseCapture();
    }

    private void UseImage_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            const int outputSize = 512;
            var scale = outputSize / PreviewSize;
            var visual = new DrawingVisual();
            using (var drawing = visual.RenderOpen())
            {
                drawing.PushTransform(new ScaleTransform(scale, scale));
                drawing.DrawImage(_source, new Rect(Canvas.GetLeft(PreviewImage), Canvas.GetTop(PreviewImage), _imageWidth, _imageHeight));
                drawing.Pop();
            }

            var cropped = new RenderTargetBitmap(outputSize, outputSize, 96, 96, PixelFormats.Pbgra32);
            cropped.Render(visual);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(cropped));
            using var stream = new MemoryStream();
            encoder.Save(stream);
            CroppedImageBytes = stream.ToArray();
            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Không thể xử lý ảnh này.\n{ex.Message}", "Lỗi ảnh", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static double Clamp(double value, double minimum, double maximum)
    {
        return Math.Max(minimum, Math.Min(maximum, value));
    }
}
