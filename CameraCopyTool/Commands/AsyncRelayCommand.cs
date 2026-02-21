using System.Windows.Input;

namespace CameraCopyTool.Commands;

/// <summary>
/// An async command implementation that supports Task-based operations.
/// Implements the ICommand interface for WPF command binding with async/await support.
/// Prevents re-entrant execution while the async operation is in progress.
/// </summary>
public class AsyncRelayCommand : ICommand
{
    /// <summary>
    /// The async function to execute when the command is invoked.
    /// </summary>
    private readonly Func<object?, Task> _execute;

    /// <summary>
    /// Optional predicate to determine if the command can execute.
    /// If null, the command can always execute (when not already executing).
    /// </summary>
    private readonly Predicate<object?>? _canExecute;

    /// <summary>
    /// Flag to prevent re-entrant execution of the command.
    /// Set to true while the async operation is in progress.
    /// </summary>
    private bool _isExecuting;

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncRelayCommand"/> class.
    /// </summary>
    /// <param name="execute">The async function to execute when the command is invoked.</param>
    /// <param name="canExecute">Optional predicate to determine if the command can execute.</param>
    /// <exception cref="ArgumentNullException">Thrown when execute is null.</exception>
    public AsyncRelayCommand(Func<object?, Task> execute, Predicate<object?>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <summary>
    /// Determines whether the command can execute in its current state.
    /// Returns false if the command is currently executing.
    /// </summary>
    /// <param name="parameter">Data used by the command. Not used in this implementation.</param>
    /// <returns>True if the command can execute; otherwise, false.</returns>
    public bool CanExecute(object? parameter)
    {
        // Prevent execution if already executing
        return !_isExecuting && (_canExecute == null || _canExecute(parameter));
    }

    /// <summary>
    /// Executes the command asynchronously.
    /// Only executes if CanExecute returns true.
    /// Uses async void to fire-and-forget the async operation.
    /// </summary>
    /// <param name="parameter">Data used by the command. Passed to the execute function.</param>
    public async void Execute(object? parameter)
    {
        if (CanExecute(parameter))
        {
            try
            {
                _isExecuting = true;
                // Notify WPF that command state has changed
                CommandManager.InvalidateRequerySuggested();
                await _execute(parameter);
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
    /// Subscribes to CommandManager.RequerySuggested for automatic updates.
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

/// <summary>
/// Generic async command for strongly-typed parameters.
/// Provides type-safe command execution with async/await support.
/// </summary>
/// <typeparam name="T">The type of the command parameter.</typeparam>
public class AsyncRelayCommand<T> : ICommand
{
    /// <summary>
    /// The async function to execute when the command is invoked.
    /// </summary>
    private readonly Func<T?, Task> _execute;

    /// <summary>
    /// Optional predicate to determine if the command can execute.
    /// </summary>
    private readonly Predicate<T?>? _canExecute;

    /// <summary>
    /// Flag to prevent re-entrant execution of the command.
    /// </summary>
    private bool _isExecuting;

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncRelayCommand{T}"/> class.
    /// </summary>
    /// <param name="execute">The async function to execute when the command is invoked.</param>
    /// <param name="canExecute">Optional predicate to determine if the command can execute.</param>
    /// <exception cref="ArgumentNullException">Thrown when execute is null.</exception>
    public AsyncRelayCommand(Func<T?, Task> execute, Predicate<T?>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <summary>
    /// Determines whether the command can execute in its current state.
    /// </summary>
    /// <param name="parameter">Data used by the command. Cast to type T.</param>
    /// <returns>True if the command can execute; otherwise, false.</returns>
    public bool CanExecute(object? parameter)
    {
        return !_isExecuting && (_canExecute == null || _canExecute((T?)parameter));
    }

    /// <summary>
    /// Executes the command asynchronously.
    /// Only executes if CanExecute returns true.
    /// </summary>
    /// <param name="parameter">Data used by the command. Cast to type T before passing to execute function.</param>
    public async void Execute(object? parameter)
    {
        if (CanExecute(parameter))
        {
            try
            {
                _isExecuting = true;
                CommandManager.InvalidateRequerySuggested();
                await _execute((T?)parameter);
            }
            finally
            {
                _isExecuting = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    /// <summary>
    /// Event raised when the command's ability to execute changes.
    /// </summary>
    public event EventHandler? CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }

    /// <summary>
    /// Manually raises the CanExecuteChanged event.
    /// </summary>
    public void RaiseCanExecuteChanged()
    {
        CommandManager.InvalidateRequerySuggested();
    }
}
