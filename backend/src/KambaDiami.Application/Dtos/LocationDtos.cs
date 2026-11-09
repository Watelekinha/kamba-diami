namespace KambaDiami.Application.Dtos;

/// <summary>Província de Angola, identificada pelo slug da API angolaprovinciasapi.ao (ex.: "luanda").</summary>
public class ProvinceDto
{
    public string Slug { get; set; } = default!;
    public string Name { get; set; } = default!;
}

/// <summary>Município de Angola, identificado pelo slug da API angolaprovinciasapi.ao (ex.: "talatona").</summary>
public class MunicipalityDto
{
    public string Slug { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string ProvinceSlug { get; set; } = default!;
}

/// <summary>
/// Bairro de um município. Os que já existem na plataforma têm Id; as comunas da API que ainda não
/// foram usadas aparecem como sugestão (Id nulo) e são criadas quando alguém as escolhe.
/// </summary>
public class NeighborhoodDto
{
    public int? Id { get; set; }
    public string Name { get; set; } = default!;
    public string MunicipalitySlug { get; set; } = default!;
    public bool IsSuggestion { get; set; }
}

public class ServiceCategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
}

public class CreateServiceCategoryRequest
{
    public string Name { get; set; } = default!;
}

public class CreateNeighborhoodRequest
{
    public string MunicipalitySlug { get; set; } = default!;
    public string Name { get; set; } = default!;
}
