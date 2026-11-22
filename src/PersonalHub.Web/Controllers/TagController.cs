using PersonalHub.Domain.Entidades;
using PersonalHub.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PersonalHub.Web.Controllers
{
    public class TagController: Controller
    {
        private readonly PersonalHubContext _context;
        
        public TagController(PersonalHubContext context)
        {
            this._context = context;
        }

        public async Task<IActionResult> ObterTodas(string query)
        {
            IQueryable<Tag> tagsQuery;

            if (string.IsNullOrEmpty(query))
                tagsQuery = _context.Tags;
            else
                tagsQuery = _context.Tags.Where(e => e.Nome.Contains(query));
            
            var tags = await tagsQuery.Select(tagAtual => new { value = tagAtual.Nome, label = tagAtual.Nome }).ToListAsync();

            return Json(tags);
        }
    }
}
