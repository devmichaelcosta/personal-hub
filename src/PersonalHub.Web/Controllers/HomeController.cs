using PersonalHub.Infrastructure.Data;
using PersonalHub.Web.Models;
using PersonalHub.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace PersonalHub.Web.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly PersonalHubContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(PersonalHubContext context,
                              ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Dashboard()
        {
            DateTime mesReferencia = new(DateTime.Now.Year, DateTime.Now.Month, 1);
            DashboardViewModel model = await ObterDadosDashboard(string.Empty, mesReferencia, new List<string>(), new List<int>());
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Dashboard(string pesquisa, string? mesReferencia, List<string> usuariosSelecionados, List<int> tagsSelecionadas)
        {
            DashboardViewModel model = await ObterDadosDashboard(pesquisa, ObterMesReferencia(mesReferencia), usuariosSelecionados, tagsSelecionadas);
            return View(model);
        }

        private async Task<DashboardViewModel> ObterDadosDashboard(
            string pesquisa,
            DateTime? mesReferencia,
            List<string> usuariosSelecionados,
            List<int> tagsSelecionadas)
        {
            await _context.GarantirOcorrenciasRecorrentesVencidas(DateTime.Today);

            DateTime? dataInicial = mesReferencia.HasValue
                ? new DateTime(mesReferencia.Value.Year, mesReferencia.Value.Month, 1)
                : null;
            DateTime? dataFinal = dataInicial?.AddMonths(1).AddDays(-1);
            List<string> usuariosFiltrados = await ObterUsuariosFiltrados(usuariosSelecionados);
            List<int> tagsFiltradas = await ObterTagsFiltradas(tagsSelecionadas);

            decimal rendimentos = await _context.RendimentosTotaisDoUsuario(usuariosFiltrados, pesquisa, dataInicial, dataFinal);

            var parcelas = await _context.ObterParcelasFiltradas(usuariosFiltrados, pesquisa, dataInicial, dataFinal, tagsFiltradas)
                .Select(parcelaAtual => new
                {
                    parcelaAtual.DespesaId,
                    Titulo = parcelaAtual.Despesa.Parcelas.Count > 1
                        ? (parcelaAtual.Despesa.Titulo ?? string.Empty) + " (" + parcelaAtual.Numero + "/" + parcelaAtual.Despesa.Parcelas.Count + ")"
                        : parcelaAtual.Despesa.Titulo ?? string.Empty,
                    parcelaAtual.Valor,
                    parcelaAtual.Vencimento,
                    parcelaAtual.Pago,
                    ParcelaId = parcelaAtual.Id,
                    Usuarios = parcelaAtual.Despesa.UsuariosEnvolvidos.Select(e => e.Usuario.Nome ?? string.Empty),
                    Tags = parcelaAtual.Despesa.TagsDespesas.Select(e => e.Tag.Nome).ToList()
                }).ToListAsync();

            var ocorrenciasRecorrentes = await _context.ObterOcorrenciasRecorrentesFiltradas(
                    usuariosFiltrados, pesquisa, dataInicial, dataFinal, tagsFiltradas)
                .Select(ocorrenciaAtual => new
                {
                    ocorrenciaAtual.Id,
                    ocorrenciaAtual.DespesaId,
                    Titulo = ocorrenciaAtual.Despesa.Titulo ?? string.Empty,
                    ocorrenciaAtual.Valor,
                    ocorrenciaAtual.Vencimento,
                    ocorrenciaAtual.Pago,
                    Usuarios = ocorrenciaAtual.Despesa.UsuariosEnvolvidos.Select(e => e.Usuario.Nome ?? string.Empty),
                    Tags = ocorrenciaAtual.Despesa.TagsDespesas.Select(e => e.Tag.Nome).ToList()
                })
                .ToListAsync();

            var despesas = parcelas.Select(e =>
                    new DespesaDashboard(e.DespesaId, e.Titulo, e.Valor, e.Vencimento, e.ParcelaId, e.Pago, e.Usuarios.ToList(), e.Tags))
                .Concat(ocorrenciasRecorrentes.Select(ocorrenciaAtual =>
                    new DespesaDashboard(ocorrenciaAtual.DespesaId, ocorrenciaAtual.Titulo, ocorrenciaAtual.Valor,
                        ocorrenciaAtual.Vencimento, ocorrenciaAtual.Id, ocorrenciaAtual.Pago,
                        ocorrenciaAtual.Usuarios.ToList(), ocorrenciaAtual.Tags, true)))
                .OrderBy(e => e.AvatarUsuariosEnvolvidos.Count)
                .ThenByDescending(e => e.Valor)
                .ToList();

            List<SelectListItem> usuariosDisponiveis = await ObterUsuariosDisponiveis(usuariosFiltrados);
            List<SelectListItem> tagsDisponiveis = await ObterTagsDisponiveis(tagsFiltradas);

            return new DashboardViewModel(
                rendimentos,
                mesReferencia,
                despesas,
                pesquisa,
                usuariosDisponiveis,
                usuariosFiltrados,
                tagsDisponiveis,
                tagsFiltradas);
        }

        public async Task<IActionResult> ObterDashboardDetails(string pesquisa, string? mesReferencia, List<string> usuariosSelecionados, List<int> tagsSelecionadas)
        {
            DashboardViewModel model = await ObterDadosDashboard(pesquisa, ObterMesReferencia(mesReferencia), usuariosSelecionados, tagsSelecionadas);
            return PartialView("~/Views/Home/DashboardDetails.cshtml", model);
        }

        public async Task<IActionResult> RendimentosDashboard(string? mesReferencia, List<string> usuariosSelecionados)
        {
            DateTime? mes = ObterMesReferencia(mesReferencia);
            DateTime? dataInicial = mes;
            DateTime? dataFinal = mes?.AddMonths(1).AddDays(-1);
            List<string> usuariosFiltrados = await ObterUsuariosFiltrados(usuariosSelecionados);

            var query = _context.Rendimentos
                .AsNoTracking()
                .Include(e => e.Usuario)
                .Where(e => (!dataInicial.HasValue || e.Data >= dataInicial.Value) &&
                            (!dataFinal.HasValue || e.Data <= dataFinal.Value) &&
                            usuariosFiltrados.Contains(e.UsuarioId));

            List<RendimentoDashboardItemViewModel> rendimentos = await query
                .OrderByDescending(e => e.Data)
                .ThenBy(e => e.Titulo)
                .Select(e => new RendimentoDashboardItemViewModel(
                    e.Id,
                    e.Titulo,
                    e.Descritivo,
                    e.Data,
                    e.Valor,
                    e.Usuario.Nome ?? e.Usuario.UserName ?? string.Empty))
                .ToListAsync();

            return PartialView("~/Views/Home/RendimentosDashboard.cshtml",
                new RendimentosDashboardViewModel(mes, rendimentos));
        }

        private static DateTime? ObterMesReferencia(string? mesReferencia)
        {
            if (string.IsNullOrWhiteSpace(mesReferencia))
                return null;

            return DateTime.TryParseExact(mesReferencia, "yyyy-MM", null,
                System.Globalization.DateTimeStyles.None, out DateTime resultado)
                ? new DateTime(resultado.Year, resultado.Month, 1)
                : null;
        }

        public async Task<IActionResult> Relatorios(DateTime? dataReferencia)
        {
            await _context.GarantirOcorrenciasRecorrentesVencidas(DateTime.Today);

            DateTime referencia = dataReferencia ?? DateTime.Today;
            DateTime mesReferencia = new(referencia.Year, referencia.Month, 1);
            List<string> usuariosFiltrados = await ObterUsuariosFiltrados(new List<string>());
            var meses = new List<RelatorioMensalItemViewModel>();

            for (int i = 5; i >= 0; i--)
            {
                DateTime mes = mesReferencia.AddMonths(-i);
                DateTime dataInicial = mes;
                DateTime dataFinal = mes.AddMonths(1).AddDays(-1);

                decimal despesasParceladas = await _context.ObterParcelasFiltradas(usuariosFiltrados, string.Empty, dataInicial, dataFinal)
                    .Select(e => e.Valor)
                    .SumAsync();
                decimal despesasRecorrentes = await _context.ObterOcorrenciasRecorrentesFiltradas(
                        usuariosFiltrados, string.Empty, dataInicial, dataFinal)
                    .Select(e => e.Valor)
                    .SumAsync();
                decimal despesas = despesasParceladas + despesasRecorrentes;

                decimal rendimentos = await _context.RendimentosTotaisDoUsuario(usuariosFiltrados, string.Empty, dataInicial, dataFinal);

                meses.Add(new RelatorioMensalItemViewModel(mes, despesas, rendimentos));
            }

            DateTime inicioMesAtual = mesReferencia;
            DateTime fimMesAtual = mesReferencia.AddMonths(1).AddDays(-1);
            var totaisPorTagParcelas = await _context.ObterParcelasFiltradas(usuariosFiltrados, string.Empty, inicioMesAtual, fimMesAtual)
                .SelectMany(parcelaAtual => parcelaAtual.Despesa.TagsDespesas.Select(tagDespesaAtual => new
                {
                    Tag = tagDespesaAtual.Tag.Nome,
                    parcelaAtual.Valor
                }))
                .ToListAsync();
            var totaisPorTagRecorrentes = await _context.ObterOcorrenciasRecorrentesFiltradas(
                    usuariosFiltrados, string.Empty, inicioMesAtual, fimMesAtual)
                .SelectMany(ocorrenciaAtual => ocorrenciaAtual.Despesa.TagsDespesas.Select(tagDespesaAtual => new
                {
                    Tag = tagDespesaAtual.Tag.Nome,
                    ocorrenciaAtual.Valor
                }))
                .ToListAsync();
            var totaisPorTag = totaisPorTagParcelas.Concat(totaisPorTagRecorrentes)
                .GroupBy(e => e.Tag)
                .Select(e => new { Tag = e.Key, Total = e.Sum(item => item.Valor) })
                .OrderByDescending(e => e.Total)
                .Take(8)
                .ToList();
            List<RelatorioTagViewModel> tags = totaisPorTag
                .Select(e => new RelatorioTagViewModel(e.Tag, e.Total))
                .ToList();

            return View(new RelatorioMensalViewModel(mesReferencia, meses, tags));
        }

        private async Task<List<string>> ObterUsuariosFiltrados(List<string> usuariosSelecionados)
        {
            usuariosSelecionados ??= new List<string>();

            string usuarioLogado = User?.Identity?.Name ?? string.Empty;
            string usuarioLogadoId = await _context.Users
                .Where(e => e.UserName == usuarioLogado)
                .Select(e => e.Id)
                .SingleOrDefaultAsync() ?? string.Empty;

            List<string> idsUsuariosValidos = await _context.Users.Select(e => e.Id).ToListAsync();
            List<string> usuariosFiltrados = usuariosSelecionados
                .Where(idsUsuariosValidos.Contains)
                .Distinct()
                .ToList();

            if (usuariosFiltrados.Count == 0 && !string.IsNullOrWhiteSpace(usuarioLogadoId))
                usuariosFiltrados.Add(usuarioLogadoId);

            return usuariosFiltrados;
        }

        private async Task<List<SelectListItem>> ObterUsuariosDisponiveis(List<string> usuariosSelecionados)
        {
            return await _context.Users
                .OrderBy(e => e.Nome)
                .Select(e => new SelectListItem(e.Nome ?? e.UserName ?? string.Empty, e.Id, usuariosSelecionados.Contains(e.Id)))
                .ToListAsync();
        }

        private async Task<List<int>> ObterTagsFiltradas(List<int> tagsSelecionadas)
        {
            tagsSelecionadas ??= new List<int>();

            List<int> idsTagsValidas = await _context.Tags.Select(e => e.Id).ToListAsync();
            return tagsSelecionadas
                .Where(idsTagsValidas.Contains)
                .Distinct()
                .ToList();
        }

        private async Task<List<SelectListItem>> ObterTagsDisponiveis(List<int> tagsSelecionadas)
        {
            return await _context.Tags
                .OrderBy(e => e.Nome)
                .Select(e => new SelectListItem(e.Nome, e.Id.ToString(), tagsSelecionadas.Contains(e.Id)))
                .ToListAsync();
        }

        public PartialViewResult DashboardDetails(DashboardViewModel model) => PartialView(model);

        public IActionResult Privacy()
        {
            throw new Exception("erro");
        }

        public IActionResult Teste()
        {
            throw new Exception("Ocorreu um erro");
        }

        [Route("erro/{id:length(3,3)}")]
        public IActionResult Error(int? id)
        {
            var model = new CustomErrorViewModel(id);

            _logger.LogError("Ocorreu um erro de codigo {StatusCode}", id);

            if (id != 500 && id != 404 && id != 403) return StatusCode(404);

            return View(model);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _context.Dispose();

            base.Dispose(disposing);
        }
    }
}
