// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AWS.Deploy.ServerMode.Client
{
    internal static class WaitUntilHelper
    {
        private static async Task WaitUntil(Func<Task<bool>> predicate, TimeSpan frequency, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var waitTask = Task.Run(async () =>
            {
                while (!await predicate())
                {
                    await Task.Delay(frequency, cancellationToken);
                }
            });

            if (waitTask != await Task.WhenAny(waitTask, Task.Delay(timeout)))
            {
                throw new TimeoutException();
            }
        }

        public static async Task<bool> WaitUntilSuccessStatusCode(string url, HttpClientHandler httpClientHandler, TimeSpan frequency, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var client = new HttpClient(httpClientHandler);

            try
            {
                await WaitUntil(async () =>
                {
                    try
                    {
                        var httpResponseMessage = await client.GetAsync(url, cancellationToken);
                        return httpResponseMessage.IsSuccessStatusCode;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }, frequency, timeout, cancellationToken);
            }
            catch (TimeoutException)
            {
                return false;
            }

            return true;
        }
    }
}
