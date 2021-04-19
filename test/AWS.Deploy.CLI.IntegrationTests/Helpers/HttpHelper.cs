// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System.Net.Http;
using System.Threading.Tasks;

namespace AWS.Deploy.CLI.IntegrationTests.Helpers
{
    public class HttpHelper
    {
        public async Task<bool> IsSuccessStatusCode(string url)
        {
            using var client = new HttpClient();
            var result = await client.GetAsync(url);
            return result.IsSuccessStatusCode;
        }
    }
}
