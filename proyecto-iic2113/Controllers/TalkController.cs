using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using proyecto_iic2113.Data;
using proyecto_iic2113.Helpers;
using proyecto_iic2113.Models;

namespace proyecto_iic2113.Controllers
{
    public class TalkController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TalkController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Talk
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Talks.Include(t => t.Conference).ThenInclude(c => c.Organizer);
            var user = await GetCurrentUserAsync();
            var userId = user?.Id;
            ViewBag.UserId = userId;
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Talk/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var talk = await _context.Talks
                .Include(t => t.TalkLecturers)
                .ThenInclude(tl => tl.Lecturer)
                .Include(t => t.Conference)
                .ThenInclude(conference => conference.Organizer)
                .FirstOrDefaultAsync(m => m.Id == id);

            var eventAttendees = await _context.EventUserAttendees
                .Where(t => t.EventId == id)
                .ToListAsync();
            ViewBag.numberOfAttendees = eventAttendees.Count;

            var user = await GetCurrentUserAsync();
            var userId = user?.Id;
            ViewBag.UserId = userId;

            ViewBag.Users = new SelectList(_context.Users, "Id", "Email");

            var attendanceHelper = new AttendanceHelper(_context);
            ViewBag.isUserAttendingEvent = user != null ? await attendanceHelper.IsUserAttendingEvent(user, talk) : false;

            if (talk == null)
            {
                return NotFound();
            }

            return View(talk);
        }

        // GET: Talk/Create
        public IActionResult Create(int id)
        {
            ViewData["ConferenceId"] = new SelectList(_context.Conferences, "Id", "Name");
            ViewBag.ConferenceId = id;
            return View();
        }

        // POST: Talk/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Subject,Name,StartDate,EndDate,Description,ConferenceId,Capacity")] Talk talk)
        {
            if (ModelState.IsValid)
            {
                _context.Add(talk);
                await _context.SaveChangesAsync();
                return RedirectToAction("Details", "Conference", new { id = talk.ConferenceId });
            }
            ViewData["ConferenceId"] = new SelectList(_context.Conferences, "Id", "Name", talk.ConferenceId);
            return View(talk);
        }

        // GET: Talk/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var talk = await _context.Talks
                .Include(t => t.Conference)
                .ThenInclude(conference => conference.Organizer)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (talk == null)
            {
                return NotFound();
            }
            var user = await GetCurrentUserAsync();
            if (user.Id != talk.Conference.Organizer.Id)
            {
                return RedirectToAction(nameof(Index));
            }
            ViewData["ConferenceId"] = new SelectList(_context.Conferences, "Id", "Name", talk.ConferenceId);
            return View(talk);
        }

        // POST: Talk/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Subject,Id,Name,StartDate,EndDate,Description,ConferenceId,Capacity")] Talk talk)
        {
            if (id != talk.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(talk);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TalkExists(talk.Id))
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
            ViewData["ConferenceId"] = new SelectList(_context.Conferences, "Id", "Name", talk.ConferenceId);
            return View(talk);
        }

        // GET: Talk/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var talk = await _context.Talks
                .Include(t => t.Conference)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (talk == null)
            {
                return NotFound();
            }

            return View(talk);
        }

        // POST: Talk/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var talk = await _context.Talks.FindAsync(id);
            _context.Talks.Remove(talk);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TalkExists(int id)
        {
            return _context.Talks.Any(e => e.Id == id);
        }

        private Task<ApplicationUser> GetCurrentUserAsync() => _userManager.GetUserAsync(HttpContext.User);
    }
}
