using System.Text.RegularExpressions;
using KRT.Onboarding.Domain.Common;

namespace KRT.Onboarding.Domain.Contas;

/// <summary>
/// CPF como objeto de valor: válido por construção. Armazena apenas os 11 dígitos
/// (sem máscara) e valida os dígitos verificadores. Não há como existir um CPF inválido
/// em memória — qualquer tentativa lança <see cref="DomainException"/>.
/// </summary>
public sealed partial class Cpf : ValueObject
{
    public const int Tamanho = 11;

    /// <summary>Os 11 dígitos do CPF, sem máscara.</summary>
    public string Valor { get; }

    private Cpf(string valor) => Valor = valor;

    /// <summary>Cria um CPF a partir de uma entrada com ou sem máscara.</summary>
    public static Cpf Criar(string? entrada)
    {
        DomainException.Requer(!string.IsNullOrWhiteSpace(entrada), "CPF é obrigatório.");

        var digitos = SomenteDigitos().Replace(entrada!, string.Empty);

        DomainException.Requer(digitos.Length == Tamanho, "CPF deve conter 11 dígitos.");
        DomainException.Requer(!TodosDigitosIguais(digitos), "CPF inválido.");
        DomainException.Requer(DigitosVerificadoresValidos(digitos), "CPF inválido.");

        return new Cpf(digitos);
    }

    /// <summary>Representação com máscara: 000.000.000-00.</summary>
    public string Formatado =>
        $"{Valor[..3]}.{Valor.Substring(3, 3)}.{Valor.Substring(6, 3)}-{Valor.Substring(9, 2)}";

    public override string ToString() => Valor;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Valor;
    }

    private static bool TodosDigitosIguais(string digitos) =>
        digitos.All(c => c == digitos[0]);

    private static bool DigitosVerificadoresValidos(string digitos)
    {
        var primeiro = CalcularDigito(digitos, pesoInicial: 10);
        var segundo = CalcularDigito(digitos, pesoInicial: 11);
        return primeiro == digitos[9] - '0' && segundo == digitos[10] - '0';
    }

    private static int CalcularDigito(string digitos, int pesoInicial)
    {
        var soma = 0;
        var peso = pesoInicial;
        for (var i = 0; i < pesoInicial - 1; i++)
        {
            soma += (digitos[i] - '0') * peso--;
        }

        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }

    [GeneratedRegex(@"\D")]
    private static partial Regex SomenteDigitos();
}
