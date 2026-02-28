using System;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace CameraCopyTool.Services
{
    /// <summary>
    /// Service for monitoring network connectivity.
    /// </summary>
    public interface INetworkService
    {
        /// <summary>
        /// Gets a value indicating whether the network is available.
        /// </summary>
        bool IsNetworkAvailable { get; }

        /// <summary>
        /// Event raised when network availability changes.
        /// </summary>
        event EventHandler<bool> NetworkAvailabilityChanged;

        /// <summary>
        /// Starts monitoring network availability.
        /// </summary>
        void StartMonitoring();

        /// <summary>
        /// Stops monitoring network availability.
        /// </summary>
        void StopMonitoring();

        /// <summary>
        /// Waits for network to become available.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if network became available, false if cancelled.</returns>
        Task WaitForNetworkAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Implementation of INetworkService using NetworkChange events.
    /// </summary>
    public class NetworkService : INetworkService
    {
        private bool _isMonitoring;
        private bool _lastKnownStatus;
        private readonly object _lockObj = new object();

        public NetworkService()
        {
            _lastKnownStatus = GetIsNetworkAvailable();
            // Start monitoring immediately so events are captured
            StartMonitoring();
        }

        public bool IsNetworkAvailable => GetIsNetworkAvailable();

        public event EventHandler<bool>? NetworkAvailabilityChanged;

        public void StartMonitoring()
        {
            if (_isMonitoring) return;

            _isMonitoring = true;
            NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;
        }

        public void StopMonitoring()
        {
            if (!_isMonitoring) return;

            _isMonitoring = false;
            NetworkChange.NetworkAvailabilityChanged -= OnNetworkAvailabilityChanged;
        }

        public async Task WaitForNetworkAsync(CancellationToken cancellationToken = default)
        {
            FileLogger.Log($"WaitForNetworkAsync started. Current network status: {IsNetworkAvailable}");
            
            if (IsNetworkAvailable)
            {
                FileLogger.Log("Network already available, returning immediately");
                return;
            }

            var tcs = new TaskCompletionSource<bool>();
            var pollCount = 0;

            void OnNetworkAvailable(object? sender, bool isAvailable)
            {
                FileLogger.Log($"NetworkAvailabilityChanged event fired: {isAvailable}");
                if (isAvailable)
                {
                    FileLogger.Log("Network restored via event");
                    tcs.TrySetResult(true);
                }
            }

            NetworkAvailabilityChanged += OnNetworkAvailable;

            try
            {
                // Poll every 500ms as a fallback
                while (!cancellationToken.IsCancellationRequested)
                {
                    pollCount++;
                    var networkStatus = IsNetworkAvailable;
                    
                    if (pollCount % 10 == 0) // Log every 5 seconds
                    {
                        FileLogger.Log($"Waiting for network... (poll {pollCount}, status: {networkStatus})");
                    }
                    
                    if (networkStatus)
                    {
                        FileLogger.Log($"Network restored via polling after {pollCount} attempts");
                        tcs.TrySetResult(true);
                        break;
                    }

                    await Task.Delay(500, cancellationToken).ConfigureAwait(false);
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    FileLogger.Log("Network wait cancelled by token");
                    tcs.TrySetCanceled(cancellationToken);
                }
            }
            finally
            {
                NetworkAvailabilityChanged -= OnNetworkAvailable;
            }

            await tcs.Task.ConfigureAwait(false);
            FileLogger.Log("WaitForNetworkAsync completed");
        }

        private bool GetIsNetworkAvailable()
        {
            try
            {
                // First check if any network interface is up
                if (!NetworkInterface.GetIsNetworkAvailable())
                {
                    return false;
                }

                // Try to ping multiple reliable hosts for better accuracy
                var hostsToPing = new[] { "8.8.8.8", "1.1.1.1", "www.google.com" };
                
                foreach (var host in hostsToPing)
                {
                    try
                    {
                        using var ping = new System.Net.NetworkInformation.Ping();
                        var reply = ping.Send(host, 2000); // 2 second timeout
                        if (reply.Status == IPStatus.Success)
                        {
                            return true;
                        }
                    }
                    catch
                    {
                        // Try next host
                        continue;
                    }
                }

                // If all pings fail, fall back to just checking if network interface is up
                // This handles cases where internet is down but local network is up
                return NetworkInterface.GetIsNetworkAvailable();
            }
            catch (Exception ex)
            {
                FileLogger.Log($"GetIsNetworkAvailable error: {ex.Message}");
                return false;
            }
        }

        private void OnNetworkAvailabilityChanged(object? sender, EventArgs e)
        {
            var isAvailable = GetIsNetworkAvailable();

            lock (_lockObj)
            {
                if (isAvailable != _lastKnownStatus)
                {
                    _lastKnownStatus = isAvailable;
                    NetworkAvailabilityChanged?.Invoke(this, isAvailable);
                }
            }
        }
    }
}
