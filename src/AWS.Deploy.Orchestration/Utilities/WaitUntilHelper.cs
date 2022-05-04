// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading;
using System.Threading.Tasks;

namespace AWS.Deploy.Orchestration.Utilities
{
    public static class WaitUntilHelper
    {
        /// <summary>
        /// Wait for <see cref="timeout"/> TimeSpan until <see cref="Predicate{T}"/> isn't satisfied
        /// </summary>
        /// <param name="predicate">Termination condition for breaking the wait loop</param>
        /// <param name="frequency">Interval between the two executions of the task</param>
        /// <param name="timeout">Interval for timeout, if timeout passes, methods throws <see cref="TimeoutException"/></param>
        /// <exception cref="TimeoutException">Throws when timeout passes</exception>
        public static async Task WaitUntil(Func<Task<bool>> predicate, TimeSpan frequency, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            var waitTask = Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested && !await predicate())
                {
                    await Task.Delay(frequency);
                }
            });

            if (waitTask != await Task.WhenAny(waitTask, Task.Delay(timeout)))
            {
                throw new TimeoutException();
            }
        }
    }
}
