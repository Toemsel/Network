#region Licence - LGPLv3

// ***********************************************************************
// Assembly         : Network
// Author           : Thomas Christof
// Created          : 02-11-2016
//
// Last Modified By : Thomas Christof
// Last Modified On : 10-10-2015
// ***********************************************************************
// <copyright>
// Company: Indie-Dev
// Thomas Christof (c) 2018
// </copyright>
// <License>
// GNU LESSER GENERAL PUBLIC LICENSE
// </License>
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
// ***********************************************************************

#endregion Licence - LGPLv3

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Network.Extensions
{
    /// <summary>
    /// Provides additional functionality to the <see cref="WaitHandle"/>
    /// class.
    /// </summary>
    internal static class WaitHandleExtensions
    {
        #region Methods

        /// <summary>
        /// Returns a <see cref="Task"/> that represents asynchronously waiting
        /// for the <see cref="WaitHandle"/> instance to be set. There is no
        /// timeout for the wait operation.
        /// </summary>
        /// <param name="handle">
        /// The <see cref="WaitHandle"/> instance this extension method affects.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public static Task AsTask(this WaitHandle handle)
        {
            return AsTask(handle, Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Returns a <see cref="Task"/> that represents asynchronously waiting
        /// for the <see cref="WaitHandle"/> instance to be set. Cancels the
        /// <see cref="Task"/> if the given timeout is exceeded.
        /// </summary>
        /// <param name="handle">
        /// The <see cref="WaitHandle"/> instance this extension method affects.
        /// </param>
        /// <param name="timeout">
        /// The <see cref="TimeSpan"/> to wait for the operation to complete
        /// before it is cancelled.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public static Task AsTask(this WaitHandle handle, TimeSpan timeout)
        {
            TaskCompletionSource<object> taskCompletionSource =
                new TaskCompletionSource<object>();

            // we register a wait handle on the default thread pool so that
            // we can await its setting as a task
            RegisteredWaitHandle taskRegistationHandle =
                ThreadPool.RegisterWaitForSingleObject(handle, (state, timedOut) =>
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

            // once the task completes we deregister it from the thread pool
            taskCompletionSource.Task.ContinueWith((_, state) =>
            {
                RegisteredWaitHandle localWaitHandle =
                    (RegisteredWaitHandle)state;

                localWaitHandle.Unregister(null);
            }, taskRegistationHandle, TaskScheduler.Default);

            return taskCompletionSource.Task;
        }

        #endregion Methods
    }
}