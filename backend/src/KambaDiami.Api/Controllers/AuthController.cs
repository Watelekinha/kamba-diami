using System.Text;
using FluentValidation;
using KambaDiami.Api.Services;
using KambaDiami.Application.Abstractions;
using KambaDiami.Application.Dtos;
using KambaDiami.Application.Mapping;
using KambaDiami.Domain.Entities;
using KambaDiami.Domain.Enums;
using KambaDiami.Infrastructure.Persistence;
using KambaDiami.Shared.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace KambaDiami.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly KambaDiamiDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly IUserSessionService _sessions;
    private readonly PasswordResetSender _passwordReset;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        KambaDiamiDbContext db,
        ITokenService tokenService,
        IUserSessionService sessions,
        PasswordResetSender passwordReset,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _db = db;
        _tokenService = tokenService;
        _sessions = sessions;
        _passwordReset = passwordReset;
        _logger = logger;
    }

    [HttpPost("register")]
    [EnableRateLimiting(RateLimitPolicies.Register)]
    public async Task<ActionResult<AuthResponse>> Register(
        RegisterRequest request,
        [FromServices] IValidator<RegisterRequest> validator,
        [FromServices] MunicipalityAreaResolver areas,
        [FromServices] WelcomeEmailSender welcomeEmail)
    {
        request.Email = NormalizeEmail(request.Email);
        request.FirstName = request.FirstName?.Trim() ?? string.Empty;
        request.LastName = request.LastName?.Trim() ?? string.Empty;

        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return ValidationProblem(BuildModelState(validation));
        }

        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
        {
            return Conflict(new { message = "Já existe uma conta com este email." });
        }

        Neighborhood? area;
        try
        {
            area = await areas.ResolveAsync(request.MunicipalitySlug, HttpContext.RequestAborted);
        }
        catch (AngolaLocationsUnavailableException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }
        if (area is null)
        {
            return BadRequest(new { message = "Município inválido." });
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();

        var address = new Address
        {
            NeighborhoodId = area.Id,
            StreetDetail = string.IsNullOrWhiteSpace(request.StreetDetail) ? null : request.StreetDetail.Trim()
        };
        _db.Addresses.Add(address);
        await _db.SaveChangesAsync();

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            PhoneNumber = AngolaPhone.Normalize(request.PhoneNumber),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = request.Role,
            Status = AccountStatus.Ativo,
            AddressId = address.Id,
            LastLoginAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            await transaction.RollbackAsync();
            return IdentityProblem(result);
        }

        await _userManager.AddToRoleAsync(user, request.Role.ToString());

        if (request.Role == UserRole.Familia)
        {
            _db.FamiliaProfiles.Add(new FamiliaProfile { UserId = user.Id });
        }
        else
        {
            _db.SecretariaProfiles.Add(new SecretariaProfile { UserId = user.Id });
        }
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        // Em segundo plano: a resposta do registo não espera pelo email.
        welcomeEmail.Queue(user);

        await _sessions.TrackActivityAsync(user.Id);
        return Ok(await BuildAuthResponseAsync(user));
    }

    [HttpPost("login")]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var email = NormalizeEmail(request.Email);
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(request.Password))
        {
            return Unauthorized(new { message = "Email ou palavra-passe inválidos." });
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            await RecordLoginAsync(email, null, succeeded: false, "UtilizadorInexistente");
            return Unauthorized(new { message = "Email ou palavra-passe inválidos." });
        }

        var check = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (check.IsLockedOut)
        {
            await RecordLoginAsync(email, user.Id, succeeded: false, "ContaBloqueada");
            var minutes = MinutesUntil(user.LockoutEnd);
            return StatusCode(StatusCodes.Status423Locked, new
            {
                message = $"Por segurança, a conta foi bloqueada depois de várias tentativas falhadas. Tenta de novo dentro de {minutes} min ou recupera a palavra-passe."
            });
        }

        if (!check.Succeeded)
        {
            await RecordLoginAsync(email, user.Id, succeeded: false, "PalavraPasseErrada");
            var remaining = _userManager.Options.Lockout.MaxFailedAccessAttempts - await _userManager.GetAccessFailedCountAsync(user);
            var message = remaining is > 0 and <= 2
                ? $"Email ou palavra-passe inválidos. Restam {remaining} tentativa(s) antes de a conta ser bloqueada temporariamente."
                : "Email ou palavra-passe inválidos.";
            return Unauthorized(new { message });
        }

        if (user.Status == AccountStatus.Suspenso)
        {
            await RecordLoginAsync(email, user.Id, succeeded: false, "ContaSuspensa");
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "Esta conta está suspensa. Se achas que é um engano, contacta o suporte da Kamba Diami."
            });
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
        await RecordLoginAsync(email, user.Id, succeeded: true, null);
        await _sessions.TrackActivityAsync(user.Id);

        return Ok(await BuildAuthResponseAsync(user));
    }

    /// <summary>
    /// Responde sempre da mesma forma, exista ou não a conta, para não revelar que emails estão registados.
    /// </summary>
    [HttpPost("forgot-password")]
    [EnableRateLimiting(RateLimitPolicies.PasswordReset)]
    public async Task<IActionResult> ForgotPassword(
        ForgotPasswordRequest request,
        [FromServices] IValidator<ForgotPasswordRequest> validator)
    {
        request.Email = NormalizeEmail(request.Email);
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return ValidationProblem(BuildModelState(validation));
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is not null && user.Status != AccountStatus.Suspenso)
        {
            try
            {
                await _passwordReset.SendAsync(user, HttpContext.RequestAborted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao enviar o email de recuperação para {UserId}", user.Id);
            }
        }

        return Ok(new
        {
            message = "Se existir uma conta com esse email, vais receber uma ligação para definir uma nova palavra-passe. A ligação é válida durante 2 horas."
        });
    }

    [HttpPost("reset-password")]
    [EnableRateLimiting(RateLimitPolicies.PasswordReset)]
    public async Task<IActionResult> ResetPassword(
        ResetPasswordRequest request,
        [FromServices] IValidator<ResetPasswordRequest> validator)
    {
        request.Email = NormalizeEmail(request.Email);
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return ValidationProblem(BuildModelState(validation));
        }

        const string invalidLink = "A ligação de recuperação é inválida ou já expirou. Pede uma nova.";

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return BadRequest(new { message = invalidLink });
        }

        string token;
        try
        {
            token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token));
        }
        catch (FormatException)
        {
            return BadRequest(new { message = invalidLink });
        }

        var result = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);
        if (!result.Succeeded)
        {
            if (result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.InvalidToken)))
            {
                return BadRequest(new { message = invalidLink });
            }
            return IdentityProblem(result);
        }

        // Quem recupera a palavra-passe também desbloqueia a conta; as sessões antigas deixam de valer
        // porque o carimbo de segurança muda.
        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);
        _sessions.Invalidate(user.Id);

        return Ok(new { message = "Palavra-passe alterada. Já podes entrar com a nova palavra-passe." });
    }

    private async Task<AuthResponse> BuildAuthResponseAsync(ApplicationUser user)
    {
        var token = await _tokenService.GenerateTokenAsync(user);
        var fullUser = await _db.Users
            .AsNoTracking()
            .Include(u => u.Address).ThenInclude(a => a.Neighborhood)
            .FirstAsync(u => u.Id == user.Id);

        return new AuthResponse
        {
            Token = token.Token,
            ExpiresAt = token.ExpiresAt,
            User = fullUser.ToProfileDto()
        };
    }

    private async Task RecordLoginAsync(string email, Guid? userId, bool succeeded, string? failureReason)
    {
        var userAgent = Request.Headers.UserAgent.ToString();
        _db.LoginEvents.Add(new LoginEvent
        {
            Email = email.Length > 256 ? email[..256] : email,
            UserId = userId,
            Succeeded = succeeded,
            FailureReason = failureReason,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = userAgent.Length > 300 ? userAgent[..300] : userAgent,
            OccurredAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    private static string NormalizeEmail(string? email) => (email ?? string.Empty).Trim().ToLowerInvariant();

    private static int MinutesUntil(DateTimeOffset? end)
    {
        if (end is null) return 15;
        return Math.Max(1, (int)Math.Ceiling((end.Value - DateTimeOffset.UtcNow).TotalMinutes));
    }

    private ActionResult IdentityProblem(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(error.Code, error.Description);
        }
        return ValidationProblem(ModelState);
    }

    private static Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary BuildModelState(FluentValidation.Results.ValidationResult validation)
    {
        var modelState = new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary();
        foreach (var error in validation.Errors)
        {
            modelState.AddModelError(error.PropertyName, error.ErrorMessage);
        }
        return modelState;
    }
}
