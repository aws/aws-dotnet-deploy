// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using AWS.Deploy.Common;

namespace AWS.Deploy.CLI.UnitTests
{
    /// <summary>
    /// Helpful fake of <see cref="IAWSClientFactory"/>.  Pass in one or more
    /// Mock <see cref="IAmazonService"/>s and this will handle the plumbing.
    /// </summary>
    public class TestAWSClientFactory : IAWSClientFactory
    {
        private readonly IAmazonService[] _clients;

        public TestAWSClientFactory(params IAmazonService[] clientMocks)
        {
            _clients = clientMocks ?? new IAmazonService[0];
        }

        public T GetAWSClient<T>(string? awsRegion = null) where T : IAmazonService
        {
            var match = _clients.OfType<T>().FirstOrDefault();

            if (null == match)
                throw new Exception(
                    $"Test setup exception.  Somebody wanted a [{typeof(T)}] but I don't have it." +
                    $"I have the following clients: {string.Join(",", _clients.Select(x => x.GetType().Name))}");

            return match;
        }

        public void ConfigureAWSOptions(Action<AWSOptions> awsOptionsAction) => throw new NotImplementedException();
    }
}
