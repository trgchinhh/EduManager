using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using QuanLySinhVien.Models;
using QuanLySinhVien.Services;

namespace QuanLySinhVien.Views;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
    }

    public void Refresh()
    {
        var lecturer = DataStore.CurrentLecturer;
        if (lecturer is null)
        {
            return;
        }

        GreetingText.Text = $"Xin chào, {lecturer.FullName}";
        HeaderSubtitle.Text = $"Bạn đang phụ trách {DataStore.MyClasses.Count} lớp học và {DataStore.MyStudents.Count} sinh viên.";
        LecturerAvatarInitials.Text = lecturer.Initials;
        ShowLecturerAvatar(lecturer.AvatarPath);

        TotalStudentsText.Text = DataStore.MyStudents.Count.ToString();
        TotalClassesText.Text = DataStore.MyClasses.Count.ToString();
        ClassCountBadge.Text = $"{DataStore.MyClasses.Count} lớp";

        var scores = DataStore.MyStudents.Select(s => s.Score).ToList();
        AverageScoreText.Text = scores.Count > 0 ? scores.Average().ToString("0.0") : "--";
        PassRateText.Text = scores.Count > 0
            ? $"{scores.Count(score => score >= 5) * 100.0 / scores.Count:0}%"
            : "0%";

        ClassList.ItemsSource = DataStore.MyClasses;
        NoClassText.Visibility = DataStore.MyClasses.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        var topStudents = DataStore.MyStudents
            .OrderByDescending(s => s.Score)
            .ThenBy(s => s.FullName)
            .Take(5)
            .Select((student, index) => new TopStudentItem
            {
                Position = index + 1,
                Student = student
            })
            .ToList();

        TopStudents.ItemsSource = topStudents;
        TopStudents.Visibility = topStudents.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        NoTopStudentText.Visibility = topStudents.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        RankList.ItemsSource = BuildRankDistribution();
        StatusList.ItemsSource = BuildStatusDistribution();
    }

    private void ShowLecturerAvatar(string? path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                LecturerAvatarImage.Fill = null;
                LecturerAvatarImage.Visibility = Visibility.Collapsed;
                LecturerAvatarInitials.Visibility = Visibility.Visible;
                return;
            }

            using var stream = new MemoryStream(File.ReadAllBytes(path));
            var image = BitmapFrame.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            image.Freeze();
            LecturerAvatarImage.Fill = new ImageBrush(image) { Stretch = Stretch.UniformToFill };
            LecturerAvatarImage.Visibility = Visibility.Visible;
            LecturerAvatarInitials.Visibility = Visibility.Collapsed;
        }
        catch
        {
            LecturerAvatarImage.Fill = null;
            LecturerAvatarImage.Visibility = Visibility.Collapsed;
            LecturerAvatarInitials.Visibility = Visibility.Visible;
        }
    }

    private List<DistributionItem> BuildRankDistribution()
    {
        var total = DataStore.MyStudents.Count;
        var ranks = new (string Label, Func<double, bool> Match, string Color, Brush Soft)[]
        {
            ("Xuất sắc", score => score >= 9, "#7C3AED", BrushFrom("#F3E8FF")),
            ("Giỏi", score => score >= 8 && score < 9, "#4F46E5", BrushFrom("#EEF2FF")),
            ("Khá", score => score >= 6.5 && score < 8, "#0284C7", BrushFrom("#E0F2FE")),
            ("Trung bình", score => score >= 5 && score < 6.5, "#D97706", BrushFrom("#FEF3C7")),
            ("Yếu", score => score < 5, "#DC2626", BrushFrom("#FEE2E2"))
        };

        return ranks.Select(rank =>
        {
            var count = DataStore.MyStudents.Count(s => rank.Match(s.Score));
            var percent = total == 0 ? 0 : count * 100.0 / total;
            return new DistributionItem
            {
                Label = rank.Label,
                ValueText = $"{count} · {percent:0}%",
                Percent = percent,
                Brush = BrushFrom(rank.Color),
                SoftBrush = rank.Soft
            };
        }).ToList();
    }

    private List<DistributionItem> BuildStatusDistribution()
    {
        var statuses = new (string Label, string Color, Brush Soft)[]
        {
            (StudentStatuses.Active, "#059669", BrushFrom("#D1FAE5")),
            (StudentStatuses.Reserved, "#D97706", BrushFrom("#FEF3C7")),
            (StudentStatuses.Graduated, "#0284C7", BrushFrom("#E0F2FE")),
            (StudentStatuses.Dropped, "#DC2626", BrushFrom("#FEE2E2"))
        };

        return statuses.Select(status =>
        {
            var count = DataStore.MyStudents.Count(s => s.Status == status.Label);
            return new DistributionItem
            {
                Label = status.Label,
                ValueText = $"{count} SV",
                Percent = 0,
                Brush = BrushFrom(status.Color),
                SoftBrush = status.Soft
            };
        }).ToList();
    }

    private static SolidColorBrush BrushFrom(string color)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)!);
        brush.Freeze();
        return brush;
    }

    public class DistributionItem
    {
        public string Label { get; set; } = string.Empty;
        public string ValueText { get; set; } = string.Empty;
        public double Percent { get; set; }
        public Brush Brush { get; set; } = Brushes.Gray;
        public Brush SoftBrush { get; set; } = Brushes.WhiteSmoke;
    }

    public class TopStudentItem
    {
        public int Position { get; set; }
        public Student Student { get; set; } = null!;
    }
}
