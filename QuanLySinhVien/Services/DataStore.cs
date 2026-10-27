using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using QuanLySinhVien.Models;

namespace QuanLySinhVien.Services;

public class DataSnapshot
{
    public List<Lecturer> Lecturers { get; set; } = new();
    public List<Classroom> Classes { get; set; } = new();
    public List<Student> Students { get; set; } = new();
}

public static class DataStore
{
    public static IReadOnlyList<string> LecturerDepartments { get; } = Array.AsReadOnly(new[]
    {
        "Khoa Công nghệ thông tin", "Khoa Khoa học máy tính", "Khoa Hệ thống thông tin",
        "Khoa An toàn thông tin", "Khoa Kinh tế", "Khoa Kế toán", "Khoa Tài chính - Ngân hàng",
        "Khoa Quản trị kinh doanh", "Khoa Ngôn ngữ Anh", "Khoa Luật", "Khoa Du lịch",
        "Khoa Kỹ thuật công trình", "Khoa Điện - Điện tử", "Khoa Thiết kế đồ họa"
    });

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static readonly string DataDirectory = AppContext.BaseDirectory;

    private static readonly string DataFile = Path.Combine(DataDirectory, "data.json");

    public static DataSnapshot Storage { get; private set; } = new();

    public static Lecturer? CurrentLecturer { get; private set; }

    public static ObservableCollection<Classroom> MyClasses { get; } = new();

    public static ObservableCollection<Student> MyStudents { get; } = new();

    public static string DataFilePath => DataFile;

    public static void Load()
    {
        try
        {
            if (!File.Exists(DataFile))
            {
                var legacyDataFile = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Data", "data.json"));
                if (File.Exists(legacyDataFile))
                {
                    Directory.CreateDirectory(DataDirectory);
                    File.Copy(legacyDataFile, DataFile);
                }
            }

            if (File.Exists(DataFile))
            {
                var json = File.ReadAllText(DataFile);
                var loaded = JsonSerializer.Deserialize<DataSnapshot>(json, JsonOptions);
                if (loaded is not null &&
                    loaded.Lecturers is not null &&
                    loaded.Lecturers.Count > 0 &&
                    loaded.Classes is not null &&
                    loaded.Students is not null)
                {
                    Storage = loaded;
                }
                else
                {
                    Storage = CreateSeedData();
                    Save();
                }
            }
            else
            {
                Storage = CreateSeedData();
                Save();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Không thể tải dữ liệu: {ex.Message}");
            Storage = CreateSeedData();
            Save();
        }

        RefreshDerived();
    }

    public static bool Login(string username, string password)
    {
        var lecturer = Storage.Lecturers.FirstOrDefault(l =>
            string.Equals(l.Username, username.Trim(), StringComparison.OrdinalIgnoreCase) &&
            l.Password == password);

        if (lecturer is null)
        {
            return false;
        }

        CurrentLecturer = lecturer;
        RefreshCurrent();
        return true;
    }

    public static bool RegisterLecturer(string fullName, string username, string password, string department, string degree, out string error)
    {
        fullName = fullName.Trim();
        username = username.Trim();

        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            error = "Vui lòng nhập họ tên, tên đăng nhập và mật khẩu.";
            return false;
        }

        if (username.Length < 4)
        {
            error = "Tên đăng nhập phải có ít nhất 4 ký tự.";
            return false;
        }

        if (password.Length < 6)
        {
            error = "Mật khẩu phải có ít nhất 6 ký tự.";
            return false;
        }

        if (Storage.Lecturers.Any(l => string.Equals(l.Username, username, StringComparison.OrdinalIgnoreCase)))
        {
            error = "Tên đăng nhập này đã được sử dụng.";
            return false;
        }

        var nextId = Storage.Lecturers
            .Select(l => l.Id.Length > 2 && int.TryParse(l.Id[2..], out var number) ? number : 0)
            .DefaultIfEmpty(0)
            .Max() + 1;

        Storage.Lecturers.Add(new Lecturer
        {
            Id = $"GV{nextId:000}",
            Username = username,
            Password = password,
            FullName = fullName,
            Degree = string.IsNullOrWhiteSpace(degree) ? "Giảng viên" : degree.Trim(),
            Department = department.Trim(),
            Gender = "Nam",
            DateOfBirth = new DateTime(1990, 1, 1)
        });
        Save();
        error = string.Empty;
        return true;
    }

    public static void Logout()
    {
        CurrentLecturer = null;
        MyClasses.Clear();
        MyStudents.Clear();
    }

    public static void RefreshCurrent()
    {
        MyClasses.Clear();
        MyStudents.Clear();

        if (CurrentLecturer is null)
        {
            return;
        }

        foreach (var classroom in Storage.Classes
                     .Where(c => c.LecturerId == CurrentLecturer.Id)
                     .OrderBy(c => c.Id))
        {
            MyClasses.Add(classroom);
        }

        var classIds = MyClasses.Select(c => c.Id).ToHashSet();
        foreach (var student in Storage.Students
                     .Where(s => classIds.Contains(s.ClassId))
                     .OrderBy(s => s.Id))
        {
            MyStudents.Add(student);
        }

        RefreshDerived();
    }

    public static void RefreshDerived()
    {
        var studentsByClass = Storage.Students
            .GroupBy(s => s.ClassId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var classroom in Storage.Classes)
        {
            if (studentsByClass.TryGetValue(classroom.Id, out var students))
            {
                classroom.StudentCount = students.Count;
                classroom.AverageScore = students.Count == 0 ? 0 : students.Average(s => s.Score);
            }
            else
            {
                classroom.StudentCount = 0;
                classroom.AverageScore = 0;
            }
        }

        var classNames = Storage.Classes.ToDictionary(c => c.Id, c => c.Name);
        foreach (var student in Storage.Students)
        {
            student.ClassName = classNames.TryGetValue(student.ClassId, out var name) ? name : "--";
        }
    }

    public static string NextStudentId()
    {
        var max = Storage.Students
            .Select(s => s.Id.Length > 2 && int.TryParse(s.Id[2..], out var number) ? number : 0)
            .DefaultIfEmpty(0)
            .Max();

        return $"SV{max + 1:000}";
    }

    public static bool IsStudentIdTaken(string id, Student? except = null)
    {
        return Storage.Students.Any(s =>
            !ReferenceEquals(s, except) &&
            string.Equals(s.Id, id.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsClassIdTaken(string id, Classroom? except = null)
    {
        return Storage.Classes.Any(c =>
            !ReferenceEquals(c, except) &&
            string.Equals(c.Id, id.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public static void AddStudent(Student student)
    {
        Storage.Students.Add(student);
        if (CurrentLecturer is not null &&
            MyClasses.Any(c => c.Id == student.ClassId))
        {
            MyStudents.Add(student);
        }

        RefreshDerived();
        Save();
    }

    public static void UpdateStudent(Student target, Student updated)
    {
        var wasMine = MyStudents.Contains(target);
        target.CopyFrom(updated);

        if (CurrentLecturer is not null && MyClasses.Any(c => c.Id == target.ClassId))
        {
            if (!MyStudents.Contains(target))
            {
                MyStudents.Add(target);
            }
        }
        else if (wasMine)
        {
            MyStudents.Remove(target);
        }

        RefreshDerived();
        Save();
    }

    public static void DeleteStudent(Student student)
    {
        Storage.Students.Remove(student);
        MyStudents.Remove(student);
        RefreshDerived();
        Save();
    }

    public static void AddClass(Classroom classroom)
    {
        Storage.Classes.Add(classroom);
        if (CurrentLecturer is not null && classroom.LecturerId == CurrentLecturer.Id)
        {
            MyClasses.Add(classroom);
        }

        RefreshDerived();
        Save();
    }

    public static void UpdateClass(Classroom target, Classroom updated)
    {
        var oldId = target.Id;
        target.CopyFrom(updated);

        if (!string.Equals(oldId, target.Id, StringComparison.OrdinalIgnoreCase))
        {
            foreach (var student in Storage.Students.Where(s => s.ClassId == oldId))
            {
                student.ClassId = target.Id;
            }
        }

        RefreshDerived();
        Save();
    }

    public static int DeleteClass(Classroom classroom)
    {
        var students = Storage.Students.Where(s => s.ClassId == classroom.Id).ToList();
        foreach (var student in students)
        {
            Storage.Students.Remove(student);
            MyStudents.Remove(student);
        }

        Storage.Classes.Remove(classroom);
        MyClasses.Remove(classroom);
        RefreshDerived();
        Save();
        return students.Count;
    }

    public static void UpdateLecturer(Lecturer updated)
    {
        if (CurrentLecturer is null)
        {
            return;
        }

        CurrentLecturer.CopyFrom(updated);
        RefreshDerived();
        Save();
    }

    public static void Save()
    {
        try
        {
            Directory.CreateDirectory(DataDirectory);
            var json = JsonSerializer.Serialize(Storage, JsonOptions);
            File.WriteAllText(DataFile, json);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Không thể lưu dữ liệu: {ex.Message}");
        }
    }

    private static DataSnapshot CreateSeedData()
    {
        var lecturers = new List<Lecturer>
        {
            new()
            {
                Id = "GV001",
                Username = "gv.minh",
                Password = "123456",
                FullName = "Nguyễn Văn Minh",
                Email = "minh.nv@university.edu.vn",
                Phone = "0912345678",
                Degree = "TS.",
                Department = "Khoa Công nghệ thông tin",
                Gender = "Nam",
                DateOfBirth = new DateTime(1980, 5, 12)
            },
            new()
            {
                Id = "GV002",
                Username = "gv.lan",
                Password = "123456",
                FullName = "Trần Thị Ngọc Lan",
                Email = "lan.ttn@university.edu.vn",
                Phone = "0987654321",
                Degree = "ThS.",
                Department = "Khoa Kinh tế",
                Gender = "Nữ",
                DateOfBirth = new DateTime(1985, 9, 3)
            }
        };

        var classes = new List<Classroom>
        {
            new()
            {
                Id = "CNTT01",
                Name = "Công nghệ phần mềm - K18",
                Subject = "Lập trình hướng đối tượng",
                Major = "Công nghệ thông tin",
                SchoolYear = "2024 - 2028",
                Room = "A2-301",
                LecturerId = "GV001"
            },
            new()
            {
                Id = "CNTT02",
                Name = "Khoa học máy tính - K18",
                Subject = "Cấu trúc dữ liệu & Giải thuật",
                Major = "Khoa học máy tính",
                SchoolYear = "2024 - 2028",
                Room = "A2-305",
                LecturerId = "GV001"
            },
            new()
            {
                Id = "CNTT03",
                Name = "Hệ thống thông tin - K17",
                Subject = "Cơ sở dữ liệu",
                Major = "Hệ thống thông tin",
                SchoolYear = "2023 - 2027",
                Room = "B1-204",
                LecturerId = "GV001"
            },
            new()
            {
                Id = "CNTT04",
                Name = "An toàn thông tin - K18",
                Subject = "Mạng máy tính",
                Major = "An toàn thông tin",
                SchoolYear = "2024 - 2028",
                Room = "B2-108",
                LecturerId = "GV001"
            },
            new()
            {
                Id = "KT01",
                Name = "Kế toán doanh nghiệp - K18",
                Subject = "Nguyên lý kế toán",
                Major = "Kế toán",
                SchoolYear = "2024 - 2028",
                Room = "C1-201",
                LecturerId = "GV002"
            },
            new()
            {
                Id = "KT02",
                Name = "Tài chính ngân hàng - K18",
                Subject = "Tài chính doanh nghiệp",
                Major = "Tài chính - Ngân hàng",
                SchoolYear = "2024 - 2028",
                Room = "C1-305",
                LecturerId = "GV002"
            }
        };

        var names = new (string Name, string Gender)[]
        {
            ("Nguyễn Hoàng Anh", "Nữ"),
            ("Trần Minh Khang", "Nam"),
            ("Lê Thị Thu Hà", "Nữ"),
            ("Phạm Đức Long", "Nam"),
            ("Hoàng Ngọc Mai", "Nữ"),
            ("Vũ Quốc Bảo", "Nam"),
            ("Đặng Thanh Huyền", "Nữ"),
            ("Bùi Anh Tuấn", "Nam"),
            ("Ngô Thùy Linh", "Nữ"),
            ("Đỗ Văn Nam", "Nam"),
            ("Nguyễn Phương Thảo", "Nữ"),
            ("Trần Quang Huy", "Nam"),
            ("Lê Minh Châu", "Nữ"),
            ("Phạm Gia Hân", "Nữ"),
            ("Võ Thành Đạt", "Nam"),
            ("Đinh Khánh Vy", "Nữ"),
            ("Nguyễn Trung Kiên", "Nam"),
            ("Trần Thị Kim Ngân", "Nữ"),
            ("Lương Hoài Nam", "Nam"),
            ("Mai Thanh Tú", "Nữ"),
            ("Phan Nhật Minh", "Nam"),
            ("Tạ Thu Trang", "Nữ"),
            ("Nguyễn Đức Anh", "Nam"),
            ("Hồ Bảo Ngọc", "Nữ"),
            ("Lý Gia Bảo", "Nam"),
            ("Trịnh Thị Mai", "Nữ"),
            ("Cao Minh Quân", "Nam"),
            ("Đào Ngọc Ánh", "Nữ"),
            ("Nguyễn Hải Đăng", "Nam"),
            ("Vũ Thị Hồng", "Nữ"),
            ("Trần Tuấn Kiệt", "Nam"),
            ("Phạm Thanh Hương", "Nữ"),
            ("Lê Quốc Cường", "Nam"),
            ("Nguyễn Thị Diễm", "Nữ")
        };

        var addresses = new[]
        {
            "Cầu Giấy, Hà Nội",
            "Lê Chân, Hải Phòng",
            "Hải Châu, Đà Nẵng",
            "Quận 7, TP. Hồ Chí Minh",
            "Ninh Kiều, Cần Thơ",
            "Vinh, Nghệ An",
            "Thanh Xuân, Hà Nội",
            "Hạ Long, Quảng Ninh",
            "Từ Sơn, Bắc Ninh",
            "TP. Huế"
        };

        var statusOverrides = new Dictionary<int, string>
        {
            [5] = StudentStatuses.Reserved,
            [11] = StudentStatuses.Reserved,
            [17] = StudentStatuses.Dropped,
            [22] = StudentStatuses.Graduated,
            [26] = StudentStatuses.Reserved,
            [31] = StudentStatuses.Dropped
        };

        var random = new Random(2026);
        var students = new List<Student>();

        for (var i = 0; i < names.Length; i++)
        {
            var classId = i < 24
                ? classes[i % 4].Id
                : classes[4 + (i - 24) % 2].Id;

            var score = Math.Round(4.2 + random.NextDouble() * 5.5, 1);
            var id = $"SV{i + 1:000}";

            students.Add(new Student
            {
                Id = id,
                FullName = names[i].Name,
                Gender = names[i].Gender,
                DateOfBirth = new DateTime(2004 + i % 3, 1 + (i * 5) % 12, 1 + (i * 7) % 27),
                Email = $"{id.ToLowerInvariant()}@student.edu.vn",
                Phone = $"09{random.Next(10000000, 99999999)}",
                Address = addresses[i % addresses.Length],
                ClassId = classId,
                Score = score,
                Status = statusOverrides.TryGetValue(i, out var status) ? status : StudentStatuses.Active
            });
        }

        return new DataSnapshot
        {
            Lecturers = lecturers,
            Classes = classes,
            Students = students
        };
    }
}
