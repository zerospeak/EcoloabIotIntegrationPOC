using System;
using System.ComponentModel.DataAnnotations;

namespace ApiGateway.Models
{
    public class Location
    {
        [Key]
        public string LocationId { get; set; }
        
        [Required]
        public string LocationName { get; set; }
        
        [Required]
        public string CustomerId { get; set; }
        
        public string CustomerName { get; set; }
        
        public string Address { get; set; }
        
        public string City { get; set; }
        
        public string State { get; set; }
        
        public string ZipCode { get; set; }
        
        public string Country { get; set; }
        
        public double Latitude { get; set; }
        
        public double Longitude { get; set; }
        
        public LocationType Type { get; set; }
        
        public int TotalDevices { get; set; }
        
        public DateTime LastActivityDate { get; set; }
        
        public bool IsActive { get; set; }
    }
    
    public enum LocationType
    {
        Restaurant,
        CoffeeShop,
        Warehouse,
        Factory,
        Office,
        Retail,
        Hospital,
        School,
        Other
    }
}
