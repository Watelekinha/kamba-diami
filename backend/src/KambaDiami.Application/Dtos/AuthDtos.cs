using KambaDiami.Domain.Enums;

namespace KambaDiami.Application.Dtos;

public class RegisterRequest
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string PhoneNumber { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string ConfirmPassword { get; set; } = default!;
    public UserRole Role { get; set; }

    /// <summary>Município da API angolaprovinciasapi.ao (ex.: "belas"). A plataforma já não pede o bairro.</summary>
    public string MunicipalitySlug { get; set; } = default!;
    public string? StreetDetail { get; set; }
}

public class LoginRequest
{
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
}

public class AuthResponse
{
    public string Token { get; set; } = default!;
    public DateTime ExpiresAt { get; set; }
    public UserProfileDto User { get; set; } = default!;
}

public class ForgotPasswordRequest
{
    public string Email { get; set; } = default!;
}

public class ResetPasswordRequest
{
    public string Email { get; set; } = default!;
    public string Token { get; set; } = default!;
    public string NewPassword { get; set; } = default!;
    public string ConfirmNewPassword { get; set; } = default!;
}

public class TokenResponse
{
    public string Token { get; set; } = default!;
    public DateTime ExpiresAt { get; set; }
}
