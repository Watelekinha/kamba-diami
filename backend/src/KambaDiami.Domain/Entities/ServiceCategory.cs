namespace KambaDiami.Domain.Entities;

public class ServiceCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;

    public ICollection<JobPostingCategory> JobPostingCategories { get; set; } = new List<JobPostingCategory>();
}
