import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { interval } from 'rxjs';
import { switchMap } from 'rxjs/operators';

interface IoTDevice {
  deviceId: string;
  deviceName: string;
  deviceType: string;
  locationId: string;
  locationName: string;
  customerName: string;
  batteryLevel: number;
  status: string;
  lastCommunicationDate: string;
}

interface TrapEvent {
  eventId: string;
  deviceId: string;
  timestamp: string;
  eventType: string;
  locationId: string;
  locationName: string;
  customerName: string;
  batteryLevel: number;
  additionalData: string;
}

interface DeviceStats {
  totalDevices: number;
  activeDevices: number;
  triggeredDevices: number;
  lowBatteryDevices: number;
  maintenanceDevices: number;
  alertDevices: number;
}

interface LocationStats {
  locationId: string;
  locationName: string;
  customerName: string;
  deviceCount: number;
  captureCount: number;
  lastActivity: string;
}

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss']
})
export class DashboardComponent implements OnInit {
  devices: IoTDevice[] = [];
  recentEvents: TrapEvent[] = [];
  deviceStats: DeviceStats = {
    totalDevices: 0,
    activeDevices: 0,
    triggeredDevices: 0,
    lowBatteryDevices: 0,
    maintenanceDevices: 0,
    alertDevices: 0
  };
  locationStats: LocationStats[] = [];
  
  selectedLocation: string = 'all';
  selectedCustomer: string = 'all';
  
  customers: string[] = [];
  locations: { [key: string]: string[] } = {};
  
  loading = true;
  error = false;

  constructor(private http: HttpClient) { }

  ngOnInit(): void {
    // Initial data load
    this.loadDashboardData();
    
    // Refresh data every 30 seconds
    interval(30000)
      .pipe(
        switchMap(() => this.loadDevices())
      )
      .subscribe({
        next: (devices) => {
          this.devices = devices;
          this.updateDeviceStats();
        },
        error: (err) => {
          console.error('Error refreshing device data', err);
          this.error = true;
        }
      });
      
    interval(30000)
      .pipe(
        switchMap(() => this.loadRecentEvents())
      )
      .subscribe({
        next: (events) => {
          this.recentEvents = events;
        },
        error: (err) => {
          console.error('Error refreshing event data', err);
          this.error = true;
        }
      });
  }
  
  loadDashboardData(): void {
    this.loading = true;
    
    // Load devices
    this.loadDevices().subscribe({
      next: (devices) => {
        this.devices = devices;
        this.updateDeviceStats();
        this.extractCustomersAndLocations();
      },
      error: (err) => {
        console.error('Error loading device data', err);
        this.error = true;
        this.loading = false;
      }
    });
    
    // Load recent events
    this.loadRecentEvents().subscribe({
      next: (events) => {
        this.recentEvents = events;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading event data', err);
        this.error = true;
        this.loading = false;
      }
    });
    
    // Load location statistics
    this.loadLocationStats().subscribe({
      next: (stats) => {
        this.locationStats = stats;
      },
      error: (err) => {
        console.error('Error loading location statistics', err);
      }
    });
  }
  
  loadDevices() {
    return this.http.get<IoTDevice[]>('/api/devices');
  }
  
  loadRecentEvents() {
    return this.http.get<TrapEvent[]>('/api/events');
  }
  
  loadLocationStats() {
    // In a real implementation, this would call a dedicated endpoint
    // For this prototype, we'll simulate it by transforming device data
    return this.http.get<LocationStats[]>('/api/locations/stats');
  }
  
  updateDeviceStats(): void {
    this.deviceStats.totalDevices = this.devices.length;
    this.deviceStats.activeDevices = this.devices.filter(d => d.status === 'Active').length;
    this.deviceStats.triggeredDevices = this.devices.filter(d => d.status === 'Triggered').length;
    this.deviceStats.lowBatteryDevices = this.devices.filter(d => d.status === 'LowBattery').length;
    this.deviceStats.maintenanceDevices = this.devices.filter(d => d.status === 'Maintenance').length;
    this.deviceStats.alertDevices = this.devices.filter(d => d.status === 'Alert').length;
  }
  
  extractCustomersAndLocations(): void {
    // Extract unique customers
    this.customers = [...new Set(this.devices.map(d => d.customerName))];
    
    // Extract locations by customer
    this.locations = {};
    this.devices.forEach(device => {
      if (!this.locations[device.customerName]) {
        this.locations[device.customerName] = [];
      }
      
      if (!this.locations[device.customerName].includes(device.locationName)) {
        this.locations[device.customerName].push(device.locationName);
      }
    });
  }
  
  onCustomerChange(): void {
    // Reset location selection when customer changes
    this.selectedLocation = 'all';
    this.filterDevices();
  }
  
  filterDevices(): void {
    // Apply filters based on selected customer and location
    this.loadDashboardData();
    // In a real implementation, we would pass filter parameters to the API
  }
  
  getStatusClass(status: string): string {
    switch (status) {
      case 'Active': return 'status-active';
      case 'Triggered': return 'status-triggered';
      case 'LowBattery': return 'status-low-battery';
      case 'Maintenance': return 'status-maintenance';
      case 'Alert': return 'status-alert';
      default: return 'status-inactive';
    }
  }
  
  formatTimestamp(timestamp: string): string {
    const date = new Date(timestamp);
    return date.toLocaleString();
  }
  
  getBatteryLevelClass(level: number): string {
    if (level < 20) return 'battery-critical';
    if (level < 50) return 'battery-low';
    return 'battery-good';
  }
}
