using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CameraCopyTool.Commands;
using CameraCopyTool.Services;

namespace CameraCopyTool.ViewModels
{
    /// <summary>
    /// ViewModel for managing Google Drive authentication state.
    /// </summary>
    public class GoogleDriveAuthViewModel : ViewModelBase
    {
        private readonly IGoogleDriveService _driveService;
        private bool _isAuthenticated;
        private string? _userEmail;
        private bool _isAuthenticating;
        private string? _authStatusMessage;

        public GoogleDriveAuthViewModel(IGoogleDriveService driveService)
        {
            _driveService = driveService;
            _isAuthenticated = false;

            AuthenticateCommand = new AsyncRelayCommand(_ => AuthenticateAsync(), _ => CanAuthenticate());
            LogoutCommand = new AsyncRelayCommand(_ => LogoutAsync(), _ => CanLogout());
        }

        /// <summary>
        /// Gets a value indicating whether the user is authenticated.
        /// </summary>
        public bool IsAuthenticated
        {
            get => _isAuthenticated;
            private set => SetProperty(ref _isAuthenticated, value);
        }

        /// <summary>
        /// Gets the authenticated user's email address.
        /// </summary>
        public string? UserEmail
        {
            get => _userEmail;
            private set => SetProperty(ref _userEmail, value);
        }

        /// <summary>
        /// Gets a value indicating whether an authentication operation is in progress.
        /// </summary>
        public bool IsAuthenticating
        {
            get => _isAuthenticating;
            private set => SetProperty(ref _isAuthenticating, value);
        }

        /// <summary>
        /// Gets the current authentication status message.
        /// </summary>
        public string? AuthStatusMessage
        {
            get => _authStatusMessage;
            private set => SetProperty(ref _authStatusMessage, value);
        }

        /// <summary>
        /// Gets the command to authenticate with Google Drive.
        /// </summary>
        public ICommand AuthenticateCommand { get; }

        /// <summary>
        /// Gets the command to logout from Google Drive.
        /// </summary>
        public ICommand LogoutCommand { get; }

        private bool CanAuthenticate() => !IsAuthenticated && !IsAuthenticating;

        private bool CanLogout() => IsAuthenticated && !IsAuthenticating;

        /// <summary>
        /// Authenticates the user with Google Drive.
        /// </summary>
        private async Task AuthenticateAsync()
        {
            IsAuthenticating = true;
            AuthStatusMessage = "Starting authentication...";
            ((AsyncRelayCommand)AuthenticateCommand).RaiseCanExecuteChanged();
            ((AsyncRelayCommand)LogoutCommand).RaiseCanExecuteChanged();

            try
            {
                AuthStatusMessage = "Opening browser for sign-in...";
                var success = await _driveService.AuthenticateAsync(CancellationToken.None);

                if (success)
                {
                    IsAuthenticated = true;
                    UserEmail = _driveService.UserEmail;
                    AuthStatusMessage = $"Connected to Google Drive as {UserEmail}";
                }
                else
                {
                    AuthStatusMessage = "Authentication failed or was cancelled";
                }
            }
            catch (Exception ex)
            {
                AuthStatusMessage = $"Authentication error: {ex.Message}";
            }
            finally
            {
                IsAuthenticating = false;
                ((AsyncRelayCommand)AuthenticateCommand).RaiseCanExecuteChanged();
                ((AsyncRelayCommand)LogoutCommand).RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Logs out the user from Google Drive.
        /// </summary>
        private async Task LogoutAsync()
        {
            IsAuthenticating = true;
            ((AsyncRelayCommand)AuthenticateCommand).RaiseCanExecuteChanged();
            ((AsyncRelayCommand)LogoutCommand).RaiseCanExecuteChanged();

            try
            {
                AuthStatusMessage = "Logging out...";
                _driveService.Logout();

                IsAuthenticated = false;
                UserEmail = null;
                AuthStatusMessage = "Logged out from Google Drive";
            }
            catch (Exception ex)
            {
                AuthStatusMessage = $"Logout error: {ex.Message}";
            }
            finally
            {
                IsAuthenticating = false;
                ((AsyncRelayCommand)AuthenticateCommand).RaiseCanExecuteChanged();
                ((AsyncRelayCommand)LogoutCommand).RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Refreshes the authentication state from the service.
        /// </summary>
        public void RefreshAuthState()
        {
            IsAuthenticated = _driveService.IsAuthenticated;
            UserEmail = _driveService.UserEmail;
            AuthStatusMessage = IsAuthenticated 
                ? $"Connected to Google Drive as {UserEmail}" 
                : "Not connected to Google Drive";
        }
    }
}
