using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using FutureTech_Academy.Models;
using FutureTech_Academy.Interfaces;

namespace FutureTech_Academy.Services
{
    public class CosmosDbService : IStudentService
    {
        private readonly Container _container;
        private readonly IConfiguration _configuration;
        private readonly BlobStorageService _blobStorageService;

        public CosmosDbService(IConfiguration configuration, BlobStorageService blobStorageService)
        {
            _configuration = configuration;
            _blobStorageService = blobStorageService;
            var cosmosClient = new CosmosClient(
                _configuration["CosmosDb:Endpoint"],
                _configuration["CosmosDb:Key"]
            );

            // Create database if it doesn't exist
            var databaseResponse = cosmosClient.CreateDatabaseIfNotExistsAsync(_configuration["CosmosDb:DatabaseName"]).GetAwaiter().GetResult();
            var database = databaseResponse.Database;

            // Create container if it doesn't exist
            var containerResponse = database.CreateContainerIfNotExistsAsync(
                id: _configuration["CosmosDb:ContainerName"],
                partitionKeyPath: "/id",
                throughput: 400
            ).GetAwaiter().GetResult();

            _container = containerResponse.Container;

            // Add dummy student if none exists
            AddDummyStudentIfNeeded().GetAwaiter().GetResult();
        }

        private async Task AddDummyStudentIfNeeded()
        {
            try
            {
                // Check if any students exist
                var query = new QueryDefinition("SELECT VALUE COUNT(1) FROM c");
                var countIterator = _container.GetItemQueryIterator<int>(query);
                var countResponse = await countIterator.ReadNextAsync();
                var count = countResponse.FirstOrDefault();

                if (count == 0)
                {
                    // Create dummy student
                    var dummyStudent = new Student
                    {
                        id = "student-001",
                        FirstName = "John",
                        LastName = "Doe",
                        Email = "john.doe@example.com",
                        MobileNumber = "+1234567890",
                        EnrolmentStatus = "Active",
                        ProfileImageUrl = ""
                    };

                    // Add the dummy student
                    await _container.CreateItemAsync(dummyStudent, new PartitionKey(dummyStudent.id));
                    Console.WriteLine("Dummy student added successfully");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding dummy student: {ex.Message}");
            }
        }

        public async Task<Student> CreateStudentAsync(Student student)
        {
            if (string.IsNullOrEmpty(student.id))
            {
                student.id = Guid.NewGuid().ToString();
            }

            try
            {
                // Log the attempt to create student
                Console.WriteLine($"Attempting to create student with ID: {student.id}");
                Console.WriteLine($"Student data: {System.Text.Json.JsonSerializer.Serialize(student)}");
                Console.WriteLine($"Container ID: {_container.Id}");
                Console.WriteLine($"Database ID: {_container.Database.Id}");

                // Validate the student object
                if (string.IsNullOrEmpty(student.FirstName))
                    throw new ArgumentException("FirstName is required");
                if (string.IsNullOrEmpty(student.LastName))
                    throw new ArgumentException("LastName is required");
                if (string.IsNullOrEmpty(student.Email))
                    throw new ArgumentException("Email is required");
                if (string.IsNullOrEmpty(student.MobileNumber))
                    throw new ArgumentException("MobileNumber is required");
                if (string.IsNullOrEmpty(student.EnrolmentStatus))
                    throw new ArgumentException("EnrolmentStatus is required");

                var response = await _container.CreateItemAsync(student, new PartitionKey(student.id));
                Console.WriteLine($"Student created successfully with ID: {student.id}");
                return response.Resource;
            }
            catch (CosmosException ex)
            {
                // Log detailed error information
                Console.WriteLine($"Cosmos DB Error creating student:");
                Console.WriteLine($"Status Code: {ex.StatusCode}");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                Console.WriteLine($"Activity ID: {ex.ActivityId}");
                Console.WriteLine($"Request Charge: {ex.RequestCharge}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                throw new Exception($"Failed to create student in Cosmos DB: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                // Log any other errors
                Console.WriteLine($"General Error creating student:");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                throw new Exception($"Failed to create student: {ex.Message}", ex);
            }
        }

        public async Task<Student> GetStudentByIdAsync(string id)
        {
            try
            {
                Console.WriteLine($"Attempting to get student with ID: {id}");
                Console.WriteLine($"Using container: {_container.Id} in database: {_container.Database.Id}");

                // Create a query to get the student by ID
                var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @id")
                    .WithParameter("@id", id);

                var iterator = _container.GetItemQueryIterator<Student>(query);
                var response = await iterator.ReadNextAsync();

                if (response.Count == 0)
                {
                    Console.WriteLine($"No student found with ID: {id}");
                    return null;
                }

                var student = response.First();
                Console.WriteLine($"Successfully retrieved student: {student.FirstName} {student.LastName} (ID: {student.id})");
                return student;
            }
            catch (CosmosException ex)
            {
                Console.WriteLine($"Cosmos DB Error in GetStudentByIdAsync:");
                Console.WriteLine($"Status Code: {ex.StatusCode}");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine($"Activity ID: {ex.ActivityId}");
                Console.WriteLine($"Request Charge: {ex.RequestCharge}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetStudentByIdAsync: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<IEnumerable<Student>> GetAllStudentsAsync(int pageNumber = 1, int pageSize = 10)
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.IsDeleted = false")
                .WithParameter("@pageSize", pageSize)
                .WithParameter("@offset", (pageNumber - 1) * pageSize);

            var iterator = _container.GetItemQueryIterator<Student>(query);
            var results = new List<Student>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response.ToList());
            }

            return results;
        }

        public async Task<IEnumerable<Student>> SearchStudentsAsync(string searchTerm)
        {
            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.IsDeleted = false AND " +
                "(CONTAINS(c.FirstName, @searchTerm, true) OR " +
                "CONTAINS(c.LastName, @searchTerm, true) OR " +
                "CONTAINS(c.Email, @searchTerm, true))")
                .WithParameter("@searchTerm", searchTerm);

            var iterator = _container.GetItemQueryIterator<Student>(query);
            var results = new List<Student>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response.ToList());
            }

            return results;
        }

        public async Task<Student> UpdateStudentAsync(Student student)
        {
            try
            {
                // If the student has an existing profile image and it's being updated,
                // delete the old image from blob storage
                if (!string.IsNullOrEmpty(student.ProfileImageUrl) && 
                    student.ProfileImageUrl.Contains("blob.core.windows.net"))
                {
                    await _blobStorageService.DeleteFileAsync(student.ProfileImageUrl);
                }

                var response = await _container.UpsertItemAsync(student, new PartitionKey(student.id));
                return response.Resource;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating student: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteStudentAsync(string id, bool permanentDelete = false)
        {
            try
            {
                if (permanentDelete)
                {
                    await _container.DeleteItemAsync<Student>(id, new PartitionKey(id));
                }
                else
                {
                    var student = await GetStudentByIdAsync(id);
                    if (student != null)
                    {
                        student.IsDeleted = true;
                        await _container.UpsertItemAsync(student);
                    }
                }
                return true;
            }
            catch (CosmosException)
            {
                return false;
            }
        }

        public async Task<string> UploadProfileImageAsync(IFormFile file, string studentId)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return string.Empty;
                }

                // Upload the file to Azure Blob Storage
                var fileName = $"{studentId}-{file.FileName}";
                var imageUrl = await _blobStorageService.UploadFileAsync(file, fileName);
                
                Console.WriteLine($"Profile image uploaded successfully: {imageUrl}");
                return imageUrl;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UploadProfileImageAsync: {ex.Message}");
                return string.Empty;
            }
        }
    }
} 