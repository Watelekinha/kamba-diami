namespace KambaDiami.Domain.Entities;

public class Rating
{
    public int Id { get; set; }

    public int ContractId { get; set; }
    public Contract Contract { get; set; } = default!;

    public Guid RaterUserId { get; set; }
    public ApplicationUser RaterUser { get; set; } = default!;

    public Guid RateeUserId { get; set; }
    public ApplicationUser RateeUser { get; set; } = default!;

    public int Score { get; set; }
    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
