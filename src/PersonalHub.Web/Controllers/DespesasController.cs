using PersonalHub.Application;
using PersonalHub.Application.CasosUso;
using PersonalHub.Domain.Entidades;
using PersonalHub.Domain.Enums;
using PersonalHub.Web.Filters;
using PersonalHub.Web.Helpers;
using PersonalHub.Infrastructure.Data;
using PersonalHub.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace PersonalHub.Web.Controllers
{
    [Authorize]
    public class DespesasController : BaseController  
    {
        private readonly PersonalHubContext _context;
        //private readonly IMediator _mediator;
        //private readonly DespesaApiClient _client;

        public DespesasController(PersonalHubContext context
                                    //, 
                                  //IMediator mediator,
                                  //DespesaApiClient despesaApiClient
            )
        { 
            _context = context;
            //_mediator = mediator;
            //_client = despesaApiClient; 
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.UsuariosDisponiveis = await ObterSelectListUsuarios();
            ViewBag.TagsDisponiveis = await ObterSelectListTags();
            return View();
        }

        public async Task<IActionResult> ListarDataTable()
        {
            var request = ObterDataTableRequest();
            IQueryable<Despesa> queryBase = _context.ObterDespesasFiltradas(ObterUsuario());
            int recordsTotal = await queryBase.CountAsync();

            IQueryable<Despesa> queryFiltrada = AplicarFiltrosAvancados(queryBase);
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                string termoPesquisa = request.Search.Trim();
                queryFiltrada = queryFiltrada.Where(despesaAtual =>
                    (despesaAtual.Titulo != null && despesaAtual.Titulo.Contains(termoPesquisa)) ||
                    despesaAtual.Descritivo.Contains(termoPesquisa) ||
                    despesaAtual.TagsDespesas.Any(tagDespesaAtual => tagDespesaAtual.Tag.Nome.Contains(termoPesquisa)));
            }

            int recordsFiltered = await queryFiltrada.CountAsync();
            queryFiltrada = OrdenarDespesas(queryFiltrada, request.OrderColumn, request.OrderDirection);

            var despesasPaginadas = await queryFiltrada
                .Skip(request.Start)
                .Take(request.Length)
                .Select(despesaAtual => new
                {
                    despesaAtual.Id,
                    Titulo = despesaAtual.Titulo ?? string.Empty,
                    despesaAtual.Descritivo,
                    despesaAtual.Recorrente,
                    QtdParcelas = despesaAtual.QtdParcelas ?? 0,
                    Valor = despesaAtual.Valor.GetValueOrDefault(),
                    despesaAtual.Vencimento,
                    Tags = despesaAtual.TagsDespesas.Select(tagDespesaAtual => tagDespesaAtual.Tag.Nome).ToList()
                })
                .ToListAsync();

            var despesas = despesasPaginadas
                .Select(despesaAtual => new DespesaListaDataTableItem(
                    despesaAtual.Id,
                    despesaAtual.Titulo,
                    despesaAtual.Descritivo,
                    despesaAtual.Recorrente ? "Mensal" : despesaAtual.QtdParcelas.ToString(),
                    despesaAtual.Valor.ToString("N2"),
                    despesaAtual.Vencimento.ToString("dd/MM/yyyy"),
                    despesaAtual.Tags))
                .ToList();

            return Json(new
            {
                draw = request.Draw,
                recordsTotal,
                recordsFiltered,
                data = despesas
            });
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (_context == null) return Problem("");

            if (id == null ) return NotFound(); 

            Despesa? despesa = await _context.Despesas.Include(e=> e.Parcelas)
                .Include(e => e.TagsDespesas).ThenInclude(e=>e.Tag)
                .AsNoTracking()
                .SingleOrDefaultAsync(m => m.Id == id);

            if (despesa == null)  return NotFound();

            DespesaViewModel model = despesa;

            return View(model);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            //var model = await _mediator.Send(new ObterDespesaPorIdQuery((int)id));
            //Resposta resposta = await _mediator.Send(new DeletarDespesaCommand((int)id));
            //DespesaViewModel? model = await _client.ObterDespesaViewModel((int)id);

            Despesa despesa = await _context.Despesas.AsNoTracking().SingleAsync(m => m.Id == id);

            DespesaViewModel model = despesa; 
            return View(model);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            //Resposta resposta = await _client.Deletar(id);
            //Resposta resposta = await _mediator.Send(new DeletarDespesaCommand(id));

            //if (!resposta.Sucesso)
            //{
            //}

            //_context.Parcelas.RemoveRange(_context.Parcelas.Where(p => p.DespesaId == id));
            //_context.TagsDepesas.RemoveRange(_context.TagsDepesas.Where(a => a.DespesaId == id));
            _context.Despesas.Remove(_context.Despesas.Single(e => e.Id == id));
            await _context.SaveChangesAsync();

            //if (local == "dashboard")
            //    return RedirectToAction("Dashboard","Home");

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Deletar(int id)
        {
            Despesa despesa = await _context.Despesas.AsNoTracking().SingleAsync(m => m.Id == id);
            _context.Despesas.Remove(_context.Despesas.Single(e => e.Id == id));
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        private async Task<bool> DespesaExists(int id) => await _context.Despesas.AnyAsync(e => e.Id == id);

        public async Task<IActionResult> Cadastro()
        {
            ViewBag.UsuariosDisponiveis = await ObterSelectListUsuarios();
            ViewBag.TemErros = false;
            return PartialView(CadastroDespesaViewModel.CriarComValoresIniciais());
        }

        public IActionResult CadastroCampos(CadastroDespesaViewModel model) => PartialView(model);

        public async Task<IActionResult> CadastroModal()
        {
            ViewBag.UsuariosDisponiveis = await ObterSelectListUsuarios();
            ViewBag.TemErros = false;
            return PartialView(CadastroDespesaViewModel.CriarComValoresIniciais());
        }
         
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Cadastro(CadastroDespesaViewModel model)
        {
            ViewBag.TemErros = !ModelState.IsValid;

            if (!ModelState.IsValid) 
            {
                GerarErrosModelState();
                ViewBag.UsuariosDisponiveis = await ObterSelectListUsuarios();
                return View(model);
            }

            Despesa? despesa = await CadastrarDespesa(model);

            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Cadastrar(CadastroDespesaViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new
                {
                    sucesso = false,
                    erros = ModelState.Values
                        .SelectMany(valor => valor.Errors)
                        .Select(erro => erro.ErrorMessage)
                });
            }

            Despesa? despesa = await CadastrarDespesa(model);

            return Json(new { sucesso = true });
        }

        private async Task<Despesa> CadastrarDespesa(CadastroDespesaViewModel model)
        {
            model.QtdParcelas = model.Tipo == TipoCadastroDespesa.GerarParaOsProximosMeses ? 1 : model.QtdParcelas;

            List<Tag> tagsEncontradas = await _context.Tags.Where(tagAtual => model.Tags.Contains(tagAtual.Nome)).ToListAsync();
            List<string> tagsNaoEncontradas = model.Tags.Except(tagsEncontradas.Select(e => e.Nome).ToList()).ToList();
            List<Tag> tagsNovas = new();

            if (tagsNaoEncontradas.Count > 0)
            {
                tagsNovas = tagsNaoEncontradas.Select(e => new Tag(e)).ToList();
                _context.Tags.AddRange(tagsNovas);
                await _context.SaveChangesAsync();
            }

            List<Tag> tags = new();
            tags.AddRange(tagsEncontradas);
            tags.AddRange(tagsNovas);
            tags = tags.Distinct().ToList();

            model.AdicionarTags(tags);

            Despesa despesa = model.Tipo == TipoCadastroDespesa.RecorrenteMensal
                ? Despesa.CriarRecorrente(model)
                : Despesa.Criar(model);

            if (model.Tipo == TipoCadastroDespesa.GerarParaOsProximosMeses)
            {
                string padraoTitulo = despesa.Titulo ?? string.Empty;
                despesa.Titulo = $"{padraoTitulo} / {AppHelper.ObterMes(despesa.Vencimento.Month)}";
                _context.Add(despesa);

                for (int i = 2; i <= model.QtdMeses; i++)
                {
                    DateTime vencimentoNovo = despesa.Vencimento.AddMonths(i - 1);

                    Despesa despesaCopia = despesa.Clonar(vencimentoNovo);
                    despesaCopia.Titulo = $"{padraoTitulo} / {AppHelper.ObterMes(vencimentoNovo.Month)}";

                    _context.Add(despesaCopia);
                }
            }
            else
                _context.Add(despesa);

            await _context.SaveChangesAsync();

            return despesa;
        }

        public async Task<IActionResult> Edicao(int? id)
        {
            if (id == null)  return NotFound();

            Despesa? despesa = await _context.ObterDespesaComAgregados((int)id);

            if (despesa == null) return NotFound();

            ViewBag.UsuariosDisponiveis = await ObterSelectListUsuarios();
            ViewBag.TemErros = false;
            var edicaoDespesa = EdicaoDespesaViewModel.Criar(despesa);
            return View(edicaoDespesa);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edicao(int id, EdicaoDespesaViewModel model)
        {
            ViewBag.TemErros = false;

            if (id != model.Id) return NotFound();
            
            if (!ModelState.IsValid) 
            {
                GerarErrosModelState();
                ViewBag.UsuariosDisponiveis = await ObterSelectListUsuarios();
                return View(model); 
            }

            Despesa? despesa = await EditarDespesa(model);
             
            return RedirectToAction(nameof(Index)); 
        }

        public async Task<IActionResult> EdicaoModal(int id, int parcelaId = 0, int ocorrenciaId = 0)
        {
            ViewBag.UsuariosDisponiveis = await ObterSelectListUsuarios();
            ViewBag.TemErros = false;

            Despesa? despesa = await _context.ObterDespesaComAgregados((int)id);

            if (despesa == null) return NotFound();

            var edicaoDespesa = EdicaoDespesaViewModel.Criar(despesa);
            if (despesa.Recorrente)
            {
                var ocorrencia = await _context.OcorrenciasDespesasRecorrentes
                    .SingleOrDefaultAsync(item => item.Id == ocorrenciaId && item.DespesaId == id);
                if (ocorrencia == null) return NotFound();

                edicaoDespesa.OcorrenciaId = ocorrenciaId;
                return PartialView(edicaoDespesa);
            }

            var parcela = despesa.Parcelas.SingleOrDefault(e => e.Id == parcelaId);
            if (parcela == null) return NotFound();

            edicaoDespesa.ParcelaId = parcelaId;
            edicaoDespesa.Vencimento = parcela.Vencimento;
            return PartialView(edicaoDespesa);
        }

        [HttpPost]
        public async Task<JsonResult> EdicaoModal(int id, EdicaoDespesaViewModel model)
        {
            if (!ModelState.IsValid || id != model.Id ||
                (!model.Recorrente && (model.QtdParcelas is null || model.QtdParcelas < 1)) ||
                model.ValorParcela is null || model.ValorParcela <= 0)
                return Json(new { sucesso = false, erro = "Informe uma quantidade de parcelas válida e um valor de parcela maior que zero." });

            Despesa? despesa = await _context.ObterDespesaComAgregados(model.Id);

            if (despesa == null || despesa.Recorrente != model.Recorrente)
                return Json(new { sucesso = false });

            if (despesa.Recorrente && !await _context.OcorrenciasDespesasRecorrentes
                    .AnyAsync(item => item.Id == model.OcorrenciaId && item.DespesaId == despesa.Id))
                return Json(new { sucesso = false });

            Parcela? parcela = despesa.Recorrente
                ? null
                : despesa.Parcelas.SingleOrDefault(e => e.Id == model.ParcelaId);
            if (!despesa.Recorrente && parcela == null) return Json(new { sucesso = false });

            await using var transacao = await _context.Database.BeginTransactionAsync();

            DateTime vencimentoOriginal = despesa.Vencimento;
            int parcelaInicialId = despesa.Recorrente ? 0 : despesa.Parcelas.OrderBy(e => e.Numero).First().Id;

            List<Tag> tagsEncontradas = new();
            List<string> tagsNaoEncontradas = new();

            if (model.Tags != null)
            {
                tagsEncontradas = await _context.Tags.Where(tagAtual => model.Tags.Contains(tagAtual.Nome)).ToListAsync();
                tagsNaoEncontradas = model.Tags.Except(tagsEncontradas.Select(e => e.Nome).ToList()).ToList();
            }

            List<Tag> tagsNovas = new();

            if (tagsNaoEncontradas.Count > 0)
            {
                tagsNovas = tagsNaoEncontradas.Select(e => new Tag(e)).ToList();
                _context.Tags.AddRange(tagsNovas);
                await _context.SaveChangesAsync();
            }

            List<Tag> tags = new();
            tags.AddRange(tagsEncontradas);
            tags.AddRange(tagsNovas);
            tags = tags.Distinct().ToList();

            model.AdicionarTags(tags);
            despesa.Atualizar(model);

            if (despesa.Recorrente)
            {
                _context.Update(despesa);
                await _context.SaveChangesAsync();
                await transacao.CommitAsync();
                return Json(new { sucesso = true });
            }

            parcela!.Valor = model.ValorParcela.GetValueOrDefault();
            parcela.Vencimento = model.Vencimento;

            List<Parcela> parcelasRemovidas = despesa.AjustarQuantidadeParcelas(
                model.QtdParcelas.GetValueOrDefault(),
                model.ValorParcela.Value);
            _context.Parcelas.RemoveRange(parcelasRemovidas);

            if (parcela.Id != parcelaInicialId)
                despesa.Vencimento = vencimentoOriginal;

            if (!parcelasRemovidas.Contains(parcela))
                _context.Entry(parcela).State = EntityState.Modified;

            _context.Update(despesa);
            await _context.SaveChangesAsync();
            await transacao.CommitAsync();

            return Json(new { sucesso = true });
        }

        public IActionResult EdicaoCampos(EdicaoDespesaViewModel model) => PartialView(model);

        private async Task<Despesa?> EditarDespesa(EdicaoDespesaViewModel model)
        {
            Despesa? despesa = await _context.ObterDespesaComAgregados(model.Id);

            if (despesa == null) return null;

            List<Tag> tagsEncontradas = new();
            List<string> tagsNaoEncontradas = new();

            if (model.Tags != null)
            {
                tagsEncontradas = await _context.Tags.Where(tagAtual => model.Tags.Contains(tagAtual.Nome)).ToListAsync();
                tagsNaoEncontradas = model.Tags.Except(tagsEncontradas.Select(e => e.Nome).ToList()).ToList();
            }

            List<Tag> tagsNovas = new();

            if (tagsNaoEncontradas.Count > 0)
            {
                tagsNovas = tagsNaoEncontradas.Select(e => new Tag(e)).ToList();
                _context.Tags.AddRange(tagsNovas);
                await _context.SaveChangesAsync();
            }

            List<Tag> tags = new();
            tags.AddRange(tagsEncontradas);
            tags.AddRange(tagsNovas);
            tags = tags.Distinct().ToList();

            model.AdicionarTags(tags);
            despesa.Atualizar(model);

            _context.Update(despesa);
            await _context.SaveChangesAsync();

            return despesa;
        }

        private async Task<List<SelectListItem>> ObterSelectListUsuarios()
        {
            return await _context.Users.OrderBy(e => e.Nome).Select(e => new SelectListItem(e.Nome, e.Id)).ToListAsync();
        }

        private async Task<List<SelectListItem>> ObterSelectListTags()
        {
            return await _context.Tags.OrderBy(e => e.Nome).Select(e => new SelectListItem(e.Nome, e.Id.ToString())).ToListAsync();
        }

        private IQueryable<Despesa> AplicarFiltrosAvancados(IQueryable<Despesa> query)
        {
            IQueryCollection parametros = Request.Query;
            DateTime? dataInicial = ObterDataQuery(parametros, "dataInicial");
            DateTime? dataFinal = ObterDataQuery(parametros, "dataFinal");
            List<string> usuariosSelecionados = ObterListaQuery(parametros, "usuarios");
            List<int> tagsSelecionadas = ObterListaInteirosQuery(parametros, "tags");

            if (dataInicial.HasValue)
                query = query.Where(despesaAtual => despesaAtual.Vencimento >= dataInicial.Value);

            if (dataFinal.HasValue)
                query = query.Where(despesaAtual => despesaAtual.Vencimento <= dataFinal.Value);

            if (usuariosSelecionados.Count > 0)
                query = query.Where(despesaAtual => despesaAtual.UsuariosEnvolvidos
                    .Any(usuarioDespesaAtual => usuariosSelecionados.Contains(usuarioDespesaAtual.UsuarioId)));

            if (tagsSelecionadas.Count > 0)
                query = query.Where(despesaAtual => despesaAtual.TagsDespesas
                    .Any(tagDespesaAtual => tagsSelecionadas.Contains(tagDespesaAtual.TagId)));

            return query;
        }

        private DataTableRequest ObterDataTableRequest()
        {
            IQueryCollection query = Request.Query;

            return new DataTableRequest(
                ObterInteiroQuery(query, "draw"),
                ObterInteiroQuery(query, "start"),
                Math.Clamp(ObterInteiroQuery(query, "length", 10), 1, 100),
                query["search[value]"].ToString(),
                ObterInteiroQuery(query, "order[0][column]"),
                query["order[0][dir]"].ToString());
        }

        private static int ObterInteiroQuery(IQueryCollection query, string nome, int valorPadrao = 0)
        {
            return int.TryParse(query[nome].ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int valor)
                ? valor
                : valorPadrao;
        }

        private static DateTime? ObterDataQuery(IQueryCollection query, string nome)
        {
            return DateTime.TryParse(query[nome].ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime valor)
                ? valor
                : null;
        }

        private static List<string> ObterListaQuery(IQueryCollection query, string nome)
        {
            return query[nome]
                .Select(e => e ?? string.Empty)
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Distinct()
                .ToList();
        }

        private static List<int> ObterListaInteirosQuery(IQueryCollection query, string nome)
        {
            return query[nome]
                .Select(e => int.TryParse(e, NumberStyles.Integer, CultureInfo.InvariantCulture, out int valor) ? valor : 0)
                .Where(e => e > 0)
                .Distinct()
                .ToList();
        }

        private static IQueryable<Despesa> OrdenarDespesas(IQueryable<Despesa> query, int coluna, string direcao)
        {
            bool descendente = string.Equals(direcao, "desc", StringComparison.OrdinalIgnoreCase);

            return coluna switch
            {
                0 => descendente ? query.OrderByDescending(e => e.Titulo) : query.OrderBy(e => e.Titulo),
                1 => descendente ? query.OrderByDescending(e => e.Descritivo) : query.OrderBy(e => e.Descritivo),
                2 => descendente ? query.OrderByDescending(e => e.QtdParcelas) : query.OrderBy(e => e.QtdParcelas),
                3 => descendente ? query.OrderByDescending(e => e.Valor) : query.OrderBy(e => e.Valor),
                4 => descendente ? query.OrderByDescending(e => e.Vencimento) : query.OrderBy(e => e.Vencimento),
                _ => query.OrderByDescending(e => e.Id)
            };
        }

        private sealed record DataTableRequest(
            int Draw,
            int Start,
            int Length,
            string Search,
            int OrderColumn,
            string OrderDirection);

        private sealed record DespesaListaDataTableItem(
            int Id,
            string Titulo,
            string Descritivo,
            string QtdParcelas,
            string Valor,
            string Vencimento,
            List<string> Tags);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
