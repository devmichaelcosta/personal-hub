using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using PersonalHub.Domain.Entidades;
using PersonalHub.Domain.Enums;
using PersonalHub.Infrastructure.Data;
using PersonalHub.Web.Models.ViewModels;
using PersonalHub.Web.Services.Chat;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace PersonalHub.Tests;

[Trait("Category", "ChatIntegration")]
public sealed class ChatAssistenteIntegracaoTestes
{
    private static readonly CancellationToken TestCancellationToken = CancellationToken.None;

    [ChatIntegrationFact]
    public async Task PedidoOriginalDeEdicaoEncontraDespesaPorTituloAtualEPreparaAlteracoes()
    {
        await using BancoTeste banco = await BancoTeste.CriarAsync();
        Usuario usuario = await banco.CriarUsuarioAsync("edicao-titulo-atual");
        Despesa despesa = new(
            0,
            "TESTE IA ORCA 20261120",
            1,
            250m,
            string.Empty,
            new DateTime(2026, 11, 20),
            new List<string> { usuario.Id },
            new List<Tag>());
        banco.Context.Despesas.Add(despesa);
        await banco.Context.SaveChangesAsync(TestCancellationToken);

        ChatAssistenteService service = new(
            banco.Context,
            new OllamaFake(new HttpRequestException("O parser determinístico não deve chamar o Ollama.")));
        ChatRespostaDto resposta = await service.EnviarAsync(
            usuario.Id,
            "Use o comando 'editar' mude o valor da despesa de titulo \"TESTE IA ORCA 20261120\" para 321,00€, e mude o título para Mercado",
            null,
            null,
            TestCancellationToken);

        Assert.True(resposta.Sucesso, resposta.Mensagem);
        Assert.Equal("edicao", resposta.Resposta?.Tipo);
        ChatEdicaoRascunho rascunho = Assert.Single(resposta.Resposta!.Edicoes!);
        Assert.Equal(despesa.Id, rascunho.DespesaId);
        Assert.Equal("Mercado", rascunho.Titulo);
        Assert.Equal(321.00m, rascunho.ValorParcela);
        Assert.Equal("TESTE IA ORCA 20261120", despesa.Titulo);
        Assert.Equal(250m, despesa.Valor);
    }

    [ChatIntegrationFact]
    public async Task ConsultaRetornaSomenteDespesasDoUsuarioECalculaRateioCompartilhado()
    {
        await using BancoTeste banco = await BancoTeste.CriarAsync();
        Usuario usuario = await banco.CriarUsuarioAsync("dono");
        Usuario outro = await banco.CriarUsuarioAsync("outro");

        banco.Context.Despesas.Add(new Despesa(
            0,
            "Compra compartilhada",
            1,
            101.01m,
            string.Empty,
            new DateTime(2026, 11, 10),
            new List<string> { usuario.Id, outro.Id },
            new List<Tag>()));
        banco.Context.Despesas.Add(new Despesa(
            0,
            "Despesa pessoal de outra pessoa",
            1,
            500m,
            string.Empty,
            new DateTime(2026, 11, 11),
            new List<string> { outro.Id },
            new List<Tag>()));
        await banco.Context.SaveChangesAsync(TestCancellationToken);

        ChatInterpretacao interpretacao = new()
        {
            Acao = "consulta",
            Consulta = new ChatConsultaIntent
            {
                Inicio = new DateOnly(2026, 11, 1),
                Fim = new DateOnly(2026, 11, 30)
            }
        };
        ChatAssistenteService service = new(banco.Context, new OllamaFake(interpretacao));

        ChatRespostaDto resposta = await service.EnviarAsync(
            usuario.Id,
            "Faça uma consulta financeira no período configurado.",
            null,
            null,
            TestCancellationToken);

        Assert.True(resposta.Sucesso);
        ChatPayloadConsulta consulta = Assert.IsType<ChatPayloadConsulta>(resposta.Resposta!.Consulta);
        ChatItemConsulta item = Assert.Single(consulta.Itens);
        Assert.Equal(101.01m, consulta.ValorTotal);
        Assert.Equal(101.01m, item.ValorTotal);
        Assert.Equal(2, item.Partes.Count);
        Assert.Equal(item.Partes.Single(parte => parte.Nome == $"{usuario.Nome} {usuario.SobreNome}").Valor, item.ParteUsuario);
        Assert.Equal(101.01m, item.Partes.Sum(parte => parte.Valor));
    }

    [ChatIntegrationFact]
    public async Task PerguntaOriginalDeTotalMensalConsultaBancoERetornaTotalEParteDoUsuario()
    {
        await using BancoTeste banco = await BancoTeste.CriarAsync();
        Usuario usuario = await banco.CriarUsuarioAsync("total-mensal-original");
        Usuario outro = await banco.CriarUsuarioAsync("total-mensal-compartilhado");
        DateTime primeiroDiaMesAtual = new(DateTime.Today.Year, DateTime.Today.Month, 1);
        DateTime vencimentoMesAtual = DateTime.Today;

        banco.Context.Despesas.AddRange(
            new Despesa(0, "Compra compartilhada do mês", 1, 101.01m, string.Empty, vencimentoMesAtual, new List<string> { usuario.Id, outro.Id }, new List<Tag>()),
            new Despesa(0, "Compra pessoal do mês", 1, 25m, string.Empty, primeiroDiaMesAtual, new List<string> { usuario.Id }, new List<Tag>()),
            new Despesa(0, "Compra do mês anterior", 1, 900m, string.Empty, primeiroDiaMesAtual.AddDays(-1), new List<string> { usuario.Id }, new List<Tag>()));
        await banco.Context.SaveChangesAsync(TestCancellationToken);

        ChatAssistenteService service = new(banco.Context, new OllamaFake(new HttpRequestException("A consulta determinística não deve chamar o Ollama.")));
        ChatRespostaDto resposta = await service.EnviarAsync(
            usuario.Id,
            "Preciso que me informe o total de gastos do mes atual.",
            null,
            null,
            TestCancellationToken);

        Assert.True(resposta.Sucesso);
        ChatPayloadConsulta consulta = Assert.IsType<ChatPayloadConsulta>(resposta.Resposta!.Consulta);
        Assert.Equal(2, consulta.Quantidade);
        Assert.Equal(126.01m, consulta.ValorTotal);
        Assert.Equal(consulta.Itens.Sum(item => item.ParteUsuario), consulta.ParteUsuario);
        Assert.Equal(2, consulta.Itens.Single(item => item.Titulo == "Compra compartilhada do mês").Partes.Count);
    }

    [ChatIntegrationFact]
    public async Task ConsultaEmAbertoIncluiDespesasVencidasNaoPagasEDesconsideraPagas()
    {
        await using BancoTeste banco = await BancoTeste.CriarAsync();
        Usuario usuario = await banco.CriarUsuarioAsync("em-aberto");
        DateTime inicioMesAnterior = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-1);
        Despesa vencida = new(0, "Conta vencida em aberto", 1, 35m, string.Empty, inicioMesAnterior, new List<string> { usuario.Id }, new List<Tag>());
        Despesa paga = new(0, "Conta paga", 1, 60m, string.Empty, inicioMesAnterior.AddDays(1), new List<string> { usuario.Id }, new List<Tag>());
        paga.Parcelas.Single().MarcarComoPaga();
        banco.Context.Despesas.AddRange(vencida, paga);
        await banco.Context.SaveChangesAsync(TestCancellationToken);

        ChatAssistenteService service = new(banco.Context, new OllamaFake(new HttpRequestException("A consulta determinística não deve chamar o Ollama.")));
        ChatRespostaDto resposta = await service.EnviarAsync(
            usuario.Id,
            "Mostre as despesas em aberto do mês passado.",
            null,
            null,
            TestCancellationToken);

        Assert.True(resposta.Sucesso);
        ChatPayloadConsulta consulta = Assert.IsType<ChatPayloadConsulta>(resposta.Resposta!.Consulta);
        ChatItemConsulta item = Assert.Single(consulta.Itens);
        Assert.Equal("Conta vencida em aberto", item.Titulo);
        Assert.Equal("Vencida", item.Situacao);
        Assert.Equal(35m, consulta.ValorTotal);
    }

    [ChatIntegrationFact]
    public async Task ConsultaAgrupaPorTagEComparaOsTotaisDosPeriodos()
    {
        await using BancoTeste banco = await BancoTeste.CriarAsync();
        Usuario usuario = await banco.CriarUsuarioAsync("relatorio-tag");
        Tag tag = new("Mercado");
        banco.Context.Tags.Add(tag);
        await banco.Context.SaveChangesAsync(TestCancellationToken);

        Despesa despesaAtual = new(0, "Compra atual", 1, 50m, string.Empty, new DateTime(2026, 11, 10), new List<string> { usuario.Id }, new List<Tag>());
        Despesa despesaComparada = new(0, "Compra anterior", 1, 30m, string.Empty, new DateTime(2026, 10, 10), new List<string> { usuario.Id }, new List<Tag>());
        banco.Context.Despesas.AddRange(despesaAtual, despesaComparada);
        await banco.Context.SaveChangesAsync(TestCancellationToken);
        banco.Context.TagsDepesas.AddRange(
            new TagDespesa { TagId = tag.Id, DespesaId = despesaAtual.Id, Tag = tag, Despesa = despesaAtual },
            new TagDespesa { TagId = tag.Id, DespesaId = despesaComparada.Id, Tag = tag, Despesa = despesaComparada });
        await banco.Context.SaveChangesAsync(TestCancellationToken);

        ChatInterpretacao interpretacao = new()
        {
            Acao = "consulta",
            Consulta = new ChatConsultaIntent
            {
                Inicio = new DateOnly(2026, 11, 1),
                Fim = new DateOnly(2026, 11, 30),
                InicioComparacao = new DateOnly(2026, 10, 1),
                FimComparacao = new DateOnly(2026, 10, 31),
                Tags = new List<string> { "Mercado" },
                AgruparPor = "tag"
            }
        };
        ChatAssistenteService service = new(banco.Context, new OllamaFake(interpretacao));

        ChatRespostaDto resposta = await service.EnviarAsync(
            usuario.Id,
            "Use os parâmetros de consulta do teste de relatório.",
            null,
            null,
            TestCancellationToken);

        Assert.True(resposta.Sucesso);
        ChatPayloadConsulta consulta = Assert.IsType<ChatPayloadConsulta>(resposta.Resposta!.Consulta);
        Assert.Equal(50m, consulta.ValorTotal);
        Assert.Equal("Mercado", Assert.Single(consulta.Grupos).Nome);
        ChatPayloadComparacao comparacao = Assert.IsType<ChatPayloadComparacao>(consulta.Comparacao);
        Assert.Equal(30m, comparacao.TotalComparado);
        Assert.Equal(20m, comparacao.Diferenca);
    }

    [ChatIntegrationFact]
    public async Task ConsultaIncluiOcorrenciaRecorrenteFuturaNoPeriodo()
    {
        await using BancoTeste banco = await BancoTeste.CriarAsync();
        Usuario usuario = await banco.CriarUsuarioAsync("recorrencia-futura");
        CadastroDespesaViewModel cadastro = new()
        {
            Titulo = "Internet mensal",
            Descritivo = string.Empty,
            QtdParcelas = 1,
            ValorParcela = 89m,
            Vencimento = new DateTime(2026, 1, 31),
            UsuariosEnvolvidos = new List<string> { usuario.Id },
            Tipo = TipoCadastroDespesa.RecorrenteMensal
        };
        banco.Context.Despesas.Add(Despesa.CriarRecorrente(cadastro));
        await banco.Context.SaveChangesAsync(TestCancellationToken);

        ChatInterpretacao interpretacao = new()
        {
            Acao = "consulta",
            Consulta = new ChatConsultaIntent
            {
                Inicio = new DateOnly(2026, 2, 1),
                Fim = new DateOnly(2026, 2, 28)
            }
        };
        ChatAssistenteService service = new(banco.Context, new OllamaFake(interpretacao));

        ChatRespostaDto resposta = await service.EnviarAsync(
            usuario.Id,
            "Use os parâmetros de consulta do teste de recorrência.",
            null,
            null,
            TestCancellationToken);

        Assert.True(resposta.Sucesso);
        ChatPayloadConsulta consulta = Assert.IsType<ChatPayloadConsulta>(resposta.Resposta!.Consulta);
        ChatItemConsulta item = Assert.Single(consulta.Itens);
        Assert.Equal("28/02/2026", item.Data);
        Assert.Equal(89m, consulta.ValorTotal);
        Assert.Single(await banco.Context.OcorrenciasDespesasRecorrentes.ToListAsync(TestCancellationToken));
    }

    [ChatIntegrationFact]
    public async Task ConfirmacaoEmLoteInvalidaNaoGravaNenhumaDespesa()
    {
        await using BancoTeste banco = await BancoTeste.CriarAsync();
        Usuario usuario = await banco.CriarUsuarioAsync("lote-invalido");
        ChatAssistenteService service = new(banco.Context, CriarOllamaComDoisRascunhos());
        ChatRespostaDto preparado = await service.EnviarAsync(
            usuario.Id,
            "Prepare dois lançamentos de teste.",
            null,
            null,
            TestCancellationToken);

        List<ChatConfirmacaoRascunho> confirmacoes = CriarConfirmacoes(usuario.Id);
        confirmacoes[1].ValorParcela = 0m;

        ChatRespostaDto resposta = await service.ConfirmarDespesasEmLoteAsync(
            usuario.Id,
            preparado.Resposta!.Id,
            confirmacoes,
            false,
            TestCancellationToken);

        Assert.False(resposta.Sucesso);
        Assert.Equal("lote_invalido", resposta.Erro);
        Assert.Empty(await banco.Context.Despesas.ToListAsync(TestCancellationToken));
    }

    [ChatIntegrationFact]
    public async Task ConfirmacaoEmLoteValidaGravaTodosOsItensEAtualizaRascunho()
    {
        await using BancoTeste banco = await BancoTeste.CriarAsync();
        Usuario usuario = await banco.CriarUsuarioAsync("lote-valido");
        ChatAssistenteService service = new(banco.Context, CriarOllamaComDoisRascunhos());
        ChatRespostaDto preparado = await service.EnviarAsync(
            usuario.Id,
            "Prepare dois lançamentos de teste.",
            null,
            null,
            TestCancellationToken);

        ChatRespostaDto resposta = await service.ConfirmarDespesasEmLoteAsync(
            usuario.Id,
            preparado.Resposta!.Id,
            CriarConfirmacoes(usuario.Id),
            false,
            TestCancellationToken);

        Assert.True(resposta.Sucesso, resposta.Mensagem);
        Assert.Equal(2, await banco.Context.Despesas.CountAsync(TestCancellationToken));
        Assert.All(resposta.Resposta!.Rascunhos!, rascunho => Assert.True(rascunho.Confirmado));
    }

    [ChatIntegrationFact]
    public async Task OutroUsuarioNaoLeHistoricoNemConfirmaRascunhoAlheio()
    {
        await using BancoTeste banco = await BancoTeste.CriarAsync();
        Usuario dono = await banco.CriarUsuarioAsync("historico-dono");
        Usuario outro = await banco.CriarUsuarioAsync("historico-outro");
        ChatAssistenteService service = new(banco.Context, CriarOllamaComDoisRascunhos());
        ChatRespostaDto preparado = await service.EnviarAsync(
            dono.Id,
            "Prepare dois lançamentos de teste.",
            null,
            null,
            TestCancellationToken);

        ChatCarregamentoDto historicoAlheio = await service.CarregarAsync(outro.Id, TestCancellationToken);
        ChatRespostaDto tentativa = await service.ConfirmarDespesasEmLoteAsync(
            outro.Id,
            preparado.Resposta!.Id,
            CriarConfirmacoes(outro.Id),
            false,
            TestCancellationToken);

        Assert.Empty(historicoAlheio.Mensagens);
        Assert.False(tentativa.Sucesso);
        Assert.Equal("rascunho_nao_encontrado", tentativa.Erro);
        Assert.Empty(await banco.Context.Despesas.ToListAsync(TestCancellationToken));
    }

    [ChatIntegrationFact]
    public async Task FalhaDoOllamaMantemMensagemENaoGravaDespesa()
    {
        await using BancoTeste banco = await BancoTeste.CriarAsync();
        Usuario usuario = await banco.CriarUsuarioAsync("ollama-indisponivel");
        ChatAssistenteService service = new(banco.Context, new OllamaFake(new HttpRequestException("Ollama indisponível.")));

        ChatRespostaDto resposta = await service.EnviarAsync(
            usuario.Id,
            "Quero lançar uma despesa de teste.",
            null,
            null,
            TestCancellationToken);
        ChatCarregamentoDto historico = await service.CarregarAsync(usuario.Id, TestCancellationToken);

        Assert.True(resposta.Sucesso);
        Assert.Equal("erro", resposta.Resposta!.Tipo);
        Assert.Contains(historico.Mensagens, mensagem => mensagem.Papel == "user" && mensagem.Conteudo.Contains("despesa de teste", StringComparison.Ordinal));
        Assert.Empty(await banco.Context.Despesas.ToListAsync(TestCancellationToken));
    }

    private static OllamaFake CriarOllamaComDoisRascunhos() => new(new ChatInterpretacao
    {
        Acao = "rascunho",
        Despesas = new List<ChatDespesaExtraida>
        {
            new() { Titulo = "Integração A", ValorParcela = 12.34m, Vencimento = new DateOnly(2026, 11, 25), QuantidadeParcelas = 1 },
            new() { Titulo = "Integração B", ValorParcela = 56.78m, Vencimento = new DateOnly(2026, 11, 26), QuantidadeParcelas = 1 }
        }
    });

    private static List<ChatConfirmacaoRascunho> CriarConfirmacoes(string usuarioId) => new()
    {
        new()
        {
            Indice = 0,
            Titulo = "Integração A",
            ValorParcela = 12.34m,
            Vencimento = new DateOnly(2026, 11, 25),
            QuantidadeParcelas = 1,
            UsuariosIds = new List<string> { usuarioId }
        },
        new()
        {
            Indice = 1,
            Titulo = "Integração B",
            ValorParcela = 56.78m,
            Vencimento = new DateOnly(2026, 11, 26),
            QuantidadeParcelas = 1,
            UsuariosIds = new List<string> { usuarioId }
        }
    };

    private sealed class OllamaFake : IChatOllamaClient
    {
        private readonly ChatInterpretacao? _interpretacao;
        private readonly Exception? _erro;

        public OllamaFake(ChatInterpretacao interpretacao) => _interpretacao = interpretacao;

        public OllamaFake(Exception erro) => _erro = erro;

        public Task<ChatInterpretacao> InterpretarAsync(
            string mensagem,
            IReadOnlyList<ChatTurn> historico,
            IReadOnlyList<string> tagsDisponiveis,
            IReadOnlyList<string> usuariosDisponiveis,
            byte[]? imagem,
            string? mimeType,
            CancellationToken cancellationToken)
        {
            if (_erro != null)
                throw _erro;
            return Task.FromResult(_interpretacao!);
        }
    }

    private sealed class BancoTeste : IAsyncDisposable
    {
        private readonly string _nomeBanco;
        private readonly string _connectionStringMaster;
        private bool _bancoCriado;

        private BancoTeste(string nomeBanco, string connectionStringMaster, PersonalHubContext context)
        {
            _nomeBanco = nomeBanco;
            _connectionStringMaster = connectionStringMaster;
            Context = context;
            _bancoCriado = true;
        }

        public PersonalHubContext Context { get; }

        public static async Task<BancoTeste> CriarAsync()
        {
            string? connectionString = Environment.GetEnvironmentVariable("CHAT_INTEGRATION_TEST_CONNECTION");
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("Defina CHAT_INTEGRATION_TEST_CONNECTION para executar a integração com SQL Server.");

            SqlConnectionStringBuilder origem = new(connectionString)
            {
                Encrypt = false,
                TrustServerCertificate = true
            };
            string nomeBanco = $"ChatAssistenteTest_{Guid.NewGuid():N}";
            SqlConnectionStringBuilder master = new(origem.ConnectionString) { InitialCatalog = "master" };
            await using (SqlConnection conexao = new(master.ConnectionString))
            {
                await conexao.OpenAsync(TestCancellationToken);
                await using SqlCommand comando = conexao.CreateCommand();
                comando.CommandText = $"CREATE DATABASE [{nomeBanco}]";
                await comando.ExecuteNonQueryAsync(TestCancellationToken);
            }

            SqlConnectionStringBuilder destino = new(origem.ConnectionString) { InitialCatalog = nomeBanco };
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:ContextConnection"] = destino.ConnectionString
                })
                .Build();
            PersonalHubContext context = new(configuration);
            BancoTeste banco = new(nomeBanco, master.ConnectionString, context);
            try
            {
                await context.Database.MigrateAsync(TestCancellationToken);
                return banco;
            }
            catch
            {
                await banco.DisposeAsync();
                throw;
            }
        }

        public async Task<Usuario> CriarUsuarioAsync(string identificador)
        {
            string sufixo = Guid.NewGuid().ToString("N");
            string nomeUsuario = $"chat-it-{identificador}-{sufixo}";
            string email = $"{nomeUsuario}@integration.test";
            Usuario usuario = new(nomeUsuario, "Chat", identificador, email)
            {
                NormalizedUserName = nomeUsuario.ToUpperInvariant(),
                NormalizedEmail = email.ToUpperInvariant(),
                EmailConfirmed = true
            };
            Context.Users.Add(usuario);
            await Context.SaveChangesAsync(TestCancellationToken);
            return usuario;
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            if (!_bancoCriado)
                return;

            await using SqlConnection conexao = new(_connectionStringMaster);
            await conexao.OpenAsync(TestCancellationToken);
            await using SqlCommand comando = conexao.CreateCommand();
            comando.CommandText = $"ALTER DATABASE [{_nomeBanco}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{_nomeBanco}]";
            await comando.ExecuteNonQueryAsync(TestCancellationToken);
            _bancoCriado = false;
        }
    }
}

public sealed class ChatIntegrationFactAttribute : FactAttribute
{
    public ChatIntegrationFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CHAT_INTEGRATION_TEST_CONNECTION")))
            Skip = "Defina CHAT_INTEGRATION_TEST_CONNECTION para executar a integração com SQL Server.";
    }
}
