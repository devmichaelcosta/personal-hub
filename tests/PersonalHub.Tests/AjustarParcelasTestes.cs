using PersonalHub.Domain.Entidades;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace PersonalHub.Tests;

public class AjustarParcelasTestes
{
    [Fact]
    public void AumentarQuantidadeCriaParcelasMensaisComNumeracaoEValor()
    {
        var vencimentoInicial = new DateTime(2026, 1, 10);
        var despesa = CriarDespesa(2, vencimentoInicial);

        var removidas = despesa.AjustarQuantidadeParcelas(4, 125m);

        Assert.Empty(removidas);
        Assert.Equal(4, despesa.QtdParcelas);
        Assert.Equal(new[] { 1, 2, 3, 4 }, despesa.Parcelas.OrderBy(p => p.Numero).Select(p => p.Numero));
        Assert.Equal(vencimentoInicial.AddMonths(2), despesa.Parcelas.Single(p => p.Numero == 3).Vencimento);
        Assert.Equal(vencimentoInicial.AddMonths(3), despesa.Parcelas.Single(p => p.Numero == 4).Vencimento);
        Assert.Equal(125m, despesa.Parcelas.Single(p => p.Numero == 4).Valor);
        Assert.Equal(450m, despesa.Valor);
    }

    [Fact]
    public void ReduzirQuantidadeRemoveSempreAsParcelasFinais()
    {
        var despesa = CriarDespesa(4, new DateTime(2026, 1, 10));
        var parcelaPaga = despesa.Parcelas.Single(p => p.Numero == 4);
        parcelaPaga.MarcarComoPaga();

        var removidas = despesa.AjustarQuantidadeParcelas(2, 100m);

        Assert.Equal(new[] { 3, 4 }, removidas.Select(p => p.Numero).OrderBy(numero => numero));
        Assert.Contains(removidas, p => p.Pago);
        Assert.Equal(2, despesa.QtdParcelas);
        Assert.Equal(new[] { 1, 2 }, despesa.Parcelas.Select(p => p.Numero).OrderBy(numero => numero));
    }

    [Fact]
    public void QuantidadePrecisaSerPositiva()
    {
        var despesa = CriarDespesa(1, DateTime.Today);

        Assert.Throws<ArgumentOutOfRangeException>(() => despesa.AjustarQuantidadeParcelas(0, 100m));
    }

    private static Despesa CriarDespesa(int quantidade, DateTime vencimento)
        => new(1, "Teste", quantidade, 100m, string.Empty, vencimento,
            new List<string> { "usuario" }, new List<Tag>());
}
