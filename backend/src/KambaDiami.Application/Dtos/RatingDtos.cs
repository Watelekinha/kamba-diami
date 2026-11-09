namespace KambaDiami.Application.Dtos;

public class RatingDto
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public int Score { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid RaterUserId { get; set; }
    public string RaterName { get; set; } = default!;
}

public class CreateRatingRequest
{
    public int Score { get; set; }
    public string? Comment { get; set; }
}
