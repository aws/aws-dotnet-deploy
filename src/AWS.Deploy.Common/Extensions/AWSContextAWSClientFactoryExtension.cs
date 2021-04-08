// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using Amazon;
using Amazon.Runtime;

namespace AWS.Deploy.Common.Extensions
{
    public static class AWSContextAWSClientFactoryExtension
    {
        /// AWS Credentials and Region information is determined after DI container is built.
        /// <see cref="RegisterAWSContext"/> extension method allows to register late bound properties (credentials & region) to
        /// <see cref="IAWSClientFactory"/> instance.
        public static void RegisterAWSContext(this IAWSClientFactory awsClientFactory,
            AWSCredentials awsCredentials,
            string region)
        {
            awsClientFactory.ConfigureAWSOptions(awsOption =>
            {
                awsOption.Credentials = awsCredentials;
                awsOption.Region = RegionEndpoint.GetBySystemName(region);
            });
        }
    }
}
