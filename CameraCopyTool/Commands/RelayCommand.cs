using System.Windows.Input;

namespace CameraCopyTool.Commands;

/// <summary>
/// A command implementation that supports synchronous operations and CanExecute notifications.
/// Implements the ICommand interface for WPF command binding.
/// This is a standard RelayCommand pattern used throughout WPF applications.
/// </summary>
public class RelayCommand : ICommand
{
    /// <summary>
    /// The action to execute when the command is invoked.
    /// </summary>
    private readonly Action<object?> _execute;

    /// <summary>
    /// Optional predicate to determine if the command can execute.
    /// If null, the command can always execute.
    /// </summary>
    private readonly Predicate<object?>? _canExecute;

    /// <summary>
    /// Flag to prevent re-entrant execution of the command.
    /// </summary>
    private bool _isExecuting;

    /// <summary>
    /// Initializes a new instance of the <see cref="RelayCommand"/> class.
    /// </summary>
    /// <param name="execute">The action to execute when the command is invoked.</param>
    /// <param name="canExecute">Optional predicate to determine if the command can execute.</param>
    /// <exception cref="ArgumentNullException">Thrown when execute is null.</exception>
    public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <summary>
    /// Determines whether the command can execute in its current state.
    /// </summary>
    /// <param name="parameter">Data used by the command. Not used in this implementation.</param>
    /// <returns>True if the command can execute; otherwise, false.</returns>
    public bool CanExecute(object? parameter)
    {
        // Prevent execution if already executing
        return !_isExecuting && (_canExecute == null || _canExecute(parameter));
    }

    /// <summary>
    /// Executes the command.
    /// Only executes if CanExecute returns true.
    /// </summary>
    /// <param name="parameter">Data used by the command. Passed to the execute action.</param>
    public void Execute(object? parameter)
    {
        if (CanExecute(parameter))
        {
            try
            {
                _isExecuting = true;
                // Notify WPF that command state has changed
                CommandManager.InvalidateRequerySuggested();
                _execute(parameter);
            }
            finally
            {
                _isExecuting = false;
                // Notify WPF that command state has changed
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    /// <summary>
    /// Event raised when the command's ability to execute changes.
    /// WPF uses this to update UI element enabled/disabled states.
    /// </summary>
    public event EventHandler? CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }

    /// <summary>
    /// Manually raises the CanExecuteChanged event.
    /// Call this when the conditions for CanExecute may have changed.
    /// </summary>
    public void RaiseCanExecuteChanged()
    {
        CommandManager.InvalidateRequerySuggested();
    }
}
