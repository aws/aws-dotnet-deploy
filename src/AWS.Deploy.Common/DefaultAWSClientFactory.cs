// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Amazon;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;

namespace AWS.Deploy.Common
{
    public class DefaultAWSClientFactory : IAWSClientFactory
    {
        private readonly AWSCredentials _credentials;
        private readonly string _region;

        public DefaultAWSClientFactory(AWSCredentials credentials, string region)
        {
            _credentials = credentials;
            _region = region;
        }

        public T GetAWSClient<T>() where T : IAmazonService
        {
            var awsOptions = new AWSOptions { Credentials = _credentials, Region = RegionEndpoint.GetBySystemName(_region) };

            return awsOptions.CreateServiceClient<T>();
        }
    }
}
