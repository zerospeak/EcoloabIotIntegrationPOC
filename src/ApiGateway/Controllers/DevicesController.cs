using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ApiGateway.Data;
using ApiGateway.Models;

namespace ApiGateway.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DevicesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DevicesController> _logger;

        public DevicesController(ApplicationDbContext context, ILogger<DevicesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/devices
        [HttpGet]
        public async Task<ActionResult<IEnumerable<IoTDevice>>> GetDevices()
        {
            _logger.LogInformation("Getting all devices");
            return await _context.Devices.ToListAsync();
        }

        // GET: api/devices/TRAP-SB-001-01
        [HttpGet("{id}")]
        public async Task<ActionResult<IoTDevice>> GetDevice(string id)
        {
            _logger.LogInformation($"Getting device with ID: {id}");
            
            var device = await _context.Devices.FindAsync(id);

            if (device == null)
            {
                _logger.LogWarning($"Device with ID: {id} not found");
                return NotFound();
            }

            return device;
        }

        // GET: api/devices/location/LOC-SB-001
        [HttpGet("location/{locationId}")]
        public async Task<ActionResult<IEnumerable<IoTDevice>>> GetDevicesByLocation(string locationId)
        {
            _logger.LogInformation($"Getting devices for location: {locationId}");
            
            var devices = await _context.Devices
                .Where(d => d.LocationId == locationId)
                .ToListAsync();

            if (devices == null || !devices.Any())
            {
                _logger.LogWarning($"No devices found for location: {locationId}");
                return NotFound();
            }

            return devices;
        }

        // POST: api/devices
        [HttpPost]
        public async Task<ActionResult<IoTDevice>> CreateDevice(IoTDevice device)
        {
            _logger.LogInformation($"Creating new device: {device.DeviceName}");
            
            _context.Devices.Add(device);
            
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, $"Error creating device: {device.DeviceName}");
                
                if (DeviceExists(device.DeviceId))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtAction(nameof(GetDevice), new { id = device.DeviceId }, device);
        }

        // PUT: api/devices/TRAP-SB-001-01
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDevice(string id, IoTDevice device)
        {
            if (id != device.DeviceId)
            {
                _logger.LogWarning($"ID mismatch: {id} vs {device.DeviceId}");
                return BadRequest();
            }

            _context.Entry(device).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, $"Concurrency error updating device: {id}");
                
                if (!DeviceExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/devices/TRAP-SB-001-01
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDevice(string id)
        {
            _logger.LogInformation($"Deleting device with ID: {id}");
            
            var device = await _context.Devices.FindAsync(id);
            if (device == null)
            {
                _logger.LogWarning($"Device with ID: {id} not found for deletion");
                return NotFound();
            }

            _context.Devices.Remove(device);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool DeviceExists(string id)
        {
            return _context.Devices.Any(e => e.DeviceId == id);
        }
    }
}
