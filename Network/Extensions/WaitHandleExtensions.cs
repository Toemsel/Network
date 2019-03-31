using System;
using System.Threading;
using System.Threading.Tasks;

namespace Network.Extensions
{
    /// <summary>
    /// Provides additional functionality to the <see cref="WaitHandle"/> class.
    /// </summary>
    internal static class WaitHandleExtensions
    {
        #region Methods

        /// <summary>
        /// Returns a <see cref="Task"/> that represents asynchronously waiting for the <see cref="WaitHandle"/> instance to be set. There is no
        /// timeout for the wait operation.
        /// </summary>
        /// <param name="handle">The <see cref="WaitHandle"/> instance this extension method affects.</param>
        /// <returns>The <see cref="Task"/> representing the asynchronous operation.</returns>
        internal static Task AsTask(this WaitHandle handle)
        {
            return AsTask(handle, Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Returns a <see cref="Task"/> that represents asynchronously waiting for the <see cref="WaitHandle"/> instance to be set. Cancels the
        /// <see cref="Task"/> if the given timeout is exceeded.
        /// </summary>
        /// <param name="handle">The <see cref="WaitHandle"/> instance this extension method affects.</param>
        /// <param name="timeout">The <see cref="TimeSpan"/> to wait for the operation to complete before it is cancelled.</param>
        /// <returns>The <see cref="Task"/> representing the asynchronous operation.</returns>
        public static Task AsTask(this WaitHandle handle, TimeSpan timeout)
        {
            TaskCompletionSource<object> taskCompletionSource = new TaskCompletionSource<object>();

            //We register a wait handle on the default thread pool so that we can await its setting as a task
            RegisteredWaitHandle taskRegistrationHandle = ThreadPool.RegisterWaitForSingleObject(handle, (state, timedOut) =>
                    {
                        TaskCompletionSource<object> localTcs =
                            (TaskCompletionSource<object>)state;

                        if (timedOut)
                        {
                            localTcs.TrySetCanceled();
                        }
                        else
                        {
                            localTcs.TrySetResult(null);
                        }
                    }, taskCompletionSource, timeout, true);

            //Once the task completes we deregister it from the thread pool
            taskCompletionSource.Task.ContinueWith((_, state) =>
                {
                    RegisteredWaitHandle localWaitHandle =
                        (RegisteredWaitHandle)state;

                    localWaitHandle.Unregister(null);
                }, taskRegistrationHandle, TaskScheduler.Default);

            return taskCompletionSource.Task;
        }

        #endregion Methods
    }
}