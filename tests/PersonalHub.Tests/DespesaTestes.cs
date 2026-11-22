using PersonalHub.Domain.Entidades;
using PersonalHub.Web.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace PersonalHub.Tests
{
    public class DespesaTestes
    {
        [Theory(DisplayName = "Nova Despesa Válida")]
        [Trait("Categoria", "Despesas")]
        [InlineData("Conta 1", "", 1, 100, new string[] { "usuario-1", "usuario-2" },  100)]
        [InlineData("Conta 2", "", 6, 16.6, new string[] { "usuario-1", "usuario-2" }, 100)]
        [InlineData("Conta 3", "", 10, 163.49, new string[] { "usuario-1", "usuario-2" },  1634.90)]
        public void Despesa_NovaDespesa_DeveEstarValida(string titulo, string descritivo, int qtdParcelas,
                                                        decimal valorParcela, 
                                                        string[] usuarios, 
                                                        decimal valorParcelaEsperado)
        {
            var cadastroDespesa = new CadastroDespesaViewModel(
                titulo, 
                descritivo, 
                valorParcela, 
                qtdParcelas, 
                DateTime.Now, 
                usuarios.ToList()
            );

            Despesa despesa = Despesa.Criar(cadastroDespesa);

            // verificando numero de parcelas...
            int numeroParcelasObtido = despesa.Parcelas.Count;
            Assert.Equal(qtdParcelas, numeroParcelasObtido);

            // verificando valores total...
            var valoresEsperados = new List<decimal>
            {
                valorParcelaEsperado
            };

            var valores = new List<decimal>
            {
                despesa.Valor.GetValueOrDefault(),
                decimal.Ceiling(despesa.Valor.GetValueOrDefault())
            };

            Assert.Contains(valores, e => valoresEsperados.Contains(e));
        }

        [Theory(DisplayName = "Nova Despesa Inválida")]
        [Trait("Categoria", "Despesas")]
        [InlineData(0, "Conta RGE", 1, -100, new string[] { "usuario-1", "usuario-2" }, "Lorem ipsum dolor")]
        [InlineData(0, "Conta RGE", 0, 0, new string[] { "usuario-1", "usuario-2" }, "Lorem ipsum dolor")]
        [InlineData(0, "Parcela UCS 2/2022", 6, 0, new string[] { "usuario-1", "usuario-2" }, "Lorem ipsum dolor")]
        public void Despesa_NovaDespesa_DeveEstarInvalida(int despesaId, string titulo, int qtdParcelas, decimal valor, string[] usuarios, string descritivo)
        {
            Assert.Throws<ArgumentException>(
                () => {
                    var contaLuz = new Despesa(despesaId,
                        titulo,
                        qtdParcelas,
                        valor,
                        descritivo,
                        DateTime.Now,
                        usuarios.ToList(),
                        new List<Tag>()
                    );
                }
            );
        }
        [Fact(DisplayName = "Nova Despesa Deve Gerar Parcelas Sequenciais")]
        [Trait("Categoria", "Despesas")]
        public void Despesa_NovaDespesa_DeveGerarParcelasSequenciais()
        {
            var vencimento = new DateTime(2026, 1, 10);
            var despesa = new Despesa(
                1,
                "Notebook",
                3,
                100,
                "Compra parcelada",
                vencimento,
                new List<string> { "usuario-1" },
                new List<Tag>());

            Assert.Equal(new[] { 1, 2, 3 }, despesa.Parcelas.Select(parcela => parcela.Numero));
            Assert.Equal(new[] { vencimento, vencimento.AddMonths(1), vencimento.AddMonths(2) }, despesa.Parcelas.Select(parcela => parcela.Vencimento));
            Assert.All(despesa.Parcelas, parcela => Assert.Equal(100, parcela.Valor));
        }

        [Fact(DisplayName = "Nova Despesa Deve Ignorar Usuarios E Tags Duplicados")]
        [Trait("Categoria", "Despesas")]
        public void Despesa_NovaDespesa_DeveIgnorarUsuariosETagsDuplicados()
        {
            var despesa = new Despesa(
                10,
                "Internet",
                1,
                120,
                string.Empty,
                new DateTime(2026, 2, 5),
                new List<string> { "usuario-1", "usuario-1", "usuario-2" },
                new List<Tag> { new Tag("casa") { Id = 1 }, new Tag("casa") { Id = 1 } });

            Assert.Equal(new[] { "usuario-1", "usuario-2" }, despesa.UsuariosEnvolvidos.Select(usuarioDespesa => usuarioDespesa.UsuarioId));
            Assert.Single(despesa.TagsDespesas);
            Assert.Equal(1, despesa.TagsDespesas.Single().TagId);
        }

        [Fact(DisplayName = "Atualizar Despesa Deve Sincronizar Dados Basicos Usuarios E Tags")]
        [Trait("Categoria", "Despesas")]
        public void Despesa_Atualizar_DeveSincronizarDadosBasicosUsuariosETags()
        {
            var despesa = new Despesa(
                7,
                "Mercado",
                2,
                50,
                "Compra mensal",
                new DateTime(2026, 3, 1),
                new List<string> { "usuario-1", "usuario-2" },
                new List<Tag> { new Tag("casa") { Id = 1 }, new Tag("mercado") { Id = 2 } });

            foreach (var tagDespesa in despesa.TagsDespesas)
                tagDespesa.Tag = tagDespesa.TagId == 1 ? new Tag("casa") { Id = 1 } : new Tag("mercado") { Id = 2 };

            var model = new CadastroDespesaViewModel(
                "Mercado atualizado",
                "Compra semanal",
                80,
                1,
                new DateTime(2026, 3, 15),
                new List<string> { "usuario-1", "usuario-3" });

            model.AdicionarTags(new List<Tag> { new Tag("mercado") { Id = 2 }, new Tag("urgente") { Id = 3 } });

            despesa.Atualizar(model);

            Assert.Equal("Mercado atualizado", despesa.Titulo);
            Assert.Equal("Compra semanal", despesa.Descritivo);
            Assert.Equal(80, despesa.Valor);
            Assert.Equal(new DateTime(2026, 3, 15), despesa.Vencimento);
            Assert.Equal(new[] { "usuario-1", "usuario-3" }, despesa.UsuariosEnvolvidos.Select(usuarioDespesa => usuarioDespesa.UsuarioId).OrderBy(usuarioId => usuarioId));
            Assert.Equal(new[] { 2, 3 }, despesa.TagsDespesas.Select(tagDespesa => tagDespesa.TagId).OrderBy(tagId => tagId));
        }
    }
}
