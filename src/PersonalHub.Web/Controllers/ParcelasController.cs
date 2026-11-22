using PersonalHub.Domain.Entidades;
using PersonalHub.Infrastructure.Data;
using PersonalHub.Web.Models.Resultados;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PersonalHub.Web.Controllers
{
    [Authorize]
    public class ParcelasController : Controller
    {
        private readonly PersonalHubContext _context;
        
        public ParcelasController(PersonalHubContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> MarcarComoPaga(int parcelaId = 0, bool pago = false, int ocorrenciaId = 0)
        {
            if (ocorrenciaId > 0)
            {
                OcorrenciaDespesaRecorrente ocorrencia = await _context.OcorrenciasDespesasRecorrentes
                    .SingleAsync(e => e.Id == ocorrenciaId);

                if (pago)
                    ocorrencia.MarcarComoPaga();
                else
                    ocorrencia.MarcarComoNaoPaga();

                await _context.SaveChangesAsync();
                return Json(new ResultadoSucesso());
            }

            if (parcelaId <= 0)
                return BadRequest();

            Parcela parcela = await _context.Parcelas.SingleAsync(e => e.Id == parcelaId);

            if (pago)
                parcela.MarcarComoPaga();
            else
                parcela.MarcarComoNaoPaga();

            await _context.SaveChangesAsync();
            return Json(new ResultadoSucesso());
        }

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
