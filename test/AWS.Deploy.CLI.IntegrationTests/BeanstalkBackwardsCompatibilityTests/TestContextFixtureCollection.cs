// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Xunit;

namespace AWS.Deploy.CLI.IntegrationTests.BeanstalkBackwardsCompatibilityTests
{
    [CollectionDefinition(nameof(TestContextFixture), DisableParallelization = true)]
    public class TestContextFixtureCollection : ICollectionFixture<TestContextFixture>
    {
    }
}
