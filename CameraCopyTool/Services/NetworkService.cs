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
            if (IsNetworkAvailable)
                return;

            var tcs = new TaskCompletionSource<bool>();

            void OnNetworkAvailable(object? sender, bool isAvailable)
            {
                if (isAvailable)
                {
                    tcs.TrySetResult(true);
                }
            }

            NetworkAvailabilityChanged += OnNetworkAvailable;

            try
            {
                // Poll every 500ms as a fallback
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (IsNetworkAvailable)
                    {
                        tcs.TrySetResult(true);
                        break;
                    }

                    await Task.Delay(500, cancellationToken).ConfigureAwait(false);
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    tcs.TrySetCanceled(cancellationToken);
                }
            }
            finally
            {
                NetworkAvailabilityChanged -= OnNetworkAvailable;
            }

            await tcs.Task.ConfigureAwait(false);
        }

        private bool GetIsNetworkAvailable()
        {
            try
            {
                return NetworkInterface.GetIsNetworkAvailable();
            }
            catch
            {
                // Fallback: try to ping a reliable host
                try
                {
                    using var ping = new System.Net.NetworkInformation.Ping();
                    var reply = ping.Send("8.8.8.8", 1000);
                    return reply.Status == IPStatus.Success;
                }
                catch
                {
                    return false;
                }
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
