using System.Text.Json.Serialization;

namespace QuanLySinhVien.Models;

public class Lecturer : ObservableObject
{
    private string _id = string.Empty;
    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _fullName = string.Empty;
    private string _email = string.Empty;
    private string _phone = string.Empty;
    private string _degree = string.Empty;
    private string _department = string.Empty;
    private string _gender = "Nam";
    private string _avatarPath = string.Empty;
    private DateTime _dateOfBirth = new(1980, 1, 1);

    public string Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string FullName
    {
        get => _fullName;
        set
        {
            if (SetProperty(ref _fullName, value))
            {
                OnPropertyChanged(nameof(Initials));
                OnPropertyChanged(nameof(DisplayTitle));
            }
        }
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

    public string Degree
    {
        get => _degree;
        set
        {
            if (SetProperty(ref _degree, value))
            {
                OnPropertyChanged(nameof(DisplayTitle));
            }
        }
    }

    public string Department
    {
        get => _department;
        set => SetProperty(ref _department, value);
    }

    public string Gender
    {
        get => _gender;
        set => SetProperty(ref _gender, value);
    }

    public string AvatarPath
    {
        get => _avatarPath;
        set => SetProperty(ref _avatarPath, value);
    }

    public DateTime DateOfBirth
    {
        get => _dateOfBirth;
        set => SetProperty(ref _dateOfBirth, value);
    }

    [JsonIgnore]
    public string Initials
    {
        get
        {
            var parts = FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return "GV";
            }

            if (parts.Length == 1)
            {
                return parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant();
            }

            return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
        }
    }

    [JsonIgnore]
    public string DisplayTitle => $"{Degree} {FullName}".Trim();

    public Lecturer Clone()
    {
        return (Lecturer)MemberwiseClone();
    }

    public void CopyFrom(Lecturer other)
    {
        Id = other.Id;
        Username = other.Username;
        Password = other.Password;
        FullName = other.FullName;
        Email = other.Email;
        Phone = other.Phone;
        Degree = other.Degree;
        Department = other.Department;
        Gender = other.Gender;
        AvatarPath = other.AvatarPath;
        DateOfBirth = other.DateOfBirth;
    }
}
