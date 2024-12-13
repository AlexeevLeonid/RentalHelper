using Microsoft.EntityFrameworkCore;
using RentalHelper.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class RoomService : ServiceBase
    {
        public RoomService(AppDbContext context) : base(context)
        {
        }

        public async Task<Room> GetRoomByIdAsync(int id)
        {
            return await context.Rooms.FirstAsync(x => x.Id == id);
        }

        public async Task<List<Room>> GetFreeRoomsAsync()
        {
            return await context.Rooms.Where(x => !x.TenantId.HasValue).ToListAsync();
        }

        public async Task AssignRoomAsync(User user, Room room)
        {
            room.TenantId = user.TelegramId;
            await context.SaveChangesAsync();
        }

        public async Task RevokeRoomAsync(User user, Room room)
        {
            room.TenantId = null;
            await context.SaveChangesAsync();
        }
    }
}
