using System.Data;
using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.FileProviders;
using MySql.Data.MySqlClient;
using StudentInfoSystemAPI;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSingleton<MySqlConnectionFactory>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5161",
                "https://localhost:7086",
                "http://127.0.0.1:5161",
                "https://127.0.0.1:7086")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();
var sharedWwwRoot = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "..", "wwwroot"));

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("frontend");

if (Directory.Exists(sharedWwwRoot))
{
    var sharedFileProvider = new PhysicalFileProvider(sharedWwwRoot);
    app.UseDefaultFiles(new DefaultFilesOptions
    {
        FileProvider = sharedFileProvider
    });
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = sharedFileProvider,
        OnPrepareResponse = context =>
        {
            context.Context.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
            context.Context.Response.Headers.Pragma = "no-cache";
            context.Context.Response.Headers.Expires = "0";
        }
    });
}

app.MapPost("/api/auth/login", async Task<Results<Ok<AuthResponse>, UnauthorizedHttpResult, ProblemHttpResult>> (LoginRequest request, MySqlConnectionFactory factory) =>
{
    try
    {
        await using var conn = factory.CreateConnection();
        await conn.OpenAsync();

        const string sql = """
            SELECT account_id, username, email, password_hash, role, status
            FROM accounts
            WHERE username = @usernameOrEmail OR email = @usernameOrEmail
            LIMIT 1
            """;

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@usernameOrEmail", request.Username.Trim());
        await using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return TypedResults.Unauthorized();
        }

        var passwordHash = reader.GetString("password_hash");
        var status = reader.GetString("status");
        var incomingHash = ComputeSha256(request.Password);

        if (!string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase))
        {
            return TypedResults.Unauthorized();
        }

        if (!string.Equals(passwordHash, incomingHash, StringComparison.OrdinalIgnoreCase))
        {
            return TypedResults.Unauthorized();
        }

        return TypedResults.Ok(new AuthResponse(
            reader.GetInt32("account_id"),
            reader.GetString("username"),
            reader.GetString("email"),
            reader.GetString("role"),
            status,
            "demo-admin-token"));
    }
    catch (Exception ex)
    {
        return DatabaseProblem(ex);
    }
});

app.MapPost("/api/auth/recover", async Task<Results<Ok<string>, BadRequest<string>, ProblemHttpResult>> (PasswordRecoveryRequest request, MySqlConnectionFactory factory) =>
{
    if (string.IsNullOrWhiteSpace(request.UsernameOrEmail) ||
        string.IsNullOrWhiteSpace(request.Email) ||
        string.IsNullOrWhiteSpace(request.NewPassword))
    {
        return TypedResults.BadRequest("Username/email, email, and new password are required.");
    }

    if (request.NewPassword.Length < 6)
    {
        return TypedResults.BadRequest("New password must be at least 6 characters.");
    }

    try
    {
        await using var conn = factory.CreateConnection();
        await conn.OpenAsync();

        const string sql = """
            UPDATE accounts
            SET password_hash = @passwordHash
            WHERE (username = @usernameOrEmail OR email = @usernameOrEmail)
              AND email = @email
            """;

        var affected = await ExecuteNonQueryAsync(
            conn,
            sql,
            new MySqlParameter("@passwordHash", ComputeSha256(request.NewPassword)),
            new MySqlParameter("@usernameOrEmail", request.UsernameOrEmail.Trim()),
            new MySqlParameter("@email", request.Email.Trim()));

        if (affected == 0)
        {
            return TypedResults.BadRequest("No matching account was found for recovery.");
        }

        return TypedResults.Ok("Password updated successfully.");
    }
    catch (Exception ex)
    {
        return DatabaseProblem(ex);
    }
});

app.MapGet("/api/accounts", async Task<Results<Ok<List<AccountDto>>, ProblemHttpResult>> (string? search, MySqlConnectionFactory factory) =>
{
    try
    {
        await using var conn = factory.CreateConnection();
        await conn.OpenAsync();

        const string sql = """
            SELECT account_id, username, email, role, status, student_id, instructor_id, created_at, updated_at
            FROM accounts
            WHERE @search IS NULL
               OR @search = ''
               OR username LIKE CONCAT('%', @search, '%')
               OR email LIKE CONCAT('%', @search, '%')
               OR role LIKE CONCAT('%', @search, '%')
               OR status LIKE CONCAT('%', @search, '%')
            ORDER BY username
            """;

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@search", search);

        var accounts = await QueryAsync(
            cmd,
            reader => new AccountDto(
                reader.GetInt32("account_id"),
                reader.GetString("username"),
                reader.GetString("email"),
                reader.GetString("role"),
                reader.GetString("status"),
                reader.IsDBNull("student_id") ? null : reader.GetInt32("student_id"),
                reader.IsDBNull("instructor_id") ? null : reader.GetInt32("instructor_id"),
                reader.GetDateTime("created_at"),
                reader.GetDateTime("updated_at")));

        return TypedResults.Ok(accounts);
    }
    catch (Exception ex)
    {
        return DatabaseProblem(ex);
    }
});

app.MapPost("/api/accounts", async Task<Results<Ok, BadRequest<string>, ProblemHttpResult>> (AccountCreateRequest request, MySqlConnectionFactory factory) =>
{
    var validationError = ValidateAccountCreate(request);
    if (validationError is not null)
    {
        return TypedResults.BadRequest(validationError);
    }

    try
    {
        await using var conn = factory.CreateConnection();
        await conn.OpenAsync();

        const string sql = """
            INSERT INTO accounts (username, email, password_hash, role, status, student_id, instructor_id)
            VALUES (@username, @email, @passwordHash, @role, @status, @studentId, @instructorId)
            """;

        await ExecuteNonQueryAsync(
            conn,
            sql,
            new MySqlParameter("@username", request.Username.Trim()),
            new MySqlParameter("@email", request.Email.Trim()),
            new MySqlParameter("@passwordHash", ComputeSha256(request.Password)),
            new MySqlParameter("@role", request.Role),
            new MySqlParameter("@status", request.Status),
            new MySqlParameter("@studentId", request.StudentId ?? (object)DBNull.Value),
            new MySqlParameter("@instructorId", request.InstructorId ?? (object)DBNull.Value));

        return TypedResults.Ok();
    }
    catch (MySqlException ex) when (ex.Number == 1062)
    {
        return TypedResults.BadRequest("Username or email already exists.");
    }
    catch (Exception ex)
    {
        return DatabaseProblem(ex);
    }
});

app.MapPut("/api/accounts/{id:int}", async Task<Results<Ok, BadRequest<string>, ProblemHttpResult>> (int id, AccountUpdateRequest request, MySqlConnectionFactory factory) =>
{
    var validationError = ValidateAccountUpdate(request);
    if (validationError is not null)
    {
        return TypedResults.BadRequest(validationError);
    }

    try
    {
        await using var conn = factory.CreateConnection();
        await conn.OpenAsync();

        const string sql = """
            UPDATE accounts
            SET username = @username,
                email = @email,
                role = @role,
                status = @status,
                student_id = @studentId,
                instructor_id = @instructorId,
                password_hash = COALESCE(@passwordHash, password_hash)
            WHERE account_id = @id
            """;

        await ExecuteNonQueryAsync(
            conn,
            sql,
            new MySqlParameter("@id", id),
            new MySqlParameter("@username", request.Username.Trim()),
            new MySqlParameter("@email", request.Email.Trim()),
            new MySqlParameter("@role", request.Role),
            new MySqlParameter("@status", request.Status),
            new MySqlParameter("@studentId", request.StudentId ?? (object)DBNull.Value),
            new MySqlParameter("@instructorId", request.InstructorId ?? (object)DBNull.Value),
            new MySqlParameter("@passwordHash", string.IsNullOrWhiteSpace(request.Password) ? DBNull.Value : ComputeSha256(request.Password)));

        return TypedResults.Ok();
    }
    catch (MySqlException ex) when (ex.Number == 1062)
    {
        return TypedResults.BadRequest("Username or email already exists.");
    }
    catch (Exception ex)
    {
        return DatabaseProblem(ex);
    }
});

app.MapPut("/api/accounts/{id:int}/status", async Task<Results<Ok, BadRequest<string>, ProblemHttpResult>> (int id, AccountStatusUpdateRequest request, MySqlConnectionFactory factory) =>
{
    if (!IsAllowedStatus(request.Status))
    {
        return TypedResults.BadRequest("Status must be Active, Inactive, or Suspended.");
    }

    try
    {
        await using var conn = factory.CreateConnection();
        await conn.OpenAsync();

        const string sql = "UPDATE accounts SET status = @status WHERE account_id = @id";
        await ExecuteNonQueryAsync(
            conn,
            sql,
            new MySqlParameter("@id", id),
            new MySqlParameter("@status", request.Status));

        return TypedResults.Ok();
    }
    catch (Exception ex)
    {
        return DatabaseProblem(ex);
    }
});

app.MapGet("/api/dashboard", async Task<Results<Ok<DashboardDto>, ProblemHttpResult>> (MySqlConnectionFactory factory) =>
{
    try
    {
        await using var conn = factory.CreateConnection();
        await conn.OpenAsync();

        var totalStudents = await ExecuteIntAsync(conn, "SELECT COUNT(*) FROM students");
        var totalCourses = await ExecuteIntAsync(conn, "SELECT COUNT(*) FROM courses");
        var averageGpa = await ExecuteNullableDecimalAsync(conn, "SELECT AVG(grade) FROM enrollments WHERE grade IS NOT NULL");

        const string recentQuery = """
            SELECT
                e.enrollment_id,
                CONCAT(s.first_name, ' ', s.last_name) AS student_name,
                c.course_name,
                e.semester,
                e.year,
                e.grade
            FROM enrollments e
            JOIN students s ON e.student_id = s.student_id
            JOIN courses c ON e.course_id = c.course_id
            ORDER BY e.enrollment_id DESC
            LIMIT 10
            """;

        await using var cmd = new MySqlCommand(recentQuery, conn);
        var recentEnrollments = await QueryAsync(
            cmd,
            reader => new EnrollmentViewDto(
                reader.GetInt32("enrollment_id"),
                reader.GetString("student_name"),
                reader.GetString("course_name"),
                reader.GetString("semester"),
                reader.GetInt32("year"),
                reader.IsDBNull("grade") ? null : reader.GetDecimal("grade")));

        return TypedResults.Ok(new DashboardDto(totalStudents, totalCourses, averageGpa, recentEnrollments));
    }
    catch (Exception ex)
    {
        return DatabaseProblem(ex);
    }
});

app.MapGet("/api/students", async Task<Results<Ok<List<StudentDto>>, ProblemHttpResult>> (MySqlConnectionFactory factory) =>
{
    try
    {
        await using var conn = factory.CreateConnection();
        await conn.OpenAsync();

        const string sql = """
            SELECT student_id, first_name, last_name, gender, birth_date, email
            FROM students
            ORDER BY last_name, first_name
            """;

        await using var cmd = new MySqlCommand(sql, conn);
        var students = await QueryAsync(
            cmd,
            reader => new StudentDto(
                reader.GetInt32("student_id"),
                reader.GetString("first_name"),
                reader.GetString("last_name"),
                reader.GetString("gender"),
                reader.IsDBNull("birth_date") ? null : reader.GetDateTime("birth_date").ToString("yyyy-MM-dd"),
                reader.IsDBNull("email") ? "" : reader.GetString("email")));

        return TypedResults.Ok(students);
    }
    catch (Exception ex)
    {
        return DatabaseProblem(ex);
    }
});

app.MapPost("/api/students", async Task<Results<Ok, BadRequest<string>, ProblemHttpResult>> (StudentUpsertRequest request, MySqlConnectionFactory factory) =>
{
    if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
    {
        return TypedResults.BadRequest("First name and last name are required.");
    }

    try
    {
        await using var conn = factory.CreateConnection();
        await conn.OpenAsync();

        const string sql = """
            INSERT INTO students (first_name, last_name, gender, birth_date, email)
            VALUES (@firstName, @lastName, @gender, @birthDate, @email)
            """;

        await ExecuteNonQueryAsync(
            conn,
            sql,
            new MySqlParameter("@firstName", request.FirstName),
            new MySqlParameter("@lastName", request.LastName),
            new MySqlParameter("@gender", request.Gender ?? "Male"),
            new MySqlParameter("@birthDate", string.IsNullOrWhiteSpace(request.BirthDate) ? DBNull.Value : DateTime.Parse(request.BirthDate)),
            new MySqlParameter("@email", request.Email ?? ""));

        return TypedResults.Ok();
    }
    catch (Exception ex)
    {
        return DatabaseProblem(ex);
    }
});

app.MapPut("/api/students/{id:int}", async Task<Results<Ok, BadRequest<string>, ProblemHttpResult>> (int id, StudentUpsertRequest request, MySqlConnectionFactory factory) =>
{
    if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
    {
        return TypedResults.BadRequest("First name and last name are required.");
    }

    try
    {
        await using var conn = factory.CreateConnection();
        await conn.OpenAsync();

        const string sql = """
            UPDATE students
            SET first_name = @firstName,
                last_name = @lastName,
                gender = @gender,
                birth_date = @birthDate,
                email = @email
            WHERE student_id = @id
            """;

        await ExecuteNonQueryAsync(
            conn,
            sql,
            new MySqlParameter("@id", id),
            new MySqlParameter("@firstName", request.FirstName),
            new MySqlParameter("@lastName", request.LastName),
            new MySqlParameter("@gender", request.Gender ?? "Male"),
            new MySqlParameter("@birthDate", string.IsNullOrWhiteSpace(request.BirthDate) ? DBNull.Value : DateTime.Parse(request.BirthDate)),
            new MySqlParameter("@email", request.Email ?? ""));

        return TypedResults.Ok();
    }
    catch (Exception ex)
    {
        return DatabaseProblem(ex);
    }
});

app.MapGet("/api/courses", async Task<Results<Ok<List<CourseDto>>, ProblemHttpResult>> (MySqlConnectionFactory factory) =>
{
    try
    {
        await using var conn = factory.CreateConnection();
        await conn.OpenAsync();

        const string sql = """
            SELECT course_id, course_name, course_code, units
            FROM courses
            ORDER BY course_name
            """;

        await using var cmd = new MySqlCommand(sql, conn);
        var courses = await QueryAsync(
            cmd,
            reader => new CourseDto(
                reader.GetInt32("course_id"),
                reader.GetString("course_name"),
                reader.GetString("course_code"),
                reader.GetInt32("units")));

        return TypedResults.Ok(courses);
    }
    catch (Exception ex)
    {
        return DatabaseProblem(ex);
    }
});

app.MapPost("/api/courses", async Task<Results<Ok, BadRequest<string>, ProblemHttpResult>> (CourseUpsertRequest request, MySqlConnectionFactory factory) =>
{
    if (string.IsNullOrWhiteSpace(request.CourseName) || string.IsNullOrWhiteSpace(request.CourseCode))
    {
        return TypedResults.BadRequest("Course name and code are required.");
    }

    try
    {
        await using var conn = factory.CreateConnection();
        await conn.OpenAsync();

        const string sql = """
            INSERT INTO courses (course_name, course_code, units)
            VALUES (@courseName, @courseCode, @units)
            """;

        await ExecuteNonQueryAsync(
            conn,
            sql,
            new MySqlParameter("@courseName", request.CourseName),
            new MySqlParameter("@courseCode", request.CourseCode),
            new MySqlParameter("@units", request.Units));

        return TypedResults.Ok();
    }
    catch (Exception ex)
    {
        return DatabaseProblem(ex);
    }
});

app.MapPut("/api/courses/{id:int}", async Task<Results<Ok, BadRequest<string>, ProblemHttpResult>> (int id, CourseUpsertRequest request, MySqlConnectionFactory factory) =>
{
    if (string.IsNullOrWhiteSpace(request.CourseName) || string.IsNullOrWhiteSpace(request.CourseCode))
    {
        return TypedResults.BadRequest("Course name and code are required.");
    }

    try
    {
        await using var conn = factory.CreateConnection();
        await conn.OpenAsync();

        const string sql = """
            UPDATE courses
            SET course_name = @courseName,
                course_code = @courseCode,
                units = @units
            WHERE course_id = @id
            """;

        await ExecuteNonQueryAsync(
            conn,
            sql,
            new MySqlParameter("@id", id),
            new MySqlParameter("@courseName", request.CourseName),
            new MySqlParameter("@courseCode", request.CourseCode),
            new MySqlParameter("@units", request.Units));

        return TypedResults.Ok();
    }
    catch (Exception ex)
    {
        return DatabaseProblem(ex);
    }
});

app.MapGet("/api/enrollments", async Task<Results<Ok<List<EnrollmentDto>>, ProblemHttpResult>> (MySqlConnectionFactory factory) =>
{
    try
    {
        await using var conn = factory.CreateConnection();
        await conn.OpenAsync();

        const string sql = """
            SELECT
                e.enrollment_id,
                e.student_id,
                e.course_id,
                CONCAT(s.first_name, ' ', s.last_name) AS student_name,
                c.course_name,
                e.semester,
                e.year,
                e.grade
            FROM enrollments e
            JOIN students s ON e.student_id = s.student_id
            JOIN courses c ON e.course_id = c.course_id
            ORDER BY e.enrollment_id DESC
            """;

        await using var cmd = new MySqlCommand(sql, conn);
        var enrollments = await QueryAsync(
            cmd,
            reader => new EnrollmentDto(
                reader.GetInt32("enrollment_id"),
                reader.GetInt32("student_id"),
                reader.GetInt32("course_id"),
                reader.GetString("student_name"),
                reader.GetString("course_name"),
                reader.GetString("semester"),
                reader.GetInt32("year"),
                reader.IsDBNull("grade") ? null : reader.GetDecimal("grade")));

        return TypedResults.Ok(enrollments);
    }
    catch (Exception ex)
    {
        return DatabaseProblem(ex);
    }
});

app.MapPost("/api/enrollments", async Task<Results<Ok, BadRequest<string>, ProblemHttpResult>> (EnrollmentUpsertRequest request, MySqlConnectionFactory factory) =>
{
    if (request.StudentId <= 0 || request.CourseId <= 0)
    {
        return TypedResults.BadRequest("Student and course are required.");
    }

    try
    {
        await using var conn = factory.CreateConnection();
        await conn.OpenAsync();

        const string sql = """
            INSERT INTO enrollments (student_id, course_id, semester, year, grade)
            VALUES (@studentId, @courseId, @semester, @year, @grade)
            """;

        await ExecuteNonQueryAsync(
            conn,
            sql,
            new MySqlParameter("@studentId", request.StudentId),
            new MySqlParameter("@courseId", request.CourseId),
            new MySqlParameter("@semester", request.Semester ?? "1st"),
            new MySqlParameter("@year", request.Year),
            new MySqlParameter("@grade", request.Grade ?? (object)DBNull.Value));

        return TypedResults.Ok();
    }
    catch (Exception ex)
    {
        return DatabaseProblem(ex);
    }
});

app.MapPut("/api/enrollments/{id:int}", async Task<Results<Ok, BadRequest<string>, ProblemHttpResult>> (int id, EnrollmentUpsertRequest request, MySqlConnectionFactory factory) =>
{
    if (request.StudentId <= 0 || request.CourseId <= 0)
    {
        return TypedResults.BadRequest("Student and course are required.");
    }

    try
    {
        await using var conn = factory.CreateConnection();
        await conn.OpenAsync();

        const string sql = """
            UPDATE enrollments
            SET student_id = @studentId,
                course_id = @courseId,
                semester = @semester,
                year = @year,
                grade = @grade
            WHERE enrollment_id = @id
            """;

        await ExecuteNonQueryAsync(
            conn,
            sql,
            new MySqlParameter("@id", id),
            new MySqlParameter("@studentId", request.StudentId),
            new MySqlParameter("@courseId", request.CourseId),
            new MySqlParameter("@semester", request.Semester ?? "1st"),
            new MySqlParameter("@year", request.Year),
            new MySqlParameter("@grade", request.Grade ?? (object)DBNull.Value));

        return TypedResults.Ok();
    }
    catch (Exception ex)
    {
        return DatabaseProblem(ex);
    }
});

app.MapGet("/api/reports", async Task<Results<Ok<ReportResponse>, BadRequest<string>, ProblemHttpResult>> (string type, string? semester, int? year, MySqlConnectionFactory factory) =>
{
    try
    {
        await using var conn = factory.CreateConnection();
        await conn.OpenAsync();

        var reportType = type?.Trim().ToLowerInvariant();
        var sql = GetReportSql(reportType);

        if (sql is null)
        {
            return TypedResults.BadRequest("Unsupported report type.");
        }

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@semester", semester);
        cmd.Parameters.AddWithValue("@year", year);

        await using var reader = await cmd.ExecuteReaderAsync();
        var table = new DataTable();
        table.Load(reader);

        var columns = table.Columns.Cast<DataColumn>().Select(column => column.ColumnName).ToArray();
        var rows = table.Rows.Cast<DataRow>()
            .Select(row => columns.Select(column => row[column] == DBNull.Value ? null : row[column]).ToArray())
            .ToArray();

        return TypedResults.Ok(new ReportResponse(columns, rows));
    }
    catch (Exception ex)
    {
        return DatabaseProblem(ex);
    }
});

app.MapPost("/api/reports/export", async Task<IResult> (ReportExportRequest request, MySqlConnectionFactory factory) =>
{
    try
    {
        await using var conn = factory.CreateConnection();
        await conn.OpenAsync();

        var reportType = request.Type?.Trim().ToLowerInvariant();
        var sql = GetReportSql(reportType);

        if (sql is null)
        {
            return TypedResults.BadRequest("Unsupported report type.");
        }

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@semester", request.Semester);
        cmd.Parameters.AddWithValue("@year", request.Year);

        await using var reader = await cmd.ExecuteReaderAsync();
        var table = new DataTable();
        table.Load(reader);

        var generator = new ExcelReportGenerator();
        var title = GetReportTitle(reportType);
        var excelData = generator.GenerateDataTableReport(table, title, request.SignerName ?? "Authorized Signatory");
        var safeTitle = title.Replace(" ", "");

        return Results.File(
            excelData,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"{safeTitle}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
    }
    catch (Exception ex)
    {
        return TypedResults.Problem(title: "Error generating Excel report", detail: ex.Message);
    }
});

// ============ TRANSACTION ENDPOINTS ============

// Create Enrollment Transaction
app.MapPost("/api/transactions/enrollment", async Task<Results<Ok<EnrollmentTransaction>, BadRequest<string>, ProblemHttpResult>> (CreateEnrollmentTransactionRequest request, MySqlConnectionFactory factory) =>
{
    if (request.StudentId <= 0 || request.CourseId <= 0)
    {
        return TypedResults.BadRequest("Invalid student or course ID.");
    }

    try
    {
        await using var conn = factory.CreateConnection();
        await conn.OpenAsync();
        await EnsureTransactionTablesAsync(conn);

        const string sql = """
            INSERT INTO enrollment_transactions (student_id, course_id, transaction_type, transaction_date, status, notes, created_by_user_id)
            VALUES (@student_id, @course_id, 'Enrollment', NOW(), 'Completed', @notes, 1)
            """;

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@student_id", request.StudentId);
        cmd.Parameters.AddWithValue("@course_id", request.CourseId);
        cmd.Parameters.AddWithValue("@notes", request.Notes ?? "");

        await cmd.ExecuteNonQueryAsync();

        return TypedResults.Ok(new EnrollmentTransaction
        {
            StudentId = request.StudentId,
            CourseId = request.CourseId,
            TransactionType = "Enrollment",
            TransactionDate = DateTime.Now,
            Status = "Completed",
            Notes = request.Notes ?? ""
        });
    }
    catch (Exception ex)
    {
        return DatabaseProblem(ex);
    }
});

// Create Course Registration Transaction
app.MapPost("/api/transactions/course-registration", async Task<Results<Ok<CourseRegistrationTransaction>, BadRequest<string>, ProblemHttpResult>> (CreateCourseRegistrationRequest request, MySqlConnectionFactory factory) =>
{
    if (request.StudentId <= 0 || request.CourseId <= 0 || request.RegistrationFee <= 0)
    {
        return TypedResults.BadRequest("Invalid request parameters.");
    }

    try
    {
        await using var conn = factory.CreateConnection();
        await conn.OpenAsync();
        await EnsureTransactionTablesAsync(conn);

        const string sql = """
            INSERT INTO course_registration_transactions (student_id, course_id, transaction_type, semester, year, registration_date, status, registration_fee, created_by_user_id)
            VALUES (@student_id, @course_id, 'Course Registration', @semester, @year, NOW(), 'Active', @fee, 1)
            """;

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@student_id", request.StudentId);
        cmd.Parameters.AddWithValue("@course_id", request.CourseId);
        cmd.Parameters.AddWithValue("@semester", request.Semester);
        cmd.Parameters.AddWithValue("@year", request.Year);
        cmd.Parameters.AddWithValue("@fee", request.RegistrationFee);

        await cmd.ExecuteNonQueryAsync();

        return TypedResults.Ok(new CourseRegistrationTransaction
        {
            StudentId = request.StudentId,
            CourseId = request.CourseId,
            TransactionType = "Course Registration",
            Semester = request.Semester,
            Year = request.Year,
            RegistrationDate = DateTime.Now,
            Status = "Active",
            RegistrationFee = request.RegistrationFee
        });
    }
    catch (Exception ex)
    {
        return DatabaseProblem(ex);
    }
});

// Create Payment Transaction
app.MapPost("/api/transactions/payment", async Task<Results<Ok<PaymentTransaction>, BadRequest<string>, ProblemHttpResult>> (CreatePaymentTransactionRequest request, MySqlConnectionFactory factory) =>
{
    if (request.StudentId <= 0 || request.Amount <= 0)
    {
        return TypedResults.BadRequest("Invalid request parameters.");
    }

    try
    {
        await using var conn = factory.CreateConnection();
        await conn.OpenAsync();
        await EnsureTransactionTablesAsync(conn);

        string receiptNumber = $"REC-{DateTime.Now:yyyyMMddHHmmss}";
        
        const string sql = """
            INSERT INTO payment_transactions (student_id, amount, payment_method, payment_date, transaction_type, payment_description, status, receipt_number, created_by_user_id)
            VALUES (@student_id, @amount, @method, NOW(), 'Payment', @description, 'Completed', @receipt, 1)
            """;

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@student_id", request.StudentId);
        cmd.Parameters.AddWithValue("@amount", request.Amount);
        cmd.Parameters.AddWithValue("@method", request.PaymentMethod ?? "Credit Card");
        cmd.Parameters.AddWithValue("@description", request.PaymentDescription ?? "");
        cmd.Parameters.AddWithValue("@receipt", receiptNumber);

        await cmd.ExecuteNonQueryAsync();

        return TypedResults.Ok(new PaymentTransaction
        {
            StudentId = request.StudentId,
            Amount = request.Amount,
            PaymentMethod = request.PaymentMethod ?? "Credit Card",
            PaymentDate = DateTime.Now,
            TransactionType = "Payment",
            PaymentDescription = request.PaymentDescription ?? "",
            Status = "Completed",
            ReceiptNumber = receiptNumber
        });
    }
    catch (Exception ex)
    {
        return DatabaseProblem(ex);
    }
});

// Get All Transactions
app.MapGet("/api/transactions", async Task<Results<Ok<List<TransactionReportData>>, ProblemHttpResult>> (MySqlConnectionFactory factory) =>
{
    try
    {
        await using var conn = factory.CreateConnection();
        await conn.OpenAsync();
        await EnsureTransactionTablesAsync(conn);
        var transactions = await QueryTransactionsAsync(conn, new TransactionReportRequest());

        return TypedResults.Ok(transactions);
    }
    catch (Exception ex)
    {
        return DatabaseProblem(ex);
    }
});

// Generate Excel Report
app.MapPost("/api/transactions/report/export", async Task<IResult> (TransactionReportRequest request, MySqlConnectionFactory factory) =>
{
    try
    {
        await using var conn = factory.CreateConnection();
        await conn.OpenAsync();
        await EnsureTransactionTablesAsync(conn);
        var transactions = await QueryTransactionsAsync(conn, request);

        var generator = new ExcelReportGenerator();
        byte[] excelData = generator.GenerateTransactionReport(transactions, "Transaction Report", request.SignerName ?? "Authorized Signatory");

        return Results.File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"TransactionReport_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
    }
    catch (Exception ex)
    {
        return TypedResults.Problem(title: "Error generating report", detail: ex.Message);
    }
});

app.Run();

static string? GetReportSql(string? reportType) =>
    reportType switch
    {
        "student-academic-performance" => """
            SELECT
                s.student_id,
                CONCAT(s.first_name, ' ', s.last_name) AS student_name,
                COUNT(e.course_id) AS courses_taken,
                AVG(e.grade) AS average_gpa,
                MIN(e.grade) AS best_grade,
                MAX(CASE WHEN e.year IS NOT NULL THEN e.year END) AS latest_year,
                SUBSTRING_INDEX(
                    GROUP_CONCAT(e.semester ORDER BY e.year DESC,
                        CASE e.semester
                            WHEN '1st' THEN 1
                            WHEN '2nd' THEN 2
                            WHEN 'Summer' THEN 3
                            ELSE 99
                        END DESC SEPARATOR ','
                    ), ',', 1
                ) AS latest_semester
            FROM students s
            LEFT JOIN enrollments e
                ON s.student_id = e.student_id
               AND (@semester IS NULL OR @semester = '' OR e.semester = @semester)
               AND (@year IS NULL OR e.year = @year)
            GROUP BY s.student_id, s.first_name, s.last_name
            ORDER BY average_gpa ASC, student_name
            """,
        "course-popularity-performance" => """
            SELECT
                c.course_id,
                c.course_name,
                c.course_code,
                c.units,
                COUNT(e.student_id) AS enrolled_students,
                AVG(e.grade) AS average_grade,
                CONCAT(
                    ROUND(
                        IFNULL(
                            SUM(CASE WHEN e.grade IS NOT NULL AND e.grade <= 3 THEN 1 ELSE 0 END) * 100.0 /
                            NULLIF(SUM(CASE WHEN e.grade IS NOT NULL THEN 1 ELSE 0 END), 0),
                            0
                        ), 0
                    ),
                    '%'
                ) AS pass_rate
            FROM courses c
            LEFT JOIN enrollments e
                ON c.course_id = e.course_id
               AND (@semester IS NULL OR @semester = '' OR e.semester = @semester)
               AND (@year IS NULL OR e.year = @year)
            GROUP BY c.course_id, c.course_name, c.course_code, c.units
            ORDER BY enrolled_students DESC, average_grade ASC, c.course_name
            """,
        "semester-summary-trends" => """
            SELECT
                e.semester,
                e.year,
                COUNT(*) AS total_enrollments,
                COUNT(DISTINCT e.student_id) AS unique_students,
                AVG(e.grade) AS average_grade,
                SUM(CASE WHEN e.grade IS NOT NULL AND e.grade <= 3 THEN 1 ELSE 0 END) AS passing_students
            FROM enrollments e
            WHERE (@semester IS NULL OR @semester = '' OR e.semester = @semester)
              AND (@year IS NULL OR e.year = @year)
            GROUP BY e.year, e.semester
            ORDER BY e.year, CASE e.semester
                WHEN '1st' THEN 1
                WHEN '2nd' THEN 2
                WHEN 'Summer' THEN 3
                ELSE 99
            END
            """,
        _ => null
    };

static string GetReportTitle(string? reportType) =>
    reportType switch
    {
        "student-academic-performance" => "Student Academic Performance Report",
        "course-popularity-performance" => "Course Popularity Performance Report",
        "semester-summary-trends" => "Semester Summary Trends Report",
        _ => "Student Information System Report"
    };

static async Task EnsureTransactionTablesAsync(MySqlConnection conn)
{
    string[] statements =
    [
        """
        CREATE TABLE IF NOT EXISTS enrollment_transactions (
            transaction_id INT AUTO_INCREMENT PRIMARY KEY,
            student_id INT NOT NULL,
            course_id INT NOT NULL,
            transaction_type VARCHAR(50) NOT NULL DEFAULT 'Enrollment',
            transaction_date DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
            status VARCHAR(30) NOT NULL DEFAULT 'Completed',
            notes VARCHAR(255) NULL,
            created_by_user_id INT NULL
        )
        """,
        """
        CREATE TABLE IF NOT EXISTS course_registration_transactions (
            transaction_id INT AUTO_INCREMENT PRIMARY KEY,
            student_id INT NOT NULL,
            course_id INT NOT NULL,
            transaction_type VARCHAR(50) NOT NULL DEFAULT 'Course Registration',
            semester VARCHAR(20) NOT NULL,
            year INT NOT NULL,
            registration_date DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
            status VARCHAR(30) NOT NULL DEFAULT 'Active',
            registration_fee DECIMAL(10,2) NOT NULL DEFAULT 0,
            created_by_user_id INT NULL
        )
        """,
        """
        CREATE TABLE IF NOT EXISTS payment_transactions (
            transaction_id INT AUTO_INCREMENT PRIMARY KEY,
            student_id INT NOT NULL,
            amount DECIMAL(10,2) NOT NULL,
            payment_method VARCHAR(50) NOT NULL DEFAULT 'Cash',
            payment_date DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
            transaction_type VARCHAR(50) NOT NULL DEFAULT 'Payment',
            payment_description VARCHAR(255) NULL,
            status VARCHAR(30) NOT NULL DEFAULT 'Completed',
            receipt_number VARCHAR(60) NOT NULL,
            created_by_user_id INT NULL
        )
        """
    ];

    foreach (var statement in statements)
    {
        await using var cmd = new MySqlCommand(statement, conn);
        await cmd.ExecuteNonQueryAsync();
    }
}

static async Task<List<TransactionReportData>> QueryTransactionsAsync(MySqlConnection conn, TransactionReportRequest request)
{
    const string sql = """
        SELECT *
        FROM (
            SELECT
                et.transaction_id,
                et.student_id,
                COALESCE(CONCAT(s.first_name, ' ', s.last_name), 'Unknown') AS student_name,
                et.transaction_type,
                COALESCE(et.notes, '') AS description,
                et.transaction_date,
                NULL AS amount,
                et.status
            FROM enrollment_transactions et
            LEFT JOIN students s ON et.student_id = s.student_id

            UNION ALL

            SELECT
                crt.transaction_id,
                crt.student_id,
                COALESCE(CONCAT(s.first_name, ' ', s.last_name), 'Unknown') AS student_name,
                crt.transaction_type,
                CONCAT(crt.semester, ' ', crt.year) AS description,
                crt.registration_date AS transaction_date,
                crt.registration_fee AS amount,
                crt.status
            FROM course_registration_transactions crt
            LEFT JOIN students s ON crt.student_id = s.student_id

            UNION ALL

            SELECT
                pt.transaction_id,
                pt.student_id,
                COALESCE(CONCAT(s.first_name, ' ', s.last_name), 'Unknown') AS student_name,
                pt.transaction_type,
                COALESCE(pt.payment_description, pt.receipt_number) AS description,
                pt.payment_date AS transaction_date,
                pt.amount,
                pt.status
            FROM payment_transactions pt
            LEFT JOIN students s ON pt.student_id = s.student_id
        ) transactions
        WHERE (@transactionType IS NULL OR @transactionType = '' OR @transactionType = 'All' OR transaction_type = @transactionType)
          AND (@startDate IS NULL OR transaction_date >= @startDate)
          AND (@endDate IS NULL OR transaction_date <= @endDate)
          AND (@studentId IS NULL OR student_id = @studentId)
        ORDER BY transaction_date DESC
        LIMIT 1000
        """;

    await using var cmd = new MySqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@transactionType", string.IsNullOrWhiteSpace(request.TransactionType) ? "All" : request.TransactionType);
    cmd.Parameters.AddWithValue("@startDate", request.StartDate ?? (object)DBNull.Value);
    cmd.Parameters.AddWithValue("@endDate", request.EndDate ?? (object)DBNull.Value);
    cmd.Parameters.AddWithValue("@studentId", request.StudentId is > 0 ? request.StudentId.Value : DBNull.Value);

    await using var reader = await cmd.ExecuteReaderAsync();
    var transactions = new List<TransactionReportData>();

    while (await reader.ReadAsync())
    {
        transactions.Add(new TransactionReportData
        {
            TransactionId = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
            StudentId = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
            StudentName = reader.IsDBNull(2) ? "Unknown" : reader.GetString(2),
            TransactionType = reader.IsDBNull(3) ? "Unknown" : reader.GetString(3),
            Description = reader.IsDBNull(4) ? "" : reader.GetString(4),
            TransactionDate = reader.IsDBNull(5) ? DateTime.Now : reader.GetDateTime(5),
            Amount = reader.IsDBNull(6) ? null : reader.GetDecimal(6),
            Status = reader.IsDBNull(7) ? "Unknown" : reader.GetString(7)
        });
    }

    return transactions;
}

static string? ValidateAccountCreate(AccountCreateRequest request)
{
    if (string.IsNullOrWhiteSpace(request.Username) ||
        string.IsNullOrWhiteSpace(request.Email) ||
        string.IsNullOrWhiteSpace(request.Password) ||
        string.IsNullOrWhiteSpace(request.Role))
    {
        return "Username, email, password, and role are required.";
    }

    if (request.Password.Length < 6)
    {
        return "Password must be at least 6 characters.";
    }

    return ValidateAccountCommon(request.Role, request.Status, request.StudentId, request.InstructorId);
}

static string? ValidateAccountUpdate(AccountUpdateRequest request)
{
    if (string.IsNullOrWhiteSpace(request.Username) ||
        string.IsNullOrWhiteSpace(request.Email) ||
        string.IsNullOrWhiteSpace(request.Role))
    {
        return "Username, email, and role are required.";
    }

    if (!string.IsNullOrWhiteSpace(request.Password) && request.Password.Length < 6)
    {
        return "Password must be at least 6 characters.";
    }

    return ValidateAccountCommon(request.Role, request.Status, request.StudentId, request.InstructorId);
}

static string? ValidateAccountCommon(string role, string status, int? studentId, int? instructorId)
{
    if (!new[] { "Admin", "Instructor", "Student" }.Contains(role))
    {
        return "Role must be Admin, Instructor, or Student.";
    }

    if (!IsAllowedStatus(status))
    {
        return "Status must be Active, Inactive, or Suspended.";
    }

    if (role == "Student" && studentId is null)
    {
        return "Student role requires a student ID.";
    }

    if (role == "Instructor" && instructorId is null)
    {
        return "Instructor role requires an instructor ID.";
    }

    return null;
}

static bool IsAllowedStatus(string status) =>
    new[] { "Active", "Inactive", "Suspended" }.Contains(status);

static string ComputeSha256(string value)
{
    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
    return Convert.ToHexString(bytes).ToLowerInvariant();
}

static async Task<int> ExecuteIntAsync(MySqlConnection conn, string sql)
{
    await using var cmd = new MySqlCommand(sql, conn);
    var result = await cmd.ExecuteScalarAsync();
    return Convert.ToInt32(result);
}

static async Task<decimal?> ExecuteNullableDecimalAsync(MySqlConnection conn, string sql)
{
    await using var cmd = new MySqlCommand(sql, conn);
    var result = await cmd.ExecuteScalarAsync();
    return result == null || result == DBNull.Value ? null : Convert.ToDecimal(result);
}

static async Task<int> ExecuteNonQueryAsync(MySqlConnection conn, string sql, params MySqlParameter[] parameters)
{
    await using var cmd = new MySqlCommand(sql, conn);
    cmd.Parameters.AddRange(parameters);
    return await cmd.ExecuteNonQueryAsync();
}

static async Task<List<T>> QueryAsync<T>(MySqlCommand cmd, Func<DbDataReader, T> map)
{
    await using var reader = await cmd.ExecuteReaderAsync();
    var items = new List<T>();

    while (await reader.ReadAsync())
    {
        items.Add(map(reader));
    }

    return items;
}

static ProblemHttpResult DatabaseProblem(Exception ex)
{
    return TypedResults.Problem(
        title: "Database operation failed",
        detail: ex.Message,
        statusCode: StatusCodes.Status500InternalServerError);
}

record LoginRequest(string Username, string Password);
record PasswordRecoveryRequest(string UsernameOrEmail, string Email, string NewPassword);
record AuthResponse(int AccountId, string UserName, string Email, string Role, string Status, string Token);
record AccountDto(int AccountId, string Username, string Email, string Role, string Status, int? StudentId, int? InstructorId, DateTime CreatedAt, DateTime UpdatedAt);
record AccountCreateRequest(string Username, string Email, string Password, string Role, string Status, int? StudentId, int? InstructorId);
record AccountUpdateRequest(string Username, string Email, string? Password, string Role, string Status, int? StudentId, int? InstructorId);
record AccountStatusUpdateRequest(string Status);
record DashboardDto(int TotalStudents, int TotalCourses, decimal? AverageGpa, List<EnrollmentViewDto> RecentEnrollments);
record EnrollmentViewDto(int EnrollmentId, string StudentName, string CourseName, string Semester, int Year, decimal? Grade);
record StudentDto(int StudentId, string FirstName, string LastName, string Gender, string? BirthDate, string Email);
record StudentUpsertRequest(string FirstName, string LastName, string? Gender, string? BirthDate, string? Email);
record CourseDto(int CourseId, string CourseName, string CourseCode, int Units);
record CourseUpsertRequest(string CourseName, string CourseCode, int Units);
record EnrollmentDto(int EnrollmentId, int StudentId, int CourseId, string StudentName, string CourseName, string Semester, int Year, decimal? Grade);
record EnrollmentUpsertRequest(int StudentId, int CourseId, string? Semester, int Year, decimal? Grade);
record ReportResponse(string[] Columns, object?[][] Rows);
record ReportExportRequest(string Type, string? Semester, int? Year, string? SignerName);

static class DataReaderExtensions
{
    public static int GetInt32(this DbDataReader reader, string columnName) =>
        reader.GetInt32(reader.GetOrdinal(columnName));

    public static string GetString(this DbDataReader reader, string columnName) =>
        reader.GetString(reader.GetOrdinal(columnName));

    public static DateTime GetDateTime(this DbDataReader reader, string columnName) =>
        reader.GetDateTime(reader.GetOrdinal(columnName));

    public static decimal GetDecimal(this DbDataReader reader, string columnName) =>
        reader.GetDecimal(reader.GetOrdinal(columnName));

    public static bool IsDBNull(this DbDataReader reader, string columnName) =>
        reader.IsDBNull(reader.GetOrdinal(columnName));
}
