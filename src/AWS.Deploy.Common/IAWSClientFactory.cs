// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;

namespace AWS.Deploy.Common
{
    public interface IAWSClientFactory
    {
        T GetAWSClient<T>() where T : IAmazonService;
        void ConfigureAWSOptions(Action<AWSOptions> awsOptionsAction);
    }
}
