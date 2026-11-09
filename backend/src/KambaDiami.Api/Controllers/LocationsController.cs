using System.Text.RegularExpressions;
using KambaDiami.Api.Services;
using KambaDiami.Application.Abstractions;
using KambaDiami.Application.Dtos;
using KambaDiami.Domain.Entities;
using KambaDiami.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace KambaDiami.Api.Controllers;

/// <summary>
/// Províncias e municípios vêm da API pública de Angola (angolaprovinciasapi.ao), através de
/// <see cref="IAngolaLocationsProvider"/>. Os bairros são da plataforma: a API só tem comunas, e
/// essas aparecem como sugestão de bairro.
/// </summary>
[ApiController]
[Route("api/locations")]
public class LocationsController : ControllerBase
{
    private readonly KambaDiamiDbContext _db;
    private readonly IAngolaLocationsProvider _angola;

    public LocationsController(KambaDiamiDbContext db, IAngolaLocationsProvider angola)
    {
        _db = db;
        _angola = angola;
    }

    [HttpGet("provinces")]
    public async Task<ActionResult<List<ProvinceDto>>> GetProvinces(CancellationToken ct)
    {
        try
        {
            var provinces = await _angola.GetProvincesAsync(ct);
            return Ok(provinces.Select(p => new ProvinceDto { Slug = p.Slug, Name = p.Name }).ToList());
        }
        catch (AngolaLocationsUnavailableException ex)
        {
            return Unavailable(ex);
        }
    }

    [HttpGet("provinces/{provinceSlug}/municipalities")]
    public async Task<ActionResult<List<MunicipalityDto>>> GetMunicipalities(string provinceSlug, CancellationToken ct)
    {
        try
        {
            var province = (await _angola.GetProvincesAsync(ct))
                .FirstOrDefault(p => string.Equals(p.Slug, provinceSlug, StringComparison.OrdinalIgnoreCase));
            if (province is null) return NotFound(new { message = "Província não encontrada." });

            return Ok(province.Municipalities
                .Select(m => new MunicipalityDto { Slug = m.Slug, Name = m.Name, ProvinceSlug = province.Slug })
                .ToList());
        }
        catch (AngolaLocationsUnavailableException ex)
        {
            return Unavailable(ex);
        }
    }

    /// <summary>
    /// Os bairros já criados no município e, como sugestão, as comunas da API que ainda não têm bairro
    /// com o mesmo nome. Se a API estiver em baixo, devolve só os bairros da plataforma.
    /// </summary>
    [HttpGet("municipalities/{municipalitySlug}/neighborhoods")]
    public async Task<ActionResult<List<NeighborhoodDto>>> GetNeighborhoods(string municipalitySlug, CancellationToken ct)
    {
        var slug = municipalitySlug.Trim().ToLowerInvariant();
        var existing = await _db.Neighborhoods
            .AsNoTracking()
            .Where(n => n.MunicipalitySlug == slug)
            .Select(n => new NeighborhoodDto { Id = n.Id, Name = n.Name, MunicipalitySlug = n.MunicipalitySlug })
            .ToListAsync(ct);

        IReadOnlyList<AngolaCommune> communes = [];
        try
        {
            var found = await _angola.FindMunicipalityAsync(slug, ct);
            if (found is null && existing.Count == 0)
                return NotFound(new { message = "Município não encontrado." });
            communes = found?.Municipality.Communes ?? [];
        }
        catch (AngolaLocationsUnavailableException)
        {
            // Sem a API não há sugestões, mas os bairros já criados continuam a poder ser escolhidos.
        }

        var existingNames = new HashSet<string>(existing.Select(n => n.Name), StringComparer.CurrentCultureIgnoreCase);
        var suggestions = communes
            .Where(c => !existingNames.Contains(c.Name))
            .Select(c => new NeighborhoodDto { Id = null, Name = c.Name, MunicipalitySlug = slug, IsSuggestion = true });

        return Ok(existing.Concat(suggestions)
            .OrderBy(n => n.Name, StringComparer.CurrentCulture)
            .ToList());
    }

    [HttpPost("neighborhoods")]
    // Sem sessão: quem se está a registar também precisa de acrescentar o seu bairro.
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Locations)]
    public async Task<ActionResult<NeighborhoodDto>> CreateNeighborhood(CreateNeighborhoodRequest request, CancellationToken ct)
    {
        var name = NormalizeName(request.Name);
        if (name is null)
            return BadRequest(new { message = InvalidNameMessage("bairro") });

        (AngolaProvince Province, AngolaMunicipality Municipality)? found;
        try
        {
            found = await _angola.FindMunicipalityAsync(request.MunicipalitySlug?.Trim() ?? string.Empty, ct);
        }
        catch (AngolaLocationsUnavailableException ex)
        {
            return Unavailable(ex);
        }
        if (found is null)
            return BadRequest(new { message = "Município inválido." });

        var (province, municipality) = found.Value;

        var existing = await _db.Neighborhoods.FirstOrDefaultAsync(n =>
            n.MunicipalitySlug == municipality.Slug && n.Name.ToLower() == name.ToLower(), ct);
        if (existing is not null)
            return Ok(ToDto(existing));

        var neighborhood = new Neighborhood
        {
            Name = name,
            MunicipalitySlug = municipality.Slug,
            MunicipalityName = municipality.Name,
            ProvinceSlug = province.Slug,
            ProvinceName = province.Name
        };
        _db.Neighborhoods.Add(neighborhood);
        await _db.SaveChangesAsync(ct);

        return Ok(ToDto(neighborhood));
    }

    private static NeighborhoodDto ToDto(Neighborhood n) =>
        new() { Id = n.Id, Name = n.Name, MunicipalitySlug = n.MunicipalitySlug };

    private ObjectResult Unavailable(AngolaLocationsUnavailableException ex) =>
        StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });

    private static readonly Regex AllowedName = new(@"^[\p{L}\p{N}][\p{L}\p{N} .'-]*$", RegexOptions.Compiled);

    /// <summary>Junta espaços repetidos e rejeita nomes vazios, demasiado curtos ou longos, ou com símbolos.</summary>
    private static string? NormalizeName(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var name = Regex.Replace(raw.Trim(), @"\s+", " ");
        return name.Length is >= 2 and <= 80 && AllowedName.IsMatch(name) ? name : null;
    }

    private static string InvalidNameMessage(string what) =>
        $"Indica um nome de {what} válido, com 2 a 80 caracteres (letras, números, espaços, hífen ou apóstrofo).";
}
