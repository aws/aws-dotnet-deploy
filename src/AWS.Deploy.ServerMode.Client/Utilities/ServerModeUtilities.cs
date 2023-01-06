// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using Amazon.Runtime;
using System.Threading.Tasks;
using System.Threading;

namespace AWS.Deploy.ServerMode.Client.Utilities
{
    public static class ServerModeUtilities
    {
        /// <summary>
        /// Checks server mode health API and waits until the API returns a `Ready` status.
        /// This is useful when initializing a server mode connection to make sure server mode is ready to accept requests.
        /// </summary>
        public static async Task WaitUntilServerModeReady(this RestAPIClient restApiClient, CancellationToken cancellationToken = default(CancellationToken))
        {
            await WaitUntilHelper.WaitUntil(async () =>
            {
                var status = SystemStatus.Error;
                try
                {
                    status = (await restApiClient.HealthAsync()).Status;
                }
                catch (Exception)
                {
                }

                return status == SystemStatus.Ready;
            }, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10), cancellationToken);
        }

        /// <summary>
        /// Uses AWS .NET SDK <see cref="FallbackCredentialsFactory"/> to resolve the default credentials from multiple fallback sources.
        /// This includes AWS credentials file stored on the local machine, environment variables , etc...
        /// This method does not take into account the AWS Profile and Region defined on the CLI level for AWS Deploy Tool for .NET.
        /// </summary>
        public static Task<AWSCredentials> ResolveDefaultCredentials()
        {
            var testCredentials = FallbackCredentialsFactory.GetCredentials();
            return Task.FromResult(testCredentials);
        }
    }
}
