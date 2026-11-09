using KambaDiami.Application.Dtos;
using KambaDiami.Domain.Entities;

namespace KambaDiami.Application.Mapping;

public static class JobPostingMappingExtensions
{
    public static JobPostingDto ToDto(this JobPosting posting, double? authorAverageRating = null, int authorRatingsCount = 0)
    {
        return new JobPostingDto
        {
            Id = posting.Id,
            AuthorUserId = posting.AuthorUserId,
            AuthorName = $"{posting.Author.FirstName} {posting.Author.LastName}",
            AuthorPhotoPath = posting.Author.PhotoPath,
            AuthorRole = posting.Author.Role,
            AuthorAverageRating = authorAverageRating,
            AuthorRatingsCount = authorRatingsCount,
            Type = posting.Type,
            PaymentType = posting.PaymentType,
            PaymentValue = posting.PaymentValue,
            HoursPerDay = posting.HoursPerDay,
            Comments = posting.Comments,
            Status = posting.Status,
            CreatedAt = posting.CreatedAt,
            Categories = posting.Categories
                .Select(c => new ServiceCategoryDto { Id = c.ServiceCategoryId, Name = c.ServiceCategory.Name })
                .ToList(),
            Locations = posting.Locations
                .Select(l => new JobPostingLocationDto
                {
                    NeighborhoodId = l.NeighborhoodId,
                    NeighborhoodName = l.Neighborhood.Name,
                    MunicipalityId = l.Neighborhood.Municipality.Id,
                    MunicipalityName = l.Neighborhood.Municipality.Name,
                    ProvinceId = l.Neighborhood.Municipality.Province.Id,
                    ProvinceName = l.Neighborhood.Municipality.Province.Name
                })
                .ToList()
        };
    }
}
