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
    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ChatController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Chat
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Chat.Include(chat => chat.Moderator)
                .Include(chat => chat.Conference)
                .ThenInclude(conference => conference.Organizer);
            var user = await GetCurrentUserAsync();
            var userId = user?.Id;
            ViewBag.UserId = userId;
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Chat/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var chat = await _context.Chat
                .Include(c => c.Moderator)
                .Include(c => c.Conference)
                .ThenInclude(conference => conference.Organizer)
                .FirstOrDefaultAsync(m => m.Id == id);

            var eventAttendees = await _context.EventUserAttendees
                .Where(t => t.EventId == id)
                .ToListAsync();
            ViewBag.numberOfAttendees = eventAttendees.Count;

            var user = await GetCurrentUserAsync();
            var userId = user?.Id;
            ViewBag.UserId = userId;

            var attendanceHelper = new AttendanceHelper(_context);
            ViewBag.isUserAttendingEvent = user != null ? await attendanceHelper.IsUserAttendingEvent(user, chat) : false;

            if (chat == null)
            {
                return NotFound();
            }

            return View(chat);
        }

        // GET: Chat/Create
        public IActionResult Create(int id)
        {
            ViewData["ConferenceId"] = new SelectList(_context.Conferences, "Id", "Name");
            ViewData["ApplicationUserId"] = new SelectList(_context.Users, "Id", "Email");
            ViewBag.ConferenceId = id;
            return View();
        }

        // POST: Chat/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ApplicationUserId,Name,StartDate,EndDate,Description,ConferenceId,Capacity")] Chat chat)
        {
            if (ModelState.IsValid)
            {
                _context.Add(chat);
                await _context.SaveChangesAsync();
                return RedirectToAction("Details", "Conference", new { id = chat.ConferenceId });
            }
            ViewData["ConferenceId"] = new SelectList(_context.Conferences, "Id", "Name", chat.ConferenceId);
            ViewData["ApplicationUserId"] = new SelectList(_context.Users, "Id", "Id", chat.ApplicationUserId);
            return View(chat);
        }

        // GET: Chat/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var chat = await _context.Chat
                .Include(c => c.Conference)
                .ThenInclude(conference => conference.Organizer)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (chat == null)
            {
                return NotFound();
            }
            var user = await GetCurrentUserAsync();
            if (user.Id != chat.Conference.Organizer.Id)
            {
                return RedirectToAction(nameof(Index));
            }

            ViewData["ConferenceId"] = new SelectList(_context.Conferences, "Id", "Name", chat.ConferenceId);
            ViewData["ApplicationUserId"] = new SelectList(_context.Users, "Id", "Id", chat.ApplicationUserId);
            return View(chat);
        }

        // POST: Chat/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ApplicationUserId,Id,Name,StartDate,EndDate,Description,ConferenceId,Capacity")] Chat chat)
        {
            if (id != chat.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(chat);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ChatExists(chat.Id))
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
            ViewData["ConferenceId"] = new SelectList(_context.Conferences, "Id", "Name", chat.ConferenceId);
            ViewData["ApplicationUserId"] = new SelectList(_context.Users, "Id", "Id", chat.ApplicationUserId);
            return View(chat);
        }

        // GET: Chat/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var chat = await _context.Chat
                .Include(c => c.Conference)
                .Include(c => c.Moderator)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (chat == null)
            {
                return NotFound();
            }

            return View(chat);
        }

        // POST: Chat/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var chat = await _context.Chat.FindAsync(id);
            _context.Chat.Remove(chat);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ChatExists(int id)
        {
            return _context.Chat.Any(e => e.Id == id);
        }
        private Task<ApplicationUser> GetCurrentUserAsync() => _userManager.GetUserAsync(HttpContext.User);
    }
}
