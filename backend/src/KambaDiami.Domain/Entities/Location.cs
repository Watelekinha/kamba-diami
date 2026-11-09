namespace KambaDiami.Domain.Entities;

/// <summary>
/// Bairro criado na plataforma. As províncias e os municípios vêm da API pública de Angola
/// (angolaprovinciasapi.ao) e não têm tabela própria: o bairro guarda o slug que os identifica
/// nessa API e os nomes, para a morada aparecer nos contratos e nas pesquisas mesmo sem a API.
/// </summary>
public class Neighborhood
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;

    public string MunicipalitySlug { get; set; } = default!;
    public string MunicipalityName { get; set; } = default!;

    public string ProvinceSlug { get; set; } = default!;
    public string ProvinceName { get; set; } = default!;
}

public class Address
{
    public int Id { get; set; }
    public string? StreetDetail { get; set; }

    public int NeighborhoodId { get; set; }
    public Neighborhood Neighborhood { get; set; } = default!;
}
