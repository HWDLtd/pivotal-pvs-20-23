namespace TabletController.Shared.Utilities
{
    public static class TaskExtensions
    {
        /// <summary>
        /// Safely executes a task without awaiting, ensuring exceptions are logged rather than swallowed.
        /// Use this instead of the _ = SomeAsyncMethod() pattern.
        /// </summary>
        /// <param name="task">The task to execute.</param>
        /// <param name="onException">Optional callback for handling exceptions.</param>
        public static async void SafeFireAndForget(this Task task, Action<Exception>? onException = null)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                onException?.Invoke(ex);
                System.Diagnostics.Debug.WriteLine($"SafeFireAndForget exception: {ex}");
            }
        }

        /// <summary>
        /// Safely executes a task without awaiting, ensuring exceptions are logged rather than swallowed.
        /// Use this instead of the _ = SomeAsyncMethod() pattern.
        /// </summary>
        /// <typeparam name="T">The result type of the task.</typeparam>
        /// <param name="task">The task to execute.</param>
        /// <param name="onException">Optional callback for handling exceptions.</param>
        public static async void SafeFireAndForget<T>(this Task<T> task, Action<Exception>? onException = null)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                onException?.Invoke(ex);
                System.Diagnostics.Debug.WriteLine($"SafeFireAndForget exception: {ex}");
            }
        }
    }
}
