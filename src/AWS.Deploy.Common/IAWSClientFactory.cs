// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Amazon.Runtime;

namespace AWS.Deploy.Common
{
    public interface IAWSClientFactory
    {
        T GetAWSClient<T>(AWSCredentials credentials, string region) where T : IAmazonService;
    }
}
