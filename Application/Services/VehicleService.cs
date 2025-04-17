using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;
using RentalHelper.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace Application.Services
{
    public class VehicleService : ServiceBase
    {
        private static Dictionary<long, int> vehicles = new Dictionary<long, int>();
        public VehicleService(AppDbContext context) : base(context)
        {
        }
        

        public async Task<Vehicle> GetVehicleByIdAsync(int id)
        {
            return await context.Vehicles.FirstAsync(x => x.Id == id);
        }

        public async Task<List<Vehicle>> GetVehiclesAsync()
        {
            return await context.Vehicles.ToListAsync();
        }

        public async Task AddVehicleAccessAsync(Vehicle v)
        {
            await context.Vehicles.AddAsync(v);
            await context.SaveChangesAsync();
        }

        public async Task<(long, string)> RevokeVehicleAccessByIdAsync(int id)
        {
            var v = await GetVehicleByIdAsync(id);
            var responce = (v.UserId, v.PlateNumber);
            context.Vehicles.Remove(v);
            return responce;
        }

        public void PrepareVehicle(long userid, int id)
        {
            vehicles[userid] = id;
        }

        public async Task<(string, long)> SetVehiclePrice(long userId, int price)
        {
            var vehicle = await GetVehicleByIdAsync(vehicles[userId]);
            vehicle.Price = price;
            return (vehicle.PlateNumber, vehicle.UserId);
        }

        public async Task<List<Vehicle>> GetUserVehiclesByIdAsync(long id)
        {
            return await context.Vehicles.Where(x => x.UserId == id).ToListAsync();
        }
    }
}
