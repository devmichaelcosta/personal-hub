using PersonalHub.Domain.Entidades;
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
    public class RendimentosController : Controller
    {
        private readonly PersonalHubContext _context;

        public RendimentosController(PersonalHubContext context)
        {
            _context = context;
        }

        public IActionResult Index() => View();

        public async Task<IActionResult> ListarDataTable()
        {
            var request = ObterDataTableRequest();
            string usuarioLogadoId = await ObterUsuarioLogadoId();
            IQueryable<Rendimento> queryBase = _context.Rendimentos
                .AsNoTracking()
                .Where(rendimentoAtual => rendimentoAtual.UsuarioId == usuarioLogadoId);
            int recordsTotal = await queryBase.CountAsync();

            IQueryable<Rendimento> queryFiltrada = AplicarFiltroRendimentos(queryBase, request.Search);
            int recordsFiltered = await queryFiltrada.CountAsync();

            queryFiltrada = OrdenarRendimentos(queryFiltrada, request.OrderColumn, request.OrderDirection);

            var rendimentosPaginados = await queryFiltrada
                .Skip(request.Start)
                .Take(request.Length)
                .Select(rendimentoAtual => new
                {
                    rendimentoAtual.Id,
                    rendimentoAtual.Data,
                    rendimentoAtual.Valor,
                    rendimentoAtual.Descritivo,
                    rendimentoAtual.Titulo
                })
                .ToListAsync();

            var rendimentos = rendimentosPaginados
                .Select(rendimentoAtual => new RendimentoListaDataTableItem(
                    rendimentoAtual.Id,
                    rendimentoAtual.Data.ToString("dd/MM/yyyy"),
                    rendimentoAtual.Valor.ToString("N2"),
                    rendimentoAtual.Descritivo,
                    rendimentoAtual.Titulo))
                .ToList();

            return Json(new
            {
                draw = request.Draw,
                recordsTotal,
                recordsFiltered,
                data = rendimentos
            });
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)  return NotFound(); 

            var rendimento = await _context.Rendimentos.FirstOrDefaultAsync(m => m.Id == id);
            if (rendimento == null)  return NotFound();

            RendimentoViewModel model = rendimento;
            return View(model);
        }

        public async Task<IActionResult> Create()
        {
            var usuarios = await _context.Users.OrderBy(e => e.Nome).Select(e => new SelectListItem(e.Nome, e.Id)).ToListAsync();
            var model = new RendimentoCadastroViewModel
            {
                UsuariosDisponiveis = usuarios
            };
            return View(model);
        } 

        public async Task<IActionResult> CadastroModal()
        {
            var usuarioLogadoId = await ObterUsuarioLogadoId();

            var model = new RendimentoCadastroViewModel
            {
                Data = DateTime.Now,
                UsuarioId = usuarioLogadoId
            };

            return PartialView(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RendimentoCadastroViewModel rendimento)
        {
            if (ModelState.IsValid)
            {
                _context.Add(new Rendimento
                {
                    Data = rendimento.Data,
                    Descritivo = rendimento.Descritivo ?? string.Empty,
                    Titulo = rendimento.Titulo,
                    Valor = rendimento.Valor,   
                    UsuarioId = rendimento.UsuarioId
                });
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            var usuarios = await _context.Users.OrderBy(e => e.Nome).Select(e => new SelectListItem(e.Nome, e.Id)).ToListAsync();
            rendimento.UsuariosDisponiveis = usuarios;
            return View(rendimento);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<JsonResult> Cadastrar(RendimentoCadastroViewModel rendimento)
        {
            rendimento.UsuarioId = await ObterUsuarioLogadoId();
            ModelState.Remove(nameof(RendimentoCadastroViewModel.UsuarioId));

            if (!ModelState.IsValid)
                return Json(new { sucesso = false });

            _context.Add(new Rendimento
            {
                Data = rendimento.Data,
                Descritivo = rendimento.Descritivo ?? string.Empty,
                Titulo = rendimento.Titulo,
                Valor = rendimento.Valor,
                UsuarioId = rendimento.UsuarioId
            });

            await _context.SaveChangesAsync();

            return Json(new { sucesso = true });
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)  return NotFound();

            Rendimento? rendimento = await _context.Rendimentos.SingleOrDefaultAsync(e => e.Id == id);
            if (rendimento == null)  return NotFound();
            var usuarios = await _context.Users.OrderBy(e => e.Nome).Select(e => new SelectListItem(e.Nome, e.Id)).ToListAsync();

            RendimentoEdicaoViewModel model = rendimento;
            model.UsuariosDisponiveis = usuarios;
            return View(model);
        }

        [HttpPost,ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, 
            [Bind("Id,Data,Valor,Descritivo,Titulo,UsuarioId")] RendimentoEdicaoViewModel model)
        {
            if (id != model.Id)  return NotFound();

            if (!ModelState.IsValid)
            {
                var usuarios = await _context.Users.OrderBy(e => e.Nome).Select(e => new SelectListItem(e.Nome, e.Id)).ToListAsync();
                model.UsuariosDisponiveis = usuarios;
            }
            else
            {
                try
                {
                    Rendimento rendimento = await _context.Rendimentos.SingleAsync(e=>e.Id == id);
                    rendimento.Atualizar(model);
                    _context.Update(rendimento);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RendimentoExists(model.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)  return NotFound(); 

            var rendimento = await _context.Rendimentos.FirstOrDefaultAsync(m => m.Id == id);
            if (rendimento == null)  return NotFound();

            RendimentoViewModel model = rendimento;
            return View(model);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var rendimento = await _context.Rendimentos.SingleAsync(e => e.Id == id);
            _context.Rendimentos.Remove(rendimento);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool RendimentoExists(int id)
        {
            return _context.Rendimentos.Any(e => e.Id == id);
        }

        private async Task<string> ObterUsuarioLogadoId()
        {
            string usuarioLogado = User?.Identity?.Name ?? string.Empty;

            return await _context.Users
                .Where(e => e.UserName == usuarioLogado)
                .Select(e => e.Id)
                .SingleOrDefaultAsync() ?? string.Empty;
        }

        private async Task<List<SelectListItem>> ObterSelectListUsuarios(string usuarioSelecionadoId = "")
        {
            return await _context.Users
                .OrderBy(e => e.Nome)
                .Select(e => new SelectListItem(e.Nome ?? e.UserName ?? string.Empty, e.Id, e.Id == usuarioSelecionadoId))
                .ToListAsync();
        }

        private IQueryable<Rendimento> AplicarFiltroRendimentos(IQueryable<Rendimento> query, string termoPesquisa)
        {
            if (string.IsNullOrWhiteSpace(termoPesquisa)) return query;

            termoPesquisa = termoPesquisa.Trim();
            IQueryable<Rendimento> queryFiltrada = query.Where(rendimentoAtual =>
                rendimentoAtual.Titulo.Contains(termoPesquisa) ||
                rendimentoAtual.Descritivo.Contains(termoPesquisa));

            if (decimal.TryParse(termoPesquisa, NumberStyles.Number, CultureInfo.GetCultureInfo("pt-BR"), out decimal valorPtBr) ||
                decimal.TryParse(termoPesquisa, NumberStyles.Number, CultureInfo.InvariantCulture, out valorPtBr))
            {
                queryFiltrada = queryFiltrada.Union(query.Where(rendimentoAtual => rendimentoAtual.Valor == valorPtBr));
            }

            if (DateTime.TryParseExact(termoPesquisa, "dd/MM/yyyy", CultureInfo.GetCultureInfo("pt-BR"), DateTimeStyles.None, out DateTime data))
            {
                queryFiltrada = queryFiltrada.Union(query.Where(rendimentoAtual => rendimentoAtual.Data.Date == data.Date));
            }

            return queryFiltrada;
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

        private static IQueryable<Rendimento> OrdenarRendimentos(IQueryable<Rendimento> query, int coluna, string direcao)
        {
            bool descendente = string.Equals(direcao, "desc", StringComparison.OrdinalIgnoreCase);

            return coluna switch
            {
                0 => descendente ? query.OrderByDescending(e => e.Data) : query.OrderBy(e => e.Data),
                1 => descendente ? query.OrderByDescending(e => e.Valor) : query.OrderBy(e => e.Valor),
                2 => descendente ? query.OrderByDescending(e => e.Descritivo) : query.OrderBy(e => e.Descritivo),
                3 => descendente ? query.OrderByDescending(e => e.Titulo) : query.OrderBy(e => e.Titulo),
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

        private sealed record RendimentoListaDataTableItem(
            int Id,
            string Data,
            string Valor,
            string Descritivo,
            string Titulo);
    }
}
