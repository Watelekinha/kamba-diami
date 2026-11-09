using KambaDiami.Domain.Enums;

namespace KambaDiami.Application.Dtos;

public class UserProfileDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string PhoneNumber { get; set; } = default!;
    public string? PhotoPath { get; set; }
    public UserRole Role { get; set; }
    public AccountStatus Status { get; set; }

    public AddressDto Address { get; set; } = default!;

    public double? AverageRating { get; set; }
    public int RatingsCount { get; set; }
    public string? Bio { get; set; }
    public int? YearsOfExperience { get; set; }
    public string? FamilyDescription { get; set; }
    public int? HouseholdSize { get; set; }
}

public class AddressDto
{
    public int NeighborhoodId { get; set; }
    public string NeighborhoodName { get; set; } = default!;
    public string MunicipalitySlug { get; set; } = default!;
    public string MunicipalityName { get; set; } = default!;
    public string ProvinceSlug { get; set; } = default!;
    public string ProvinceName { get; set; } = default!;
    public string? StreetDetail { get; set; }
}

public class UpdateProfileRequest
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string PhoneNumber { get; set; } = default!;
    public int NeighborhoodId { get; set; }
    public string? StreetDetail { get; set; }
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = default!;
    public string NewPassword { get; set; } = default!;
    public string ConfirmNewPassword { get; set; } = default!;
}
