// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Amazon.Runtime.CredentialManagement;
using Xunit;

namespace AWS.Deploy.CLI.UnitTests
{
    public class SharedCredentialsFileFactoryTests
    {
        [Fact]
        public void Create_ReturnsSharedCredentialsFileInstance()
        {
            // Arrange
            var factory = new SharedCredentialsFileFactory();

            // Act
            var result = factory.Create();

            // Assert
            Assert.NotNull(result);
            Assert.IsType<SharedCredentialsFile>(result);
        }
    }
}
