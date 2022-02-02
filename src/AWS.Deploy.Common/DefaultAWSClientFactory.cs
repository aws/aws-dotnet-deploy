// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using Amazon;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;

namespace AWS.Deploy.Common
{
    public class DefaultAWSClientFactory : IAWSClientFactory
    {
        private Action<AWSOptions>? _awsOptionsAction;

        public void ConfigureAWSOptions(Action<AWSOptions> awsOptionsAction)
        {
            _awsOptionsAction = awsOptionsAction;
        }

        public T GetAWSClient<T>(string? awsRegion = null) where T : IAmazonService
        {
            var awsOptions = new AWSOptions();

            _awsOptionsAction?.Invoke(awsOptions);

            if (!string.IsNullOrEmpty(awsRegion))
                awsOptions.Region = RegionEndpoint.GetBySystemName(awsRegion);

            return awsOptions.CreateServiceClient<T>();
        }
    }
}
