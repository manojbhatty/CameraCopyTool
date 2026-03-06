using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CameraCopyTool.ViewModels;

/// <summary>
/// Base class for ViewModels implementing INotifyPropertyChanged.
/// Provides common functionality for property change notification and property setting.
/// All ViewModels in the application inherit from this class.
/// </summary>
public class ViewModelBase : INotifyPropertyChanged
{
    /// <summary>
    /// Event raised when a property value changes.
    /// Required for INotifyPropertyChanged implementation.
    /// WPF data binding subscribes to this event to update UI elements.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raises the PropertyChanged event for the specified property.
    /// Uses CallerMemberName attribute to automatically get the property name.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.
    /// If not provided, the caller member name is used automatically.</param>
    public virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Sets the backing field to the new value and raises PropertyChanged
    /// if the value has actually changed.
    /// This is a helper method to reduce boilerplate in property setters.
    /// </summary>
    /// <typeparam name="T">The type of the property.</typeparam>
    /// <param name="field">Reference to the backing field.</param>
    /// <param name="value">The new value to set.</param>
    /// <param name="propertyName">The name of the property.
    /// Automatically provided by CallerMemberName attribute.</param>
    /// <returns>True if the value was changed; false if the value was the same.</returns>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        // Don't update if the value hasn't changed
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
