using System.Globalization;
using KambaDiami.Application.Abstractions;
using KambaDiami.Domain.Entities;
using KambaDiami.Domain.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace KambaDiami.Infrastructure.Pdf;

public class ContractPdfService : IContractPdfService
{
    private static readonly Color Clay = Color.FromHex("#C06C4D");
    private static readonly Color ClayDeep = Color.FromHex("#9A5239");
    private static readonly Color Ink = Color.FromHex("#3A322C");
    private static readonly Color Cream = Color.FromHex("#FBF7F2");
    private static readonly Color Paper = Color.FromHex("#F3EBE3");
    private static readonly Color Muted = Color.FromHex("#7A6E64");
    private static readonly CultureInfo Pt = CultureInfo.GetCultureInfo("pt-PT");

    public byte[] Generate(Contract contract)
    {
        var clauses = contract.Clauses.OrderBy(c => c.SortOrder).ThenBy(c => c.Id).ToList();
        var number = $"KD-{contract.Id:D6}";

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(0);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10.5f).FontColor(Ink).FontFamily("Times New Roman").LineHeight(1.35f));

                page.Header().Element(c => ComposeHeader(c, number));
                page.Content().PaddingHorizontal(48).PaddingVertical(22).Element(c => ComposeBody(c, contract, clauses, number));
                page.Footer().PaddingHorizontal(48).PaddingBottom(22).Element(c => ComposeFooter(c, number));
            });
        }).GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, string number)
    {
        container.Background(Clay).PaddingHorizontal(48).PaddingVertical(18).Row(row =>
        {
            row.ConstantItem(42).Height(42).Background(Colors.White).AlignCenter().AlignMiddle()
                .Text("KD").FontColor(Clay).Bold().FontSize(13).FontFamily("Times New Roman");

            row.RelativeItem().PaddingLeft(14).Column(col =>
            {
                col.Item().Text("Kamba Diami").FontSize(22).FontColor(Colors.White).FontFamily("Times New Roman").SemiBold();
                col.Item().Text("O seu lar em boas mãos.").FontSize(10).FontColor(Color.FromHex("#F3EBE3")).Italic();
            });

            row.AutoItem().AlignRight().AlignMiddle().Column(col =>
            {
                col.Item().AlignRight().Text("CONTRATO DE TRABALHO DOMÉSTICO").FontSize(8).FontColor(Colors.White).Bold().LetterSpacing(0.6f);
                col.Item().AlignRight().Text(number).FontSize(11).FontColor(Colors.White).SemiBold();
            });
        });
    }

    private static void ComposeBody(IContainer container, Contract contract, IReadOnlyList<ContractClause> clauses, string number)
    {
        container.Column(column =>
        {
            column.Spacing(14);

            column.Item().Text("Contrato de trabalho doméstico")
                .FontSize(18).SemiBold().FontColor(Ink).FontFamily("Times New Roman");

            column.Item().Text(text =>
            {
                text.Span("Celebrado na plataforma ").FontColor(Muted);
                text.Span("Kamba Diami").SemiBold().FontColor(Clay);
                text.Span($", n.º {number}, ao abrigo do Decreto Presidencial n.º 155/16, de 9 de Agosto.").FontColor(Muted);
            });

            column.Item().Row(row =>
            {
                row.RelativeItem().Element(c => PartyCard(c, "Entidade empregadora (Família)", contract.FamiliaUser));
                row.ConstantItem(16);
                row.RelativeItem().Element(c => PartyCard(c, "Trabalhadora (Secretária do lar)", contract.SecretariaUser));
            });

            column.Item().Background(Paper).Padding(12).Text(text =>
            {
                var paymentLabel = contract.PaymentType == PaymentType.Mensal ? "por mês" : "por dia";
                text.Span("Remuneração combinada: ").SemiBold();
                text.Span($"{contract.PaymentValue.ToString("N0", Pt)} Kzs {paymentLabel}.");
            });

            var index = 1;
            foreach (var clause in clauses)
            {
                var ordinal = $"{index}.ª";
                column.Item().Element(c =>
                {
                    c.BorderLeft(3).BorderColor(Clay).PaddingLeft(12).Column(inner =>
                    {
                        inner.Item().Text($"Cláusula {ordinal}  {clause.Title}")
                            .FontSize(12).SemiBold().FontColor(ClayDeep);
                        inner.Item().PaddingTop(4).Text(clause.Body).FontSize(10.5f).FontColor(Ink);
                    });
                });
                index++;
            }

            column.Item().PaddingTop(18).Text("Assinaturas").FontSize(13).SemiBold().FontColor(Ink);
            column.Item().Text("As partes declaram ter lido e aceite as cláusulas deste contrato, gerado na aplicação Kamba Diami. " +
                               "A validade depende da assinatura manuscrita de ambas as partes e do posterior carregamento do PDF assinado na plataforma.")
                .FontColor(Muted).FontSize(9.5f);

            column.Item().PaddingTop(28).Row(row =>
            {
                row.RelativeItem().Element(c => SignatureBlock(c, "A entidade empregadora", FullName(contract.FamiliaUser)));
                row.ConstantItem(28);
                row.RelativeItem().Element(c => SignatureBlock(c, "A trabalhadora", FullName(contract.SecretariaUser)));
            });
        });
    }

    private static void PartyCard(IContainer container, string title, ApplicationUser user)
    {
        container.Background(Cream).Border(1).BorderColor(Paper).Padding(12).Column(col =>
        {
            col.Item().Text(title).FontSize(8).Bold().FontColor(Clay).LetterSpacing(0.4f);
            col.Item().PaddingTop(4).Text(FullName(user)).FontSize(12).SemiBold();
            col.Item().Text(user.Email ?? "email não indicado").FontSize(9).FontColor(Muted);
            col.Item().Text(user.PhoneNumber ?? "telefone não indicado").FontSize(9).FontColor(Muted);
            col.Item().PaddingTop(4).Text(FormatAddress(user)).FontSize(9).FontColor(Ink);
        });
    }

    private static void SignatureBlock(IContainer container, string role, string name)
    {
        container.Column(col =>
        {
            col.Item().BorderBottom(1).BorderColor(Clay).Height(42);
            col.Item().PaddingTop(8).Text(role).FontSize(8).FontColor(Muted);
            col.Item().Text(name).SemiBold().FontSize(10);
            col.Item().PaddingTop(10).Text("Data: ____ / ____ / ________").FontSize(9).FontColor(Muted);
        });
    }

    private static void ComposeFooter(IContainer container, string number)
    {
        container.BorderTop(1).BorderColor(Paper).PaddingTop(10).Row(row =>
        {
            row.RelativeItem().Text(text =>
            {
                text.Span("Kamba Diami  ·  ").FontColor(Clay).SemiBold().FontSize(8);
                text.Span("Contrato gerado na plataforma  ·  ").FontColor(Muted).FontSize(8);
                text.Span(number).FontColor(Ink).FontSize(8);
            });
            row.AutoItem().AlignRight().DefaultTextStyle(x => x.FontSize(8).FontColor(Muted)).Text(text =>
            {
                text.Span("Página ");
                text.CurrentPageNumber();
                text.Span(" de ");
                text.TotalPages();
            });
        });
    }

    private static string FullName(ApplicationUser user) => $"{user.FirstName} {user.LastName}";

    private static string FormatAddress(ApplicationUser user)
    {
        var address = user.Address;
        var neighborhood = address?.Neighborhood;
        if (address is null || neighborhood is null) return "Morada registada na plataforma Kamba Diami";
        var street = string.IsNullOrWhiteSpace(address.StreetDetail) ? "" : $", {address.StreetDetail}";
        return $"{neighborhood.MunicipalityName}, {neighborhood.ProvinceName}{street}";
    }
}
