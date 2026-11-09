namespace KambaDiami.Application.Dtos;

public class ProvinceDto
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
}

public class MunicipalityDto
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public int ProvinceId { get; set; }
}

public class NeighborhoodDto
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public int MunicipalityId { get; set; }
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
    public int MunicipalityId { get; set; }
    public string Name { get; set; } = default!;
}

public class CreateMunicipalityRequest
{
    public int ProvinceId { get; set; }
    public string Name { get; set; } = default!;
}
