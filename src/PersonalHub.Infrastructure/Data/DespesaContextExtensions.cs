using PersonalHub.Domain.Entidades;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace PersonalHub.Infrastructure.Data
{
    public static class DespesaContextExtensions
    {
        public static Task GarantirOcorrenciasRecorrentesVencidas(this PersonalHubContext context, DateTime hoje)
            => context.GarantirOcorrenciasRecorrentesNoPeriodo(DateTime.MinValue, hoje.Date, CancellationToken.None);

        public static async Task GarantirOcorrenciasRecorrentesNoPeriodo(
            this PersonalHubContext context,
            DateTime inicio,
            DateTime fim,
            CancellationToken cancellationToken)
        {
            if (inicio > fim)
                return;

            List<Despesa> despesasRecorrentes = await context.Despesas
                .AsNoTracking()
                .Where(despesaAtual => despesaAtual.Recorrente)
                .ToListAsync(cancellationToken);

            if (despesasRecorrentes.Count == 0)
                return;

            List<int> idsDespesas = despesasRecorrentes.Select(despesaAtual => despesaAtual.Id).ToList();
            DateTime primeiroMesConsulta = new(inicio.Year, inicio.Month, 1);
            DateTime ultimoMesConsulta = new(fim.Year, fim.Month, 1);

            for (int tentativa = 0; tentativa < 2; tentativa++)
            {
                var existentes = await context.OcorrenciasDespesasRecorrentes
                    .Where(ocorrenciaAtual => idsDespesas.Contains(ocorrenciaAtual.DespesaId) &&
                        ocorrenciaAtual.MesReferencia >= primeiroMesConsulta &&
                        ocorrenciaAtual.MesReferencia <= ultimoMesConsulta)
                    .Select(ocorrenciaAtual => new { ocorrenciaAtual.DespesaId, ocorrenciaAtual.MesReferencia })
                    .ToListAsync(cancellationToken);
                var mesesExistentes = existentes
                    .Select(ocorrenciaAtual => (ocorrenciaAtual.DespesaId, ocorrenciaAtual.MesReferencia.Date))
                    .ToHashSet();

                foreach (Despesa despesa in despesasRecorrentes)
                {
                    DateTime primeiroMesDaDespesa = new(despesa.Vencimento.Year, despesa.Vencimento.Month, 1);
                    DateTime primeiroMes = primeiroMesDaDespesa > primeiroMesConsulta ? primeiroMesDaDespesa : primeiroMesConsulta;

                    for (DateTime mes = primeiroMes; mes <= ultimoMesConsulta;)
                    {
                        DateTime vencimento = despesa.ObterVencimentoRecorrente(mes);
                        var chave = (despesa.Id, mes.Date);

                        if (vencimento.Date >= inicio.Date && vencimento.Date <= fim.Date && !mesesExistentes.Contains(chave))
                        {
                            context.OcorrenciasDespesasRecorrentes.Add(new OcorrenciaDespesaRecorrente(
                                despesa.Id,
                                mes,
                                vencimento,
                                despesa.Valor.GetValueOrDefault()));
                            mesesExistentes.Add(chave);
                        }

                        if (mes == ultimoMesConsulta)
                            break;
                        mes = mes.AddMonths(1);
                    }
                }

                if (!context.ChangeTracker.Entries<OcorrenciaDespesaRecorrente>()
                    .Any(entrada => entrada.State == EntityState.Added))
                    return;

                try
                {
                    await context.SaveChangesAsync(cancellationToken);
                    return;
                }
                catch (DbUpdateException excecao) when (
                    tentativa == 0 &&
                    excecao.GetBaseException() is SqlException erroSql &&
                    erroSql.Number is 2601 or 2627)
                {
                    foreach (var entrada in context.ChangeTracker.Entries<OcorrenciaDespesaRecorrente>()
                        .Where(entrada => entrada.State == EntityState.Added))
                        entrada.State = EntityState.Detached;
                }
            }
        }

        public static async Task<Despesa?> ObterDespesaComAgregados(this PersonalHubContext context, int id)
        {
            return await context.Despesas.Include(despesaAtual => despesaAtual.Parcelas)
                .Include(despesaAtual => despesaAtual.UsuariosEnvolvidos)
                .Include(despesaAtual => despesaAtual.TagsDespesas).ThenInclude(tagDespesaAtual => tagDespesaAtual.Tag)
                .SingleOrDefaultAsync(despesaAtual => despesaAtual.Id == id);
        }

        public static IQueryable<Despesa> ObterDespesasFiltradas(
            this PersonalHubContext context,
            string usuario,
            string? termoPesquisa = null)
        {
            IQueryable<Despesa> query = context.Despesas.AsNoTracking()
                .Include(despesaAtual => despesaAtual.TagsDespesas)
                    .ThenInclude(tagDespesaAtual => tagDespesaAtual.Tag)
                .Where(despesaAtual => despesaAtual.UsuariosEnvolvidos
                    .Any(usuarioDespesaAtual => usuarioDespesaAtual.Usuario.UserName == usuario));

            if (!string.IsNullOrWhiteSpace(termoPesquisa))
            {
                query = query.Where(despesaAtual =>
                    (despesaAtual.Titulo != null && despesaAtual.Titulo.Contains(termoPesquisa)) ||
                    despesaAtual.Descritivo.Contains(termoPesquisa) ||
                    despesaAtual.TagsDespesas.Any(tagDespesaAtual => tagDespesaAtual.Tag.Nome.Contains(termoPesquisa)));
            }

            return query;
        }

        public static async Task<decimal> RendimentosTotaisDoUsuario(
            this PersonalHubContext context,
            IEnumerable<string> usuariosIds, string termoPesquisa, DateTime? dataInicial, DateTime? dataFinal)
        {
            List<string> usuariosSelecionados = (usuariosIds ?? Enumerable.Empty<string>())
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .ToList();

            return await context.Rendimentos
                .Where(e => (!dataInicial.HasValue || e.Data >= dataInicial.Value) &&
                            (!dataFinal.HasValue || e.Data <= dataFinal.Value) &&
                            usuariosSelecionados.Contains(e.UsuarioId))
                .Select(e => e.Valor)
                .SumAsync();
        }

        public static IQueryable<Parcela> ObterParcelasFiltradas(
            this PersonalHubContext context,
            IEnumerable<string> usuariosIds, string termoPesquisa, DateTime? dataInicial, DateTime? dataFinal,
            IEnumerable<int>? tagsIds = null)
        {
            List<string> usuariosSelecionados = (usuariosIds ?? Enumerable.Empty<string>())
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .ToList();
            List<int> tagsSelecionadas = (tagsIds ?? Enumerable.Empty<int>())
                .Distinct()
                .ToList();

            IQueryable<Parcela> query = context.Parcelas.AsNoTracking()
                .Include(parcelaAtual => parcelaAtual.Despesa)
                    .ThenInclude(despesaAtual => despesaAtual.Parcelas)
                .Include(parcelaAtual => parcelaAtual.Despesa)
                    .ThenInclude(tagsAtual => tagsAtual.TagsDespesas)
                        .ThenInclude(tagAtual => tagAtual.Tag)
                .Include(parcelaAtual => parcelaAtual.Despesa)
                    .ThenInclude(despesaAtual => despesaAtual.UsuariosEnvolvidos)
                        .ThenInclude(usuarioDespesaAtual => usuarioDespesaAtual.Usuario)
                .Where(parcelaAtual => (!dataInicial.HasValue || parcelaAtual.Vencimento >= dataInicial.Value) &&
                                       (!dataFinal.HasValue || parcelaAtual.Vencimento <= dataFinal.Value) &&
                                       parcelaAtual.Despesa.UsuariosEnvolvidos.Any(e => usuariosSelecionados.Contains(e.UsuarioId)));

            if (!string.IsNullOrEmpty(termoPesquisa))
            {
                query = query.Where(
                    parcelaAtual => (parcelaAtual.Despesa.Titulo != null && parcelaAtual.Despesa.Titulo.Contains(termoPesquisa)) ||
                                    parcelaAtual.Despesa.Descritivo.Contains(termoPesquisa) ||
                                    parcelaAtual.Despesa.TagsDespesas.Any(e => e.Tag.Nome.Contains(termoPesquisa))
                );
            }

            if (tagsSelecionadas.Count > 0)
            {
                query = query.Where(parcelaAtual => parcelaAtual.Despesa.TagsDespesas
                    .Any(tagDespesaAtual => tagsSelecionadas.Contains(tagDespesaAtual.TagId)));
            }

            return query;
        }

        public static IQueryable<OcorrenciaDespesaRecorrente> ObterOcorrenciasRecorrentesFiltradas(
            this PersonalHubContext context,
            IEnumerable<string> usuariosIds,
            string termoPesquisa,
            DateTime? dataInicial,
            DateTime? dataFinal,
            IEnumerable<int>? tagsIds = null)
        {
            List<string> usuariosSelecionados = (usuariosIds ?? Enumerable.Empty<string>())
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Distinct()
                .ToList();
            List<int> tagsSelecionadas = (tagsIds ?? Enumerable.Empty<int>())
                .Distinct()
                .ToList();

            IQueryable<OcorrenciaDespesaRecorrente> query = context.OcorrenciasDespesasRecorrentes.AsNoTracking()
                .Include(ocorrenciaAtual => ocorrenciaAtual.Despesa)
                    .ThenInclude(despesaAtual => despesaAtual.TagsDespesas)
                        .ThenInclude(tagDespesaAtual => tagDespesaAtual.Tag)
                .Include(ocorrenciaAtual => ocorrenciaAtual.Despesa)
                    .ThenInclude(despesaAtual => despesaAtual.UsuariosEnvolvidos)
                        .ThenInclude(usuarioDespesaAtual => usuarioDespesaAtual.Usuario)
                .Where(ocorrenciaAtual => (!dataInicial.HasValue || ocorrenciaAtual.Vencimento >= dataInicial.Value) &&
                                          (!dataFinal.HasValue || ocorrenciaAtual.Vencimento <= dataFinal.Value) &&
                                          ocorrenciaAtual.Despesa.UsuariosEnvolvidos.Any(usuarioDespesaAtual =>
                                              usuariosSelecionados.Contains(usuarioDespesaAtual.UsuarioId)));

            if (!string.IsNullOrWhiteSpace(termoPesquisa))
            {
                query = query.Where(ocorrenciaAtual =>
                    (ocorrenciaAtual.Despesa.Titulo != null && ocorrenciaAtual.Despesa.Titulo.Contains(termoPesquisa)) ||
                    ocorrenciaAtual.Despesa.Descritivo.Contains(termoPesquisa) ||
                    ocorrenciaAtual.Despesa.TagsDespesas.Any(tagDespesaAtual => tagDespesaAtual.Tag.Nome.Contains(termoPesquisa)));
            }

            if (tagsSelecionadas.Count > 0)
            {
                query = query.Where(ocorrenciaAtual => ocorrenciaAtual.Despesa.TagsDespesas
                    .Any(tagDespesaAtual => tagsSelecionadas.Contains(tagDespesaAtual.TagId)));
            }

            return query;
        }
    }
}
