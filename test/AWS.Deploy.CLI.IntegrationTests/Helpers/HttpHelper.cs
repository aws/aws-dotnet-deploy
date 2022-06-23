// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Net.Http;
using System.Threading.Tasks;
using AWS.Deploy.CLI.IntegrationTests.Services;
using AWS.Deploy.Common;
using AWS.Deploy.Orchestration.Utilities;
using Xunit;

namespace AWS.Deploy.CLI.IntegrationTests.Helpers
{
    public class HttpHelper
    {
        private readonly InMemoryInteractiveService _interactiveService;

        public HttpHelper(InMemoryInteractiveService interactiveService)
        {
            _interactiveService = interactiveService;
        }

        public async Task WaitUntilSuccessStatusCode(string url, TimeSpan frequency, TimeSpan timeout)
        {
            using var client = new HttpClient();

            try
            {
                await Orchestration.Utilities.Helpers.WaitUntil(async () =>
                {
                    var httpResponseMessage = await client.GetAsync(url);
                    return httpResponseMessage.IsSuccessStatusCode;
                }, frequency, timeout);
            }
            catch (TimeoutException ex)
            {
                _interactiveService.WriteErrorLine(ex.PrettyPrint());
                Assert.True(false, $"{url} URL is not reachable.");
            }
        }
    }
}
