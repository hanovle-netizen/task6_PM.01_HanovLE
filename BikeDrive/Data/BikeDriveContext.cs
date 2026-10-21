using System.IO;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace BikeDrive.Data;

public class BikeDriveContext : DbContext
{
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Manufacturer> Manufacturers => Set<Manufacturer>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<PickupPoint> PickupPoints => Set<PickupPoint>();
    public DbSet<OrderStatus> OrderStatuses => Set<OrderStatus>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(ReadConnectionString());
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().ToTable("Role");
        modelBuilder.Entity<User>().ToTable("User");
        modelBuilder.Entity<Unit>().ToTable("Unit");
        modelBuilder.Entity<Supplier>().ToTable("Supplier");
        modelBuilder.Entity<Manufacturer>().ToTable("Manufacturer");
        modelBuilder.Entity<Category>().ToTable("Category");
        modelBuilder.Entity<Product>().ToTable("Product").HasKey(product => product.Article);
        modelBuilder.Entity<Product>().Property(product => product.Price).HasColumnType("decimal(10,2)");
        modelBuilder.Entity<PickupPoint>().ToTable("PickupPoint");
        modelBuilder.Entity<OrderStatus>().ToTable("OrderStatus").HasKey(status => status.StatusId);
        modelBuilder.Entity<Order>().ToTable("Order").Property(order => order.OrderId).ValueGeneratedNever();
        modelBuilder.Entity<OrderItem>().ToTable("OrderItem").HasKey(item => new { item.OrderId, item.Article });
        modelBuilder.Entity<OrderItem>()
            .HasOne(item => item.Product)
            .WithMany()
            .HasForeignKey(item => item.Article);
    }

    // Строку подключения можно переопределить переменной окружения BIKEDRIVE_CONNECTION
    private static string ReadConnectionString()
    {
        string? fromEnvironment = Environment.GetEnvironmentVariable("BIKEDRIVE_CONNECTION");
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return fromEnvironment;
        }

        string settingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        using JsonDocument settings = JsonDocument.Parse(File.ReadAllText(settingsPath));
        return settings.RootElement.GetProperty("ConnectionString").GetString()!;
    }
}
