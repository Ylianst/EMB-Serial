using System;
using System.Management;
using System.Threading;

namespace SerialComm
{
    /// <summary>
    /// Monitors for COM port additions and removals using WMI event notifications.
    /// This avoids polling and provides real-time notifications when serial devices are connected/disconnected.
    /// </summary>
    public class ComPortMonitor : IDisposable
    {
        private ManagementEventWatcher? _portArrivalWatcher;
        private ManagementEventWatcher? _portRemovalWatcher;
        private bool _disposed = false;

        /// <summary>
        /// Raised when a COM port is added or removed from the system.
        /// </summary>
        public event EventHandler? ComPortsChanged;

        public ComPortMonitor()
        {
        }

        /// <summary>
        /// Starts monitoring for COM port changes.
        /// </summary>
        public void Start()
        {
            if (_disposed)
                throw new ObjectDisposedException("ComPortMonitor");

            try
            {
                // Monitor for device arrival (COM port added)
                var arrivalQuery = new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 2");
                _portArrivalWatcher = new ManagementEventWatcher(arrivalQuery);
                _portArrivalWatcher.EventArrived += OnDeviceChangeEvent;
                _portArrivalWatcher.Start();

                // Monitor for device removal (COM port removed)
                var removalQuery = new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 3");
                _portRemovalWatcher = new ManagementEventWatcher(removalQuery);
                _portRemovalWatcher.EventArrived += OnDeviceChangeEvent;
                _portRemovalWatcher.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error starting COM port monitor: {ex.Message}");
                Stop();
                throw;
            }
        }

        /// <summary>
        /// Stops monitoring for COM port changes.
        /// </summary>
        public void Stop()
        {
            if (_portArrivalWatcher != null)
            {
                _portArrivalWatcher.EventArrived -= OnDeviceChangeEvent;
                try
                {
                    _portArrivalWatcher.Stop();
                }
                catch { }
            }

            if (_portRemovalWatcher != null)
            {
                _portRemovalWatcher.EventArrived -= OnDeviceChangeEvent;
                try
                {
                    _portRemovalWatcher.Stop();
                }
                catch { }
            }
        }

        private void OnDeviceChangeEvent(object sender, EventArrivedEventArgs e)
        {
            // Raise the event on the thread pool to avoid blocking the WMI thread
            ThreadPool.QueueUserWorkItem(_ => ComPortsChanged?.Invoke(this, EventArgs.Empty));
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            Stop();

            _portArrivalWatcher?.Dispose();
            _portRemovalWatcher?.Dispose();

            _disposed = true;
        }
    }
}
