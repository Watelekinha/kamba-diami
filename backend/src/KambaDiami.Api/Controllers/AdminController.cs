using System.Security.Claims;
using FluentValidation;
using KambaDiami.Api.Services;
using KambaDiami.Application.Abstractions;
using KambaDiami.Application.Dtos;
using KambaDiami.Domain.Entities;
using KambaDiami.Domain.Enums;
using KambaDiami.Infrastructure.Persistence;
using KambaDiami.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KambaDiami.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly KambaDiamiDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserSessionService _sessions;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        KambaDiamiDbContext db,
        UserManager<ApplicationUser> userManager,
        IUserSessionService sessions,
        ILogger<AdminController> logger)
    {
        _db = db;
        _userManager = userManager;
        _sessions = sessions;
        _logger = logger;
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // =====================================================================
    // Visão geral
    // =====================================================================

    [HttpGet("overview")]
    public async Task<ActionResult<AdminOverviewDto>> GetOverview([FromQuery] int days = 30)
    {
        days = Math.Clamp(days, 1, 365);

        var today = AngolaTime.Today;
        var fromDay = today.AddDays(-(days - 1));
        var previousFromDay = fromDay.AddDays(-days);

        var fromUtc = AngolaTime.StartOfDayUtc(fromDay);
        var toUtc = AngolaTime.StartOfDayUtc(today.AddDays(1));
        var previousFromUtc = AngolaTime.StartOfDayUtc(previousFromDay);

        // Contagens diárias de cada indicador, cobrindo o período actual e o anterior.
        var registrations = await DailyCountsAsync(
            _db.Users.Where(u => u.Role != UserRole.Admin).Select(u => u.CreatedAt), previousFromUtc, toUtc);
        var logins = await DailyCountsAsync(
            _db.LoginEvents.Where(e => e.Succeeded && e.User!.Role != UserRole.Admin).Select(e => e.OccurredAt), previousFromUtc, toUtc);
        var failedLogins = await DailyCountsAsync(
            _db.LoginEvents.Where(e => !e.Succeeded).Select(e => e.OccurredAt), previousFromUtc, toUtc);
        var postings = await DailyCountsAsync(_db.JobPostings.Select(j => j.CreatedAt), previousFromUtc, toUtc);
        var matches = await DailyCountsAsync(_db.Matches.Select(m => m.CreatedAt), previousFromUtc, toUtc);
        var contracts = await DailyCountsAsync(_db.Contracts.Select(c => c.CreatedAt), previousFromUtc, toUtc);
        var messages = await DailyCountsAsync(_db.Messages.Select(m => m.SentAt), previousFromUtc, toUtc);

        // Logins e actividade medem o uso por famílias e secretárias; os administradores ficam de fora.
        var platformActivity = _db.UserDailyActivities.Where(a => a.User.Role != UserRole.Admin);

        var activeByDay = (await platformActivity
                .Where(a => a.Day >= previousFromDay && a.Day <= today)
                .GroupBy(a => a.Day)
                .Select(g => new { Day = g.Key, Count = g.Count() })
                .ToListAsync())
            .ToDictionary(r => r.Day, r => r.Count);

        var activeCurrent = await platformActivity
            .Where(a => a.Day >= fromDay && a.Day <= today)
            .Select(a => a.UserId).Distinct().CountAsync();
        var activePrevious = await platformActivity
            .Where(a => a.Day >= previousFromDay && a.Day < fromDay)
            .Select(a => a.UserId).Distinct().CountAsync();

        var series = new List<AdminDailyPointDto>(days);
        for (var day = fromDay; day <= today; day = day.AddDays(1))
        {
            series.Add(new AdminDailyPointDto
            {
                Date = day,
                Registrations = registrations.GetValueOrDefault(day),
                Logins = logins.GetValueOrDefault(day),
                ActiveUsers = activeByDay.GetValueOrDefault(day),
                Postings = postings.GetValueOrDefault(day),
                Matches = matches.GetValueOrDefault(day),
                Contracts = contracts.GetValueOrDefault(day),
                Messages = messages.GetValueOrDefault(day)
            });
        }

        AdminMetricDto Metric(string key, Dictionary<DateOnly, int> counts) =>
            BuildMetric(key, SumRange(counts, fromDay, today), SumRange(counts, previousFromDay, fromDay.AddDays(-1)));

        var metrics = new List<AdminMetricDto>
        {
            Metric("registrations", registrations),
            Metric("logins", logins),
            BuildMetric("activeUsers", activeCurrent, activePrevious),
            Metric("postings", postings),
            Metric("matches", matches),
            Metric("contracts", contracts),
            Metric("messages", messages),
            Metric("failedLogins", failedLogins)
        };

        var ratingScores = _db.Ratings.Select(r => (double?)r.Score);
        var totals = new AdminTotalsDto
        {
            Users = await _db.Users.CountAsync(u => u.Role != UserRole.Admin),
            Familias = await _db.Users.CountAsync(u => u.Role == UserRole.Familia),
            Secretarias = await _db.Users.CountAsync(u => u.Role == UserRole.Secretaria),
            Admins = await _db.Users.CountAsync(u => u.Role == UserRole.Admin),
            SuspendedUsers = await _db.Users.CountAsync(u => u.Status == AccountStatus.Suspenso),
            ActivePostings = await _db.JobPostings.CountAsync(j => j.Status == JobPostingStatus.Ativo),
            Postings = await _db.JobPostings.CountAsync(j => j.Status != JobPostingStatus.Removido),
            Matches = await _db.Matches.CountAsync(),
            Contracts = await _db.Contracts.CountAsync(),
            ActiveContracts = await _db.Contracts.CountAsync(c => c.Status == ContractStatus.Ativo),
            CompletedContracts = await _db.Contracts.CountAsync(c => c.Status == ContractStatus.Concluido),
            Messages = await _db.Messages.CountAsync(),
            Ratings = await _db.Ratings.CountAsync(),
            AverageRating = await ratingScores.AverageAsync(),
            ActiveUsersToday = activeByDay.GetValueOrDefault(today)
        };

        var funnel = new AdminFunnelDto
        {
            Postings = await _db.JobPostings.CountAsync(j => j.CreatedAt >= fromUtc && j.CreatedAt < toUtc),
            Matches = await _db.Matches.CountAsync(m => m.CreatedAt >= fromUtc && m.CreatedAt < toUtc),
            AcceptedMatches = await _db.Matches.CountAsync(m =>
                m.CreatedAt >= fromUtc && m.CreatedAt < toUtc
                && (m.Status == MatchStatus.Aceite || m.Status == MatchStatus.Encerrado)),
            Contracts = await _db.Contracts.CountAsync(c => c.CreatedAt >= fromUtc && c.CreatedAt < toUtc),
            SignedContracts = await _db.Contracts.CountAsync(c =>
                c.CreatedAt >= fromUtc && c.CreatedAt < toUtc
                && (c.Status == ContractStatus.Ativo || c.Status == ContractStatus.Concluido))
        };

        var contractsByStatus = (await _db.Contracts
                .GroupBy(c => c.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync())
            .OrderBy(r => r.Status)
            .Select(r => new AdminCountDto { Label = r.Status.ToString(), Count = r.Count })
            .ToList();

        var usersByProvince = await _db.Users
            .Where(u => u.Role != UserRole.Admin)
            .GroupBy(u => u.Address.Neighborhood.ProvinceName)
            .Select(g => new AdminCountDto { Label = g.Key, Count = g.Count() })
            .OrderByDescending(r => r.Count)
            .Take(8)
            .ToListAsync();

        var topCategories = await _db.JobPostingCategories
            .Where(jc => jc.JobPosting.Status != JobPostingStatus.Removido)
            .GroupBy(jc => jc.ServiceCategory.Name)
            .Select(g => new AdminCountDto { Label = g.Key, Count = g.Count() })
            .OrderByDescending(r => r.Count)
            .Take(8)
            .ToListAsync();

        return Ok(new AdminOverviewDto
        {
            PeriodDays = days,
            From = fromDay,
            To = today,
            GeneratedAt = DateTime.UtcNow,
            Totals = totals,
            Metrics = metrics,
            Series = series,
            Funnel = funnel,
            ContractsByStatus = contractsByStatus,
            UsersByProvince = usersByProvince,
            TopCategories = topCategories,
            RecentActivity = await RecentActivityAsync()
        });
    }

    private async Task<List<AdminActivityItemDto>> RecentActivityAsync()
    {
        const int take = 8;

        var users = await _db.Users.AsNoTracking()
            .Where(u => u.Role != UserRole.Admin)
            .OrderByDescending(u => u.CreatedAt).Take(take)
            .Select(u => new AdminActivityItemDto
            {
                Type = "registration",
                Description = u.Role == UserRole.Familia ? "Nova conta de família" : "Nova conta de secretária do lar",
                OccurredAt = u.CreatedAt,
                UserId = u.Id,
                UserName = u.FirstName + " " + u.LastName
            }).ToListAsync();

        var postings = await _db.JobPostings.AsNoTracking()
            .OrderByDescending(j => j.CreatedAt).Take(take)
            .Select(j => new AdminActivityItemDto
            {
                Type = "posting",
                Description = j.Type == JobPostingType.OfertaEmprego ? "Publicou uma vaga" : "Publicou disponibilidade",
                OccurredAt = j.CreatedAt,
                UserId = j.AuthorUserId,
                UserName = j.Author.FirstName + " " + j.Author.LastName
            }).ToListAsync();

        var matches = await _db.Matches.AsNoTracking()
            .OrderByDescending(m => m.CreatedAt).Take(take)
            .Select(m => new AdminActivityItemDto
            {
                Type = "match",
                Description = "Manifestou interesse num anúncio",
                OccurredAt = m.CreatedAt,
                UserId = m.InitiatorUserId,
                UserName = m.InitiatorUser.FirstName + " " + m.InitiatorUser.LastName
            }).ToListAsync();

        var contracts = await _db.Contracts.AsNoTracking()
            .OrderByDescending(c => c.CreatedAt).Take(take)
            .Select(c => new AdminActivityItemDto
            {
                Type = "contract",
                Description = "Contrato iniciado com " + c.SecretariaUser.FirstName + " " + c.SecretariaUser.LastName,
                OccurredAt = c.CreatedAt,
                UserId = c.FamiliaUserId,
                UserName = c.FamiliaUser.FirstName + " " + c.FamiliaUser.LastName
            }).ToListAsync();

        return users.Concat(postings).Concat(matches).Concat(contracts)
            .OrderByDescending(a => a.OccurredAt)
            .Take(15)
            .ToList();
    }

    // =====================================================================
    // Utilizadores
    // =====================================================================

    [HttpGet("users")]
    public async Task<ActionResult<PagedResult<AdminUserListItemDto>>> GetUsers([FromQuery] AdminUserFilter filter)
    {
        var query = _db.Users.AsNoTracking();

        if (filter.Role.HasValue) query = query.Where(u => u.Role == filter.Role);
        if (filter.Status.HasValue) query = query.Where(u => u.Status == filter.Status);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLowerInvariant();
            query = query.Where(u =>
                (u.FirstName + " " + u.LastName).ToLower().Contains(term)
                || u.Email!.ToLower().Contains(term)
                || u.PhoneNumber!.Contains(term));
        }

        query = filter.Sort switch
        {
            "name" => query.OrderBy(u => u.FirstName).ThenBy(u => u.LastName),
            "lastLogin" => query.OrderByDescending(u => u.LastLoginAt),
            "oldest" => query.OrderBy(u => u.CreatedAt),
            _ => query.OrderByDescending(u => u.CreatedAt)
        };

        var (page, pageSize) = Paging(filter.Page, filter.PageSize);
        var totalCount = await query.CountAsync();
        var items = await ProjectUsers(query.Skip((page - 1) * pageSize).Take(pageSize)).ToListAsync();

        return Ok(new PagedResult<AdminUserListItemDto> { Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize });
    }

    [HttpGet("users/{id:guid}")]
    public async Task<ActionResult<AdminUserDetailDto>> GetUser(Guid id)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound();

        var summary = await ProjectUsers(_db.Users.AsNoTracking().Where(u => u.Id == id)).FirstAsync();

        var since = DateTime.UtcNow.AddDays(-30);
        var sinceDay = AngolaTime.Today.AddDays(-29);
        var scores = await _db.Ratings.Where(r => r.RateeUserId == id).Select(r => r.Score).ToListAsync();

        var detail = new AdminUserDetailDto
        {
            Id = summary.Id,
            FullName = summary.FullName,
            Email = summary.Email,
            PhoneNumber = summary.PhoneNumber,
            PhotoPath = summary.PhotoPath,
            Role = summary.Role,
            Status = summary.Status,
            Location = summary.Location,
            CreatedAt = summary.CreatedAt,
            LastLoginAt = summary.LastLoginAt,
            IsLockedOut = summary.IsLockedOut,
            PostingsCount = summary.PostingsCount,
            ContractsCount = summary.ContractsCount,

            SuspendedAt = user.SuspendedAt,
            SuspensionReason = user.SuspensionReason,
            LockoutEnd = user.LockoutEnd,
            FailedLoginAttempts = user.AccessFailedCount,
            LoginsLast30Days = await _db.LoginEvents.CountAsync(e => e.UserId == id && e.Succeeded && e.OccurredAt >= since),
            ActiveDaysLast30Days = await _db.UserDailyActivities.CountAsync(a => a.UserId == id && a.Day >= sinceDay),
            MatchesCount = await _db.Matches.CountAsync(m => m.InitiatorUserId == id || m.CounterpartUserId == id),
            MessagesCount = await _db.Messages.CountAsync(m => m.SenderUserId == id),
            AverageRating = scores.Count > 0 ? scores.Average() : null,
            RatingsCount = scores.Count,

            RecentLogins = await ProjectLoginEvents(_db.LoginEvents.AsNoTracking()
                    .Where(e => e.UserId == id || e.Email == user.Email)
                    .OrderByDescending(e => e.OccurredAt)
                    .Take(10))
                .ToListAsync(),
            RecentPostings = await LoadPostingsAsync(_db.JobPostings.AsNoTracking()
                    .Where(j => j.AuthorUserId == id)
                    .OrderByDescending(j => j.CreatedAt)
                    .Take(10)),
            RecentContracts = await ProjectContracts(_db.Contracts.AsNoTracking()
                    .Where(c => c.FamiliaUserId == id || c.SecretariaUserId == id)
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(10))
                .ToListAsync()
        };

        return Ok(detail);
    }

    [HttpPut("users/{id:guid}/status")]
    public async Task<IActionResult> UpdateUserStatus(Guid id, UpdateUserStatusRequest request)
    {
        if (id == CurrentUserId)
            return BadRequest(new { message = "Não podes alterar o estado da tua própria conta." });
        if (request.Status is not (AccountStatus.Ativo or AccountStatus.Suspenso))
            return BadRequest(new { message = "Estado inválido." });

        var reason = request.Reason?.Trim();
        if (request.Status == AccountStatus.Suspenso && (string.IsNullOrEmpty(reason) || reason.Length < 5))
            return BadRequest(new { message = "Indica o motivo da suspensão (pelo menos 5 caracteres)." });
        if (reason is { Length: > 500 })
            return BadRequest(new { message = "O motivo não pode ter mais de 500 caracteres." });

        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null) return NotFound();
        if (user.Role == UserRole.Admin && request.Status == AccountStatus.Suspenso)
            return BadRequest(new { message = "Não é possível suspender contas de administrador." });
        if (user.Status == request.Status)
            return Ok(new { message = "A conta já estava nesse estado." });

        if (request.Status == AccountStatus.Suspenso)
        {
            user.Status = AccountStatus.Suspenso;
            user.SuspendedAt = DateTime.UtcNow;
            user.SuspensionReason = reason;
        }
        else
        {
            user.Status = AccountStatus.Ativo;
            user.SuspendedAt = null;
            user.SuspensionReason = null;
        }
        user.UpdatedAt = DateTime.UtcNow;

        await _userManager.UpdateAsync(user);
        if (request.Status == AccountStatus.Suspenso)
        {
            // Termina de imediato todas as sessões abertas da conta.
            await _userManager.UpdateSecurityStampAsync(user);
        }
        else
        {
            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);
        }
        _sessions.Invalidate(user.Id);

        await AuditAsync(
            request.Status == AccountStatus.Suspenso ? "SuspenderConta" : "ReactivarConta",
            "Utilizador", user.Id.ToString(),
            $"{user.FirstName} {user.LastName} ({user.Email}){(reason is null ? "" : ". Motivo: " + reason)}");

        return Ok(new
        {
            message = request.Status == AccountStatus.Suspenso
                ? "Conta suspensa. As sessões abertas foram terminadas."
                : "Conta reactivada."
        });
    }

    [HttpPost("users/{id:guid}/unlock")]
    public async Task<IActionResult> UnlockUser(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null) return NotFound();

        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);
        await AuditAsync("DesbloquearConta", "Utilizador", user.Id.ToString(), $"{user.FirstName} {user.LastName} ({user.Email})");

        return Ok(new { message = "Bloqueio por tentativas falhadas removido." });
    }

    [HttpPost("users/{id:guid}/password-reset")]
    public async Task<IActionResult> SendPasswordReset(Guid id, [FromServices] PasswordResetSender passwordReset)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null) return NotFound();
        if (user.Status == AccountStatus.Suspenso)
            return BadRequest(new { message = "Reactiva a conta antes de enviar a recuperação da palavra-passe." });

        try
        {
            await passwordReset.SendAsync(user, HttpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar recuperação de palavra-passe para {UserId}", user.Id);
            return StatusCode(StatusCodes.Status502BadGateway, new { message = "Não foi possível enviar o email. Verifica a configuração SMTP." });
        }

        await AuditAsync("EnviarRecuperacaoPalavraPasse", "Utilizador", user.Id.ToString(), user.Email);
        return Ok(new { message = $"Foi enviada uma ligação de recuperação para {user.Email}." });
    }

    [HttpPost("admins")]
    public async Task<ActionResult<AdminUserListItemDto>> CreateAdmin(
        CreateAdminRequest request,
        [FromServices] IValidator<CreateAdminRequest> validator)
    {
        request.Email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        request.FirstName = request.FirstName?.Trim() ?? string.Empty;
        request.LastName = request.LastName?.Trim() ?? string.Empty;

        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            return ValidationProblem(ModelState);
        }

        if (await _userManager.FindByEmailAsync(request.Email) is not null)
            return Conflict(new { message = "Já existe uma conta com este email." });

        var neighborhood = await _db.Neighborhoods
            .OrderBy(n => n.ProvinceSlug == "luanda" ? 0 : 1)
            .ThenBy(n => n.Id)
            .FirstAsync();

        await using var transaction = await _db.Database.BeginTransactionAsync();
        var address = new Address { NeighborhoodId = neighborhood.Id };
        _db.Addresses.Add(address);
        await _db.SaveChangesAsync();

        var admin = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true,
            PhoneNumber = AngolaPhone.Normalize(request.PhoneNumber),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = UserRole.Admin,
            Status = AccountStatus.Ativo,
            AddressId = address.Id
        };

        var result = await _userManager.CreateAsync(admin, request.Password);
        if (!result.Succeeded)
        {
            await transaction.RollbackAsync();
            foreach (var error in result.Errors)
                ModelState.AddModelError(error.Code, error.Description);
            return ValidationProblem(ModelState);
        }

        await _userManager.AddToRoleAsync(admin, UserRole.Admin.ToString());
        await transaction.CommitAsync();

        await AuditAsync("CriarAdministrador", "Utilizador", admin.Id.ToString(), $"{admin.FirstName} {admin.LastName} ({admin.Email})");

        var dto = await ProjectUsers(_db.Users.AsNoTracking().Where(u => u.Id == admin.Id)).FirstAsync();
        return Ok(dto);
    }

    // =====================================================================
    // Anúncios
    // =====================================================================

    [HttpGet("postings")]
    public async Task<ActionResult<PagedResult<AdminPostingListItemDto>>> GetPostings([FromQuery] AdminPostingFilter filter)
    {
        var query = _db.JobPostings.AsNoTracking();

        if (filter.Status.HasValue) query = query.Where(j => j.Status == filter.Status);
        if (filter.Type.HasValue) query = query.Where(j => j.Type == filter.Type);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLowerInvariant();
            query = query.Where(j =>
                (j.Comments != null && j.Comments.ToLower().Contains(term))
                || (j.Author.FirstName + " " + j.Author.LastName).ToLower().Contains(term)
                || j.Categories.Any(c => c.ServiceCategory.Name.ToLower().Contains(term)));
        }

        var (page, pageSize) = Paging(filter.Page, filter.PageSize);
        var totalCount = await query.CountAsync();
        var items = await LoadPostingsAsync(query.OrderByDescending(j => j.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize));

        return Ok(new PagedResult<AdminPostingListItemDto> { Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize });
    }

    [HttpPut("postings/{id:int}/status")]
    public async Task<IActionResult> UpdatePostingStatus(int id, UpdatePostingStatusRequest request)
    {
        var reason = request.Reason?.Trim();
        if (request.Status == JobPostingStatus.Removido && (string.IsNullOrEmpty(reason) || reason.Length < 5))
            return BadRequest(new { message = "Indica o motivo da remoção (pelo menos 5 caracteres)." });
        if (reason is { Length: > 500 })
            return BadRequest(new { message = "O motivo não pode ter mais de 500 caracteres." });

        var posting = await _db.JobPostings.Include(j => j.Author).FirstOrDefaultAsync(j => j.Id == id);
        if (posting is null) return NotFound();

        var previous = posting.Status;
        posting.Status = request.Status;
        posting.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await AuditAsync(
            request.Status == JobPostingStatus.Removido ? "RemoverAnuncio" : "AlterarEstadoAnuncio",
            "Anuncio", posting.Id.ToString(),
            $"{previous} para {request.Status}. Autor: {posting.Author.FirstName} {posting.Author.LastName}{(reason is null ? "" : ". Motivo: " + reason)}");

        return Ok(new { message = request.Status == JobPostingStatus.Removido ? "Anúncio removido do feed." : "Estado do anúncio actualizado." });
    }

    // =====================================================================
    // Contratos
    // =====================================================================

    [HttpGet("contracts")]
    public async Task<ActionResult<PagedResult<AdminContractListItemDto>>> GetContracts([FromQuery] AdminContractFilter filter)
    {
        var query = _db.Contracts.AsNoTracking();

        if (filter.Status.HasValue) query = query.Where(c => c.Status == filter.Status);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLowerInvariant();
            var digits = new string(term.Where(char.IsDigit).ToArray());
            var byId = int.TryParse(digits, out var contractId) ? contractId : -1;
            query = query.Where(c =>
                c.Id == byId
                || (c.FamiliaUser.FirstName + " " + c.FamiliaUser.LastName).ToLower().Contains(term)
                || (c.SecretariaUser.FirstName + " " + c.SecretariaUser.LastName).ToLower().Contains(term));
        }

        var (page, pageSize) = Paging(filter.Page, filter.PageSize);
        var totalCount = await query.CountAsync();
        var items = await ProjectContracts(query.OrderByDescending(c => c.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize))
            .ToListAsync();

        return Ok(new PagedResult<AdminContractListItemDto> { Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize });
    }

    // =====================================================================
    // Segurança e auditoria
    // =====================================================================

    [HttpGet("login-events")]
    public async Task<ActionResult<PagedResult<AdminLoginEventDto>>> GetLoginEvents([FromQuery] AdminLoginEventFilter filter)
    {
        var query = _db.LoginEvents.AsNoTracking();

        if (filter.Succeeded.HasValue) query = query.Where(e => e.Succeeded == filter.Succeeded);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLowerInvariant();
            query = query.Where(e => e.Email.ToLower().Contains(term) || (e.IpAddress != null && e.IpAddress.Contains(term)));
        }

        var (page, pageSize) = Paging(filter.Page, filter.PageSize);
        var totalCount = await query.CountAsync();
        var items = await ProjectLoginEvents(query.OrderByDescending(e => e.OccurredAt).Skip((page - 1) * pageSize).Take(pageSize))
            .ToListAsync();

        return Ok(new PagedResult<AdminLoginEventDto> { Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize });
    }

    [HttpGet("audit")]
    public async Task<ActionResult<PagedResult<AdminAuditLogDto>>> GetAuditLog([FromQuery] int page = 1, [FromQuery] int pageSize = 30)
    {
        (page, pageSize) = Paging(page, pageSize);
        var query = _db.AdminAuditLogs.AsNoTracking();
        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AdminAuditLogDto
            {
                Id = a.Id,
                AdminUserId = a.AdminUserId,
                AdminName = a.AdminUser.FirstName + " " + a.AdminUser.LastName,
                Action = a.Action,
                TargetType = a.TargetType,
                TargetId = a.TargetId,
                Details = a.Details,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        return Ok(new PagedResult<AdminAuditLogDto> { Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize });
    }

    // =====================================================================
    // Auxiliares
    // =====================================================================

    private async Task AuditAsync(string action, string targetType, string? targetId, string? details)
    {
        _db.AdminAuditLogs.Add(new AdminAuditLog
        {
            AdminUserId = CurrentUserId,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            Details = details is { Length: > 1000 } ? details[..1000] : details,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    private static async Task<Dictionary<DateOnly, int>> DailyCountsAsync(IQueryable<DateTime> timestamps, DateTime fromUtc, DateTime toUtc)
    {
        var rows = await timestamps
            .Where(t => t >= fromUtc && t < toUtc)
            .GroupBy(t => t.AddHours(AngolaTime.UtcOffsetHours).Date)
            .Select(g => new { Day = g.Key, Count = g.Count() })
            .ToListAsync();

        return rows.ToDictionary(r => DateOnly.FromDateTime(r.Day), r => r.Count);
    }

    private static int SumRange(Dictionary<DateOnly, int> counts, DateOnly from, DateOnly to) =>
        counts.Where(kv => kv.Key >= from && kv.Key <= to).Sum(kv => kv.Value);

    private static AdminMetricDto BuildMetric(string key, int current, int previous) => new()
    {
        Key = key,
        Current = current,
        Previous = previous,
        // Sem valor anterior não há percentagem que faça sentido; a interface mostra "novo".
        ChangePercent = previous == 0
            ? (current == 0 ? 0 : null)
            : Math.Round((current - previous) * 100.0 / previous, 1)
    };

    private static (int page, int pageSize) Paging(int page, int pageSize) =>
        (Math.Max(page, 1), Math.Clamp(pageSize, 1, 100));

    private IQueryable<AdminUserListItemDto> ProjectUsers(IQueryable<ApplicationUser> users)
    {
        var now = DateTimeOffset.UtcNow;
        return users.Select(u => new AdminUserListItemDto
        {
            Id = u.Id,
            FullName = u.FirstName + " " + u.LastName,
            Email = u.Email ?? string.Empty,
            PhoneNumber = u.PhoneNumber ?? string.Empty,
            PhotoPath = u.PhotoPath,
            Role = u.Role,
            Status = u.Status,
            Location = u.Address.Neighborhood.MunicipalityName + " (" + u.Address.Neighborhood.ProvinceName + ")",
            CreatedAt = u.CreatedAt,
            LastLoginAt = u.LastLoginAt,
            IsLockedOut = u.LockoutEnd != null && u.LockoutEnd > now,
            PostingsCount = _db.JobPostings.Count(j => j.AuthorUserId == u.Id && j.Status != JobPostingStatus.Removido),
            ContractsCount = _db.Contracts.Count(c => c.FamiliaUserId == u.Id || c.SecretariaUserId == u.Id)
        });
    }

    /// <summary>
    /// Carrega os anúncios projectados. As localidades repetidas são removidas em memória: um
    /// Distinct sobre um texto calculado dentro da subconsulta não é traduzível pelo EF Core.
    /// </summary>
    private static async Task<List<AdminPostingListItemDto>> LoadPostingsAsync(IQueryable<JobPosting> postings)
    {
        var items = await ProjectPostings(postings).ToListAsync();
        foreach (var item in items) item.Locations = item.Locations.Distinct().ToList();
        return items;
    }

    private static IQueryable<AdminPostingListItemDto> ProjectPostings(IQueryable<JobPosting> postings) =>
        postings.AsSplitQuery().Select(j => new AdminPostingListItemDto
        {
            Id = j.Id,
            Type = j.Type,
            Status = j.Status,
            PaymentType = j.PaymentType,
            PaymentValue = j.PaymentValue,
            Comments = j.Comments,
            AuthorId = j.AuthorUserId,
            AuthorName = j.Author.FirstName + " " + j.Author.LastName,
            Categories = j.Categories.Select(c => c.ServiceCategory.Name).ToList(),
            Locations = j.Locations.Select(l => l.Neighborhood.MunicipalityName + ", " + l.Neighborhood.ProvinceName).ToList(),
            MatchesCount = j.Matches.Count,
            CreatedAt = j.CreatedAt
        });

    private static IQueryable<AdminContractListItemDto> ProjectContracts(IQueryable<Contract> contracts) =>
        contracts.Select(c => new AdminContractListItemDto
        {
            Id = c.Id,
            Reference = "KD-" + c.Id.ToString(),
            Status = c.Status,
            PaymentType = c.PaymentType,
            PaymentValue = c.PaymentValue,
            FamiliaId = c.FamiliaUserId,
            FamiliaName = c.FamiliaUser.FirstName + " " + c.FamiliaUser.LastName,
            SecretariaId = c.SecretariaUserId,
            SecretariaName = c.SecretariaUser.FirstName + " " + c.SecretariaUser.LastName,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        });

    private static IQueryable<AdminLoginEventDto> ProjectLoginEvents(IQueryable<LoginEvent> events) =>
        events.Select(e => new AdminLoginEventDto
        {
            Id = e.Id,
            UserId = e.UserId,
            UserName = e.User != null ? e.User.FirstName + " " + e.User.LastName : null,
            Email = e.Email,
            Succeeded = e.Succeeded,
            FailureReason = e.FailureReason,
            IpAddress = e.IpAddress,
            UserAgent = e.UserAgent,
            OccurredAt = e.OccurredAt
        });
}
