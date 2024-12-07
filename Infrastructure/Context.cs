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

    public static void UseDbContext(IServiceCollection c)
    {
        c.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase("YourConnectionStringHere"));
    }
    
}
