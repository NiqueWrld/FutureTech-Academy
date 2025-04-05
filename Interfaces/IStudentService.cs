using FutureTech_Academy.Models;

namespace FutureTech_Academy.Interfaces
{
    public interface IStudentService
    {
        Task<Student> CreateStudentAsync(Student student);
        Task<Student> GetStudentByIdAsync(string id);
        Task<IEnumerable<Student>> GetAllStudentsAsync(int pageNumber = 1, int pageSize = 10);
        Task<IEnumerable<Student>> SearchStudentsAsync(string searchTerm);
        Task<Student> UpdateStudentAsync(Student student);
        Task<bool> DeleteStudentAsync(string id, bool permanentDelete = false);
        Task<string> UploadProfileImageAsync(IFormFile file, string studentId);
    }
} 