// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Xunit;

namespace AWS.Deploy.CLI.IntegrationTests
{
    // Serializes web app deploy/session tests so parallel runs don't throttle Elastic Beanstalk APIs.
    [CollectionDefinition(nameof(ElasticBeanstalkTestCollection), DisableParallelization = true)]
    public class ElasticBeanstalkTestCollection
    {
    }
}
