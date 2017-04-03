using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using StreamSurfer.Models;
using Microsoft.EntityFrameworkCore;

namespace StreamSurfer.Controllers
{
    public class ShowController : Controller
    {
        private readonly PostgresDataContext _context;

        public ShowController(PostgresDataContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Detail(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var service = await _context.Services
                .Include(m => m.ShowService)
                .ToListAsync();
            var show = await _context.Shows
                .Include(m => m.ShowService)
                .SingleOrDefaultAsync(m => m.ID == id);
            if (show == null)
            {
                return NotFound();
            }
            return View(show);
        }
    }
}