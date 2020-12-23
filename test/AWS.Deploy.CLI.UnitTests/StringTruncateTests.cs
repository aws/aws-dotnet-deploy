// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using AWS.Deploy.CLI.Extensions;
using Xunit;

namespace AWS.Deploy.CLI.UnitTests
{
    public class StringTruncateTests
    {
        [Fact]
        public void Truncate_MaxLengthLessThanThree_Ellipsis_Throws()
        {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                "Hi".Truncate(2, true);
            });
            Assert.Equal("maxLength must be greater than three when replacing with an ellipsis. (Parameter 'maxLength')", exception.Message);
        }

        [Theory]
        [InlineData("Hello World", "He...")]
        public void Truncate_Ellipsis(string input, string expectedOutput)
        {
            var output = input.Truncate(5, true);
            Assert.Equal(expectedOutput, output);
        }

        [Theory]
        [InlineData("Hello World", "Hello")]
        [InlineData("Hello", "Hello")]
        [InlineData("", "")]
        public void Truncate(string input, string expectedOutput)
        {
            var output = input.Truncate(5);
            Assert.Equal(expectedOutput, output);
        }
    }
}
