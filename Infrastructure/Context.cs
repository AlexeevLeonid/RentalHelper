using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ValueGeneration.Internal;
using RentalHelper.Domain;
using System.Data;

using Microsoft.Extensions.DependencyInjection;

public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Request> Requests { get; set; }
    public DbSet<Vehicle> Vehicles { get; set; }
    public DbSet<Booking> Bookings { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasKey(x => x.TelegramId);
        modelBuilder.Entity<User>()
            .HasMany<Request>(u => u.Requests)
            .WithOne(r => r.CreatedBy)
            .HasForeignKey(r => r.CreatedById);

        modelBuilder.Entity<User>()
            .HasMany(u => u.Vehicles)
            .WithOne(r => r.User)
            .HasForeignKey(r => r.UserId);

        modelBuilder.Entity<Vehicle>()
            .HasOne(u => u.User)
            .WithMany(r => r.Vehicles)
            .HasForeignKey(r => r.UserId);

        modelBuilder.Entity<Request>()
            .HasOne<User>(r => r.AssignedTo)
            .WithMany()
            .HasForeignKey(r => r.AssignedToId);

        modelBuilder.Entity<Booking>()
            .HasOne<User>(r => r.User)
            .WithMany(u => u.Bookings)
            .HasForeignKey(r => r.UserId);
    }

    public static void Seed(AppDbContext context)
    {
        if (!context.Users.Any())
        {
            context.Users.AddRange(
                new User { Name = "@centerhades", TelegramId = 663509662, Role = Role.Сотрудник, UserState = uState.Idle }
            );
        }
        // Проверяем, есть ли данные в базе, чтобы не засевать их заново
        if (!context.Vehicles.Any())
        {
            context.Vehicles.AddRange(
                new Vehicle { PlateNumber = "ABC123", IsPaid = true, IsOneTime = false, UserId = 663509662 },
                new Vehicle { PlateNumber = "XYZ456", IsPaid = false, IsOneTime = true, UserId = 663509662 }
            );
        }

        if (!context.Bookings.Any())
        {
            context.Bookings.AddRange(
                new Booking { Date = DateTime.Now.AddDays(1), UserId = 663509662 },
                new Booking { Date = DateTime.Now.AddDays(2), UserId = 663509662 }
            );
        }

        if (!context.Requests.Any())
        {
            context.Requests.AddRange(
                new Request { Description = "Петрович врубай насос", CreatedById = 663509662, Status = Status.Новая },
                new Request { Description = "резать чуррос", CreatedById = 663509662, Status = Status.Выполняется }
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
