// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Amazon.Runtime;
using Xunit;

namespace AWS.Deploy.CLI.UnitTests
{
    public class AWSCredentialsFactoryTests
    {
        [Fact]
        public void Create_ReturnsAWSCredentialsInstance()
        {
            // Arrange
            var factory = new AWSCredentialsFactory();

            // Act
            var result = factory.Create();

            // Assert
            Assert.NotNull(result);
            Assert.IsAssignableFrom<AWSCredentials>(result);
        }
    }
}
