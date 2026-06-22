namespace KRT.Onboarding.IntegrationTests;

/// <summary>
/// Gera CPFs válidos e únicos por chamada (dígitos verificadores corretos, pelo mesmo
/// algoritmo do domínio). Necessário porque os testes compartilham um Postgres e o CPF
/// tem índice único — cada teste precisa do seu próprio CPF.
/// </summary>
internal static class CpfFaker
{
    private static int _seq;

    public static string Novo()
    {
        var n = Interlocked.Increment(ref _seq);
        var nove = (100_000_000 + (n % 800_000_000)).ToString(); // 9 dígitos, sempre distintos

        var d1 = Digito(nove, pesoInicial: 10);
        var dez = nove + d1;
        var d2 = Digito(dez, pesoInicial: 11);

        return nove + d1 + d2;
    }

    private static int Digito(string digitos, int pesoInicial)
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
}
