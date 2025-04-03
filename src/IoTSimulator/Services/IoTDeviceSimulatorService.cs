using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Azure.Messaging.ServiceBus;

namespace IoTSimulator.Services
{
    public class IoTDeviceSimulatorService : BackgroundService
    {
        private readonly ILogger<IoTDeviceSimulatorService> _logger;
        private readonly IConfiguration _configuration;
        private readonly ServiceBusClient _serviceBusClient;
        private readonly ServiceBusSender _sender;
        private readonly Random _random = new Random();
        private readonly string[] _deviceIds = new string[] 
        {
            "TRAP-SB-001-01", "TRAP-SB-001-02", "TRAP-SB-001-03",
            "TRAP-SB-002-01", "TRAP-SB-002-02",
            "TRAP-MCD-001-01", "TRAP-MCD-001-02", "TRAP-MCD-001-03",
            "TRAP-MCD-002-01", "TRAP-MCD-002-02"
        };
        private readonly string[] _locationIds = new string[]
        {
            "LOC-SB-001", "LOC-SB-002", "LOC-MCD-001", "LOC-MCD-002"
        };
        private readonly string[] _locationNames = new string[]
        {
            "Starbucks - Downtown", "Starbucks - North Side", 
            "McDonald's - Loop", "McDonald's - West Side"
        };
        private readonly string[] _customerNames = new string[]
        {
            "Starbucks Corporation", "Starbucks Corporation", 
            "McDonald's Corporation", "McDonald's Corporation"
        };

        public IoTDeviceSimulatorService(
            ILogger<IoTDeviceSimulatorService> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            
            var serviceBusConnection = _configuration.GetValue<string>("ServiceBusConnection");
            _serviceBusClient = new ServiceBusClient(serviceBusConnection);
            _sender = _serviceBusClient.CreateSender("trap-events");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("IoT Device Simulator Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Simulate events from random devices
                    await SimulateDeviceEvents();
                    
                    // Wait for a random interval between 5 and 30 seconds
                    var delay = _random.Next(5000, 30000);
                    _logger.LogInformation($"Waiting {delay/1000} seconds before next simulation cycle.");
                    await Task.Delay(delay, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while simulating device events.");
                    await Task.Delay(5000, stoppingToken);
                }
            }

            _logger.LogInformation("IoT Device Simulator Service is stopping.");
        }

        private async Task SimulateDeviceEvents()
        {
            // Determine how many events to generate (1-3)
            int eventCount = _random.Next(1, 4);
            
            for (int i = 0; i < eventCount; i++)
            {
                // Select a random device
                string deviceId = _deviceIds[_random.Next(_deviceIds.Length)];
                
                // Determine the location based on the device ID
                int locationIndex = 0;
                if (deviceId.StartsWith("TRAP-SB-001")) locationIndex = 0;
                else if (deviceId.StartsWith("TRAP-SB-002")) locationIndex = 1;
                else if (deviceId.StartsWith("TRAP-MCD-001")) locationIndex = 2;
                else if (deviceId.StartsWith("TRAP-MCD-002")) locationIndex = 3;
                
                string locationId = _locationIds[locationIndex];
                string locationName = _locationNames[locationIndex];
                string customerName = _customerNames[locationIndex];
                
                // Generate a random event type with weighted probabilities
                EventType eventType = GenerateRandomEventType();
                
                // Generate a random battery level appropriate for the event type
                int batteryLevel = GenerateBatteryLevel(eventType);
                
                // Create the event
                var trapEvent = new
                {
                    EventId = Guid.NewGuid(),
                    DeviceId = deviceId,
                    Timestamp = DateTime.UtcNow,
                    EventType = eventType.ToString(),
                    LocationId = locationId,
                    LocationName = locationName,
                    CustomerName = customerName,
                    BatteryLevel = batteryLevel,
                    AdditionalData = GenerateAdditionalData(eventType),
                    IsProcessed = false,
                    ProcessedTimestamp = (DateTime?)null
                };
                
                // Serialize and send the event
                string messageBody = JsonSerializer.Serialize(trapEvent);
                var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(messageBody));
                
                // Add custom properties for message routing
                message.ApplicationProperties.Add("EventType", eventType.ToString());
                message.ApplicationProperties.Add("DeviceId", deviceId);
                message.ApplicationProperties.Add("LocationId", locationId);
                
                await _sender.SendMessageAsync(message);
                
                _logger.LogInformation($"Sent {eventType} event for device {deviceId} at {locationName}");
            }
        }

        private EventType GenerateRandomEventType()
        {
            // Weighted random selection of event types
            int value = _random.Next(100);
            
            if (value < 5) return EventType.Activation;      // 5%
            if (value < 10) return EventType.Capture;        // 5%
            if (value < 20) return EventType.BatteryLow;     // 10%
            if (value < 25) return EventType.Maintenance;    // 5%
            if (value < 30) return EventType.Malfunction;    // 5%
            if (value < 35) return EventType.Reset;          // 5%
            return EventType.Heartbeat;                      // 65%
        }

        private int GenerateBatteryLevel(EventType eventType)
        {
            switch (eventType)
            {
                case EventType.BatteryLow:
                    return _random.Next(5, 20); // Low battery: 5-20%
                case EventType.Maintenance:
                    return _random.Next(50, 101); // After maintenance: 50-100%
                case EventType.Malfunction:
                    return _random.Next(20, 80); // Malfunction: random level
                default:
                    return _random.Next(60, 101); // Normal operation: 60-100%
            }
        }

        private string GenerateAdditionalData(EventType eventType)
        {
            switch (eventType)
            {
                case EventType.Activation:
                case EventType.Capture:
                    return JsonSerializer.Serialize(new
                    {
                        weight = Math.Round(_random.NextDouble() * 20, 1), // 0-20 grams
                        motion = true,
                        temperature = Math.Round(18 + _random.NextDouble() * 10, 1) // 18-28 degrees C
                    });
                case EventType.BatteryLow:
                    return JsonSerializer.Serialize(new
                    {
                        voltage = Math.Round(2.5 + _random.NextDouble(), 2) // 2.5-3.5 volts
                    });
                case EventType.Maintenance:
                    return JsonSerializer.Serialize(new
                    {
                        technician = $"Tech-{_random.Next(1000, 10000)}",
                        action = "Replaced battery and cleaned sensors"
                    });
                case EventType.Malfunction:
                    string[] malfunctions = new[] 
                    { 
                        "Sensor failure", 
                        "Communication error", 
                        "Mechanism jammed", 
                        "Calibration error" 
                    };
                    return JsonSerializer.Serialize(new
                    {
                        errorCode = $"ERR-{_random.Next(100, 1000)}",
                        description = malfunctions[_random.Next(malfunctions.Length)]
                    });
                case EventType.Reset:
                    return JsonSerializer.Serialize(new
                    {
                        firmwareVersion = $"v2.{_random.Next(0, 5)}.{_random.Next(0, 10)}",
                        resetReason = "Scheduled maintenance"
                    });
                case EventType.Heartbeat:
                default:
                    return JsonSerializer.Serialize(new
                    {
                        temperature = Math.Round(18 + _random.NextDouble() * 10, 1), // 18-28 degrees C
                        humidity = _random.Next(30, 70), // 30-70%
                        signalStrength = _random.Next(-90, -50) // -90 to -50 dBm
                    });
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Stopping IoT Device Simulator Service");
            
            await _sender.DisposeAsync();
            await _serviceBusClient.DisposeAsync();
            
            await base.StopAsync(stoppingToken);
        }
    }

    public enum EventType
    {
        Activation,
        Capture,
        BatteryLow,
        Maintenance,
        Malfunction,
        Reset,
        Heartbeat
    }
}
