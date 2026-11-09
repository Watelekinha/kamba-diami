using KambaDiami.Application.Abstractions;
using KambaDiami.Domain.Entities;
using KambaDiami.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KambaDiami.Api.Services;

/// <summary>
/// A plataforma já não pede o bairro: a morada e as localidades dos anúncios ficam ao nível do município.
/// A tabela Neighborhoods mantém-se (moradas, anúncios e contratos apontam para ela), e cada município passa
/// a ter um registo com o nome do próprio município, criado na primeira vez que alguém o escolhe.
/// </summary>
public class MunicipalityAreaResolver
{
    private readonly KambaDiamiDbContext _db;
    private readonly IAngolaLocationsProvider _angola;

    public MunicipalityAreaResolver(KambaDiamiDbContext db, IAngolaLocationsProvider angola)
    {
        _db = db;
        _angola = angola;
    }

    /// <summary>
    /// O registo do município indicado, ou null se o município não existir. Lança
    /// <see cref="AngolaLocationsUnavailableException"/> se a API de províncias não responder e o município
    /// ainda não tiver nenhum registo na base de dados.
    /// </summary>
    public async Task<Neighborhood?> ResolveAsync(string? municipalitySlug, CancellationToken ct = default)
    {
        var slug = municipalitySlug?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(slug)) return null;

        (AngolaProvince Province, AngolaMunicipality Municipality)? found;
        try
        {
            found = await _angola.FindMunicipalityAsync(slug, ct);
        }
        catch (AngolaLocationsUnavailableException)
        {
            // Sem a API, ainda se aceita um município que já tenha registo na plataforma.
            var known = await _db.Neighborhoods
                .Where(n => n.MunicipalitySlug == slug)
                .OrderBy(n => n.Name == n.MunicipalityName ? 0 : 1)
                .ThenBy(n => n.Id)
                .FirstOrDefaultAsync(ct);
            if (known is not null) return known;
            throw;
        }

        if (found is null) return null;
        var (province, municipality) = found.Value;

        var existing = await _db.Neighborhoods
            .Where(n => n.MunicipalitySlug == municipality.Slug && n.Name == municipality.Name)
            .OrderBy(n => n.Id)
            .FirstOrDefaultAsync(ct);
        if (existing is not null) return existing;

        var area = new Neighborhood
        {
            Name = municipality.Name,
            MunicipalitySlug = municipality.Slug,
            MunicipalityName = municipality.Name,
            ProvinceSlug = province.Slug,
            ProvinceName = province.Name
        };
        _db.Neighborhoods.Add(area);
        await _db.SaveChangesAsync(ct);
        return area;
    }
}
