using PersonalHub.Web.Services.Chat;
using PersonalHub.Domain.Entidades;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace PersonalHub.Tests;

public class ChatAssistenteTestes
{
    [Fact]
    public void DesserializarInterpretacao_LeJsonEstruturadoSemExporCamposExtras()
    {
        const string json = """
            {"acao":"consulta","mensagem":"Vou consultar o mês.","consulta":{"inicio":"2026-11-01","fim":"2026-11-30","situacao":"todas","agruparPor":"nenhum"},"despesas":[],"thinking":"raciocínio interno"}
            """;

        ChatInterpretacao resultado = ChatOllamaClient.DesserializarInterpretacao(json);

        Assert.Equal("consulta", resultado.Acao);
        Assert.Equal("Vou consultar o mês.", resultado.Mensagem);
        Assert.Equal(new DateOnly(2026, 11, 1), resultado.Consulta.Inicio);
        Assert.Empty(resultado.Despesas);
    }

    [Fact]
    public void DesserializarInterpretacao_RemoveDespesaNulaENormalizaParticipantesNulos()
    {
        const string json = "{\"acao\":\"rascunho\",\"despesas\":[null,{\"titulo\":\"Compra\",\"participantes\":null}]}";

        ChatInterpretacao resultado = ChatOllamaClient.DesserializarInterpretacao(json);

        ChatDespesaExtraida despesa = Assert.Single(resultado.Despesas);
        Assert.Empty(despesa.Participantes);
    }

    [Fact]
    public void DesserializarInterpretacao_ArredondaValorMonetarioAosCentavos()
    {
        const string json = "{\"acao\":\"rascunho\",\"despesas\":[{\"titulo\":\"Compra\",\"valorParcela\":30.899999618530273}]}";

        ChatInterpretacao resultado = ChatOllamaClient.DesserializarInterpretacao(json);

        Assert.Equal(30.90m, Assert.Single(resultado.Despesas).ValorParcela);
    }

    [Fact]
    public void DesserializarInterpretacao_ExtraiPedidoDeEdicaoSemCriarIdentificadorDeDespesa()
    {
        const string json = """
            {"acao":"editar","edicao":{"textoBusca":"Conta de luz","titulo":"Energia elétrica","valorParcela":148.75,"vencimento":"2026-11-25","quantidadeParcelas":null,"tags":null,"participantes":null}}
            """;

        ChatInterpretacao resultado = ChatOllamaClient.DesserializarInterpretacao(json);

        Assert.Equal("editar", resultado.Acao);
        Assert.NotNull(resultado.Edicao);
        Assert.Equal("Conta de luz", resultado.Edicao.TextoBusca);
        Assert.Equal("Energia elétrica", resultado.Edicao.Titulo);
        Assert.Equal(148.75m, resultado.Edicao.ValorParcela);
        Assert.Equal(new DateOnly(2026, 11, 25), resultado.Edicao.Vencimento);
        Assert.Null(resultado.Edicao.Tags);
        Assert.Null(resultado.Edicao.Participantes);
    }

    [Fact]
    public void ParserDeEdicao_ExtraiAlvoEDescricaoDoPedidoDireto()
    {
        const string mensagem = "Altere a despesa ORCA TESTE LOTE 20261120 MERCADO. Mude a descrição para DESCRICAO ATUALIZADA PELO CHAT.";

        bool reconheceu = ChatEdicaoParser.TentarInterpretar(mensagem, out ChatEdicaoIntent intent);

        Assert.True(reconheceu);
        Assert.Equal("ORCA TESTE LOTE 20261120 MERCADO", intent.TextoBusca);
        Assert.Equal("DESCRICAO ATUALIZADA PELO CHAT", intent.Descricao);
        Assert.Null(intent.ValorParcela);
    }

    [Fact]
    public void ParserDeEdicao_ExtraiValorMonetarioEmFormatoBrasileiro()
    {
        const string mensagem = "Edite a despesa identificada como Conta de luz; defina o valor para R$ 1.234,56.";

        bool reconheceu = ChatEdicaoParser.TentarInterpretar(mensagem, out ChatEdicaoIntent intent);

        Assert.True(reconheceu);
        Assert.Equal("Conta de luz", intent.TextoBusca);
        Assert.Equal(1234.56m, intent.ValorParcela);
    }

    [Theory]
    [InlineData("Preciso que me informe o total de gastos do mes atual.")]
    [InlineData("Quanto gastei este mês?")]
    public void ParserDeConsulta_RoteiaTotalDoMesAtualParaConsultaDeterministica(string mensagem)
    {
        bool reconheceu = ChatConsultaParser.TentarInterpretarTotalMensal(mensagem, out ChatConsultaIntent consulta);
        DateOnly hoje = DateOnly.FromDateTime(DateTime.Today);

        Assert.True(reconheceu);
        Assert.Equal(new DateOnly(hoje.Year, hoje.Month, 1), consulta.Inicio);
        Assert.Equal(new DateOnly(hoje.Year, hoje.Month, DateTime.DaysInMonth(hoje.Year, hoje.Month)), consulta.Fim);
    }

    [Fact]
    public void ParserDeConsulta_InterpretaMesExplicitoEStatus()
    {
        bool reconheceu = ChatConsultaParser.TentarInterpretarTotalMensal(
            "Mostre o total das despesas pagas em novembro de 2026.",
            out ChatConsultaIntent consulta);

        Assert.True(reconheceu);
        Assert.Equal(new DateOnly(2026, 11, 1), consulta.Inicio);
        Assert.Equal(new DateOnly(2026, 11, 30), consulta.Fim);
        Assert.Equal("pagas", consulta.Situacao);
    }

    [Theory]
    [InlineData("Mostre as despesas em aberto do mês passado.", "abertas")]
    [InlineData("Liste as despesas a pagar deste mês.", "abertas")]
    [InlineData("Mostre as despesas pendentes deste mês.", "pendentes")]
    public void ParserDeConsulta_DistingueContasEmAbertoDePendentes(string mensagem, string situacaoEsperada)
    {
        bool reconheceu = ChatConsultaParser.TentarInterpretarTotalMensal(mensagem, out ChatConsultaIntent consulta);

        Assert.True(reconheceu);
        Assert.Equal(situacaoEsperada, consulta.Situacao);
    }

    [Theory]
    [InlineData("Registre uma despesa total de R$ 45,00 no mercado.")]
    [InlineData("Compare as despesas deste mês com o mês passado.")]
    public void ParserDeConsulta_NaoInterpretaCadastroNemComparacaoComoTotalMensal(string mensagem)
    {
        Assert.False(ChatConsultaParser.TentarInterpretarTotalMensal(mensagem, out _));
    }

    [Fact]
    public void DesserializarInterpretacao_MoveDataDoTituloParaVencimentoEPriorizaDescricaoTextual()
    {
        const string json = """
            {"acao":"rascunho","despesas":[
              {"titulo":"20/11/2026","descricao":"20/11/2026 - MERCADO BOM PRECO compra no débito","vencimento":null},
              {"titulo":"2026-11-21 - FARMACIA VIDA","descricao":"Compra no cartão","vencimento":null},
              {"titulo":"","descricao":"20/11/2026 — PADARIA CENTRAL","vencimento":"2026-11-20"}
            ]}
            """;

        ChatInterpretacao resultado = ChatOllamaClient.DesserializarInterpretacao(json);

        Assert.Equal(3, resultado.Despesas.Count);
        Assert.Equal("MERCADO BOM PRECO compra no débito", resultado.Despesas[0].Titulo);
        Assert.Equal(new DateOnly(2026, 11, 20), resultado.Despesas[0].Vencimento);
        Assert.Equal("FARMACIA VIDA", resultado.Despesas[1].Titulo);
        Assert.Equal(new DateOnly(2026, 11, 21), resultado.Despesas[1].Vencimento);
        Assert.Equal("PADARIA CENTRAL", resultado.Despesas[2].Titulo);
        Assert.Equal(new DateOnly(2026, 11, 20), resultado.Despesas[2].Vencimento);
    }

    [Fact]
    public void DesserializarInterpretacao_PedePartesMenoresQuandoHaMaisDeVinteDespesas()
    {
        string despesas = string.Join(',', Enumerable.Repeat("{\"titulo\":\"Compra\"}", 21));
        string json = $"{{\"acao\":\"rascunho\",\"despesas\":[{despesas}]}}";

        ChatInterpretacao resultado = ChatOllamaClient.DesserializarInterpretacao(json);

        Assert.Equal("esclarecimento", resultado.Acao);
        Assert.Contains("mais de 20", resultado.Mensagem);
        Assert.Empty(resultado.Despesas);
    }

    [Theory]
    [InlineData(10.00, "user-a", 3.34)]
    [InlineData(10.00, "user-b", 3.33)]
    [InlineData(10.00, "user-c", 3.33)]
    public void Rateio_AlocaCentavosRestantesEConservaTotal(decimal total, string usuarioId, decimal parteEsperada)
    {
        (string Id, string Nome)[] participantes =
        {
            ("user-c", "Caio"),
            ("user-a", "Ana"),
            ("user-b", "Bia")
        };

        List<ChatParteUsuario> partes = ChatRateio.Dividir(total, participantes);
        decimal parteUsuario = ChatRateio.ObterParteDoUsuario(total, participantes, usuarioId);

        Assert.Equal(3, partes.Count);
        Assert.Equal(total, partes.Sum(parte => parte.Valor));
        Assert.Equal(parteEsperada, parteUsuario);
    }

    [Fact]
    public void Rateio_SemParticipantes_NaoCriaPartesNemValorPessoal()
    {
        Assert.Empty(ChatRateio.Dividir(12.50m, Array.Empty<(string Id, string Nome)>()));
        Assert.Equal(0m, ChatRateio.ObterParteDoUsuario(12.50m, Array.Empty<(string Id, string Nome)>(), "user-a"));
    }

    [Fact]
    public void AssinaturaDaEdicao_MudaQuandoUmaParcelaEAlterada()
    {
        Despesa despesa = CriarDespesaTeste();
        string assinaturaOriginal = ChatEdicaoSnapshot.Assinar(despesa, Array.Empty<OcorrenciaDespesaRecorrente>());

        despesa.Parcelas.Single().MarcarComoPaga();

        string assinaturaAtual = ChatEdicaoSnapshot.Assinar(despesa, Array.Empty<OcorrenciaDespesaRecorrente>());
        Assert.NotEqual(assinaturaOriginal, assinaturaAtual);
    }

    [Fact]
    public void AssinaturaDaEdicao_MudaQuandoUmaOcorrenciaRecorrenteEAlterada()
    {
        Despesa despesa = CriarDespesaTeste();
        OcorrenciaDespesaRecorrente ocorrencia = new(despesa.Id, new DateTime(2026, 11, 1), new DateTime(2026, 11, 20), 25m);
        string assinaturaOriginal = ChatEdicaoSnapshot.Assinar(despesa, new[] { ocorrencia });

        ocorrencia.MarcarComoPaga();

        string assinaturaAtual = ChatEdicaoSnapshot.Assinar(despesa, new[] { ocorrencia });
        Assert.NotEqual(assinaturaOriginal, assinaturaAtual);
    }

    private static Despesa CriarDespesaTeste() => new(
        42,
        "Conta de teste",
        1,
        25m,
        "Descrição",
        new DateTime(2026, 11, 20),
        new List<string> { "user-a" },
        new List<Tag>());
}
