using KambaDiami.Domain.Enums;

namespace KambaDiami.Domain.Entities;

public class JobPosting
{
    public int Id { get; set; }

    public Guid AuthorUserId { get; set; }
    public ApplicationUser Author { get; set; } = default!;

    public JobPostingType Type { get; set; }
    public PaymentType PaymentType { get; set; }
    public decimal PaymentValue { get; set; }
    public decimal? HoursPerDay { get; set; }
    public string? Comments { get; set; }
    public JobPostingStatus Status { get; set; } = JobPostingStatus.Ativo;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<JobPostingCategory> Categories { get; set; } = new List<JobPostingCategory>();
    public ICollection<JobPostingLocation> Locations { get; set; } = new List<JobPostingLocation>();
    public ICollection<Match> Matches { get; set; } = new List<Match>();
}

public class JobPostingCategory
{
    public int JobPostingId { get; set; }
    public JobPosting JobPosting { get; set; } = default!;

    public int ServiceCategoryId { get; set; }
    public ServiceCategory ServiceCategory { get; set; } = default!;
}

public class JobPostingLocation
{
    public int JobPostingId { get; set; }
    public JobPosting JobPosting { get; set; } = default!;

    public int NeighborhoodId { get; set; }
    public Neighborhood Neighborhood { get; set; } = default!;
}
