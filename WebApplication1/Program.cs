using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Pomelo.EntityFrameworkCore.MySql;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger/OpenAPI
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Student Information System API", 
        Version = "v1",
        Description = "API for managing students, courses, enrollments, and instructors"
    });
    
    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Configure Database Connection for act_edp database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Configure JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "YourSuperSecretKeyHereAtLeast32CharactersLong!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "StudentInfoSystem";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "StudentInfoSystemAPI";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => 
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Student API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Test endpoint to check database connection
app.MapGet("/api/test-connection", async (ApplicationDbContext db) =>
{
    try
    {
        var canConnect = await db.Database.CanConnectAsync();
        if (canConnect)
        {
            var studentCount = await db.Students.CountAsync();
            return Results.Ok(new { 
                message = "Database connection successful!", 
                database = "act_edp",
                studentCount = studentCount 
            });
        }
        return Results.Ok(new { message = "Connected but cannot verify data" });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("TestConnection")
.WithTags("Test");

// Student Endpoints
app.MapGet("/api/students", async (ApplicationDbContext db) =>
{
    var students = await db.Students.ToListAsync();
    return Results.Ok(students);
})
.WithName("GetAllStudents")
.WithTags("Students");

app.MapGet("/api/students/{id}", async (ApplicationDbContext db, int id) =>
{
    var student = await db.Students.FindAsync(id);
    if (student == null)
        return Results.NotFound();
    return Results.Ok(student);
})
.WithName("GetStudentById")
.WithTags("Students");

app.MapPost("/api/students", async (ApplicationDbContext db, Student student) =>
{
    db.Students.Add(student);
    await db.SaveChangesAsync();
    return Results.Created($"/api/students/{student.StudentId}", student);
})
.WithName("CreateStudent")
.WithTags("Students");

app.MapPut("/api/students/{id}", async (ApplicationDbContext db, int id, Student updatedStudent) =>
{
    var student = await db.Students.FindAsync(id);
    if (student == null)
        return Results.NotFound();
    
    student.FirstName = updatedStudent.FirstName;
    student.LastName = updatedStudent.LastName;
    student.Gender = updatedStudent.Gender;
    student.BirthDate = updatedStudent.BirthDate;
    student.Email = updatedStudent.Email;
    
    await db.SaveChangesAsync();
    return Results.Ok(student);
})
.WithName("UpdateStudent")
.WithTags("Students");

app.MapDelete("/api/students/{id}", async (ApplicationDbContext db, int id) =>
{
    var student = await db.Students.FindAsync(id);
    if (student == null)
        return Results.NotFound();
    
    db.Students.Remove(student);
    await db.SaveChangesAsync();
    return Results.NoContent();
})
.WithName("DeleteStudent")
.WithTags("Students");

// Course Endpoints
app.MapGet("/api/courses", async (ApplicationDbContext db) =>
{
    var courses = await db.Courses.ToListAsync();
    return Results.Ok(courses);
})
.WithName("GetAllCourses")
.WithTags("Courses");

app.MapGet("/api/courses/{id}", async (ApplicationDbContext db, int id) =>
{
    var course = await db.Courses.FindAsync(id);
    if (course == null)
        return Results.NotFound();
    return Results.Ok(course);
})
.WithName("GetCourseById")
.WithTags("Courses");

app.MapPost("/api/courses", async (ApplicationDbContext db, Course course) =>
{
    db.Courses.Add(course);
    await db.SaveChangesAsync();
    return Results.Created($"/api/courses/{course.CourseId}", course);
})
.WithName("CreateCourse")
.WithTags("Courses");

app.MapPut("/api/courses/{id}", async (ApplicationDbContext db, int id, Course updatedCourse) =>
{
    var course = await db.Courses.FindAsync(id);
    if (course == null)
        return Results.NotFound();
    
    course.CourseName = updatedCourse.CourseName;
    course.CourseCode = updatedCourse.CourseCode;
    course.Units = updatedCourse.Units;
    
    await db.SaveChangesAsync();
    return Results.Ok(course);
})
.WithName("UpdateCourse")
.WithTags("Courses");

app.MapDelete("/api/courses/{id}", async (ApplicationDbContext db, int id) =>
{
    var course = await db.Courses.FindAsync(id);
    if (course == null)
        return Results.NotFound();
    
    db.Courses.Remove(course);
    await db.SaveChangesAsync();
    return Results.NoContent();
})
.WithName("DeleteCourse")
.WithTags("Courses");

// Enrollment Endpoints
app.MapGet("/api/enrollments", async (ApplicationDbContext db) =>
{
    var enrollments = await db.Enrollments
        .Include(e => e.Student)
        .Include(e => e.Course)
        .ToListAsync();
    return Results.Ok(enrollments);
})
.WithName("GetAllEnrollments")
.WithTags("Enrollments");

app.MapPost("/api/enrollments", async (ApplicationDbContext db, Enrollment enrollment) =>
{
    db.Enrollments.Add(enrollment);
    await db.SaveChangesAsync();
    
    // Log the enrollment action
    var log = new EnrollmentLog
    {
        ActionType = "INSERT",
        EnrollmentId = enrollment.EnrollmentId,
        StudentId = enrollment.StudentId,
        CourseId = enrollment.CourseId,
        ActionTime = DateTime.Now
    };
    db.EnrollmentLogs.Add(log);
    await db.SaveChangesAsync();
    
    return Results.Created($"/api/enrollments/{enrollment.EnrollmentId}", enrollment);
})
.WithName("EnrollStudent")
.WithTags("Enrollments");

app.MapPut("/api/enrollments/{id}/grade", async (ApplicationDbContext db, int id, decimal grade) =>
{
    var enrollment = await db.Enrollments.FindAsync(id);
    if (enrollment == null)
        return Results.NotFound();
    
    enrollment.Grade = grade;
    await db.SaveChangesAsync();
    
    // Log the grade update
    var log = new EnrollmentLog
    {
        ActionType = "UPDATE",
        EnrollmentId = enrollment.EnrollmentId,
        StudentId = enrollment.StudentId,
        CourseId = enrollment.CourseId,
        ActionTime = DateTime.Now
    };
    db.EnrollmentLogs.Add(log);
    await db.SaveChangesAsync();
    
    return Results.Ok(enrollment);
})
.WithName("UpdateGrade")
.WithTags("Enrollments");

app.MapDelete("/api/enrollments/{id}", async (ApplicationDbContext db, int id) =>
{
    var enrollment = await db.Enrollments.FindAsync(id);
    if (enrollment == null)
        return Results.NotFound();
    
    // Log before deleting
    var log = new EnrollmentLog
    {
        ActionType = "DELETE",
        EnrollmentId = enrollment.EnrollmentId,
        StudentId = enrollment.StudentId,
        CourseId = enrollment.CourseId,
        ActionTime = DateTime.Now
    };
    db.EnrollmentLogs.Add(log);
    
    db.Enrollments.Remove(enrollment);
    await db.SaveChangesAsync();
    
    return Results.NoContent();
})
.WithName("DropEnrollment")
.WithTags("Enrollments");

// Instructor Endpoints
app.MapGet("/api/instructors", async (ApplicationDbContext db) =>
{
    var instructors = await db.Instructors.ToListAsync();
    return Results.Ok(instructors);
})
.WithName("GetAllInstructors")
.WithTags("Instructors");

app.MapPost("/api/course-assignments", async (ApplicationDbContext db, CourseAssignment assignment) =>
{
    db.CourseAssignments.Add(assignment);
    await db.SaveChangesAsync();
    return Results.Ok(assignment);
})
.WithName("AssignCourse")
.WithTags("Instructors");

// Report Endpoints
app.MapGet("/api/reports/student-grades", async (ApplicationDbContext db, string? semester, int? year) =>
{
    var query = from e in db.Enrollments
                join s in db.Students on e.StudentId equals s.StudentId
                join c in db.Courses on e.CourseId equals c.CourseId
                select new
                {
                    StudentId = s.StudentId,
                    StudentName = s.FirstName + " " + s.LastName,
                    CourseName = c.CourseName,
                    Semester = e.Semester,
                    Year = e.Year,
                    Grade = e.Grade
                };
    
    if (!string.IsNullOrEmpty(semester))
        query = query.Where(x => x.Semester == semester);
    if (year.HasValue)
        query = query.Where(x => x.Year == year.Value);
    
    var result = await query.ToListAsync();
    return Results.Ok(result);
})
.WithName("GetStudentGradesReport")
.WithTags("Reports");

app.MapGet("/api/reports/course-enrollment", async (ApplicationDbContext db) =>
{
    var report = await (from c in db.Courses
                        join e in db.Enrollments on c.CourseId equals e.CourseId into gj
                        from subEnrollment in gj.DefaultIfEmpty()
                        group subEnrollment by new { c.CourseId, c.CourseName, c.CourseCode, c.Units } into g
                        select new
                        {
                            CourseName = g.Key.CourseName,
                            CourseCode = g.Key.CourseCode,
                            Units = g.Key.Units,
                            TotalStudents = g.Count(x => x != null),
                            AverageGrade = g.Average(x => x != null ? x.Grade : (decimal?)null)
                        })
                        .ToListAsync();
    
    return Results.Ok(report);
})
.WithName("GetCourseEnrollmentReport")
.WithTags("Reports");

// GPA Computation endpoint
app.MapGet("/api/students/{id}/compute-gpa", async (ApplicationDbContext db, int id) =>
{
    var gpa = await db.Enrollments
        .Where(e => e.StudentId == id && e.Grade.HasValue)
        .AverageAsync(e => e.Grade);
    
    return Results.Ok(new { StudentId = id, GPA = gpa ?? 0 });
})
.WithName("ComputeGPA")
.WithTags("Students");

// Login endpoint (simple version)
app.MapPost("/api/login", (LoginRequest request) =>
{
    if (request.Username == "admin" && request.Password == "admin123")
    {
        var token = GenerateJwtToken();
        return Results.Ok(new { 
            token = token, 
            message = "Login successful",
            username = request.Username
        });
    }
    return Results.Unauthorized();
})
.WithName("Login")
.WithTags("Authentication");

string GenerateJwtToken()
{
    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
    var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
        issuer: jwtIssuer,
        audience: jwtAudience,
        expires: DateTime.Now.AddMinutes(60),
        signingCredentials: credentials);
    return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
}

app.Run();

// Database Context
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
    
    public DbSet<Student> Students { get; set; }
    public DbSet<Course> Courses { get; set; }
    public DbSet<Enrollment> Enrollments { get; set; }
    public DbSet<Instructor> Instructors { get; set; }
    public DbSet<CourseAssignment> CourseAssignments { get; set; }
    public DbSet<EnrollmentLog> EnrollmentLogs { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Enrollment>()
            .HasKey(e => e.EnrollmentId);
        
        modelBuilder.Entity<CourseAssignment>()
            .HasKey(ca => ca.AssignmentId);
        
        modelBuilder.Entity<EnrollmentLog>()
            .HasKey(el => el.LogId);
    }
}

// Models matching your database
public class Student
{
    public int StudentId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Gender { get; set; }
    public DateTime BirthDate { get; set; }
    public string Email { get; set; }
}

public class Course
{
    public int CourseId { get; set; }
    public string CourseName { get; set; }
    public string CourseCode { get; set; }
    public int Units { get; set; }
}

public class Enrollment
{
    public int EnrollmentId { get; set; }
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public string Semester { get; set; }
    public int Year { get; set; }
    public decimal? Grade { get; set; }
    
    public Student Student { get; set; }
    public Course Course { get; set; }
}

public class Instructor
{
    public int InstructorId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
}

public class CourseAssignment
{
    public int AssignmentId { get; set; }
    public int InstructorId { get; set; }
    public int CourseId { get; set; }
    public string Semester { get; set; }
    public int Year { get; set; }
}

public class EnrollmentLog
{
    public int LogId { get; set; }
    public string ActionType { get; set; }
    public int EnrollmentId { get; set; }
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public DateTime ActionTime { get; set; }
}

public record LoginRequest(string Username, string Password);