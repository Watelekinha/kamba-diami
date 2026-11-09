using System.Globalization;
using KambaDiami.Domain.Entities;
using KambaDiami.Domain.Enums;

namespace KambaDiami.Application.Contracts;

public static class DefaultContractClauses
{
    public static List<ContractClause> Build(ApplicationUser familia, ApplicationUser secretaria, JobPosting posting)
    {
        var categories = posting.Categories.Count > 0
            ? string.Join(", ", posting.Categories.Select(c => c.ServiceCategory.Name))
            : "serviço doméstico";

        var locations = posting.Locations.Count > 0
            ? string.Join("; ", posting.Locations.Select(l =>
                $"{l.Neighborhood.Name}, {l.Neighborhood.Municipality.Name}, {l.Neighborhood.Municipality.Province.Name}"))
            : "a combinar pelas partes";

        var paymentLabel = posting.PaymentType == PaymentType.Mensal ? "por mês" : "por dia";
        var paymentValue = posting.PaymentValue.ToString("N0", CultureInfo.GetCultureInfo("pt-PT"));
        var hours = posting.HoursPerDay.HasValue
            ? $"{posting.HoursPerDay.Value:0.##} horas por dia, em horário a combinar pelas partes."
            : "O horário de trabalho será combinado pelas partes, com respeito ao descanso diário previsto na lei.";

        var familiaMorada = FormatAddress(familia);
        var secretariaMorada = FormatAddress(secretaria);

        return
        [
            new ContractClause
            {
                SortOrder = 1,
                Title = "Identificação das partes",
                Body =
                    $"São partes neste contrato, na qualidade de entidade empregadora, {FullName(familia)}, " +
                    $"com o email {familia.Email} e o telefone {familia.PhoneNumber}, residente em {familiaMorada}; " +
                    $"e, na qualidade de trabalhadora de serviço doméstico, {FullName(secretaria)}, " +
                    $"com o email {secretaria.Email} e o telefone {secretaria.PhoneNumber}, residente em {secretariaMorada}."
            },
            new ContractClause
            {
                SortOrder = 2,
                Title = "Objecto",
                Body =
                    $"O presente contrato tem por objecto a prestação de trabalho doméstico nas áreas de {categories}, " +
                    "nos termos do Regime Jurídico do Trabalho Doméstico e de Protecção Social do Trabalhador de Serviço Doméstico " +
                    "(Decreto Presidencial n.º 155/16, de 9 de Agosto)."
            },
            new ContractClause
            {
                SortOrder = 3,
                Title = "Local de trabalho",
                Body = $"O trabalho será prestado nas seguintes localidades: {locations}."
            },
            new ContractClause
            {
                SortOrder = 4,
                Title = "Remuneração",
                Body =
                    $"A trabalhadora receberá a quantia de {paymentValue} Kzs {paymentLabel}, " +
                    "paga, por regra, em dinheiro, não podendo a parte paga em espécie exceder 20% do salário (artigo 14.º), " +
                    "e a entidade empregadora entregará um recibo assinado pela trabalhadora que comprove cada pagamento (artigo 15.º)."
            },
            new ContractClause
            {
                SortOrder = 5,
                Title = "Horário e descanso",
                Body =
                    hours +
                    " É garantido o direito a pelo menos 1 hora de intervalo por dia para descanso e refeições (artigo 20.º)."
            },
            new ContractClause
            {
                SortOrder = 6,
                Title = "Direitos e deveres",
                Body =
                    "As partes obrigam-se a cumprir a lei angolana aplicável ao trabalho doméstico, incluindo a idade mínima de 18 anos (artigo 10.º), " +
                    "o respeito pela dignidade da trabalhadora (artigos 4.º e 5.º), a inscrição de ambas na Protecção Social Obrigatória " +
                    "no prazo de 30 dias úteis após o início da actividade (artigo 45.º), com contribuições de 6% a cargo da entidade empregadora " +
                    "e de 2% a cargo da trabalhadora (artigo 48.º), e os deveres de lealdade, assiduidade e boa-fé (artigo 6.º)."
            },
            new ContractClause
            {
                SortOrder = 7,
                Title = "Duração",
                Body =
                    "O contrato produz efeitos a partir da data de assinatura de ambas as partes, " +
                    "salvo se as partes indicarem outra data nesta cláusula. A cessação far-se-á nos termos da lei."
            },
            new ContractClause
            {
                SortOrder = 8,
                Title = "Lei aplicável e foro",
                Body =
                    "O presente contrato rege-se pela lei da República de Angola. " +
                    "Qualquer litígio será obrigatoriamente submetido a tentativa de conciliação ou mediação (artigo 40.º) " +
                    "e, não havendo acordo, à sala do trabalho do tribunal provincial competente (artigo 41.º)."
            }
        ];
    }

    private static string FullName(ApplicationUser user) => $"{user.FirstName} {user.LastName}";

    private static string FormatAddress(ApplicationUser user)
    {
        var address = user.Address;
        var neighborhood = address?.Neighborhood;
        if (address is null || neighborhood is null) return "morada registada na plataforma";
        var street = string.IsNullOrWhiteSpace(address.StreetDetail) ? "" : $", {address.StreetDetail}";
        return $"{neighborhood.Name}, {neighborhood.Municipality.Name}, {neighborhood.Municipality.Province.Name}{street}";
    }
}
