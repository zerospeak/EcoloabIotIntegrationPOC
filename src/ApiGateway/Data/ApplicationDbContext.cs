using Microsoft.EntityFrameworkCore;
using ApiGateway.Models;

namespace ApiGateway.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<IoTDevice> Devices { get; set; }
        public DbSet<TrapEvent> Events { get; set; }
        public DbSet<Location> Locations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships and constraints
            modelBuilder.Entity<IoTDevice>()
                .HasIndex(d => d.DeviceId)
                .IsUnique();

            modelBuilder.Entity<TrapEvent>()
                .HasIndex(e => e.EventId)
                .IsUnique();

            modelBuilder.Entity<Location>()
                .HasIndex(l => l.LocationId)
                .IsUnique();

            // Seed some sample data for development
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Sample locations (Starbucks and McDonald's)
            modelBuilder.Entity<Location>().HasData(
                new Location
                {
                    LocationId = "LOC-SB-001",
                    LocationName = "Starbucks - Downtown",
                    CustomerId = "CUST-SB",
                    CustomerName = "Starbucks Corporation",
                    Address = "123 Main Street",
                    City = "Chicago",
                    State = "IL",
                    ZipCode = "60601",
                    Country = "USA",
                    Latitude = 41.8781,
                    Longitude = -87.6298,
                    Type = LocationType.CoffeeShop,
                    TotalDevices = 5,
                    LastActivityDate = DateTime.Now.AddDays(-1),
                    IsActive = true
                },
                new Location
                {
                    LocationId = "LOC-SB-002",
                    LocationName = "Starbucks - North Side",
                    CustomerId = "CUST-SB",
                    CustomerName = "Starbucks Corporation",
                    Address = "456 North Avenue",
                    City = "Chicago",
                    State = "IL",
                    ZipCode = "60614",
                    Country = "USA",
                    Latitude = 41.9100,
                    Longitude = -87.6770,
                    Type = LocationType.CoffeeShop,
                    TotalDevices = 4,
                    LastActivityDate = DateTime.Now.AddDays(-2),
                    IsActive = true
                },
                new Location
                {
                    LocationId = "LOC-MCD-001",
                    LocationName = "McDonald's - Loop",
                    CustomerId = "CUST-MCD",
                    CustomerName = "McDonald's Corporation",
                    Address = "789 State Street",
                    City = "Chicago",
                    State = "IL",
                    ZipCode = "60605",
                    Country = "USA",
                    Latitude = 41.8757,
                    Longitude = -87.6243,
                    Type = LocationType.Restaurant,
                    TotalDevices = 6,
                    LastActivityDate = DateTime.Now.AddHours(-12),
                    IsActive = true
                },
                new Location
                {
                    LocationId = "LOC-MCD-002",
                    LocationName = "McDonald's - West Side",
                    CustomerId = "CUST-MCD",
                    CustomerName = "McDonald's Corporation",
                    Address = "101 West Madison",
                    City = "Chicago",
                    State = "IL",
                    ZipCode = "60607",
                    Country = "USA",
                    Latitude = 41.8820,
                    Longitude = -87.6800,
                    Type = LocationType.Restaurant,
                    TotalDevices = 5,
                    LastActivityDate = DateTime.Now.AddHours(-6),
                    IsActive = true
                }
            );

            // Sample IoT devices (mouse traps)
            modelBuilder.Entity<IoTDevice>().HasData(
                new IoTDevice
                {
                    DeviceId = "TRAP-SB-001-01",
                    DeviceName = "Kitchen Trap 1",
                    DeviceType = DeviceType.MouseTrap,
                    LocationId = "LOC-SB-001",
                    LocationName = "Starbucks - Downtown",
                    CustomerName = "Starbucks Corporation",
                    Latitude = 41.8781,
                    Longitude = -87.6298,
                    InstallationDate = DateTime.Now.AddMonths(-6),
                    LastMaintenanceDate = DateTime.Now.AddDays(-30),
                    LastCommunicationDate = DateTime.Now.AddHours(-2),
                    BatteryLevel = 85,
                    Status = DeviceStatus.Active,
                    FirmwareVersion = "v2.1.0",
                    IsActive = true
                },
                new IoTDevice
                {
                    DeviceId = "TRAP-SB-001-02",
                    DeviceName = "Storage Room Trap 1",
                    DeviceType = DeviceType.MouseTrap,
                    LocationId = "LOC-SB-001",
                    LocationName = "Starbucks - Downtown",
                    CustomerName = "Starbucks Corporation",
                    Latitude = 41.8781,
                    Longitude = -87.6298,
                    InstallationDate = DateTime.Now.AddMonths(-6),
                    LastMaintenanceDate = DateTime.Now.AddDays(-30),
                    LastCommunicationDate = DateTime.Now.AddHours(-1),
                    BatteryLevel = 90,
                    Status = DeviceStatus.Active,
                    FirmwareVersion = "v2.1.0",
                    IsActive = true
                },
                new IoTDevice
                {
                    DeviceId = "TRAP-MCD-001-01",
                    DeviceName = "Kitchen Trap 1",
                    DeviceType = DeviceType.MouseTrap,
                    LocationId = "LOC-MCD-001",
                    LocationName = "McDonald's - Loop",
                    CustomerName = "McDonald's Corporation",
                    Latitude = 41.8757,
                    Longitude = -87.6243,
                    InstallationDate = DateTime.Now.AddMonths(-3),
                    LastMaintenanceDate = DateTime.Now.AddDays(-15),
                    LastCommunicationDate = DateTime.Now.AddMinutes(-30),
                    BatteryLevel = 75,
                    Status = DeviceStatus.Triggered,
                    FirmwareVersion = "v2.0.5",
                    IsActive = true
                },
                new IoTDevice
                {
                    DeviceId = "TRAP-MCD-001-02",
                    DeviceName = "Storage Room Trap 1",
                    DeviceType = DeviceType.RatTrap,
                    LocationId = "LOC-MCD-001",
                    LocationName = "McDonald's - Loop",
                    CustomerName = "McDonald's Corporation",
                    Latitude = 41.8757,
                    Longitude = -87.6243,
                    InstallationDate = DateTime.Now.AddMonths(-3),
                    LastMaintenanceDate = DateTime.Now.AddDays(-15),
                    LastCommunicationDate = DateTime.Now.AddHours(-3),
                    BatteryLevel = 45,
                    Status = DeviceStatus.LowBattery,
                    FirmwareVersion = "v2.0.5",
                    IsActive = true
                }
            );

            // Sample trap events
            modelBuilder.Entity<TrapEvent>().HasData(
                new TrapEvent
                {
                    EventId = Guid.NewGuid(),
                    DeviceId = "TRAP-MCD-001-01",
                    Timestamp = DateTime.Now.AddMinutes(-30),
                    EventType = EventType.Activation,
                    LocationId = "LOC-MCD-001",
                    LocationName = "McDonald's - Loop",
                    CustomerName = "McDonald's Corporation",
                    BatteryLevel = 75,
                    AdditionalData = "{\"weight\":12.5,\"motion\":true}",
                    IsProcessed = false,
                    ProcessedTimestamp = null
                },
                new TrapEvent
                {
                    EventId = Guid.NewGuid(),
                    DeviceId = "TRAP-MCD-001-02",
                    Timestamp = DateTime.Now.AddHours(-4),
                    EventType = EventType.BatteryLow,
                    LocationId = "LOC-MCD-001",
                    LocationName = "McDonald's - Loop",
                    CustomerName = "McDonald's Corporation",
                    BatteryLevel = 45,
                    AdditionalData = "{\"voltage\":3.2}",
                    IsProcessed = true,
                    ProcessedTimestamp = DateTime.Now.AddHours(-3.9)
                },
                new TrapEvent
                {
                    EventId = Guid.NewGuid(),
                    DeviceId = "TRAP-SB-001-01",
                    Timestamp = DateTime.Now.AddDays(-1),
                    EventType = EventType.Heartbeat,
                    LocationId = "LOC-SB-001",
                    LocationName = "Starbucks - Downtown",
                    CustomerName = "Starbucks Corporation",
                    BatteryLevel = 85,
                    AdditionalData = "{\"temperature\":22.5,\"humidity\":45}",
                    IsProcessed = true,
                    ProcessedTimestamp = DateTime.Now.AddDays(-1).AddMinutes(1)
                }
            );
        }
    }
}
