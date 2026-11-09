namespace KambaDiami.Application.Abstractions;

/// <summary>
/// Valida, em cada pedido autenticado, se o token ainda corresponde a uma conta activa
/// e regista a actividade diária do utilizador.
/// </summary>
public interface IUserSessionService
{
    /// <summary>
    /// Falso quando a conta foi suspensa ou apagada, ou quando o carimbo de segurança mudou
    /// (palavra-passe alterada ou sessão revogada pelo administrador).
    /// </summary>
    Task<bool> IsSessionValidAsync(Guid userId, string? securityStamp, CancellationToken ct = default);

    Task TrackActivityAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Esquece o estado guardado em cache para que a próxima validação vá à base de dados.</summary>
    void Invalidate(Guid userId);
}
