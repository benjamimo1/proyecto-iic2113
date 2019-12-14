using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using proyecto_iic2113.Data;
using proyecto_iic2113.Models;

namespace proyecto_iic2113.Helpers
{
    public class AverageCalculator
    {
        private ApplicationDbContext _context;

        public AverageCalculator(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<double> CalculateEventAverageAsync(int? eventId)
        {
            var reviews = await _context.Reviews
                .Where(r => r.EventId == eventId)
                .ToListAsync();

            var ratings = reviews.Select(x => x.Rating).ToList();
            var averageRating = ratings.Count > 0 ? ratings.Average() : 0.0;
            return averageRating;
        }
    }
}
