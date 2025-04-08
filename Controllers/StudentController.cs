using Microsoft.AspNetCore.Mvc;
using FutureTech_Academy.Models;
using FutureTech_Academy.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace FutureTech_Academy.Controllers
{
    [Authorize]
    public class StudentController : Controller
    {
        private readonly IStudentService _studentService;

        public StudentController(IStudentService studentService)
        {
            _studentService = studentService;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            try
            {
                Console.WriteLine("Attempting to get all students...");
                var students = await _studentService.GetAllStudentsAsync(page, pageSize);
                
                if (students == null || !students.Any())
                {
                    Console.WriteLine("No students found in the database");
                    ViewBag.Message = "No students found in the database.";
                    return View(new List<Student>());
                }

                Console.WriteLine($"Successfully retrieved {students.Count()} students");
                return View(students);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in StudentController.Index: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                TempData["ErrorMessage"] = "An error occurred while retrieving students. Please check the console for details.";
                return View(new List<Student>());
            }
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FirstName,LastName,Email,MobileNumber,EnrolmentStatus")] Student student, IFormFile? profileImage)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Generate a new ID for the student
                    student.id = Guid.NewGuid().ToString();
                    
                    // Handle profile image upload if provided
                    if (profileImage != null && profileImage.Length > 0)
                    {
                        Console.WriteLine($"Processing profile image upload for student {student.id}");
                        Console.WriteLine($"File name: {profileImage.FileName}");
                        Console.WriteLine($"Content type: {profileImage.ContentType}");
                        Console.WriteLine($"File size: {profileImage.Length} bytes");

                        if (profileImage.ContentType != "image/jpeg" && profileImage.ContentType != "image/png")
                        {
                            ModelState.AddModelError("ProfileImage", "Only JPEG and PNG images are allowed.");
                            return View(student);
                        }

                        if (profileImage.Length > 5 * 1024 * 1024) // 5MB limit
                        {
                            ModelState.AddModelError("ProfileImage", "Image size should not exceed 5MB.");
                            return View(student);
                        }

                        student.ProfileImageUrl = await _studentService.UploadProfileImageAsync(profileImage, student.id);
                        Console.WriteLine($"Profile image uploaded successfully. URL: {student.ProfileImageUrl}");
                    }

                    // Create the student and get the created student back
                    var createdStudent = await _studentService.CreateStudentAsync(student);
                    
                    // Set success message
                    TempData["SuccessMessage"] = $"Student {createdStudent.FirstName} {createdStudent.LastName} created successfully!";
                    
                    // Redirect to the list view
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in StudentController.Create: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    }
                    ModelState.AddModelError("", "An error occurred while creating the student. Please check the console for details.");
                }
            }
            return View(student);
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var student = await _studentService.GetStudentByIdAsync(id);
            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("id,FirstName,LastName,Email,MobileNumber,EnrolmentStatus,ProfileImageUrl")] Student student, IFormFile? profileImage)
        {
            if (id != student.id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Handle profile image upload if provided
                    if (profileImage != null && profileImage.Length > 0)
                    {
                        Console.WriteLine($"Processing profile image upload for student {student.id}");
                        Console.WriteLine($"File name: {profileImage.FileName}");
                        Console.WriteLine($"Content type: {profileImage.ContentType}");
                        Console.WriteLine($"File size: {profileImage.Length} bytes");

                        if (profileImage.ContentType != "image/jpeg" && profileImage.ContentType != "image/png")
                        {
                            ModelState.AddModelError("ProfileImage", "Only JPEG and PNG images are allowed.");
                            return View(student);
                        }

                        if (profileImage.Length > 5 * 1024 * 1024) // 5MB limit
                        {
                            ModelState.AddModelError("ProfileImage", "Image size should not exceed 5MB.");
                            return View(student);
                        }

                        student.ProfileImageUrl = await _studentService.UploadProfileImageAsync(profileImage, student.id);
                        Console.WriteLine($"Profile image uploaded successfully. URL: {student.ProfileImageUrl}");
                    }

                    await _studentService.UpdateStudentAsync(student);
                    TempData["SuccessMessage"] = $"Student {student.FirstName} {student.LastName} updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in StudentController.Edit: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    }
                    ModelState.AddModelError("", "An error occurred while updating the student. Please check the console for details.");
                }
            }
            return View(student);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id, bool permanentDelete = false)
        {
            var result = await _studentService.DeleteStudentAsync(id, permanentDelete);
            if (result)
            {
                TempData["SuccessMessage"] = permanentDelete ? 
                    "Student permanently deleted!" : 
                    "Student marked as inactive!";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete student.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Search(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return RedirectToAction(nameof(Index));
            }

            var students = await _studentService.SearchStudentsAsync(searchTerm);
            return View("Index", students);
        }

        // GET: Student/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var student = await _studentService.GetStudentByIdAsync(id);
                if (student == null)
                {
                    return NotFound();
                }

                return View(student);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in StudentController.Details: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                TempData["ErrorMessage"] = "An error occurred while retrieving student details.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
} 