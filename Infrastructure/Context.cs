using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ValueGeneration.Internal;
using RentalHelper.Domain;
using System.Data;

using Microsoft.Extensions.DependencyInjection;

public class AppDbContext : DbContext
{
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<Room> Rooms { get; set; }
    public DbSet<Worker> Workers { get; set; }
    public DbSet<NewUser> NewUsers { get; set; }
    public DbSet<Admin> Admins { get; set; }
    public DbSet<Request> Requests { get; set; }
    public DbSet<Vehicle> Vehicles { get; set; }
    public DbSet<Booking> Bookings { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>()
            .HasKey(x => x.TelegramId);
        modelBuilder.Entity<Worker>()
            .HasKey(x => x.TelegramId);
        modelBuilder.Entity<Admin>()
            .HasKey(x => x.TelegramId);
        modelBuilder.Entity<NewUser>()
            .HasKey(x => x.TelegramId);

        modelBuilder.Entity<Tenant>()
            .HasMany<Request>(u => u.Requests)
            .WithOne(r => r.CreatedBy)
            .HasForeignKey(r => r.CreatedById);

        modelBuilder.Entity<Tenant>()
            .HasMany(u => u.Vehicles)
            .WithOne(r => r.User)
            .HasForeignKey(r => r.UserId);

        modelBuilder.Entity<Tenant>()
            .HasMany(u => u.Rooms)
            .WithOne(r => r.Tenant)
            .HasForeignKey(r => r.TenantId);

        modelBuilder.Entity<Vehicle>()
            .HasOne(u => u.User)
            .WithMany(r => r.Vehicles)
            .HasForeignKey(r => r.UserId);

        modelBuilder.Entity<Request>()
            .HasOne(r => r.AssignedTo)
            .WithMany()
            .HasForeignKey(r => r.AssignedToId);

        modelBuilder.Entity<Room>().
            HasMany(x => x.Requests).
            WithOne(x => x.Room).
            HasForeignKey(r => r.RoomId);

        modelBuilder.Entity<Booking>()
            .HasOne(r => r.User)
            .WithMany(u => u.Bookings)
            .HasForeignKey(r => r.UserId);
    }

    public static void Seed(AppDbContext context)
    {
        long id = 663509662;
        if (!context.Workers.Any())
        {
            context.Workers.AddRange(
                new Worker { Name = "@centerhades", TelegramId = id, Role = Role.Сотрудник, UserState = uState.Idle }
            );
        }
        if (!context.Tenants.Any())
        {
            context.Tenants.AddRange(
                new Tenant { Name = "@centerhades", TelegramId = id, Role = Role.Арендатор, UserState = uState.Idle }
            );
        }
        if (!context.Admins.Any())
        {
            context.Admins.AddRange(
                new Admin { Name = "@centerhades", TelegramId = id, Role = Role.Админ, UserState = uState.Idle }
            );
        }
        // Проверяем, есть ли данные в базе, чтобы не засевать их заново
        if (!context.Vehicles.Any())
        {
            context.Vehicles.AddRange(
                new Vehicle { PlateNumber = "ABC123", IsPaid = true, IsOneTime = false, UserId = id },
                new Vehicle { PlateNumber = "XYZ456", IsPaid = false, IsOneTime = true, UserId = id }
            );
        }

        if (!context.Bookings.Any())
        {
            context.Bookings.AddRange(
                new Booking { Date = DateTime.Now.AddDays(1), UserId = id },
                new Booking { Date = DateTime.Now.AddDays(2), UserId = id }
            );
        }

        if (!context.Requests.Any())
        {
            context.Requests.AddRange(
                new Request { Description = "Петрович врубай насос", CreatedById = id, Status = Status.Новая },
                new Request { Description = "резать чуррос", CreatedById = id, AssignedToId = id, Status = Status.Выполняется }
            );
        }

        // Сохраняем изменения в базе данных
        context.SaveChanges();
    }

    public static void UseDbContext(IServiceCollection c)
    {
        c.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase("YourConnectionStringHere"));
    }
    
}
