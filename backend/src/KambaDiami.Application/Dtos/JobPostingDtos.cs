using KambaDiami.Domain.Enums;

namespace KambaDiami.Application.Dtos;

public class JobPostingLocationDto
{
    public int NeighborhoodId { get; set; }
    public string NeighborhoodName { get; set; } = default!;
    public int MunicipalityId { get; set; }
    public string MunicipalityName { get; set; } = default!;
    public int ProvinceId { get; set; }
    public string ProvinceName { get; set; } = default!;
}

public class JobPostingDto
{
    public int Id { get; set; }

    public Guid AuthorUserId { get; set; }
    public string AuthorName { get; set; } = default!;
    public string? AuthorPhotoPath { get; set; }
    public UserRole AuthorRole { get; set; }
    public double? AuthorAverageRating { get; set; }
    public int AuthorRatingsCount { get; set; }

    public JobPostingType Type { get; set; }
    public PaymentType PaymentType { get; set; }
    public decimal PaymentValue { get; set; }
    public decimal? HoursPerDay { get; set; }
    public string? Comments { get; set; }
    public JobPostingStatus Status { get; set; }

    public List<ServiceCategoryDto> Categories { get; set; } = [];
    public List<JobPostingLocationDto> Locations { get; set; } = [];

    public DateTime CreatedAt { get; set; }
}

public class CreateJobPostingRequest
{
    public List<int> CategoryIds { get; set; } = [];
    public PaymentType PaymentType { get; set; }
    public decimal PaymentValue { get; set; }
    public decimal? HoursPerDay { get; set; }
    public string? Comments { get; set; }
    public List<int> NeighborhoodIds { get; set; } = [];
}

public class JobPostingFilter
{
    public JobPostingType? Type { get; set; }
    public int? CategoryId { get; set; }
    public PaymentType? PaymentType { get; set; }
    public int? ProvinceId { get; set; }
    public int? MunicipalityId { get; set; }
    public int? NeighborhoodId { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
