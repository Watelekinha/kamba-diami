namespace KambaDiami.Application.Abstractions;

public interface IContractPdfService
{
    byte[] Generate(Domain.Entities.Contract contract);
}
