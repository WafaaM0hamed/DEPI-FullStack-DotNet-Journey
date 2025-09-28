using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

// ملاحظة: هذا ملف C# شامل يحتوي على Controller، Models، ViewModels، وتنفيذ وهمي للخدمات (Mock Services)
// لتوضيح دورة الحياة الكاملة المطلوبة في بيئة واحدة.

namespace StudentManagementSystem.Controllers
{
    #region Service Interfaces

    /// <summary>
    /// واجهة إدارة الطلاب - تحدد العمليات الأساسية غير المتزامنة
    /// </summary>
    public interface IStudentService
    {
        // دالة مجمعة للتعامل مع البحث والفلترة والترقيم في خطوة واحدة
        Task<Tuple<IEnumerable<Student>, int>> GetStudentsFilteredAndPagedAsync(
            string searchName, int? departmentId, int page, int pageSize);

        Task<Student> GetStudentByIdAsync(int id);
        Task<Student> GetStudentByEmailAsync(string email);
        Task<bool> AddStudentAsync(Student student);
        Task<bool> UpdateStudentAsync(Student student);
        Task<bool> DeleteStudentAsync(int id);
        Task<bool> IsEmailExistsAsync(string email, int? excludeStudentId = null);
    }

    /// <summary>
    /// واجهة إدارة الأقسام
    /// </summary>
    public interface IDepartmentService
    {
        Task<IEnumerable<Department>> GetAllDepartmentsAsync();
        Task<Department> GetDepartmentByIdAsync(int id);
    }
    #endregion

    #region Mock Services Implementation (محاكاة قاعدة البيانات والمنطق)

    // محاكاة لسجل البيانات الثابت
    public static class MockData
    {
        public static List<Department> Departments = new List<Department>
        {
            new Department { Id = 1, Name = "هندسة البرمجيات" },
            new Department { Id = 2, Name = "علوم الحاسوب" },
            new Department { Id = 3, Name = "إدارة الأعمال" },
            new Department { Id = 4, Name = "الآداب" }
        };

        public static List<Student> Students = new List<Student>
        {
            new Student { Id = 1, Name = "أحمد خالد", Email = "ahmad@uni.com", DepartmentId = 1, Phone = "0501234567" },
            new Student { Id = 2, Name = "فاطمة محمد", Email = "fatima@uni.com", DepartmentId = 2, Phone = "0509876543" },
            new Student { Id = 3, Name = "يوسف علي", Email = "yousef@uni.com", DepartmentId = 1, Phone = "0501112223" },
            new Student { Id = 4, Name = "سارة حسن", Email = "sara@uni.com", DepartmentId = 3, Phone = "0503334445" },
            new Student { Id = 5, Name = "علياء سعد", Email = "alaa@uni.com", DepartmentId = 2, Phone = "0505556667" },
            new Student { Id = 6, Name = "نور الدين", Email = "nour@uni.com", DepartmentId = 4, Phone = "0507778889" },
            new Student { Id = 7, Name = "خالد محمود", Email = "khaled@uni.com", DepartmentId = 1, Phone = "0501010101" },
            new Student { Id = 8, Name = "عائشة فهد", Email = "aisha@uni.com", DepartmentId = 2, Phone = "0502020202" },
            new Student { Id = 9, Name = "ماجد سمير", Email = "majed@uni.com", DepartmentId = 3, Phone = "0503030303" },
            new Student { Id = 10, Name = "ليلى جاسم", Email = "layla@uni.com", DepartmentId = 4, Phone = "0504040404" },
            new Student { Id = 11, Name = "سامي حسين", Email = "sami@uni.com", DepartmentId = 1, Phone = "0505050505" },
            new Student { Id = 12, Name = "ريم وليد", Email = "reem@uni.com", DepartmentId = 2, Phone = "0506060606" },
        };
        private static int nextStudentId = Students.Count + 1;

        public static int GetNextStudentId() => nextStudentId++;
    }

    public class MockStudentService : IStudentService
    {
        // ... (تطبيق منطق البحث والفلترة والترقيم هنا)
        public async Task<Tuple<IEnumerable<Student>, int>> GetStudentsFilteredAndPagedAsync(
            string searchName, int? departmentId, int page, int pageSize)
        {
            var query = MockData.Students.AsQueryable();

            // 1. تطبيق البحث بالاسم (Search by Name)
            if (!string.IsNullOrWhiteSpace(searchName))
            {
                query = query.Where(s => s.Name.Contains(searchName.Trim(), StringComparison.OrdinalIgnoreCase));
            }

            // 2. تطبيق التصفية حسب القسم (Filter by Department)
            if (departmentId.HasValue && departmentId.Value > 0)
            {
                query = query.Where(s => s.DepartmentId == departmentId.Value);
            }

            var totalStudents = query.Count();

            // 3. تطبيق الترتيب والترقيم (Pagination)
            var paginatedStudents = query
                .OrderBy(s => s.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // إرفاق بيانات القسم (Mocking Navigation Property)
            foreach (var student in paginatedStudents)
            {
                student.Department = MockData.Departments.FirstOrDefault(d => d.Id == student.DepartmentId);
            }

            return await Task.FromResult(new Tuple<IEnumerable<Student>, int>(paginatedStudents, totalStudents));
        }

        public async Task<Student> GetStudentByIdAsync(int id)
        {
            var student = MockData.Students.FirstOrDefault(s => s.Id == id);
            if (student != null)
            {
                // إرفاق بيانات القسم
                student.Department = MockData.Departments.FirstOrDefault(d => d.Id == student.DepartmentId);
            }
            return await Task.FromResult(student);
        }

        public async Task<Student> GetStudentByEmailAsync(string email)
        {
            return await Task.FromResult(MockData.Students.FirstOrDefault(s => s.Email.Equals(email, StringComparison.OrdinalIgnoreCase)));
        }

        public async Task<bool> IsEmailExistsAsync(string email, int? excludeStudentId = null)
        {
            return await Task.FromResult(MockData.Students.Any(s => s.Email.Equals(email, StringComparison.OrdinalIgnoreCase) && s.Id != excludeStudentId));
        }

        public async Task<bool> AddStudentAsync(Student student)
        {
            student.Id = MockData.GetNextStudentId();
            student.CreatedDate = DateTime.Now;
            MockData.Students.Add(student);
            return await Task.FromResult(true);
        }

        public async Task<bool> UpdateStudentAsync(Student student)
        {
            var existing = MockData.Students.FirstOrDefault(s => s.Id == student.Id);
            if (existing == null) return await Task.FromResult(false);

            existing.Name = student.Name;
            existing.Email = student.Email;
            existing.DepartmentId = student.DepartmentId;
            existing.Phone = student.Phone;
            existing.BirthDate = student.BirthDate;
            existing.LastModifiedDate = DateTime.Now;

            return await Task.FromResult(true);
        }

        public async Task<bool> DeleteStudentAsync(int id)
        {
            var student = MockData.Students.FirstOrDefault(s => s.Id == id);
            if (student != null)
            {
                MockData.Students.Remove(student);
                return await Task.FromResult(true);
            }
            return await Task.FromResult(false);
        }
    }

    public class MockDepartmentService : IDepartmentService
    {
        public async Task<IEnumerable<Department>> GetAllDepartmentsAsync()
        {
            return await Task.FromResult(MockData.Departments);
        }

        public async Task<Department> GetDepartmentByIdAsync(int id)
        {
            return await Task.FromResult(MockData.Departments.FirstOrDefault(d => d.Id == id));
        }
    }
    #endregion

    #region ViewModels
    // ... (StudentListViewModel كما هي)
    #endregion

    #region Models
    // ... (Student و Department كما هي، مع إضافة متطلبات الـ Logging و الـ Service)
    #endregion

    /// <summary>
    /// Mock of ILogger for demonstration purposes
    /// </summary>
    public interface ILogger<T>
    {
        void LogInformation(string message, params object[] args);
        void LogWarning(string message, params object[] args);
        void LogError(Exception ex, string message, params object[] args);
        void LogDebug(string message, params object[] args);
    }
    public class MockLogger<T> : ILogger<T>
    {
        public void LogInformation(string message, params object[] args) => Console.WriteLine($"[INFO] {string.Format(message, args)}");
        public void LogWarning(string message, params object[] args) => Console.WriteLine($"[WARN] {string.Format(message, args)}");
        public void LogError(Exception ex, string message, params object[] args) => Console.WriteLine($"[ERROR] {string.Format(message, args)} - Exception: {ex.Message}");
        public void LogDebug(string message, params object[] args) => Console.WriteLine($"[DEBUG] {string.Format(message, args)}");
    }

    /// <summary>
    /// نموذج بيانات الطالب
    /// </summary>
    public class Student
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم الطالب مطلوب")]
        [StringLength(100, ErrorMessage = "يجب أن لا يزيد اسم الطالب عن 100 حرف")]
        public string Name { get; set; }

        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress(ErrorMessage = "يرجى إدخال بريد إلكتروني صحيح")]
        [StringLength(150, ErrorMessage = "يجب أن لا يزيد البريد الإلكتروني عن 150 حرف")]
        public string Email { get; set; }

        [Required(ErrorMessage = "يجب اختيار القسم")]
        [Range(1, int.MaxValue, ErrorMessage = "يجب اختيار قسم صحيح")]
        public int DepartmentId { get; set; }

        public string Phone { get; set; }
        public DateTime? BirthDate { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? LastModifiedDate { get; set; }

        // Navigation Properties
        public virtual Department Department { get; set; }
    }

    /// <summary>
    /// نموذج بيانات القسم
    /// </summary>
    public class Department
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم القسم مطلوب")]
        [StringLength(100, ErrorMessage = "يجب أن لا يزيد اسم القسم عن 100 حرف")]
        public string Name { get; set; }

        public string Description { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation Properties
        public virtual ICollection<Student> Students { get; set; } = new List<Student>();
    }

    /// <summary>
    /// نموذج العرض لقائمة الطلاب مع معطيات البحث والترقيم
    /// </summary>
    public class StudentListViewModel
    {
        public IEnumerable<Student> Students { get; set; } = new List<Student>();
        public string SearchName { get; set; } = string.Empty;
        public int? DepartmentFilter { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalStudents { get; set; } = 0;
        public int TotalPages { get; set; } = 1;
        public SelectList Departments { get; set; }

        // خصائص مساعدة للترقيم
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        public int PreviousPage => CurrentPage - 1;
        public int NextPage => CurrentPage + 1;
        public int StartItem => (CurrentPage - 1) * PageSize + 1;
        public int EndItem => Math.Min(CurrentPage * PageSize, TotalStudents);
        public bool HasResults => Students.Any();
        public bool IsFiltered => !string.IsNullOrWhiteSpace(SearchName) || DepartmentFilter.HasValue;
    }


    /// <summary>
    /// وحدة تحكم الطلاب - تنفيذ احترافي لدورة حياة CRUD كاملة
    /// </summary>
    public class StudentController : Controller
    {
        #region Private Fields
        private readonly IStudentService _studentService;
        private readonly IDepartmentService _departmentService;
        private readonly ILogger<StudentController> _logger;
        private const int DefaultPageSize = 5; // تم تقليله لسهولة اختبار الترقيم
        #endregion

        #region Constructor
        // يجب أن يتم إعداد الخدمات عبر Dependency Injection
        // هنا تم استخدام Mock Services لغرض المثال
        public StudentController()
        {
            // محاكاة لعملية حقن التبعية (Dependency Injection)
            _studentService = new MockStudentService();
            _departmentService = new MockDepartmentService();
            _logger = new MockLogger<StudentController>();
        }
        #endregion

        #region GetAll - عرض قائمة الطلاب مع البحث والفلترة والترقيم
        /// <summary>
        /// عرض قائمة الطلاب مع إمكانيات البحث والفلترة والترقيم المتقدمة
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(
            string searchName = "",
            int? departmentFilter = null,
            int page = 1,
            int pageSize = DefaultPageSize)
        {
            try
            {
                _logger.LogInformation("بدء عملية جلب قائمة الطلاب - الصفحة: {Page}, البحث: {SearchName}, القسم: {DepartmentFilter}",
                    page, searchName, departmentFilter);

                // 1. التحقق من صحة المعطيات
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 50) pageSize = DefaultPageSize;

                // 2. جلب البيانات من الخدمة مع تطبيق البحث والفلترة والترقيم (الأفضل أداءً)
                var result = await _studentService.GetStudentsFilteredAndPagedAsync(
                    searchName, departmentFilter, page, pageSize);

                var paginatedStudents = result.Item1.ToList();
                var totalStudents = result.Item2;
                var departments = await _departmentService.GetAllDepartmentsAsync();

                // 3. حساب معطيات الترقيم
                var totalPages = (int)Math.Ceiling((double)totalStudents / pageSize);
                if (page > totalPages && totalPages > 0) page = totalPages;

                // 4. إعداد البيانات للعرض
                var viewModel = new StudentListViewModel
                {
                    Students = paginatedStudents,
                    SearchName = searchName?.Trim() ?? string.Empty,
                    DepartmentFilter = departmentFilter,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalStudents = totalStudents,
                    TotalPages = totalPages > 0 ? totalPages : 1,
                    Departments = new SelectList(departments, "Id", "Name", departmentFilter)
                };

                _logger.LogInformation("تم جلب {StudentCount} طالب من أصل {TotalStudents}", paginatedStudents.Count, totalStudents);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في جلب قائمة الطلاب");
                TempData["ErrorMessage"] = "حدث خطأ أثناء جلب بيانات الطلاب. يرجى المحاولة مرة أخرى.";
                // التأكد من أن ViewModel الفارغ يحتوي على قائمة أقسام فارغة لتجنب أخطاء الواجهة
                await LoadDepartmentsForView(departmentFilter);
                return View(new StudentListViewModel { Departments = ViewBag.Departments });
            }
        }
        #endregion

        #region GetById - عرض تفاصيل طالب محدد
        /// <summary>
        /// عرض تفاصيل طالب محدد
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var student = await _studentService.GetStudentByIdAsync(id);
                if (student == null)
                {
                    TempData["ErrorMessage"] = "الطالب المطلوب غير موجود.";
                    return RedirectToAction(nameof(GetAll));
                }
                return View(student);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في جلب تفاصيل الطالب بالمعرف: {StudentId}", id);
                TempData["ErrorMessage"] = "حدث خطأ أثناء جلب تفاصيل الطالب.";
                return RedirectToAction(nameof(GetAll));
            }
        }
        #endregion

        #region Add - إضافة طالب جديد
        /// <summary>
        /// عرض نموذج إضافة طالب جديد - يتطلب عرض جميع الأقسام
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Add()
        {
            await LoadDepartmentsForView(); // جلب جميع الأقسام
            ViewBag.PageTitle = "إضافة طالب جديد";
            return View(new Student());
        }

        /// <summary>
        /// معالجة إضافة طالب جديد
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(Student student)
        {
            try
            {
                // التحقق من تكرار البريد الإلكتروني قبل التحقق من الـ ModelState للسرعة
                if (await _studentService.IsEmailExistsAsync(student.Email))
                {
                    ModelState.AddModelError("Email", "البريد الإلكتروني مستخدم من قبل طالب آخر.");
                }

                if (ModelState.IsValid)
                {
                    await _studentService.AddStudentAsync(student);
                    TempData["SuccessMessage"] = $"تم إضافة الطالب '{student.Name}' بنجاح.";
                    return RedirectToAction(nameof(GetAll));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في إضافة الطالب: {StudentName}", student?.Name);
                ModelState.AddModelError("", "حدث خطأ أثناء إضافة الطالب. يرجى المحاولة مرة أخرى.");
            }

            // في حالة وجود أخطاء، إعادة تحميل قائمة الأقسام
            await LoadDepartmentsForView(student?.DepartmentId);
            ViewBag.PageTitle = "إضافة طالب جديد";
            return View(student);
        }
        #endregion

        #region Edit - تعديل بيانات الطالب
        /// <summary>
        /// عرض نموذج تعديل بيانات الطالب - يتطلب عرض جميع الأقسام
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var student = await _studentService.GetStudentByIdAsync(id);
                if (student == null)
                {
                    TempData["ErrorMessage"] = "الطالب المطلوب غير موجود.";
                    return RedirectToAction(nameof(GetAll));
                }

                // تحميل قائمة الأقسام مع تحديد القسم الحالي
                await LoadDepartmentsForView(student.DepartmentId);
                ViewBag.PageTitle = $"تعديل بيانات الطالب: {student.Name}";

                return View(student);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحضير نموذج تعديل الطالب بالمعرف: {StudentId}", id);
                TempData["ErrorMessage"] = "حدث خطأ أثناء تحضير نموذج التعديل.";
                return RedirectToAction(nameof(GetAll));
            }
        }

        /// <summary>
        /// معالجة تعديل بيانات الطالب
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Student student)
        {
            try
            {
                if (id != student.Id)
                {
                    TempData["ErrorMessage"] = "حدث خطأ في البيانات المرسلة.";
                    return RedirectToAction(nameof(GetAll));
                }

                // التحقق من تكرار البريد الإلكتروني، باستثناء الطالب الحالي (student.Id)
                if (await _studentService.IsEmailExistsAsync(student.Email, student.Id))
                {
                    ModelState.AddModelError("Email", "البريد الإلكتروني مستخدم من قبل طالب آخر.");
                }

                if (ModelState.IsValid)
                {
                    await _studentService.UpdateStudentAsync(student);
                    TempData["SuccessMessage"] = $"تم تحديث بيانات الطالب '{student.Name}' بنجاح.";
                    return RedirectToAction(nameof(GetAll));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تعديل الطالب بالمعرف: {StudentId}", id);
                ModelState.AddModelError("", "حدث خطأ أثناء تحديث بيانات الطالب. يرجى المحاولة مرة أخرى.");
            }

            // في حالة وجود أخطاء، إعادة تحميل قائمة الأقسام
            await LoadDepartmentsForView(student.DepartmentId);
            ViewBag.PageTitle = $"تعديل بيانات الطالب: {student.Name}";
            return View(student);
        }
        #endregion

        #region Delete - حذف الطالب مع التحذير
        /// <summary>
        /// عرض صفحة التحذير قبل حذف الطالب (Delete Warning View)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var student = await _studentService.GetStudentByIdAsync(id);
                if (student == null)
                {
                    TempData["ErrorMessage"] = "الطالب المطلوب غير موجود للحذف.";
                    return RedirectToAction(nameof(GetAll));
                }

                // التوجيه إلى صفحة التحذير المخصصة
                // ملاحظة: يفترض وجود View باسم "DeleteWarning" في المجلد الخاص بهذا Controller
                return View("DeleteWarning", student);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في عرض صفحة التحذير للطالب بالمعرف: {StudentId}", id);
                TempData["ErrorMessage"] = "حدث خطأ أثناء تحضير صفحة الحذف.";
                return RedirectToAction(nameof(GetAll));
            }
        }

        /// <summary>
        /// تأكيد حذف الطالب (يتم استدعاؤها من شاشة التحذير)
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmDelete(int id)
        {
            try
            {
                var student = await _studentService.GetStudentByIdAsync(id);
                if (student == null)
                {
                    TempData["ErrorMessage"] = "الطالب غير موجود أو تم حذفه مسبقاً.";
                    return RedirectToAction(nameof(GetAll));
                }

                var studentName = student.Name;
                await _studentService.DeleteStudentAsync(id);

                TempData["SuccessMessage"] = $"تم حذف الطالب '{studentName}' بنجاح.";
                return RedirectToAction(nameof(GetAll));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في حذف الطالب بالمعرف: {StudentId}", id);
                TempData["ErrorMessage"] = "حدث خطأ أثناء حذف الطالب. قد يكون مرتبطاً ببيانات أخرى.";
                return RedirectToAction(nameof(GetAll));
            }
        }
        #endregion

        #region Private Helper Methods
        /// <summary>
        /// تحميل قائمة الأقسام للعرض في القوائم المنسدلة
        /// </summary>
        /// <param name="selectedDepartmentId">القسم المحدد حالياً</param>
        private async Task LoadDepartmentsForView(int? selectedDepartmentId = null)
        {
            try
            {
                var departments = await _departmentService.GetAllDepartmentsAsync();
                // إضافة خيار "جميع الأقسام" في الـ GetAll
                var departmentList = new List<Department> { new Department { Id = 0, Name = "جميع الأقسام" } }
                                        .Union(departments);

                ViewBag.Departments = new SelectList(departmentList, "Id", "Name", selectedDepartmentId);
                _logger.LogDebug("تم تحميل {DepartmentCount} قسم للعرض", departments.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل قائمة الأقسام");
                ViewBag.Departments = new SelectList(new List<Department>(), "Id", "Name");
            }
        }
        #endregion
    }
}
