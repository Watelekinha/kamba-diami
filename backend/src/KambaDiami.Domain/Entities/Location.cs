namespace KambaDiami.Domain.Entities;

public class Province
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;

    public ICollection<Municipality> Municipalities { get; set; } = new List<Municipality>();
}

public class Municipality
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;

    public int ProvinceId { get; set; }
    public Province Province { get; set; } = default!;

    public ICollection<Neighborhood> Neighborhoods { get; set; } = new List<Neighborhood>();
}

public class Neighborhood
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;

    public int MunicipalityId { get; set; }
    public Municipality Municipality { get; set; } = default!;
}

public class Address
{
    public int Id { get; set; }
    public string? StreetDetail { get; set; }

    public int NeighborhoodId { get; set; }
    public Neighborhood Neighborhood { get; set; } = default!;
}
