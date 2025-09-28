using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace StudentManagementSystem.Controllers
{
    /// <summary>
    /// وحدة تحكم الأقسام المتقدمة - تنفيذ احترافي لدورة حياة MVC كاملة
    /// مع التركيز على الأمان وأفضل الممارسات
    /// </summary>
    [Authorize] // طبقة أمان أساسية
    public class DepartmentController : Controller
    {
        #region Private Fields & Dependencies
        private readonly IDepartmentService _departmentService;
        private readonly IStudentService _studentService;
        private readonly ILogger<DepartmentController> _logger;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper; // AutoMapper للأمان

        private const string CACHE_KEY_DEPARTMENTS = "departments_list";
        private const int CACHE_EXPIRY_MINUTES = 30;
        private const int MIN_AGE_THRESHOLD = 25;
        private const int MAIN_DEPARTMENT_THRESHOLD = 50;
        #endregion

        #region Constructor with Dependency Injection
        public DepartmentController(
            IDepartmentService departmentService,
            IStudentService studentService,
            ILogger<DepartmentController> logger,
            IMemoryCache cache,
            IConfiguration configuration,
            IMapper mapper)
        {
            _departmentService = departmentService ?? throw new ArgumentNullException(nameof(departmentService));
            _studentService = studentService ?? throw new ArgumentNullException(nameof(studentService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }
        #endregion

        #region ShowAll LifeCycle - عرض جميع الأقسام
        /// <summary>
        /// دورة حياة ShowAll - عرض جميع الأقسام مع إحصائيات متقدمة وأمان محسن
        /// </summary>
        /// <param name="page">رقم الصفحة للترقيم</param>
        /// <param name="sortBy">طريقة الترتيب</param>
        /// <param name="filterActive">فلترة الأقسام النشطة فقط</param>
        /// <returns>صفحة قائمة الأقسام الآمنة</returns>
        [HttpGet]
        [ResponseCache(Duration = 300)] // تحسين الأداء
        public async Task<IActionResult> ShowAll(int page = 1, string sortBy = "name", bool filterActive = true)
        {
            var requestId = Guid.NewGuid().ToString("N")[..8];
            var userId = GetCurrentUserId();

            try
            {
                _logger.LogInformation("[{RequestId}] بدء دورة حياة ShowAll للمستخدم: {UserId} - الصفحة: {Page}",
                    requestId, userId, page);

                // التحقق من صحة المعطيات
                if (page < 1) page = 1;
                var validSortOptions = new[] { "name", "studentsCount", "createdDate", "state" };
                if (!validSortOptions.Contains(sortBy.ToLower()))
                    sortBy = "name";

                // جلب البيانات مع التخزين المؤقت للأمان والأداء
                var departments = await GetDepartmentsWithCaching();

                // تطبيق الفلترة الآمنة
                var filteredDepartments = filterActive
                    ? departments.Where(d => d.IsActive)
                    : departments;

                // إنشاء ViewModels آمنة مع فصل البيانات الحساسة
                var departmentViewModels = new List<SecureDepartmentListItemViewModel>();

                foreach (var department in filteredDepartments)
                {
                    var studentsCount = await _studentService.GetStudentsCountByDepartmentAsync(department.Id);
                    var studentsOver25Count = await GetStudentsOver25CountAsync(department.Id);

                    // استخدام Mapper للأمان ومنع تسرب البيانات
                    var secureViewModel = _mapper.Map<SecureDepartmentListItemViewModel>(department);
                    secureViewModel.StudentsCount = studentsCount;
                    secureViewModel.StudentsOver25Count = studentsOver25Count;
                    secureViewModel.State = studentsCount > MAIN_DEPARTMENT_THRESHOLD
                        ? DepartmentState.Main
                        : DepartmentState.Branch;
                    secureViewModel.CanEdit = await CanUserEditDepartment(userId, department.Id);
                    secureViewModel.CanDelete = await CanUserDeleteDepartment(userId, department.Id);

                    departmentViewModels.Add(secureViewModel);
                }

                // تطبيق الترتيب الآمن
                var sortedDepartments = ApplySorting(departmentViewModels, sortBy);

                // تطبيق الترقيم
                const int pageSize = 10;
                var totalItems = sortedDepartments.Count();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                if (page > totalPages && totalPages > 0) page = totalPages;

                var paginatedDepartments = sortedDepartments
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // إنشاء ViewModel النهائي الآمن
                var viewModel = new SecureDepartmentListViewModel
                {
                    Departments = paginatedDepartments,
                    CurrentPage = page,
                    TotalPages = totalPages,
                    PageSize = pageSize,
                    TotalDepartments = totalItems,
                    SortBy = sortBy,
                    FilterActive = filterActive,
                    MainDepartmentsCount = departmentViewModels.Count(d => d.State == DepartmentState.Main),
                    BranchDepartmentsCount = departmentViewModels.Count(d => d.State == DepartmentState.Branch),
                    UserCanAdd = await CanUserAddDepartment(userId),
                    RequestId = requestId
                };

                _logger.LogInformation("[{RequestId}] تمت دورة حياة ShowAll بنجاح - عرض {Count} قسم من أصل {Total}",
                    requestId, paginatedDepartments.Count, totalItems);

                return View(viewModel);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("[{RequestId}] محاولة وصول غير مصرح بها للمستخدم: {UserId} - {Error}",
                    requestId, userId, ex.Message);
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{RequestId}] خطأ في دورة حياة ShowAll للمستخدم: {UserId}",
                    requestId, userId);
                TempData["ErrorMessage"] = "حدث خطأ أثناء جلب بيانات الأقسام. يرجى المحاولة مرة أخرى.";
                return View(new SecureDepartmentListViewModel());
            }
        }
        #endregion

        #region ShowDetails(int id) LifeCycle - عرض تفاصيل قسم معين
        /// <summary>
        /// دورة حياة ShowDetails - عرض تفاصيل قسم محدد مع الطلاب فوق 25 سنة
        /// مع تطبيق أعلى معايير الأمان
        /// </summary>
        /// <param name="id">معرف القسم - يتم التحقق من صحته وأمانه</param>
        /// <returns>صفحة تفاصيل القسم الآمنة</returns>
        [HttpGet]
        [ValidateAntiForgeryToken] // حماية من CSRF
        public async Task<IActionResult> ShowDetails(int id)
        {
            var requestId = Guid.NewGuid().ToString("N")[..8];
            var userId = GetCurrentUserId();

            try
            {
                _logger.LogInformation("[{RequestId}] بدء دورة حياة ShowDetails للقسم: {DepartmentId} بواسطة المستخدم: {UserId}",
                    requestId, id, userId);

                // التحقق الأمني من صحة المعرف
                if (id <= 0)
                {
                    _logger.LogWarning("[{RequestId}] محاولة وصول بمعرف قسم غير صحيح: {DepartmentId} من المستخدم: {UserId}",
                        requestId, id, userId);
                    TempData["ErrorMessage"] = "معرف القسم غير صحيح.";
                    return RedirectToAction(nameof(ShowAll));
                }

                // التحقق من صلاحيات المستخدم لعرض القسم
                if (!await CanUserViewDepartment(userId, id))
                {
                    _logger.LogWarning("[{RequestId}] محاولة وصول غير مصرح بها للقسم: {DepartmentId} من المستخدم: {UserId}",
                        requestId, id, userId);
                    return Forbid("ليس لديك صلاحية لعرض هذا القسم.");
                }

                // جلب بيانات القسم بشكل آمن
                var department = await _departmentService.GetDepartmentByIdSecureAsync(id);
                if (department == null)
                {
                    _logger.LogWarning("[{RequestId}] لم يتم العثور على القسم: {DepartmentId}", requestId, id);
                    TempData["ErrorMessage"] = "القسم المطلوب غير موجود أو تم حذفه.";
                    return RedirectToAction(nameof(ShowAll));
                }

                // جلب الطلاب بشكل آمن مع فلترة العمر
                var allStudentsInDepartment = await _studentService.GetStudentsByDepartmentSecureAsync(id);

                // تطبيق منطق العمر مع التحقق الآمن
                var studentsOver25 = FilterStudentsOver25Securely(allStudentsInDepartment);

                // حساب الإحصائيات بشكل آمن
                var totalStudentsCount = allStudentsInDepartment.Count();
                var studentsOver25Count = studentsOver25.Count();
                var departmentState = totalStudentsCount > MAIN_DEPARTMENT_THRESHOLD
                    ? DepartmentState.Main
                    : DepartmentState.Branch;

                // إنشاء ViewModel آمن مع فصل البيانات الحساسة
                var viewModel = new SecureDepartmentDetailsViewModel
                {
                    Id = department.Id,
                    Name = SanitizeString(department.Name), // تنظيف البيانات للأمان
                    Description = SanitizeString(department.Description),
                    TotalStudentsCount = totalStudentsCount,
                    StudentsOver25Count = studentsOver25Count,
                    State = departmentState,
                    StateDescription = GetStateDescription(departmentState),
                    StateClass = GetStateClass(departmentState),
                    StateIcon = GetStateIcon(departmentState),

                    // قائمة منسدلة آمنة للطلاب فوق 25 سنة
                    StudentsOver25DropdownList = new SelectList(
                        studentsOver25.Select(s => new {
                            Value = s.Id,
                            Text = SanitizeString(s.Name) // تنظيف الأسماء
                        }),
                        "Value", "Text"
                    ),

                    // تفاصيل محدودة وآمنة للطلاب
                    StudentsOver25Details = studentsOver25.Select(s => new SecureStudentBasicInfoViewModel
                    {
                        Id = s.Id,
                        DisplayName = SanitizeString(s.Name), // بيانات آمنة فقط
                        Age = CalculateAge(s.BirthDate ?? DateTime.MinValue),
                        IsActive = s.IsActive
                        // لا نعرض البريد الإلكتروني أو البيانات الحساسة
                    }).ToList(),

                    // معلومات أمنية إضافية
                    IsActive = department.IsActive,
                    CreatedDate = department.CreatedDate,
                    CanEdit = await CanUserEditDepartment(userId, id),
                    CanDelete = await CanUserDeleteDepartment(userId, id),
                    ViewedBy = GetSafeUserName(userId),
                    ViewedAt = DateTime.UtcNow,
                    RequestId = requestId
                };

                // حساب الإحصائيات المتقدمة
                viewModel.PercentageOver25 = totalStudentsCount > 0
                    ? Math.Round((double)studentsOver25Count / totalStudentsCount * 100, 1)
                    : 0;

                _logger.LogInformation("[{RequestId}] تمت دورة حياة ShowDetails بنجاح للقسم: {DepartmentName} مع {StudentsOver25} طالب فوق 25 سنة",
                    requestId, department.Name, studentsOver25Count);

                return View(viewModel);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("[{RequestId}] وصول غير مصرح به للقسم: {DepartmentId} - {Error}",
                    requestId, id, ex.Message);
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{RequestId}] خطأ في دورة حياة ShowDetails للقسم: {DepartmentId}",
                    requestId, id);
                TempData["ErrorMessage"] = "حدث خطأ أثناء جلب تفاصيل القسم.";
                return RedirectToAction(nameof(ShowAll));
            }
        }
        #endregion

        #region Add() LifeCycle - إضافة قسم جديد
        /// <summary>
        /// دورة حياة Add (GET) - عرض نموذج إضافة قسم جديد مع التحقق الأمني
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,DepartmentManager")] // تحديد الأدوار المسموحة
        public async Task<IActionResult> Add()
        {
            var requestId = Guid.NewGuid().ToString("N")[..8];
            var userId = GetCurrentUserId();

            try
            {
                _logger.LogInformation("[{RequestId}] بدء دورة حياة Add (GET) بواسطة المستخدم: {UserId}",
                    requestId, userId);

                // التحقق من صلاحيات إضافة الأقسام
                if (!await CanUserAddDepartment(userId))
                {
                    _logger.LogWarning("[{RequestId}] محاولة إضافة قسم بدون صلاحية من المستخدم: {UserId}",
                        requestId, userId);
                    return Forbid("ليس لديك صلاحية لإضافة أقسام جديدة.");
                }

                // إنشاء ViewModel آمن للإضافة
                var viewModel = new SecureDepartmentCreateViewModel
                {
                    IsActive = true,
                    CreatedBy = GetSafeUserName(userId),
                    MaxNameLength = GetMaxNameLength(),
                    MaxDescriptionLength = GetMaxDescriptionLength(),
                    RequestId = requestId
                };

                _logger.LogInformation("[{RequestId}] تم تحضير نموذج الإضافة بنجاح", requestId);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{RequestId}] خطأ في دورة حياة Add (GET)", requestId);
                TempData["ErrorMessage"] = "حدث خطأ أثناء تحضير نموذج الإضافة.";
                return RedirectToAction(nameof(ShowAll));
            }
        }

        /// <summary>
        /// دورة حياة Add (POST) - معالجة إضافة قسم جديد مع أعلى معايير الأمان
        /// </summary>
        /// <param name="viewModel">النموذج الآمن للإضافة</param>
        [HttpPost]
        [ValidateAntiForgeryToken] // حماية من CSRF
        [Authorize(Roles = "Admin,DepartmentManager")]
        public async Task<IActionResult> Add(SecureDepartmentCreateViewModel viewModel)
        {
            var requestId = viewModel?.RequestId ?? Guid.NewGuid().ToString("N")[..8];
            var userId = GetCurrentUserId();

            try
            {
                _logger.LogInformation("[{RequestId}] بدء دورة حياة Add (POST) للقسم: {DepartmentName} بواسطة المستخدم: {UserId}",
                    requestId, viewModel?.Name, userId);

                // التحقق الأمني من الصلاحيات مرة أخرى
                if (!await CanUserAddDepartment(userId))
                {
                    _logger.LogWarning("[{RequestId}] محاولة إضافة قسم بدون صلاحية (POST) من المستخدم: {UserId}",
                        requestId, userId);
                    return Forbid();
                }

                // التحقق من صحة النموذج
                if (ModelState.IsValid)
                {
                    // تنظيف وتطهير البيانات المدخلة للأمان
                    var sanitizedName = SanitizeAndValidateInput(viewModel.Name, "اسم القسم");
                    var sanitizedDescription = SanitizeAndValidateInput(viewModel.Description, "وصف القسم");

                    // التحقق من عدم تكرار اسم القسم بشكل آمن
                    if (await _departmentService.IsDepartmentNameExistsAsync(sanitizedName))
                    {
                        ModelState.AddModelError("Name", "اسم القسم موجود بالفعل. يرجى اختيار اسم مختلف.");
                        _logger.LogWarning("[{RequestId}] محاولة إضافة قسم بـاسم مكرر: {DepartmentName} من المستخدم: {UserId}",
                            requestId, sanitizedName, userId);
                        return View(viewModel);
                    }

                    // إنشاء كيان القسم الآمن
                    var department = new Department
                    {
                        Name = sanitizedName,
                        Description = sanitizedDescription,
                        IsActive = viewModel.IsActive,
                        CreatedDate = DateTime.UtcNow,
                        CreatedBy = userId,
                        LastModifiedDate = DateTime.UtcNow,
                        LastModifiedBy = userId
                    };

                    // إضافة القسم مع التحقق من النجاح
                    var departmentId = await _departmentService.AddDepartmentSecureAsync(department, userId);

                    if (departmentId > 0)
                    {
                        _logger.LogInformation("[{RequestId}] تم إضافة القسم بنجاح: {DepartmentName} (ID: {DepartmentId}) بواسطة المستخدم: {UserId}",
                            requestId, department.Name, departmentId, userId);

                        // تسجيل العملية في سجل التدقيق
                        await LogAuditAction("CREATE_DEPARTMENT", departmentId, userId, $"أنشأ القسم: {department.Name}");

                        // إزالة البيانات المخزنة مؤقتاً لضمان التحديث
                        _cache.Remove(CACHE_KEY_DEPARTMENTS);

                        TempData["SuccessMessage"] = $"تم إضافة القسم '{department.Name}' بنجاح.";
                        return RedirectToAction(nameof(ShowAll));
                    }
                    else
                    {
                        ModelState.AddModelError("", "فشل في إضافة القسم. يرجى المحاولة مرة أخرى.");
                        _logger.LogError("[{RequestId}] فشل في إضافة القسم: {DepartmentName}", requestId, sanitizedName);
                    }
                }
                else
                {
                    _logger.LogWarning("[{RequestId}] فشل التحقق من صحة البيانات عند إضافة القسم", requestId);
                    LogModelStateErrors(requestId);
                }

                // في حالة الفشل، إعادة ملء البيانات الآمنة
                viewModel.CreatedBy = GetSafeUserName(userId);
                viewModel.MaxNameLength = GetMaxNameLength();
                viewModel.MaxDescriptionLength = GetMaxDescriptionLength();

                return View(viewModel);
            }
            catch (SecurityException ex)
            {
                _logger.LogError(ex, "[{RequestId}] خطأ أمني في دورة حياة Add (POST)", requestId);
                return Forbid("عملية غير آمنة. تم إيقاف المحاولة.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{RequestId}] خطأ في دورة حياة Add (POST) للقسم: {DepartmentName}",
                    requestId, viewModel?.Name);
                ModelState.AddModelError("", "حدث خطأ غير متوقع. يرجى المحاولة مرة أخرى.");
                return View(viewModel);
            }
        }
        #endregion

        #region Security & Helper Methods
        /// <summary>
        /// الحصول على معرف المستخدم الحالي بشكل آمن
        /// </summary>
        private string GetCurrentUserId()
        {
            return User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Anonymous";
        }

        /// <summary>
        /// الحصول على اسم المستخدم الآمن للعرض
        /// </summary>
        private string GetSafeUserName(string userId)
        {
            var name = User?.FindFirst(ClaimTypes.Name)?.Value ?? "مجهول";
            return SanitizeString(name);
        }

        /// <summary>
        /// تطهير النصوص من المحتوى الضار - Security Case
        /// </summary>
        private static string SanitizeString(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // إزالة HTML Tags والمحتوى الضار
            var sanitized = System.Web.HttpUtility.HtmlEncode(input.Trim());

            // إزالة الأحرف الخطرة
            var dangerousChars = new[] { "<script", "</script", "javascript:", "vbscript:", "onload=", "onerror=" };
            foreach (var dangerousChar in dangerousChars)
            {
                sanitized = sanitized.Replace(dangerousChar, "", StringComparison.OrdinalIgnoreCase);
            }

            return sanitized;
        }

        /// <summary>
        /// تطهير والتحقق من صحة المدخلات - Security Case
        /// </summary>
        private string SanitizeAndValidateInput(string input, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException($"{fieldName} لا يمكن أن يكون فارغاً.");

            var sanitized = SanitizeString(input);

            // التحقق من الطول الآمن
            if (sanitized.Length > 200) // حد أقصى آمن
                throw new ArgumentException($"{fieldName} طويل جداً.");

            return sanitized;
        }

        /// <summary>
        /// فلترة الطلاب فوق 25 سنة بشكل آمن
        /// </summary>
        private List<Student> FilterStudentsOver25Securely(IEnumerable<Student> students)
        {
            var currentDate = DateTime.UtcNow.Date;

            return students
                .Where(s => s != null &&
                           s.BirthDate.HasValue &&
                           CalculateAge(s.BirthDate.Value) > MIN_AGE_THRESHOLD &&
                           s.IsActive) // فقط الطلاب النشطون
                .OrderBy(s => s.Name)
                .ToList();
        }

        /// <summary>
        /// حساب العمر بدقة وأمان
        /// </summary>
        private static int CalculateAge(DateTime birthDate)
        {
            if (birthDate > DateTime.UtcNow.Date)
                return 0; // تاريخ مستقبلي غير صحيح

            var today = DateTime.UtcNow.Date;
            var age = today.Year - birthDate.Year;

            if (birthDate.Date > today.AddYears(-age))
                age--;

            return Math.Max(0, age); // ضمان عدم إرجاع عمر سالب
        }

        /// <summary>
        /// التحقق من صلاحيات المستخدم لعرض القسم - Security Case
        /// </summary>
        private async Task<bool> CanUserViewDepartment(string userId, int departmentId)
        {
            // التحقق من الأدوار والصلاحيات
            if (User.IsInRole("Admin"))
                return true;

            if (User.IsInRole("DepartmentManager"))
            {
                // التحقق من أن المستخدم مدير لهذا القسم
                return await _departmentService.IsUserDepartmentManagerAsync(userId, departmentId);
            }

            if (User.IsInRole("Teacher"))
            {
                // التحقق من أن المدرس يدرّس في هذا القسم
                return await _departmentService.IsTeacherInDepartmentAsync(userId, departmentId);
            }

            return false; // رفض الوصول افتراضياً
        }

        /// <summary>
        /// التحقق من صلاحية إضافة الأقسام - Security Case
        /// </summary>
        private async Task<bool> CanUserAddDepartment(string userId)
        {
            return User.IsInRole("Admin") || User.IsInRole("DepartmentManager");
        }

        /// <summary>
        /// التحقق من صلاحية تعديل القسم - Security Case
        /// </summary>
        private async Task<bool> CanUserEditDepartment(string userId, int departmentId)
        {
            if (User.IsInRole("Admin"))
                return true;

            if (User.IsInRole("DepartmentManager"))
                return await _departmentService.IsUserDepartmentManagerAsync(userId, departmentId);

            return false;
        }

        /// <summary>
        /// التحقق من صلاحية حذف القسم - Security Case
        /// </summary>
        private async Task<bool> CanUserDeleteDepartment(string userId, int departmentId)
        {
            // فقط المدراء يمكنهم الحذف
            return User.IsInRole("Admin");
        }

        /// <summary>
        /// جلب الأقسام مع التخزين المؤقت الآمن
        /// </summary>
        private async Task<IEnumerable<Department>> GetDepartmentsWithCaching()
        {
            return await _cache.GetOrCreateAsync(CACHE_KEY_DEPARTMENTS, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CACHE_EXPIRY_MINUTES);
                return await _departmentService.GetAllDepartmentsAsync();
            });
        }

        /// <summary>
        /// حساب عدد الطلاب فوق 25 سنة بشكل آمن
        /// </summary>
        private async Task<int> GetStudentsOver25CountAsync(int departmentId)
        {
            var students = await _studentService.GetStudentsByDepartmentSecureAsync(departmentId);
            return FilterStudentsOver25Securely(students).Count;
        }

        /// <summary>
        /// تطبيق الترتيب الآمن
        /// </summary>
        private IEnumerable<SecureDepartmentListItemViewModel> ApplySorting(
            IEnumerable<SecureDepartmentListItemViewModel> departments, string sortBy)
        {
            return sortBy.ToLower() switch
            {
            "studentscount" => departments.OrderByDescending(d => d.StudentsCount),
            "createddate" => departments.OrderByDescending(d => d.CreatedDate),
            "state" => departments.OrderBy(d => d.State).ThenBy(d => d.Name),
            _ => departments.OrderBy(d => d.Name) // افتراضي آمن