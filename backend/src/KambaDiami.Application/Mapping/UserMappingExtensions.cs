using KambaDiami.Application.Dtos;
using KambaDiami.Domain.Entities;

namespace KambaDiami.Application.Mapping;

public static class UserMappingExtensions
{
    public static UserProfileDto ToProfileDto(this ApplicationUser user, double? averageRating = null, int ratingsCount = 0)
    {
        return new UserProfileDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            PhotoPath = user.PhotoPath,
            Role = user.Role,
            Status = user.Status,
            AverageRating = averageRating,
            RatingsCount = ratingsCount,
            Bio = user.SecretariaProfile?.Bio,
            YearsOfExperience = user.SecretariaProfile?.AnosExperiencia,
            FamilyDescription = user.FamiliaProfile?.Descricao,
            HouseholdSize = user.FamiliaProfile?.NumeroAgregadoFamiliar,
            Address = new AddressDto
            {
                NeighborhoodId = user.Address.NeighborhoodId,
                NeighborhoodName = user.Address.Neighborhood.Name,
                MunicipalityId = user.Address.Neighborhood.Municipality.Id,
                MunicipalityName = user.Address.Neighborhood.Municipality.Name,
                ProvinceId = user.Address.Neighborhood.Municipality.Province.Id,
                ProvinceName = user.Address.Neighborhood.Municipality.Province.Name,
                StreetDetail = user.Address.StreetDetail
            }
        };
    }
}
