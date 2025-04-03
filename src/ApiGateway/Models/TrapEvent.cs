using System;
using System.ComponentModel.DataAnnotations;

namespace ApiGateway.Models
{
    public class TrapEvent
    {
        [Key]
        public Guid EventId { get; set; }
        
        [Required]
        public string DeviceId { get; set; }
        
        [Required]
        public DateTime Timestamp { get; set; }
        
        [Required]
        public EventType EventType { get; set; }
        
        public string LocationId { get; set; }
        
        public string LocationName { get; set; }
        
        public string CustomerName { get; set; }
        
        public int BatteryLevel { get; set; }
        
        public string AdditionalData { get; set; }
        
        public bool IsProcessed { get; set; }
        
        public DateTime? ProcessedTimestamp { get; set; }
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
