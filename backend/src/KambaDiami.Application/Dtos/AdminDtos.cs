using KambaDiami.Domain.Enums;

namespace KambaDiami.Application.Dtos;

// ---- Visão geral ----

public class AdminOverviewDto
{
    public int PeriodDays { get; set; }
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public DateTime GeneratedAt { get; set; }

    public AdminTotalsDto Totals { get; set; } = new();
    public List<AdminMetricDto> Metrics { get; set; } = [];
    public List<AdminDailyPointDto> Series { get; set; } = [];
    public AdminFunnelDto Funnel { get; set; } = new();

    public List<AdminCountDto> ContractsByStatus { get; set; } = [];
    public List<AdminCountDto> UsersByProvince { get; set; } = [];
    public List<AdminCountDto> TopCategories { get; set; } = [];

    public List<AdminActivityItemDto> RecentActivity { get; set; } = [];
}

public class AdminTotalsDto
{
    public int Users { get; set; }
    public int Familias { get; set; }
    public int Secretarias { get; set; }
    public int Admins { get; set; }
    public int SuspendedUsers { get; set; }
    public int ActivePostings { get; set; }
    public int Postings { get; set; }
    public int Matches { get; set; }
    public int Contracts { get; set; }
    public int ActiveContracts { get; set; }
    public int CompletedContracts { get; set; }
    public int Messages { get; set; }
    public int Ratings { get; set; }
    public double? AverageRating { get; set; }
    public int ActiveUsersToday { get; set; }
}

/// <summary>Um indicador do período escolhido comparado com o período anterior de igual duração.</summary>
public class AdminMetricDto
{
    public string Key { get; set; } = default!;
    public int Current { get; set; }
    public int Previous { get; set; }
    public double? ChangePercent { get; set; }
}

public class AdminDailyPointDto
{
    public DateOnly Date { get; set; }
    public int Registrations { get; set; }
    public int Logins { get; set; }
    public int ActiveUsers { get; set; }
    public int Postings { get; set; }
    public int Matches { get; set; }
    public int Contracts { get; set; }
    public int Messages { get; set; }
}

public class AdminFunnelDto
{
    public int Postings { get; set; }
    public int Matches { get; set; }
    public int AcceptedMatches { get; set; }
    public int Contracts { get; set; }
    public int SignedContracts { get; set; }
}

public class AdminCountDto
{
    public string Label { get; set; } = default!;
    public int Count { get; set; }
}

public class AdminActivityItemDto
{
    public string Type { get; set; } = default!;
    public string Description { get; set; } = default!;
    public DateTime OccurredAt { get; set; }
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
}

// ---- Utilizadores ----

public class AdminUserFilter
{
    public string? Search { get; set; }
    public UserRole? Role { get; set; }
    public AccountStatus? Status { get; set; }
    public string Sort { get; set; } = "recent";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class AdminUserListItemDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string PhoneNumber { get; set; } = default!;
    public string? PhotoPath { get; set; }
    public UserRole Role { get; set; }
    public AccountStatus Status { get; set; }
    public string Location { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsLockedOut { get; set; }
    public int PostingsCount { get; set; }
    public int ContractsCount { get; set; }
}

public class AdminUserDetailDto : AdminUserListItemDto
{
    public DateTime? SuspendedAt { get; set; }
    public string? SuspensionReason { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public int FailedLoginAttempts { get; set; }
    public int LoginsLast30Days { get; set; }
    public int ActiveDaysLast30Days { get; set; }
    public int MatchesCount { get; set; }
    public int MessagesCount { get; set; }
    public double? AverageRating { get; set; }
    public int RatingsCount { get; set; }
    public List<AdminLoginEventDto> RecentLogins { get; set; } = [];
    public List<AdminPostingListItemDto> RecentPostings { get; set; } = [];
    public List<AdminContractListItemDto> RecentContracts { get; set; } = [];
}

public class UpdateUserStatusRequest
{
    public AccountStatus Status { get; set; }
    public string? Reason { get; set; }
}

public class CreateAdminRequest
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string PhoneNumber { get; set; } = default!;
    public string Password { get; set; } = default!;
}

// ---- Anúncios ----

public class AdminPostingFilter
{
    public string? Search { get; set; }
    public JobPostingStatus? Status { get; set; }
    public JobPostingType? Type { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class AdminPostingListItemDto
{
    public int Id { get; set; }
    public JobPostingType Type { get; set; }
    public JobPostingStatus Status { get; set; }
    public PaymentType PaymentType { get; set; }
    public decimal PaymentValue { get; set; }
    public string? Comments { get; set; }
    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = default!;
    public List<string> Categories { get; set; } = [];
    public List<string> Locations { get; set; } = [];
    public int MatchesCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UpdatePostingStatusRequest
{
    public JobPostingStatus Status { get; set; }
    public string? Reason { get; set; }
}

// ---- Contratos ----

public class AdminContractFilter
{
    public string? Search { get; set; }
    public ContractStatus? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class AdminContractListItemDto
{
    public int Id { get; set; }
    public string Reference { get; set; } = default!;
    public ContractStatus Status { get; set; }
    public PaymentType PaymentType { get; set; }
    public decimal PaymentValue { get; set; }
    public Guid FamiliaId { get; set; }
    public string FamiliaName { get; set; } = default!;
    public Guid SecretariaId { get; set; }
    public string SecretariaName { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// ---- Segurança e auditoria ----

public class AdminLoginEventFilter
{
    public string? Search { get; set; }
    public bool? Succeeded { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 30;
}

public class AdminLoginEventDto
{
    public long Id { get; set; }
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public string Email { get; set; } = default!;
    public bool Succeeded { get; set; }
    public string? FailureReason { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime OccurredAt { get; set; }
}

public class AdminAuditLogDto
{
    public long Id { get; set; }
    public Guid AdminUserId { get; set; }
    public string AdminName { get; set; } = default!;
    public string Action { get; set; } = default!;
    public string TargetType { get; set; } = default!;
    public string? TargetId { get; set; }
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; }
}
