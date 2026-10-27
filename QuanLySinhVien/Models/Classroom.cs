using System.Text.Json.Serialization;

namespace QuanLySinhVien.Models;

public class Classroom : ObservableObject
{
    private string _id = string.Empty;
    private string _name = string.Empty;
    private string _subject = string.Empty;
    private string _major = string.Empty;
    private string _schoolYear = string.Empty;
    private string _room = string.Empty;
    private string _lecturerId = string.Empty;
    private int _studentCount;
    private double _averageScore;

    public string Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string Subject
    {
        get => _subject;
        set => SetProperty(ref _subject, value);
    }

    public string Major
    {
        get => _major;
        set => SetProperty(ref _major, value);
    }

    public string SchoolYear
    {
        get => _schoolYear;
        set => SetProperty(ref _schoolYear, value);
    }

    public string Room
    {
        get => _room;
        set => SetProperty(ref _room, value);
    }

    public string LecturerId
    {
        get => _lecturerId;
        set => SetProperty(ref _lecturerId, value);
    }

    [JsonIgnore]
    public int StudentCount
    {
        get => _studentCount;
        set
        {
            if (SetProperty(ref _studentCount, value))
            {
                OnPropertyChanged(nameof(StudentCountText));
            }
        }
    }

    [JsonIgnore]
    public double AverageScore
    {
        get => _averageScore;
        set
        {
            if (SetProperty(ref _averageScore, value))
            {
                OnPropertyChanged(nameof(AverageScoreText));
            }
        }
    }

    [JsonIgnore]
    public string StudentCountText => $"{StudentCount} sinh viên";

    [JsonIgnore]
    public string AverageScoreText => StudentCount == 0 ? "--" : AverageScore.ToString("0.0");

    [JsonIgnore]
    public string ShortName
    {
        get
        {
            var parts = Name.Split('-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 0 ? parts[0] : Name;
        }
    }

    public Classroom Clone()
    {
        return (Classroom)MemberwiseClone();
    }

    public void CopyFrom(Classroom other)
    {
        Id = other.Id;
        Name = other.Name;
        Subject = other.Subject;
        Major = other.Major;
        SchoolYear = other.SchoolYear;
        Room = other.Room;
        LecturerId = other.LecturerId;
    }
}
