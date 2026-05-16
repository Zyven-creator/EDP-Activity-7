using System;

namespace StudentInfoSystemAPI;

// Transaction Models
public class EnrollmentTransaction
{
    public int TransactionId { get; set; }
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public string TransactionType { get; set; } = "Enrollment"; // Enrollment
    public DateTime TransactionDate { get; set; }
    public string Status { get; set; } = "Completed"; // Pending, Completed, Cancelled
    public string Notes { get; set; } = "";
    public int CreatedByUserId { get; set; }
}

public class CourseRegistrationTransaction
{
    public int TransactionId { get; set; }
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public string TransactionType { get; set; } = "Course Registration";
    public string Semester { get; set; }
    public int Year { get; set; }
    public DateTime RegistrationDate { get; set; }
    public string Status { get; set; } = "Active"; // Active, Dropped, Completed
    public decimal RegistrationFee { get; set; }
    public int CreatedByUserId { get; set; }
}

public class PaymentTransaction
{
    public int TransactionId { get; set; }
    public int StudentId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "Credit Card"; // Credit Card, Check, Cash, Bank Transfer
    public DateTime PaymentDate { get; set; }
    public string TransactionType { get; set; } = "Payment";
    public string PaymentDescription { get; set; } = "";
    public string Status { get; set; } = "Completed"; // Pending, Completed, Failed, Refunded
    public string ReceiptNumber { get; set; }
    public int CreatedByUserId { get; set; }
}

// Request/Response Models
public class CreateEnrollmentTransactionRequest
{
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public string Notes { get; set; } = "";
}

public class CreateCourseRegistrationRequest
{
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public string Semester { get; set; }
    public int Year { get; set; }
    public decimal RegistrationFee { get; set; }
}

public class CreatePaymentTransactionRequest
{
    public int StudentId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; }
    public string PaymentDescription { get; set; }
}

public class TransactionReportRequest
{
    public string TransactionType { get; set; } = "All"; // All, Enrollment, CourseRegistration, Payment
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? StudentId { get; set; }
    public string? SignerName { get; set; }
}

public class TransactionReportData
{
    public int TransactionId { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; }
    public string TransactionType { get; set; }
    public string Description { get; set; }
    public DateTime TransactionDate { get; set; }
    public decimal? Amount { get; set; }
    public string Status { get; set; }
}

public class LoginRequest
{
    public string Username { get; set; }
    public string Password { get; set; }
}

public class AuthResponse
{
    public AuthResponse(int accountId, string username, string email, string role, string status, string token)
    {
        AccountId = accountId;
        Username = username;
        Email = email;
        Role = role;
        Status = status;
        Token = token;
    }

    public int AccountId { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string Role { get; set; }
    public string Status { get; set; }
    public string Token { get; set; }
}

public class PasswordRecoveryRequest
{
    public string UsernameOrEmail { get; set; }
    public string Email { get; set; }
    public string NewPassword { get; set; }
}
