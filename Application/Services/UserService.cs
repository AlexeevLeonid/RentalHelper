using Microsoft.EntityFrameworkCore;
using RentalHelper.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class UserService : ServiceBase
    {
        public UserService(AppDbContext context) : base(context)
        {
        }

        public async Task AddNewUserAsync(NewUser user)
        {
            await context.NewUsers.AddAsync(user);
            context.SaveChanges();
        }
        public async Task<User> GetUserByIdAsync(long id)
        {
            User user = null;
            
            if (user == null)
                user = await context.Admins.FirstOrDefaultAsync(x => x.TelegramId == id);
            if (user == null)
                user = await context.Workers.FirstOrDefaultAsync(x => x.TelegramId == id);
            if (user == null)
                user = await context.Tenants.FirstOrDefaultAsync(x => x.TelegramId == id);
            
            if (user == null)
                await context.NewUsers.FirstOrDefaultAsync(x => x.TelegramId == id);

            if (user == null) throw new ArgumentException("User not found");
            return user;
        }

        public async Task<Admin> GetAdminByIdAsync(long id)
        {
            return await context.Admins.FirstAsync(x => x.TelegramId == id);
        }
        public async Task<Worker> GetWorkerByIdAsync(long id)
        {
            return await context.Workers.
                Include(x => x.Requests).ThenInclude(x => x.CreatedBy).
                FirstAsync(x => x.TelegramId == id);
        }
        public async Task<Tenant> GetTenantByIdAsync(long id)
        {
            return await context.Tenants.Include(x => x.Bookings).
                Include(x => x.Vehicles).
                Include(x => x.Rooms).
                Include(x => x.Requests).ThenInclude(x => x.AssignedTo).
                FirstAsync(x => x.TelegramId == id);
        }

        public async Task<List<NewUser>> GetNewUsersAsync()
        {
            return await context.NewUsers.
                ToListAsync();
        }

        public async Task<User> AssignRoleAsync(NewUser user, Role role)
        {
            User newUser = null;
            if (role == Role.Арендатор)
            {
                newUser = new Tenant()
                {
                    Name = user.Name,
                    TelegramId = user.TelegramId,
                    UserState = uState.Idle,
                    Role = role
                };
                await context.Tenants.AddAsync(newUser as Tenant);
            }
            else if (role == Role.Сотрудник)
            {
                newUser = new Worker()
                {
                    Name = user.Name,
                    TelegramId = user.TelegramId,
                    UserState = uState.Idle,
                    Role = role
                };
                await context.Workers.AddAsync(newUser as Worker);
            }
            else throw new ArgumentException($"Невозможно присвоить следующую роль {role.ToString()}");
            context.NewUsers.Remove(user);
            await context.SaveChangesAsync();
            return newUser;
        }

        public async Task<List<Tenant>> GetTenantsAsync()
        {
            return await context.Tenants.
                Include(x => x.Bookings).
                Include(x => x.Vehicles).
                Include(x => x.Rooms).
                Include(x => x.Requests).ThenInclude(x => x.AssignedTo).
                ToListAsync();
        }

        public async Task<List<Worker>> GetWorkersAsync()
        {
            return await context.Workers.
                Include(x => x.Requests).ThenInclude(x => x.CreatedBy).
                ToListAsync();
        }

        public async Task<List<Admin>> GetAdminsAsync()
        {
            return await context.Admins.ToListAsync();
        }

        public async Task<List<Worker>> GetFreeWorkersAsync()
        {
            return await context.Workers.
                Where(x => x.Requests.Any(x => x.Status == Status.Выполняется)).
                ToListAsync();
        }

        public async Task SetUserState(User user, uState state)
        {
            user.UserState = state;
            await context.SaveChangesAsync();
        }
    }
}
