using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

using Core.Flash;

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
    public class ConferenceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IFlasher _flasher;

        public ConferenceController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IFlasher flasher)
        {
            _context = context;
            _userManager = userManager;
            _flasher = flasher;
        }

        // GET: Conference
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Conferences.Include(c => c.Venue).Include(c => c.Organizer);
            var user = await GetCurrentUserAsync();
            var userId = user?.Id;
            ViewBag.UserId = userId;
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Conference/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var conference = await _context.Conferences
                .Include(c => c.Organizer)
                .Include(c => c.Sponsors)
                .Include(c => c.Franchise)
                .Include(c => c.Venue)
                .Include(c => c.Launches)
                .Include(c => c.Workshops)
                .Include(c => c.Parties)
                .Include(c => c.Talks)
                .Include(c => c.Chats)
                .Include(c => c.ConferenceUserAttendees)
                .ThenInclude(conferenceUserAttendee => conferenceUserAttendee.UserAttendee)
                .FirstOrDefaultAsync(m => m.Id == id);

            var sponsors = await _context.Sponsors.Where(s => s.ConferenceId == id).ToListAsync();
            var attendees = await _context.ConferenceUserAttendees.Where(s => s.ConferenceId == id).ToListAsync();

            ViewBag.Sponsors = sponsors;
            ViewBag.Attendees = attendees;

            var chats = await _context.Chat.Where(e => e.ConferenceId == id).ToListAsync();
            var parties = await _context.Parties.Where(e => e.ConferenceId == id).ToListAsync();
            var workshops = await _context.Workshops.Where(e => e.ConferenceId == id).ToListAsync();
            var launches = await _context.Launches.Where(e => e.ConferenceId == id).ToListAsync();
            var talks = await _context.Talks.Where(e => e.ConferenceId == id).ToListAsync();

            ViewBag.Chats = chats;
            ViewBag.Parties = parties;
            ViewBag.Workshops = workshops;
            ViewBag.Launches = launches;
            ViewBag.Talks = talks;

            var averageCalculator = new AverageCalculator(_context);
            var ChatsAttendees = await averageCalculator.CalculateNumberOfEventAttendeesAsync(chats);
            ViewBag.ChatsAttendees = ChatsAttendees;
            var PartiessAttendees = await averageCalculator.CalculateNumberOfEventAttendeesAsync(parties);
            ViewBag.PartiessAttendees = PartiessAttendees;
            var WorkshopsAttendees = await averageCalculator.CalculateNumberOfEventAttendeesAsync(workshops);
            ViewBag.WorkshopsAttendees = WorkshopsAttendees;
            var LaunchesAttendees = await averageCalculator.CalculateNumberOfEventAttendeesAsync(launches);
            ViewBag.LaunchesAttendees = LaunchesAttendees;
            var TalksAttendees = await averageCalculator.CalculateNumberOfEventAttendeesAsync(talks);
            ViewBag.TalksAttendees = TalksAttendees;

            var user = await GetCurrentUserAsync();
            var userId = user?.Id;
            ViewBag.UserId = userId;

            if (conference == null)
            {
                return NotFound();
            }

            return View(conference);
        }

        // GET: Conference/Create
        [Authorize]
        public async Task<IActionResult> Create()
        {
            var currentUser = await GetCurrentUserAsync();

            ViewData["VenueId"] = new SelectList(_context.Venues, "Id", "Name");
            ViewData["FranchiseId"] = new SelectList(_context.Franchise.Where(f => f.Organizer.Id == currentUser.Id), "Id", "Name");
            return View();
        }

        // POST: Conference/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Description,DateTime,EndDate,Capacity,VenueId,FranchiseId")] Conference conference)
        {
            ApplicationUser currentUser = await GetCurrentUserAsync();
            conference.Organizer = currentUser;
            if (ModelState.IsValid)
            {
                _context.Add(conference);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["VenueId"] = new SelectList(_context.Venues, "Id", "Name", conference.VenueId);
            ViewData["FranchiseId"] = new SelectList(_context.Franchise.Where(f => f.Organizer.Id == currentUser.Id), "Id", "Name", conference.FranchiseId);
            return View(conference);
        }

        // GET: Conference/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            var currentUser = await GetCurrentUserAsync();

            if (id == null)
            {
                return NotFound();
            }

            var conference = await _context.Conferences
                .Include(c => c.Organizer)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (conference == null)
            {
                return NotFound();
            }

            var user = await GetCurrentUserAsync();

            if (user.Id != conference.Organizer.Id)
            {
                return RedirectToAction(nameof(Index));
            }
            ViewData["VenueId"] = new SelectList(_context.Venues, "Id", "Name", conference.VenueId);
            ViewData["FranchiseId"] = new SelectList(_context.Franchise.Where(f => f.Organizer.Id == currentUser.Id), "Id", "Name", conference.FranchiseId);
            return View(conference);
        }

        // POST: Conference/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,DateTime,EndDate,Capacity,VenueId,FranchiseId")] Conference conference)
        {
            if (id != conference.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(conference);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ConferenceExists(conference.Id))
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
            ViewData["VenueId"] = new SelectList(_context.Venues, "Id", "Name", conference.VenueId);
            return View(conference);
        }

        // GET: Conference/Dashboard/5
        public async Task<IActionResult> Dashboard(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var conference = await _context.Conferences
                .FirstOrDefaultAsync(m => m.Id == id);

            if (conference == null)
            {
                return NotFound();
            }

            var events = await _context.Events
                .Where(e => e.ConferenceId == id)
                .ToListAsync();

            var averageCalculator = new AverageCalculator(_context);
            var averageRating = await averageCalculator.CalculateConferenceAverageAsync(id);
            ViewBag.averageRating = averageRating;

            var eventsAverageReviews = events
                .Select(async e => await averageCalculator.CalculateEventAverageAsync(e.Id))
                .Select(task => task.Result)
                .ToList();

            ViewBag.eventsAverageReviews = eventsAverageReviews;
            ViewBag.events = events;

            return View(conference);
        }

        // GET: Conference/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var conference = await _context.Conferences
                .Include(c => c.Venue)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (conference == null)
            {
                return NotFound();
            }

            return View(conference);
        }

        // POST: Conference/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var conference = await _context.Conferences.FindAsync(id);
            _context.Conferences.Remove(conference);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AttendConference(int id)
        {

            var conference = await _context.Conferences.FindAsync(id);
            var currentUser = await GetCurrentUserAsync();
            var existingConferenceUserAttendee = await _context.ConferenceUserAttendees.SingleOrDefaultAsync(m => m.ConferenceId == conference.Id && m.ApplicationUserId == currentUser.Id);

            // Check if user is already attending this conference
            if (existingConferenceUserAttendee != null)
            {
                _flasher.Flash("Danger", "You are already attending this conference");
            }
            else
            {
                var conferenceUserAttendee = new ConferenceUserAttendee();
                conferenceUserAttendee.UserAttendee = currentUser;
                conferenceUserAttendee.Conference = conference;

                _context.ConferenceUserAttendees.Add(conferenceUserAttendee);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id = id });
        }

        private bool ConferenceExists(int id)
        {
            return _context.Conferences.Any(e => e.Id == id);
        }

        [HttpGet]
        [Route("Conference/Details/{id}/CreateNotification")]
        public IActionResult CreateNotification(int? id)
        {
            ViewBag.ConferenceId = id;
            return View();
        }

        // POST: Launch/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Conference/Details/{id}/CreateNotification")]
        public async Task<IActionResult> CreateNotification([Bind("Body,ConferenceId")] Notifications notification)
        {
            var attendees = await _context.ConferenceUserAttendees.Where(s => s.ConferenceId == notification.ConferenceId).ToListAsync();
            ViewBag.Attendees = attendees;

            if (ModelState.IsValid)
            {
                foreach (var attendee in ViewBag.Attendees)
                {
                    var userNotification = new Notifications();
                    userNotification.ApplicationUserId = attendee.ApplicationUserId;
                    userNotification.ConferenceId = notification.ConferenceId;
                    userNotification.Body = notification.Body;
                    userNotification.Date = DateTime.Now;
                    _context.Notifications.Add(userNotification);
                    await _context.SaveChangesAsync();

                }
                return RedirectToAction("Details", "Conference", new { id = notification.ConferenceId });
            }
            return View(notification);
        }

        private Task<ApplicationUser> GetCurrentUserAsync() => _userManager.GetUserAsync(HttpContext.User);

    }
}
