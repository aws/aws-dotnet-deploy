// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Amazon.Runtime.CredentialManagement;
using Xunit;

namespace AWS.Deploy.CLI.UnitTests
{
    public class CredentialProfileStoreChainFactoryTests
    {
        [Fact]
        public void Create_ReturnsCredentialProfileStoreChainInstance()
        {
            // Arrange
            var factory = new CredentialProfileStoreChainFactory();

            // Act
            var result = factory.Create();

            // Assert
            Assert.NotNull(result);
            Assert.IsType<CredentialProfileStoreChain>(result);
        }
    }
}
