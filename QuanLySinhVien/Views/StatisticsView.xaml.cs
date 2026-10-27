using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using QuanLySinhVien.Models;
using QuanLySinhVien.Services;

namespace QuanLySinhVien.Views;

public partial class StatisticsView : UserControl
{
    private bool _isRefreshing;

    public StatisticsView()
    {
        InitializeComponent();
        ClassChartCard.AddHandler(
            UIElement.PreviewMouseWheelEvent,
            new System.Windows.Input.MouseWheelEventHandler(ClassChartScrollViewer_PreviewMouseWheel),
            handledEventsToo: true);
        Refresh();
    }

    private void ClassChartScrollViewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
        if ((System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Shift) != 0 &&
            ClassChartScrollViewer.ScrollableWidth > 0)
        {
            var horizontalOffset = Math.Clamp(
                ClassChartScrollViewer.HorizontalOffset - e.Delta / 120d * 105d,
                0,
                ClassChartScrollViewer.ScrollableWidth);
            ClassChartScrollViewer.ScrollToHorizontalOffset(horizontalOffset);
        }
        else
        {
            var verticalOffset = Math.Clamp(
                StatisticsPageScrollViewer.VerticalOffset - e.Delta,
                0,
                StatisticsPageScrollViewer.ScrollableHeight);
            StatisticsPageScrollViewer.ScrollToVerticalOffset(verticalOffset);
        }

        e.Handled = true;
    }

    public void Refresh()
    {
        _isRefreshing = true;
        try
        {
            RebuildScopeFilter();
            Recalculate();
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    private void RebuildScopeFilter()
    {
        var selectedId = (ScopeFilter.SelectedItem as FilterOption)?.Id ?? string.Empty;
        var options = new List<FilterOption> { new(string.Empty, "Tất cả lớp học") };
        options.AddRange(DataStore.MyClasses.Select(classroom => new FilterOption(classroom.Id, classroom.Name)));

        ScopeFilter.ItemsSource = options;
        ScopeFilter.SelectedItem = options.FirstOrDefault(option => option.Id == selectedId) ?? options[0];
    }

    private void Recalculate()
    {
        var selectedId = (ScopeFilter.SelectedItem as FilterOption)?.Id ?? string.Empty;
        var classes = string.IsNullOrEmpty(selectedId)
            ? DataStore.MyClasses.ToList()
            : DataStore.MyClasses.Where(c => c.Id == selectedId).ToList();
        var students = string.IsNullOrEmpty(selectedId)
            ? DataStore.MyStudents.ToList()
            : DataStore.MyStudents.Where(s => s.ClassId == selectedId).ToList();

        ScopeSubtitle.Text = string.IsNullOrEmpty(selectedId)
            ? $"Tổng hợp kết quả học tập của {DataStore.MyClasses.Count} lớp và {DataStore.MyStudents.Count} sinh viên."
            : $"Kết quả học tập của lớp {classes.FirstOrDefault()?.Name ?? "đã chọn"}.";

        StatTotalText.Text = students.Count.ToString();
        StatAverageText.Text = students.Count > 0 ? students.Average(s => s.Score).ToString("0.0") : "--";
        StatPassRateText.Text = students.Count > 0
            ? $"{students.Count(s => s.Score >= 5) * 100.0 / students.Count:0}%"
            : "0%";
        StatHighestText.Text = students.Count > 0 ? students.Max(s => s.Score).ToString("0.0") : "--";

        BuildClassChart(classes, students, selectedId);
        BuildClassRankCharts(classes, students);
        BuildRankDistribution(students);
        BuildTopStudents(students);
        BuildRiskStudents(students);
    }

    private void BuildClassChart(List<Classroom> classes, List<Student> students, string selectedId)
    {
        var palette = new[] { "#4F46E5", "#7C3AED", "#0284C7", "#059669", "#D97706", "#DB2777" };
        var items = new List<ClassChartItem>();

        if (!string.IsNullOrEmpty(selectedId))
        {
            var classroom = classes.FirstOrDefault();
            ClassChartSubtitle.Text = classroom is null
                ? "Điểm từng sinh viên trong lớp đã chọn"
                : $"Điểm của từng sinh viên lớp {classroom.Name}";

            items = students
                .OrderBy(student => student.FullName)
                .Select((student, index) => new ClassChartItem
                {
                    ShortName = student.FullName,
                    FullName = $"{student.FullName} · {student.Id}",
                    CountText = student.Id,
                    AverageText = student.Score.ToString("0.0"),
                    BarHeight = Math.Max(18, student.Score / 10.0 * 155),
                    BarWidth = 32,
                    ItemWidth = 78,
                    Brush = BrushFrom(student.Score >= 9 ? "#7C3AED" : student.Score >= 8 ? "#4F46E5" : student.Score >= 6.5 ? "#0284C7" : student.Score >= 5 ? "#D97706" : "#DC2626")
                })
                .ToList();

            NoClassChartText.Text = "Lớp này chưa có sinh viên để thống kê.";
        }
        else
        {
            ClassChartSubtitle.Text = "So sánh điểm trung bình giữa các lớp";
            NoClassChartText.Text = "Chưa có lớp học để thống kê.";
            for (var i = 0; i < classes.Count; i++)
            {
                var classroom = classes[i];
                var classStudents = students.Where(s => s.ClassId == classroom.Id).ToList();
                var average = classStudents.Count > 0 ? classStudents.Average(s => s.Score) : 0;

                items.Add(new ClassChartItem
                {
                    ShortName = classroom.ShortName,
                    FullName = classroom.Name,
                    CountText = $"{classStudents.Count} SV",
                    AverageText = classStudents.Count > 0 ? average.ToString("0.0") : "--",
                    BarHeight = classStudents.Count > 0 ? Math.Max(18, average / 10.0 * 155) : 8,
                    BarWidth = 46,
                    ItemWidth = 100,
                    Brush = BrushFrom(palette[i % palette.Length])
                });
            }
        }

        ClassChart.ItemsSource = items;
        ClassChart.Visibility = items.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        NoClassChartText.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void BuildClassRankCharts(List<Classroom> classes, List<Student> students)
    {
        var ranks = new (Func<double, bool> Match, string Color)[]
        {
            (score => score >= 9, "#8B5CF6"),
            (score => score >= 8 && score < 9, "#F4622A"),
            (score => score >= 6.5 && score < 8, "#0EA5E9"),
            (score => score >= 5 && score < 6.5, "#EAB308"),
            (score => score < 5, "#EF4444")
        };

        var items = classes.Select(classroom =>
        {
            var classStudents = students.Where(student => student.ClassId == classroom.Id).ToList();
            return new ClassRankChartItem
            {
                ClassName = classroom.Name,
                StudentCountText = $"{classStudents.Count} sinh viên",
                Slices = BuildPieSlices(classStudents, ranks)
            };
        }).ToList();

        ClassRankCharts.ItemsSource = items;
        ClassRankCharts.Visibility = items.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        NoClassRankChartsText.Visibility = items.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
    }

    private static List<PieSliceItem> BuildPieSlices(
        List<Student> students,
        (Func<double, bool> Match, string Color)[] ranks)
    {
        const double center = 74;
        const double radius = 70;
        if (students.Count == 0)
        {
            var empty = new EllipseGeometry(new Rect(center - radius, center - radius, radius * 2, radius * 2));
            empty.Freeze();
            return new List<PieSliceItem> { new() { Geometry = empty, Brush = BrushFrom("#858A94") } };
        }

        var slices = new List<PieSliceItem>();
        var startAngle = -90d;
        foreach (var rank in ranks)
        {
            var count = students.Count(student => rank.Match(student.Score));
            if (count == 0)
            {
                continue;
            }

            var sweepAngle = count * 360d / students.Count;
            Geometry geometry;
            var middleAngle = startAngle + sweepAngle / 2;
            var labelPoint = PointOnCircle(center, radius * 0.59, middleAngle);
            if (sweepAngle >= 359.99)
            {
                geometry = new EllipseGeometry(new Rect(center - radius, center - radius, radius * 2, radius * 2));
            }
            else
            {
                var endAngle = startAngle + sweepAngle;
                var start = PointOnCircle(center, radius, startAngle);
                var end = PointOnCircle(center, radius, endAngle);
                var pie = new StreamGeometry();
                using (var context = pie.Open())
                {
                    context.BeginFigure(new Point(center, center), isFilled: true, isClosed: true);
                    context.LineTo(start, isStroked: true, isSmoothJoin: false);
                    context.ArcTo(end, new Size(radius, radius), rotationAngle: 0,
                        isLargeArc: sweepAngle > 180, SweepDirection.Clockwise,
                        isStroked: true, isSmoothJoin: false);
                    context.LineTo(new Point(center, center), isStroked: true, isSmoothJoin: false);
                }
                pie.Freeze();
                geometry = pie;
                startAngle = endAngle;
            }

            if (geometry.CanFreeze)
            {
                geometry.Freeze();
            }

            slices.Add(new PieSliceItem
            {
                Geometry = geometry,
                Brush = BrushFrom(rank.Color),
                PercentText = $"{count * 100d / students.Count:0}%",
                LabelX = labelPoint.X - 19,
                LabelY = labelPoint.Y - 9
            });
        }

        return slices;
    }

    private static Point PointOnCircle(double center, double radius, double angleDegrees)
    {
        var angle = angleDegrees * Math.PI / 180;
        return new Point(center + radius * Math.Cos(angle), center + radius * Math.Sin(angle));
    }

    private void BuildRankDistribution(List<Student> students)
    {
        var ranks = new (string Label, Func<double, bool> Match, string Color)[]
        {
            ("Xuất sắc", score => score >= 9, "#7C3AED"),
            ("Giỏi", score => score >= 8 && score < 9, "#4F46E5"),
            ("Khá", score => score >= 6.5 && score < 8, "#0284C7"),
            ("Trung bình", score => score >= 5 && score < 6.5, "#D97706"),
            ("Yếu", score => score < 5, "#DC2626")
        };

        var total = students.Count;
        var items = ranks.Select(rank =>
        {
            var count = students.Count(s => rank.Match(s.Score));
            var percent = total == 0 ? 0 : count * 100.0 / total;
            return new DistributionItem
            {
                Label = rank.Label,
                ValueText = $"{count} SV · {percent:0}%",
                Percent = percent,
                Brush = BrushFrom(rank.Color)
            };
        }).ToList();

        RankList.ItemsSource = items;

        if (total == 0)
        {
            RankSummaryText.Text = "Chưa có dữ liệu sinh viên trong phạm vi đã chọn.";
            return;
        }

        var passCount = students.Count(s => s.Score >= 5);
        RankSummaryText.Text = $"{passCount}/{total} sinh viên đạt từ 5.0 trở lên, chiếm {passCount * 100.0 / total:0}%.";
    }

    private void BuildTopStudents(List<Student> students)
    {
        var items = students
            .OrderByDescending(s => s.Score)
            .ThenBy(s => s.FullName)
            .Take(5)
            .Select((student, index) => new TopStudentRow
            {
                Position = index + 1,
                Student = student
            })
            .ToList();

        TopStudents.ItemsSource = items;
        TopStudents.Visibility = items.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        NoTopStudentText.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void BuildRiskStudents(List<Student> students)
    {
        var risk = students
            .Where(s => s.Score < 5)
            .OrderBy(s => s.Score)
            .ThenBy(s => s.FullName)
            .ToList();

        RiskCountText.Text = risk.Count.ToString();
        RiskStudents.ItemsSource = risk.Take(6).ToList();
        RiskStudents.Visibility = risk.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        NoRiskText.Visibility = risk.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ScopeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isRefreshing)
        {
            return;
        }

        Recalculate();
    }

    private static SolidColorBrush BrushFrom(string color)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)!);
        brush.Freeze();
        return brush;
    }

    public class ClassChartItem
    {
        public string ShortName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string CountText { get; set; } = string.Empty;
        public string AverageText { get; set; } = string.Empty;
        public double BarHeight { get; set; }
        public double BarWidth { get; set; }
        public double ItemWidth { get; set; }
        public Brush Brush { get; set; } = Brushes.Indigo;
    }

    public class DistributionItem
    {
        public string Label { get; set; } = string.Empty;
        public string ValueText { get; set; } = string.Empty;
        public double Percent { get; set; }
        public Brush Brush { get; set; } = Brushes.Gray;
    }

    public class ClassRankChartItem
    {
        public string ClassName { get; set; } = string.Empty;
        public string StudentCountText { get; set; } = string.Empty;
        public List<PieSliceItem> Slices { get; set; } = new();
    }

    public class PieSliceItem
    {
        public Geometry Geometry { get; set; } = Geometry.Empty;
        public Brush Brush { get; set; } = Brushes.Gray;
        public string PercentText { get; set; } = string.Empty;
        public double LabelX { get; set; }
        public double LabelY { get; set; }
    }

    public class TopStudentRow
    {
        public int Position { get; set; }
        public Student Student { get; set; } = null!;
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
}
