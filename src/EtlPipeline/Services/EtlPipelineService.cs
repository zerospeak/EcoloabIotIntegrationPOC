using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;

namespace EtlPipeline.Services
{
    public class EtlPipelineService : BackgroundService
    {
        private readonly ILogger<EtlPipelineService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly string _blobConnectionString;
        private readonly string _serviceBusConnectionString;
        private readonly BlobContainerClient _rawDataContainer;
        private readonly BlobContainerClient _processedDataContainer;
        private readonly ServiceBusProcessor _serviceBusProcessor;

        public EtlPipelineService(
            ILogger<EtlPipelineService> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
            _blobConnectionString = _configuration.GetValue<string>("AzureStorage:ConnectionString");
            _serviceBusConnectionString = _configuration.GetValue<string>("ServiceBusConnection");
            
            // Initialize blob storage containers
            var blobServiceClient = new BlobServiceClient(_blobConnectionString);
            _rawDataContainer = blobServiceClient.GetBlobContainerClient("raw-data");
            _processedDataContainer = blobServiceClient.GetBlobContainerClient("processed-data");
            
            // Ensure containers exist
            _rawDataContainer.CreateIfNotExists();
            _processedDataContainer.CreateIfNotExists();
            
            // Initialize Service Bus processor
            var serviceBusClient = new ServiceBusClient(_serviceBusConnectionString);
            _serviceBusProcessor = serviceBusClient.CreateProcessor("trap-events", new ServiceBusProcessorOptions
            {
                MaxConcurrentCalls = 1,
                AutoCompleteMessages = false
            });
            
            _serviceBusProcessor.ProcessMessageAsync += ProcessServiceBusMessage;
            _serviceBusProcessor.ProcessErrorAsync += ProcessServiceBusError;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ETL Pipeline Service is starting.");

            try
            {
                // Start processing messages
                await _serviceBusProcessor.StartProcessingAsync(stoppingToken);
                
                // Run periodic ETL jobs
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        // Run ETL processes
                        await RunDailyAggregationJob();
                        await RunLocationAnalysisJob();
                        
                        // Wait for next cycle (every 15 minutes in a real system, but shorter for demo)
                        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error occurred during ETL job execution.");
                        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                    }
                }
            }
            finally
            {
                await _serviceBusProcessor.StopProcessingAsync();
            }

            _logger.LogInformation("ETL Pipeline Service is stopping.");
        }

        private async Task ProcessServiceBusMessage(ProcessMessageEventArgs args)
        {
            try
            {
                string messageBody = args.Message.Body.ToString();
                _logger.LogInformation($"Received message: {messageBody}");
                
                // Extract message properties
                string eventType = args.Message.ApplicationProperties.TryGetValue("EventType", out var type) 
                    ? type.ToString() 
                    : "Unknown";
                
                string deviceId = args.Message.ApplicationProperties.TryGetValue("DeviceId", out var device) 
                    ? device.ToString() 
                    : "Unknown";
                
                // Store raw message in blob storage
                string blobName = $"{DateTime.UtcNow:yyyy/MM/dd/HH}/{deviceId}/{Guid.NewGuid()}.json";
                var blob = _rawDataContainer.GetBlobClient(blobName);
                await blob.UploadAsync(BinaryData.FromString(messageBody), overwrite: true);
                
                // Process the message based on event type
                await ProcessEventData(messageBody, eventType);
                
                // Complete the message
                await args.CompleteMessageAsync(args.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Service Bus message");
                
                // Abandon the message to retry later
                await args.AbandonMessageAsync(args.Message);
            }
        }

        private Task ProcessServiceBusError(ProcessErrorEventArgs args)
        {
            _logger.LogError(args.Exception, "Error handling Service Bus message");
            return Task.CompletedTask;
        }

        private async Task ProcessEventData(string messageBody, string eventType)
        {
            try
            {
                // Parse the message
                var eventData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(messageBody);
                
                if (eventData == null)
                {
                    _logger.LogError("Failed to parse event data");
                    return;
                }
                
                // Extract key fields
                string deviceId = GetStringValue(eventData, "DeviceId");
                string locationId = GetStringValue(eventData, "LocationId");
                DateTime timestamp = GetDateTimeValue(eventData, "Timestamp");
                
                // Store in SQL database based on event type
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    // Different processing based on event type
                    switch (eventType)
                    {
                        case "Activation":
                        case "Capture":
                            await StoreCapture(connection, eventData);
                            break;
                        case "BatteryLow":
                            await StoreBatteryAlert(connection, eventData);
                            break;
                        case "Maintenance":
                            await StoreMaintenance(connection, eventData);
                            break;
                        case "Malfunction":
                            await StoreMalfunction(connection, eventData);
                            break;
                        default:
                            await StoreGenericEvent(connection, eventData);
                            break;
                    }
                    
                    // Update device statistics
                    await UpdateDeviceStatistics(connection, deviceId);
                    
                    // Update location statistics
                    await UpdateLocationStatistics(connection, locationId);
                }
                
                // Transform and store processed data
                await StoreProcessedData(eventData, eventType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing event data");
                throw;
            }
        }

        private async Task StoreCapture(SqlConnection connection, Dictionary<string, JsonElement> eventData)
        {
            // In a real implementation, this would insert into a Captures table
            _logger.LogInformation("Storing capture event in database");
            
            // Simulate database operation
            await Task.Delay(100);
        }

        private async Task StoreBatteryAlert(SqlConnection connection, Dictionary<string, JsonElement> eventData)
        {
            // In a real implementation, this would insert into a BatteryAlerts table
            _logger.LogInformation("Storing battery alert in database");
            
            // Simulate database operation
            await Task.Delay(100);
        }

        private async Task StoreMaintenance(SqlConnection connection, Dictionary<string, JsonElement> eventData)
        {
            // In a real implementation, this would insert into a MaintenanceRecords table
            _logger.LogInformation("Storing maintenance record in database");
            
            // Simulate database operation
            await Task.Delay(100);
        }

        private async Task StoreMalfunction(SqlConnection connection, Dictionary<string, JsonElement> eventData)
        {
            // In a real implementation, this would insert into a Malfunctions table
            _logger.LogInformation("Storing malfunction in database");
            
            // Simulate database operation
            await Task.Delay(100);
        }

        private async Task StoreGenericEvent(SqlConnection connection, Dictionary<string, JsonElement> eventData)
        {
            // In a real implementation, this would insert into a generic Events table
            _logger.LogInformation("Storing generic event in database");
            
            // Simulate database operation
            await Task.Delay(100);
        }

        private async Task UpdateDeviceStatistics(SqlConnection connection, string deviceId)
        {
            // In a real implementation, this would update device statistics
            _logger.LogInformation($"Updating statistics for device {deviceId}");
            
            // Simulate database operation
            await Task.Delay(100);
        }

        private async Task UpdateLocationStatistics(SqlConnection connection, string locationId)
        {
            // In a real implementation, this would update location statistics
            _logger.LogInformation($"Updating statistics for location {locationId}");
            
            // Simulate database operation
            await Task.Delay(100);
        }

        private async Task StoreProcessedData(Dictionary<string, JsonElement> eventData, string eventType)
        {
            try
            {
                // Transform the data for analytics
                var processedData = new Dictionary<string, object>
                {
                    ["event_type"] = eventType,
                    ["device_id"] = GetStringValue(eventData, "DeviceId"),
                    ["location_id"] = GetStringValue(eventData, "LocationId"),
                    ["customer_name"] = GetStringValue(eventData, "CustomerName"),
                    ["timestamp"] = GetDateTimeValue(eventData, "Timestamp"),
                    ["processed_at"] = DateTime.UtcNow
                };
                
                // Add event-specific data
                if (eventData.TryGetValue("AdditionalData", out var additionalDataElement))
                {
                    var additionalData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(additionalDataElement.GetString());
                    if (additionalData != null)
                    {
                        foreach (var item in additionalData)
                        {
                            processedData[$"data_{item.Key}"] = GetJsonElementValue(item.Value);
                        }
                    }
                }
                
                // Store in processed data container
                string blobName = $"{eventType}/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}.json";
                var blob = _processedDataContainer.GetBlobClient(blobName);
                await blob.UploadAsync(BinaryData.FromString(JsonSerializer.Serialize(processedData)), overwrite: true);
                
                _logger.LogInformation($"Stored processed {eventType} data in blob storage");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing processed data");
            }
        }

        private async Task RunDailyAggregationJob()
        {
            _logger.LogInformation("Running daily aggregation ETL job");
            
            try
            {
                // In a real implementation, this would:
                // 1. Query the database for events in the last 24 hours
                // 2. Aggregate data by location, device type, event type, etc.
                // 3. Store results in an analytics database or data warehouse
                
                // Simulate ETL processing
                await Task.Delay(500);
                
                _logger.LogInformation("Daily aggregation ETL job completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running daily aggregation ETL job");
            }
        }

        private async Task RunLocationAnalysisJob()
        {
            _logger.LogInformation("Running location analysis ETL job");
            
            try
            {
                // In a real implementation, this would:
                // 1. Analyze trap activity patterns by location
                // 2. Generate location risk scores
                // 3. Identify locations needing attention
                
                // Simulate ETL processing
                await Task.Delay(500);
                
                _logger.LogInformation("Location analysis ETL job completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running location analysis ETL job");
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Stopping ETL Pipeline Service");
            await _serviceBusProcessor.StopProcessingAsync(stoppingToken);
            await base.StopAsync(stoppingToken);
        }

        #region Helper Methods
        
        private string GetStringValue(Dictionary<string, JsonElement> data, string key)
        {
            if (data.TryGetValue(key, out var element))
            {
                return element.ValueKind == JsonValueKind.String 
                    ? element.GetString() 
                    : element.ToString();
            }
            return string.Empty;
        }
        
        private DateTime GetDateTimeValue(Dictionary<string, JsonElement> data, string key)
        {
            if (data.TryGetValue(key, out var element) && element.ValueKind == JsonValueKind.String)
            {
                if (DateTime.TryParse(element.GetString(), out var dateTime))
                {
                    return dateTime;
                }
            }
            return DateTime.UtcNow;
        }
        
        private object GetJsonElementValue(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    return element.GetString();
                case JsonValueKind.Number:
                    return element.TryGetInt64(out var longValue) 
                        ? longValue 
                        : element.GetDouble();
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Null:
                    return null;
                default:
                    return element.ToString();
            }
        }
        
        #endregion
    }
}
