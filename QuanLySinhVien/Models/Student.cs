using System.Text.Json.Serialization;

namespace QuanLySinhVien.Models;

public static class StudentStatuses
{
    public const string Active = "Đang học";
    public const string Reserved = "Bảo lưu";
    public const string Graduated = "Tốt nghiệp";
    public const string Dropped = "Thôi học";

    public static readonly string[] All = { Active, Reserved, Graduated, Dropped };
}

public class Student : ObservableObject
{
    private string _id = string.Empty;
    private string _fullName = string.Empty;
    private DateTime _dateOfBirth = new(2005, 1, 1);
    private string _gender = "Nam";
    private string _email = string.Empty;
    private string _phone = string.Empty;
    private string _address = string.Empty;
    private string _classId = string.Empty;
    private string _className = string.Empty;
    private double _score;
    private string _status = StudentStatuses.Active;

    public string Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public string FullName
    {
        get => _fullName;
        set
        {
            if (SetProperty(ref _fullName, value))
            {
                OnPropertyChanged(nameof(Initials));
            }
        }
    }

    public DateTime DateOfBirth
    {
        get => _dateOfBirth;
        set => SetProperty(ref _dateOfBirth, value);
    }

    public string Gender
    {
        get => _gender;
        set => SetProperty(ref _gender, value);
    }

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string Phone
    {
        get => _phone;
        set => SetProperty(ref _phone, value);
    }

    public string Address
    {
        get => _address;
        set => SetProperty(ref _address, value);
    }

    public string ClassId
    {
        get => _classId;
        set => SetProperty(ref _classId, value);
    }

    [JsonIgnore]
    public string ClassName
    {
        get => _className;
        set => SetProperty(ref _className, value);
    }

    public double Score
    {
        get => _score;
        set
        {
            if (SetProperty(ref _score, value))
            {
                OnPropertyChanged(nameof(ScoreText));
                OnPropertyChanged(nameof(Rank));
            }
        }
    }

    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    [JsonIgnore]
    public string Initials
    {
        get
        {
            var parts = FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return "SV";
            }

            if (parts.Length == 1)
            {
                return parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant();
            }

            return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
        }
    }

    [JsonIgnore]
    public string ScoreText => Score.ToString("0.0");

    [JsonIgnore]
    public string Rank => Score switch
    {
        >= 9.0 => "Xuất sắc",
        >= 8.0 => "Giỏi",
        >= 6.5 => "Khá",
        >= 5.0 => "Trung bình",
        _ => "Yếu"
    };

    public Student Clone()
    {
        return (Student)MemberwiseClone();
    }

    public void CopyFrom(Student other)
    {
        Id = other.Id;
        FullName = other.FullName;
        DateOfBirth = other.DateOfBirth;
        Gender = other.Gender;
        Email = other.Email;
        Phone = other.Phone;
        Address = other.Address;
        ClassId = other.ClassId;
        Score = other.Score;
        Status = other.Status;
    }
}
