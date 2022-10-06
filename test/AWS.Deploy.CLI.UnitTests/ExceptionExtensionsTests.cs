// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using AWS.Deploy.Common;
using Xunit;

namespace AWS.Deploy.CLI.UnitTests
{
    public class ExceptionExtensionsTests
    {
        [Fact]
        public void GetTruncatedErrorMessage_EmptyMessage()
        {
            // ARRANGE
            var ex = new Exception(string.Empty);

            // ACT and ASSERT
            Assert.Equal(string.Empty, ex.GetTruncatedErrorMessage());
        }

        [Fact]
        public void GetTruncatedErrorMessage_NoTruncation()
        {
            // ARRANGE
            var message = "This is an AWSDeployToolException";
            var ex = new Exception(message);

            // ACT and ASSERT
            // No truncation is performed because the message length < 2 * k
            Assert.Equal(message, ex.GetTruncatedErrorMessage(numChars: 50));
        }

        [Fact]
        public void GetTruncatedErrorMessage()
        {
            // ARRANGE
            var message =
                "error text. error text. error text. error text. error text. " +
                "error text. error text. error text. error text. error text. error text. " +
                "error text. error text. error text. error text. error text. error text. " +
                "error text. error text. error text. error text. error text. error text. " +
                "error text. error text. error text. error text. error text. error text. " +
                "error text. error text. error text. error text. error text. error text. " +
                "error text. error text. error text. error text. error text. error text. " +
                "error text. error text. error text. error text. error text. error text. " +
                "error text. error text. error text. error text. error text. error text. error text.";


            var ex = new Exception(message);

            // ACT
            var truncatedErrorMessage = ex.GetTruncatedErrorMessage(numChars: 50);

            // ACT and ASSERT
            var expectedMessage =
                @"error text. error text. error text. error text. er
...
Error truncated to the first and last 50 characters
...
t. error text. error text. error text. error text.";

            Assert.Equal(expectedMessage, truncatedErrorMessage);
        }
    }
}
