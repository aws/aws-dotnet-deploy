// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace AWS.Deploy.CLI.IntegrationTests.Helpers
{
    public class HttpHelper
    {
        public async Task WaitUntilSuccessStatusCode(string url, TimeSpan frequency, TimeSpan timeout)
        {
            using var client = new HttpClient();

            try
            {
                await WaitUntilHelper.WaitUntil(async () =>
                {
                    var httpResponseMessage = await client.GetAsync(url);
                    return httpResponseMessage.IsSuccessStatusCode;
                }, frequency, timeout);
            }
            catch (TimeoutException)
            {
                Assert.True(false, $"{url} URL is not reachable.");
            }
        }
    }
}
