using System.Security.Claims;
using FluentValidation;
using KambaDiami.Api.Services;
using KambaDiami.Application.Abstractions;
using KambaDiami.Application.Dtos;
using KambaDiami.Application.Mapping;
using KambaDiami.Domain.Entities;
using KambaDiami.Domain.Enums;
using KambaDiami.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KambaDiami.Api.Controllers;

[ApiController]
[Route("api/job-postings")]
[Authorize]
public class JobPostingsController : ControllerBase
{
    private readonly KambaDiamiDbContext _db;
    private readonly IValidator<CreateJobPostingRequest> _validator;

    public JobPostingsController(KambaDiamiDbContext db, IValidator<CreateJobPostingRequest> validator)
    {
        _db = db;
        _validator = validator;
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string CurrentUserRole => User.FindFirstValue(ClaimTypes.Role)!;

    private IQueryable<JobPosting> BaseQuery() =>
        _db.JobPostings
            .AsNoTracking()
            .AsSplitQuery()
            .Include(j => j.Author)
            .Include(j => j.Categories).ThenInclude(c => c.ServiceCategory)
            .Include(j => j.Locations).ThenInclude(l => l.Neighborhood);

    [HttpGet]
    public async Task<ActionResult<PagedResult<JobPostingDto>>> GetFeed([FromQuery] JobPostingFilter filter)
    {
        var query = BaseQuery().Where(j => j.Status == JobPostingStatus.Ativo && j.Author.Status != AccountStatus.Suspenso);

        if (filter.Type.HasValue) query = query.Where(j => j.Type == filter.Type);
        if (filter.PaymentType.HasValue) query = query.Where(j => j.PaymentType == filter.PaymentType);
        if (filter.CategoryId.HasValue)
            query = query.Where(j => j.Categories.Any(c => c.ServiceCategoryId == filter.CategoryId));
        if (filter.NeighborhoodId.HasValue)
            query = query.Where(j => j.Locations.Any(l => l.NeighborhoodId == filter.NeighborhoodId));
        else if (!string.IsNullOrWhiteSpace(filter.MunicipalitySlug))
            query = query.Where(j => j.Locations.Any(l => l.Neighborhood.MunicipalitySlug == filter.MunicipalitySlug));
        else if (!string.IsNullOrWhiteSpace(filter.ProvinceSlug))
            query = query.Where(j => j.Locations.Any(l => l.Neighborhood.ProvinceSlug == filter.ProvinceSlug));

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            // O Postgres distingue maiúsculas de minúsculas, ao contrário do SQL Server.
            var term = filter.Search.Trim().ToLowerInvariant();
            query = query.Where(j =>
                (j.Comments != null && j.Comments.ToLower().Contains(term)) ||
                j.Categories.Any(c => c.ServiceCategory.Name.ToLower().Contains(term)) ||
                j.Author.FirstName.ToLower().Contains(term) ||
                j.Author.LastName.ToLower().Contains(term));
        }

        var page = Math.Max(filter.Page, 1);
        var pageSize = Math.Clamp(filter.PageSize, 1, 50);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(j => j.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtoItems = await MapWithRatingsAsync(items);

        return Ok(new PagedResult<JobPostingDto>
        {
            Items = dtoItems,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpGet("preview")]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResult<JobPostingDto>>> GetPreview(
        [FromQuery] JobPostingType type = JobPostingType.ProcuraEmprego,
        [FromQuery] int take = 3)
    {
        // Amostra para a página inicial: só os mais recentes, sem contar o total (evita uma consulta extra
        // em cada visita anónima e não expõe o volume de anúncios).
        take = Math.Clamp(take, 1, 6);

        var items = await BaseQuery()
            .Where(j => j.Status == JobPostingStatus.Ativo && j.Type == type && j.Author.Status != AccountStatus.Suspenso)
            .OrderByDescending(j => j.CreatedAt)
            .Take(take)
            .ToListAsync();

        return Ok(new PagedResult<JobPostingDto>
        {
            Items = await MapWithRatingsAsync(items),
            TotalCount = items.Count,
            Page = 1,
            PageSize = take
        });
    }

    [HttpGet("mine")]
    public async Task<ActionResult<List<JobPostingDto>>> GetMine()
    {
        var items = await BaseQuery()
            .Where(j => j.AuthorUserId == CurrentUserId && j.Status != JobPostingStatus.Removido)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync();

        return Ok(await MapWithRatingsAsync(items));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<JobPostingDto>> GetById(int id)
    {
        var posting = await BaseQuery().FirstOrDefaultAsync(j => j.Id == id);
        if (posting is null) return NotFound();

        var (avg, count) = await GetRatingStatsAsync(posting.AuthorUserId);
        return Ok(posting.ToDto(avg, count));
    }

    [HttpPost]
    [Authorize(Roles = "Familia,Secretaria")]
    public async Task<ActionResult<JobPostingDto>> Create(
        CreateJobPostingRequest request,
        [FromServices] MunicipalityAreaResolver areas)
    {
        var validation = await _validator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            return ValidationProblem(ModelState);
        }

        var categoriesExist = await _db.ServiceCategories.CountAsync(c => request.CategoryIds.Contains(c.Id));
        if (categoriesExist != request.CategoryIds.Distinct().Count())
            return BadRequest(new { message = "Uma ou mais categorias são inválidas." });

        var areaIds = new List<int>();
        foreach (var slug in request.MunicipalitySlugs.Select(s => s?.Trim().ToLowerInvariant()).Distinct())
        {
            Neighborhood? area;
            try
            {
                area = await areas.ResolveAsync(slug, HttpContext.RequestAborted);
            }
            catch (AngolaLocationsUnavailableException ex)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
            }
            if (area is null)
                return BadRequest(new { message = "Um ou mais municípios são inválidos." });
            if (!areaIds.Contains(area.Id)) areaIds.Add(area.Id);
        }

        var type = CurrentUserRole == UserRole.Familia.ToString()
            ? JobPostingType.OfertaEmprego
            : JobPostingType.ProcuraEmprego;

        var posting = new JobPosting
        {
            AuthorUserId = CurrentUserId,
            Type = type,
            PaymentType = request.PaymentType,
            PaymentValue = request.PaymentValue,
            HoursPerDay = request.HoursPerDay,
            Comments = request.Comments,
            Status = JobPostingStatus.Ativo,
            Categories = request.CategoryIds.Distinct()
                .Select(id => new JobPostingCategory { ServiceCategoryId = id })
                .ToList(),
            Locations = areaIds
                .Select(id => new JobPostingLocation { NeighborhoodId = id })
                .ToList()
        };

        _db.JobPostings.Add(posting);
        await _db.SaveChangesAsync();

        var created = await BaseQuery().FirstAsync(j => j.Id == posting.Id);
        var (avg, count) = await GetRatingStatsAsync(created.AuthorUserId);
        return CreatedAtAction(nameof(GetById), new { id = posting.Id }, created.ToDto(avg, count));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var posting = await _db.JobPostings.FirstOrDefaultAsync(j => j.Id == id);
        if (posting is null) return NotFound();
        if (posting.AuthorUserId != CurrentUserId) return Forbid();

        posting.Status = JobPostingStatus.Removido;
        posting.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private async Task<List<JobPostingDto>> MapWithRatingsAsync(List<JobPosting> postings)
    {
        var authorIds = postings.Select(p => p.AuthorUserId).Distinct().ToList();
        var stats = await _db.Ratings
            .Where(r => authorIds.Contains(r.RateeUserId))
            .GroupBy(r => r.RateeUserId)
            .Select(g => new { UserId = g.Key, Avg = g.Average(r => r.Score), Count = g.Count() })
            .ToListAsync();

        var statsMap = stats.ToDictionary(s => s.UserId, s => (s.Avg, s.Count));

        return postings
            .Select(p => statsMap.TryGetValue(p.AuthorUserId, out var s)
                ? p.ToDto(s.Avg, s.Count)
                : p.ToDto())
            .ToList();
    }

    private async Task<(double? avg, int count)> GetRatingStatsAsync(Guid userId)
    {
        var scores = await _db.Ratings.Where(r => r.RateeUserId == userId).Select(r => r.Score).ToListAsync();
        if (scores.Count == 0) return (null, 0);
        return (scores.Average(), scores.Count);
    }
}
