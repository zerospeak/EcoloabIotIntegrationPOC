using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Json;

namespace EventProcessor.Functions
{
    public class TrapEventProcessor
    {
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;

        public TrapEventProcessor(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<TrapEventProcessor>();
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("http://api-gateway/api/");
        }

        [Function("ProcessTrapEvents")]
        public async Task Run(
            [ServiceBusTrigger("trap-events", Connection = "ServiceBusConnection")] string message,
            FunctionContext context)
        {
            try
            {
                _logger.LogInformation($"Processing trap event: {message}");

                // Parse the message
                var trapEvent = JsonSerializer.Deserialize<TrapEvent>(message);

                if (trapEvent == null)
                {
                    _logger.LogError("Failed to deserialize trap event message");
                    return;
                }

                // Process the event based on its type
                await ProcessEventByType(trapEvent);

                // Mark the event as processed
                trapEvent.IsProcessed = true;
                trapEvent.ProcessedTimestamp = DateTime.UtcNow;

                // Update the event in the database
                await _httpClient.PutAsJsonAsync($"events/{trapEvent.EventId}", trapEvent);

                _logger.LogInformation($"Successfully processed {trapEvent.EventType} event for device {trapEvent.DeviceId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing trap event");
                throw;
            }
        }

        private async Task ProcessEventByType(TrapEvent trapEvent)
        {
            switch (trapEvent.EventType)
            {
                case EventType.Activation:
                case EventType.Capture:
                    await ProcessCaptureEvent(trapEvent);
                    break;
                case EventType.BatteryLow:
                    await ProcessBatteryLowEvent(trapEvent);
                    break;
                case EventType.Maintenance:
                    await ProcessMaintenanceEvent(trapEvent);
                    break;
                case EventType.Malfunction:
                    await ProcessMalfunctionEvent(trapEvent);
                    break;
                case EventType.Reset:
                    await ProcessResetEvent(trapEvent);
                    break;
                case EventType.Heartbeat:
                    await ProcessHeartbeatEvent(trapEvent);
                    break;
                default:
                    _logger.LogWarning($"Unknown event type: {trapEvent.EventType}");
                    break;
            }
        }

        private async Task ProcessCaptureEvent(TrapEvent trapEvent)
        {
            _logger.LogInformation($"Processing capture event for device {trapEvent.DeviceId}");
            
            // In a real system, this might:
            // 1. Generate an alert for immediate attention
            // 2. Create a maintenance ticket
            // 3. Send notifications to technicians
            
            // For this prototype, we'll simulate updating the device status
            await UpdateDeviceStatus(trapEvent.DeviceId, "Triggered");
            
            // Log the capture for analytics
            await LogCaptureForAnalytics(trapEvent);
        }

        private async Task ProcessBatteryLowEvent(TrapEvent trapEvent)
        {
            _logger.LogInformation($"Processing battery low event for device {trapEvent.DeviceId}");
            
            // In a real system, this might:
            // 1. Add the device to a maintenance schedule
            // 2. Send a notification to the maintenance team
            
            // For this prototype, we'll simulate updating the device status
            await UpdateDeviceStatus(trapEvent.DeviceId, "LowBattery");
        }

        private async Task ProcessMaintenanceEvent(TrapEvent trapEvent)
        {
            _logger.LogInformation($"Processing maintenance event for device {trapEvent.DeviceId}");
            
            // In a real system, this might:
            // 1. Update maintenance records
            // 2. Close any open maintenance tickets
            
            // For this prototype, we'll simulate updating the device status
            await UpdateDeviceStatus(trapEvent.DeviceId, "Active");
        }

        private async Task ProcessMalfunctionEvent(TrapEvent trapEvent)
        {
            _logger.LogInformation($"Processing malfunction event for device {trapEvent.DeviceId}");
            
            // In a real system, this might:
            // 1. Create a high-priority maintenance ticket
            // 2. Send alerts to the technical team
            
            // For this prototype, we'll simulate updating the device status
            await UpdateDeviceStatus(trapEvent.DeviceId, "Alert");
        }

        private async Task ProcessResetEvent(TrapEvent trapEvent)
        {
            _logger.LogInformation($"Processing reset event for device {trapEvent.DeviceId}");
            
            // In a real system, this might:
            // 1. Verify the device is functioning correctly after reset
            // 2. Update firmware version records
            
            // For this prototype, we'll simulate updating the device status
            await UpdateDeviceStatus(trapEvent.DeviceId, "Active");
        }

        private async Task ProcessHeartbeatEvent(TrapEvent trapEvent)
        {
            _logger.LogInformation($"Processing heartbeat event for device {trapEvent.DeviceId}");
            
            // In a real system, this might:
            // 1. Update last-seen timestamp
            // 2. Check for any anomalies in the reported data
            
            // For this prototype, we'll just log the heartbeat
            // No status update needed for routine heartbeats
        }

        private async Task UpdateDeviceStatus(string deviceId, string status)
        {
            try
            {
                // Get the current device
                var response = await _httpClient.GetAsync($"devices/{deviceId}");
                
                if (response.IsSuccessStatusCode)
                {
                    var device = await response.Content.ReadFromJsonAsync<IoTDevice>();
                    
                    if (device != null)
                    {
                        // Update the status
                        device.Status = Enum.Parse<DeviceStatus>(status);
                        
                        // Save the updated device
                        await _httpClient.PutAsJsonAsync($"devices/{deviceId}", device);
                        
                        _logger.LogInformation($"Updated device {deviceId} status to {status}");
                    }
                }
                else
                {
                    _logger.LogWarning($"Failed to get device {deviceId}: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating device {deviceId} status");
            }
        }

        private async Task LogCaptureForAnalytics(TrapEvent trapEvent)
        {
            try
            {
                // In a real system, this would send data to an analytics service
                // For this prototype, we'll just log it
                _logger.LogInformation($"Logging capture for analytics: Device {trapEvent.DeviceId} at location {trapEvent.LocationName}");
                
                // Simulate sending to a blob storage for later analysis
                var captureData = new
                {
                    trapEvent.EventId,
                    trapEvent.DeviceId,
                    trapEvent.LocationId,
                    trapEvent.LocationName,
                    trapEvent.CustomerName,
                    trapEvent.Timestamp,
                    trapEvent.AdditionalData
                };
                
                // In a real implementation, this would use Azure Blob Storage SDK
                // await _blobClient.UploadTextAsync("captures", $"{trapEvent.EventId}.json", JsonSerializer.Serialize(captureData));
                
                await Task.CompletedTask; // Placeholder for actual implementation
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging capture for analytics");
            }
        }
    }

    // These classes should match the models in the API Gateway
    public class TrapEvent
    {
        public Guid EventId { get; set; }
        public string DeviceId { get; set; }
        public DateTime Timestamp { get; set; }
        public EventType EventType { get; set; }
        public string LocationId { get; set; }
        public string LocationName { get; set; }
        public string CustomerName { get; set; }
        public int BatteryLevel { get; set; }
        public string AdditionalData { get; set; }
        public bool IsProcessed { get; set; }
        public DateTime? ProcessedTimestamp { get; set; }
    }

    public class IoTDevice
    {
        public string DeviceId { get; set; }
        public string DeviceName { get; set; }
        public DeviceType DeviceType { get; set; }
        public string LocationId { get; set; }
        public string LocationName { get; set; }
        public string CustomerName { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime InstallationDate { get; set; }
        public DateTime LastMaintenanceDate { get; set; }
        public DateTime LastCommunicationDate { get; set; }
        public int BatteryLevel { get; set; }
        public DeviceStatus Status { get; set; }
        public string FirmwareVersion { get; set; }
        public bool IsActive { get; set; }
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

    public enum DeviceType
    {
        MouseTrap,
        RatTrap,
        InsectMonitor,
        TemperatureSensor,
        HumiditySensor
    }

    public enum DeviceStatus
    {
        Active,
        Inactive,
        Maintenance,
        Alert,
        Triggered,
        LowBattery,
        Offline
    }
}
