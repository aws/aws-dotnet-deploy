// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using AWS.Deploy.ServerMode.Client.Utilities;
using Xunit;

namespace AWS.Deploy.ServerMode.Client.UnitTests
{
    public class CappedStringBuilderTests
    {
        private readonly CappedStringBuilder _cappedStringBuilder;

        public CappedStringBuilderTests()
        {
            _cappedStringBuilder = new CappedStringBuilder(5);
        }

        [Fact]
        public void AppendLineTest()
        {
            _cappedStringBuilder.AppendLine("test1");
            _cappedStringBuilder.AppendLine("test2");
            _cappedStringBuilder.AppendLine("test3");

            Assert.Equal(3, _cappedStringBuilder.LineCount);
            Assert.Equal($"test1{Environment.NewLine}test2{Environment.NewLine}test3", _cappedStringBuilder.ToString());
        }

        [Fact]
        public void GetLastLinesTest()
        {
            _cappedStringBuilder.AppendLine("test1");
            _cappedStringBuilder.AppendLine("test2");

            Assert.Equal(2, _cappedStringBuilder.LineCount);
            Assert.Equal("test2", _cappedStringBuilder.GetLastLines(1));
            Assert.Equal($"test1{Environment.NewLine}test2", _cappedStringBuilder.GetLastLines(2));
        }
    }
}
