using Microsoft.EntityFrameworkCore;
using RentalHelper.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Telegram.Bot.Types;

namespace Application.Services
{
    public class BookingService : ServiceBase
    {
        public BookingService(AppDbContext context) : base(context)
        {
        }

        public async Task<List<DateTime>> GetAviableDates()
        {
            var bookedDates = await context.Bookings
                    .Where(b => b.Date >= DateTime.Now.Date)
                    .Select(b => b.Date)
                    .ToListAsync();
            var availableDays = new List<DateTime>();
            var currentDate = DateTime.Now.Date;

            while (availableDays.Count < 10)
            {
                // Если текущая дата не забронирована, добавляем её в список
                if (!bookedDates.Contains(currentDate))
                {
                    availableDays.Add(currentDate);
                }

                // Переход к следующему дню
                currentDate = currentDate.AddDays(1);
            }
            return availableDays;
        }

        public async Task BookDateAsync(DateTime dateTime, long id)
        {
            await context.Bookings.AddAsync(new Booking()
            {
                Date = dateTime,
                UserId = id,
            });
        }
    }
}
