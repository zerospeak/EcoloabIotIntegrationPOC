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
    public class EventsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EventsController> _logger;

        public EventsController(ApplicationDbContext context, ILogger<EventsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/events
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TrapEvent>>> GetEvents()
        {
            _logger.LogInformation("Getting all trap events");
            return await _context.Events.ToListAsync();
        }

        // GET: api/events/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TrapEvent>> GetEvent(Guid id)
        {
            _logger.LogInformation($"Getting event with ID: {id}");
            
            var trapEvent = await _context.Events.FindAsync(id);

            if (trapEvent == null)
            {
                _logger.LogWarning($"Event with ID: {id} not found");
                return NotFound();
            }

            return trapEvent;
        }

        // GET: api/events/device/TRAP-SB-001-01
        [HttpGet("device/{deviceId}")]
        public async Task<ActionResult<IEnumerable<TrapEvent>>> GetEventsByDevice(string deviceId)
        {
            _logger.LogInformation($"Getting events for device: {deviceId}");
            
            var events = await _context.Events
                .Where(e => e.DeviceId == deviceId)
                .OrderByDescending(e => e.Timestamp)
                .ToListAsync();

            if (events == null || !events.Any())
            {
                _logger.LogWarning($"No events found for device: {deviceId}");
                return NotFound();
            }

            return events;
        }

        // GET: api/events/location/LOC-SB-001
        [HttpGet("location/{locationId}")]
        public async Task<ActionResult<IEnumerable<TrapEvent>>> GetEventsByLocation(string locationId)
        {
            _logger.LogInformation($"Getting events for location: {locationId}");
            
            var events = await _context.Events
                .Where(e => e.LocationId == locationId)
                .OrderByDescending(e => e.Timestamp)
                .ToListAsync();

            if (events == null || !events.Any())
            {
                _logger.LogWarning($"No events found for location: {locationId}");
                return NotFound();
            }

            return events;
        }

        // POST: api/events
        [HttpPost]
        public async Task<ActionResult<TrapEvent>> CreateEvent(TrapEvent trapEvent)
        {
            _logger.LogInformation($"Creating new event for device: {trapEvent.DeviceId}");
            
            // Set event ID if not provided
            if (trapEvent.EventId == Guid.Empty)
            {
                trapEvent.EventId = Guid.NewGuid();
            }
            
            // Set timestamp if not provided
            if (trapEvent.Timestamp == default)
            {
                trapEvent.Timestamp = DateTime.UtcNow;
            }
            
            _context.Events.Add(trapEvent);
            
            try
            {
                await _context.SaveChangesAsync();
                
                // Update the device status based on the event type
                await UpdateDeviceStatus(trapEvent);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, $"Error creating event for device: {trapEvent.DeviceId}");
                
                if (EventExists(trapEvent.EventId))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtAction(nameof(GetEvent), new { id = trapEvent.EventId }, trapEvent);
        }

        // PUT: api/events/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEvent(Guid id, TrapEvent trapEvent)
        {
            if (id != trapEvent.EventId)
            {
                _logger.LogWarning($"ID mismatch: {id} vs {trapEvent.EventId}");
                return BadRequest();
            }

            _context.Entry(trapEvent).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, $"Concurrency error updating event: {id}");
                
                if (!EventExists(id))
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

        // DELETE: api/events/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvent(Guid id)
        {
            _logger.LogInformation($"Deleting event with ID: {id}");
            
            var trapEvent = await _context.Events.FindAsync(id);
            if (trapEvent == null)
            {
                _logger.LogWarning($"Event with ID: {id} not found for deletion");
                return NotFound();
            }

            _context.Events.Remove(trapEvent);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool EventExists(Guid id)
        {
            return _context.Events.Any(e => e.EventId == id);
        }
        
        private async Task UpdateDeviceStatus(TrapEvent trapEvent)
        {
            var device = await _context.Devices.FindAsync(trapEvent.DeviceId);
            
            if (device != null)
            {
                // Update device status based on event type
                switch (trapEvent.EventType)
                {
                    case EventType.Activation:
                    case EventType.Capture:
                        device.Status = DeviceStatus.Triggered;
                        break;
                    case EventType.BatteryLow:
                        device.Status = DeviceStatus.LowBattery;
                        break;
                    case EventType.Maintenance:
                        device.Status = DeviceStatus.Maintenance;
                        device.LastMaintenanceDate = trapEvent.Timestamp;
                        break;
                    case EventType.Malfunction:
                        device.Status = DeviceStatus.Alert;
                        break;
                    case EventType.Reset:
                        device.Status = DeviceStatus.Active;
                        break;
                    case EventType.Heartbeat:
                        // Only update if not in a special state
                        if (device.Status != DeviceStatus.Triggered && 
                            device.Status != DeviceStatus.LowBattery && 
                            device.Status != DeviceStatus.Maintenance && 
                            device.Status != DeviceStatus.Alert)
                        {
                            device.Status = DeviceStatus.Active;
                        }
                        break;
                }
                
                // Update battery level and last communication date
                device.BatteryLevel = trapEvent.BatteryLevel;
                device.LastCommunicationDate = trapEvent.Timestamp;
                
                await _context.SaveChangesAsync();
            }
        }
    }
}
