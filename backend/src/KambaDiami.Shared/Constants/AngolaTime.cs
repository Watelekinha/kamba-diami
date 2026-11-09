namespace KambaDiami.Shared.Constants;

/// <summary>
/// Angola usa WAT (UTC+1) todo o ano, sem horário de verão. As estatísticas por dia
/// são agrupadas nesta hora para que "hoje" corresponda ao dia do utilizador.
/// </summary>
public static class AngolaTime
{
    public const int UtcOffsetHours = 1;

    public static DateTime ToLocal(DateTime utc) => utc.AddHours(UtcOffsetHours);

    public static DateOnly Today => DateOnly.FromDateTime(ToLocal(DateTime.UtcNow));

    /// <summary>Instante UTC em que começa o dia indicado em hora de Angola.</summary>
    public static DateTime StartOfDayUtc(DateOnly day) =>
        day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddHours(-UtcOffsetHours);
}
