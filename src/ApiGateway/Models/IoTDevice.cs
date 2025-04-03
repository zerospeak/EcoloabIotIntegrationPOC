using System;
using System.ComponentModel.DataAnnotations;

namespace ApiGateway.Models
{
    public class IoTDevice
    {
        [Key]
        public string DeviceId { get; set; }
        
        [Required]
        public string DeviceName { get; set; }
        
        [Required]
        public DeviceType DeviceType { get; set; }
        
        [Required]
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
